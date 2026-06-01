using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>
/// 重複なしで一巡し、尽きたら再シャッフルして無限ループするセレクタ(「重複なしでループし続ける」)。
/// 再シャッフル時、直前に鳴らしたクリップが新しい一巡の先頭に来ないようにして、
/// ループ境界での連続重複(同じ音が 2 回続けて鳴る)を防ぐ。
/// </summary>
public sealed class ShuffleQueueSelector : ISoundSelector
{
    private readonly IReadOnlyList<ISoundClip> _clips;
    private readonly IRandomSource _random;
    private readonly Queue<ISoundClip> _queue = new();
    private ISoundClip? _lastPlayed;

    /// <summary>選択対象のクリップ群と乱数source を指定して生成する。</summary>
    /// <param name="clips">一巡の対象となるクリップ群。</param>
    /// <param name="random">シャッフルに使う乱数source。</param>
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

        // Fisher-Yates シャッフル。
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = _random.NextBelow(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // ループ境界の連続重複回避: 新しい一巡の先頭が直前再生クリップなら 2 番目と入れ替える。
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
