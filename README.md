# TypingSound

Play typing sounds from the Windows system tray, with a carriage-return bell on Enter.

## Download

Download the ZIP from the [latest release](https://github.com/P4suta/typing-sound/releases/latest), extract it, and run `TypingSound.exe`.
Windows 11 is required.

## Build and run

Install [mise](https://mise.jdx.dev/), then use the pinned tools:

```sh
mise install
mise x -- just setup
mise x -- just build-all
mise x -- just run
```

Use `mise x -- just verify` for formatting, the full build, and coverage tests.
See [CONTRIBUTING.md](CONTRIBUTING.md) and the [architecture guide](docs/architecture.md) for development.
Release and verification procedures live in [RELEASING.md](docs/RELEASING.md), [SIGNING.md](docs/SIGNING.md), and [SUPPLY_CHAIN.md](docs/SUPPLY_CHAIN.md).

## License and credits

Source code uses the [MIT License](LICENSE).
Bundled keystroke and bell sounds come from [Mixkit](https://mixkit.co) under the Mixkit Free License; see `LICENSE`.
The app icon is original to this project.
