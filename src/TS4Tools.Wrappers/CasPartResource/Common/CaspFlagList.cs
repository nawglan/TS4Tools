
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of CAS part flags with uint32 count prefix.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResourceTS4.cs
/// See lines 596-621: FlagList class
/// </summary>
public sealed class CaspFlagList : List<CaspFlag>
{
    /// <summary>
    /// Maximum reasonable number of flags (OOM protection).
    /// </summary>
    private const int MaxFlagCount = 1000;

    /// <summary>
    /// Creates an empty CaspFlagList.
    /// </summary>
    public CaspFlagList()
    {
    }

    /// <summary>
    /// Creates a CaspFlagList with the specified flags.
    /// </summary>
    public CaspFlagList(IEnumerable<CaspFlag> flags) : base(flags)
    {
    }

    /// <summary>
    /// Parses a CaspFlagList from a span.
    /// Format: uint32 count + (count * 4-byte flag entries)
    /// </summary>
    /// <param name="data">The data span to parse from.</param>
    /// <param name="bytesRead">The total bytes consumed.</param>
    public static CaspFlagList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count > MaxFlagCount)
        {
            throw new InvalidDataException($"Flag count {count} exceeds maximum {MaxFlagCount}");
        }

        var list = new CaspFlagList();
        list.Capacity = (int)count;

        for (int i = 0; i < count; i++)
        {
            if (offset + CaspFlag.SerializedSize > data.Length)
            {
                throw new InvalidDataException($"Truncated flag data at index {i}");
            }

            list.Add(CaspFlag.Parse(data[offset..]));
            offset += CaspFlag.SerializedSize;
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Parses a CaspFlagList from a span with version-specific flag size.
    /// </summary>
    /// <param name="data">The data span to parse from.</param>
    /// <param name="offset">The current offset, updated after parsing.</param>
    /// <param name="use32BitValue">If true, uses 32-bit value (6 bytes per flag); otherwise 16-bit (4 bytes).</param>
    public static CaspFlagList Parse(ReadOnlySpan<byte> data, ref int offset, bool use32BitValue)
    {
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count > MaxFlagCount)
        {
            throw new InvalidDataException($"Flag count {count} exceeds maximum {MaxFlagCount}");
        }

        var list = new CaspFlagList();
        list.Capacity = (int)count;

        int flagSize = use32BitValue ? 6 : 4;

        for (int i = 0; i < count; i++)
        {
            if (offset + flagSize > data.Length)
            {
                throw new InvalidDataException($"Truncated flag data at index {i}");
            }

            ushort category = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            ushort value;
            if (use32BitValue)
            {
                // Read 32-bit value but store as 16-bit (truncate high bits)
                uint value32 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                value = (ushort)(value32 & 0xFFFF);
                offset += 4;
            }
            else
            {
                value = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
                offset += 2;
            }

            list.Add(new CaspFlag(category, value));
        }

        return list;
    }

    /// <summary>
    /// Writes this list to a span.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], (uint)Count);
        offset += 4;

        foreach (var flag in this)
        {
            flag.WriteTo(buffer[offset..]);
            offset += CaspFlag.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Serializes this list with version-specific flag size.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="offset">The current offset, updated after writing.</param>
    /// <param name="use32BitValue">If true, writes 32-bit value (6 bytes per flag); otherwise 16-bit (4 bytes).</param>
    public void Serialize(Span<byte> buffer, ref int offset, bool use32BitValue)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], (uint)Count);
        offset += 4;

        foreach (var flag in this)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], flag.Category);
            offset += 2;

            if (use32BitValue)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], flag.Value);
                offset += 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], flag.Value);
                offset += 2;
            }
        }
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize()
    {
        return 4 + (Count * CaspFlag.SerializedSize);
    }

    /// <summary>
    /// Gets the serialized size with version-specific flag size.
    /// </summary>
    /// <param name="use32BitValue">If true, uses 32-bit value (6 bytes per flag); otherwise 16-bit (4 bytes).</param>
    public int GetSerializedSize(bool use32BitValue)
    {
        int flagSize = use32BitValue ? 6 : 4;
        return 4 + (Count * flagSize);
    }
}
