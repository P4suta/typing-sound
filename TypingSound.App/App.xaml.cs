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
/// Application composition root. Binds Platform implementations (hook/NAudio/sound loading)
/// to the Core abstractions and provides a tray-resident UI. Windowless, tray-only app.
/// </summary>
public sealed partial class App : Application, IDisposable
{
    // Unique mutex name for single-instance enforcement. A fixed GUID avoids collisions with
    // other apps. Unprefixed (Local) scopes it per logon session; use "Global\\" for one per machine.
    private const string SingleInstanceMutexName = "TypingSound-7B3F1E9C-2A4D-4C8E-9F10-SingleInstance";

    // Id of the clip used as the return bell. Every other wav in Assets/Sounds is a typing-sound clip.
    private const string ReturnBellId = "bell";

    private static Mutex? _singleInstanceMutex;

    private AppDiagnostics? _diagnostics;
    private ILogger? _log;
    private NAudioEngine? _audio;
    private LowLevelKeyboardHook? _hook;
    private TypingSoundEngine? _engine;
    private TaskbarIcon? _tray;
    private bool _loggedFirstKey;

    /// <summary>Initializes the singleton application object.</summary>
    public App() => InitializeComponent();

    /// <inheritdoc/>
    public void Dispose()
    {
        _hook?.Dispose();
        _tray?.Dispose();
        _engine?.Dispose();
        _audio?.Dispose();
        _singleInstanceMutex?.Dispose();

        // Dispose diagnostics last: Platform Dispose paths (e.g. hook-removal logging) still
        // write to the logger, so the sink must not close before them.
        _diagnostics?.Dispose();
    }

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Enforce a single instance: a second launch would install a second hook and double the sound.
        // If a prior instance already holds the mutex, isNew is false and the second one exits immediately.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isNew);
        if (!isNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Environment.Exit(0);
            return;
        }

        // --- Bring up diagnostics first and register global exception handlers before anything else ---
        // so exceptions thrown by later initialization (audio/hook/tray) are still caught and logged.
        _diagnostics = new AppDiagnostics();
        ILogger log = _diagnostics.LoggerFactory.CreateLogger("App");
        _log = log;

        UnhandledException += OnAppUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

        // --- Composition root: bind Platform implementations to Core abstractions ---
        _audio = new NAudioEngine(_diagnostics.LoggerFactory.CreateLogger("Audio"));

        string soundsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
        AssetSoundBank bank = AssetSoundBank.LoadFromDirectory(
            soundsDirectory,
            _audio.OutputFormat,
            _diagnostics.LoggerFactory.CreateLogger("Sounds"));

        // Every wav in Assets/Sounds except "bell" forms the typing-sound pool. Adding wavs to the
        // Assets/Sounds folder next to the exe grows the pool, which ShuffleQueueSelector cycles through
        // without repeats. Configuration stays portable.
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

        // Startup health log: version/OS/output format/clip count/hook state/initial mode on one line.
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
        // Fatal: log and notify the user. Do not casually set e.Handled = true; let it crash the default way.
        if (_log is { } log)
        {
            LogUnhandledUiException(log, e.Exception);
        }

        NotifyError("An unexpected error occurred. Check the logs.");
    }

    private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        // Unhandled exception via the AppDomain. The process normally terminates, so at least ensure it is logged.
        if (_log is { } log)
        {
            LogUnhandledDomainException(log, (Exception)e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Unobserved Task exception. SetObserved() prevents process termination and lets the app degrade gracefully.
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
        // Only one mode (Typewriter: typing sound plus a return bell on Enter), so the tray shows no
        // mode picker. The switching mechanism itself remains in Core (TypingSoundEngine.SwitchTo / ISoundMode).
        MenuFlyout menu = new();

        MenuFlyoutItem openLogsItem = new()
        {
            Text = "Open log folder",
            Command = new RelayCommand(OpenLogFolder),
        };
        menu.Items.Add(openLogsItem);

        MenuFlyoutItem exitItem = new()
        {
            Text = "Exit",
            Command = new RelayCommand(ExitApp),
        };
        menu.Items.Add(exitItem);

        TaskbarIcon icon = new()
        {
            ToolTipText = "TypingSound",

            // The tray icon loads the same AppIcon.ico embedded in the exe. H.NotifyIcon's IconSource
            // is an ImageSource, so pass a BitmapImage.
            IconSource = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"))),
            ContextFlyout = menu,

            // Windowless tray-only app: show the context menu via a native Win32 popup that needs no
            // XamlRoot. The default mode requires a XamlRoot, and without a window the menu would not respond.
            ContextMenuMode = ContextMenuMode.PopupMenu,
        };
        icon.ForceCreate();
        return icon;
    }

    private void OpenLogFolder()
    {
        // Open the log folder with explorer.exe. A launch failure must not affect the app.
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
        }
    }

    private void NotifyError(string message)
    {
        // A tray-notification failure (tray not created, wrong thread, etc.) must not affect the app.
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
        }
    }

    private void ExitApp()
    {
        Dispose();
        Exit();
    }
}
