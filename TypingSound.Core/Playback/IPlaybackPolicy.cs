using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Playback;

/// <summary>
/// 軸C:「どう鳴らすか」を決める戦略。<see cref="IAudioEngine"/> をコンストラクタで受け取る。
/// </summary>
public interface IPlaybackPolicy
{
    /// <summary>クリップを方針に従って再生する。</summary>
    /// <param name="clip">再生するクリップ。</param>
    void Play(ISoundClip clip);
}
