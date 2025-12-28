
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a CAS part tag/flag entry.
/// The category identifies the flag type (e.g., Mood, Color, Style).
/// The value is the specific flag value.
///
/// Binary format: ushort category + ushort value (4 bytes total)
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResourceTS4.cs
/// See lines 558-594: Flag class with ushort flagCatagory + ushort flagValue
/// </summary>
public readonly record struct CaspFlag(ushort Category, ushort Value)
{
    /// <summary>
    /// Size in bytes when serialized.
    /// </summary>
    public const int SerializedSize = 4;

    /// <summary>
    /// Parses a CaspFlag from a span.
    /// </summary>
    public static CaspFlag Parse(ReadOnlySpan<byte> data)
    {
        ushort category = BinaryPrimitives.ReadUInt16LittleEndian(data);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
        return new CaspFlag(category, value);
    }

    /// <summary>
    /// Writes this CaspFlag to a span.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Category);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Value);
    }
}
