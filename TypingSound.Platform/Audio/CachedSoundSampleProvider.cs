using NAudio.Wave;

namespace TypingSound.Platform.Audio;

/// <summary>Sample provider that plays an in-memory <see cref="CachedSound"/> once.</summary>
internal sealed class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _sound;
    private int _position;

    public CachedSoundSampleProvider(CachedSound sound) => _sound = sound;

    public WaveFormat WaveFormat => _sound.WaveFormat;

    public int Read(Span<float> buffer)
    {
        int available = _sound.AudioData.Length - _position;
        int toCopy = Math.Min(available, buffer.Length);
        if (toCopy > 0)
        {
            _sound.AudioData.AsSpan(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
        }

        return toCopy;
    }
}
