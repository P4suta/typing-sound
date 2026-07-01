using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using TypingSound.Core.Abstractions;

namespace TypingSoundApp.Timing;

/// <summary>
/// One-shot timer built on <see cref="DispatcherQueueTimer"/>. The callback fires on the <b>UI thread</b>,
/// so debounce-driven pipeline processing stays single-threaded on the UI thread and Core needs no locks.
/// Exceptions are swallowed at the boundary so Tick never leaks them into WinUI (which could crash via UnhandledException).
/// </summary>
internal sealed partial class DispatcherQueueSoundTimer : ISoundTimer
{
    private readonly DispatcherQueueTimer _timer;
    private readonly ILogger _logger;
    private Action? _callback;

    public DispatcherQueueSoundTimer(DispatcherQueue queue, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _timer = queue.CreateTimer();
        _timer.IsRepeating = false;
        _timer.Tick += OnTick;
    }

    public void Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callback = callback;
        _timer.Interval = delay;
        _timer.Stop();
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _callback = null;
    }

    public void Dispose()
    {
        _timer.Tick -= OnTick;
        _timer.Stop();
        _callback = null;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "debounce tick handler threw and was swallowed")]
    private static partial void LogTickHandlerThrew(ILogger logger, Exception ex);

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        // Log inside the exception filter and return true so Tick never leaks the exception.
        static bool LogAndSwallow(ILogger logger, Exception ex)
        {
            LogTickHandlerThrew(logger, ex);
            return true;
        }

        _timer.Stop();
        Action? callback = _callback;
        _callback = null;
        if (callback is null)
        {
            return;
        }

        try
        {
            callback();
        }
        catch (Exception ex) when (LogAndSwallow(_logger, ex))
        {
        }
    }
}
