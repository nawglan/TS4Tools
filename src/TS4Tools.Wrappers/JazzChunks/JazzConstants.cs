// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs

// Suppress CA1711 (naming convention for Flags) - these match legacy s4pi naming
// Suppress CA1069 (duplicate enum values) - legacy uses aliases for semantic clarity
#pragma warning disable CA1711
#pragma warning disable CA1069

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Constants for Jazz animation state machine resources.
/// Source: JazzResource.cs - all Jazz chunk type IDs
/// </summary>
public static class JazzConstants
{
    // Markers
    public const uint DeadBeef = 0xDEADBEEF;
    public const uint CloseDgn = 0x4E47442F; // "/DGN" in little-endian

    // Jazz chunk type IDs
    public const uint StateMachine = 0x02D5DF13;           // S_SM
    public const uint State = 0x02EEDAFE;                   // S_St (note: legacy code shows S_St for State, but returns 0x02EEDAFE)
    public const uint DecisionGraph = 0x02EEDB18;           // S_DG
    public const uint ActorDefinition = 0x02EEDB2F;         // S_AD
    public const uint ParameterDefinition = 0x02EEDB46;     // S_PD
    public const uint PlayAnimationNode = 0x02EEDB5F;       // Play
    public const uint RandomNode = 0x02EEDB70;              // Rand
    public const uint SelectOnParameterNode = 0x02EEDB92;   // SoPn
    public const uint SelectOnDestinationNode = 0x02EEDBA5; // DG00
    public const uint NextStateNode = 0x02EEEBDC;           // SNSN
    public const uint CreatePropNode = 0x02EEEBDD;          // Prop
    public const uint ActorOperationNode = 0x02EEEBDE;      // AcOp
    public const uint StopAnimationNode = 0x0344D438;       // Stop

    /// <summary>
    /// All Jazz chunk type IDs.
    /// </summary>
    public static readonly uint[] AllTypes =
    [
        StateMachine,
        State,
        DecisionGraph,
        ActorDefinition,
        ParameterDefinition,
        PlayAnimationNode,
        RandomNode,
        SelectOnParameterNode,
        SelectOnDestinationNode,
        NextStateNode,
        CreatePropNode,
        ActorOperationNode,
        StopAnimationNode,
    ];
}

/// <summary>
/// Animation priority levels.
/// Source: JazzResource.cs lines 68-87
/// </summary>
public enum JazzAnimationPriority : uint
{
    Default = 0xFFFFFFFE,    // -2
    Broadcast = 0xFFFFFFFF,  // -1
    Unset = 0,
    Low = 6000,
    LowPlus = 8000,
    Normal = 10000,
    NormalPlus = 15000,
    FacialIdle = 17500,
    High = 20000,
    HighPlus = 25000,
    CarryRight = 30000,
    CarryRightPlus = 35000,
    CarryLeft = 40000,
    CarryLeftPlus = 45000,
    Ultra = 50000,
    UltraPlus = 55000,
    LookAt = 60000,
}

/// <summary>
/// Awareness overlay levels.
/// Source: JazzResource.cs lines 89-98
/// </summary>
public enum JazzAwarenessLevel : uint
{
    ThoughtBubble = 0,
    OverlayFace = 1,
    OverlayHead = 2,
    OverlayBothArms = 3,
    OverlayUpperbody = 4,
    OverlayNone = 5,
    Unset = 6,
}

/// <summary>
/// State machine flags.
/// Source: JazzResource.cs lines 139-147
/// </summary>
[Flags]
public enum JazzStateMachineFlags : uint
{
    Default = 0x01,
    UnilateralActor = 0x01,  // Alias for Default in legacy
    PinAllResources = 0x02,
    BlendMotionAccumulation = 0x04,
    HoldAllPoses = 0x08,
}

/// <summary>
/// State flags.
/// Source: JazzResource.cs lines 390-403
/// </summary>
[Flags]
public enum JazzStateFlags : uint
{
    None = 0x0000,
    Public = 0x0001,
    Entry = 0x0002,
    Exit = 0x0004,
    Loop = 0x0008,
    OneShot = 0x0010,
    OneShotHold = 0x0020,
    Synchronized = 0x0040,
    Join = 0x0080,
    Explicit = 0x0100,
}

/// <summary>
/// Animation node flags.
/// Source: JazzResource.cs lines 750-776
/// </summary>
[Flags]
public enum JazzAnimationNodeFlags : uint
{
    TimingNormal = 0x00,
    Default = 0x01,
    AtEnd = 0x01,              // Alias for Default in legacy
    LoopAsNeeded = 0x02,
    OverridePriority = 0x04,
    Mirror = 0x08,
    OverrideMirror = 0x10,
    OverrideTiming0 = 0x20,
    OverrideTiming1 = 0x40,
    TimingMaster = 0x20,       // Alias for OverrideTiming0 in legacy
    TimingSlave = 0x40,        // Alias for OverrideTiming1 in legacy
    TimingIgnored = 0x60,
    TimingMask = 0x60,         // Alias for TimingIgnored in legacy
    Interruptible = 0x80,
    ForceBlend = 0x100,
    UseTimingPriority = 0x200,
    UseTimingPriorityAsClockMaster = 0x400,
    BaseClipIsSocial = 0x800,
    AdditiveClipIsSocial = 0x1000,
    BaseClipIsObjectOnly = 0x2000,
    AdditiveClipIsObjectOnly = 0x4000,
    HoldPose = 0x8000,
    BlendMotionAccumulation = 0x10000,
}

/// <summary>
/// Random node flags.
/// Source: JazzResource.cs lines 1271-1276
/// </summary>
[Flags]
public enum JazzRandomNodeFlags : uint
{
    None = 0x00,
    AvoidRepeats = 0x01,
}

/// <summary>
/// Actor operation types.
/// Source: JazzResource.cs lines 2081-2085
/// </summary>
public enum JazzActorOperation : uint
{
    None = 0,
    SetMirror = 1,
}

#pragma warning restore CA1069
#pragma warning restore CA1711
