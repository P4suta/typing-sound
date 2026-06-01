using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class DefaultModeSetTests
{
    [Fact]
    public void CreateReturnsTheTypewriterMode()
    {
        IReadOnlyList<ISoundMode> modes = DefaultModeSet.Create(SampleCatalog());

        Assert.Equal(["typewriter"], modes.Select(mode => mode.Id));
    }

    [Fact]
    public void EveryModeHasANonEmptyDisplayName()
    {
        IReadOnlyList<ISoundMode> modes = DefaultModeSet.Create(SampleCatalog());

        Assert.All(modes, mode => Assert.False(string.IsNullOrWhiteSpace(mode.DisplayName)));
    }

    [Fact]
    public void EveryModeActivatesANonNullActiveMode()
    {
        IReadOnlyList<ISoundMode> modes = DefaultModeSet.Create(SampleCatalog());
        SoundModeContext context = SampleContext();

        Assert.All(modes, mode => Assert.NotNull(mode.Activate(context)));
    }

    [Fact]
    public void CreateWithNullCatalogThrows()
    {
        Assert.Throws<ArgumentNullException>(() => DefaultModeSet.Create(null!));
    }

    private static SoundCatalog SampleCatalog() =>
        new([new FakeClip("a"), new FakeClip("b")], new FakeClip("bell"));

    private static SoundModeContext SampleContext() =>
        new(new FakeAudioEngine(), new FakeTimerFactory(), new CryptoRandomSource());
}
