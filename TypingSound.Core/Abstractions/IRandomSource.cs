namespace TypingSound.Core.Abstractions;

/// <summary>Abstraction over a random source (injected to make tests deterministic).</summary>
public interface IRandomSource
{
    /// <summary>Returns an integer in [0, <paramref name="maxExclusive"/>).</summary>
    /// <param name="maxExclusive">Exclusive upper bound; must be at least 1.</param>
    int NextBelow(int maxExclusive);
}
