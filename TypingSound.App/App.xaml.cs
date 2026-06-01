using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;
using TypingSound.Core;
using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Platform;
using TypingSound.Platform.Audio;
using TypingSoundApp.Diagnostics;
using TypingSoundApp.Timing;

namespace TypingSoundApp;

/// <summary>
/// アプリの合成ルート。Platform 実装(フック/NAudio/音源ロード)を Core の抽象へ束ね、
/// トレイ常駐 UI(モード切替・終了)を提供する。ウィンドウは持たないトレイ専用アプリ。
/// </summary>
public sealed partial class App : Application, IDisposable
{
    // 単一インスタンス用の一意な mutex 名(他アプリと衝突しないよう固定 GUID を含める)。
    // 接頭辞なし(Local)はログオンセッション単位。マシン全体で 1 つに限るなら "Global\\" を付ける。
    private const string SingleInstanceMutexName = "TypingSound-7B3F1E9C-2A4D-4C8E-9F10-SingleInstance";

    // 復帰ベルとして扱うクリップの Id。これ以外の Assets/Sounds 内の全 wav が打鍵音プールになる。
    private const string ReturnBellId = "bell";

    private static Mutex? _singleInstanceMutex;

    private AppDiagnostics? _diagnostics;
    private ILogger? _log;
    private NAudioEngine? _audio;
    private LowLevelKeyboardHook? _hook;
    private TypingSoundEngine? _engine;
    private TaskbarIcon? _tray;
    private bool _loggedFirstKey;

    /// <summary>シングルトン アプリケーション オブジェクトを初期化する。</summary>
    public App() => InitializeComponent();

