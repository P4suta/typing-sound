namespace TypingSound.Core.Abstractions;

/// <summary>
/// 低レベルな音声出力デバイスの抽象。実装(Platform 層)はデバイスを 1 つ開きっぱなしにし、
/// <see cref="Play"/> ごとにミキサーへ入力を足す(重ね再生)。1 打ごとの open/close はしない。
/// </summary>
public interface IAudioEngine
{
    /// <summary>クリップの再生を開始し、停止操作用のハンドルを返す。</summary>
    /// <param name="clip">再生するクリップ。</param>
    IPlayingSound Play(ISoundClip clip);
}
