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
tsbuild  := "eng/TsBuild/TsBuild.csproj"

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
    {{dotnet}} run --project {{tsbuild}} -- kill

# Build and launch (unpackaged, so run the exe directly); default x64
run arch="x64": kill build
    {{dotnet}} run --project {{tsbuild}} -- run {{arch}}

# Release publish the app only (portable bundle); default x64. e.g. just publish arm64
publish arch="x64":
    {{dotnet}} publish {{app}} -c Release -p:Platform={{arch}} -p:PublishProfile=win-{{arch}}

# Build the distribution layout: dist/TypingSound/{TypingSound.exe (entry), app/ (bundle)}; default x64
# The layout assembly (copy/README/checksums) lives in the C# tool eng/TsBuild, not shell.
dist arch="x64": (publish arch)
    {{dotnet}} publish {{launcher}} -c Release -r win-{{arch}} --self-contained true
    {{dotnet}} run --project {{tsbuild}} -- assemble {{arch}}

# Zip + checksum the distribution into build/package/. e.g. just package v0.1.0
# Run just dist {{arch}} first to create dist/TypingSound (release.yml/nightly.yml do).
package tag arch="x64":
    {{dotnet}} run --project {{tsbuild}} -- pack {{tag}} {{arch}}

# Remove build artifacts and dist
clean:
    {{dotnet}} run --project {{tsbuild}} -- clean
    {{dotnet}} clean TypingSound.slnx
