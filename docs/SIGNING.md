# Code Signing — Authenticode Signing of Distributables

Runbook for Authenticode-signing TypingSound's own binaries with **SSL.com eSigner**
via the official **`SSLcom/esigner-codesign` Action** (`command: batch_sign`).
Provider / certificate rationale: [ADR-0002](adr/0002-code-signing-provider.md).
Pipeline shape and hardening (build → sign → publish split, approval gate, shared
verify): [ADR-0003](adr/0003-ci-signing-pipeline.md).

## What is signed

Only the **project's own PE files** — exactly two:

| File | Role |
|---|---|
| `TypingSound.exe` | the portable launcher / entry exe the user double-clicks (main SmartScreen target) |
| `app\TypingSound.App.exe` | the WinUI 3 app host |

The bundled .NET / Windows App SDK runtime DLLs are already Microsoft-signed and are
**not** re-signed. The certificate is a personal **Individual Validation (IV)**
code-signing cert from SSL.com, subject **`CN=Yasunobu Sakashita`** (code-signing EKU).

Signing is **non-blocking** when the secrets are unset (finishes unsigned with a
`::warning::`) and runs only in the dispatch-driven `release.yml`. The `publish` job
re-verifies both PEs and **hard-fails an unsigned publish** before the immutable
Release is created — a missing or misconfigured secret can never ship an unsigned
release unnoticed.

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
  timestamp + `CN=Yasunobu Sakashita` for both files. The run ends after `sign`.
- **Real release**: merge the `release: approved` Release PR — release-please
  dispatches `release.yml`; approve `sign` then `publish`.
- **Local confirmation** on the downloaded zip:
  ```powershell
  signtool verify /pa /tw /v TypingSound.exe        # → Successfully verified, with a timestamp
  Get-AuthenticodeSignature TypingSound.exe          # → Status: Valid
  Get-AuthenticodeSignature app\TypingSound.App.exe  # → Status: Valid
  ```
  `signtool /tw` makes a missing timestamp a non-zero exit;
  `Get-AuthenticodeSignature.TimeStamperCertificate` is not used (it is null under
  `-FilePath` on the runner).

## Related

- [ADR-0002 — Code signing provider](adr/0002-code-signing-provider.md)
- [ADR-0003 — CI signing pipeline](adr/0003-ci-signing-pipeline.md)
- [SUPPLY_CHAIN.md](SUPPLY_CHAIN.md)
- Wiring itself: `.github/workflows/release.yml`
