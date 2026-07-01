# Repository rulesets (version-controlled branch protection)

`main`'s branch protection is expressed as **rulesets** whose JSON definitions are
the source of truth here (not classic branch protection / UI edits). GitHub does not
auto-apply them from the tree; apply them once with `gh` (needs admin).

| File | Target | Enforces |
|---|---|---|
| `protect-default-branch.json` | `main` | PR required, `ci-required` + `analyze` (CodeQL) status checks (strict), linear history, conversation resolution, no force-push/deletion, no admin bypass, 0 required approvals (solo self-merge) |
| `require-signed-commits.json` | all branches but `gh-pages` | signed commits (release-please commits via the GitHub App / Contents API, which are GitHub-signed) |

## Apply

```sh
# New ruleset → POST; updating an existing one → PUT .../rulesets/<id>
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/protect-default-branch.json
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/require-signed-commits.json

# Update: list to find the ID, then PUT
gh api repos/P4suta/typing-sound/rulesets
gh api --method PUT repos/P4suta/typing-sound/rulesets/<ID> \
  --input .github/rulesets/protect-default-branch.json
```

## Verify

```sh
gh api repos/P4suta/typing-sound/rules/branches/main --jq '[.[].type] | unique'
# → deletion, non_fast_forward, pull_request, required_linear_history,
#   required_signatures, required_status_checks
```

After any UI/`gh` change, re-export the ruleset back into this directory so the tree
stays canonical. If a CI job name changes, keep `required_status_checks` contexts
(`ci-required`, `analyze`) in sync.
