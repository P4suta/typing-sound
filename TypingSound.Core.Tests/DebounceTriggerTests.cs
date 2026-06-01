using TypingSound.Core.Abstractions;
using TypingSound.Core.Tests.Fakes;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Tests;

public class DebounceTriggerTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void DoesNotFireUntilTimerElapses()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Notify(KeyCategory.Other);

        Assert.Equal(0, fired);
        Assert.True(timers.Timer.HasPending);
    }

    [Fact]
    public void FiresOnceAfterQuietPeriod()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Notify(KeyCategory.Other);
        timers.Timer.Elapse();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void BurstOfKeysFiresOnlyOnceAfterTheLastKey()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);
        Assert.Equal(0, fired);

        timers.Timer.Elapse();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void EachNotifyReschedulesTheTimer()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);

        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);

        Assert.Equal(3, timers.Timer.ScheduleCount);
    }

    [Fact]
    public void SchedulesWithConfiguredQuietPeriod()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(TimeSpan.FromMilliseconds(250), timers);

        sut.Notify(KeyCategory.Other);

        Assert.Equal(TimeSpan.FromMilliseconds(250), timers.Timer.LastDelay);
    }

    [Fact]
    public void DisposeDisposesTheTimer()
    {
        FakeTimerFactory timers = new();
        DebounceTrigger sut = new(Quiet, timers);

        sut.Notify(KeyCategory.Other);
        sut.Dispose();

        Assert.False(timers.Timer.HasPending);
    }

    [Fact]
    public void NullTimerFactoryThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new DebounceTrigger(Quiet, null!));
    }
}
