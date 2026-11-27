// Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs lines 28-72 (SevenBitString and BigEndianUnicodeString)

namespace TS4Tools.Wrappers.Utilities;

/// <summary>
/// Reads and writes seven-bit encoded length-prefixed strings in a given <see cref="Encoding"/>.
/// The prefix is the string's length in BYTES, encoded 7 bits per byte (high bit = continuation).
/// </summary>
/// <remarks>
/// Source: SevenBitString.cs lines 33-61. Legacy reads via <c>BinaryReader.ReadString</c> and writes by hand;
/// this port does both by hand so the length can be validated against the available data. The span-based
/// <c>TS4Tools.Wrappers.CasPartResource.BigEndianUnicodeString</c> covers the same format for parsed resource data;
/// the members here add the legacy stream-based API and arbitrary encodings.
/// </remarks>
public static class SevenBitString
{
    /// <summary>Largest byte length accepted when reading a string.</summary>
    public const int MaxStringByteLength = 64 * 1024 * 1024;

    /// <summary>
    /// Reads a string from <paramref name="stream"/> using <paramref name="encoding"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The length prefix is malformed or exceeds the data available.</exception>
    /// <exception cref="EndOfStreamException">The stream ended before the string was complete.</exception>
    public static string Read(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        int length = 0;
        int shift = 0;
        while (true)
        {
            int b = stream.ReadByte();
            if (b < 0)
                throw new EndOfStreamException("Stream ended inside a string length prefix.");

            length |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                break;

            shift += 7;
            if (shift >= 35)
                throw new InvalidDataException("Seven-bit encoded string length is too long.");
        }

        if (length < 0 || length > MaxStringByteLength)
            throw new InvalidDataException($"Invalid string byte length {length}.");
        if (stream.CanSeek && length > stream.Length - stream.Position)
            throw new InvalidDataException($"String byte length {length} exceeds the data available.");

        var bytes = new byte[length];
        stream.ReadExactly(bytes);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Writes a string to <paramref name="stream"/> using <paramref name="encoding"/>. A null value is written as an empty string.
    /// </summary>
    public static void Write(Stream stream, Encoding encoding, string? value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        byte[] bytes = encoding.GetBytes(value ?? string.Empty);
        for (int i = bytes.Length; ; )
        {
            stream.WriteByte((byte)((i & 0x7F) | (i > 0x7F ? 0x80 : 0)));
            i >>= 7;
            if (i == 0)
                break; // zero length writes a single zero byte
        }

        stream.Write(bytes);
    }

    /// <summary>Reads a string from <paramref name="data"/>, returning the bytes consumed.</summary>
    public static string Read(ReadOnlySpan<byte> data, Encoding encoding, out int bytesRead)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        int length = 0;
        int shift = 0;
        int offset = 0;
        while (true)
        {
            if (offset >= data.Length)
                throw new InvalidDataException("Data ended inside a string length prefix.");

            byte b = data[offset++];
            length |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                break;

            shift += 7;
            if (shift >= 35)
                throw new InvalidDataException("Seven-bit encoded string length is too long.");
        }

        if (length < 0 || length > data.Length - offset)
            throw new InvalidDataException($"String byte length {length} exceeds the data available.");

        bytesRead = offset + length;
        return encoding.GetString(data.Slice(offset, length));
    }

    /// <summary>Reads a BigEndianUnicode (UTF-16 BE) seven-bit length-prefixed string from a stream.</summary>
    /// <remarks>Source: SevenBitString.cs lines 63-72 (BigEndianUnicodeString.Read).</remarks>
    public static string ReadBigEndianUnicode(Stream stream) => Read(stream, Encoding.BigEndianUnicode);

    /// <summary>Writes a BigEndianUnicode (UTF-16 BE) seven-bit length-prefixed string to a stream.</summary>
    /// <remarks>Source: SevenBitString.cs lines 63-72 (BigEndianUnicodeString.Write).</remarks>
    public static void WriteBigEndianUnicode(Stream stream, string? value) => Write(stream, Encoding.BigEndianUnicode, value);
}
