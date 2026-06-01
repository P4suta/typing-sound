using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>単音。新しい音を鳴らす前に直前の音を止める(常に最新の 1 音だけが鳴る)。</summary>
public sealed class MonophonicPolicy : IPlaybackPolicy
{
    private readonly IAudioEngine _engine;
    private IPlayingSound? _current;

    /// <summary>音声エンジンを指定して生成する。</summary>
    /// <param name="engine">音を鳴らすエンジン。</param>
    public MonophonicPolicy(IAudioEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }

    /// <inheritdoc/>
    public void Play(ISoundClip clip)
    {
        _current?.Halt();
        _current = _engine.Play(clip);
    }
}
