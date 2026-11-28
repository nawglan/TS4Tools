// Source: legacy BinaryReader.ReadChars / BinaryWriter.Write(char[]) with the default UTF-8 encoding, as used by
// legacy_references/Sims4Tools/s4pi Wrappers/NameMapResource/NameMapResource.cs line 61 and
// legacy_references/Sims4Tools/s4pi Wrappers/TxtcResource/TxtcResource.cs lines 473, 1000.

namespace TS4Tools.Wrappers;

/// <summary>
/// Reads strings stored as a count of chars followed by their UTF-8 bytes (legacy <c>BinaryReader.ReadChars</c>).
/// </summary>
internal static class LegacyChars
{
    /// <summary>
    /// Reads <paramref name="charCount"/> UTF-8 encoded chars starting at <paramref name="offset"/>.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="offset">Position of the first byte; advanced past the bytes read.</param>
    /// <param name="charCount">Number of UTF-16 chars to read (a surrogate pair counts as two).</param>
    /// <param name="what">What is being read, for error messages.</param>
    /// <exception cref="ResourceFormatException">The data ends first, or the last char is half a surrogate pair.</exception>
    public static string ReadUtf8Chars(ReadOnlySpan<byte> data, ref int offset, int charCount, string what)
    {
        if (charCount == 0)
            return string.Empty;
        // Each char takes at least one UTF-8 byte.
        CountGuard.Check(charCount, data.Length - offset, 1, what + " char");

        var decoder = Encoding.UTF8.GetDecoder();
        var chars = new char[charCount];
        int produced = 0;
        Span<char> one = stackalloc char[2];
        while (produced < charCount)
        {
            if (offset >= data.Length)
                throw new ResourceFormatException($"Unexpected end of data in {what}");

            int n = decoder.GetChars(data.Slice(offset++, 1), one, flush: false);
            if (produced + n > charCount)
                throw new ResourceFormatException($"{what} ends inside a surrogate pair");
            one[..n].CopyTo(chars.AsSpan(produced));
            produced += n;
        }

        return new string(chars);
    }
}
