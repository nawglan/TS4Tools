
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a CAS part tag/flag entry.
/// The category identifies the flag type (e.g., Mood, Color, Style).
/// The value is the specific flag value.
///
/// Binary format: ushort category + value. The value is 16-bit before CASP v37 (4 bytes total)
/// and 32-bit from v37 (6 bytes total).
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/Handlers/Flag.cs
/// lines 36-42 (UnParse, 32-bit value) and 49-54 (WriteUInt16, 16-bit value);
/// Lists/FlagList.cs lines 54-60 and 75-87 (16-bit list I/O).
/// </summary>
public readonly record struct CaspFlag(ushort Category, uint Value)
{
    /// <summary>Size in bytes when serialized with a 16-bit value.</summary>
    public const int SerializedSize = 4;

    /// <summary>Size in bytes when serialized with a 32-bit value (v37+).</summary>
    public const int SerializedSize32 = 6;

    /// <summary>Gets the serialized size for the requested value width.</summary>
    public static int GetSerializedSize(bool use32BitValue) => use32BitValue ? SerializedSize32 : SerializedSize;

    /// <summary>Parses a CaspFlag with a 16-bit value from a span.</summary>
    public static CaspFlag Parse(ReadOnlySpan<byte> data) => Parse(data, false);

    /// <summary>Parses a CaspFlag from a span with the requested value width.</summary>
    public static CaspFlag Parse(ReadOnlySpan<byte> data, bool use32BitValue)
    {
        ushort category = BinaryPrimitives.ReadUInt16LittleEndian(data);
        uint value = use32BitValue
            ? BinaryPrimitives.ReadUInt32LittleEndian(data[2..])
            : BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
        return new CaspFlag(category, value);
    }

    /// <summary>Writes this CaspFlag with a 16-bit value to a span.</summary>
    public void WriteTo(Span<byte> buffer) => WriteTo(buffer, false);

    /// <summary>Writes this CaspFlag to a span with the requested value width.</summary>
    public void WriteTo(Span<byte> buffer, bool use32BitValue)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Category);
        if (use32BitValue)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[2..], Value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)Value);
        }
    }
}
