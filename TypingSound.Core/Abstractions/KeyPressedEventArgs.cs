namespace TypingSound.Core.Abstractions;

/// <summary>キー押下イベントの引数。押下キーの<see cref="KeyCategory">分類</see>のみを伴う。</summary>
public sealed class KeyPressedEventArgs : EventArgs
{
    /// <summary><see cref="KeyCategory.Other"/> 用の共有インスタンス(押下ごとの確保を避ける)。</summary>
    public static readonly KeyPressedEventArgs Other = new(KeyCategory.Other);

    /// <summary><see cref="KeyCategory.Enter"/> 用の共有インスタンス。</summary>
    public static readonly KeyPressedEventArgs Enter = new(KeyCategory.Enter);

    /// <summary>分類を指定して生成する。</summary>
    /// <param name="category">押下キーの分類。</param>
    public KeyPressedEventArgs(KeyCategory category) => Category = category;

    /// <summary>押下キーの分類。</summary>
    public KeyCategory Category { get; }

    /// <summary>分類に対応する共有インスタンスを返す(押下ごとの確保を避ける)。</summary>
    /// <param name="category">押下キーの分類。</param>
    public static KeyPressedEventArgs For(KeyCategory category) =>
        category == KeyCategory.Enter ? Enter : Other;
}
