using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// Trigger that fires once a fixed time after input stops ("last only = one sound when typing ends").
/// Like a typewriter end-of-line bell: silent during fast typing, fires once when input goes quiet.
/// Restarting the timer on each key press detects "quiet since the last press".
/// </summary>
public sealed class DebounceTrigger : ITriggerStrategy
{
    private readonly ISoundTimer _timer;
    private readonly TimeSpan _quietPeriod;

    /// <summary>Creates the trigger with the quiet period and a timer factory.</summary>
    /// <param name="quietPeriod">Fires when no input arrives for this long after the last press.</param>
    /// <param name="timers">Factory supplying timers that fire on the UI thread.</param>
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
