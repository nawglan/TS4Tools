using System.Buffers.Binary;

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
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize()
    {
        return 4 + (Count * CaspFlag.SerializedSize);
    }
}
