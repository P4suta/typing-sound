using TypingSound.Core.Abstractions;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Tests;

public class EveryKeyTriggerTests
{
    [Fact]
    public void FiresOncePerNotify()
    {
        EveryKeyTrigger sut = new();
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);
        sut.Notify(KeyCategory.Other);

        Assert.Equal(3, fired);
    }

    [Fact]
    public void DoesNotThrowWhenNoSubscriber()
    {
        EveryKeyTrigger sut = new();
        sut.Notify(KeyCategory.Other);
    }

    [Fact]
    public void DisposeIsHarmlessAndNotifyStillWorks()
    {
        EveryKeyTrigger sut = new();
        int fired = 0;
        sut.Fired += (_, _) => fired++;

        sut.Dispose();
        sut.Notify(KeyCategory.Other);

        Assert.Equal(1, fired);
    }
}
