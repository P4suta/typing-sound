namespace TypingSound.Core.Abstractions;

/// <summary>
/// 単発タイマーの抽象。<see cref="Schedule"/> を呼ぶたびに前回の予約は取り消され、新しい遅延で測り直す
/// (デバウンス用)。Platform/App 層の実装は UI スレッド上でコールバックを発火させ、
/// パイプラインのスレッド親和性(ロック不要)を保証する。テストではフェイク実装で時間を進める。
/// </summary>
public interface ISoundTimer : IDisposable
{
    /// <summary>指定遅延後に一度だけ <paramref name="callback"/> を呼ぶよう予約する(前回の予約は取り消す)。</summary>
    /// <param name="delay">発火までの遅延。</param>
    /// <param name="callback">発火時に呼ぶコールバック。</param>
    void Schedule(TimeSpan delay, Action callback);

    /// <summary>予約中のコールバックを取り消す。</summary>
    void Cancel();
}
