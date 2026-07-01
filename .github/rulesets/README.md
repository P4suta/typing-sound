# Repository rulesets (version-controlled branch protection)

これらの JSON は、このリポジトリの分岐保護の「唯一の真実」です。GitHub は自動適用しないので、
手動(または一度きり)で `gh api` を使って投入します。設定を UI で手作業せず、レビュー可能な差分として
版管理するための仕組みです。

- `protect-default-branch.json` — 既定ブランチ(main)を保護: PR 必須 / squash マージのみ / linear history /
  会話(レビュースレッド)解決必須 / force-push・削除禁止 / 必須ステータスチェック **`ci-required`** と
  **`analyze`**(CodeQL)を strict(最新コミットで再実行)で要求。ソロ運用のため必須承認数は 0。
- `require-signed-commits.json` — `gh-pages` を除く全ブランチで署名済みコミットを要求。release-please は
  GitHub App / Contents API 経由でコミットするため GitHub 署名が付き、この規則を満たす。

## 適用

```sh
# 新規作成(初回)
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/protect-default-branch.json
gh api --method POST repos/P4suta/typing-sound/rulesets \
  --input .github/rulesets/require-signed-commits.json

# 既存を更新するとき(ruleset ID を控えておく)
gh api repos/P4suta/typing-sound/rulesets            # 一覧して ID を確認
gh api --method PUT repos/P4suta/typing-sound/rulesets/<ID> \
  --input .github/rulesets/protect-default-branch.json
```

CI のジョブ名を変えたら、`required_status_checks` の `context`(`ci-required` / `analyze`)も揃えること。
