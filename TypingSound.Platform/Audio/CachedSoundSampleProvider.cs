using NAudio.Wave;

namespace TypingSound.Platform.Audio;

/// <summary>常駐済み <see cref="CachedSound"/> を 1 回再生するためのサンプルプロバイダ。</summary>
internal sealed class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _sound;
    private int _position;

    public CachedSoundSampleProvider(CachedSound sound) => _sound = sound;

    public WaveFormat WaveFormat => _sound.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int available = _sound.AudioData.Length - _position;
        int toCopy = Math.Min(available, count);
        if (toCopy > 0)
        {
            Array.Copy(_sound.AudioData, _position, buffer, offset, toCopy);
            _position += toCopy;
        }

        return toCopy;
    }
}
