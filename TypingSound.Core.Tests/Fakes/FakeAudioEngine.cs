using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>テスト用の音声エンジン。Play されたクリップ(と各ハンドル)を順に記録する。</summary>
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
