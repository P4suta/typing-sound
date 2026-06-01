using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>
/// WAV をメモリへ展開し、出力デバイスのフォーマットに変換して保持するクリップ。
/// 押下時はこのバッファをミキサーへ流すだけ(都度デコード/オープンしない)。
/// 読み込みは <see cref="MediaFoundationReader"/> で行うため、24bit や WAVE_FORMAT_EXTENSIBLE、
/// 圧縮 WAV も扱える(古い <c>AudioFileReader</c> は Extensible を ACM 変換に回して失敗する)。
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

    /// <summary>WAV を読み込み、<paramref name="targetFormat"/>(出力デバイス形式)へ変換して常駐させる。</summary>
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
            $"チャンネル数 {source.WaveFormat.Channels} から {targetChannels} への変換は未対応です。");
    }
}
