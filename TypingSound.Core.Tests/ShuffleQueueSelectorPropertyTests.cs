using TypingSound.Core.Abstractions;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class ShuffleQueueSelectorPropertyTests
{
    private const int Alphabet = 26;
    private const int Cycles = 40;

    [Fact]
    public void NeverRepeatsTheSameClipBackToBackOverManyCycles()
    {
        IReadOnlyList<ISoundClip> pool = Pool(Alphabet);
        ShuffleQueueSelector sut = new(pool, new CryptoRandomSource());

        string? previous = null;
        for (int i = 0; i < Alphabet * Cycles; i++)
        {
            string id = sut.Pick(KeyCategory.Other)!.Id;
            Assert.NotEqual(previous, id);
            previous = id;
        }
    }

    [Fact]
    public void EveryCycleIsAFullPermutationOfThePool()
    {
        IReadOnlyList<ISoundClip> pool = Pool(Alphabet);
        string[] expected = pool.Select(static c => c.Id).OrderBy(static x => x).ToArray();
        ShuffleQueueSelector sut = new(pool, new CryptoRandomSource());

        for (int cycle = 0; cycle < Cycles; cycle++)
        {
            string[] picked = new string[Alphabet];
            for (int i = 0; i < Alphabet; i++)
            {
                picked[i] = sut.Pick(KeyCategory.Other)!.Id;
            }

            Assert.Equal(expected, picked.OrderBy(static x => x).ToArray());
        }
    }

    [Fact]
    public void EachClipAppearsExactlyOncePerCycleSoOccurrencesAreEqual()
    {
        IReadOnlyList<ISoundClip> pool = Pool(Alphabet);
        ShuffleQueueSelector sut = new(pool, new CryptoRandomSource());

        Dictionary<string, int> counts = pool.ToDictionary(static c => c.Id, static _ => 0);
        for (int i = 0; i < Alphabet * Cycles; i++)
        {
            counts[sut.Pick(KeyCategory.Other)!.Id]++;
        }

        Assert.Equal(Alphabet, counts.Count);
        Assert.All(counts.Values, static count => Assert.Equal(Cycles, count));
    }

    private static ISoundClip[] Pool(int size)
    {
        ISoundClip[] clips = new ISoundClip[size];
        for (int i = 0; i < size; i++)
        {
            clips[i] = new FakeClip(((char)('a' + i)).ToString());
        }

        return clips;
    }
}
