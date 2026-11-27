using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class HexViewerViewModel : ViewModelBase
{
    /// <summary>The row size used when the HexRowSize setting is missing or out of range.</summary>
    internal const int DefaultRowSize = 16;
    internal const int MinRowSize = 1;
    internal const int MaxRowSize = 256;
    private const int MaxDisplayBytes = 64 * 1024; // 64KB limit to prevent UI freezing

    [ObservableProperty]
    private string _hexContent = string.Empty;

    [ObservableProperty]
    private int _dataLength;

    [ObservableProperty]
    private bool _isTruncated;

    /// <summary>
    /// Number of bytes shown on each row of the dump. Starts as the HexRowSize setting.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings lines 41-42 (HexRowSize).
    /// </remarks>
    public int RowSize { get; set; } = ClampRowSize(SettingsService.Instance.Settings.HexRowSize);

    /// <summary>Limits a row size to a usable range, falling back to 16 for non-positive values.</summary>
    internal static int ClampRowSize(int rowSize)
        => rowSize < MinRowSize ? DefaultRowSize : Math.Min(rowSize, MaxRowSize);

    public void LoadData(ReadOnlyMemory<byte> data)
    {
        DataLength = data.Length;
        IsTruncated = data.Length > MaxDisplayBytes;
        HexContent = FormatHexView(data.Span, ClampRowSize(RowSize));
    }

    private static string FormatHexView(ReadOnlySpan<byte> data, int bytesPerLine)
    {
        if (data.IsEmpty)
            return "(No data)";

        var sb = new StringBuilder();

        // Limit display size to prevent crashes
        int displayLength = Math.Min(data.Length, MaxDisplayBytes);
        bool truncated = data.Length > MaxDisplayBytes;

        // Header
        sb.Append("Offset    ");
        for (int i = 0; i < bytesPerLine; i++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{i:X2} ");
        }
        sb.Append(" ASCII");
        sb.AppendLine();
        sb.AppendLine(new string('-', 10 + (bytesPerLine * 3) + 2 + bytesPerLine));

        // Data rows (limited to displayLength)
        for (int offset = 0; offset < displayLength; offset += bytesPerLine)
        {
            // Offset column
            sb.Append(CultureInfo.InvariantCulture, $"{offset:X8}  ");

            // Hex bytes
            int lineBytes = Math.Min(bytesPerLine, displayLength - offset);
            for (int i = 0; i < bytesPerLine; i++)
            {
                if (i < lineBytes)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"{data[offset + i]:X2} ");
                }
                else
                {
                    sb.Append("   ");
                }
            }

            // ASCII column
            sb.Append(' ');
            for (int i = 0; i < lineBytes; i++)
            {
                byte b = data[offset + i];
                char c = (b >= 32 && b < 127) ? (char)b : '.';
                sb.Append(c);
            }

            sb.AppendLine();
        }

        // Truncation indicator
        if (truncated)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"... and {data.Length - MaxDisplayBytes:N0} more bytes (showing first {MaxDisplayBytes / 1024}KB of {data.Length:N0} bytes)");
        }

        return sb.ToString();
    }
}
