using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly SimDataTable _table;

    public int Index { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string NameHashHex { get; }
    public string SchemaName { get; }
    public int RowCount { get; }
    public int DataLength { get; }

    /// <summary>
    /// The underlying table for editing operations.
    /// </summary>
    public SimDataTable Table => _table;

    public ObservableCollection<SimDataRowViewModel> Rows { get; } = [];

    public SimDataTableViewModel(SimDataTable table)
    {
        _table = table;
        Index = table.Index;
        Name = table.Name;
        DisplayName = string.IsNullOrEmpty(table.Name)
            ? $"Table {table.Index}"
            : table.Name;
        NameHashHex = $"0x{table.NameHash:X8}";
        SchemaName = table.Schema?.Name ?? "(none)";
        RowCount = table.RowCount;
        DataLength = table.RawData.Length;

        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        if (_table.Schema == null) return;

        for (int i = 0; i < RowCount; i++)
        {
            Rows.Add(new SimDataRowViewModel(_table, i));
        }
    }
}

/// <summary>
/// ViewModel for a single row in a SimData table.
/// </summary>
public sealed partial class SimDataRowViewModel : ViewModelBase
{
    private readonly SimDataTable _table;

    public int RowIndex { get; }
    public ObservableCollection<SimDataCellViewModel> Cells { get; } = [];

    public SimDataRowViewModel(SimDataTable table, int rowIndex)
    {
        _table = table;
        RowIndex = rowIndex;

        LoadCells();
    }

    private void LoadCells()
    {
        Cells.Clear();
        if (_table.Schema == null) return;

        foreach (var field in _table.Schema.Fields)
        {
            Cells.Add(new SimDataCellViewModel(_table, RowIndex, field));
        }
    }
}

/// <summary>
/// ViewModel for a single cell (field value) in a SimData row.
/// </summary>
public sealed partial class SimDataCellViewModel : ViewModelBase
{
    private readonly SimDataTable _table;
    private readonly int _rowIndex;
    private readonly SimDataField _field;

    [ObservableProperty]
    private string _displayValue = string.Empty;

    [ObservableProperty]
    private string _editValue = string.Empty;

    // Flag to prevent recursive apply during refresh
    private bool _isRefreshing;

    partial void OnEditValueChanged(string value)
    {
        if (!_isRefreshing && IsEditable && value != DisplayValue)
        {
            ApplyEdit();
        }
    }

    public string FieldName => _field.Name;
    public string FieldNameHex => $"0x{_field.NameHash:X8}";
    public SimDataFieldType FieldType => _field.Type;
    public string TypeName { get; }

    /// <summary>
    /// Whether this cell is editable (has a known type).
    /// </summary>
    public bool IsEditable => FieldType != SimDataFieldType.Unknown1 &&
                              FieldType != SimDataFieldType.Unknown2 &&
                              FieldType != SimDataFieldType.Unknown3;

    public SimDataCellViewModel(SimDataTable table, int rowIndex, SimDataField field)
    {
        _table = table;
        _rowIndex = rowIndex;
        _field = field;
        TypeName = GetTypeName(field.Type, field.TypeValue);

        RefreshValue();
    }

    /// <summary>
    /// Refreshes the display value from the underlying data.
    /// </summary>
    public void RefreshValue()
    {
        _isRefreshing = true;
        try
        {
            DisplayValue = ReadFormattedValue();
            EditValue = DisplayValue;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Applies the edited value to the underlying data.
    /// </summary>
    public bool ApplyEdit()
    {
        if (!IsEditable || EditValue == DisplayValue)
            return false;

        try
        {
            switch (_field.Type)
            {
                case SimDataFieldType.Boolean:
                    bool boolVal = EditValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                   EditValue.Equals("1", StringComparison.Ordinal) ||
                                   EditValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
                    _table.SetBoolean(_rowIndex, _field, boolVal);
                    break;

                case SimDataFieldType.Integer16:
                    if (short.TryParse(EditValue, CultureInfo.InvariantCulture, out short int16Val))
                        _table.SetInt16(_rowIndex, _field, int16Val);
                    else
                        return false;
                    break;

                case SimDataFieldType.FloatValue:
                case SimDataFieldType.VFX:
                    if (float.TryParse(EditValue, CultureInfo.InvariantCulture, out float floatVal))
                        _table.SetFloat(_rowIndex, _field, floatVal);
                    else
                        return false;
                    break;

                case SimDataFieldType.RGBColor:
                case SimDataFieldType.ARGBColor:
                    // Parse as hex uint32
                    string hexStr = EditValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? EditValue[2..]
                        : EditValue;
                    if (uint.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint colorVal))
                        _table.SetUInt32(_rowIndex, _field, colorVal);
                    else
                        return false;
                    break;

                case SimDataFieldType.DataInstance:
                case SimDataFieldType.ImageInstance:
                case SimDataFieldType.StringInstance:
                    // Parse as hex uint64
                    string hex64Str = EditValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? EditValue[2..]
                        : EditValue;
                    if (ulong.TryParse(hex64Str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong instanceVal))
                        _table.SetUInt64(_rowIndex, _field, instanceVal);
                    else
                        return false;
                    break;

                default:
                    return false;
            }

            // Update display value without triggering OnEditValueChanged
            _isRefreshing = true;
            try
            {
                DisplayValue = ReadFormattedValue();
            }
            finally
            {
                _isRefreshing = false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reverts the edit value to the current display value.
    /// </summary>
    public void RevertEdit()
    {
        EditValue = DisplayValue;
    }

    /// <summary>
    /// Command to apply the current edit.
    /// </summary>
    [RelayCommand]
    private void CommitEdit()
    {
        ApplyEdit();
    }

    private string ReadFormattedValue()
    {
        return _field.Type switch
        {
            SimDataFieldType.Boolean => _table.GetBoolean(_rowIndex, _field) ? "True" : "False",
            SimDataFieldType.Integer16 => _table.GetInt16(_rowIndex, _field).ToString(CultureInfo.InvariantCulture),
            SimDataFieldType.FloatValue => _table.GetFloat(_rowIndex, _field).ToString("G9", CultureInfo.InvariantCulture),
            SimDataFieldType.VFX => _table.GetFloat(_rowIndex, _field).ToString("G9", CultureInfo.InvariantCulture),
            SimDataFieldType.RGBColor => $"0x{_table.GetUInt32(_rowIndex, _field):X6}",
            SimDataFieldType.ARGBColor => $"0x{_table.GetUInt32(_rowIndex, _field):X8}",
            SimDataFieldType.DataInstance or
            SimDataFieldType.ImageInstance or
            SimDataFieldType.StringInstance => $"0x{_table.GetUInt64(_rowIndex, _field):X16}",
            _ => FormatRawBytes()
        };
    }

    private string FormatRawBytes()
    {
        var bytes = _table.GetFieldBytes(_rowIndex, _field);
        if (bytes.IsEmpty)
            return "(empty)";

        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(CultureInfo.InvariantCulture, $"{b:X2}");
        }
        return sb.ToString();
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
