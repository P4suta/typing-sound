# Architecture

`TypingSound.Core` holds sound behavior behind interfaces for time, randomness, and playback.
Tests use fakes for these boundaries.
`SoundModePipeline` combines a trigger, clip selector, and voice policy.

| Part | Responsibility | Examples |
| --- | --- | --- |
| Trigger | Decide when to play | `EveryKeyTrigger`, `DebounceTrigger` |
| Selector | Choose a clip | `FixedSelector`, `RandomSelector`, `ShuffleQueueSelector`, `TypewriterSelector` |
| Playback | Manage voices | `MonophonicPolicy`, `PolyphonicPolicy` |

`TypingSound.Platform` implements Windows keyboard input and NAudio playback.
`TypingSound.App` provides the WinUI 3 tray interface, dependency wiring, and diagnostics.
`TypingSound.Launcher` provides the portable entry executable.
`TypingSound.Core.Tests` checks the Core behavior with xUnit and property tests.
`TypingSound.Platform.Tests` checks cached audio reads against an independent queue without using an audio device.

See [CONTRIBUTING.md](../CONTRIBUTING.md) for the development checks.
