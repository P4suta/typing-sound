using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>常に同一の <see cref="FakeTimer"/> を返すファクトリ(テストから発火を制御するため)。</summary>
internal sealed class FakeTimerFactory : ITimerFactory
{
    public FakeTimer Timer { get; } = new();

    public ISoundTimer Create() => Timer;
}
