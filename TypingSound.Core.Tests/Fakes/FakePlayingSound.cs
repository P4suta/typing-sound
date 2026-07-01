using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>Test playback handle that records whether it was halted.</summary>
internal sealed class FakePlayingSound : IPlayingSound
{
    public FakePlayingSound(ISoundClip clip) => Clip = clip;

    public ISoundClip Clip { get; }

    public bool Halted { get; private set; }

    public void Halt() => Halted = true;
}
