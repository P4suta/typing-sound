using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>Polyphonic. Overlays new sounds without stopping playing ones (fast typing forms chords).</summary>
public sealed class PolyphonicPolicy : IPlaybackPolicy
{
    private readonly IAudioEngine _engine;

    /// <summary>Creates the policy with an audio engine.</summary>
    public PolyphonicPolicy(IAudioEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }

    /// <inheritdoc/>
    public void Play(ISoundClip clip) => _engine.Play(clip);
}
