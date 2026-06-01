using TypingSound.Core.Abstractions;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class RandomSelectorTests
{
    private static readonly IReadOnlyList<ISoundClip> Pool =
    [
        new FakeClip("a"),
        new FakeClip("b"),
        new FakeClip("c"),
    ];

    [Fact]
    public void AlwaysReturnsAClipFromThePool()
    {
        RandomSelector sut = new(Pool, new CryptoRandomSource());
        HashSet<string> ids = [.. Pool.Select(c => c.Id)];

        for (int i = 0; i < 200; i++)
        {
            Assert.Contains(sut.Pick(KeyCategory.Other)!.Id, ids);
        }
    }

    [Fact]
    public void SingleClipPoolAlwaysReturnsThatClip()
    {
        FakeClip only = new("solo");
        RandomSelector sut = new([only], new CryptoRandomSource());

        Assert.Same(only, sut.Pick(KeyCategory.Other));
        Assert.Same(only, sut.Pick(KeyCategory.Other));
    }

    [Fact]
    public void ReturnsNullWhenPoolIsEmpty()
    {
        RandomSelector sut = new([], new CryptoRandomSource());
        Assert.Null(sut.Pick(KeyCategory.Other));
    }

    [Fact]
    public void NullPoolThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomSelector(null!, new CryptoRandomSource()));
    }

    [Fact]
    public void NullRandomThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomSelector(Pool, null!));
    }
}
