using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// View model for an RCOL chunk entry.
/// </summary>
public partial class RcolChunkViewModel : ViewModelBase
{
    private const int HexPreviewMaxBytes = 512;

    private readonly RcolChunkEntry _entry;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// The index of this chunk in the list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The 4-character tag (e.g., "MODL", "MLOD").
    /// </summary>
    public string Tag => _entry.Block.Tag;

    /// <summary>
    /// The resource type in hex format.
    /// </summary>
    public string TypeHex => $"0x{_entry.TgiBlock.ResourceType:X8}";

    /// <summary>
    /// Human-readable type name.
    /// </summary>
    public string TypeName => RcolConstants.GetTypeName(_entry.TgiBlock.ResourceType);

    /// <summary>
    /// The resource group in hex format.
    /// </summary>
    public string GroupHex => $"0x{_entry.TgiBlock.ResourceGroup:X8}";

    /// <summary>
    /// The instance in hex format.
    /// </summary>
    public string InstanceHex => $"0x{_entry.TgiBlock.Instance:X16}";

    /// <summary>
    /// The TGI as a single string.
    /// </summary>
    public string TgiString => _entry.TgiBlock.ToString();

    /// <summary>
    /// The size of this chunk in bytes.
    /// </summary>
    public int Size => _entry.Length;

    /// <summary>
    /// Display string for size.
    /// </summary>
    public string SizeDisplay => $"{_entry.Length:N0} bytes";

    /// <summary>
    /// The position of this chunk in the resource.
    /// </summary>
    public string PositionHex => $"0x{_entry.Position:X8}";

    /// <summary>
    /// Whether this is a known/parsed block type.
    /// </summary>
    public bool IsKnownType => _entry.Block.IsKnownType;

    /// <summary>
    /// Display label combining tag and type name.
    /// </summary>
    public string DisplayLabel => Tag == TypeName
        ? Tag
        : $"{Tag} ({TypeName})";

    /// <summary>
    /// Hex dump of the chunk data.
    /// </summary>
    public string HexDump { get; }

    /// <summary>
    /// Creates a new chunk view model.
    /// </summary>
    public RcolChunkViewModel(RcolChunkEntry entry, int index)
    {
        _entry = entry;
        Index = index;
        HexDump = BuildHexDump(entry.Block.Data.Span);
    }

    private static string BuildHexDump(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder();
        var previewLength = Math.Min(HexPreviewMaxBytes, data.Length);

        for (int offset = 0; offset < previewLength; offset += 16)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{offset:X8}  ");

            int lineBytes = Math.Min(16, previewLength - offset);

            // Hex bytes
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

            // ASCII
            for (int i = 0; i < lineBytes; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }

            sb.AppendLine();
        }

        if (data.Length > HexPreviewMaxBytes)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"... ({data.Length - HexPreviewMaxBytes:N0} more bytes)");
        }

        return sb.ToString();
    }
}
