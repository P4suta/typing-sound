using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace TypingSoundApp.Diagnostics;

/// <summary>
/// App の診断/可観測性基盤。Serilog を構成し、ファイル(ローリング)・Debug 出力・
/// インメモリのリングバッファへログを送る。Microsoft.Extensions.Logging の
/// <see cref="ILoggerFactory"/> を公開し、境界(Platform/App)のロガー生成に用いる。
/// </summary>
public sealed class AppDiagnostics : IDisposable
{
    private readonly Logger _logger;
    private readonly SerilogLoggerFactory _loggerFactory;
    private readonly RingBufferSink _ringBufferSink;
    private bool _disposed;

    public AppDiagnostics()
    {
        // ポータブル方針: ログは exe と同じフォルダ配下に置き、マシン固有の場所(%LOCALAPPDATA% 等)へ痕跡を残さない。
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

        // dispose: false → SerilogLoggerFactory は Logger を所有しない。Logger は本クラスが破棄する。
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
