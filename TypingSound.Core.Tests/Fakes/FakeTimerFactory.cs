using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>Factory that always returns the same <see cref="FakeTimer"/> (so tests can control firing).</summary>
internal sealed class FakeTimerFactory : ITimerFactory
{
    public FakeTimer Timer { get; } = new();

    public ISoundTimer Create() => Timer;
}
