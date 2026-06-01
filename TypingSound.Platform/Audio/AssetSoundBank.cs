using Microsoft.Extensions.Logging;
using NAudio.Wave;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// ディレクトリ内の WAV をすべてメモリへ常駐ロードする <see cref="ISoundBank"/>。
/// クリップ Id はファイル名(拡張子なし)。読み込みは出力デバイス形式へ変換する。
/// 個々のファイルの読み込み失敗は致命ではなく、スキップして読み込めた分だけ返す。
/// </summary>
public sealed partial class AssetSoundBank : ISoundBank
{
    private readonly Dictionary<string, ISoundClip> _byId;

    private AssetSoundBank(IReadOnlyList<ISoundClip> clips)
    {
        Clips = clips;
        _byId = new Dictionary<string, ISoundClip>(StringComparer.OrdinalIgnoreCase);
        foreach (ISoundClip clip in clips)
        {
            _byId[clip.Id] = clip;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISoundClip> Clips { get; }

    /// <summary>
    /// ディレクトリ内の *.wav を読み込んでバンクを作る。失敗したファイルはログに記録してスキップする。
    /// </summary>
    /// <param name="directory">WAV を含むディレクトリ。</param>
    /// <param name="targetFormat">変換先の出力デバイス形式。</param>
    /// <param name="logger">読み込み失敗・件数を記録するロガー。</param>
    public static AssetSoundBank LoadFromDirectory(string directory, WaveFormat targetFormat, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(targetFormat);
        ArgumentNullException.ThrowIfNull(logger);

        List<ISoundClip> clips = [];

        // 例外フィルタ内でログし true を返すことで、1 ファイルの失敗で全体を止めない(劣化継続)。
        // CA1031 をフィルタ付き catch で正規に満たす。
        static bool LogAndSwallow(ILogger logger, string file, Exception ex)
        {
            LogLoadFailed(logger, ex, file);
            return true;
        }

        if (Directory.Exists(directory))
        {
            foreach (string path in Directory.EnumerateFiles(directory, "*.wav").OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
            {
                string id = Path.GetFileNameWithoutExtension(path);
                try
                {
                    clips.Add(CachedSound.Load(path, id, targetFormat));
                }
                catch (Exception ex) when (LogAndSwallow(logger, Path.GetFileName(path), ex))
                {
                    // 例外はフィルタ内でログ済み。読めたファイルだけで続行する。
                }
            }
        }

        LogClipsLoaded(logger, clips.Count);
        return new AssetSoundBank(clips);
    }

    /// <inheritdoc/>
    public ISoundClip? FindById(string id) => _byId.GetValueOrDefault(id);

    [LoggerMessage(Level = LogLevel.Error, Message = "failed to load sound {File}")]
    private static partial void LogLoadFailed(ILogger logger, Exception ex, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "loaded {Count} sound clips")]
    private static partial void LogClipsLoaded(ILogger logger, int count);
}
