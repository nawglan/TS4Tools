
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of CAS part flags with uint32 count prefix.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/Lists/FlagList.cs
/// lines 41-61 (32-bit Parse/UnParse), 63-71 (WriteUInt16Flags), 83-98 (CreateWithUInt16Flags).
/// </summary>
public sealed class CaspFlagList : List<CaspFlag>
{
    /// <summary>
    /// Maximum reasonable number of flags (OOM protection).
    /// </summary>
    private const int MaxFlagCount = 1000;

    /// <summary>Creates an empty CaspFlagList.</summary>
    public CaspFlagList()
    {
    }

    /// <summary>Creates a CaspFlagList with the specified flags.</summary>
    public CaspFlagList(IEnumerable<CaspFlag> flags) : base(flags)
    {
    }

    /// <summary>
    /// Parses a CaspFlagList with 16-bit values from a span.
    /// Format: uint32 count + (count * 4-byte flag entries)
    /// </summary>
    public static CaspFlagList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        var list = Parse(data, ref offset, false);
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
        if (offset + 4 > data.Length)
        {
            throw new InvalidDataException("Truncated flag list count");
        }

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count > MaxFlagCount)
        {
            throw new InvalidDataException($"Flag count {count} exceeds maximum {MaxFlagCount}");
        }

        var list = new CaspFlagList { Capacity = (int)count };
        int flagSize = CaspFlag.GetSerializedSize(use32BitValue);

        for (int i = 0; i < count; i++)
        {
            if (offset + flagSize > data.Length)
            {
                throw new InvalidDataException($"Truncated flag data at index {i}");
            }

            list.Add(CaspFlag.Parse(data[offset..], use32BitValue));
            offset += flagSize;
        }

        return list;
    }

    /// <summary>Writes this list with 16-bit values to a span.</summary>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        Serialize(buffer, ref offset, false);
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

        int flagSize = CaspFlag.GetSerializedSize(use32BitValue);
        foreach (var flag in this)
        {
            flag.WriteTo(buffer[offset..], use32BitValue);
            offset += flagSize;
        }
    }

    /// <summary>Gets the serialized size in bytes with 16-bit values.</summary>
    public int GetSerializedSize() => GetSerializedSize(false);

    /// <summary>Gets the serialized size with version-specific flag size.</summary>
    public int GetSerializedSize(bool use32BitValue) => 4 + (Count * CaspFlag.GetSerializedSize(use32BitValue));
}
