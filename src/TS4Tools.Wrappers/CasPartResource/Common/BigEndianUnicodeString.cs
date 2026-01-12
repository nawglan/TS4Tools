
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Reads and writes 7-bit length-prefixed strings in BigEndianUnicode (UTF-16 BE) encoding.
/// The length is encoded in bytes (not characters) using 7-bit variable length encoding.
/// Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs
/// </summary>
public static class BigEndianUnicodeString
{
    private static readonly Encoding BigEndianUnicode = Encoding.BigEndianUnicode;

    /// <summary>
    /// Reads a 7-bit length-prefixed BigEndianUnicode string from a span.
    /// </summary>
    /// <param name="data">The data span to read from.</param>
    /// <param name="bytesRead">The total bytes consumed (length prefix + string bytes).</param>
    /// <returns>The decoded string.</returns>
    public static string Read(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;

        // Read 7-bit encoded length (in bytes, not characters)
        int byteLength = Read7BitEncodedInt(data, ref offset);

        if (byteLength == 0)
        {
            bytesRead = offset;
            return string.Empty;
        }

        if (offset + byteLength > data.Length)
        {
            throw new InvalidDataException(
                $"String byte length {byteLength} exceeds available data {data.Length - offset}");
        }

        // Decode BigEndianUnicode string
        string result = BigEndianUnicode.GetString(data.Slice(offset, byteLength));
        bytesRead = offset + byteLength;
        return result;
    }

    /// <summary>
    /// Writes a string with 7-bit length prefix in BigEndianUnicode encoding.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="value">The string to write.</param>
    /// <returns>The number of bytes written.</returns>
    public static int Write(Span<byte> buffer, string value)
    {
        value ??= string.Empty;

        // Encode the string to bytes first to get the byte length
        int byteLength = BigEndianUnicode.GetByteCount(value);

        int offset = 0;

        // Write 7-bit encoded length
        Write7BitEncodedInt(buffer, byteLength, ref offset);

        // Write string bytes
        if (byteLength > 0)
        {
            BigEndianUnicode.GetBytes(value, buffer.Slice(offset, byteLength));
            offset += byteLength;
        }

        return offset;
    }

    /// <summary>
    /// Gets the total serialized size for a string (length prefix + string bytes).
    /// </summary>
    public static int GetSerializedSize(string value)
    {
        value ??= string.Empty;
        int byteLength = BigEndianUnicode.GetByteCount(value);
        return Get7BitEncodedIntSize(byteLength) + byteLength;
    }

    /// <summary>
    /// Reads a 7-bit encoded integer from a span.
    /// </summary>
    private static int Read7BitEncodedInt(ReadOnlySpan<byte> data, ref int offset)
    {
        int result = 0;
        int shift = 0;

        while (true)
        {
            if (offset >= data.Length)
            {
                throw new InvalidDataException("Unexpected end of data while reading 7-bit encoded int");
            }

            byte b = data[offset++];
            result |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;

            // Prevent overflow for unreasonably large values
            if (shift > 35)
            {
                throw new InvalidDataException("Invalid 7-bit encoded int: too many bytes");
            }
        }

        return result;
    }

    /// <summary>
    /// Writes a 7-bit encoded integer to a span.
    /// </summary>
    private static void Write7BitEncodedInt(Span<byte> buffer, int value, ref int offset)
    {
        // Write the value in 7-bit chunks, LSB first
        // If more bytes follow, set the MSB
        uint v = (uint)value;
        while (v >= 0x80)
        {
            buffer[offset++] = (byte)(v | 0x80);
            v >>= 7;
        }
        buffer[offset++] = (byte)v;
    }

    /// <summary>
    /// Gets the number of bytes needed to store a value as a 7-bit encoded int.
    /// </summary>
    private static int Get7BitEncodedIntSize(int value)
    {
        int size = 1;
        uint v = (uint)value;
        while (v >= 0x80)
        {
            size++;
            v >>= 7;
        }
        return size;
    }
}
