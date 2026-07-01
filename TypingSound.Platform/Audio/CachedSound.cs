using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// A clip decoded into memory and converted to the output device format.
/// On key press the buffer is just fed to the mixer (no per-press decode/open).
/// Loaded via <see cref="MediaFoundationReader"/> so 24-bit, WAVE_FORMAT_EXTENSIBLE and compressed WAV work
/// (the older <c>AudioFileReader</c> routes Extensible through ACM conversion and fails).
/// </summary>
internal sealed class CachedSound : ISoundClip
{
    private CachedSound(string id, float[] audioData, WaveFormat waveFormat)
    {
        Id = id;
        AudioData = audioData;
        WaveFormat = waveFormat;
    }

    /// <inheritdoc/>
    public string Id { get; }

    internal float[] AudioData { get; }

    internal WaveFormat WaveFormat { get; }

    /// <summary>Loads the WAV and converts it to <paramref name="targetFormat"/> (the output device format), held in memory.</summary>
    internal static CachedSound Load(string path, string id, WaveFormat targetFormat)
    {
        MediaFoundationApi.Startup();

        using MediaFoundationReader reader = new(path);
        ISampleProvider sampleProvider = MatchChannels(reader.ToSampleProvider(), targetFormat.Channels);
        if (sampleProvider.WaveFormat.SampleRate != targetFormat.SampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, targetFormat.SampleRate);
        }

        List<float> data = [];
        float[] buffer = new float[targetFormat.SampleRate * targetFormat.Channels];
        int read;
        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            data.AddRange(buffer.AsSpan(0, read));
        }

        return new CachedSound(id, [.. data], targetFormat);
    }

    private static ISampleProvider MatchChannels(ISampleProvider source, int targetChannels)
    {
        if (source.WaveFormat.Channels == targetChannels)
        {
            return source;
        }

        if (source.WaveFormat.Channels == 1 && targetChannels == 2)
        {
            return new MonoToStereoSampleProvider(source);
        }

        if (source.WaveFormat.Channels == 2 && targetChannels == 1)
        {
            return new StereoToMonoSampleProvider(source);
        }

        throw new NotSupportedException(
            $"Converting from {source.WaveFormat.Channels} channels to {targetChannels} is not supported.");
    }
}
