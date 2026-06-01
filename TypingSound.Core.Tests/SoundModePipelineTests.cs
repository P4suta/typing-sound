using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Tests;

public class SoundModePipelineTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void ExposesTheGivenAxesUnchanged()
    {
        EveryKeyTrigger trigger = new();
        FixedSelector selector = new(new FakeClip("a"));
        PolyphonicPolicy playback = new(new FakeAudioEngine());

        SoundModePipeline sut = new(trigger, selector, playback);

        Assert.Same(trigger, sut.Trigger);
        Assert.Same(selector, sut.Selector);
        Assert.Same(playback, sut.Playback);
    }

    [Fact]
    public void OnKeyPressedNotifiesTheTriggerAndPlaysTheSelectedClip()
    {
        FakeClip clip = new("a");
        FakeAudioEngine engine = new();
        SoundModePipeline sut = new(new EveryKeyTrigger(), new FixedSelector(clip), new PolyphonicPolicy(engine));

        sut.OnKeyPressed(KeyCategory.Other);

        Assert.Single(engine.Played);
        Assert.Same(clip, engine.Played[0].Clip);
    }

    [Fact]
    public void DisposeDisposesTheTrigger()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger trigger = new(Quiet, timers);
        SoundModePipeline sut = new(trigger, new FixedSelector(null), new PolyphonicPolicy(new FakeAudioEngine()));

        trigger.Notify(KeyCategory.Other);
        Assert.True(timers.Timer.HasPending);

        sut.Dispose();

        Assert.False(timers.Timer.HasPending);
    }

    [Fact]
    public void NullTriggerThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SoundModePipeline(null!, new FixedSelector(null), new PolyphonicPolicy(new FakeAudioEngine())));
    }

    [Fact]
    public void NullSelectorThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SoundModePipeline(new EveryKeyTrigger(), null!, new PolyphonicPolicy(new FakeAudioEngine())));
    }

    [Fact]
    public void NullPlaybackThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SoundModePipeline(new EveryKeyTrigger(), new FixedSelector(null), null!));
    }
}
