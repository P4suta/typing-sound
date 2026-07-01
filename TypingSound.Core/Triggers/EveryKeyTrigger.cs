using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// Trigger that fires immediately on every key press, regardless of category.
/// Auto-repeat (key held down) also fires as a press, producing a rapid-fire feel.
/// </summary>
public sealed class EveryKeyTrigger : ITriggerStrategy
{
    /// <inheritdoc/>
    public event EventHandler<KeyPressedEventArgs>? Fired;

    /// <inheritdoc/>
    public void Notify(KeyCategory category) => Fired?.Invoke(this, KeyPressedEventArgs.For(category));

    /// <inheritdoc/>
    public void Dispose()
    {
        // No resources held.
    }
}
