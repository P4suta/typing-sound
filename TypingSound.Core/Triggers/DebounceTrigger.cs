using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// 入力が止まってから一定時間後に一度だけ発火するトリガ(「最後だけ＝打ち終わりに 1 回」)。
/// タイプライターの行末ベルのように、連打中は鳴らさず、静かになった瞬間に 1 回鳴らす。
/// 押下のたびにタイマーを測り直すことで「最後の押下からの静止」を検出する。
/// </summary>
public sealed class DebounceTrigger : ITriggerStrategy
{
    private readonly ISoundTimer _timer;
    private readonly TimeSpan _quietPeriod;

    /// <summary>静止と見なすまでの待ち時間とタイマーファクトリを指定して生成する。</summary>
    /// <param name="quietPeriod">最後の押下からこの時間だけ入力が無ければ発火する。</param>
    /// <param name="timers">UI スレッドで発火するタイマーを供給するファクトリ。</param>
    public DebounceTrigger(TimeSpan quietPeriod, ITimerFactory timers)
    {
        ArgumentNullException.ThrowIfNull(timers);
        _quietPeriod = quietPeriod;
        _timer = timers.Create();
    }

    /// <inheritdoc/>
    public event EventHandler<KeyPressedEventArgs>? Fired;

    /// <inheritdoc/>
    public void Notify(KeyCategory category) => _timer.Schedule(_quietPeriod, () => Fired?.Invoke(this, KeyPressedEventArgs.Other));

    /// <inheritdoc/>
    public void Dispose() => _timer.Dispose();
}
