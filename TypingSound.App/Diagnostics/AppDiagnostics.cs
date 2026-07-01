using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace TypingSoundApp.Diagnostics;

/// <summary>
/// App diagnostics/observability infrastructure. Configures Serilog to write to a rolling file,
/// Debug output, and an in-memory ring buffer. Exposes an <see cref="ILoggerFactory"/> used to
/// create loggers at the Platform/App boundaries.
/// </summary>
public sealed class AppDiagnostics : IDisposable
{
    private readonly Logger _logger;
    private readonly SerilogLoggerFactory _loggerFactory;
    private readonly RingBufferSink _ringBufferSink;
    private bool _disposed;

    public AppDiagnostics()
    {
        // Portable by design: logs live under the exe's folder, leaving no trace in machine-specific
        // locations such as %LOCALAPPDATA%.
        LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(LogDirectory);

        _ringBufferSink = new RingBufferSink();

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(LogDirectory, "typingsound-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Sink(_ringBufferSink)
            .CreateLogger();

        // dispose: false means SerilogLoggerFactory does not own the Logger; this class disposes it.
        _loggerFactory = new SerilogLoggerFactory(_logger, dispose: false);
    }

    public ILoggerFactory LoggerFactory => _loggerFactory;

    public string LogDirectory { get; }

    public IReadOnlyList<string> RecentEntries => _ringBufferSink.Snapshot();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _loggerFactory.Dispose();
        _logger.Dispose();
        GC.SuppressFinalize(this);
    }
}
