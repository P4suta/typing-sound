namespace TypingSound.Core.Abstractions;

/// <summary>A set of loaded sound clips.</summary>
public interface ISoundBank
{
    /// <summary>All available clips.</summary>
    IReadOnlyList<ISoundClip> Clips { get; }

    /// <summary>Finds a clip by identifier, or <see langword="null"/> if not found.</summary>
    ISoundClip? FindById(string id);
}
