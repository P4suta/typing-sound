namespace TypingSound.Core.Abstractions;

/// <summary>
/// グローバルなキー押下イベントの供給元。<b>キーの値は通知しない</b>(押下という事実だけ)。
/// 実装(Platform 層)は低レベルキーボードフックを用い、コールバックは即 return する。
/// </summary>
public interface IKeyEventSource : IDisposable
{
    /// <summary>キーが押されたときに発火する(分類のみ伴い、キーの具体値は伴わない)。</summary>
    event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <summary>フックを開始する。UI スレッド(メッセージポンプあり)で呼ぶこと。</summary>
    void Start();

    /// <summary>フックを停止する。</summary>
    void StopListening();
}
