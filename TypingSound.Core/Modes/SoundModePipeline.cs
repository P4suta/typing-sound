using TypingSound.Core.Abstractions;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Modes;

/// <summary>
/// Clip-based <see cref="IActiveMode"/> that binds three axes (trigger/selector/playback policy).
/// Wires key press -> trigger -> (on fire) selector -> playback policy internally. Disposing also
/// disposes the trigger (and its timer resources).
/// </summary>
public sealed class SoundModePipeline : IActiveMode
{
    /// <summary>Creates a pipeline from the three axes and wires trigger firing to playback.</summary>
    /// <param name="trigger">Axis A: when to play.</param>
    /// <param name="selector">Axis B: which clip to play.</param>
    /// <param name="playback">Axis C: how to play.</param>
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

    /// <summary>Axis A: when to play.</summary>
    public ITriggerStrategy Trigger { get; }

    /// <summary>Axis B: which clip to play.</summary>
    public ISoundSelector Selector { get; }

    /// <summary>Axis C: how to play.</summary>
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
