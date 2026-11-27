// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DataBlobHandler.cs lines 25-299

namespace s4pi.Interfaces;

/// <summary>
/// A data blob of raw bytes, with a hex-dump display.
/// </summary>
public class DataBlobHandler : AHandlerElement, IEquatable<DataBlobHandler>
{
    private const int recommendedApiVersion = 1;

    private readonly byte[] data;

    #region Constructors
    /// <summary>Create a zero-filled blob of <paramref name="length"/> bytes.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="length">The blob length.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    public DataBlobHandler(int apiVersion, EventHandler? handler, long length) : base(apiVersion, handler)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        data = new byte[length];
    }

    /// <summary>Create a blob of <paramref name="length"/> bytes read from <paramref name="s"/>.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="length">The blob length.</param>
    /// <param name="s">The stream to read.</param>
    /// <remarks>Source: DataBlobHandler.cs lines 59-64 (a short stream leaves the remaining bytes zero, as legacy's single Read call may).</remarks>
    public DataBlobHandler(int apiVersion, EventHandler? handler, long length, Stream s) : this(apiVersion, handler, length)
    {
        ArgumentNullException.ThrowIfNull(s);
        s.ReadAtLeast(data, data.Length, throwOnEndOfStream: false);
    }

    /// <summary>Create a copy of <paramref name="basis"/>.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">The blob to copy.</param>
    public DataBlobHandler(int apiVersion, EventHandler? handler, DataBlobHandler basis) : base(apiVersion, handler)
    {
        ArgumentNullException.ThrowIfNull(basis);
        data = (byte[])basis.data.Clone();
    }

    /// <summary>Create a blob holding a copy of <paramref name="data"/>.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="data">The bytes to copy.</param>
    public DataBlobHandler(int apiVersion, EventHandler? handler, byte[] data) : base(apiVersion, handler)
    {
        ArgumentNullException.ThrowIfNull(data);
        this.data = (byte[])data.Clone();
    }
    #endregion

    /// <summary>Write the blob to <paramref name="s"/>.</summary>
    /// <param name="s">The stream to write to.</param>
    public void UnParse(Stream s)
    {
        ArgumentNullException.ThrowIfNull(s);
        s.Write(data, 0, data.Length);
    }

    // Source: DataBlobHandler.cs lines 104-122 (Latin-1 printable, '.' otherwise; 0xAD soft hyphen shown as '.')
    private static readonly char[] conversionTable = BuildConversionTable();

    private static char[] BuildConversionTable()
    {
        var t = new char[256];
        for (int i = 0; i < 256; i++)
            t[i] = (i >= 0x21 && i <= 0x7E) || (i >= 0xA1 && i != 0xAD) ? (char)i : '.';
        return t;
    }

    /// <summary>
    /// Build a hex dump of the blob: a column header, then 16 bytes per row prefixed by the offset.
    /// </summary>
    /// <param name="indent">Number of spaces to indent each row.</param>
    /// <param name="includeASCII">Whether to append the printable characters of each row.</param>
    /// <returns>The hex dump, or "{Empty Data Blob}".</returns>
    /// <remarks>Source: DataBlobHandler.cs lines 131-195</remarks>
    public string BuildHexDisplay(int indent, bool includeASCII)
    {
        if (data.Length == 0)
            return "{Empty Data Blob}";

        int i, j, k, size;
        char[] buffer = includeASCII ? new char[16] : [];
        size = (data.Length / 16 + 1) * (indent + 60 + (includeASCII ? 11 : 0));
        var sb = new StringBuilder(size);
        i = 2;
        size = 256;
        while (size < data.Length && size < 0x10000000)
        {
            i++;
            size *= 16;
        }
        size = data.Length;
        string format = "X" + i.ToString(CultureInfo.InvariantCulture);
        sb.Append(new string(' ', indent + i));
        sb.Append(" | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        for (i = 0; i < size; i++)
        {
            if (i % 16 == 0)
            {
                sb.AppendLine();
                sb.Append(new string(' ', indent));
                sb.Append(i.ToString(format, CultureInfo.InvariantCulture));
                sb.Append(" |");
            }
            sb.Append(' ');
            sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));
            if (includeASCII && i % 16 == 15)
            {
                sb.Append(" | ");
                Array.Clear(buffer, 0, 16);
                k = (i / 16) * 16;
                for (j = 0; j < 16; j++)
                {
                    buffer[j] = conversionTable[data[k]];
                    k++;
                }
                sb.Append(buffer, 0, 16);
            }
        }
        if (includeASCII && size % 16 != 0)
        {
            sb.Append(new string(' ', 3 * (16 - (size % 16))));
            sb.Append(" | ");
            Array.Clear(buffer, 0, 16);
            k = (size / 16) * 16;
            size %= 16;
            for (j = 0; j < size; j++)
            {
                buffer[j] = conversionTable[data[k]];
                k++;
            }
            sb.Append(buffer, 0, size);
        }
        return sb.ToString();
    }

    /// <summary>Get a copy of <paramref name="size"/> bytes from <paramref name="offset"/>.</summary>
    /// <param name="offset">The start offset.</param>
    /// <param name="size">The number of bytes.</param>
    /// <returns>The bytes, or null if the range is invalid.</returns>
    public byte[]? GetRawData(int offset, int size)
    {
        if (offset < 0 || size < 0 || offset >= data.Length || (offset + size) > data.Length) return null; // invalid operation
        var result = new byte[size];
        Array.Copy(data, offset, result, 0, size);
        return result;
    }

    /// <summary>Overwrite bytes from <paramref name="offset"/> with <paramref name="source"/> (ignored if it does not fit).</summary>
    /// <param name="offset">The start offset.</param>
    /// <param name="source">The bytes to write.</param>
    public void SetRawData(int offset, byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (offset < 0 || source.Length + offset > data.Length) return; // invalid operation
        Array.Copy(source, 0, data, offset, source.Length);
    }

    #region AHandlerElement
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>The list of available field names on this API object.</summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IEquatable<DataBlobHandler>
    /// <summary>Indicates whether this blob has the same bytes as <paramref name="other"/>.</summary>
    /// <param name="other">The other blob.</param>
    /// <returns>true if the bytes are equal.</returns>
    public bool Equals(DataBlobHandler? other) => other is not null && data.AsSpan().SequenceEqual(other.data);

    /// <summary>Determines whether <paramref name="obj"/> (a byte array or blob) has the same bytes.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if the bytes are equal.</returns>
    public override bool Equals(object? obj) => obj switch
    {
        byte[] bytes => data.AsSpan().SequenceEqual(bytes),
        DataBlobHandler other => Equals(other),
        _ => false,
    };

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code of the underlying array (identity, as in legacy).</returns>
    public override int GetHashCode() => data.GetHashCode();
    #endregion

    /// <summary>A displayable hex dump of the blob.</summary>
    public string Value => string.Concat("\r\n", BuildHexDisplay(0, true));
}
