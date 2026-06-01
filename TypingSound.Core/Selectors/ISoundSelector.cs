using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>
/// 軸B:「どのクリップを鳴らすか」を決める戦略。一部はステートフル(シャッフルキュー等)。
/// </summary>
public interface ISoundSelector
{
    /// <summary>次に鳴らすクリップを選ぶ。鳴らさない場合は <see langword="null"/>。</summary>
    /// <param name="category">発火契機のキー分類。キーに依らないセレクタは無視してよい。</param>
    ISoundClip? Pick(KeyCategory category);
}
