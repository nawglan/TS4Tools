
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A region/layer override entry. Binary format: byte region + float layer (5 bytes).
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/Handlers/Override.cs
/// lines 36-41 (Parse) and 43-48 (UnParse).
/// </summary>
public readonly record struct CaspOverride(byte Region, float Layer)
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int SerializedSize = 5;
}

/// <summary>
/// A list of overrides with a byte count prefix.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/Lists/OverrideList.cs
/// lines 41-61 (Parse/UnParse).
/// </summary>
public sealed class CaspOverrideList : List<CaspOverride>
{
    /// <summary>Creates an empty list.</summary>
    public CaspOverrideList()
    {
    }

    /// <summary>Creates a list with the specified entries.</summary>
    public CaspOverrideList(IEnumerable<CaspOverride> overrides) : base(overrides)
    {
    }

    /// <summary>Parses an override list from <paramref name="data"/> at <paramref name="offset"/>.</summary>
    public static CaspOverrideList Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset >= data.Length)
        {
            throw new InvalidDataException("Truncated override list count");
        }

        byte count = data[offset++];
        if (offset + (count * CaspOverride.SerializedSize) > data.Length)
        {
            throw new InvalidDataException($"Truncated override data: {count} entries");
        }

        var list = new CaspOverrideList { Capacity = count };
        for (int i = 0; i < count; i++)
        {
            byte region = data[offset++];
            float layer = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            list.Add(new CaspOverride(region, layer));
        }

        return list;
    }

    /// <summary>Writes this list to a span and returns the number of bytes written.</summary>
    public int WriteTo(Span<byte> buffer)
    {
        if (Count > 255)
        {
            throw new InvalidOperationException($"OverrideList count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;
        foreach (var entry in this)
        {
            buffer[offset++] = entry.Region;
            BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], entry.Layer);
            offset += 4;
        }

        return offset;
    }

    /// <summary>Gets the serialized size in bytes.</summary>
    public int GetSerializedSize() => 1 + (Count * CaspOverride.SerializedSize);
}
