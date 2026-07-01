# ADR-0003: CI signing pipeline — official SSL.com Action, build/sign/publish split, approval gate

Date: 2026-07-01 / Status: Accepted (provider/cert per [ADR-0002](0002-code-signing-provider.md))

The signing *provider and certificate* stay as [ADR-0002](0002-code-signing-provider.md)
decided (SSL.com eSigner, personal IV cert `CN=Yasunobu Sakashita`). This ADR
records the *pipeline shape and hardening* around the signing step.

## Decision

1. **Sign with the official `SSLcom/esigner-codesign` Action (`command: batch_sign`).**
   The Action downloads CodeSignTool, runs a pre-signing malware scan then signs, and
   timestamps via SSL.com's TSA. It is **SHA-pinned**. We sign only TypingSound's own
   two PEs (`TypingSound.exe`, `app\TypingSound.App.exe`), staged into an explicit
   `output_path` (the Action ignores `override`) and copied back — the Authenticode
   signature lives inside the PE, so the round-trip preserves it.

2. **Split `release.yml` into three jobs — `build` → `sign` → `publish` — passing the
   bundle as an artifact.** Only `sign` ever sees the signing secrets; `build` and
   `publish` run with a read-only / least-privilege token on a bundle they receive.
   This shrinks the credential blast radius.

3. **Gate the secrets behind an approval-gated `release` GitHub Environment on the
   `sign` job.** The eSigner secrets (`ES_USERNAME` / `ES_PASSWORD` / `CREDENTIAL_ID`
   / `ES_TOTP_SECRET`) are Environment secrets (not repo-level), with required
   reviewers (the maintainer) and `main`-only deployment, so a `workflow_dispatch`
   from an arbitrary ref or a compromised workflow cannot mint signatures unattended.

4. **Verify with `signtool verify /pa /tw` + a signer-subject assertion, shared
   between the sign self-check and the publish gate.** `/tw` makes a missing
   timestamp a non-zero exit (0 = chain valid + timestamped, 2 = untimestamped,
   1 = invalid); the subject check (`*CN=Yasunobu Sakashita*`) refuses a
   valid-but-wrong certificate. `Get-AuthenticodeSignature.TimeStamperCertificate` is
   **not** used (it is null under `-FilePath` on the runner), so the timestamp
   guarantee comes from `signtool`. The `publish` job re-runs this identical check at
   the irreversible boundary and **hard-fails an unsigned publish** — an unsigned
   bundle can never become an immutable Release.

Signing stays **non-blocking** (secrets absent → `::warning::`, unsigned) and
dispatch-driven only. A `publish=false` `workflow_dispatch` is a safe signing smoke
test (build + sign + verify, no Release).

## Rationale

- **Official Action over eSigner CKA**: the Action is SSL.com's documented, supported
  CI integration; SHA-pinnable; CodeSignTool sends only file hashes (source never
  leaves the runner) and timestamps automatically. The eSigner CKA +
  standard-`signtool` alternative fails in CI at the KSP credential-retrieval step
  (`SignerSign() 0x80090003`).
- **Three jobs over one**: defense in depth — a compromised SBOM tool or build step
  can't read signing secrets it never receives; `publish`'s write/attestation token
  never coexists with the signing credentials.
- **Shared hardened verify**: running the exact same chain + timestamp + signer check
  in both `sign` and `publish` means the irreversible step can't be reached by an
  unsigned or wrong-signer bundle.

## Rejected alternatives

- **eSigner CKA + standard signtool** — would drop the copy-back, but fails in CI
  (KSP credential retrieval). Rejected on evidence.
- **Migrate to SignPath / Azure Trusted Signing + `dotnet sign`** — Azure is
  unavailable to a Japanese individual (onboarding paused); SignPath is a provider
  migration that strands the already-purchased SSL.com cert. Rejected.

## Consequences

- `HAVE_SIGNING` keys on the presence of `ES_USERNAME` + `CREDENTIAL_ID`. The
  `release` environment holds the four secrets.
- Every release run pauses for reviewer approval before `sign`, and again before
  `publish`. The bundle round-trips through Actions artifacts (build→sign→publish);
  a few extra minutes on a release-only workflow.
- [docs/SIGNING.md](../SIGNING.md) is the runbook.

## Re-examination triggers

- **Azure Trusted Signing opens to individuals in Japan** → `dotnet sign` + OIDC
  becomes reachable; re-evaluate.
- **Artifact round-trip cost becomes painful** → collapse toward fewer jobs (the
  split's value is secret isolation, not job count).
