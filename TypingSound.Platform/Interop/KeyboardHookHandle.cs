using Microsoft.Win32.SafeHandles;

namespace TypingSound.Platform.Interop;

/// <summary>
/// Safe handle for the low-level keyboard hook (HHOOK). Reliably calls
/// <see cref="NativeMethods.UnhookWindowsHookEx"/> on dispose/finalize, guaranteeing release without a manual finalizer.
/// </summary>
internal sealed class KeyboardHookHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public KeyboardHookHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle() => NativeMethods.UnhookWindowsHookEx(handle);
}
