using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace TypingSoundApp.Diagnostics;

/// <summary>
/// Serilog sink that thread-safely retains the most recent <see cref="Capacity"/> formatted log
/// strings. Intended for display via <see cref="Snapshot"/>, e.g. from a diagnostics UI.
/// </summary>
internal sealed class RingBufferSink : ILogEventSink
{
    private const int Capacity = 500;

    private readonly object _gate = new();
    private readonly Queue<string> _buffer = new(Capacity);

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        string message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
        string timestamp = logEvent.Timestamp.ToString("O", CultureInfo.InvariantCulture);
        string line = logEvent.Exception is null
            ? $"{timestamp} [{logEvent.Level}] {message}"
            : $"{timestamp} [{logEvent.Level}] {message}{Environment.NewLine}{logEvent.Exception}";

        lock (_gate)
        {
            _buffer.Enqueue(line);
            while (_buffer.Count > Capacity)
            {
                _buffer.Dequeue();
            }
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            return _buffer.ToArray();
        }
    }
}
