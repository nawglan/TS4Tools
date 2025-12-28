using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a CAS part tag/flag entry.
/// The category identifies the flag type (e.g., Mood, Color, Style).
/// The value is the specific flag value.
///
/// Version-aware binary format:
/// - v37+: ushort category + uint32 value (6 bytes)
/// - v36 and below: ushort category + ushort value (4 bytes)
///
/// Source: legacy_references/.../Handlers/Flag.cs
/// </summary>
public readonly record struct CaspFlag(ushort Category, uint Value)
{
    /// <summary>
    /// Size in bytes when serialized (v37+ format).
    /// </summary>
    public const int SerializedSize = 6;

    /// <summary>
    /// Size in bytes when serialized (legacy v36 and below format).
    /// </summary>
    public const int LegacySerializedSize = 4;

    /// <summary>
    /// Parses a CaspFlag from a span (v37+ format: ushort + uint32).
    /// </summary>
    public static CaspFlag Parse(ReadOnlySpan<byte> data)
    {
        ushort category = BinaryPrimitives.ReadUInt16LittleEndian(data);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[2..]);
        return new CaspFlag(category, value);
    }

    /// <summary>
    /// Parses a CaspFlag from a span (legacy format: ushort + ushort).
    /// </summary>
    public static CaspFlag ParseLegacy(ReadOnlySpan<byte> data)
    {
        ushort category = BinaryPrimitives.ReadUInt16LittleEndian(data);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
        return new CaspFlag(category, value);
    }

    /// <summary>
    /// Writes this CaspFlag to a span (v37+ format: ushort + uint32).
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Category);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[2..], Value);
    }

    /// <summary>
    /// Writes this CaspFlag to a span (legacy format: ushort + ushort).
    /// Values larger than ushort max will be truncated.
    /// </summary>
    public void WriteToLegacy(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Category);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)Value);
    }
}
