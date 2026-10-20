using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using TypingSound.Platform.Audio;

namespace TypingSound.Platform.Tests;

[Trait("Category", "AudioDevice")]
public class AudioDeviceTests
{
    [Fact]
    public void EngineStartsAndDisposesOnTheDefaultRenderDevice()
    {
        using NAudioEngine engine = new(NullLogger.Instance);

        Assert.True(engine.OutputFormat.SampleRate > 0);
        Assert.True(engine.OutputFormat.Channels > 0);
        Assert.Equal(WaveFormatEncoding.IeeeFloat, engine.OutputFormat.Encoding);

        engine.Dispose();
    }

    [Fact]
    public void BundledClipDecodesToTheRequestedFormat()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "key2.wav");
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);

        CachedSound sound = CachedSound.Load(path, "fixture", format);

        Assert.Same(format, sound.WaveFormat);
        Assert.NotEmpty(sound.AudioData);
        Assert.All(sound.AudioData, static value => Assert.True(float.IsFinite(value)));
    }
}
