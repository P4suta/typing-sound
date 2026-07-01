namespace TypingSound.Core.Abstractions;

/// <summary>
/// Source of global key-press events. <b>The key value is never reported</b> (only the fact that a
/// key was pressed). Implementations (Platform layer) use a low-level keyboard hook and the callback
/// returns immediately.
/// </summary>
public interface IKeyEventSource : IDisposable
{
    /// <summary>Raised when a key is pressed (carries only the classification, not the key value).</summary>
    event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <summary>Starts the hook. Call on the UI thread (which has a message pump).</summary>
    void Start();

    /// <summary>Stops the hook.</summary>
    void StopListening();
}
