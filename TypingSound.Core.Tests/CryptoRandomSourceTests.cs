using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests;

public class CryptoRandomSourceTests
{
    [Fact]
    public void NextBelowStaysWithinRangeOverManyDraws()
    {
        CryptoRandomSource sut = new();

        for (int i = 0; i < 10_000; i++)
        {
            int value = sut.NextBelow(7);

            Assert.InRange(value, 0, 6);
        }
    }

    [Fact]
    public void NextBelowOfOneIsAlwaysZero()
    {
        CryptoRandomSource sut = new();

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(0, sut.NextBelow(1));
        }
    }

    [Fact]
    public void EventuallyProducesEveryValueInRange()
    {
        CryptoRandomSource sut = new();
        HashSet<int> seen = [];

        for (int i = 0; i < 10_000; i++)
        {
            seen.Add(sut.NextBelow(5));
        }

        Assert.Equal([0, 1, 2, 3, 4], seen.OrderBy(static v => v).ToArray());
    }

    [Fact]
    public void NextBelowRejectsNonPositiveBound()
    {
        CryptoRandomSource sut = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.NextBelow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.NextBelow(-3));
    }
}
