using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;

namespace TypingSound.Core;

/// <summary>
/// パイプラインの司令塔。キー押下(<see cref="NotifyKeyPressed"/>)を起動中のモード実体へ流し、
/// モード切替(<see cref="SwitchTo"/>)も担う。全メソッドは単一スレッド(UI スレッド)から呼ばれる前提で、
/// ロックを持たない。キー押下は同期インラインで処理し、押鍵から発音までの遅延を最小化する。
/// </summary>
public sealed class TypingSoundEngine : IDisposable
{
    private readonly SoundModeContext _context;
    private IActiveMode _active;

    /// <summary>実行時サービスと初期モードを指定して生成する。</summary>
    /// <param name="context">音声/タイマー/乱数/連続声部などの実行時サービス。</param>
    /// <param name="initialMode">起動時のモード。</param>
    public TypingSoundEngine(SoundModeContext context, ISoundMode initialMode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialMode);
        _context = context;
        CurrentMode = initialMode;
        _active = initialMode.Activate(context);
    }

    /// <summary>現在のモード。</summary>
    public ISoundMode CurrentMode { get; private set; }

    /// <summary>キーが 1 つ押されたことを通知する(分類のみ伴う)。</summary>
    /// <param name="category">押下キーの分類。</param>
    public void NotifyKeyPressed(KeyCategory category) => _active.OnKeyPressed(category);

    /// <summary>モードを切り替える。古いモード実体は破棄する。</summary>
    /// <param name="mode">切り替え先のモード。</param>
    public void SwitchTo(ISoundMode mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        _active.Dispose();
        CurrentMode = mode;
        _active = mode.Activate(_context);
    }

    /// <inheritdoc/>
    public void Dispose() => _active.Dispose();
}
