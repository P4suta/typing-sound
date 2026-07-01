# ADR-0005: The SBOM is consumed by osv-scanner — release gate + shipped-release re-scan

Date: 2026-07-01 / Status: Accepted

## Context

`release.yml` generates and attests a CycloneDX SBOM of the C# app (via the
CycloneDX dotnet tool) and attaches it to the release. But an SBOM that nothing
*consumes* is a write-only artifact whose only value is provenance/attestation and an
OpenSSF Scorecard "Secured release" tick — "you can trace it, so what?" has no
answer. The `<NuGetAudit>` build check and Dependabot both scan HEAD, and neither
answers "is the *shipped* `vX.Y.Z` affected by a CVE disclosed after we shipped it?"

## Decision

Give the SBOM a job by feeding it to **`osv-scanner`** (OSV.dev) at two points.

1. **Release gate (`release.yml`, `build` job).** Right after the SBOM is generated —
   before sign/publish — run `osv-scanner` over `app.cdx.json` with
   `--config osv-scanner.toml`. A finding (exit 1) fails the build, so a release with
   a known-vulnerable dependency in the **resolved shipped closure** never publishes.
   The SBOM is the only view of the resolved NuGet graph (`<NuGetAudit>` catches most
   of it at build, but the SBOM scan is the closure-level gate at the release
   boundary and is shared with the monitor below).

2. **Shipped-release re-scan (`sbom-monitor.yml`, weekly + `workflow_dispatch`).**
   Download the **latest** release's attested SBOM and re-scan it against the
   *current* OSV DB. This is the only check that covers *what users already
   downloaded*: source-tree scanners see only HEAD, so a CVE disclosed after a
   release is invisible for the shipped binary. The frozen, attested SBOM is exactly
   the manifest needed to answer "is the shipped `vX.Y.Z` affected?".

3. **Report findings as a single idempotent issue, not SARIF.** The monitor
   opens/updates one issue labelled `sbom-vuln` (auto-closed when the release is clean
   again). "A shipped release is vulnerable → cut a patch release" is an
   assignable/closable *task*, which an issue models better than a code-scanning
   alert (which is about current code).

4. **`osv-scanner.toml` at the repo root is the single ignore list.** Accepted /
   unfixable advisories are recorded there, honoured by both the gate and the
   monitor, so an upstream advisory with no fix can't permanently block releases or
   spam the issue. Every entry must justify itself.

5. **Tooling is CI/release-only.** `osv-scanner` (SHA-pinned, version-pinned) and the
   CycloneDX dotnet tool are not added to `mise.toml` — the dev loop stays untouched.

## Rationale

- **osv-scanner over grype / trivy**: it consumes CycloneDX natively, covers the
  NuGet graph against one DB (OSV.dev) in a single pass, is Google-maintained,
  SHA-pinnable, and uses simple exit codes. grype/trivy are heavier and
  container-oriented.
- **Consume the SBOM rather than re-scan the lockfile**: scanning the SBOM is what
  makes the artifact earn its keep and is the only way to reach the resolved
  runtime/NuGet graph and the *shipped* (not HEAD) state.
- **Gate fail-fast in `build`**: failing before the approval-gated `sign` job wastes
  no reviewer time and never signs a vulnerable bundle.
- **Dormant-first**: with no release yet, the monitor no-ops cleanly (notice + exit 0)
  and activates automatically on the first release.

## Rejected alternatives

- **Leave the SBOM as provenance-only** — leaves the "so what?" unanswered and the
  shipped-release-monitoring gap open. Rejected.
- **SARIF → Code Scanning** for the monitor — models "current code finding" rather
  than "shipped release needs a patch"; an issue fits better. Rejected for the
  monitor.
- **Scan a matrix of all supported releases** — correct at scale, but this is a solo,
  pre-1.0 project with one release line; "latest" is the whole supported surface.
  Deferred.

## Consequences

- A release can now be **blocked** by an OSV finding; the escape hatch is a justified
  `osv-scanner.toml` entry, not disabling the gate.
- One new weekly workflow (cheap ubuntu) and one extra `build`-job step on releases.
  The monitor needs `issues: write`; the gate adds no new permissions.
- The `sbom-vuln` issue is the maintainer's signal that a shipped release needs a
  patch release.

## Re-examination triggers

- **Multiple supported release lines** (post-1.0) → extend the monitor to a matrix of
  supported tags instead of "latest".
- **osv-scanner false positives grow** → revisit gate strictness (warn vs fail).
