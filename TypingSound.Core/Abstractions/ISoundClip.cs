namespace TypingSound.Core.Abstractions;

/// <summary>
/// Opaque handle to a single playable sound clip. The Core layer does not know the audio data, only
/// the identifier and reference identity; decoding/playback is done by the <see cref="IAudioEngine"/>
/// implementation (Platform layer).
/// </summary>
public interface ISoundClip
{
    /// <summary>Clip identifier (e.g. derived from the file name).</summary>
    string Id { get; }
}
