using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>
/// 意味づけされたクリップ選択。合成ルートが音源(<see cref="ISoundBank"/>)から組み立て、
/// 既定モード群へ渡す。「打鍵プール」と「return ベル」という役割で分ける。
/// </summary>
/// <param name="TypingClips">毎キーで鳴らす打鍵音のプール。</param>
/// <param name="ReturnBell">「最後だけ」で鳴らす return ベル(無ければ <see langword="null"/>)。</param>
public sealed record SoundCatalog(IReadOnlyList<ISoundClip> TypingClips, ISoundClip? ReturnBell);
