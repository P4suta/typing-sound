using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>
/// テスト用の単発タイマー。実時間を使わず、<see cref="Elapse"/> で手動発火する。
/// <see cref="Schedule"/> は前回の予約を上書きする(本物と同じデバウンス挙動)。
/// </summary>
internal sealed class FakeTimer : ISoundTimer
{
    private Action? _pending;

    public TimeSpan? LastDelay { get; private set; }

    public int ScheduleCount { get; private set; }

    public bool HasPending => _pending is not null;

    public void Schedule(TimeSpan delay, Action callback)
    {
        LastDelay = delay;
        _pending = callback;
        ScheduleCount++;
    }

    public void Cancel() => _pending = null;

    /// <summary>予約中のコールバックを 1 回だけ発火する(時間経過の模擬)。</summary>
    public void Elapse()
    {
        Action? callback = _pending;
        _pending = null;
        callback?.Invoke();
    }

    public void Dispose() => _pending = null;
}
