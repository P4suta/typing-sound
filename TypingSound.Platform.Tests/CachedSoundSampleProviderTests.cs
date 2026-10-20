using NAudio.Wave;
using TypingSound.Platform.Audio;

namespace TypingSound.Platform.Tests;

public class CachedSoundSampleProviderTests
{
    private const int TestRate = 48_000;
    private const float Sentinel = -1;

    [Fact]
    public void ReadsMatchAQueueAcrossShortInputsAndBuffers()
    {
        for (int length = 0; length <= 8; length++)
        {
            float[] data = Enumerable.Range(0, length).Select(static value => (float)value).ToArray();
            for (int capacity = 0; capacity <= 8; capacity++)
            {
                CachedSoundSampleProvider provider = new(CreateSound(data));
                Queue<float> remaining = new(data);
                for (int attempt = 0; attempt < length + 2; attempt++)
                {
                    float[] actual = Enumerable.Repeat(Sentinel, capacity + 2).ToArray();
                    float[] expected = (float[])actual.Clone();
                    int expectedCount = 0;
                    while (expectedCount < capacity && remaining.TryDequeue(out float value))
                    {
                        expected[expectedCount + 1] = value;
                        expectedCount++;
                    }

                    int count = Read(provider, actual, 1, capacity);

                    Assert.Equal(expectedCount, count);
                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Fact]
    public void EmptyDestinationDoesNotConsumeAudio()
    {
        float[] data = [0.25f, 0.5f];
        CachedSoundSampleProvider provider = new(CreateSound(data));

        Assert.Equal(0, Read(provider, [], 0, 0));
        float[] destination = new float[data.Length];
        Assert.Equal(data.Length, Read(provider, destination, 0, destination.Length));
        Assert.Equal(data, destination);
    }

    [Fact]
    public void ProvidersHaveIndependentReadPositions()
    {
        CachedSound sound = CreateSound([0.25f, 0.5f]);
        CachedSoundSampleProvider first = new(sound);
        CachedSoundSampleProvider second = new(sound);
        float[] firstBuffer = new float[1];
        float[] secondBuffer = new float[1];

        Assert.Equal(1, Read(first, firstBuffer, 0, 1));
        Assert.Equal(1, Read(first, firstBuffer, 0, 1));
        Assert.Equal(1, Read(second, secondBuffer, 0, 1));
        Assert.Equal(0.5f, firstBuffer[0]);
        Assert.Equal(0.25f, secondBuffer[0]);
    }

    [Fact]
    public void ProviderKeepsTheCachedWaveFormat()
    {
        CachedSound sound = CreateSound([0.25f]);
        CachedSoundSampleProvider provider = new(sound);

        Assert.Same(sound.WaveFormat, provider.WaveFormat);
    }

    private static CachedSound CreateSound(float[] data) =>
        new("fixture", data, WaveFormat.CreateIeeeFloatWaveFormat(TestRate, 1));

    private static int Read(CachedSoundSampleProvider provider, float[] destination, int offset, int count) =>
        provider.Read(destination.AsSpan(offset, count));
}
