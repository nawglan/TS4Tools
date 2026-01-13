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
    /// <summary>DEADBEEF marker used in Jazz chunks.</summary>
    public const uint DeadBeef = 0xDEADBEEF;

    /// <summary>CloseDGN marker ("/DGN") used in Jazz chunks.</summary>
    public const uint CloseDgn = 0x4E47442F; // "/DGN" in little-endian

    // Jazz chunk type IDs
    /// <summary>State Machine chunk type (S_SM).</summary>
    public const uint StateMachine = 0x02D5DF13;           // S_SM

    /// <summary>State chunk type (S_St).</summary>
    public const uint State = 0x02EEDAFE;                   // S_St (note: legacy code shows S_St for State, but returns 0x02EEDAFE)

    /// <summary>Decision Graph chunk type (S_DG).</summary>
    public const uint DecisionGraph = 0x02EEDB18;           // S_DG

    /// <summary>Actor Definition chunk type (S_AD).</summary>
    public const uint ActorDefinition = 0x02EEDB2F;         // S_AD

    /// <summary>Parameter Definition chunk type (S_PD).</summary>
    public const uint ParameterDefinition = 0x02EEDB46;     // S_PD

    /// <summary>Play Animation Node chunk type (Play).</summary>
    public const uint PlayAnimationNode = 0x02EEDB5F;       // Play

    /// <summary>Random Node chunk type (Rand).</summary>
    public const uint RandomNode = 0x02EEDB70;              // Rand

    /// <summary>Select On Parameter Node chunk type (SoPn).</summary>
    public const uint SelectOnParameterNode = 0x02EEDB92;   // SoPn

    /// <summary>Select On Destination Node chunk type (DG00).</summary>
    public const uint SelectOnDestinationNode = 0x02EEDBA5; // DG00

    /// <summary>Next State Node chunk type (SNSN).</summary>
    public const uint NextStateNode = 0x02EEEBDC;           // SNSN

    /// <summary>Create Prop Node chunk type (Prop).</summary>
    public const uint CreatePropNode = 0x02EEEBDD;          // Prop

    /// <summary>Actor Operation Node chunk type (AcOp).</summary>
    public const uint ActorOperationNode = 0x02EEEBDE;      // AcOp

    /// <summary>Stop Animation Node chunk type (Stop).</summary>
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
    /// <summary>Default priority (-2).</summary>
    Default = 0xFFFFFFFE,    // -2

    /// <summary>Broadcast priority (-1).</summary>
    Broadcast = 0xFFFFFFFF,  // -1

    /// <summary>Unset priority (0).</summary>
    Unset = 0,

    /// <summary>Low priority (6000).</summary>
    Low = 6000,

    /// <summary>Low plus priority (8000).</summary>
    LowPlus = 8000,

    /// <summary>Normal priority (10000).</summary>
    Normal = 10000,

    /// <summary>Normal plus priority (15000).</summary>
    NormalPlus = 15000,

    /// <summary>Facial idle priority (17500).</summary>
    FacialIdle = 17500,

    /// <summary>High priority (20000).</summary>
    High = 20000,

    /// <summary>High plus priority (25000).</summary>
    HighPlus = 25000,

    /// <summary>Carry right priority (30000).</summary>
    CarryRight = 30000,

    /// <summary>Carry right plus priority (35000).</summary>
    CarryRightPlus = 35000,

    /// <summary>Carry left priority (40000).</summary>
    CarryLeft = 40000,

    /// <summary>Carry left plus priority (45000).</summary>
    CarryLeftPlus = 45000,

    /// <summary>Ultra priority (50000).</summary>
    Ultra = 50000,

    /// <summary>Ultra plus priority (55000).</summary>
    UltraPlus = 55000,

    /// <summary>Look at priority (60000).</summary>
    LookAt = 60000,
}

/// <summary>
/// Awareness overlay levels.
/// Source: JazzResource.cs lines 89-98
/// </summary>
public enum JazzAwarenessLevel : uint
{
    /// <summary>Thought bubble overlay.</summary>
    ThoughtBubble = 0,

    /// <summary>Face overlay.</summary>
    OverlayFace = 1,

    /// <summary>Head overlay.</summary>
    OverlayHead = 2,

    /// <summary>Both arms overlay.</summary>
    OverlayBothArms = 3,

    /// <summary>Upper body overlay.</summary>
    OverlayUpperbody = 4,

    /// <summary>No overlay.</summary>
    OverlayNone = 5,

    /// <summary>Unset awareness level.</summary>
    Unset = 6,
}

/// <summary>
/// State machine flags.
/// Source: JazzResource.cs lines 139-147
/// </summary>
[Flags]
public enum JazzStateMachineFlags : uint
{
    /// <summary>Default flag.</summary>
    Default = 0x01,

