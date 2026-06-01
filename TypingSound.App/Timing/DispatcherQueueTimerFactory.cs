using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using TypingSound.Core.Abstractions;

namespace TypingSoundApp.Timing;

/// <summary>UI スレッドの <see cref="DispatcherQueue"/> 上で発火するタイマーを供給するファクトリ。</summary>
internal sealed class DispatcherQueueTimerFactory : ITimerFactory
{
    private readonly DispatcherQueue _queue;
    private readonly ILogger _logger;

    public DispatcherQueueTimerFactory(DispatcherQueue queue, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(logger);
        _queue = queue;
        _logger = logger;
    }

    public ISoundTimer Create() => new DispatcherQueueSoundTimer(_queue, _logger);
}
