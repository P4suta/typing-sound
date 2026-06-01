using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;
using TypingSound.Core.Playback;
using TypingSound.Core.Selectors;
using TypingSound.Core.Tests.Fakes;
using TypingSound.Core.Triggers;

namespace TypingSound.Core.Tests;

public class TypingSoundEngineTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void NotifyKeyPressedPlaysOncePerKeyInEveryKeyTestMode()
    {
        Harness harness = new();
        EveryKeyTestMode mode = new([new FakeClip("a"), new FakeClip("b")]);
        using TypingSoundEngine sut = new(harness.Context, mode);

        sut.NotifyKeyPressed(KeyCategory.Other);
        sut.NotifyKeyPressed(KeyCategory.Other);

        Assert.Equal(2, harness.Audio.Played.Count);
    }

    [Fact]
    public void EnterRingsTheReturnBellInTypewriterMode()
    {
        Harness harness = new();
        TypewriterMode mode = new([new FakeClip("keystroke")], new FakeClip("bell"));
        using TypingSoundEngine sut = new(harness.Context, mode);

        sut.NotifyKeyPressed(KeyCategory.Enter);

        Assert.Single(harness.Audio.Played);
        Assert.Equal("bell", harness.Audio.Played[0].Clip.Id);
    }

    [Fact]
    public void OtherKeyPlaysAKeystrokeClipInTypewriterMode()
    {
        Harness harness = new();
        TypewriterMode mode = new([new FakeClip("keystroke")], new FakeClip("bell"));
        using TypingSoundEngine sut = new(harness.Context, mode);

        sut.NotifyKeyPressed(KeyCategory.Other);

        Assert.Single(harness.Audio.Played);
        Assert.Equal("keystroke", harness.Audio.Played[0].Clip.Id);
    }

    [Fact]
    public void CurrentModeReflectsTheInitialMode()
    {
        Harness harness = new();
        EveryKeyTestMode mode = new([new FakeClip("a")]);
        using TypingSoundEngine sut = new(harness.Context, mode);

        Assert.Same(mode, sut.CurrentMode);
    }

    [Fact]
    public void CurrentModeReflectsTheModeAfterSwitchTo()
    {
        Harness harness = new();
        EveryKeyTestMode initial = new([new FakeClip("a")]);
        TypewriterMode next = new([new FakeClip("keystroke")], new FakeClip("bell"));
        using TypingSoundEngine sut = new(harness.Context, initial);

        sut.SwitchTo(next);

        Assert.Same(next, sut.CurrentMode);
    }

    [Fact]
    public void SwitchToDiscardsThePendingDebounceOfTheOldMode()
    {
        Harness harness = new();
        DebounceMode debounced = new(new FakeClip("bell"), Quiet);
        EveryKeyTestMode everyKey = new([new FakeClip("a")]);
        using TypingSoundEngine sut = new(harness.Context, debounced);

        sut.NotifyKeyPressed(KeyCategory.Other);
        sut.SwitchTo(everyKey);
        harness.Timers.Timer.Elapse();

        Assert.Empty(harness.Audio.Played);
    }

    [Fact]
    public void PlaysTheNewModeAfterSwitchingAwayFromADebouncedMode()
    {
        Harness harness = new();
        DebounceMode debounced = new(new FakeClip("bell"), Quiet);
        EveryKeyTestMode everyKey = new([new FakeClip("a")]);
        using TypingSoundEngine sut = new(harness.Context, debounced);

        sut.NotifyKeyPressed(KeyCategory.Other);
        sut.SwitchTo(everyKey);
        harness.Timers.Timer.Elapse();
        sut.NotifyKeyPressed(KeyCategory.Other);

        Assert.Single(harness.Audio.Played);
        Assert.Equal("a", harness.Audio.Played[0].Clip.Id);
    }

    [Fact]
    public void NotifyKeyPressedDoesNothingAfterDispose()
    {
        Harness harness = new();
        EveryKeyTestMode mode = new([new FakeClip("a")]);
        TypingSoundEngine sut = new(harness.Context, mode);

        sut.Dispose();
        sut.NotifyKeyPressed(KeyCategory.Other);

        Assert.Empty(harness.Audio.Played);
    }

    [Fact]
    public void NullContextThrows()
    {
        EveryKeyTestMode mode = new([new FakeClip("a")]);

        Assert.Throws<ArgumentNullException>(() => { _ = new TypingSoundEngine(null!, mode); });
    }

    [Fact]
    public void NullInitialModeThrows()
    {
        Harness harness = new();

        Assert.Throws<ArgumentNullException>(() => { _ = new TypingSoundEngine(harness.Context, null!); });
    }

    [Fact]
    public void SwitchToNullThrows()
    {
        Harness harness = new();
        EveryKeyTestMode mode = new([new FakeClip("a")]);
        using TypingSoundEngine sut = new(harness.Context, mode);

        Assert.Throws<ArgumentNullException>(() => sut.SwitchTo(null!));
    }

    private sealed class Harness
    {
        public Harness() => Context = new SoundModeContext(Audio, Timers, new CryptoRandomSource());

        public FakeAudioEngine Audio { get; } = new();

        public FakeTimerFactory Timers { get; } = new();

        public SoundModeContext Context { get; }
    }

    /// <summary>静止後に一度だけ固定クリップを鳴らす、デバウンス検証専用のモード。</summary>
    private sealed class DebounceMode : ISoundMode
    {
        private readonly ISoundClip _clip;
        private readonly TimeSpan _quietPeriod;

        public DebounceMode(ISoundClip clip, TimeSpan quietPeriod)
        {
            _clip = clip;
            _quietPeriod = quietPeriod;
        }

        public string Id => "test-debounce";

        public string DisplayName => "テスト用デバウンス";

        public IActiveMode Activate(SoundModeContext context) =>
            new SoundModePipeline(
                new DebounceTrigger(_quietPeriod, context.Timers),
                new FixedSelector(_clip),
                new PolyphonicPolicy(context.Audio));
    }

    /// <summary>毎キーで与えたクリップを巡回再生する、エンジン検証専用のモード(実モードに依存しない)。</summary>
    private sealed class EveryKeyTestMode : ISoundMode
    {
        private readonly IReadOnlyList<ISoundClip> _clips;

        public EveryKeyTestMode(IReadOnlyList<ISoundClip> clips) => _clips = clips;

        public string Id => "test-every-key";

        public string DisplayName => "テスト用毎キー";

        public IActiveMode Activate(SoundModeContext context) =>
            new SoundModePipeline(
                new EveryKeyTrigger(),
                new ShuffleQueueSelector(_clips, context.Random),
                new PolyphonicPolicy(context.Audio));
    }
}
