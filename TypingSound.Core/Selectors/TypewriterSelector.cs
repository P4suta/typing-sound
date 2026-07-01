using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>
/// Typewriter-style selector that dispatches by key category: Enter (return) plays the return bell,
/// other keys delegate to the keystroke selector. How keystrokes are chosen (random, no-repeat, etc.)
/// is left to the delegate; this class only does the dispatch.
/// </summary>
public sealed class TypewriterSelector : ISoundSelector
{
    private readonly ISoundSelector _keystrokeSelector;
    private readonly ISoundClip? _returnBell;

    /// <summary>Creates the selector with a keystroke selector and a return bell.</summary>
    /// <param name="keystrokeSelector">Keystroke selector used for general keys.</param>
    /// <param name="returnBell">Return bell played on Enter (<see langword="null"/> means Enter is silent).</param>
    public TypewriterSelector(ISoundSelector keystrokeSelector, ISoundClip? returnBell)
    {
        ArgumentNullException.ThrowIfNull(keystrokeSelector);
        _keystrokeSelector = keystrokeSelector;
        _returnBell = returnBell;
    }

    /// <inheritdoc/>
    public ISoundClip? Pick(KeyCategory category) =>
        category == KeyCategory.Enter ? _returnBell : _keystrokeSelector.Pick(category);
}
