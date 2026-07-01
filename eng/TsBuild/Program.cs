using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace TsBuild;

/// <summary>
/// リポジトリのビルド補助コマンド群。just / CI から呼ばれ、常にリポジトリルートを
/// 作業ディレクトリとして実行される前提。以前 justfile にあった PowerShell の
/// ファイル操作・パッケージング処理を、テスト可能な実コードとして置き換えたもの。
/// </summary>
internal static class Program
{
    /// <summary>WinUI アプリの出荷 TFM(TypingSound.App.csproj と一致させる)。</summary>
    private const string AppTfm = "net10.0-windows10.0.26100.0";

    /// <summary>ランチャーの TFM(TypingSound.Launcher.csproj と一致させる)。</summary>
    private const string LauncherTfm = "net10.0";

    /// <summary>配布フォルダ名(dist/&lt;DistName&gt; と zip 内のルートフォルダ)。</summary>
    private const string DistName = "TypingSound";

    /// <summary>
    /// 自前でビルドした PE(バンドル相対パス)。Authenticode 署名の対象はこれだけで、
    /// 同梱のランタイム DLL は Microsoft 署名済みなので触らない。署名対象の単一の真実の源。
    /// </summary>
    private static readonly string[] FirstPartyBinaries =
    [
        "TypingSound.exe",
        Path.Combine("app", "TypingSound.App.exe"),
    ];

    private static int Main(string[] args)
    {
        try
        {
            return args switch
            {
                ["assemble", var arch] => Assemble(arch),
                ["pack", var tag, var arch] => Pack(tag, arch),
                ["clean"] => Clean(),
                ["kill"] => Kill(),
                ["run", var arch] => RunApp(arch),
                ["sign-stage", var bundle, var stage] => SignStage(bundle, stage),
                ["sign-unstage", var signed, var bundle] => SignUnstage(signed, bundle),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"tsbuild: {ex.Message}");
            return 1;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            """
            usage: tsbuild <command>
              assemble <arch>                dist/TypingSound を publish 出力から組み立てる
              pack <tag> <arch>              dist を build/package/<zip> + SHA256SUMS.txt に固める
              clean                          起動中アプリを停止し dist/ と build/ を削除
              kill                           起動中の TypingSound.App を停止
              run <arch>                     Debug ビルド済みの App を起動
              sign-stage <bundle> <stage>    自前 PE を署名用の平坦ディレクトリへ集める
              sign-unstage <signed> <bundle> 署名済み PE をバンドルへ戻す
            """);
        return 2;
    }

    /// <summary>publish 出力から dist/TypingSound の配布レイアウトを組み立てる。</summary>
    private static int Assemble(string arch)
    {
        string appPublish = Path.Combine("TypingSound.App", "bin", "Release", AppTfm, $"win-{arch}", "publish");
        string launcherExe = Path.Combine("TypingSound.Launcher", "bin", "Release", LauncherTfm, $"win-{arch}", "publish", "TypingSound.exe");

        if (!Directory.Exists(appPublish))
        {
            throw new DirectoryNotFoundException($"app publish output not found: {appPublish} (run `just publish {arch}` first)");
        }

        if (!File.Exists(launcherExe))
        {
            throw new FileNotFoundException($"launcher not found: {launcherExe}");
        }

        string dist = Path.Combine("dist", DistName);
        if (Directory.Exists(dist))
        {
            Directory.Delete(dist, recursive: true);
        }

        string appDir = Path.Combine(dist, "app");
        CopyDirectory(appPublish, appDir);
        File.Copy(launcherExe, Path.Combine(dist, "TypingSound.exe"));
        File.WriteAllText(
            Path.Combine(dist, "README.txt"),
            "Double-click TypingSound.exe to start. The full app (runtime included) lives in app\\. " +
            "Move or copy the whole folder to run it anywhere.\n");

        int count = Directory.EnumerateFiles(appDir, "*", SearchOption.AllDirectories).Count();
        Console.WriteLine($"assembled dist/{DistName} (entry TypingSound.exe + README.txt + app/ [{count} files])");
        return 0;
    }

    /// <summary>dist/TypingSound を zip 化し、SHA256SUMS.txt を書き出す。</summary>
    private static int Pack(string tag, string arch)
    {
        string dist = Path.Combine("dist", DistName);
        if (!Directory.Exists(dist))
        {
            throw new DirectoryNotFoundException($"dist/{DistName} not found; run `just dist {arch}` first");
        }

        string outDir = Path.Combine("build", "package");
        Directory.CreateDirectory(outDir);

        string zipName = $"TypingSound-{tag}-win-{arch}.zip";
        string zipPath = Path.Combine(outDir, zipName);
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        // includeBaseDirectory: 展開すると TypingSound/ フォルダになる(README の "whole folder" と一致)。
        ZipFile.CreateFromDirectory(dist, zipPath, CompressionLevel.Optimal, includeBaseDirectory: true);

        string hash = Sha256Hex(zipPath);
        File.WriteAllText(Path.Combine(outDir, "SHA256SUMS.txt"), $"{hash}  {zipName}\n");

        Console.WriteLine($"packaged build/package/{zipName} (sha256 {hash})");
        return 0;
    }

    /// <summary>起動中アプリを停止し、生成物ディレクトリ(dist/・build/)を削除する。</summary>
    private static int Clean()
    {
        Kill();
        foreach (string dir in (string[])["dist", "build"])
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
                Console.WriteLine($"removed {dir}/");
            }
        }

        return 0;
    }

    /// <summary>起動中の TypingSound.App プロセスを停止する。</summary>
    private static int Kill()
    {
        int killed = 0;
        foreach (Process process in Process.GetProcessesByName("TypingSound.App"))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
                killed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"could not stop pid {process.Id}: {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        Console.WriteLine(killed > 0 ? $"stopped {killed} TypingSound.App process(es)" : "no running TypingSound.App");
        return 0;
    }

    /// <summary>Debug ビルド済みの App を起動する。</summary>
    private static int RunApp(string arch)
    {
        string exe = Path.Combine("TypingSound.App", "bin", arch, "Debug", AppTfm, $"win-{arch}", "TypingSound.App.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException($"app not built: {exe} (run `just build` first)");
        }

        using Process? _ = Process.Start(new ProcessStartInfo(Path.GetFullPath(exe)) { UseShellExecute = true });
        Console.WriteLine($"launched {exe}");
        return 0;
    }

    /// <summary>自前 PE を署名用の平坦ディレクトリへ集める(basename は衝突しない)。</summary>
    private static int SignStage(string bundleDir, string stageDir)
    {
        Directory.CreateDirectory(stageDir);
        foreach (string relative in FirstPartyBinaries)
        {
            string source = Path.Combine(bundleDir, relative);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"first-party binary missing: {source}");
            }

            File.Copy(source, Path.Combine(stageDir, Path.GetFileName(relative)), overwrite: true);
        }

        Console.WriteLine($"staged {FirstPartyBinaries.Length} binaries into {stageDir}");
        return 0;
    }

    /// <summary>署名済み PE をバンドルの元の場所へ戻す。</summary>
    private static int SignUnstage(string signedDir, string bundleDir)
    {
        foreach (string relative in FirstPartyBinaries)
        {
            string source = Path.Combine(signedDir, Path.GetFileName(relative));
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"signed binary missing: {source}");
            }

            File.Copy(source, Path.Combine(bundleDir, relative), overwrite: true);
        }

        Console.WriteLine($"restored {FirstPartyBinaries.Length} signed binaries into {bundleDir}");
        return 0;
    }

    private static string Sha256Hex(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }
}
