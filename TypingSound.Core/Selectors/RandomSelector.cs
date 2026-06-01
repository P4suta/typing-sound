using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>毎回ランダムに 1 つ選ぶセレクタ(直前と同じものを引くこともある)。</summary>
public sealed class RandomSelector : ISoundSelector
{
    private readonly IReadOnlyList<ISoundClip> _clips;
    private readonly IRandomSource _random;

    /// <summary>選択対象のクリップ群と乱数source を指定して生成する。</summary>
    /// <param name="clips">選択対象のクリップ群。</param>
    /// <param name="random">乱数source。</param>
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
