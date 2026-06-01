using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// 軸A:「いつ音を鳴らすか」を決めるステートフルな戦略。
/// キー押下を <see cref="Notify"/> で受け取り、鳴らすべき瞬間に <see cref="Fired"/> を発火する。
/// 実装は単一スレッド(UI スレッド)から呼ばれる前提で書かれ、ロックを持たない。
/// </summary>
public interface ITriggerStrategy : IDisposable
{
    /// <summary>
    /// 鳴らす意図が確定したときに発火する(購読者は 1 つを想定)。
    /// 引数に発火の契機となったキー分類を運び、セレクタがキー種別で出し分けできるようにする。
    /// </summary>
    event EventHandler<KeyPressedEventArgs>? Fired;

    /// <summary>キーが 1 つ押されたことを通知する。キーに依らない戦略は <paramref name="category"/> を無視してよい。</summary>
    /// <param name="category">押下キーの分類。</param>
    void Notify(KeyCategory category);
}
