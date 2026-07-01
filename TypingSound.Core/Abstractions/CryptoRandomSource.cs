using System.Security.Cryptography;

namespace TypingSound.Core.Abstractions;

/// <summary>
/// Default random source backed by <see cref="RandomNumberGenerator"/>.
/// Uses the standard API rather than a custom PRNG; cryptographic strength is not the goal
/// (sound selection is a non-security use).
/// </summary>
public sealed class CryptoRandomSource : IRandomSource
{
    /// <inheritdoc/>
    public int NextBelow(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
}
