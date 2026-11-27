namespace TS4Tools;

/// <summary>
/// Global library settings, ported from the legacy s4pi settings class.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 28-52
///
/// Legacy exposes these as read-only statics that are always <c>true</c>. They are settable here so
/// tools and tests can opt out, but the defaults match legacy.
///
/// Where <see cref="Checking"/> gates behaviour (mirroring legacy <c>if (checking)</c> sites):
/// - Header magic/major/minor validation on open and after in-place save
///   (legacy Package.cs lines 83, 553 → CheckHeader lines 607-620).
/// - Decompressed size must equal the index MemorySize
///   (legacy Compression.cs lines 83-86).
/// The legacy WrapperDealer null-handler check (WrapperDealer.cs lines 125-126) has no equivalent: the
/// registry always has a non-null default factory. <see cref="AsBytesWorkaround"/> and <see cref="IsTS4"/>
/// are exposed for wrappers; Core does not branch on them.
/// Security bounds checks (counts, offsets, sizes) are never disabled by <see cref="Checking"/>.
/// </remarks>
public static class LibrarySettings
{
    /// <summary>
    /// When true, run extra checks as part of normal operation. Default: true.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 35-39</remarks>
    public static bool Checking { get; set; } = true;

    /// <summary>
    /// When true, assume data is dirty regardless of tracking. Default: true.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 41-45</remarks>
    public static bool AsBytesWorkaround { get; set; } = true;

    /// <summary>
    /// Indicates whether wrappers should use the TS4 format to decode files. Default: true.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 47-51</remarks>
    public static bool IsTS4 { get; set; } = true;
}
