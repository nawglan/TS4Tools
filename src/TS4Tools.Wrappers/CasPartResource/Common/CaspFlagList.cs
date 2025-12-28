using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of CAS part flags with uint32 count prefix.
/// Version-aware parsing: v37+ uses 6-byte flags, v36 and below uses 4-byte flags.
/// Source: legacy_references/.../Lists/FlagList.cs
/// </summary>
public sealed class CaspFlagList : List<CaspFlag>
{
    /// <summary>
    /// Maximum reasonable number of flags (OOM protection).
    /// </summary>
    private const int MaxFlagCount = 1000;

    /// <summary>
    /// Version threshold for 32-bit flag values.
    /// v37 (0x25) and above use uint32 values; below use ushort.
    /// </summary>
    public const uint Version32BitThreshold = 37;

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
    /// Format: uint32 count + (count * flag entries)
    /// </summary>
    /// <param name="data">The data span to parse from.</param>
    /// <param name="version">The resource version (determines flag size).</param>
    /// <param name="bytesRead">The total bytes consumed.</param>
    public static CaspFlagList Parse(ReadOnlySpan<byte> data, uint version, out int bytesRead)
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

        bool use32BitValues = version >= Version32BitThreshold;
        int flagSize = use32BitValues ? CaspFlag.SerializedSize : CaspFlag.LegacySerializedSize;

        for (int i = 0; i < count; i++)
        {
            if (offset + flagSize > data.Length)
            {
                throw new InvalidDataException($"Truncated flag data at index {i}");
            }

            if (use32BitValues)
            {
                list.Add(CaspFlag.Parse(data[offset..]));
            }
            else
            {
                list.Add(CaspFlag.ParseLegacy(data[offset..]));
            }
            offset += flagSize;
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Writes this list to a span.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="version">The resource version (determines flag size).</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer, uint version)
    {
        int offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], (uint)Count);
        offset += 4;

        bool use32BitValues = version >= Version32BitThreshold;

        foreach (var flag in this)
        {
            if (use32BitValues)
            {
                flag.WriteTo(buffer[offset..]);
                offset += CaspFlag.SerializedSize;
            }
            else
            {
                flag.WriteToLegacy(buffer[offset..]);
                offset += CaspFlag.LegacySerializedSize;
            }
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes for the given version.
    /// </summary>
    public int GetSerializedSize(uint version)
    {
        bool use32BitValues = version >= Version32BitThreshold;
        int flagSize = use32BitValues ? CaspFlag.SerializedSize : CaspFlag.LegacySerializedSize;
        return 4 + (Count * flagSize);
    }
}
