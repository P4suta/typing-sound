using TypingSound.Core.Abstractions;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class FixedSelectorTests
{
    [Fact]
    public void AlwaysReturnsTheSameClip()
    {
        FakeClip clip = new("bell");
        FixedSelector sut = new(clip);

        Assert.Same(clip, sut.Pick(KeyCategory.Other));
        Assert.Same(clip, sut.Pick(KeyCategory.Other));
        Assert.Same(clip, sut.Pick(KeyCategory.Other));
    }

    [Fact]
    public void ReturnsNullWhenConstructedWithNull()
    {
        FixedSelector sut = new(null);
        Assert.Null(sut.Pick(KeyCategory.Other));
    }
}
