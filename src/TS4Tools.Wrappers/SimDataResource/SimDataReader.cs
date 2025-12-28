
namespace TS4Tools.Wrappers;

/// <summary>
/// Utility methods for reading SimData binary format.
/// Source: Util.cs from legacy DataResource
/// </summary>
internal static class SimDataReader
{
    /// <summary>
    /// Constant representing a null/invalid offset.
    /// Source: Util.cs line 12
    /// </summary>
    public const uint NullOffset = 0x80000000;

    /// <summary>
    /// Reads a relative offset and converts it to an absolute offset.
    /// Offsets in SimData are stored relative to the position after reading.
    /// Source: Util.cs lines 39-45
    /// </summary>
    /// <param name="data">The data span to read from.</param>
    /// <param name="position">The position to read the offset from.</param>
    /// <param name="absoluteOffset">The absolute offset if valid.</param>
    /// <returns>True if the offset is valid (not NullOffset), false otherwise.</returns>
    public static bool TryGetOffset(ReadOnlySpan<byte> data, int position, out uint absoluteOffset)
    {
        if (position < 0 || position + 4 > data.Length)
        {
            absoluteOffset = NullOffset;
            return false;
        }

        uint relativeOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);

        if (relativeOffset == NullOffset)
        {
            absoluteOffset = NullOffset;
            return false;
        }

        // Convert relative to absolute: offset + currentPosition (after read)
        // In legacy: offset += (uint)r.BaseStream.Position - 4;
        // Since we pass position before read, we add 4 to get position after read
        absoluteOffset = relativeOffset + (uint)(position + 4) - 4;
        // Simplifies to: absoluteOffset = relativeOffset + (uint)position;
        absoluteOffset = relativeOffset + (uint)position;

        return absoluteOffset < data.Length;
    }

    /// <summary>
    /// Reads a null-terminated ASCII string at the specified offset.
    /// Source: Util.cs lines 14-29
    /// </summary>
    /// <param name="data">The data span containing the string.</param>
    /// <param name="offset">The offset where the string starts.</param>
    /// <returns>The string, or empty string if offset is invalid.</returns>
    public static string ReadNullTerminatedString(ReadOnlySpan<byte> data, uint offset)
    {
        if (offset == NullOffset || offset >= data.Length)
            return string.Empty;

        int startIndex = (int)offset;
        int endIndex = startIndex;

        // Find null terminator
        while (endIndex < data.Length && data[endIndex] != 0)
        {
            endIndex++;
        }

        if (endIndex == startIndex)
            return string.Empty;

        return Encoding.ASCII.GetString(data[startIndex..endIndex]);
    }

    /// <summary>
    /// Reads a uint32 value at the specified position.
    /// </summary>
    public static uint ReadUInt32(ReadOnlySpan<byte> data, int position)
    {
        if (position < 0 || position + 4 > data.Length)
            return 0;
        return BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
    }

    /// <summary>
    /// Reads an int32 value at the specified position.
    /// </summary>
    public static int ReadInt32(ReadOnlySpan<byte> data, int position)
    {
        if (position < 0 || position + 4 > data.Length)
            return 0;
        return BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
    }

    /// <summary>
    /// Reads a uint64 value at the specified position.
    /// </summary>
    public static ulong ReadUInt64(ReadOnlySpan<byte> data, int position)
    {
        if (position < 0 || position + 8 > data.Length)
            return 0;
        return BinaryPrimitives.ReadUInt64LittleEndian(data[position..]);
    }

    /// <summary>
    /// Reads a float value at the specified position.
    /// </summary>
    public static float ReadFloat(ReadOnlySpan<byte> data, int position)
    {
        if (position < 0 || position + 4 > data.Length)
            return 0;
        return BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
    }
}
