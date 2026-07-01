using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TypingSoundLauncher;

/// <summary>
/// Entry point of the distribution layout. Launches the main app (WinUI) in the adjacent <c>app\</c> subfolder and exits immediately.
/// Pushes the many self-contained runtime files of the main app into <c>app\</c> so users see only the launcher exe.
/// Holds none of the main app's features (tray/hook/sound).
/// </summary>
internal static class Program
{
    private const uint MbIconError = 0x00000010;

    private static int Main()
    {
        string appDir = Path.Combine(AppContext.BaseDirectory, "app");
        string target = Path.Combine(appDir, "TypingSound.App.exe");

        // On launch failure, show a dialog in the exception filter and return true to swallow (satisfies CA1031 at the boundary).
        // The launcher exe dying silently gives no clue why, so always surface failures.
        static bool ShowAndSwallow(Exception ex)
        {
            _ = MessageBoxW(0, $"Could not start the application.\n{ex.Message}", "TypingSound", MbIconError);
            return true;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = target,
                WorkingDirectory = appDir,
                UseShellExecute = false,
            };
            using Process? process = Process.Start(startInfo);
            return process is null ? 1 : 0;
        }
        catch (Exception ex) when (ShowAndSwallow(ex))
        {
            return 1;
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);
}
