# TypingSound へのコントリビュート

興味を持っていただきありがとうございます。このプロジェクトを速く・保守しやすく
保つために、いくつかの決まりがあります。参加にあたっては
[行動規範](.github/CODE_OF_CONDUCT.md) を守ってください。

アーキテクチャの方針や意図的な非目標については [README](README.md) を、リリースの
仕組みについては [docs/RELEASING.md](docs/RELEASING.md) を先に読んでください。

## セットアップ

ツールチェインは [mise](https://mise.jdx.dev/) で固定し、タスクは
[just](https://github.com/casey/just) 経由で実行します（バージョンは `mise.toml`）。

```
mise install     # dotnet SDK / just などを mise.toml のピンに従って導入
just setup       # git フック(lefthook)を導入
```

ツールチェインをその場でインストールせず、必ず `mise.toml` に宣言して
`mise install` してください。.NET SDK は mise 管理のため素の PATH からは
見えません。ビルドは `just` 経由（内部で `mise exec -- dotnet`）で呼びます。

## 開発ループ

```
just build          # App を Debug ビルド（x64）
just build-all      # ソリューション全体（テスト/ランチャー含む）をビルド
just test           # Core のユニットテスト
just run            # ビルドして起動
```

品質ゲートは厳格です（`TreatWarningsAsErrors`、アナライザ最大、StyleCop /
Roslynator）。警告は抑制ではなく根本対処を原則とします。push 前に
`just build-all` と `just test` が緑であることを確認してください。

## コミット & PR の規約

[Conventional Commits](https://www.conventionalcommits.org/)（`feat:`, `fix:`,
`docs:`, `refactor:`, `test:`, `chore:`, `ci:` …）を使い、squash-merge するので
PR タイトルがそのままコミットになります。これは **強制** です。ローカルの
lefthook `commit-msg` フック（`committed`）が各メッセージを、CI ゲート
（`amannn/action-semantic-pull-request`）が PR タイトルを検査します。この形式は
飾りではなく、自動バージョニングを駆動します。

リリースは手作業では切りません。
[release-please](https://github.com/googleapis/release-please) が `main` の
Conventional Commits を読み、バージョン（`Directory.Build.props` の
`<Version>`）と `CHANGELOG.md` を更新する「Release PR」を常に開いておきます。
`feat:` → minor、`fix:`/`perf:` → patch、`!` / `BREAKING CHANGE:` → major。
**バージョン番号を人が選んだり手で書き換えたりすることはありません。**
詳細は [docs/RELEASING.md](docs/RELEASING.md) を参照してください。

## スコープ

TypingSound は「打鍵ごとに音を鳴らす」小さなポータブル常駐アプリです。キーの
内容の記録・送信、ネットワーク機能、キーリマップ/マクロ、クロスプラットフォーム
対応などは意図的な非目標です。新機能を提案する前に、feature-request テンプレート
の out-of-scope リストを確認してください。

## ライセンス

コントリビュートすることで、あなたの貢献が [MIT License](LICENSE) の下で
ライセンスされることに同意したものとみなします。
