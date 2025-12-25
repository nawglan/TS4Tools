using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.Package;

namespace TS4Tools.UI.Views.Dialogs;

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

        FilePathText.Text = package.FilePath ?? "(No file path)";
        VersionText.Text = $"{package.MajorVersion}.{package.MinorVersion}";
        TotalResourcesText.Text = package.ResourceCount.ToString("N0", CultureInfo.InvariantCulture);

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
