// Source: legacy_references/Sims4Tools/s4pi Wrappers/RigResource/RigResource.cs lines 156-161

namespace TS4Tools.Wrappers;

/// <summary>
/// Specifies the format of a RIG resource.
/// </summary>
public enum RigFormat
{
    /// <summary>
    /// Raw Granny2 data - passed through without parsing.
    /// </summary>
    RawGranny,

    /// <summary>
    /// Wrapped Granny format (treated as RawGranny in legacy).
    /// Detected when first 4 bytes == 0x8EAF13DE and next 4 == 0x00000000.
    /// </summary>
    WrappedGranny,

    /// <summary>
    /// Clear format - native parsed format with bones and IK chains.
    /// Detected when version is 3 or 4 with subversion 1 or 2.
    /// </summary>
    Clear
}
