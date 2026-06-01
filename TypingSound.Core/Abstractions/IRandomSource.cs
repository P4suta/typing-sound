namespace TypingSound.Core.Abstractions;

/// <summary>乱数の供給元の抽象(テストで決定的にするため注入する)。</summary>
public interface IRandomSource
{
    /// <summary>0 以上 <paramref name="maxExclusive"/> 未満の整数を返す。</summary>
    /// <param name="maxExclusive">上限(この値自身は含まない)。1 以上であること。</param>
    int NextBelow(int maxExclusive);
}
