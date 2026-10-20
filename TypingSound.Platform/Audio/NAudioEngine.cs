using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// Plays overlapping cached audio through a shared WASAPI device.
/// Keeps the mixer format stable and recreates output when the default render device changes.
/// </summary>
public sealed partial class NAudioEngine : IAudioEngine, IDisposable
{
    private const int OutputLatencyMilliseconds = 60;

    private readonly ILogger _logger;
    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDeviceNotificationClient _notificationClient;
    private readonly MixingSampleProvider _mixer;

    // This lock serializes _output / _currentDevice / _currentDeviceId / _disposed, preventing races between the
    // device-change handler (background thread) and Dispose. Play only touches _mixer (thread-safe) and needs no lock.
    private readonly object _outputLock = new();
    private WasapiPlayer? _output;
    private MMDevice? _currentDevice;
    private string? _currentDeviceId;
    private bool _disposed;

    public NAudioEngine(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        _enumerator = new MMDeviceEnumerator();

        WaveFormat mixFormat;
        using (MMDevice device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
        {
            using AudioClient audioClient = device.CreateAudioClient();
            mixFormat = audioClient.MixFormat;
        }

        OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(mixFormat.SampleRate, mixFormat.Channels);
        _mixer = new MixingSampleProvider(OutputFormat) { ReadFully = true };

        // At startup fail outright if there is no device; only the recreate on device change swallows errors.
        lock (_outputLock)
        {
            StartOutputOnDefaultDevice();
        }

        _notificationClient = _enumerator.CreateNotificationClient(useSynchronizationContext: false);
        _notificationClient.DefaultDeviceChanged += (_, args) =>
        {
            if (args.Flow == DataFlow.Render && args.Role == Role.Multimedia)
            {
                OnDefaultRenderDeviceChanged(args.DeviceId);
            }
        };

        LogAudioOutput(_logger, OutputFormat.SampleRate, OutputFormat.Channels, OutputFormat.Encoding);
    }

    /// <summary>Output mixer format (clips are converted to this format on load). Unchanged across device changes.</summary>
    public WaveFormat OutputFormat { get; }

    /// <inheritdoc/>
    public IPlayingSound Play(ISoundClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (clip is not CachedSound cached)
        {
            throw new ArgumentException($"{nameof(NAudioEngine)} can only play {nameof(CachedSound)}.", nameof(clip));
        }

        // _mixer is thread-safe (AddMixerInput/Read both lock internally), so inputs can be added safely even while
        // the output is being recreated.
        CachedSoundSampleProvider provider = new(cached);
        _mixer.AddMixerInput(provider);
        return new MixerPlayingSound(_mixer, provider);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_outputLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Stop notifications first; any device-change task that starts afterward sees _disposed after taking the lock and returns.
            UnregisterNotifications();
            TeardownOutput();
            _enumerator.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "audio output {Rate}Hz {Channels}ch {Encoding}")]
    private static partial void LogAudioOutput(ILogger logger, int rate, int channels, WaveFormatEncoding encoding);

    [LoggerMessage(Level = LogLevel.Information, Message = "audio output switched to default device {DeviceId}")]
    private static partial void LogDeviceSwitched(ILogger logger, string deviceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "failed to start audio output on default device")]
    private static partial void LogOutputStartFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "failed to tear down previous audio output")]
    private static partial void LogTeardownFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "failed to unregister audio device notifications")]
    private static partial void LogUnregisterFailed(ILogger logger, Exception ex);

    // Recreate the multimedia output while holding _outputLock.
    private void StartOutputOnDefaultDevice()
    {
        MMDevice device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        WasapiPlayer output = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithLatency(OutputLatencyMilliseconds)
            .Build();
        output.Init(_mixer);
        output.Play();

        _output = output;
        _currentDevice = device;
        _currentDeviceId = device.ID;
    }

    // Call under the lock. On device loss (e.g. headphones unplugged) stop/dispose may throw, so swallow (CA1031).
    private void TeardownOutput()
    {
        static bool LogAndSwallow(ILogger logger, Exception ex)
        {
            LogTeardownFailed(logger, ex);
            return true;
        }

        try
        {
            _output?.Stop();
            _output?.Dispose();
            _currentDevice?.Dispose();
        }
        catch (Exception ex) when (LogAndSwallow(_logger, ex))
        {
        }
        finally
        {
            _output = null;
            _currentDevice = null;
            _currentDeviceId = null;
        }
    }

    private void UnregisterNotifications()
    {
        static bool LogAndSwallow(ILogger logger, Exception ex)
        {
            LogUnregisterFailed(logger, ex);
            return true;
        }

        try
        {
            _notificationClient.Dispose();
        }
        catch (Exception ex) when (LogAndSwallow(_logger, ex))
        {
        }
    }

    // Called on a background thread from the default-device-change notification (calling device APIs inside the COM callback can deadlock).
    private void RefreshOutputDevice(string? newDefaultDeviceId)
    {
        static bool LogAndSwallow(ILogger logger, Exception ex)
        {
            LogOutputStartFailed(logger, ex);
            return true;
        }

        lock (_outputLock)
        {
            if (_disposed)
            {
                return;
            }

            // Ignore change notifications for the same device (e.g. duplicate events from multiple roles).
            if (newDefaultDeviceId is not null && newDefaultDeviceId == _currentDeviceId)
            {
                return;
            }

            TeardownOutput();

            try
            {
                StartOutputOnDefaultDevice();
                LogDeviceSwitched(_logger, _currentDeviceId ?? "(unknown)");
            }
            catch (Exception ex) when (LogAndSwallow(_logger, ex))
            {
                // No available device, etc. The next change notification retries the recreate.
            }
        }
    }

    // Called from the notification callback. Offloads the actual recreate to a background thread so the callback returns immediately.
    private void OnDefaultRenderDeviceChanged(string? defaultDeviceId) =>
        _ = Task.Run(() => RefreshOutputDevice(defaultDeviceId));
}
