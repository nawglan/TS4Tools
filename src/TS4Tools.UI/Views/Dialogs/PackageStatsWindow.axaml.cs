using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.Package;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Window displaying package statistics and header information.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs
/// </remarks>
public partial class PackageStatsWindow : Window
{
    private static readonly Dictionary<uint, string> KnownTypes = new()
    {
        { 0x220557DA, "String Table (STBL)" },
        { 0x0166038C, "Name Map" },
        { 0x545AC67A, "Audio (SNR)" },
        { 0x01D10F34, "Rig (BSRF)" },
        { 0x015A1849, "Geometry (GEOM)" },
        { 0x00B2D882, "Image (DST)" },
        { 0x00B00000, "Image (PNG)" },
        { 0x034AEECB, "CAS Part" },
        { 0x0418FE2A, "Catalog Object" },
        { 0x02D5DF13, "SimData" },
        { 0x6017E896, "Tuning (XML)" },
        { 0x73E93EEB, "Tuning Instance" }
    };

    public PackageStatsWindow() : this(null!) { }

    public PackageStatsWindow(DbpfPackage package)
    {
        InitializeComponent();
        if (package == null) return;

        // Basic info
        FilePathText.Text = package.FilePath ?? "(No file path)";
        TotalResourcesText.Text = package.ResourceCount.ToString("N0", CultureInfo.InvariantCulture);

        // Type breakdown
        var typeBreakdown = package.Resources
            .Where(r => !r.IsDeleted)
            .GroupBy(r => r.Key.ResourceType)
            .Select(g => new TypeBreakdownItem
            {
                TypeName = KnownTypes.TryGetValue(g.Key, out var name) ? name : "Unknown",
                TypeHex = $"0x{g.Key:X8}",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        TypeBreakdownGrid.ItemsSource = typeBreakdown;

        // Package Header info
        // Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs lines 85-86
        MajorVersionText.Text = package.MajorVersion.ToString(CultureInfo.InvariantCulture);
        MinorVersionText.Text = package.MinorVersion.ToString(CultureInfo.InvariantCulture);
        UserVersionText.Text = $"{package.UserVersionMajor}.{package.UserVersionMinor}";

        // Timestamps - interpret as Unix timestamp if non-zero
        CreationTimeText.Text = FormatTimestamp(package.CreationTime);
        UpdatedTimeText.Text = FormatTimestamp(package.UpdatedTime);

        // Index info
        IndexCountText.Text = package.HeaderIndexCount.ToString("N0", CultureInfo.InvariantCulture);
        IndexPositionText.Text = $"0x{package.HeaderIndexPosition:X8} ({package.HeaderIndexPosition:N0})";
        IndexSizeText.Text = $"{package.HeaderIndexSize:N0} bytes";

        // Read-only status
        ReadOnlyText.Text = package.IsReadOnly ? "Yes" : "No";
    }

    /// <summary>
    /// Formats a Unix timestamp, showing "Not set" for 0.
    /// </summary>
    private static string FormatTimestamp(int timestamp)
    {
        if (timestamp == 0) return "Not set";

        try
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        catch
        {
            return $"0x{timestamp:X8}";
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed class TypeBreakdownItem
    {
        public required string TypeName { get; init; }
        public required string TypeHex { get; init; }
        public required int Count { get; init; }
    }
}
