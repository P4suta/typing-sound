# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities **privately** via
[GitHub Security Advisories](https://github.com/P4suta/typing-sound/security/advisories/new).
**Do not open a public issue for a vulnerability.**

We aim to acknowledge a report within a few days and to ship a fix or mitigation
as quickly as the severity warrants.

## Supported versions

typing-sound is pre-1.0; only the latest release receives security fixes.

| Version | Supported |
| ------- | --------- |
| latest  | ✅        |
| older   | ❌        |

## Scope

typing-sound is a portable, offline WinUI 3 tray app that plays a sound on each
keystroke. It never records or transmits *which* keys are pressed, and it makes
no network connections. Examples of **in-scope** reports:

- A crafted sound or configuration file that, when loaded by the app, causes code
  execution, a crash exploitable beyond a benign failure, or a path-traversal /
  arbitrary-file write outside the app folder.
- A supply-chain compromise of the signed release (the distributed zip, its
  Authenticode signature, the SBOM, or the build-provenance attestations) that
  lets a tampered build pass verification. See
  [`docs/SUPPLY_CHAIN.md`](../docs/SUPPLY_CHAIN.md) and
  [`docs/SIGNING.md`](../docs/SIGNING.md).
- The low-level keyboard hook leaking keystroke *content* anywhere (to a log, a
  file, or the network) — this app is designed never to do so.

Out of scope:

- Anything that requires an attacker who already has code-execution or admin on
  the machine (the app runs with the user's own privileges; it has no service and
  no elevation).
- The bundled Microsoft .NET / Windows App SDK runtime DLLs — report those to
  Microsoft.
- SmartScreen "unknown publisher" prompts on an unsigned local build (expected;
  official releases are Authenticode-signed).
