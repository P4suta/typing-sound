namespace TypingSound.Core.Abstractions;

/// <summary>
/// 再生可能な音声クリップ 1 つを表す不透明ハンドル。
/// Core 層は音声データの中身を知らず、識別子と参照同一性だけを扱う。
/// 実際のデコード/再生は <see cref="IAudioEngine"/> の実装(Platform 層)が担う。
/// </summary>
public interface ISoundClip
{
    /// <summary>クリップの識別子(例: ファイル名から導出)。</summary>
    string Id { get; }
}
