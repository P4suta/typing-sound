# TypingSound task runner (just)
#
# dotnet SDK is mise-managed and not on the bare PATH, so everything goes through `mise exec -- dotnet`.
# The WinUI app (TypingSound.App) builds/publishes at fixed x64; target its .csproj directly since passing
# -p:Platform=x64 to the whole slnx yields "Debug|x64 is invalid".

set windows-shell := ["powershell.exe", "-NoLogo", "-NoProfile", "-Command"]

dotnet   := "mise exec -- dotnet"
app      := "TypingSound.App/TypingSound.App.csproj"
launcher := "TypingSound.Launcher/TypingSound.Launcher.csproj"
tests    := "TypingSound.Core.Tests/TypingSound.Core.Tests.csproj"
tfm      := "net10.0-windows10.0.26100.0"

# Minimum Core line-coverage (%). Ratchet: raise as coverage improves.
cov_threshold := "90"

# List recipes (default)
default:
    @just --list

# Debug build (x64)
build:
    {{dotnet}} build {{app}} -p:Platform=x64

# Build all projects (tests/launcher included; slnx on default platform)
build-all:
    {{dotnet}} build TypingSound.slnx

# Core unit tests
test:
    {{dotnet}} test {{tests}}

# Tests with coverage; fails the build below cov_threshold (CI gate).
test-cov:
    {{dotnet}} test {{tests}} -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura -p:Threshold={{cov_threshold}} -p:ThresholdType=line -p:ThresholdStat=total

# Dev setup: install git hooks (lefthook: commit-msg Conventional Commits check).
setup:
    mise exec -- lefthook install

# Format check (.editorconfig whitespace/layout only); naming/style is enforced by the strict build.
fmt-check:
    {{dotnet}} format TypingSound.slnx whitespace --verify-no-changes

# Auto-fix formatting.
fmt:
    {{dotnet}} format TypingSound.slnx whitespace

# Pre-push verification: format + full build + coverage tests.
verify: fmt-check build-all test-cov

# Stop running instances
kill:
    -taskkill /F /IM TypingSound.App.exe 2>$null

# Build and launch (unpackaged, so run the exe directly); default x64
run arch="x64": kill build
    Start-Process "TypingSound.App/bin/{{arch}}/Debug/{{tfm}}/win-{{arch}}/TypingSound.App.exe"

# Release publish the app only (portable bundle); default x64. e.g. just publish arm64
publish arch="x64":
    {{dotnet}} publish {{app}} -c Release -p:Platform={{arch}} -p:PublishProfile=win-{{arch}}

# Build the distribution layout: dist/TypingSound/{TypingSound.exe (entry), app/ (bundle)}; default x64
# Each line runs as its own powershell -Command (no variable carryover / execution-policy independent).
dist arch="x64": (publish arch)
    {{dotnet}} publish {{launcher}} -c Release -r win-{{arch}} --self-contained true
    if (Test-Path dist/TypingSound) { Remove-Item -Recurse -Force dist/TypingSound }
    New-Item -ItemType Directory -Force -Path dist/TypingSound/app -ErrorAction Stop | Out-Null
    Copy-Item "TypingSound.App/bin/Release/{{tfm}}/win-{{arch}}/publish/*" dist/TypingSound/app -Recurse -Force -ErrorAction Stop
    Copy-Item "TypingSound.Launcher/bin/Release/net10.0/win-{{arch}}/publish/TypingSound.exe" dist/TypingSound/TypingSound.exe -Force -ErrorAction Stop
    Set-Content -Path dist/TypingSound/README.txt -Encoding UTF8 -Value 'Double-click TypingSound.exe to start. The full app (runtime included) lives in app\. Move or copy the whole folder to run it anywhere.'
    Write-Host ("dist created: dist/TypingSound  (root has TypingSound.exe + README.txt + app\ ; {0} files in app)" -f (Get-ChildItem dist/TypingSound/app -Recurse -File).Count)

# Zip + checksum the distribution into build/package/. e.g. just package v0.1.0
# Run just dist {{arch}} first to create dist/TypingSound (release.yml/nightly.yml do).
package tag arch="x64":
    if (-not (Test-Path dist/TypingSound)) { throw "dist/TypingSound not found. Run just dist {{arch}} first." }
    New-Item -ItemType Directory -Force -Path build/package -ErrorAction Stop | Out-Null
    $zip = "build/package/TypingSound-{{tag}}-win-{{arch}}.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path dist/TypingSound/* -DestinationPath $zip -Force
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
    "$hash  $(Split-Path $zip -Leaf)" | Set-Content -Path build/package/SHA256SUMS.txt -Encoding ascii
    Write-Host ("package created: {0}" -f $zip)

# Remove build artifacts and dist
clean:
    -taskkill /F /IM TypingSound.App.exe 2>$null
    {{dotnet}} clean TypingSound.slnx
    if (Test-Path dist) { Remove-Item -Recurse -Force dist }
    if (Test-Path build) { Remove-Item -Recurse -Force build }
