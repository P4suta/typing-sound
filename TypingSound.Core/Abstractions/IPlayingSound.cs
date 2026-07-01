namespace TypingSound.Core.Abstractions;

/// <summary>Handle to a single playing sound.</summary>
public interface IPlayingSound
{
    /// <summary>Stops this sound immediately (used by the monophonic "stop the previous" behavior).</summary>
    void Halt();
}
