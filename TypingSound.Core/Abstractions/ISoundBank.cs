namespace TypingSound.Core.Abstractions;

/// <summary>読み込み済み音声クリップの集合。</summary>
public interface ISoundBank
{
    /// <summary>利用可能な全クリップ。</summary>
    IReadOnlyList<ISoundClip> Clips { get; }

    /// <summary>識別子でクリップを探す。見つからなければ <see langword="null"/>。</summary>
    /// <param name="id">探すクリップの識別子。</param>
    ISoundClip? FindById(string id);
}
