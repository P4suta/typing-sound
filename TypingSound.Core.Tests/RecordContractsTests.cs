using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class RecordContractsTests
{
    [Fact]
    public void SoundModeContextExposesItsComponents()
    {
        FakeAudioEngine audio = new();
        FakeTimerFactory timers = new();
        CryptoRandomSource random = new();

        SoundModeContext sut = new(audio, timers, random);

        Assert.Same(audio, sut.Audio);
        Assert.Same(timers, sut.Timers);
        Assert.Same(random, sut.Random);
    }

    [Fact]
    public void SoundModeContextsWithTheSameComponentsAreEqual()
    {
        FakeAudioEngine audio = new();
        FakeTimerFactory timers = new();
        CryptoRandomSource random = new();

        SoundModeContext a = new(audio, timers, random);
        SoundModeContext b = new(audio, timers, random);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void SoundModeContextsWithDifferentComponentsAreNotEqual()
    {
        FakeTimerFactory timers = new();
        CryptoRandomSource random = new();

        SoundModeContext a = new(new FakeAudioEngine(), timers, random);
        SoundModeContext b = new(new FakeAudioEngine(), timers, random);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SoundCatalogExposesItsComponents()
    {
        IReadOnlyList<ISoundClip> clips = [new FakeClip("a"), new FakeClip("b")];
        FakeClip bell = new("bell");

        SoundCatalog sut = new(clips, bell);

        Assert.Same(clips, sut.TypingClips);
        Assert.Same(bell, sut.ReturnBell);
    }

    [Fact]
    public void SoundCatalogAllowsANullReturnBell()
    {
        IReadOnlyList<ISoundClip> clips = [new FakeClip("a")];

        SoundCatalog sut = new(clips, null);

        Assert.Same(clips, sut.TypingClips);
        Assert.Null(sut.ReturnBell);
    }

    [Fact]
    public void SoundCatalogsWithTheSameComponentsAreEqual()
    {
        IReadOnlyList<ISoundClip> clips = [new FakeClip("a")];
        FakeClip bell = new("bell");

        SoundCatalog a = new(clips, bell);
        SoundCatalog b = new(clips, bell);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void SoundCatalogsWithNullReturnBellsAreEqual()
    {
        IReadOnlyList<ISoundClip> clips = [new FakeClip("a")];

        SoundCatalog a = new(clips, null);
        SoundCatalog b = new(clips, null);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void SoundCatalogsDifferingOnlyInReturnBellAreNotEqual()
    {
        IReadOnlyList<ISoundClip> clips = [new FakeClip("a")];

        SoundCatalog withBell = new(clips, new FakeClip("bell"));
        SoundCatalog withoutBell = new(clips, null);

        Assert.NotEqual(withBell, withoutBell);
    }

    [Fact]
    public void SoundCatalogsDifferingInTypingClipsAreNotEqual()
    {
        FakeClip bell = new("bell");

        SoundCatalog a = new([new FakeClip("a")], bell);
        SoundCatalog b = new([new FakeClip("a")], bell);

        Assert.NotEqual(a, b);
    }
}
