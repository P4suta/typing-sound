# Contributing to TypingSound

Read the [Code of Conduct](.github/CODE_OF_CONDUCT.md) before contributing.
See the [README](README.md) for setup and the [architecture guide](docs/architecture.md) for the component overview.
Release operations live in [docs/RELEASING.md](docs/RELEASING.md).

## Commits & PRs

- Use [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`,
  `docs:`, `refactor:`, `test:`, `chore:`, `ci:` …). This is **enforced** and drives
  automatic versioning: a local lefthook `commit-msg` hook checks each message, and a
  CI gate (`amannn/action-semantic-pull-request`) checks the PR title.
- PRs are squash-merged, so the PR title becomes the commit — write it as a
  Conventional Commit.
- Before pushing, confirm `just build-all` and `just test` are green. The quality
  gates are strict (warnings-as-errors, analyzers, StyleCop / Roslynator); fix causes
  rather than suppress.

`just test` checks Core behavior and the audio adapter without requiring an audio device.
Run `just test-audio-device` on Windows with an active playback device to check silent engine startup, bundled audio decoding, and disposal.

## Scope

TypingSound is a small, portable, tray-resident app that plays a sound per keystroke.
Recording/sending key contents, networking, key remapping/macros, and
cross-platform support are intentional non-goals. Check the out-of-scope list in the
feature-request template before proposing a feature.

## License

By contributing you agree that your contribution is licensed under the
[MIT License](LICENSE).
