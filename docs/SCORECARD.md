# OpenSSF Scorecard

This repo publishes an [OpenSSF Scorecard](https://scorecard.dev/) report
(`.github/workflows/scorecard.yml`, weekly + on push to `main`, SARIF to the
Security tab, README badge). Scorecard grades supply-chain hygiene across ~18 checks.

## Branch protection

`main`'s protection is expressed as version-controlled **rulesets** under
`.github/rulesets/` (not classic branch protection). The ruleset files, what they
enforce, and the apply/verify `gh` commands live in
[`.github/rulesets/README.md`](../.github/rulesets/README.md).

Notes on the score, on a solo pre-1.0 project:

- The config keeps solo self-merge (`required_approving_review_count: 0`), which caps
  Scorecard's Branch-Protection tier in the mid range. Requiring a review is the only
  path to the top tier — change the `pull_request` rule and re-apply if that changes.
- Code-Review, Contributors, Maintained, and Signed-Releases / Packaging are not
  movable by repo changes yet (they need multiple maintainers, age/activity, or a
  first cut release; the `release.yml` infra is ready).

## Re-checking the score

Trigger a fresh scan (`scorecard.yml` runs weekly, on push to `main`, and via
**Actions → scorecard → Run workflow**), then read the badge or
<https://scorecard.dev/viewer/?uri=github.com/P4suta/typing-sound>.
