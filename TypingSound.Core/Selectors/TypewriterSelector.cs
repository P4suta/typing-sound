using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>
/// タイプライター風にキー種別で出し分けるセレクタ。
/// Enter(復帰)では復帰ベルを、それ以外のキーでは打鍵音セレクタに委譲する。
/// 打鍵音の選び方(ランダム / 重複なし 等)は委譲先セレクタに任せ、ここは出し分けだけを担う(合成)。
/// </summary>
public sealed class TypewriterSelector : ISoundSelector
{
    private readonly ISoundSelector _keystrokeSelector;
    private readonly ISoundClip? _returnBell;

    /// <summary>打鍵音セレクタと復帰ベルを指定して生成する。</summary>
    /// <param name="keystrokeSelector">一般キーで使う打鍵音セレクタ。</param>
    /// <param name="returnBell">Enter で鳴らす復帰ベル(<see langword="null"/> なら Enter は無音)。</param>
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
