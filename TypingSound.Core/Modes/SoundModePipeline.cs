using TypingSound.Core.Abstractions;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Modes;

/// <summary>
/// 3 軸(トリガ/セレクタ/再生方針)を束ねた、クリップ方式の <see cref="IActiveMode"/>。
/// キー押下 → トリガ → (発火時) セレクタ → 再生方針、という配線を内部で完結させる。
/// 破棄するとトリガ(タイマー等のリソース)も破棄される。
/// </summary>
public sealed class SoundModePipeline : IActiveMode
{
    /// <summary>3 軸を指定してパイプラインを生成し、トリガの発火を内部で再生へ配線する。</summary>
    /// <param name="trigger">軸A: いつ鳴らすか。</param>
    /// <param name="selector">軸B: どのクリップを鳴らすか。</param>
    /// <param name="playback">軸C: どう鳴らすか。</param>
    public SoundModePipeline(ITriggerStrategy trigger, ISoundSelector selector, IPlaybackPolicy playback)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(playback);
        Trigger = trigger;
        Selector = selector;
        Playback = playback;
        Trigger.Fired += OnFired;
    }

    /// <summary>軸A: いつ鳴らすか。</summary>
    public ITriggerStrategy Trigger { get; }

    /// <summary>軸B: どのクリップを鳴らすか。</summary>
    public ISoundSelector Selector { get; }

    /// <summary>軸C: どう鳴らすか。</summary>
    public IPlaybackPolicy Playback { get; }

    /// <inheritdoc/>
    public void OnKeyPressed(KeyCategory category) => Trigger.Notify(category);

    /// <inheritdoc/>
    public void Dispose()
    {
        Trigger.Fired -= OnFired;
        Trigger.Dispose();
    }

    private void OnFired(object? sender, KeyPressedEventArgs e)
    {
        ISoundClip? clip = Selector.Pick(e.Category);
        if (clip is not null)
        {
            Playback.Play(clip);
        }
    }
}
