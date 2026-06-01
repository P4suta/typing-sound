namespace TypingSound.Core.Modes;

/// <summary>Alpha で既定提供するモードを生成するファクトリ。</summary>
public static class DefaultModeSet
{
    /// <summary>音源カタログから既定モード(タイプライター)を作る。先頭が既定。</summary>
    /// <param name="catalog">打鍵プールと return ベルを保持するカタログ。</param>
    public static IReadOnlyList<ISoundMode> Create(SoundCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return
        [
            new TypewriterMode(catalog.TypingClips, catalog.ReturnBell),
        ];
    }
}
