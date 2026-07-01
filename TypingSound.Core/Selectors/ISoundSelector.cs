using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Selectors;

/// <summary>Axis B: strategy deciding "which clip to play". Some implementations are stateful (e.g. shuffle queue).</summary>
public interface ISoundSelector
{
    /// <summary>Picks the next clip to play, or <see langword="null"/> to play nothing.</summary>
    /// <param name="category">Key category that triggered firing; key-agnostic selectors may ignore it.</param>
    ISoundClip? Pick(KeyCategory category);
}
