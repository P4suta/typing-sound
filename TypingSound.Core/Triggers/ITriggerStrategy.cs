using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// Axis A: stateful strategy deciding "when to play". Receives key presses via <see cref="Notify"/>
/// and raises <see cref="Fired"/> at the moment a sound should play. Implementations assume they are
/// called from a single (UI) thread and hold no locks.
/// </summary>
public interface ITriggerStrategy : IDisposable
{
    /// <summary>
    /// Raised when the intent to play is confirmed (a single subscriber is assumed). Carries the key
    /// category that triggered it so the selector can dispatch by key type.
    /// </summary>
    event EventHandler<KeyPressedEventArgs>? Fired;

    /// <summary>Notifies that a single key was pressed; key-agnostic strategies may ignore <paramref name="category"/>.</summary>
    void Notify(KeyCategory category);
}
