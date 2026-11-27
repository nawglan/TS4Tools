// Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs lines 23-72

namespace System.Text;

/// <summary>
/// Read and write a seven-bit length-prefixed string in a given <see cref="Encoding"/>.
/// </summary>
public static class SevenBitString
{
    /// <summary>
    /// Read a string from a <see cref="Stream"/> based on the <paramref name="enc"/>.
    /// </summary>
    /// <param name="s">Stream containing the string.</param>
    /// <param name="enc">Encoding of the string.</param>
    /// <returns>The string read.</returns>
    public static string Read(Stream s, Encoding enc)
    {
        using var r = new BinaryReader(s, enc, leaveOpen: true);
        return r.ReadString();
    }

    /// <summary>
    /// Write a seven-bit length-prefixed string to a <see cref="Stream"/> using the <paramref name="enc"/>.
    /// </summary>
    /// <param name="s">Stream to write to.</param>
    /// <param name="enc">Encoding to use.</param>
    /// <param name="value">String to write (null is written as empty).</param>
    /// <remarks>Source: SevenBitString.cs lines 44-50 (byte length, not char count, is written)</remarks>
    public static void Write(Stream s, Encoding enc, string? value)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(enc);
        byte[] bytes = enc.GetBytes(value ?? "");
        for (int i = bytes.Length; ;)
        {
            s.WriteByte((byte)((i & 0x7F) | (i > 0x7F ? 0x80 : 0)));
            i >>= 7;
            if (i == 0) break; // zero length writes a single zero byte
        }
        s.Write(bytes, 0, bytes.Length);
    }
}

/// <summary>
/// Read and write a seven-bit length-prefixed string in <see cref="Encoding.BigEndianUnicode"/>.
/// </summary>
public static class BigEndianUnicodeString
{
    /// <summary>Read a big-endian Unicode string.</summary>
    /// <param name="s">Stream containing the string.</param>
    /// <returns>The string read.</returns>
    public static string Read(Stream s) => SevenBitString.Read(s, Encoding.BigEndianUnicode);

    /// <summary>Write a big-endian Unicode string.</summary>
    /// <param name="s">Stream to write to.</param>
    /// <param name="value">String to write.</param>
    public static void Write(Stream s, string? value) => SevenBitString.Write(s, Encoding.BigEndianUnicode, value);
}
