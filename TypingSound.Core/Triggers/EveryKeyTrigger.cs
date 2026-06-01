using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Triggers;

/// <summary>
/// 毎押下で即座に発火するトリガ(「キー押下で毎回音が鳴る」)。キー分類は問わない。
/// オートリピート(キー長押し)も 1 押下として発火する — 連打感が出る、ジョークアプリ向けの既定。
/// </summary>
public sealed class EveryKeyTrigger : ITriggerStrategy
{
    /// <inheritdoc/>
    public event EventHandler<KeyPressedEventArgs>? Fired;

    /// <inheritdoc/>
    public void Notify(KeyCategory category) => Fired?.Invoke(this, KeyPressedEventArgs.For(category));

    /// <inheritdoc/>
    public void Dispose()
    {
        // 保持しているリソースはない。
    }
}
