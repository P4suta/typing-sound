namespace TypingSound.Core.Abstractions;

/// <summary>
/// Abstraction over the low-level audio output device. Implementations (Platform layer) keep a
/// single device open and add a mixer input per <see cref="Play"/> (overlapping playback); they do
/// not open/close the device per keystroke.
/// </summary>
public interface IAudioEngine
{
    /// <summary>Starts playing a clip and returns a handle for stopping it.</summary>
    IPlayingSound Play(ISoundClip clip);
}
