using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>ミキサーに投入された 1 音への操作ハンドル。<see cref="Stop"/> でミキサーから取り除く。</summary>
internal sealed class MixerPlayingSound : IPlayingSound
{
    private readonly MixingSampleProvider _mixer;
    private readonly ISampleProvider _input;

    public MixerPlayingSound(MixingSampleProvider mixer, ISampleProvider input)
    {
        _mixer = mixer;
        _input = input;
    }

    public void Halt() => _mixer.RemoveMixerInput(_input);
}
