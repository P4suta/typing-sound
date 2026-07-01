using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>
/// Selector that cycles through all clips without repeats, reshuffling when exhausted to loop
/// forever. On reshuffle, the clip played last is kept off the front of the new cycle to avoid a
/// back-to-back repeat (the same sound playing twice in a row) at the loop boundary.
/// </summary>
public sealed class ShuffleQueueSelector : ISoundSelector
{
    private readonly IReadOnlyList<ISoundClip> _clips;
    private readonly IRandomSource _random;
    private readonly Queue<ISoundClip> _queue = new();
    private ISoundClip? _lastPlayed;

    /// <summary>Creates the selector with a set of clips and a random source.</summary>
    public ShuffleQueueSelector(IReadOnlyList<ISoundClip> clips, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(clips);
        ArgumentNullException.ThrowIfNull(random);
        _clips = clips;
        _random = random;
    }

    /// <inheritdoc/>
    public ISoundClip? Pick(KeyCategory category)
    {
        if (_clips.Count == 0)
        {
            return null;
        }

        if (_queue.Count == 0)
        {
            Refill();
        }

        ISoundClip clip = _queue.Dequeue();
        _lastPlayed = clip;
        return clip;
    }

    private void Refill()
    {
        List<ISoundClip> shuffled = [.. _clips];

        // Fisher-Yates shuffle.
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = _random.NextBelow(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Avoid boundary repeat: if the new cycle starts with the last-played clip, swap it with the second.
        if (shuffled.Count > 1 && ReferenceEquals(shuffled[0], _lastPlayed))
        {
            (shuffled[0], shuffled[1]) = (shuffled[1], shuffled[0]);
        }

        foreach (ISoundClip clip in shuffled)
        {
            _queue.Enqueue(clip);
        }
    }
}
