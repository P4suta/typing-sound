using Microsoft.Extensions.Logging;
using NAudio.Wave;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// <see cref="ISoundBank"/> that loads every WAV in a directory fully into memory.
/// Clip Id is the file name without extension. Files are converted to the output device format on load.
/// A single file's load failure is non-fatal: it is skipped and only the files that loaded are returned.
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
    /// Loads *.wav from the directory into a bank. Failed files are logged and skipped.
    /// </summary>
    /// <param name="directory">Directory containing the WAV files.</param>
    /// <param name="targetFormat">Target output device format to convert to.</param>
    /// <param name="logger">Logger for load failures and counts.</param>
    public static AssetSoundBank LoadFromDirectory(string directory, WaveFormat targetFormat, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(targetFormat);
        ArgumentNullException.ThrowIfNull(logger);

        List<ISoundClip> clips = [];

        // Log in the exception filter and return true so one file's failure doesn't stop the rest (CA1031).
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
