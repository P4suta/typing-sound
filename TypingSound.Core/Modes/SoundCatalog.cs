using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Modes;

/// <summary>
/// Semantically labeled clip selection, assembled by the composition root from an
/// <see cref="ISoundBank"/> and passed to the default modes. Split into the roles "keystroke pool"
/// and "return bell".
/// </summary>
/// <param name="TypingClips">Pool of keystroke sounds played on every key.</param>
/// <param name="ReturnBell">Return bell played on "last only" (<see langword="null"/> if none).</param>
public sealed record SoundCatalog(IReadOnlyList<ISoundClip> TypingClips, ISoundClip? ReturnBell);
