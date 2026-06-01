using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Tests;

public class EngineLifecycleAndDisposalTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void DisposingDebounceTriggerAfterNotifyPreventsAnyLaterFiring()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Notify(KeyCategory.Other);
        sut.Dispose();
        timers.Timer.Elapse();

        Assert.False(timers.Timer.HasPending);
        Assert.Equal(0, fired);
    }

    [Fact]
    public void PipelineDisposePropagatesToTheTrigger()
    {
        FakeTimerFactory timers = new();
        SoundModePipeline sut = new(
            new DebounceTrigger(Quiet, timers),
            new FixedSelector(null),
            new PolyphonicPolicy(new FakeAudioEngine()));

        sut.Trigger.Notify(KeyCategory.Other);
        sut.Dispose();

        Assert.False(timers.Timer.HasPending);
    }

    [Fact]
    public void NotifyKeyPressedAfterDisposeDoesNotPlay()
    {
        FakeAudioEngine audio = new();
        SoundModeContext context = new(audio, new FakeTimerFactory(), new CryptoRandomSource());
        IReadOnlyList<ISoundClip> pool = [new FakeClip("a"), new FakeClip("b")];
        TypingSoundEngine sut = new(context, new TypewriterMode(pool, null));

        sut.NotifyKeyPressed(KeyCategory.Other);
        Assert.Single(audio.Played);

        sut.Dispose();
        sut.NotifyKeyPressed(KeyCategory.Other);

        Assert.Single(audio.Played);
    }

    [Fact]
    public void EveryKeyTriggerDisposeIsIdempotentAndHarmless()
    {
        EveryKeyTrigger sut = new();
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Dispose();
        sut.Dispose();
        sut.Notify(KeyCategory.Other);

        Assert.Equal(1, fired);
    }
}
