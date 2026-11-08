# ADR index

Architecture decision records describe TypingSound's choices, alternatives, and reconsideration conditions.
Historical record paths remain stable.

[Automated versioning](0001-automated-versioning-with-release-please.md) assigns versioning to release-please and defines build channels.
[Code signing provider](0002-code-signing-provider.md) selects SSL.com eSigner.
[CI signing pipeline](0003-ci-signing-pipeline.md) separates building, signing, and publication behind approval gates.
[Nightly supply chain parity](0004-nightly-supply-chain-parity.md) records unsigned artifacts and their supply chain checks.
[SBOM consumption](0005-sbom-consumed-by-osv-scanner.md) defines release and published dependency checks.
[Pull-request and release verification](pull-request-and-release-verification.md) replaces automatic nightly distributions with deliberate stable releases and shared verification.
[Release App credential synchronization](sync-release-app-credentials-to-the-protected-environment.md) proposes native synchronization into the protected environment while preserving signing OIDC.
