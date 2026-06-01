using TypingSound.Core.Abstractions;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;

namespace TypingSound.Core.Tests;

public class TypewriterSelectorTests
{
    [Fact]
    public void EnterReturnsTheReturnBell()
    {
        FakeClip bell = new("bell");
        TypewriterSelector sut = new(new StubSelector(new FakeClip("keystroke")), bell);

        Assert.Same(bell, sut.Pick(KeyCategory.Enter));
    }

    [Fact]
    public void OtherDelegatesToTheKeystrokeSelector()
    {
        FakeClip keystroke = new("keystroke");
        StubSelector keystrokeSelector = new(keystroke);
        TypewriterSelector sut = new(keystrokeSelector, new FakeClip("bell"));

        Assert.Same(keystroke, sut.Pick(KeyCategory.Other));
        Assert.Equal(KeyCategory.Other, keystrokeSelector.LastCategory);
    }

    [Fact]
    public void EnterReturnsNullWhenThereIsNoReturnBell()
    {
        TypewriterSelector sut = new(new StubSelector(new FakeClip("keystroke")), null);

        Assert.Null(sut.Pick(KeyCategory.Enter));
    }

    [Fact]
    public void NullKeystrokeSelectorThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new TypewriterSelector(null!, new FakeClip("bell")));
    }

    private sealed class StubSelector : ISoundSelector
    {
        private readonly ISoundClip? _clip;

        public StubSelector(ISoundClip? clip) => _clip = clip;

        public KeyCategory? LastCategory { get; private set; }

        public ISoundClip? Pick(KeyCategory category)
        {
            LastCategory = category;
            return _clip;
        }
    }
}
