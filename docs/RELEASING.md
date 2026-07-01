# Releasing

TypingSound versions itself from Conventional Commits — there is no manual
version bump. This page covers how a release happens, how to **activate** the
automation, and the one-time GitHub setup it needs.
Design rationale: [ADR-0001](adr/0001-automated-versioning-with-release-please.md).

## How a release happens (once activated)

1. Conventional Commits land on `main` (squash-merged PRs; the PR title is the commit).
2. [`release-please`](../.github/workflows/release-please.yml) keeps a **Release PR**
   open that bumps the `<Version>` in `Directory.Build.props` (the line annotated
   `<!-- x-release-please-version -->`) and updates [`CHANGELOG.md`](../CHANGELOG.md).
   The version is derived from the commits: `feat:` → minor, `fix:`/`perf:` → patch,
   `!` / `BREAKING CHANGE:` → major.
3. **Add the `release: approved` label** to the Release PR. Until it's there the
   `release-gate` CI job fails the PR, so a release is never an accidental merge.
4. **Merge the Release PR.** release-please creates the GitHub Release as a **draft**
   (config `"draft": true`) and **materializes the `vX.Y.Z` git tag at the release
   commit** (`"force-tag-creation": true`), then dispatches
   [`release.yml`](../.github/workflows/release.yml). The tag is forced because a
   draft release otherwise has no git tag.
5. `release.yml` runs three jobs: **build → sign → publish**, each gated. The
   `sign` and `publish` jobs pause on the `release` environment for approval. The
   publish step attaches the signed zip + `SHA256SUMS.txt` + SBOMs + attestations
   to the draft (which already carries the tag from step 4) and **publishes it**.
   Assets land *before* publish, the order
   [immutable releases](https://docs.github.com/code-security/concepts/supply-chain-security/immutable-releases)
   require (a published immutable release can't gain assets afterward).

You never hand-pick or hand-edit a version. The Release PR diff *is* the preview.

## Release safety (defence in depth)

Cutting a real, immutable release is deliberately gated by several independent
steps, so an ambiguous instruction can't ship one by accident:

- **Label gate** — the Release PR can't merge until you add `release: approved`
  (the `release-gate` CI job). Non-release PRs pass automatically.
- **No tag-triggered cascade** — `release.yml` is started only by an explicit
  dispatch from `release-please.yml`, never by a tag push, so a stray or manual
  `vX.Y.Z` tag starts nothing.
- **Two environment approvals** — both the `sign` and `publish` jobs pause on the
  `release` environment (reviewer = the maintainer); the irreversible publish has
  its own approval.
- **Agent contract** — automated tooling (incl. the AI assistant) will not merge
  the Release PR, push a `v*` tag, approve the `release` environment, or run
  `release.yml` with `publish=true` without an explicit, version-named instruction.

## Build identity (channels)

The stable release stamps a clean `X.Y.Z` into the assembly `InformationalVersion`.
Local `just build` and nightly builds stamp a channel suffix (`-dev`, `-nightly`)
so a hand-built binary is never mistaken for an official release. The base `X.Y.Z`
is the release-please-managed number in `Directory.Build.props`; the channel suffix
is layered at build time via `Directory.Build.targets` (no Nerdbank.GitVersioning).
See [ADR-0001](adr/0001-automated-versioning-with-release-please.md).

## Activation (one-time GitHub setup)

release-please ships **dormant**: until the App and signing secrets are set, all
workflows run **green but no-op** (nothing is tagged, signed, or published). Wiring
them lights the pipeline up. There is no code change — only GitHub configuration.

### (a) GitHub App for release-please

A tag pushed by the default `GITHUB_TOKEN` does **not** trigger `release.yml`
(GitHub's workflow-recursion guard), so the tag must be pushed by a different
identity. `release-please.yml` mints a short-lived installation token at runtime
via `actions/create-github-app-token`.

1. **Create a GitHub App** (personal or org). Repository permissions: **Contents:
   Read & write** and **Pull requests: Read & write**. No webhook needed.
2. **Install** it on the `P4suta/typing-sound` repo.
3. Generate a **private key** (`.pem`) and note the App's **Client ID**.
4. Settings → Environments → **New environment** → name it **`release-please`**.
   - **Deployment branches and tags** → **Selected branches** → add **`main`** only.
   - **Do NOT add required reviewers** (release-please must run unattended).
5. In that environment's **Environment secrets**, add:
   - `RELEASE_PLEASE_CLIENT_ID` = the App's Client ID
   - `RELEASE_PLEASE_PRIVATE_KEY` = the full `.pem` contents (`-----BEGIN…` through
     `…END-----`; multi-line is fine)

A **repository** secret is readable by a workflow run on *any* branch, so we scope
the App key to an environment whose branch policy is `main`-only. release-please's
environment deliberately has **no required reviewers** — the human gate is merging
the Release PR; signing's approval gate is the separate `release` environment.

### (b) `release` environment for signing + publishing

1. Settings → Environments → **New environment** → name it **`release`**.
2. **Required reviewers**: add **yourself** (every `sign` / `publish` run pauses for
   a deliberate approval). **Deployment branches and tags → Selected**: allow `main`.
3. **Environment secrets** (all four required by SSL.com eSigner `batch_sign`):

   | Secret name | Value |
   |---|---|
   | `ES_USERNAME` | SSL.com username |
   | `ES_PASSWORD` | SSL.com password |
   | `CREDENTIAL_ID` | Credential ID of the signing certificate |
   | `ES_TOTP_SECRET` | TOTP secret for eSigner automated signing (Base32) |

   See [SIGNING.md](SIGNING.md) for obtaining these. A `publish=false` run is an
   unsigned signing smoke test.

### (c) Apply branch-protection rulesets

Branch protection is version-controlled as rulesets under `.github/rulesets/`.
GitHub does not auto-apply them from the tree — apply with `gh` (needs admin):

```sh
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/protect-default-branch.json
```

The required status checks are `ci-required` and `analyze` (CodeQL). See
[SCORECARD.md](SCORECARD.md).

### (d) Enable private reporting + Discussions

- Settings → Code security → **Private vulnerability reporting** → Enable (this
  backs the [SECURITY.md](../.github/SECURITY.md) advisory link).
- Settings → General → Features → **Discussions** → Enable (backs the issue-template
  "Question or idea" link).

> Until the App and signing secrets above are set, every workflow runs green and
> dormant: `release-please.yml` no-ops, and a signing run finishes **unsigned**
> with a `::warning::`. The scaffolding ships now; the secrets light it up later.
