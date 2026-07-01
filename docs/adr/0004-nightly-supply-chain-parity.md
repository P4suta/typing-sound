# ADR-0004: Nightly carries full supply-chain provenance (signing stays stable-only)

Date: 2026-07-01 / Status: Accepted (CI-only)

## Context

[ADR-0001](0001-automated-versioning-with-release-please.md) made nightly an
unsigned 14-day GitHub Actions artifact (not a Release), and
[ADR-0003](0003-ci-signing-pipeline.md) keeps Authenticode signing dispatch-driven
and approval-gated (`release` environment), so signing is deliberately stable-only —
eSigner has a quota and a human approval gate. That part is industry-normal.

But the supply-chain gap between the channels was wider than signing. If only
`release.yml` generated a CycloneDX SBOM, ran the osv-scanner gate
([ADR-0005](0005-sbom-consumed-by-osv-scanner.md)), and issued keyless
build-provenance + SBOM attestations, then a nightly would ship with **no SBOM, no
attestation, no scan** — and nothing justifies that gap. By SLSA, **every
distributed artifact — nightlies included — should carry build provenance**, and
GitHub's `actions/attest-build-provenance` / `actions/attest` are keyless (Sigstore
Fulcio/Rekor via the workflow OIDC token): no stored secret, no human approval, no
eSigner quota.

## Decision

Give nightly the **same supply-chain artefacts as a release, minus signing**:

1. **CycloneDX 1.6 SBOM + the osv-scanner gate** run in `nightly.yml`, exactly as in
   `release.yml`. A known-vulnerable dependency in the resolved closure fails the
   nightly too.
2. **Keyless build-provenance attestation** over the zip + `SHA256SUMS.txt`, and an
   **SBOM attestation**, via the build job's `id-token: write` /
   `attestations: write` permissions. No secrets, no approval gate — so a nightly is
   `gh attestation verify`-able.
3. The SBOM is **added to the 14-day artifact** so a tester gets it alongside the zip.
4. **Signing is unchanged** — still dispatch-driven, approval-gated, stable-only
   ([ADR-0003](0003-ci-signing-pipeline.md)). A nightly stays unsigned; the
   Authenticode signature is the *only* remaining stable-only supply-chain gate.

## Rationale

- **SLSA expects provenance on every distributed build.** Keyless attestation is free
  and unattended — there was no cost reason to withhold it from nightly.
- **Signing is the legitimate stable-only gate**, not SBOM/provenance: eSigner has a
  quota and a human approval gate; attestation/SBOM have neither.

## Trade-off

A nightly's SBOM is attached + attested but **not re-scanned by `sbom-monitor.yml`** —
that monitor only tracks the latest *Release*, and a nightly expires in 14 days, so
post-hoc monitoring of it would be pointless. The build-time osv-scanner gate still
applies. The nightly's keyless attestations persist in the Attestations tab even
after the artifact expires (harmless, still verifiable for anyone who kept the
download).

## Rejected alternatives

- **Sign nightlies too.** Rejected: keeps the eSigner quota + approval-gate cost that
  ADR-0001/0003 deliberately reserve for stable. The "signed nightly wanted" trigger
  governs that, and is now the *only* supply-chain difference between the channels.
- **Leave nightly checksum-only and document the gap.** Rejected: it would document a
  non-decision; SLSA says ship the provenance, and it's free.

## Re-examination triggers

- **Signed nightly wanted** → add a `sign` job to `nightly.yml` (reuses ADR-0003's
  pipeline). This is now the sole remaining nightly/release supply-chain difference.
