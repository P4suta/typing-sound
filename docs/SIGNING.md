# Code Signing — Authenticode Signing of Distributables

Runbook for Authenticode-signing TypingSound's own binaries in the distribution
zip with **SSL.com eSigner**, via the official **`SSLcom/esigner-codesign` Action**
(`command: batch_sign`). For the provider / certificate rationale see
[ADR-0002](adr/0002-code-signing-provider.md); for the pipeline shape see
[ADR-0003](adr/0003-ci-signing-pipeline.md).

## What is signed

Only the **project's own PE files** are signed — there are exactly two:

| File | Role |
|---|---|
| `TypingSound.exe` | the portable launcher / entry exe the user double-clicks (main SmartScreen target) |
| `app\TypingSound.App.exe` | the WinUI 3 app host |

The bundled .NET / Windows App SDK runtime DLLs are already Microsoft-signed and
are **not** re-signed (to avoid wasting the signing quota and signing others'
copyrighted works).

The certificate is a personal **Individual Validation (IV)** code-signing cert from
SSL.com, subject **`CN=Yasunobu Sakashita`** (code-signing EKU).

## How it works (pipeline shape)

`release.yml` runs three jobs so the signing credentials touch the smallest
surface (ADR-0003):

1. **build** — assembles the bundle + SBOMs and uploads them as artifacts. No
   secrets, read-only token.
2. **sign** — downloads the bundle, runs the **`SSLcom/esigner-codesign` Action**
   (`batch_sign`: CodeSignTool scans then signs, timestamping via SSL.com's TSA),
   copies the signed files back, then hard-verifies. **This is the only job that
   sees the signing secrets**, so it runs in the approval-gated **`release`
   environment**.
3. **publish** — downloads the signed bundle, **re-verifies it is signed** (a gate
   at the irreversible boundary, using the same chain + timestamp + signer check),
   packages the zip + `SHA256SUMS.txt`, writes keyless attestations, attaches
   everything to the release-please draft, then publishes it. Gated on
   `publish=true`.

Signing is **gated at publish**: a real publish re-verifies both PE files are
validly signed and **hard-fails before creating the immutable Release** if they are
not — so a missing or misconfigured signing secret can never ship an unsigned
release unnoticed. Verification enforces a valid chain, an RFC 3161 timestamp, and
the expected signer:

```text
signtool verify /pa /tw <file>     # exit 0 = chain valid + timestamped; 2 = no timestamp; 1 = invalid
Get-AuthenticodeSignature <file>   # SignerCertificate.Subject must contain CN=Yasunobu Sakashita
```

`Get-AuthenticodeSignature.TimeStamperCertificate` is **not** used for the timestamp
check (it is null under `-FilePath` on the runner); the timestamp guarantee comes
from `signtool /tw`.

## Background (why this setup)

- The official `SSLcom/esigner-codesign` Action is SSL.com's recommended GitHub
  Actions integration and needs no hardware token on the runner (cloud HSM +
  TOTP → fully unattended CI signing).
- The 2026 managed flow (**Azure Trusted Signing + `dotnet sign` + OIDC**) is
  **unavailable**: Trusted Signing paused individual onboarding (US/CA orgs, 3+
  years only), and `dotnet sign` only delegates to Azure — never SSL.com. See
  [ADR-0002](adr/0002-code-signing-provider.md).
- **EV no longer grants immediate SmartScreen trust** (Microsoft changed this in
  March 2024). This app ships no kernel driver, so individual-name **IV** is
  sufficient. SmartScreen is reputation-based: even when signed, a warning may
  appear on first run and disappears as download history accumulates. The immediate
  effect of signing is that "unknown publisher" is replaced by **your name**.

## Activation / renewal (SSL.com)

Follow these when first activating or when renewing / re-issuing the certificate.

1. Create an account at [SSL.com](https://www.ssl.com/) and purchase a **Code
   Signing** certificate with **Individual Validation (IV) + eSigner (cloud
   signing)** (not the USB-token version).
2. Complete IV identity verification (government-issued ID; no corporate
   registration required).
3. In the SSL.com dashboard, configure eSigner for automated signing and note the
   **Credential ID**, the **TOTP secret** (Base32), and the account
   **username / password**.
4. Register the four secrets in the **`release`** GitHub Environment (not repo
   level) — see [RELEASING.md](RELEASING.md) §(b): `ES_USERNAME`, `ES_PASSWORD`,
   `CREDENTIAL_ID`, `ES_TOTP_SECRET`.

> Publicly trusted code-signing certificates are valid for at most ~460 days
> (CA/Browser Forum). Renew before expiry. If the **subject** ever changes, update
> the signer-subject assertion (`CN=Yasunobu Sakashita`) in `release.yml` too.

## Verification

- **Signing smoke test** (safe under immutable releases): run Actions → release via
  `workflow_dispatch` with `publish=false`. The `build` job runs, the `sign` job
  pauses for approval; approve it and confirm the verify step prints a valid chain +
  timestamp + `CN=Yasunobu Sakashita` for both files. The run ends after `sign` (no
  Release, because `publish=false`).
- **Real release**: merge the `release: approved` Release PR — release-please
  dispatches `release.yml`; approve `sign` then `publish`.
- **Local confirmation** on the downloaded zip:
  ```powershell
  signtool verify /pa /tw /v TypingSound.exe        # → Successfully verified, with a timestamp
  Get-AuthenticodeSignature TypingSound.exe          # → Status: Valid
  Get-AuthenticodeSignature app\TypingSound.App.exe  # → Status: Valid
  ```

## Related

- [ADR-0002 — Code signing provider](adr/0002-code-signing-provider.md)
- [ADR-0003 — CI signing pipeline](adr/0003-ci-signing-pipeline.md)
- [SUPPLY_CHAIN.md](SUPPLY_CHAIN.md)
- Wiring itself: `.github/workflows/release.yml`
