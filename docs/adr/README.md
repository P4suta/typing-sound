# ADR index

Architecture Decision Records for TypingSound. Each records a decision, its
rationale, the rejected alternatives, and re-examination triggers.

- [0001](0001-automated-versioning-with-release-please.md) — Automated versioning via release-please (Conventional Commits → Release PR → `vX.Y.Z` tag bumps `Directory.Build.props` `<Version>` + CHANGELOG) + dev/nightly/stable build-channel stamping via `Directory.Build.targets`; Nerdbank.GitVersioning rejected
- [0002](0002-code-signing-provider.md) — Code signing = SSL.com eSigner + personal Individual Validation cert (`CN=Yasunobu Sakashita`); Azure Trusted Signing (individual onboarding paused) and EV (no longer grants immediate SmartScreen trust) rejected
- [0003](0003-ci-signing-pipeline.md) — CI signing pipeline: official `SSLcom/esigner-codesign` Action (`batch_sign`) inside a `build`→`sign`→`publish` job split, signing secrets behind an approval-gated `release` environment, hardened verify shared between the sign self-check and the publish gate
- [0004](0004-nightly-supply-chain-parity.md) — Nightly artifacts carry the full supply chain minus signing: CycloneDX SBOM + osv-scanner gate + keyless build-provenance & SBOM attestations; Authenticode signing stays stable-only
- [0005](0005-sbom-consumed-by-osv-scanner.md) — The CycloneDX SBOM is consumed by osv-scanner: a release gate (blocks publishing a known-vulnerable resolved NuGet closure) + a weekly re-scan of the latest release's SBOM; `osv-scanner.toml` accepted-advisory escape hatch
