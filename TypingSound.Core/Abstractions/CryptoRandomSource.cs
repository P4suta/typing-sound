using System.Security.Cryptography;

namespace TypingSound.Core.Abstractions;

/// <summary>
/// 既定の乱数source。フレームウォーク標準の <see cref="RandomNumberGenerator"/> を用いる。
/// 自前 PRNG を実装せず標準 API に委ねるための実装で、暗号強度そのものが目的ではない
/// (音の選択は非セキュリティ用途)。一様な整数生成は <see cref="RandomNumberGenerator.GetInt32(int)"/> に任せる。
/// </summary>
public sealed class CryptoRandomSource : IRandomSource
{
    /// <inheritdoc/>
    public int NextBelow(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
}
