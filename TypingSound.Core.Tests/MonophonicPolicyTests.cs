using TypingSound.Core.Playback;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class MonophonicPolicyTests
{
    [Fact]
    public void StopsThePreviousSoundBeforePlayingTheNext()
    {
        FakeAudioEngine engine = new();
        MonophonicPolicy sut = new(engine);

        sut.Play(new FakeClip("a"));
        sut.Play(new FakeClip("b"));

        Assert.Equal(2, engine.Played.Count);
        Assert.True(engine.Played[0].Halted);
        Assert.False(engine.Played[1].Halted);
    }

    [Fact]
    public void FirstPlayStopsNothing()
    {
        FakeAudioEngine engine = new();
        MonophonicPolicy sut = new(engine);

        sut.Play(new FakeClip("a"));

        Assert.Single(engine.Played);
        Assert.False(engine.Played[0].Halted);
    }

    [Fact]
    public void StopsEachPredecessorAcrossManyPlays()
    {
        FakeAudioEngine engine = new();
        MonophonicPolicy sut = new(engine);

        for (int i = 0; i < 5; i++)
        {
            sut.Play(new FakeClip($"c{i}"));
        }

        for (int i = 0; i < 4; i++)
        {
            Assert.True(engine.Played[i].Halted, $"index {i} should be stopped");
        }

        Assert.False(engine.Played[4].Halted);
    }

    [Fact]
    public void NullEngineThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new MonophonicPolicy(null!));
    }
}
