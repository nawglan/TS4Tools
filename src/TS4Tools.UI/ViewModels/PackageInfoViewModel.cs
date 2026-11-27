using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Package;

namespace TS4Tools.UI.ViewModels;

/// <summary>A name/value row of the package information panel.</summary>
public sealed record PackageInfoRow(string Field, string Value);

/// <summary>
/// The package header fields shown beside the browser.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs lines 27-87 and
/// PackageInfoFields.cs lines 28-49 (every content field of APackage except those containing
/// "Stream" or "List", labelled with the field name; integers are shown as 0x{X8} the way
/// TypedValue formats them, legacy_references/Sims4Tools/s4pi/Interfaces/TypedValue.cs lines 95-135).
/// </remarks>
public sealed class PackageInfoViewModel : ObservableObject
{
    /// <summary>The fields in legacy display order.</summary>
    public static IReadOnlyList<string> FieldNames { get; } =
    [
        "Magic", "Major", "Minor", "UserVersionMajor", "UserVersionMinor", "Unused1", "CreationTime",
        "UpdatedTime", "Unused2", "Indexcount", "IndexRecordPositionLow", "Indexsize", "Unused3",
        "Unused4", "Indexposition", "Unused5", "Indextype"
    ];

    private static readonly IReadOnlyList<PackageInfoRow> EmptyRows =
        [.. FieldNames.Select(f => new PackageInfoRow(f, string.Empty))];

    private IReadOnlyList<PackageInfoRow> _rows = EmptyRows;

    /// <summary>The rows to display; values are empty while no package is open.</summary>
    public IReadOnlyList<PackageInfoRow> Rows
    {
        get => _rows;
        private set => SetProperty(ref _rows, value);
    }

    /// <summary>Reads the header fields from <paramref name="package"/>, or clears them when null.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs lines 66-87 (Package setter)</remarks>
    public void Update(DbpfPackage? package)
    {
        if (package == null)
        {
            Rows = EmptyRows;
            return;
        }

        Rows =
        [
            new("Magic", Encoding.ASCII.GetString(package.Magic.Span)),
            new("Major", Int(package.MajorVersion)),
            new("Minor", Int(package.MinorVersion)),
            new("UserVersionMajor", Int(package.UserVersionMajor)),
            new("UserVersionMinor", Int(package.UserVersionMinor)),
            new("Unused1", Int(package.Unused1)),
            new("CreationTime", Int(package.CreationTime)),
            new("UpdatedTime", Int(package.UpdatedTime)),
            new("Unused2", Int(package.Unused2)),
            new("Indexcount", Int(package.HeaderIndexCount)),
            new("IndexRecordPositionLow", Int(package.IndexRecordPositionLow)),
            new("Indexsize", Int(package.HeaderIndexSize)),
            new("Unused3", Int(package.Unused3)),
            new("Unused4", Int(package.Unused4)),
            new("Indexposition", Int(package.HeaderIndexPosition)),
            new("Unused5", Convert.ToHexString(package.Unused5.Span)),
            new("Indextype", $"0x{package.IndexType:X8}")
        ];
    }

    private static string Int(int value) => $"0x{value:X8}";
}
