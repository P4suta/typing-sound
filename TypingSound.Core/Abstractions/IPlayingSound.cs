namespace TypingSound.Core.Abstractions;

/// <summary>再生中の 1 音への操作ハンドル。</summary>
public interface IPlayingSound
{
    /// <summary>この音の再生を直ちに止める(モノフォニックの「直前を止める」用)。</summary>
    void Halt();
}
