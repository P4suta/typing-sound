using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>重ね再生。鳴っている音を止めず新しい音を重ねる(連打で和音になる)。</summary>
public sealed class PolyphonicPolicy : IPlaybackPolicy
{
    private readonly IAudioEngine _engine;

    /// <summary>音声エンジンを指定して生成する。</summary>
    /// <param name="engine">音を鳴らすエンジン。</param>
    public PolyphonicPolicy(IAudioEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }

    /// <inheritdoc/>
    public void Play(ISoundClip clip) => _engine.Play(clip);
}
