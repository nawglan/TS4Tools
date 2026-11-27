// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/S3CLIP.cs lines 232-732 (F1PaletteList, F1Palette, S3ChannelList, S3Channel, FrameList, Frame),
// lines 734-824 (ChannelType, SubTargetType enums), lines 826-906 (GetWidthForChannelType, GetCountForChannelType)

namespace TS4Tools.Wrappers;

/// <summary>Channel data encoding. Source: S3CLIP.ChannelType, S3CLIP.cs lines 736-766.</summary>
public enum ClipChannelType : byte
{
    /// <summary>Unknown.</summary>
    Unknown = 0,
    /// <summary>F1.</summary>
    F1,
    /// <summary>F2.</summary>
    F2,
    /// <summary>F3.</summary>
    F3,
    /// <summary>F4.</summary>
    F4,
    /// <summary>F1Normalized.</summary>
    F1Normalized,
    /// <summary>F2Normalized.</summary>
    F2Normalized,
    /// <summary>F3Normalized.</summary>
    F3Normalized,
    /// <summary>F4Normalized.</summary>
    F4Normalized,
    /// <summary>F1Zero.</summary>
    F1Zero,
    /// <summary>F2Zero.</summary>
    F2Zero,
    /// <summary>F3Zero.</summary>
    F3Zero,
    /// <summary>F4Zero.</summary>
    F4Zero,
    /// <summary>F1One.</summary>
    F1One,
    /// <summary>F2One.</summary>
    F2One,
    /// <summary>F3One.</summary>
    F3One,
    /// <summary>F4One.</summary>
    F4One,
    /// <summary>F4QuaternionIdentity.</summary>
    F4QuaternionIdentity,
    /// <summary>F3HighPrecisionNormalized.</summary>
    F3HighPrecisionNormalized,
    /// <summary>F4HighPrecisionNormalizedQuaternion.</summary>
    F4HighPrecisionNormalizedQuaternion,
    /// <summary>F4SuperHighPrecisionQuaternion.</summary>
    F4SuperHighPrecisionQuaternion,
    /// <summary>F3HighPrecisionNormalizedQuaternion.</summary>
    F3HighPrecisionNormalizedQuaternion,
}

/// <summary>Channel sub-target. Source: S3CLIP.SubTargetType, S3CLIP.cs lines 768-822.</summary>
public enum ClipSubTargetType : byte
{
    /// <summary>Unknown.</summary>
    Unknown = 0,
    /// <summary>Translation.</summary>
    Translation,
    /// <summary>Orientation.</summary>
    Orientation,
    /// <summary>Scale.</summary>
    Scale,
    /// <summary>TranslationX.</summary>
    TranslationX,
    /// <summary>TranslationY.</summary>
    TranslationY,
    /// <summary>TranslationZ.</summary>
    TranslationZ,
    /// <summary>OrientationX.</summary>
    OrientationX,
    /// <summary>OrientationY.</summary>
    OrientationY,
    /// <summary>OrientationZ.</summary>
    OrientationZ,
    /// <summary>OrientationW.</summary>
    OrientationW,
    /// <summary>ScaleX.</summary>
    ScaleX,
    /// <summary>ScaleY.</summary>
    ScaleY,
    /// <summary>ScaleZ.</summary>
    ScaleZ,
    /// <summary>IKTargetWeightWorld.</summary>
    IKTargetWeightWorld,
    /// <summary>IKTargetWeight1.</summary>
    IKTargetWeight1,
    /// <summary>IKTargetWeight2.</summary>
    IKTargetWeight2,
    /// <summary>IKTargetWeight3.</summary>
    IKTargetWeight3,
    /// <summary>IKTargetWeight4.</summary>
    IKTargetWeight4,
    /// <summary>IKTargetWeight5.</summary>
    IKTargetWeight5,
    /// <summary>IKTargetWeight6.</summary>
    IKTargetWeight6,
    /// <summary>IKTargetWeight7.</summary>
    IKTargetWeight7,
    /// <summary>IKTargetWeight8.</summary>
    IKTargetWeight8,
    /// <summary>IKTargetWeight9.</summary>
    IKTargetWeight9,
    /// <summary>IKTargetWeight10.</summary>
    IKTargetWeight10,
    /// <summary>IKTargetOffsetTranslationWorld.</summary>
    IKTargetOffsetTranslationWorld,
    /// <summary>IKTargetOffsetOrientationWorld.</summary>
    IKTargetOffsetOrientationWorld,
    /// <summary>IKTargetOffsetTranslation1.</summary>
    IKTargetOffsetTranslation1,
    /// <summary>IKTargetOffsetOrientation1.</summary>
    IKTargetOffsetOrientation1,
    /// <summary>IKTargetOffsetTranslation2.</summary>
    IKTargetOffsetTranslation2,
    /// <summary>IKTargetOffsetOrientation2.</summary>
    IKTargetOffsetOrientation2,
    /// <summary>IKTargetOffsetTranslation3.</summary>
    IKTargetOffsetTranslation3,
    /// <summary>IKTargetOffsetOrientation3.</summary>
    IKTargetOffsetOrientation3,
    /// <summary>IKTargetOffsetTranslation4.</summary>
    IKTargetOffsetTranslation4,
    /// <summary>IKTargetOffsetOrientation4.</summary>
    IKTargetOffsetOrientation4,
    /// <summary>IKTargetOffsetTranslation5.</summary>
    IKTargetOffsetTranslation5,
    /// <summary>IKTargetOffsetOrientation5.</summary>
    IKTargetOffsetOrientation5,
    /// <summary>IKTargetOffsetTranslation6.</summary>
    IKTargetOffsetTranslation6,
    /// <summary>IKTargetOffsetOrientation6.</summary>
    IKTargetOffsetOrientation6,
    /// <summary>IKTargetOffsetTranslation7.</summary>
    IKTargetOffsetTranslation7,
    /// <summary>IKTargetOffsetOrientation7.</summary>
    IKTargetOffsetOrientation7,
    /// <summary>IKTargetOffsetTranslation8.</summary>
    IKTargetOffsetTranslation8,
    /// <summary>IKTargetOffsetOrientation8.</summary>
    IKTargetOffsetOrientation8,
    /// <summary>IKTargetOffsetTranslation9.</summary>
    IKTargetOffsetTranslation9,
    /// <summary>IKTargetOffsetOrientation9.</summary>
    IKTargetOffsetOrientation9,
    /// <summary>IKTargetOffsetTranslation10.</summary>
    IKTargetOffsetTranslation10,
    /// <summary>IKTargetOffsetOrientation10.</summary>
    IKTargetOffsetOrientation10,
}

