namespace TypingSound.Core.Abstractions;

/// <summary>Arguments for a key-press event; carries only the <see cref="KeyCategory">classification</see>.</summary>
public sealed class KeyPressedEventArgs : EventArgs
{
    /// <summary>Shared instance for <see cref="KeyCategory.Other"/> (avoids per-press allocation).</summary>
    public static readonly KeyPressedEventArgs Other = new(KeyCategory.Other);

    /// <summary>Shared instance for <see cref="KeyCategory.Enter"/>.</summary>
    public static readonly KeyPressedEventArgs Enter = new(KeyCategory.Enter);

    /// <summary>Creates an instance for the given classification.</summary>
    public KeyPressedEventArgs(KeyCategory category) => Category = category;

    /// <summary>Classification of the pressed key.</summary>
    public KeyCategory Category { get; }

    /// <summary>Returns the shared instance for the classification (avoids per-press allocation).</summary>
    public static KeyPressedEventArgs For(KeyCategory category) =>
        category == KeyCategory.Enter ? Enter : Other;
}
