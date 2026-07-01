namespace TypingSound.Core.Abstractions;

/// <summary>
/// Abstraction over a one-shot timer. Each <see cref="Schedule"/> call cancels the previous
/// reservation and restarts with the new delay (used for debouncing). Platform/App implementations
/// fire the callback on the UI thread, guaranteeing the pipeline's thread affinity (lock-free). Tests
/// use a fake implementation to advance time.
/// </summary>
public interface ISoundTimer : IDisposable
{
    /// <summary>Schedules <paramref name="callback"/> to fire once after the delay (cancels any prior reservation).</summary>
    void Schedule(TimeSpan delay, Action callback);

    /// <summary>Cancels the pending callback.</summary>
    void Cancel();
}
