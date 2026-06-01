using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests.Fakes;

/// <summary>テスト用のクリップ。Id と参照同一性だけを持つ。</summary>
internal sealed class FakeClip : ISoundClip
{
    public FakeClip(string id) => Id = id;

    public string Id { get; }

    public override string ToString() => Id;
}