/// <summary>
/// A single frame of an S3CLIP channel.
/// </summary>
/// <remarks>Source: S3CLIP.Frame, S3CLIP.cs lines 591-730.</remarks>
public sealed class ClipFrame
{
    /// <summary>Tick at which this frame starts.</summary>
    public ushort StartTick { get; set; }

    /// <summary>Frame flags.</summary>
    public ushort Flags { get; set; }

    /// <summary>Palette/quantised indices; length depends on the channel type.</summary>
    public ushort[] Indices { get; set; } = [];

    /// <summary>
    /// Pad byte following an odd-length 8-bit index run (real data uses either 0x00 or 0xFF; preserved for exact round-trip).
    /// </summary>
    public byte Padding { get; set; } = 0xFF;
}

/// <summary>
/// A channel (track) of an S3CLIP animation.
/// </summary>
/// <remarks>Source: S3CLIP.S3Channel, S3CLIP.cs lines 376-516.</remarks>
public sealed class ClipChannel
{
    /// <summary>Size in bytes of a channel table entry. Source: S3CLIP.cs lines 447-489.</summary>
    internal const int EntrySize = 20;

    /// <summary>Target (bone/IK) hash.</summary>
    public uint Target { get; set; }

    /// <summary>Value offset.</summary>
    public float Offset { get; set; }

    /// <summary>Value scale.</summary>
    public float Scale { get; set; }

    /// <summary>Channel data encoding.</summary>
    public ClipChannelType ChannelType { get; set; }

    /// <summary>Channel sub-target.</summary>
    public ClipSubTargetType SubTarget { get; set; }

    /// <summary>Frames of this channel.</summary>
    public List<ClipFrame> Frames { get; } = [];

    /// <summary>Source: S3CLIP.GetWidthForChannelType, S3CLIP.cs lines 827-863.</summary>
    internal static uint GetWidth(ClipChannelType t) => t switch
    {
        ClipChannelType.F1Normalized or ClipChannelType.F2Normalized
            or ClipChannelType.F3Normalized or ClipChannelType.F4Normalized => 1,
        ClipChannelType.F3HighPrecisionNormalized or ClipChannelType.F4HighPrecisionNormalizedQuaternion
            or ClipChannelType.F3HighPrecisionNormalizedQuaternion => 10,
        ClipChannelType.F1 or ClipChannelType.F2 or ClipChannelType.F3 or ClipChannelType.F4
            or ClipChannelType.F4SuperHighPrecisionQuaternion => 2,
        _ => 0,
    };

    /// <summary>Source: S3CLIP.GetCountForChannelType, S3CLIP.cs lines 865-905.</summary>
    internal static uint GetCount(ClipChannelType t) => t switch
    {
        ClipChannelType.F1 or ClipChannelType.F1Normalized => 1,
        ClipChannelType.F2 or ClipChannelType.F2Normalized => 2,
        ClipChannelType.F3 or ClipChannelType.F3Normalized or ClipChannelType.F3HighPrecisionNormalized => 3,
        ClipChannelType.F4 or ClipChannelType.F4Normalized or ClipChannelType.F4SuperHighPrecisionQuaternion
            or ClipChannelType.F4HighPrecisionNormalizedQuaternion
            or ClipChannelType.F3HighPrecisionNormalizedQuaternion => 4,
        _ => 0,
    };
}
