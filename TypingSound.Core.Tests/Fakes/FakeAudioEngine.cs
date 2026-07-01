using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>Test audio engine that records played clips (and their handles) in order.</summary>
internal sealed class FakeAudioEngine : IAudioEngine
{
    public List<FakePlayingSound> Played { get; } = [];

    public IPlayingSound Play(ISoundClip clip)
    {
        FakePlayingSound sound = new(clip);
        Played.Add(sound);
        return sound;
    }
}
