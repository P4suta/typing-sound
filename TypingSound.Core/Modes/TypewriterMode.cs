using TypingSound.Core.Abstractions;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Modes;

/// <summary>
/// タイプライター モード。一般キーでは打鍵音(重複なしで巡回)を、Enter では復帰ベルを鳴らす。
/// 音は重ねて鳴らす(ポリフォニック)。
/// = <see cref="EveryKeyTrigger"/> + <see cref="TypewriterSelector"/>(打鍵は <see cref="ShuffleQueueSelector"/> に委譲)
///   + <see cref="PolyphonicPolicy"/>。
/// </summary>
public sealed class TypewriterMode : ISoundMode
{
    private readonly IReadOnlyList<ISoundClip> _keystrokePool;
    private readonly ISoundClip? _returnBell;

    /// <summary>打鍵音のプールと復帰ベルを指定して生成する。</summary>
    /// <param name="keystrokePool">一般キーで鳴らす打鍵音クリップ群。</param>
    /// <param name="returnBell">Enter で鳴らす復帰ベル(<see langword="null"/> なら Enter は無音)。</param>
    public TypewriterMode(IReadOnlyList<ISoundClip> keystrokePool, ISoundClip? returnBell)
    {
        ArgumentNullException.ThrowIfNull(keystrokePool);
        _keystrokePool = keystrokePool;
        _returnBell = returnBell;
    }

    /// <inheritdoc/>
    public string Id => "typewriter";

    /// <inheritdoc/>
    public string DisplayName => "タイプライター（打鍵音＋Enterで復帰ベル）";

    /// <inheritdoc/>
    public IActiveMode Activate(SoundModeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new SoundModePipeline(
            new EveryKeyTrigger(),
            new TypewriterSelector(new ShuffleQueueSelector(_keystrokePool, context.Random), _returnBell),
            new PolyphonicPolicy(context.Audio));
    }
}
