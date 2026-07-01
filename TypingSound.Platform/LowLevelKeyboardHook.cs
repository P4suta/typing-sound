using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TypingSound.Core.Abstractions;
using TypingSound.Platform.Interop;

namespace TypingSound.Platform;

/// <summary>
/// <see cref="IKeyEventSource"/> implementation using a global low-level keyboard hook (WH_KEYBOARD_LL).
/// Notifies key presses as a "trigger signal" only; <b>the key value is never retained, recorded or sent</b>.
///
/// Constraint: the callback runs on the thread that installed the hook, and if it takes more than ~300ms Windows
/// silently removes the hook. So the callback only invokes subscribers and returns immediately.
/// A subscriber exception crossing the native boundary breaks the hook, so it must always be swallowed here.
/// Call <see cref="Start"/> on the UI thread (which has a message pump).
/// The hook handle is reliably released via <see cref="KeyboardHookHandle"/> (a SafeHandle).
/// </summary>
public sealed partial class LowLevelKeyboardHook : IKeyEventSource
{
    private readonly NativeMethods.HookProc _proc;
    private readonly ILogger _logger;
    private KeyboardHookHandle? _hook;

    public LowLevelKeyboardHook(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _proc = HookCallback;
    }

    /// <inheritdoc/>
    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <inheritdoc/>
    public void Start()
    {
        if (_hook is { IsInvalid: false })
        {
            return;
        }

        _hook = NativeMethods.SetWindowsHookExW(NativeMethods.WhKeyboardLl, _proc, NativeMethods.GetModuleHandleW(null), 0);
        if (_hook.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install the low-level keyboard hook.");
        }

        LogHookInstalled(_logger);
    }

    /// <inheritdoc/>
    public void StopListening()
    {
        _hook?.Dispose();
        _hook = null;
        LogHookRemoved(_logger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "keyboard hook installed")]
    private static partial void LogHookInstalled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "keyboard hook removed")]
    private static partial void LogHookRemoved(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "keyboard hook handler threw and was swallowed")]
    private static partial void LogHandlerThrew(ILogger logger, Exception ex);

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        // Log in the exception filter and return true so no exception ever escapes the hook callback
        // (which would make Windows remove the hook). Satisfies CA1031 via a filtered catch.
        static bool LogAndSwallow(ILogger logger, Exception ex)
        {
            LogHandlerThrew(logger, ex);
            return true;
        }

        if (nCode >= 0)
        {
            int message = (int)wParam;
            if (message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown)
            {
                // Read the first field of KBDLLHOOKSTRUCT (vkCode) and classify only as Enter vs Other.
                // After classifying, vkCode is discarded; the specific key value is never retained, recorded or sent (privacy).
                int virtualKey = Marshal.ReadInt32(lParam);
                KeyPressedEventArgs args = virtualKey == NativeMethods.VkReturn
                    ? KeyPressedEventArgs.Enter
                    : KeyPressedEventArgs.Other;

                try
                {
                    KeyPressed?.Invoke(this, args);
                }
                catch (Exception ex) when (LogAndSwallow(_logger, ex))
                {
                }
            }
        }

        // For low-level hooks the hhk argument of CallNextHookEx is ignored, so Zero is fine.
        return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);
    }
}