    /// <inheritdoc/>
    public void Dispose()
    {
        _hook?.Dispose();
        _tray?.Dispose();
        _engine?.Dispose();
        _audio?.Dispose();
        _singleInstanceMutex?.Dispose();

        // 診断基盤は最後に破棄する。Platform の Dispose 経路(例: フック除去ログ)が
        // まだロガーへ書くため、それより先にシンクを閉じない。
        _diagnostics?.Dispose();
    }

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 単一インスタンスを保証: 二重起動 = フック二重 = 音が二重になるのを防ぐ。
        // 先行インスタンスが mutex を保持していれば isNew=false となり、2 つ目は即終了する。
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isNew);
        if (!isNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Environment.Exit(0);
            return;
        }

        // --- 診断/可観測性基盤を最優先で起動し、グローバル例外ハンドラを最初に登録する ---
        // これ以降の初期化(audio/hook/tray)で投げられた例外も確実に捕捉・記録できるようにする。
        _diagnostics = new AppDiagnostics();
        ILogger log = _diagnostics.LoggerFactory.CreateLogger("App");
        _log = log;

        UnhandledException += OnAppUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

        // --- 合成ルート: Platform 実装を Core 抽象へ束ねる ---
        _audio = new NAudioEngine(_diagnostics.LoggerFactory.CreateLogger("Audio"));

        string soundsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
        AssetSoundBank bank = AssetSoundBank.LoadFromDirectory(
            soundsDirectory,
            _audio.OutputFormat,
            _diagnostics.LoggerFactory.CreateLogger("Sounds"));

        // Assets/Sounds 内の "bell" 以外の全 wav を打鍵音プールにする。現状の同梱は key2.wav のみなので
        // 打鍵音は key2 に確定する(聴き比べの末に採用)。exe 隣の Assets/Sounds に wav を足せばプールが増え、
        // ShuffleQueueSelector が重複なしで巡回する。ポータブルのまま構成を変えられる。
        IReadOnlyList<ISoundClip> typingPool = [.. bank.Clips.Where(clip => clip.Id != ReturnBellId)];
        SoundCatalog catalog = new(typingPool, bank.FindById(ReturnBellId));
        IReadOnlyList<ISoundMode> modes = DefaultModeSet.Create(catalog);

        SoundModeContext context = new(
            _audio,
            new DispatcherQueueTimerFactory(dispatcher, _diagnostics.LoggerFactory.CreateLogger("Timer")),
            new CryptoRandomSource());
        _engine = new TypingSoundEngine(context, modes[0]);

        _hook = new LowLevelKeyboardHook(_diagnostics.LoggerFactory.CreateLogger("Hook"));
        _hook.KeyPressed += OnKeyPressed;
        _hook.Start();

        _tray = CreateTrayIcon();

        // 起動ヘルスログ: バージョン/OS/出力形式/クリップ件数/フック状態/初期モードを 1 行で記録する。
        Version version = typeof(App).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);
        LogStartupComplete(
            log,
            version,
            Environment.OSVersion.VersionString,
            _audio.OutputFormat.SampleRate,
            _audio.OutputFormat.Channels,
            _audio.OutputFormat.Encoding,
            bank.Clips.Count,
            true,
            _engine.CurrentMode.Id);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "startup complete: version={Version} os={Os} audio={Rate}Hz/{Channels}ch/{Encoding} clips={Clips} hookInstalled={Hook} mode={Mode}")]
    private static partial void LogStartupComplete(
        ILogger logger,
        Version version,
        string os,
        int rate,
        int channels,
        WaveFormatEncoding encoding,
        int clips,
        bool hook,
        string mode);

    [LoggerMessage(Level = LogLevel.Critical, Message = "unhandled exception on UI thread")]
    private static partial void LogUnhandledUiException(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Critical, Message = "unhandled exception in AppDomain")]
    private static partial void LogUnhandledDomainException(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "unobserved task exception")]
    private static partial void LogUnobservedTaskException(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "first key press received by hook")]
    private static partial void LogFirstKeyPress(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "failed to open log folder")]
    private static partial void LogOpenLogFolderFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "failed to show error notification")]
    private static partial void LogNotificationFailed(ILogger logger, Exception ex);

    private void OnAppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // 致命的: ログを残し、利用者へ通知する。e.Handled は安易に true にせず、既定の経路で落とす。
        if (_log is { } log)
        {
            LogUnhandledUiException(log, e.Exception);
        }

        NotifyError("予期しないエラーが発生しました。ログを確認してください。");
    }

    private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        // AppDomain 経由の未処理例外。プロセスは原則終了するため、確実にログだけは残す。
        if (_log is { } log)
        {
            LogUnhandledDomainException(log, (Exception)e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // 観測されなかった Task 例外。SetObserved() でプロセス終了を防ぎ、劣化継続する。
        if (_log is { } log)
        {
            LogUnobservedTaskException(log, e.Exception);
        }

        e.SetObserved();
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        if (!_loggedFirstKey)
        {
            _loggedFirstKey = true;
            if (_log is { } log)
            {
                LogFirstKeyPress(log);
            }
        }

        _engine?.NotifyKeyPressed(e.Category);
    }

    private TaskbarIcon CreateTrayIcon()
    {
        // モードは Typewriter 一本(打鍵音 key2 ＋ Enter で復帰ベル)なので、トレイにモード選択は出さない。
        // 切替の仕組み自体は Core 側(TypingSoundEngine.SwitchTo / ISoundMode)に温存している。
        MenuFlyout menu = new();

        MenuFlyoutItem openLogsItem = new()
        {
            Text = "ログフォルダを開く",
            Command = new RelayCommand(OpenLogFolder),
        };
        menu.Items.Add(openLogsItem);

        MenuFlyoutItem exitItem = new()
        {
            Text = "終了",
            Command = new RelayCommand(ExitApp),
        };
        menu.Items.Add(exitItem);

        TaskbarIcon icon = new()
        {
            ToolTipText = "TypingSound",

            // トレイアイコンは exe に埋め込んだものと同じ AppIcon.ico(白タイル＋黒タイプライター)を読み込む。
            // H.NotifyIcon の IconSource は ImageSource 型なので BitmapImage を渡す。
            IconSource = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"))),
            ContextFlyout = menu,

            // ウィンドウを持たないトレイ専用アプリのため、XamlRoot 不要のネイティブ Win32 ポップアップで
            // コンテキストメニューを表示する(既定モードは XamlRoot を要し、窓無しだとメニューが反応しない)。
            ContextMenuMode = ContextMenuMode.PopupMenu,
        };
        icon.ForceCreate();
        return icon;
    }

    private void OpenLogFolder()
    {
        // explorer.exe でログフォルダを開く。起動失敗はアプリ本体に影響させない(フィルタ＋swallow)。
        bool LogAndSwallow(Exception ex)
        {
            if (_log is { } log)
            {
                LogOpenLogFolderFailed(log, ex);
            }

            return true;
        }

        if (_diagnostics is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", _diagnostics.LogDirectory) { UseShellExecute = true });
        }
        catch (Exception ex) when (LogAndSwallow(ex))
        {
            // 例外はフィルタ内でログ済み。ここでは握りつぶす。
        }
    }

    private void NotifyError(string message)
    {
        // トレイ通知の失敗(トレイ未生成・別スレッド等)はアプリ本体に影響させない(フィルタ＋swallow)。
        bool LogAndSwallow(Exception ex)
        {
            if (_log is { } log)
            {
                LogNotificationFailed(log, ex);
            }

            return true;
        }

        TaskbarIcon? tray = _tray;
        if (tray is null)
        {
            return;
        }

        try
        {
            tray.ShowNotification("TypingSound", message, NotificationIcon.Error);
        }
        catch (Exception ex) when (LogAndSwallow(ex))
        {
            // 例外はフィルタ内でログ済み。ここでは握りつぶす。
        }
    }

    private void ExitApp()
    {
        Dispose();
        Exit();
    }
}
