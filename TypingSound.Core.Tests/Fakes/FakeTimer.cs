using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>
/// Test one-shot timer that uses no real time and fires manually via <see cref="Elapse"/>.
/// <see cref="Schedule"/> overwrites the previous reservation (same debounce behavior as the real one).
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

    /// <summary>Fires the pending callback once (simulates time passing).</summary>
    public void Elapse()
    {
        Action? callback = _pending;
        _pending = null;
        callback?.Invoke();
    }

    public void Dispose() => _pending = null;
}
