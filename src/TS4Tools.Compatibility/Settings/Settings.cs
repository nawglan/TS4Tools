// Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 23-53

namespace s4pi.Settings;

/// <summary>
/// Holds global settings. Forwards to <see cref="global::TS4Tools.LibrarySettings"/>, so TS4Tools and legacy
/// code observe the same values.
/// </summary>
public static class Settings
{
    /// <summary>
    /// When true, run extra checks as part of normal operation.
    /// </summary>
    public static bool Checking => global::TS4Tools.LibrarySettings.Checking;

    /// <summary>
    /// When true, assume data is dirty regardless of tracking.
    /// </summary>
    public static bool AsBytesWorkaround => global::TS4Tools.LibrarySettings.AsBytesWorkaround;

    /// <summary>
    /// Indicate whether the wrapper should use TS4 format to decode files.
    /// </summary>
    public static bool IsTS4 => global::TS4Tools.LibrarySettings.IsTS4;
}
