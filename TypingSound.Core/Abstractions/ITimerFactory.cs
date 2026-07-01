namespace TypingSound.Core.Abstractions;

/// <summary>Factory that creates <see cref="ISoundTimer"/> instances.</summary>
public interface ITimerFactory
{
    /// <summary>Creates a new one-shot timer.</summary>
    ISoundTimer Create();
}
