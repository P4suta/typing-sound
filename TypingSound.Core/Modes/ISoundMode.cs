namespace TypingSound.Core.Modes;

/// <summary>
/// 「モード」の定義。<see cref="Activate"/> で 1 セッション分の <see cref="IActiveMode"/> を生成する。
/// クリップ方式は 3 軸を <see cref="SoundModePipeline"/> に合成し、連続声部方式は別の <see cref="IActiveMode"/> を返す。
/// </summary>
public interface ISoundMode
{
    /// <summary>安定した識別子(設定保存・既定選択に使う)。</summary>
    string Id { get; }

    /// <summary>UI 表示名。</summary>
    string DisplayName { get; }

    /// <summary>実行時サービスからこのモードの実体を起動する。</summary>
    /// <param name="context">音声/タイマー/乱数/連続声部などの実行時サービス。</param>
    IActiveMode Activate(SoundModeContext context);
}
