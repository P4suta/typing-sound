# Releasing

Operational runbook for cutting a release and for the one-time GitHub setup the
automation needs. Design rationale (why release-please, channels, the gates):
[ADR-0001](adr/0001-automated-versioning-with-release-please.md) and, for the signing
pipeline, [ADR-0003](adr/0003-ci-signing-pipeline.md).

## How a release happens (once activated)

1. Conventional Commits land on `main` (squash-merged PRs; the PR title is the commit).
2. [`release-please`](../.github/workflows/release-please.yml) keeps a **Release PR**
   open that bumps `<Version>` in `Directory.Build.props` (the line annotated
   `<!-- x-release-please-version -->`) and updates [`CHANGELOG.md`](../CHANGELOG.md).
   `feat:` → minor, `fix:`/`perf:` → patch, `!` / `BREAKING CHANGE:` → major.
3. **Add the `release: approved` label** to the Release PR (the `release-gate` CI job
   fails the PR until it's present).
4. **Merge the Release PR.** release-please creates the GitHub Release as a draft,
   materializes the `vX.Y.Z` tag at the release commit, then dispatches
   [`release.yml`](../.github/workflows/release.yml).
5. `release.yml` runs three jobs: **build → sign → publish**. The `sign` and
   `publish` jobs pause on the `release` environment for approval; publish attaches
   the signed zip + `SHA256SUMS.txt` + SBOMs + attestations, then publishes.

A real, immutable release requires several deliberate steps (label, merge, two
environment approvals), and `release.yml` starts only via that dispatch, never a tag
push. Automated tooling (incl. the AI assistant) will not merge the Release PR, push
a `v*` tag, approve the `release` environment, or run `release.yml` with
`publish=true` without an explicit, version-named instruction.

## Activation (one-time GitHub setup)

release-please ships **dormant**: until the App and signing secrets are set, every
workflow runs **green but no-ops** (nothing is tagged, signed, or published). Wiring
them lights the pipeline up — no code change, only GitHub configuration.

### (a) GitHub App for release-please

A tag pushed by the default `GITHUB_TOKEN` does **not** trigger `release.yml`
(GitHub's workflow-recursion guard), so `release-please.yml` mints a short-lived
installation token at runtime via `actions/create-github-app-token`.

1. **Create a GitHub App** (personal or org). Repository permissions: **Contents:
   Read & write** and **Pull requests: Read & write**. No webhook needed.
2. **Install** it on the `P4suta/typing-sound` repo.
3. Generate a **private key** (`.pem`) and note the App's **Client ID**.
4. Settings → Environments → **New environment** → name it **`release-please`**.
   - **Deployment branches and tags** → **Selected branches** → add **`main`** only.
   - **Do NOT add required reviewers** (release-please must run unattended; the human
     gate is merging the Release PR).
5. In that environment's **Environment secrets**, add:
   - `RELEASE_PLEASE_CLIENT_ID` = the App's Client ID
   - `RELEASE_PLEASE_PRIVATE_KEY` = the full `.pem` contents (multi-line is fine)

A repository secret is readable on *any* branch, so the App key is scoped to an
environment whose branch policy is `main`-only.

### (b) `release` environment for signing + publishing

1. Settings → Environments → **New environment** → name it **`release`**.
2. **Required reviewers**: add **yourself** (every `sign` / `publish` run pauses for a
   deliberate approval). **Deployment branches and tags → Selected**: allow `main`.
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

Branch protection is version-controlled as rulesets under `.github/rulesets/`. Apply
them with `gh` (needs admin) — commands and verification are in
[`.github/rulesets/README.md`](../.github/rulesets/README.md).

### (d) Enable private reporting + Discussions

- Settings → Code security → **Private vulnerability reporting** → Enable (backs the
  [SECURITY.md](../.github/SECURITY.md) advisory link).
- Settings → General → Features → **Discussions** → Enable (backs the issue-template
  "Question or idea" link).
