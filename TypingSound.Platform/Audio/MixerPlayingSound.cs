using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TypingSound.Core.Abstractions;

namespace TypingSound.Platform.Audio;

/// <summary>Handle to a single sound submitted to the mixer. <see cref="Halt"/> removes it from the mixer.</summary>
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
