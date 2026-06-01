namespace TypingSound.Core.Abstractions;

/// <summary><see cref="ISoundTimer"/> を生成するファクトリ。</summary>
public interface ITimerFactory
{
    /// <summary>新しい単発タイマーを生成する。</summary>
    ISoundTimer Create();
}