    /// <summary>Unilateral actor flag (alias for Default).</summary>
    UnilateralActor = 0x01,  // Alias for Default in legacy

    /// <summary>Pin all resources flag.</summary>
    PinAllResources = 0x02,

    /// <summary>Blend motion accumulation flag.</summary>
    BlendMotionAccumulation = 0x04,

    /// <summary>Hold all poses flag.</summary>
    HoldAllPoses = 0x08,
}

/// <summary>
/// State flags.
/// Source: JazzResource.cs lines 390-403
/// </summary>
[Flags]
public enum JazzStateFlags : uint
{
    /// <summary>No flags.</summary>
    None = 0x0000,

    /// <summary>Public state flag.</summary>
    Public = 0x0001,

    /// <summary>Entry state flag.</summary>
    Entry = 0x0002,

    /// <summary>Exit state flag.</summary>
    Exit = 0x0004,

    /// <summary>Loop state flag.</summary>
    Loop = 0x0008,

    /// <summary>One-shot state flag.</summary>
    OneShot = 0x0010,

    /// <summary>One-shot hold state flag.</summary>
    OneShotHold = 0x0020,

    /// <summary>Synchronized state flag.</summary>
    Synchronized = 0x0040,

    /// <summary>Join state flag.</summary>
    Join = 0x0080,

    /// <summary>Explicit state flag.</summary>
    Explicit = 0x0100,
}

/// <summary>
/// Animation node flags.
/// Source: JazzResource.cs lines 750-776
/// </summary>
[Flags]
public enum JazzAnimationNodeFlags : uint
{
    /// <summary>Normal timing flag.</summary>
    TimingNormal = 0x00,

    /// <summary>Default flag.</summary>
    Default = 0x01,

    /// <summary>At end flag (alias for Default).</summary>
    AtEnd = 0x01,              // Alias for Default in legacy

    /// <summary>Loop as needed flag.</summary>
    LoopAsNeeded = 0x02,

    /// <summary>Override priority flag.</summary>
    OverridePriority = 0x04,

    /// <summary>Mirror animation flag.</summary>
    Mirror = 0x08,

    /// <summary>Override mirror flag.</summary>
    OverrideMirror = 0x10,

    /// <summary>Override timing 0 flag.</summary>
    OverrideTiming0 = 0x20,

    /// <summary>Override timing 1 flag.</summary>
    OverrideTiming1 = 0x40,

    /// <summary>Timing master flag (alias for OverrideTiming0).</summary>
    TimingMaster = 0x20,       // Alias for OverrideTiming0 in legacy

    /// <summary>Timing slave flag (alias for OverrideTiming1).</summary>
    TimingSlave = 0x40,        // Alias for OverrideTiming1 in legacy

    /// <summary>Timing ignored flag.</summary>
    TimingIgnored = 0x60,

    /// <summary>Timing mask flag (alias for TimingIgnored).</summary>
    TimingMask = 0x60,         // Alias for TimingIgnored in legacy

    /// <summary>Interruptible animation flag.</summary>
    Interruptible = 0x80,

    /// <summary>Force blend flag.</summary>
    ForceBlend = 0x100,

    /// <summary>Use timing priority flag.</summary>
    UseTimingPriority = 0x200,

    /// <summary>Use timing priority as clock master flag.</summary>
    UseTimingPriorityAsClockMaster = 0x400,

    /// <summary>Base clip is social flag.</summary>
    BaseClipIsSocial = 0x800,

    /// <summary>Additive clip is social flag.</summary>
    AdditiveClipIsSocial = 0x1000,

    /// <summary>Base clip is object only flag.</summary>
    BaseClipIsObjectOnly = 0x2000,

    /// <summary>Additive clip is object only flag.</summary>
    AdditiveClipIsObjectOnly = 0x4000,

    /// <summary>Hold pose flag.</summary>
    HoldPose = 0x8000,

    /// <summary>Blend motion accumulation flag.</summary>
    BlendMotionAccumulation = 0x10000,
}

/// <summary>
/// Random node flags.
/// Source: JazzResource.cs lines 1271-1276
/// </summary>
[Flags]
public enum JazzRandomNodeFlags : uint
{
    /// <summary>No flags.</summary>
    None = 0x00,

    /// <summary>Avoid repeats flag.</summary>
    AvoidRepeats = 0x01,
}

/// <summary>
/// Actor operation types.
/// Source: JazzResource.cs lines 2081-2085
/// </summary>
public enum JazzActorOperation : uint
{
    /// <summary>No operation.</summary>
    None = 0,

    /// <summary>Set mirror operation.</summary>
    SetMirror = 1,
}

#pragma warning restore CA1069
#pragma warning restore CA1711
