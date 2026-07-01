using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>Selector that picks one clip at random each time (may repeat the previous one).</summary>
public sealed class RandomSelector : ISoundSelector
{
    private readonly IReadOnlyList<ISoundClip> _clips;
    private readonly IRandomSource _random;

    /// <summary>Creates the selector with a set of clips and a random source.</summary>
    public RandomSelector(IReadOnlyList<ISoundClip> clips, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(clips);
        ArgumentNullException.ThrowIfNull(random);
        _clips = clips;
        _random = random;
    }

    /// <inheritdoc/>
    public ISoundClip? Pick(KeyCategory category) => _clips.Count == 0 ? null : _clips[_random.NextBelow(_clips.Count)];
}
