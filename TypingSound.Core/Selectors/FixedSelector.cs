using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>Selector that always returns the same clip (e.g. the "last only" return bell).</summary>
public sealed class FixedSelector : ISoundSelector
{
    private readonly ISoundClip? _clip;

    /// <summary>Creates the selector with the fixed clip to return (<see langword="null"/> means always silent).</summary>
    public FixedSelector(ISoundClip? clip) => _clip = clip;

    /// <inheritdoc/>
    public ISoundClip? Pick(KeyCategory category) => _clip;
}
