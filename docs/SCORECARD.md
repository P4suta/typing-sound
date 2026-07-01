# OpenSSF Scorecard

This repo publishes an [OpenSSF Scorecard](https://scorecard.dev/) report
(`.github/workflows/scorecard.yml`, weekly + on push to `main`, SARIF to the
Security tab, README badge). Scorecard grades supply-chain hygiene across ~18
checks. This page records the branch-protection posture and how to apply it, so the
score is understood rather than chased blindly.

## Branch protection = version-controlled rulesets

`main`'s protection is expressed as **rulesets** whose definitions are
version-controlled under `.github/rulesets/` as the source of truth (not classic
branch protection). Two rulesets, both committed:

| File | Target | Enforces |
|---|---|---|
| `.github/rulesets/protect-default-branch.json` | `main` | PR required, `ci-required` + `analyze` (CodeQL) status checks (strict), linear history, conversation resolution, no force-push/deletion, no admin bypass |
| `.github/rulesets/require-signed-commits.json` | all branches but `gh-pages` | signed commits |

GitHub does **not** auto-apply repo-level rulesets from the tree. Apply with `gh`
(needs admin; run it yourself — no admin tokens are stored):

```sh
# New ruleset → POST; updating an existing one → PUT .../rulesets/<id>
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/protect-default-branch.json
```

Verify the effective rules on `main`:

```sh
gh api repos/P4suta/typing-sound/rules/branches/main --jq '[.[].type] | unique'
# → deletion, non_fast_forward, pull_request, required_linear_history,
#   required_signatures, required_status_checks
```

After any UI/`gh` change, re-export the ruleset back into `.github/rulesets/` so the
tree stays canonical.

> Confirm the status-check context names (`ci-required`, `analyze`) match the jobs
> in `ci.yml` / `codeql.yml` as GitHub reports them — check **Settings → Rules** if
> a context never resolves.

## The solo-maintainer tension

Requiring ≥1 approval conflicts with solo self-merge (you can't approve your own PR
→ every merge blocks). The committed config keeps **self-merge**
(`required_approving_review_count: 0`), so a solo project realistically tops out in
the mid range on Scorecard's Branch-Protection tiers. Requiring a review (a second
reviewer, or you stop self-merging) is the only path to the top tier — edit the
`pull_request` rule and re-apply with `--method PUT` if that changes.

## Deliberately left

A few checks are not movable by repo changes on a solo, pre-1.0 project:
Code-Review and Contributors (need multiple maintainers), Maintained (resolves with
age + activity), and Signed-Releases / Packaging (inconclusive until the first
release is cut — the `release.yml` infra is ready). Cutting the first release is a
product decision, out of scope for a hardening pass.

## Re-checking the score

Trigger a fresh scan (`scorecard.yml` runs weekly, on push to `main`, and via
**Actions → scorecard → Run workflow**), then read the badge or
<https://scorecard.dev/viewer/?uri=github.com/P4suta/typing-sound>.
