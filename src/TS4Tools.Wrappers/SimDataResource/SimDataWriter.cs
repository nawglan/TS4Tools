
namespace TS4Tools.Wrappers;

/// <summary>
/// Utility methods for writing SimData binary format.
/// Source: DataResource.cs UnParse and WriteOffsets methods
/// </summary>
internal static class SimDataWriter
{
    /// <summary>
    /// Constant representing a null/invalid offset.
    /// Source: Util.cs line 12
    /// </summary>
    public const uint NullOffset = 0x80000000;

    /// <summary>
    /// Zero value for placeholder offsets.
    /// Source: DataResource.cs line 78
    /// </summary>
    public const uint Zero32 = 0;

    /// <summary>
    /// Calculates the relative offset to write.
    /// Source: DataResource.cs WriteOffsets methods
    /// Formula: relativeOffset = absoluteTarget - positionWhereOffsetIsWritten - fieldOffsetInEntry
    /// </summary>
    /// <param name="absoluteTarget">The absolute position of the target.</param>
    /// <param name="entryPosition">The position of the entry containing this offset.</param>
    /// <param name="fieldOffset">The offset of this field within the entry.</param>
    /// <returns>The relative offset to write, or NullOffset if target is null.</returns>
    public static uint CalculateRelativeOffset(uint absoluteTarget, uint entryPosition, uint fieldOffset)
    {
        if (absoluteTarget == NullOffset)
            return NullOffset;

        // Source: DataResource.cs Structure.WriteOffsets line 270
        // w.Write(this.fieldTablePosition - myPosition - 0x10);
        return absoluteTarget - entryPosition - fieldOffset;
    }

    /// <summary>
    /// Writes a null-terminated ASCII string to a buffer.
    /// Source: Util.cs lines 31-37
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="offset">The offset in the buffer to write at.</param>
    /// <param name="value">The string to write.</param>
    /// <returns>The number of bytes written (including null terminator).</returns>
    public static int WriteNullTerminatedString(Span<byte> buffer, int offset, string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        int bytesWritten = Encoding.ASCII.GetBytes(value, buffer[offset..]);
        buffer[offset + bytesWritten] = 0; // null terminator
        return bytesWritten + 1;
    }

    /// <summary>
    /// Calculates padding needed to align to a boundary.
    /// Source: DataResource.cs line 774 - Util.Padding
    /// </summary>
    /// <param name="currentPosition">The current position in the buffer.</param>
    /// <param name="alignment">The alignment boundary.</param>
    /// <returns>The number of padding bytes needed.</returns>
    public static int CalculatePadding(int currentPosition, int alignment)
    {
        if (alignment <= 0)
            return 0;

        int remainder = currentPosition % alignment;
        return remainder == 0 ? 0 : alignment - remainder;
    }

    /// <summary>
    /// Writes a uint32 value at the specified position.
    /// </summary>
    public static void WriteUInt32(Span<byte> buffer, int position, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[position..], value);
    }

    /// <summary>
    /// Writes an int32 value at the specified position.
    /// </summary>
    public static void WriteInt32(Span<byte> buffer, int position, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], value);
    }

    /// <summary>
    /// Writes zeros for padding at the specified position.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="position">The position to start writing.</param>
    /// <param name="count">The number of zero bytes to write.</param>
    /// <returns>The new position after writing.</returns>
    public static int WritePadding(Span<byte> buffer, int position, int count)
    {
        buffer.Slice(position, count).Clear();
        return position + count;
    }
}
