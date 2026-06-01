using Microsoft.Win32.SafeHandles;

namespace TypingSound.Platform.Interop;

/// <summary>
/// 低レベルキーボードフック(HHOOK)の安全ハンドル。破棄/ファイナライズ時に確実に
/// <see cref="NativeMethods.UnhookWindowsHookEx"/> を呼ぶ(手動ファイナライザを書かずに解放を保証する)。
/// </summary>
internal sealed class KeyboardHookHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public KeyboardHookHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle() => NativeMethods.UnhookWindowsHookEx(handle);
}
