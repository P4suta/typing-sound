using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TypingSoundLauncher;

/// <summary>
/// 配布レイアウトの入口。隣の <c>app\</c> サブフォルダにある本体(WinUI アプリ)を起動して即終了する。
/// ランタイム同梱で多数になる本体一式を <c>app\</c> に押し込み、利用者には起動 exe だけを見せるためのドア。
/// 本体側の機能(トレイ/フック/音)は一切持たない。
/// </summary>
internal static class Program
{
    private const uint MbIconError = 0x00000010;

    private static int Main()
    {
        string appDir = Path.Combine(AppContext.BaseDirectory, "app");
        string target = Path.Combine(appDir, "TypingSound.App.exe");

        // 起動失敗は例外フィルタ内でダイアログ表示し true を返して握りつぶす(境界で CA1031 を正規に満たす)。
        // 入口 exe は無言で死ぬと原因が分からないため、失敗時は必ず可視化する。
        static bool ShowAndSwallow(Exception ex)
        {
            _ = MessageBoxW(0, $"本体を起動できませんでした。\n{ex.Message}", "TypingSound", MbIconError);
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
