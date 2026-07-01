using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>Axis C: strategy deciding "how to play". Takes an <see cref="IAudioEngine"/> in its constructor.</summary>
public interface IPlaybackPolicy
{
    /// <summary>Plays a clip according to the policy.</summary>
    void Play(ISoundClip clip);
}
