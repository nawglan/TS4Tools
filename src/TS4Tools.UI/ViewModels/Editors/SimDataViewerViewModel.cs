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

    public ObservableCollection<SimDataSchemaViewModel> Schemas { get; } = [];

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
    }

    private void BuildHeaderInfo(SimDataResource resource)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Magic: DATA (0x{SimDataResource.Magic:X8})");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Version: {VersionHex}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Table Offset: 0x{resource.TableOffset:X8}");
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
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}

public sealed class SimDataSchemaViewModel
{
    public int Index { get; }
    public string Name { get; }
    public string NameHashHex { get; }
    public int ColumnCount { get; }

    public SimDataSchemaViewModel(SimDataSchema schema)
    {
        Index = schema.Index;
        Name = schema.Name;
        NameHashHex = $"0x{schema.NameHash:X8}";
        ColumnCount = schema.ColumnCount;
    }
}
