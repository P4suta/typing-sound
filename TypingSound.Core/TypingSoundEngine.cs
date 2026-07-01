using TypingSound.Core.Abstractions;
using TypingSound.Core.Modes;

namespace TypingSound.Core;

/// <summary>
/// Pipeline coordinator. Routes key presses (<see cref="NotifyKeyPressed"/>) to the running mode
/// instance and handles mode switching (<see cref="SwitchTo"/>). All methods assume a single (UI)
/// thread and hold no locks. Key presses are handled synchronously inline to minimize the delay from
/// keystroke to sound.
/// </summary>
public sealed class TypingSoundEngine : IDisposable
{
    private readonly SoundModeContext _context;
    private IActiveMode _active;

    /// <summary>Creates the engine with runtime services and an initial mode.</summary>
    /// <param name="context">Runtime services: audio, timers, random, etc.</param>
    /// <param name="initialMode">Mode active at startup.</param>
    public TypingSoundEngine(SoundModeContext context, ISoundMode initialMode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialMode);
        _context = context;
        CurrentMode = initialMode;
        _active = initialMode.Activate(context);
    }

    /// <summary>Current mode.</summary>
    public ISoundMode CurrentMode { get; private set; }

    /// <summary>Notifies that a single key was pressed (classification only).</summary>
    public void NotifyKeyPressed(KeyCategory category) => _active.OnKeyPressed(category);

    /// <summary>Switches the mode, disposing the old mode instance.</summary>
    public void SwitchTo(ISoundMode mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        _active.Dispose();
        CurrentMode = mode;
        _active = mode.Activate(_context);
    }

    /// <inheritdoc/>
    public void Dispose() => _active.Dispose();
}
