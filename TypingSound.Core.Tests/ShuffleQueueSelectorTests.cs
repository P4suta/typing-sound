using TypingSound.Core.Abstractions;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class ShuffleQueueSelectorTests
{
    private static readonly IReadOnlyList<ISoundClip> ThreeClips =
    [
        new FakeClip("a"),
        new FakeClip("b"),
        new FakeClip("c"),
    ];

    [Fact]
    public void EachCycleIsAFullPermutationWithoutDuplicates()
    {
        ShuffleQueueSelector sut = new(ThreeClips, new CryptoRandomSource());

        for (int cycle = 0; cycle < 5; cycle++)
        {
            List<string> picked = [];
            for (int i = 0; i < ThreeClips.Count; i++)
            {
                picked.Add(sut.Pick(KeyCategory.Other)!.Id);
            }

            Assert.Equal(ThreeClips.Count, picked.Distinct().Count());
            Assert.Equal(new[] { "a", "b", "c" }, picked.OrderBy(static x => x).ToArray());
        }
    }

    [Fact]
    public void NeverRepeatsTheSameClipBackToBack_IncludingAcrossReshuffleBoundary()
    {
        ShuffleQueueSelector sut = new(ThreeClips, new CryptoRandomSource());

        string? previous = null;
        for (int i = 0; i < 300; i++)
        {
            string id = sut.Pick(KeyCategory.Other)!.Id;
            Assert.NotEqual(previous, id);
            previous = id;
        }
    }

    [Fact]
    public void TwoClipsAlternateForever()
    {
        IReadOnlyList<ISoundClip> two = [new FakeClip("x"), new FakeClip("y")];
        ShuffleQueueSelector sut = new(two, new CryptoRandomSource());

        string? previous = null;
        for (int i = 0; i < 50; i++)
        {
            string id = sut.Pick(KeyCategory.Other)!.Id;
            Assert.NotEqual(previous, id);
            previous = id;
        }
    }

    [Fact]
    public void ReturnsNullWhenPoolIsEmpty()
    {
        ShuffleQueueSelector sut = new([], new CryptoRandomSource());
        Assert.Null(sut.Pick(KeyCategory.Other));
    }
}
