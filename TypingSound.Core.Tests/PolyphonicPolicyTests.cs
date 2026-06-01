using TypingSound.Core.Playback;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class PolyphonicPolicyTests
{
    [Fact]
    public void PlaysEachClipWithoutStoppingPrevious()
    {
        FakeAudioEngine engine = new();
        PolyphonicPolicy sut = new(engine);

        sut.Play(new FakeClip("a"));
        sut.Play(new FakeClip("b"));

        Assert.Equal(2, engine.Played.Count);
        Assert.False(engine.Played[0].Halted);
        Assert.False(engine.Played[1].Halted);
    }

    [Fact]
    public void NullEngineThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new PolyphonicPolicy(null!));
    }
}
