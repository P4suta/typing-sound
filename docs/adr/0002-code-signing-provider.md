# ADR-0002: Code-signing provider selection (SSL.com eSigner / individual IV)

Date: 2026-07-01 / Status: Accepted (runbook: [docs/SIGNING.md](../SIGNING.md))

Certificate holder: `CN=Yasunobu Sakashita` (SSL.com individual IV, code-signing EKU).

## Context

An unsigned portable app triggers SmartScreen's "unknown publisher" prompt and
gives users no way to attribute the download to a real author. We want the
distributed binaries Authenticode-signed, signed **unattended in CI** (no hardware
token on the runner), and obtainable by a **Japanese individual** without corporate
registration.

## Decision

Authenticode signing is done with **SSL.com eSigner** (a cloud HSM signing service)
plus a **personal Individual Validation (IV)** certificate. The signing targets are
**only TypingSound's own two PE files** — the portable launcher `TypingSound.exe`
and the app host `app\TypingSound.App.exe`. The bundled .NET / Windows App SDK
runtime DLLs are Microsoft-signed and are not re-signed. Signing is **non-blocking**
when the secrets are unset (finish unsigned + `::warning::`), and lives as a CI YAML
step in `release.yml`, not in the dev tooling.

## Rationale

- **Azure Trusted Signing not adopted**: managed and easy to integrate, but as of
  2026 it **paused individual onboarding** — new tenants are limited to US/CA
  organizations with 3+ years of history, so a Japanese individual cannot apply.
  Eliminated by eligibility.
- **EV not adopted (IV adopted)**: since March 2024, EV **no longer grants instant
  SmartScreen trust** (Microsoft official). SmartScreen is reputation-based —
  reputation accrues from the signer cert + file hash via download history, and
  "first-time warning → cleared by track record" is the same for EV/IV. This app
  ships **no kernel driver**, so EV's remaining benefits do not apply. IV is the
  cheapest obtainable-under-a-personal-name option and the rational choice.
- **SSL.com eSigner adopted**: cloud HSM needs no hardware token on the runner;
  fully unattended CI signing via TOTP; an official GitHub Action
  (`SSLcom/esigner-codesign`) exists; supports personal IV; obtainable from Japan.
  - Certum personal (cheapest) requires a phone OTP per signature — a poor fit for
    unattended CI. SignPath Foundation (free for OSS) requires review and may put new
    projects on hold. Both are inferior on the "fully outsourced managed" requirement.

## Consequences

- Signing is limited to the dispatch-driven `release.yml` (`ci.yml` never signs — no
  distributing dev intermediates, conserve quota, fork PRs cannot read secrets).
- Publicly trusted code-signing certs are valid for at most ~460 days (CA/Browser
  Forum). Renewal procedure is in [docs/SIGNING.md](../SIGNING.md).

## Re-examination triggers

- **Azure Trusted Signing opens to individuals in Japan** → re-evaluate; `dotnet
  sign` + OIDC would then be reachable.
- **The app ships a kernel driver** → EV becomes mandatory.
- **A corporate EV procurement requirement arises** → reconsider Sole Proprietor /
  corporate EV.
