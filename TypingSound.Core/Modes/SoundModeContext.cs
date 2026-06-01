using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>モードがパイプラインを構築する際に使う実行時サービス一式。</summary>
/// <param name="Audio">音声出力エンジン。</param>
/// <param name="Timers">タイマーファクトリ(デバウンス系トリガ用)。</param>
/// <param name="Random">乱数source(ランダム/シャッフル用)。</param>
public sealed record SoundModeContext(
    IAudioEngine Audio,
    ITimerFactory Timers,
    IRandomSource Random);
