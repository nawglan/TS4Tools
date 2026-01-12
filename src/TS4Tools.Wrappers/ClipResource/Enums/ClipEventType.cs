// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/ClipEvents.cs lines 629-657

namespace TS4Tools.Wrappers;

/// <summary>
/// Types of events that can occur during animation playback.
/// </summary>
public enum ClipEventType : uint
{
    /// <summary>Invalid or uninitialized event.</summary>
    Invalid = 0,

    /// <summary>Parent attachment event.</summary>
    Parent = 1,

    /// <summary>Unparent detachment event.</summary>
    UnParent = 2,

    /// <summary>Sound playback event.</summary>
    Sound = 3,

    /// <summary>Script execution event.</summary>
    Script = 4,

    /// <summary>Visual effect event.</summary>
    Effect = 5,

    /// <summary>Visibility change event.</summary>
    Visibility = 6,

    /// <summary>Deprecated event type 6.</summary>
    Deprecated6 = 7,

    /// <summary>Create prop event.</summary>
    CreateProp = 8,

    /// <summary>Destroy prop event.</summary>
    DestroyProp = 9,

    /// <summary>Stop effect event.</summary>
    StopEffect = 10,

    /// <summary>Block transition event.</summary>
    BlockTransition = 11,

    /// <summary>Snap/positioning event.</summary>
    Snap = 12,

    /// <summary>Reaction event.</summary>
    Reaction = 13,

    /// <summary>Double modifier sound event.</summary>
    DoubleModifierSound = 14,

    /// <summary>DSP interval event.</summary>
    DspInterval = 15,

    /// <summary>Material state change event.</summary>
    MaterialState = 16,

    /// <summary>Focus compatibility event.</summary>
    FocusCompatibility = 17,

    /// <summary>Suppress lip sync event.</summary>
    SuppressLipSync = 18,

    /// <summary>Censor event.</summary>
    Censor = 19,

    /// <summary>Simulation sound start event.</summary>
    SimulationSoundStart = 20,

    /// <summary>Simulation sound stop event.</summary>
    SimulationSoundStop = 21,

    /// <summary>Enable facial overlay event.</summary>
    EnableFacialOverlay = 22,

    /// <summary>Fade object event.</summary>
    FadeObject = 23,

    /// <summary>Disable object highlight event.</summary>
    DisableObjectHighlight = 24,

    /// <summary>Thigh target offset event.</summary>
    ThighTargetOffset = 25
}
