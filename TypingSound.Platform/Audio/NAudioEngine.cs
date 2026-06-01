using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// NAudio(WASAPI 共有モード)による <see cref="IAudioEngine"/> 実装。
/// デバイスを 1 つだけ開きっぱなしにし、押下ごとにミキサーへ入力を足す(低遅延・重ね再生)。
/// 共有モードはリサンプルしないため、ミキサーの内部形式は <see cref="OutputFormat"/>(起動時の既定デバイスの
/// ミックス形式)に固定し、クリップ側をその形式へ変換して読み込む(<see cref="AssetSoundBank"/>)。
/// 既定の再生デバイスが切り替わった(イヤホン接続等)場合は、ミキサーは保ったまま <see cref="WasapiOut"/> だけを
/// 新デバイスへ作り直して追従する。新デバイスのミックス形式が固定形式と異なる場合は WasapiOut が内部でリサンプルする。
/// </summary>
public sealed partial class NAudioEngine : IAudioEngine, IDisposable
{
    private readonly ILogger _logger;
    private readonly MMDeviceEnumerator _enumerator;
    private readonly DeviceChangeNotificationClient _notificationClient;
    private readonly MixingSampleProvider _mixer;

    // _output / _currentDevice / _currentDeviceId / _disposed はこのロックで直列化する。
    // デバイス変更ハンドラ(別スレッド)と Dispose の競合を防ぐ。Play は _mixer(スレッドセーフ)しか触らずロック不要。
    private readonly object _outputLock = new();
    private WasapiOut? _output;
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
            mixFormat = device.AudioClient.MixFormat;
        }

        OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(mixFormat.SampleRate, mixFormat.Channels);
        _mixer = new MixingSampleProvider(OutputFormat) { ReadFully = true };

        // 起動時はデバイスが無ければ素直に失敗させる(従来挙動)。デバイス変更時の作り直しのみ握りつぶす。
        lock (_outputLock)
        {
            StartOutputOnDefaultDevice();
        }

        _notificationClient = new DeviceChangeNotificationClient(this);
        _enumerator.RegisterEndpointNotificationCallback(_notificationClient);

        LogAudioOutput(_logger, OutputFormat.SampleRate, OutputFormat.Channels, OutputFormat.Encoding);
    }

    /// <summary>出力ミキサーの形式(クリップはこの形式へ変換して読み込む)。デバイスが変わっても不変。</summary>
    public WaveFormat OutputFormat { get; }

    /// <inheritdoc/>
    public IPlayingSound Play(ISoundClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (clip is not CachedSound cached)
        {
            throw new ArgumentException($"{nameof(NAudioEngine)} は {nameof(CachedSound)} のみ再生できます。", nameof(clip));
        }

        // _mixer は AddMixerInput/Read とも内部ロックを持つためスレッドセーフ。出力の作り直し中でも安全に積める。
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

            // 先に通知を止める。以降に走り出すデバイス変更タスクはロック取得後 _disposed を見て即抜ける。
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

    // ロック内で呼ぶこと。既定の再生デバイス(Multimedia)へ WasapiOut を新規生成して再生開始する。
    private void StartOutputOnDefaultDevice()
    {
        MMDevice device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        WasapiOut output = new(device, AudioClientShareMode.Shared, useEventSync: true, latency: 60);
        output.Init(_mixer);
        output.Play();

        _output = output;
        _currentDevice = device;
        _currentDeviceId = device.ID;
    }

    // ロック内で呼ぶこと。デバイス消失時(イヤホン抜去等)は停止/破棄が例外を投げうるので握りつぶす。
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
            // 例外はフィルタ内でログ済み。作り直しを止めないため握りつぶす。
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
            _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        }
        catch (Exception ex) when (LogAndSwallow(_logger, ex))
        {
            // 解除失敗は致命ではない。ログして続行する。
        }
    }

    // 既定デバイス変更通知から別スレッドで呼ばれる(コールバック内からデバイス API を呼ぶとデッドロックしうるため)。
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

            // 同一デバイスへの変更通知(複数ロール由来の重複イベント等)は無視する。
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
                // 利用可能なデバイスが無い等。次の変更通知で再度作り直しを試みる。
            }
        }
    }

    // 通知コールバックから呼ばれる。コールバックを即座に返すため、実際の作り直しは別スレッドへ逃がす。
    private void OnDefaultRenderDeviceChanged(string? defaultDeviceId) =>
        _ = Task.Run(() => RefreshOutputDevice(defaultDeviceId));

    /// <summary>
    /// 既定の再生デバイス変更を監視し、Render/Multimedia の既定が変わったらエンジンへ作り直しを依頼する。
    /// COM コールバック内ではデバイス API を呼ばず、エンジン側で別スレッドへ逃がす(デッドロック回避)。
    /// </summary>
    private sealed class DeviceChangeNotificationClient(NAudioEngine engine) : IMMNotificationClient
    {
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                engine.OnDefaultRenderDeviceChanged(defaultDeviceId);
            }
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            // 関心なし。
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            // 関心なし。
        }

        public void OnDeviceRemoved(string deviceId)
        {
            // 関心なし。
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // 関心なし。
        }
    }
}
