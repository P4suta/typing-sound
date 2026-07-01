using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>
/// A single running mode instance, responsible for producing sound in response to key presses.
/// Both the clip-based approach (<see cref="SoundModePipeline"/>) and continuous-voice approaches
/// take this shape. Assumed to be called from a single (UI) thread. Disposing releases resources
/// (timers/voices).
/// </summary>
public interface IActiveMode : IDisposable
{
    /// <summary>Notifies that a single key was pressed.</summary>
    void OnKeyPressed(KeyCategory category);
}
