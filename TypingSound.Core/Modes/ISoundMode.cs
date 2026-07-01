namespace TypingSound.Core.Modes;

/// <summary>
/// Definition of a "mode". <see cref="Activate"/> creates one <see cref="IActiveMode"/> per session.
/// The clip-based approach composes three axes into a <see cref="SoundModePipeline"/>; other
/// approaches return a different <see cref="IActiveMode"/>.
/// </summary>
public interface ISoundMode
{
    /// <summary>Stable identifier (used for settings persistence and default selection).</summary>
    string Id { get; }

    /// <summary>Display name for the UI.</summary>
    string DisplayName { get; }

    /// <summary>Activates a running instance of this mode from runtime services.</summary>
    /// <param name="context">Runtime services: audio, timers, random, etc.</param>
    IActiveMode Activate(SoundModeContext context);
}
