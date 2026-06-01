using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TypingSound.Core.Abstractions;
using TypingSound.Platform.Interop;

namespace TypingSound.Platform;

/// <summary>
/// グローバルな低レベルキーボードフック(WH_KEYBOARD_LL)による <see cref="IKeyEventSource"/> 実装。
/// 押下を「トリガ信号」として通知するだけで、<b>キーの値は一切保持・記録・送信しない</b>。
///
/// 制約: コールバックはインストールしたスレッド上で動き、処理が約300ms を超えると Windows が
/// フックを黙って外す。よってコールバックは購読者を即時に呼んで return するだけにする。
/// さらに購読者の例外がネイティブ境界を越えるとフックが壊れるため、ここで必ず握りつぶす。
/// UI スレッド(メッセージポンプあり)で <see cref="Start"/> すること。
/// フックハンドルは <see cref="KeyboardHookHandle"/>(SafeHandle)で確実に解放する。
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
            throw new Win32Exception(Marshal.GetLastWin32Error(), "低レベルキーボードフックのインストールに失敗しました。");
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
        // 例外フィルタ内でログし true を返すことで、フックコールバックから例外を絶対に外へ出さない
        // (出すと Windows がフックを除去する)。CA1031 をフィルタ付き catch で正規に満たす。
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
                // KBDLLHOOKSTRUCT の先頭フィールド vkCode を読み、Enter かどうかだけ分類する。
                // 分類後に vkCode は破棄し、キーの具体値は保持・記録・送信しない(プライバシー原則)。
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
                    // 例外はフィルタ内でログ済み。ここでは何もせず握りつぶす。
                }
            }
        }

        // 低レベルフックでは CallNextHookEx の hhk 引数は無視されるため Zero で良い。
        return NativeMethods.CallNextHookEx(nint.Zero, nCode, wParam, lParam);
    }
}
