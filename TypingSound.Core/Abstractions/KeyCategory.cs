namespace TypingSound.Core.Abstractions;

/// <summary>
/// Classification of a key press. <b>The key value is never stored</b>; only the distinction needed
/// for sound output (privacy principle: content is never recorded or sent; only the category is used
/// transiently to produce a sound). More values may be added later without breaking callers.
/// </summary>
public enum KeyCategory
{
    /// <summary>General key (letters, symbols, etc. with no special distinction).</summary>
    Other = 0,

    /// <summary>Enter (newline/return); corresponds to a typewriter carriage return.</summary>
    Enter = 1,
}
