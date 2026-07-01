using TypingSound.Core.Abstractions;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Modes;

/// <summary>
/// Typewriter mode. General keys play a keystroke sound (cycled without repeats), Enter plays a
/// return bell. Sounds overlap (polyphonic).
/// = <see cref="EveryKeyTrigger"/> + <see cref="TypewriterSelector"/> (keystrokes delegate to
///   <see cref="ShuffleQueueSelector"/>) + <see cref="PolyphonicPolicy"/>.
/// </summary>
public sealed class TypewriterMode : ISoundMode
{
    private readonly IReadOnlyList<ISoundClip> _keystrokePool;
    private readonly ISoundClip? _returnBell;

    /// <summary>Creates the mode from a keystroke sound pool and a return bell.</summary>
    /// <param name="keystrokePool">Keystroke clips played on general keys.</param>
    /// <param name="returnBell">Return bell played on Enter (<see langword="null"/> means Enter is silent).</param>
    public TypewriterMode(IReadOnlyList<ISoundClip> keystrokePool, ISoundClip? returnBell)
    {
        ArgumentNullException.ThrowIfNull(keystrokePool);
        _keystrokePool = keystrokePool;
        _returnBell = returnBell;
    }

    /// <inheritdoc/>
    public string Id => "typewriter";

    /// <inheritdoc/>
    public string DisplayName => "Typewriter (keystroke + return bell on Enter)";

    /// <inheritdoc/>
    public IActiveMode Activate(SoundModeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new SoundModePipeline(
            new EveryKeyTrigger(),
            new TypewriterSelector(new ShuffleQueueSelector(_keystrokePool, context.Random), _returnBell),
            new PolyphonicPolicy(context.Audio));
    }
}
