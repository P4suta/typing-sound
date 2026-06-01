using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>
/// 起動中のモード 1 つ分の実体。キー押下を受けて音を鳴らす責務を持つ。
/// クリップ方式(<see cref="SoundModePipeline"/>)も連続声部方式(無限音階)も、この形に統一される。
/// 単一スレッド(UI スレッド)から呼ばれる前提。破棄でリソース(タイマー/声部)を解放する。
/// </summary>
public interface IActiveMode : IDisposable
{
    /// <summary>キーが 1 つ押されたことを通知する。</summary>
    /// <param name="category">押下キーの分類。</param>
    void OnKeyPressed(KeyCategory category);
}
