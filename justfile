# TypingSound タスクランナー (just)
#
# dotnet SDK は mise 管理で素の PATH から見えないため、すべて `mise exec -- dotnet` 経由で呼ぶ。
# WinUI 本体(TypingSound.App)は x64 固定でビルド/publish する(slnx 全体に -p:Platform=x64 を渡すと
# "Debug|x64 は無効" になるため、App は .csproj を直接ターゲットにする)。

set windows-shell := ["powershell.exe", "-NoLogo", "-NoProfile", "-Command"]

dotnet   := "mise exec -- dotnet"
app      := "TypingSound.App/TypingSound.App.csproj"
launcher := "TypingSound.Launcher/TypingSound.Launcher.csproj"
tests    := "TypingSound.Core.Tests/TypingSound.Core.Tests.csproj"
tfm      := "net10.0-windows10.0.26100.0"

# レシピ一覧(既定)
default:
    @just --list

# Debug ビルド (x64)
build:
    {{dotnet}} build {{app}} -p:Platform=x64

# 全プロジェクトをビルド(テスト/ランチャー含む。slnx は既定プラットフォームで)
build-all:
    {{dotnet}} build TypingSound.slnx

# Core のユニットテスト
test:
    {{dotnet}} test {{tests}}

# 実行中インスタンスを停止
kill:
    -taskkill /F /IM TypingSound.App.exe 2>$null

# ビルドして起動(アンパッケージなので exe 直起動でよい) 既定 x64
run arch="x64": kill build
    Start-Process "TypingSound.App/bin/{{arch}}/Debug/{{tfm}}/win-{{arch}}/TypingSound.App.exe"

# 本体のみ Release publish(ポータブル一式) 既定 x64。例: just publish arm64
publish arch="x64":
    {{dotnet}} publish {{app}} -c Release -p:Platform={{arch}} -p:PublishProfile=win-{{arch}}

# 配布レイアウトを作成: dist/TypingSound/{TypingSound.exe(入口), app/(本体一式)} 既定 x64
# 各行は独立した powershell -Command で実行される(変数は持ち越さない / 実行ポリシーに非依存)。
dist arch="x64": (publish arch)
    {{dotnet}} publish {{launcher}} -c Release -r win-{{arch}} --self-contained true
    if (Test-Path dist/TypingSound) { Remove-Item -Recurse -Force dist/TypingSound }
    New-Item -ItemType Directory -Force -Path dist/TypingSound/app -ErrorAction Stop | Out-Null
    Copy-Item "TypingSound.App/bin/Release/{{tfm}}/win-{{arch}}/publish/*" dist/TypingSound/app -Recurse -Force -ErrorAction Stop
    Copy-Item "TypingSound.Launcher/bin/Release/net10.0/win-{{arch}}/publish/TypingSound.exe" dist/TypingSound/TypingSound.exe -Force -ErrorAction Stop
    Set-Content -Path dist/TypingSound/README.txt -Encoding UTF8 -Value 'TypingSound.exe をダブルクリックで起動します。本体一式(ランタイム同梱)は app\ にあります。フォルダごと移動/コピーすれば動きます。'
    Write-Host ("dist 作成完了: dist/TypingSound  (直下に TypingSound.exe + README.txt + app\ ; app内 {0} ファイル)" -f (Get-ChildItem dist/TypingSound/app -Recurse -File).Count)

# ビルド成果物と dist を削除
clean:
    -taskkill /F /IM TypingSound.App.exe 2>$null
    {{dotnet}} clean TypingSound.slnx
    if (Test-Path dist) { Remove-Item -Recurse -Force dist }
