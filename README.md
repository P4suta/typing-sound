# TypingSound

[![CI](https://github.com/P4suta/typing-sound/actions/workflows/ci.yml/badge.svg)](https://github.com/P4suta/typing-sound/actions/workflows/ci.yml)
[![CodeQL](https://github.com/P4suta/typing-sound/actions/workflows/codeql.yml/badge.svg)](https://github.com/P4suta/typing-sound/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/P4suta/typing-sound/badge)](https://scorecard.dev/viewer/?uri=github.com/P4suta/typing-sound)
[![Release](https://img.shields.io/github/v/release/P4suta/typing-sound?sort=semver)](https://github.com/P4suta/typing-sound/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> A tray-resident Windows app that plays a sound on every keystroke, like a
> typewriter, with a carriage-return bell on Enter.

It never handles what you type. It sees only that a key was pressed and its
rough class (normal key / return key).

## Features

- **Typewriter mode** — a keystroke sound per key, a return bell on Enter.
- **Portable** — no install. A self-contained build with the runtime bundled; copy
  the folder and run (no admin rights or developer mode needed).
- **Tray-resident & light** — lives in the system tray. Heavy assemblies (ML,
  WinForms, etc.) are stripped at build time.
- **Follows the default audio device** — tracks output-device switches.
- **Private** — key contents are never recorded or sent. No network traffic.

## Download

Get the zip from the [latest release](https://github.com/P4suta/typing-sound/releases/latest),
extract it, and double-click `TypingSound.exe`. The folder can be moved or copied
freely.

Landing page: https://P4suta.github.io/typing-sound/

## Architecture

Platform-independent domain logic (`TypingSound.Core`) is strictly separated from
the Windows-specific implementation (`TypingSound.Platform` / `TypingSound.App`).

- **`TypingSound.Core`** — domain layer; pure logic with no OS or audio dependency.
  Sound behaviour decomposes into three orthogonal axes wired by `SoundModePipeline`;
  a new behaviour is composed by swapping an axis implementation.

  | Axis | Role | Examples |
  | --- | --- | --- |
  | **Trigger** | when to play | `EveryKeyTrigger`, `DebounceTrigger` |
  | **Selector** | which clip | `FixedSelector`, `RandomSelector`, `ShuffleQueueSelector`, `TypewriterSelector` |
  | **Playback** | how (voice management) | `MonophonicPolicy`, `PolyphonicPolicy` |

  ```
  key press ─▶ Trigger ──(fire)──▶ Selector ──(clip)──▶ Playback ─▶ sound
  ```

  `Core` touches timers, randomness, and audio only through abstractions, so tests
  substitute fakes and verify deterministically.
- **`TypingSound.Platform`** — Windows adapters satisfying the Core interfaces:
  NAudio-based audio, and the low-level keyboard hook (P/Invoke).
- **`TypingSound.App`** — WinUI 3 shell: tray UI, DI wiring, diagnostics/logging.
- **`TypingSound.Launcher`** — portable entry exe (self-contained launcher).
- **`TypingSound.Core.Tests`** — xUnit tests for Core (incl. property tests).

## Build & run

Requires Windows 11 (WinUI 3 / Windows App SDK) and
[mise](https://mise.jdx.dev/), which pins the .NET SDK and
[just](https://github.com/casey/just) via `mise.toml`.

```sh
mise install        # install the pinned dotnet SDK, just, etc.
just setup          # install git hooks (lefthook: Conventional Commits check)
just build          # Debug build of the App (x64)
just run            # build and launch
```

The .NET SDK is mise-managed and not on the bare PATH — call it through `just`
(which runs `mise exec -- dotnet`). See the `justfile` for the full task list.

## Release & contributing

- Versions are decided automatically by
  [release-please](https://github.com/googleapis/release-please) from
  [Conventional Commits](https://www.conventionalcommits.org/); versions are never
  hand-edited. See [`docs/RELEASING.md`](docs/RELEASING.md).
- Distributables are Authenticode-signed (SSL.com eSigner) and ship a CycloneDX SBOM
  plus Sigstore build-provenance / SBOM attestations. Verify:
  `gh attestation verify <zip> --repo P4suta/typing-sound`. See
  [`docs/SUPPLY_CHAIN.md`](docs/SUPPLY_CHAIN.md) and [`docs/SIGNING.md`](docs/SIGNING.md).
- Contribution guide: [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Credits

- Keystroke / bell sounds (`TypingSound.App/Assets/Sounds/*.wav`):
  [Mixkit](https://mixkit.co) (Mixkit Free License)
- App icon: original to this project

## License

Source code is under the [MIT License](LICENSE). The bundled sound assets follow the
Mixkit license terms (see `LICENSE`).
