using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>Test clip carrying only an Id and reference identity.</summary>
internal sealed class FakeClip : ISoundClip
{
    public FakeClip(string id) => Id = id;

    public string Id { get; }

    public override string ToString() => Id;
}
