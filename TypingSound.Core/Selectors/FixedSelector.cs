using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>常に同じクリップを返すセレクタ(「最後だけ」の return ベル等)。</summary>
public sealed class FixedSelector : ISoundSelector
{
    private readonly ISoundClip? _clip;

    /// <summary>固定で返すクリップを指定して生成する(<see langword="null"/> なら常に無音)。</summary>
    /// <param name="clip">常に返すクリップ。</param>
    public FixedSelector(ISoundClip? clip) => _clip = clip;

    /// <inheritdoc/>
    public ISoundClip? Pick(KeyCategory category) => _clip;
}
