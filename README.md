# TypingSound

[![CI](https://github.com/P4suta/typing-sound/actions/workflows/ci.yml/badge.svg)](https://github.com/P4suta/typing-sound/actions/workflows/ci.yml)
[![CodeQL](https://github.com/P4suta/typing-sound/actions/workflows/codeql.yml/badge.svg)](https://github.com/P4suta/typing-sound/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/P4suta/typing-sound/badge)](https://scorecard.dev/viewer/?uri=github.com/P4suta/typing-sound)
[![Release](https://img.shields.io/github/v/release/P4suta/typing-sound?sort=semver)](https://github.com/P4suta/typing-sound/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> キーを打つと心地よい音が鳴る、Windows 常駐型のタイピング・サウンドアプリ。
> タイプライターのように打鍵ごとに音を鳴らし、Enter で行頭復帰のベルを鳴らします。

タイプ内容（どのキーを押したか）は一切扱いません。扱うのは「キーが押された」という事実と、その大まかな分類（通常キー / 復帰キーなど）だけです。

## 特長

- **タイプライターモード** — 打鍵ごとに打鍵音、Enter で復帰ベル。
- **ポータブル** — インストール不要。ランタイム同梱の self-contained ビルドで、フォルダごとコピーすれば動きます（管理者権限・開発者モード不要）。
- **常駐 & 軽量** — システムトレイに常駐。不要な重量級アセンブリ（ML / WinForms 等）はビルド時に除去済み。
- **既定オーディオデバイス追従** — 出力デバイスの切り替えに追従します。
- **プライバシー** — キーの内容は記録も送信もしません。ネットワーク通信はありません。

## ダウンロード

[**最新リリース**](https://github.com/P4suta/typing-sound/releases/latest) から zip を取得し、展開して `TypingSound.exe` をダブルクリックするだけです。フォルダごと移動・コピーしても動作します。

ランディングページ: https://P4suta.github.io/typing-sound/

## アーキテクチャ

このプロジェクトの最優先目標は **抽象化の美しさ** です。プラットフォーム非依存のドメインロジック（`TypingSound.Core`）と、Windows 固有の実装（`TypingSound.Platform` / `TypingSound.App`）を厳密に分離しています。

```
TypingSound.Core        ドメイン層。OS にもオーディオ実装にも依存しない純粋ロジック。
├─ Abstractions/        IAudioEngine / ISoundClip / ITimerFactory / IRandomSource ... 境界インターフェース
├─ Triggers/            軸A「いつ鳴らすか」  EveryKeyTrigger / DebounceTrigger
├─ Selectors/           軸B「どのクリップを鳴らすか」 Fixed / Random / ShuffleQueue / Typewriter
├─ Playback/            軸C「どう鳴らすか」  Monophonic / Polyphonic
├─ Modes/               3 軸を束ねる SoundModePipeline と各モード
└─ TypingSoundEngine    司令塔。キー押下を起動中モードへ流し、モード切替を担う

TypingSound.Platform    Core の境界インターフェースを Windows 実装で満たすアダプタ層。
├─ Audio/               NAudio ベースの IAudioEngine / ISoundBank 実装
└─ Interop/             低レベルキーボードフック（P/Invoke）

TypingSound.App         WinUI 3 シェル。トレイ常駐 UI、DI 配線、診断/ロギング。
TypingSound.Launcher    ポータブル配布の入口 exe（self-contained ランチャー）。
TypingSound.Core.Tests  Core 層の xUnit ユニットテスト（プロパティテスト含む）。
```

### サウンドの組み立て（3 軸 × パイプライン）

音の鳴り方を直交する 3 つの軸に分解し、`SoundModePipeline` が配線します。新しい鳴らし方は、軸の実装を差し替えるだけで合成できます。

| 軸 | 役割 | 実装例 |
| --- | --- | --- |
| **Trigger** | いつ鳴らすか | `EveryKeyTrigger`, `DebounceTrigger` |
| **Selector** | どのクリップを鳴らすか | `FixedSelector`, `RandomSelector`, `ShuffleQueueSelector`, `TypewriterSelector` |
| **Playback** | どう鳴らすか（声部管理） | `MonophonicPolicy`, `PolyphonicPolicy` |

```
キー押下 ─▶ Trigger ──(発火)──▶ Selector ──(クリップ)──▶ Playback ─▶ 発音
```

`Core` は実時間タイマーも乱数もオーディオ出力も抽象越しにしか触らないため、テストではすべてフェイクに差し替えて決定的に検証できます。

## ビルド & 実行

### 前提

- Windows 11（WinUI 3 / Windows App SDK）
- [mise](https://mise.jdx.dev/) — .NET SDK と [just](https://github.com/casey/just) を宣言的に管理します（バージョンは `mise.toml` 固定）。

```sh
mise install        # mise.toml に従い dotnet SDK と just などを導入
just setup          # git フック(lefthook: Conventional Commits 検査)を導入
```

### 主なタスク（`just`）

```sh
just build          # App を Debug ビルド（x64）
just build-all      # ソリューション全体（テスト/ランチャー含む）をビルド
just test           # Core のユニットテスト
just test-cov       # カバレッジ付きテスト（しきい値ゲート）
just verify         # 整形チェック + 全ビルド + カバレッジ付きテスト（push 前の一括検証）
just run            # ビルドして起動
just publish        # App を Release publish（ポータブル一式 / 既定 x64）
just dist           # 配布レイアウト dist/TypingSound/ を作成（入口 exe + app/）
just package v0.1.0 # dist を zip + SHA256SUMS 化（build/package/）
just clean          # ビルド成果物と dist を削除
```

> .NET SDK は mise 管理のため素の PATH からは見えません。`just` 経由（内部で `mise exec -- dotnet`）で呼んでください。
> WinUI 本体は x64 固定でビルドします（詳細は `justfile` 冒頭のコメント参照）。

## 開発方針

- **超厳格な品質ゲート** — 警告はエラー扱い（`TreatWarningsAsErrors`）、アナライザ最大（`latest-all`）、StyleCop / Roslynator、`EnforceCodeStyleInBuild`。抑制ではなく根本対処を原則とします（`Directory.Build.props` / `.editorconfig`）。
- **観測可能性** — Serilog によるログとグローバル例外ハンドラを備え、境界で握りつぶさず記録します。

## リリース & 貢献

- バージョンは [release-please](https://github.com/googleapis/release-please) が [Conventional Commits](https://www.conventionalcommits.org/) から自動決定します。**手でバージョンを編集しません**（`feat:` → minor / `fix:`・`perf:` → patch / `!` → major）。変更履歴は [`CHANGELOG.md`](CHANGELOG.md)（自動生成）。
- 配布物は SSL.com eSigner による Authenticode 署名 + CycloneDX SBOM + Sigstore の build-provenance / SBOM attestation 付き。検証: `gh attestation verify <zip> --repo P4suta/typing-sound`。
- 詳細は [`CONTRIBUTING.md`](CONTRIBUTING.md) / [`docs/RELEASING.md`](docs/RELEASING.md) / [`docs/SIGNING.md`](docs/SIGNING.md) / [`docs/SUPPLY_CHAIN.md`](docs/SUPPLY_CHAIN.md)。

## クレジット

- 打鍵音 / ベル音（`TypingSound.App/Assets/Sounds/*.wav`）: [Mixkit](https://mixkit.co)（Mixkit Free License）
- アプリアイコン: 本プロジェクトのオリジナル

## ライセンス

ソースコードは [MIT License](LICENSE) です。同梱のサウンドアセットは Mixkit のライセンス条項に従います（詳細は `LICENSE` 参照）。
