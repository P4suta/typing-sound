# Supply Chain and Provenance

The mechanisms that let users machine-verify that a distributable was "built from
this commit of this repository, by an untampered CI." For code signing
(Authenticode) see [SIGNING.md](SIGNING.md). This document covers **build
provenance (SLSA), SBOM, and dependency controls**.

## For users: verify a download

Both **stable releases** (`release.yml`) and **nightlies** (`nightly.yml`) issue
GitHub-native keyless attestation — build provenance plus an SBOM attestation.
There is **no private key**; they sign to Sigstore (Fulcio/Rekor) with the
workflow's OIDC token. The only supply-chain difference is the Authenticode **code
signature**, which is stable-only ([ADR-0004](adr/0004-nightly-supply-chain-parity.md)).
All you need to verify is `gh`:

```
# Verify build provenance (which commit / workflow / runner built it)
gh attestation verify typing-sound-vX.Y.Z-win-x64.zip --repo P4suta/typing-sound

# Verify that the SBOM is bound to the same zip (CycloneDX predicate)
gh attestation verify typing-sound-vX.Y.Z-win-x64.zip --repo P4suta/typing-sound \
  --predicate-type https://cyclonedx.org/bom
```

Successful verification means "the artifact's digest matches an attestation issued
from `P4suta/typing-sound`'s `release.yml`." A release attaches:

| Asset | Contents |
|---|---|
| `typing-sound-vX.Y.Z-win-x64.zip` | The app (Authenticode-signed launcher + app host) |
| `SHA256SUMS.txt` | SHA-256 of the zip |
| `app.cdx.json` | CycloneDX 1.6 SBOM of the C# app (the resolved NuGet graph) |

**Nightlies** (a 14-day Actions artifact, not a Release) carry the same SBOM and the
same attestations ([ADR-0004](adr/0004-nightly-supply-chain-parity.md)) — they are
just unsigned. The attestations persist in the repo's **Attestations** tab even
after the artifact expires.

## Dependency and build controls

| Aspect | Mechanism |
|---|---|
| C# dependency lock | `packages.lock.json` (committed). CI treats stale as failure via `-p:RestoreLockedMode=true` |
| NuGet audit | `<NuGetAudit>` on + `dotnet list package --vulnerable` — the SDK fails the build on a known-vulnerable NuGet package |
| Vulnerabilities (shipped releases) | `osv-scanner` consumes the release SBOM: a **release gate** in `release.yml` (blocks publishing a build with a known-vulnerable dep in the resolved NuGet closure) and a **weekly re-scan** of the latest release's SBOM (`sbom-monitor.yml`) that catches advisories disclosed *after* shipping. Accepted/unfixable advisories: `osv-scanner.toml`. See [ADR-0005](adr/0005-sbom-consumed-by-osv-scanner.md) |
| Static analysis | CodeQL (`analyze`), plus the in-build analyzers (warnings-as-errors) |
| Auto-update | Dependabot (nuget + github-actions, weekly) |
| Action pinning | Third-party actions are pinned to a 40-char commit SHA (with `# vX.Y.Z` alongside); `actionlint` validates workflows |
| Posture monitoring | OpenSSF Scorecard (weekly, SARIF to the Security tab). See [SCORECARD.md](SCORECARD.md) |
| Reproducible build | `ContinuousIntegrationBuild=true` in CI (source-path normalization; `Deterministic` is the SDK default) |

## SBOMs are consumed, not just attached (ADR-0005)

The SBOM isn't a write-only release artifact — `osv-scanner` (OSV.dev) reads it at
two points:

1. **Build gate** — `release.yml`'s `build` job scans `app.cdx.json` right after
   generating it, before sign/publish, and `nightly.yml` scans before upload. A
   known-vulnerable dependency in the resolved shipped closure fails the build. The
   SBOM is the only view of the resolved NuGet graph.
2. **Shipped-release re-scan** — `sbom-monitor.yml` runs weekly, downloads the
   **latest release's** attested SBOM, and re-scans it against the current OSV DB.
   This is the only check covering *what users already downloaded*: a CVE disclosed
   after a release is invisible to source-tree scanners (which only see HEAD).

When the weekly re-scan finds something it opens (or updates) a single issue
labelled **`sbom-vuln`**; once the affected release is clean again the issue is
auto-closed. With no published release yet the job is a clean no-op and activates on
the first release. Accepted/unfixable advisories go in **`osv-scanner.toml`** at the
repo root (honoured by both the gate and the monitor).

## Notes

- **SBOM tools are CI/release-only** — the CycloneDX generator is
  `dotnet tool install --global CycloneDX` and `osv-scanner` is fetched in CI; they
  are **not** in the `mise.toml` dev loop. The project standardizes on **CycloneDX
  1.6**.
- For lock file updates, Dependabot's nuget PR regenerates `packages.lock.json`.
  After adding a package version locally, run `dotnet restore` → commit.
