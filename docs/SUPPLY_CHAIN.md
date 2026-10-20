# Supply Chain and Provenance

Stable releases include Authenticode signatures, build provenance, and a CycloneDX 1.6 SBOM.
See [SIGNING.md](SIGNING.md) for signing and [the verification decision](adr/pull-request-and-release-verification.md) for CI scope.

## Verify a download

Use GitHub CLI to verify the archive and its SBOM attestation:

```sh
gh attestation verify TypingSound-vX.Y.Z-win-x64.zip --repo P4suta/typing-sound
gh attestation verify TypingSound-vX.Y.Z-win-x64.zip --repo P4suta/typing-sound \
  --predicate-type https://cyclonedx.org/bom
```

Verification binds the archive digest to the repository's release workflow.
Each release contains the archive, `SHA256SUMS.txt`, and `app.cdx.json`.

## Checks

| Boundary | Check |
| --- | --- |
| Pull request | Formatting, strict analyzers, solution build, tests, coverage, and CodeQL share the selected CI jobs. |
| Restored dependencies | NuGet audits direct and transitive dependencies; known vulnerabilities fail the build. |
| Release distribution | CycloneDX records the resolved app graph, and osv-scanner checks it before signing. |
| Published binary | A weekly Linux workflow scans the latest release SBOM and reports findings in a `sbom-vuln` issue. |
| Publication | Protected environments gate signing and publication; the release workflow verifies signatures and issues provenance and SBOM attestations. |
| Updates | Renovate proposes NuGet, action, and tool updates; actions use commit pins. |

The local tool manifest pins CycloneDX.
The release scan reads accepted advisories from `osv-scanner.toml`.
See [ADR-0005](adr/0005-sbom-consumed-by-osv-scanner.md) for the shipped-release scan.
