// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/s4pi.Commons/Extensions/BinaryWriterExtensions.cs lines 22-42
// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/s4pi.Commons/Extensions/StringExtensions.cs lines 22-45

namespace TS4Tools.Wrappers.Utilities;

/// <summary>
/// Extensions for <see cref="BinaryWriter"/>.
/// </summary>
public static class BinaryWriterExtensions
{
    /// <summary>
    /// Writes the specified number of zero bytes.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="count">Number of zero bytes to write; must not be negative.</param>
    /// <remarks>Source: BinaryWriterExtensions.cs lines 29-37 (WriteEmptyBytes). Legacy writes one byte at a time; this writes in chunks.</remarks>
    public static void WriteEmptyBytes(this BinaryWriter writer, int count)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        Span<byte> zeros = stackalloc byte[64];
        zeros.Clear();
        while (count > 0)
        {
            int chunk = Math.Min(count, zeros.Length);
            writer.Write(zeros[..chunk]);
            count -= chunk;
        }
    }
}

/// <summary>
/// Extensions for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Gets the text after the last '.' of a file name, or an empty string when there is none.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The extension without the dot.</returns>
    /// <remarks>Source: StringExtensions.cs lines 29-42 (FileExtension). Deliberately not Path.GetExtension: legacy splits on the last '.' only.</remarks>
    public static string FileExtension(this string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        int index = fileName.LastIndexOf('.');
        return index == -1 ? string.Empty : fileName[(index + 1)..];
    }
}
