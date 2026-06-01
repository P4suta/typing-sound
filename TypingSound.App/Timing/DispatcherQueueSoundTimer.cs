using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using TypingSound.Core.Abstractions;

namespace TypingSoundApp.Timing;

/// <summary>
/// <see cref="DispatcherQueueTimer"/> を用いた単発タイマー。コールバックは <b>UI スレッド</b>で発火するため、
/// デバウンス由来のパイプライン処理も UI スレッド単一親和となり、Core 側はロック不要になる。
/// Tick から例外を WinUI へ漏らさない(漏らすと UnhandledException でクラッシュしうる)よう境界で握りつぶす。
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
        // 例外フィルタ内でログし true を返すことで、Tick から例外を漏らさない。
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
            // 例外はフィルタ内でログ済み。ここでは握りつぶす。
        }
    }
}
