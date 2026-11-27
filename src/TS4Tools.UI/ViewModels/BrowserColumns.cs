namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Sort direction of the browser list, using the values of the legacy WinForms SortOrder enum
/// so persisted settings stay compatible.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/SortingListviews.cs lines 48-52
/// (ColumnToSort / OrderOfSort loaded from and saved to the SortOrder setting).
/// </remarks>
public enum BrowserSortOrder
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// The browser list columns and the helpers that persist their widths.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 584-621 (SetColumns:
/// Name, Tag, then the resource index entry fields with "Resource" removed from their names) and
/// lines 46-83 (ColumnWidths setting, a ':'-separated list of widths).
/// The legacy Unknown2 column is not shown: the index entry interface has no such field.
/// </remarks>
public static class BrowserColumns
{
    public const string Name = "Name";
    public const string Tag = "Tag";
    public const string Type = "Type";
    public const string Group = "Group";
    public const string Instance = "Instance";
    public const string ChunkOffset = "Chunkoffset";
    public const string FileSize = "Filesize";
    public const string MemorySize = "Memsize";
    public const string Compressed = "Compressed";

    /// <summary>All browser columns in display order; the index is the legacy ColumnToSort value.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Name, Tag, Type, Group, Instance, ChunkOffset, FileSize, MemorySize, Compressed];

    /// <summary>
    /// The columns the filter offers: every column except Chunkoffset, Filesize and Memsize.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 58-59, 112-120</remarks>
    public static IReadOnlyList<string> FilterFields { get; } = [Name, Tag, Type, Group, Instance, Compressed];

    /// <summary>Columns whose filter is an exact (whole value) match rather than a sub-string match.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs lines 54, 77</remarks>
    private static readonly FrozenSet<string> NonExactFields = new[] { Name, Tag }.ToFrozenSet();

    public static bool IsExactFilter(string column) => !NonExactFields.Contains(column);

    /// <summary>The default width of a column when no width has been saved.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 598, 604, 614</remarks>
    public static double DefaultWidth(string column) => column switch
    {
        // Legacy pixel widths were for an 8pt WinForms font; scaled for the larger Avalonia default font.
        Name => 160,
        Tag => 70,
        Instance => 220,
        _ => 125
    };

    /// <summary>
    /// Parses the ':'-separated width list; entries that are missing, invalid or not positive are
    /// returned as -1 (use the default).
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 46-60</remarks>
    public static double[] ParseWidths(string? setting)
    {
        if (string.IsNullOrEmpty(setting)) return [];

        var parts = setting.Split(':');
        var widths = new double[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            widths[i] = double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && w > 0
                ? w
                : -1;
        }

        return widths;
    }

    /// <summary>Formats widths for the ColumnWidths setting.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 80-89</remarks>
    public static string FormatWidths(IEnumerable<double> widths)
        => string.Join(':', widths.Select(w => Math.Round(w).ToString(CultureInfo.InvariantCulture)));

    /// <summary>Gets the width to apply to the column at <paramref name="index"/>.</summary>
    public static double WidthFor(double[] saved, int index)
        => index < saved.Length && saved[index] > 0 ? saved[index] : DefaultWidth(All[index]);
}
