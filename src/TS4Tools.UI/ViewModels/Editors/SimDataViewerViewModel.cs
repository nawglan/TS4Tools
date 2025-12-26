using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class SimDataViewerViewModel : ViewModelBase
{
    private const int HexPreviewBytes = 256;

    private SimDataResource? _resource;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _versionHex = string.Empty;

    [ObservableProperty]
    private int _schemaCount;

    [ObservableProperty]
    private int _tableCount;

    [ObservableProperty]
    private int _dataSize;

    [ObservableProperty]
    private string _headerInfo = string.Empty;

    [ObservableProperty]
    private string _hexPreview = string.Empty;

    [ObservableProperty]
    private SimDataSchemaViewModel? _selectedSchema;

    [ObservableProperty]
    private SimDataTableViewModel? _selectedTable;

    public ObservableCollection<SimDataSchemaViewModel> Schemas { get; } = [];
    public ObservableCollection<SimDataTableViewModel> Tables { get; } = [];

    public void LoadResource(SimDataResource resource)
    {
        _resource = resource;
        IsValid = resource.IsValid;
        VersionHex = $"0x{resource.Version:X8}";
        SchemaCount = resource.SchemaCount;
        TableCount = resource.TableCount;
        DataSize = resource.DataSize;

        BuildHeaderInfo(resource);
        BuildHexPreview(resource);
        LoadSchemas(resource);
        LoadTables(resource);
    }

    private void BuildHeaderInfo(SimDataResource resource)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Magic: DATA (0x{SimDataResource.Magic:X8})");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Version: {VersionHex}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Data Table Position: 0x{resource.DataTablePosition:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Structure Table Position: 0x{resource.StructureTablePosition:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Schema Count: {SchemaCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Table Count: {TableCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Data Size: {DataSize:N0} bytes");

        if (!IsValid)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING: Invalid or unsupported SimData format");
        }

        HeaderInfo = sb.ToString();
    }

    private void BuildHexPreview(SimDataResource resource)
    {
        var data = resource.RawData.Span;
        var previewLength = Math.Min(HexPreviewBytes, data.Length);
        var sb = new StringBuilder();

        sb.AppendLine("First bytes (hex dump):");
        sb.AppendLine();

        for (int offset = 0; offset < previewLength; offset += 16)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{offset:X8}  ");

            int lineBytes = Math.Min(16, previewLength - offset);
            for (int i = 0; i < 16; i++)
            {
                if (i < lineBytes)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"{data[offset + i]:X2} ");
                }
                else
                {
                    sb.Append("   ");
                }
                if (i == 7) sb.Append(' ');
            }

            sb.Append(' ');
            for (int i = 0; i < lineBytes; i++)
            {
                byte b = data[offset + i];
                char c = (b >= 32 && b < 127) ? (char)b : '.';
                sb.Append(c);
            }

            sb.AppendLine();
        }

        if (data.Length > previewLength)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"... and {data.Length - previewLength:N0} more bytes");
        }

        HexPreview = sb.ToString();
    }

    private void LoadSchemas(SimDataResource resource)
    {
        Schemas.Clear();

        foreach (var schema in resource.Schemas)
        {
            Schemas.Add(new SimDataSchemaViewModel(schema));
        }

        if (Schemas.Count > 0)
            SelectedSchema = Schemas[0];
    }

    private void LoadTables(SimDataResource resource)
    {
        Tables.Clear();

        foreach (var table in resource.Tables)
        {
            Tables.Add(new SimDataTableViewModel(table));
        }

        if (Tables.Count > 0)
            SelectedTable = Tables[0];
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}

/// <summary>
/// ViewModel for SimData schema (structure) display.
/// </summary>
public sealed class SimDataSchemaViewModel
{
    public int Index { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string NameHashHex { get; }
    public int ColumnCount { get; }
    public uint Size { get; }
    public ObservableCollection<SimDataFieldViewModel> Fields { get; } = [];

    public SimDataSchemaViewModel(SimDataSchema schema)
    {
        Index = schema.Index;
        Name = schema.Name;
        DisplayName = string.IsNullOrEmpty(schema.Name)
            ? $"Schema {schema.Index}"
            : schema.Name;
        NameHashHex = $"0x{schema.NameHash:X8}";
        ColumnCount = schema.ColumnCount;
        Size = schema.Size;

        foreach (var field in schema.Fields)
        {
            Fields.Add(new SimDataFieldViewModel(field));
        }
    }
}

/// <summary>
/// ViewModel for SimData field (column) display.
/// </summary>
public sealed class SimDataFieldViewModel
{
    public int Index { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string NameHashHex { get; }
    public string TypeName { get; }
    public uint TypeValue { get; }
    public uint DataOffset { get; }
    public int DataSize { get; }

    public SimDataFieldViewModel(SimDataField field)
    {
        Index = field.Index;
        Name = field.Name;
        DisplayName = string.IsNullOrEmpty(field.Name)
            ? $"Field {field.Index}"
            : field.Name;
        NameHashHex = $"0x{field.NameHash:X8}";
        TypeName = GetTypeName(field.Type, field.TypeValue);
        TypeValue = field.TypeValue;
        DataOffset = field.DataOffset;
        DataSize = field.DataSize;
    }

    private static string GetTypeName(SimDataFieldType type, uint rawValue)
    {
        return type switch
        {
            SimDataFieldType.Boolean => "Boolean",
            SimDataFieldType.Integer16 => "Int16",
            SimDataFieldType.FloatValue => "Float",
            SimDataFieldType.VFX => "VFX",
            SimDataFieldType.RGBColor => "RGB Color",
            SimDataFieldType.ARGBColor => "ARGB Color",
            SimDataFieldType.DataInstance => "Data Instance",
            SimDataFieldType.ImageInstance => "Image Instance",
            SimDataFieldType.StringInstance => "String Instance",
            SimDataFieldType.Unknown1 => "Unknown (0x07)",
            SimDataFieldType.Unknown2 => "Unknown (0x08)",
            SimDataFieldType.Unknown3 => "Unknown (0x0E)",
            _ => $"Unknown (0x{rawValue:X2})"
        };
    }
}

/// <summary>
/// ViewModel for SimData table (record) display.
/// </summary>
public sealed class SimDataTableViewModel
{
    public int Index { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string NameHashHex { get; }
    public string SchemaName { get; }
    public int RowCount { get; }
    public int DataLength { get; }

    public SimDataTableViewModel(SimDataTable table)
    {
        Index = table.Index;
        Name = table.Name;
        DisplayName = string.IsNullOrEmpty(table.Name)
            ? $"Table {table.Index}"
            : table.Name;
        NameHashHex = $"0x{table.NameHash:X8}";
        SchemaName = table.Schema?.Name ?? "(none)";
        RowCount = table.RowCount;
        DataLength = table.RawData.Length;
    }
}
