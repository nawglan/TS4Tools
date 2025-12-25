using System;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class HexViewerViewModel : ViewModelBase
{
    private const int BytesPerLine = 16;

    [ObservableProperty]
    private string _hexContent = string.Empty;

    [ObservableProperty]
    private int _dataLength;

    public void LoadData(ReadOnlyMemory<byte> data)
    {
        DataLength = data.Length;
        HexContent = FormatHexView(data.Span);
    }

    private static string FormatHexView(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return "(No data)";

        var sb = new StringBuilder();

        // Header
        sb.Append("Offset    ");
        for (int i = 0; i < BytesPerLine; i++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{i:X2} ");
        }
        sb.Append(" ASCII");
        sb.AppendLine();
        sb.AppendLine(new string('-', 10 + (BytesPerLine * 3) + 2 + BytesPerLine));

        // Data rows
        for (int offset = 0; offset < data.Length; offset += BytesPerLine)
        {
            // Offset column
            sb.Append(CultureInfo.InvariantCulture, $"{offset:X8}  ");

            // Hex bytes
            int lineBytes = Math.Min(BytesPerLine, data.Length - offset);
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

        return sb.ToString();
    }
}
