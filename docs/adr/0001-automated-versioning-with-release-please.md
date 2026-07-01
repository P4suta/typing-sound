# ADR-0001: Automated versioning (release-please) + dev/nightly/stable build channels

Date: 2026-07-01 / Status: Accepted

## Context

Cutting a release by hand-editing a version number is error-prone and human-driven.
And a bare `X.Y.Z` in the assembly gives a build no *identity*: a contributor's
local build, a nightly, and an official release all report the same string and are
indistinguishable. Both are the shape to remove pre-1.0, while the app is still free
to change. The decision criterion is **convenience + how industry-standard the
workflow is**, not daily build-loop cost.

## Decision

1. **release-please owns the version, CHANGELOG, and a draft release.** Conventional
   Commits on `main` drive a bot (`googleapis/release-please-action`, SHA-pinned)
   that keeps a "Release PR" open; merging it bumps the version, updates
   `CHANGELOG.md`, and creates the GitHub Release as a **draft** (config
   `"draft": true`). The maintainer never hand-picks or hand-edits a number — they
   merge a PR, and the Release PR diff *is* the preview. A draft release creates no
   git tag by itself, so the config sets `"force-tag-creation": true` to materialize
   the `vX.Y.Z` tag at the release commit; `release-please.yml` then dispatches
   `release.yml`, which builds → signs → attaches assets → publishes (assets before
   publish, the order [immutable releases](https://docs.github.com/code-security/concepts/supply-chain-security/immutable-releases)
   demand).

2. **The version stays declared in a file; the bot edits it.** The single version
   lives in `Directory.Build.props` `<Version>`, on the line annotated
   `<!-- x-release-please-version -->`, and a `generic` extra-file updater keyed on
   that annotation bumps it. `feat:` → minor, `fix:`/`perf:` → patch,
   `!` / `BREAKING CHANGE:` → major. The stored number keeps builds reproducible
   (tarball / `.git`-less builds); the thing removed is the *human driver*, not the
   stored number.

3. **Three channels, stamped at build time.** The base `X.Y.Z` is the
   release-please-managed number; the channel suffix is layered onto the assembly
   `InformationalVersion` at build time by `Directory.Build.targets`:
   - **dev** (local `just build`) → `X.Y.Z-dev+g<sha>`
   - **nightly** → `X.Y.Z-nightly.<date>+g<sha>`
   - **stable** → clean `X.Y.Z`
   So a hand-built binary is never mistaken for an official one.

4. **Conventional Commits are enforced.** Locally via a lefthook `commit-msg` hook
   (`committed`, mise-pinned); on PRs via `amannn/action-semantic-pull-request`
   (squash-merge → the PR title becomes the commit, so the title is what
   release-please reads).

5. **Dormant until a GitHub App is provisioned.** A tag pushed by the default
   `GITHUB_TOKEN` does not trigger `release.yml` (GitHub's recursion guard), so
   release-please runs as a **GitHub App**: `release-please.yml` mints a short-lived,
   repo-scoped installation token (`actions/create-github-app-token`), authenticating
   by **Client ID** from the `RELEASE_PLEASE_CLIENT_ID` + `RELEASE_PLEASE_PRIVATE_KEY`
   secrets. Those live in a dedicated **`release-please` environment** with a
   `main`-only deployment-branch policy and, deliberately, **no required reviewers**
   (the human gate is merging the Release PR; signing's gate is the separate
   `release` environment). With the secrets unset the job runs green and no-ops.

6. **A real release requires multiple deliberate actions.** Opening the Release PR
   does nothing. Cutting a release then takes, in order: adding the
   `release: approved` label (a CI `release-gate` job fails the Release PR until it's
   present), merging the Release PR, approving the `sign` job in the `release`
   environment, and approving the `publish` job. `release.yml` is dispatch-triggered
   (never tag-triggered), so a stray `vX.Y.Z` tag starts nothing.

## Rationale

- **release-please over an in-tree script**: the Release-PR bot is the lower-friction,
  more-recommended 2024+ workflow, and needs no local release CLI.
- **Declared-and-bot-edited over git-derived (Nerdbank.GitVersioning)**: a
  height / `git describe` version is not Conventional-Commits-*semantic* (it can't
  turn `feat:` into a minor bump), breaks on `.git`-less source builds, and would be
  a second versioning owner. The stored number costs almost nothing.
- **Channel suffix at build time, base in the file**: `Directory.Build.targets` is
  the right home for the *derived* part (channel + sha); the *declared* base never
  needs git at build time.

## Rejected alternatives

- **Nerdbank.GitVersioning (git-derived, no stored version)** — the purest "no
  version to manage" model, but non-composable with Conventional-Commits-semantic
  bumps and `.git`-dependent. Rejected.
- **Manual version editing** — the human-driven shackle this ADR removes. Rejected.
- **Nightly as a rolling GitHub Release** — incompatible with Immutable Releases
  (can't overwrite the asset). Nightly is a 14-day Actions artifact instead. Rejected.

## Consequences

- The release ritual becomes "write Conventional Commits, merge the Release PR."
- Every contributor commit must be a Conventional Commit (local hook + PR-title gate).
- A `release-please.yml` and a `nightly.yml` workflow appear; both are dormant
  without the App secrets.

## Re-examination triggers

- **NuGet publishing begins** → re-evaluate a real package-publish step.
- **The version-bearing MSBuild surface grows** (multiple version props) →
  reconsider Nerdbank.GitVersioning for the .NET side specifically.
