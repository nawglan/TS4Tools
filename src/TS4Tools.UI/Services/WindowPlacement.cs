namespace TS4Tools.UI.Services;

/// <summary>Where the main window and its splitters should be when the application starts.</summary>
/// <param name="Width">Window width.</param>
/// <param name="Height">Window height.</param>
/// <param name="X">Window left edge, or null to centre the window.</param>
/// <param name="Y">Window top edge, or null to centre the window.</param>
/// <param name="Maximized">Whether the window starts maximized.</param>
/// <param name="BrowserWidth">Width of the browser column, or null for the default.</param>
/// <param name="PackageInfoHeight">Height of the package information panel, or null for the default.</param>
public readonly record struct WindowPlacementResult(
    double Width,
    double Height,
    int? X,
    int? Y,
    bool Maximized,
    double? BrowserWidth,
    double? PackageInfoHeight);

/// <summary>
/// Restores and captures the main window size, position, state and splitter positions.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 146-199 (MainForm_LoadFormSettings and
/// MainForm_SaveSettings). The saved state uses the WinForms FormWindowState values (0 normal,
/// 1 minimized, 2 maximized); a minimized or unset state means "use the defaults", which
/// legacy relies on so a window that was last iconised is never restored off screen.
/// </remarks>
public static class WindowPlacement
{
    public const int Normal = 0;
    public const int Minimized = 1;
    public const int Maximized = 2;

    /// <summary>Works out the placement from the saved settings.</summary>
    /// <param name="settings">The saved settings.</param>
    /// <param name="screenWidth">Width of the primary screen's working area.</param>
    /// <param name="screenHeight">Height of the primary screen's working area.</param>
    public static WindowPlacementResult Resolve(AppSettings settings, double screenWidth, double screenHeight)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Source: MainForm.cs lines 153-154 (four fifths of the working area)
        var defaultWidth = 4 * screenWidth / 5;
        var defaultHeight = 4 * screenHeight / 5;

        if (settings.WindowState is not (Normal or Maximized))
        {
            return new WindowPlacementResult(defaultWidth, defaultHeight, null, null, false, null, null);
        }

        // Source: MainForm.cs lines 172-187 (negative values mean "not saved")
        return new WindowPlacementResult(
            settings.WindowWidth < 0 ? defaultWidth : settings.WindowWidth,
            settings.WindowHeight < 0 ? defaultHeight : settings.WindowHeight,
            settings.WindowX < 0 || settings.WindowY < 0 ? null : settings.WindowX,
            settings.WindowX < 0 || settings.WindowY < 0 ? null : settings.WindowY,
            settings.WindowState == Maximized,
            settings.Splitter1Position < 0 ? null : settings.Splitter1Position,
            settings.Splitter2Position < 0 ? null : settings.Splitter2Position);
    }

    /// <summary>Stores the window state and splitter positions into the settings.</summary>
    /// <param name="settings">The settings to update.</param>
    /// <param name="width">Width of the window when not maximized.</param>
    /// <param name="height">Height of the window when not maximized.</param>
    /// <param name="x">Left edge of the window when not maximized.</param>
    /// <param name="y">Top edge of the window when not maximized.</param>
    /// <param name="state">Window state: 0 normal, 1 minimized, 2 maximized.</param>
    /// <param name="browserWidth">Width of the browser column.</param>
    /// <param name="packageInfoHeight">Height of the package information panel.</param>
    /// <remarks>Source: MainForm.cs lines 191-198 (MainForm_SaveSettings)</remarks>
    public static void Capture(
        AppSettings settings,
        double width,
        double height,
        int x,
        int y,
        int state,
        double browserWidth,
        double packageInfoHeight)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.WindowState = state;
        settings.WindowWidth = (int)Math.Round(width);
        settings.WindowHeight = (int)Math.Round(height);
        settings.WindowX = x;
        settings.WindowY = y;
        settings.Splitter1Position = (int)Math.Round(browserWidth);
        settings.Splitter2Position = (int)Math.Round(packageInfoHeight);
    }
}
