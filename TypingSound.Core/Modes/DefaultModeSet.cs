namespace TypingSound.Core.Modes;

/// <summary>Factory for the modes shipped by default in Alpha.</summary>
public static class DefaultModeSet
{
    /// <summary>Builds the default modes (Typewriter) from a sound catalog; the first is the default.</summary>
    public static IReadOnlyList<ISoundMode> Create(SoundCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return
        [
            new TypewriterMode(catalog.TypingClips, catalog.ReturnBell),
        ];
    }
}
