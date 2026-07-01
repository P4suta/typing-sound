using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>Monophonic. Stops the previous sound before playing a new one (only the latest sound plays).</summary>
public sealed class MonophonicPolicy : IPlaybackPolicy
{
    private readonly IAudioEngine _engine;
    private IPlayingSound? _current;

    /// <summary>Creates the policy with an audio engine.</summary>
    public MonophonicPolicy(IAudioEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }

    /// <inheritdoc/>
    public void Play(ISoundClip clip)
    {
        _current?.Halt();
        _current = _engine.Play(clip);
    }
}
