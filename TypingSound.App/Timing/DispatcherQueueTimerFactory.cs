using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using TypingSound.Core.Abstractions;

namespace TypingSoundApp.Timing;

/// <summary>Factory that supplies timers firing on the UI thread's <see cref="DispatcherQueue"/>.</summary>
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
