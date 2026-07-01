using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>Runtime services a mode uses when building its pipeline.</summary>
/// <param name="Audio">Audio output engine.</param>
/// <param name="Timers">Timer factory (for debounce-style triggers).</param>
/// <param name="Random">Random source (for random/shuffle selection).</param>
public sealed record SoundModeContext(
    IAudioEngine Audio,
    ITimerFactory Timers,
    IRandomSource Random);
