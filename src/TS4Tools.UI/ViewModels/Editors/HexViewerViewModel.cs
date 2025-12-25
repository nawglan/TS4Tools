using System;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class HexViewerViewModel : ViewModelBase
{
    private const int BytesPerLine = 16;
    private const int MaxDisplayBytes = 64 * 1024; // 64KB limit to prevent UI freezing

    [ObservableProperty]
    private string _hexContent = string.Empty;

    [ObservableProperty]
    private int _dataLength;

    [ObservableProperty]
    private bool _isTruncated;

    public void LoadData(ReadOnlyMemory<byte> data)
    {
        DataLength = data.Length;
        IsTruncated = data.Length > MaxDisplayBytes;
        HexContent = FormatHexView(data.Span);
    }

    private static string FormatHexView(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return "(No data)";

        var sb = new StringBuilder();

        // Limit display size to prevent crashes
        int displayLength = Math.Min(data.Length, MaxDisplayBytes);
        bool truncated = data.Length > MaxDisplayBytes;

        // Header
        sb.Append("Offset    ");
        for (int i = 0; i < BytesPerLine; i++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{i:X2} ");
        }
        sb.Append(" ASCII");
        sb.AppendLine();
        sb.AppendLine(new string('-', 10 + (BytesPerLine * 3) + 2 + BytesPerLine));

        // Data rows (limited to displayLength)
        for (int offset = 0; offset < displayLength; offset += BytesPerLine)
        {
            // Offset column
            sb.Append(CultureInfo.InvariantCulture, $"{offset:X8}  ");

            // Hex bytes
            int lineBytes = Math.Min(BytesPerLine, displayLength - offset);
            for (int i = 0; i < BytesPerLine; i++)
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
