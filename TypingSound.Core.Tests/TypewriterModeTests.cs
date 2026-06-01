using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class TypewriterModeTests
{
    private static readonly IReadOnlyList<ISoundClip> Pool =
    [
        new FakeClip("a"),
        new FakeClip("b"),
        new FakeClip("c"),
    ];

    private static readonly FakeClip Bell = new("bell");

    [Fact]
    public void HasStableId()
    {
        TypewriterMode sut = new(Pool, Bell);

        Assert.Equal("typewriter", sut.Id);
    }

    [Fact]
    public void HasNonEmptyDisplayName()
    {
        TypewriterMode sut = new(Pool, Bell);

        Assert.False(string.IsNullOrWhiteSpace(sut.DisplayName));
    }

    [Fact]
    public void NullPoolThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new TypewriterMode(null!, Bell));
    }

    [Fact]
    public void ActivateWithNullContextThrows()
    {
        TypewriterMode sut = new(Pool, Bell);

        Assert.Throws<ArgumentNullException>(() => sut.Activate(null!));
    }

    [Fact]
    public void AnOrdinaryKeyPlaysAKeystrokeFromThePool()
    {
        FakeAudioEngine audio = new();
        TypewriterMode sut = new(Pool, Bell);
        using TypingSoundEngine engine = new(NewContext(audio), sut);

        engine.NotifyKeyPressed(KeyCategory.Other);

        Assert.Single(audio.Played);
        Assert.Contains(audio.Played[0].Clip, Pool);
    }

    [Fact]
    public void EnterPlaysTheReturnBell()
    {
        FakeAudioEngine audio = new();
        TypewriterMode sut = new(Pool, Bell);
        using TypingSoundEngine engine = new(NewContext(audio), sut);

        engine.NotifyKeyPressed(KeyCategory.Enter);

        Assert.Single(audio.Played);
        Assert.Same(Bell, audio.Played[0].Clip);
    }

    private static SoundModeContext NewContext(FakeAudioEngine audio) =>
        new(audio, new FakeTimerFactory(), new CryptoRandomSource());
}
