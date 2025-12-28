using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a CAS part override (5 bytes).
/// Source: legacy_references/.../Handlers/Override.cs
/// </summary>
public readonly record struct CaspOverride(byte Region, float Layer)
{
    /// <summary>
    /// Size in bytes when serialized.
    /// </summary>
    public const int SerializedSize = 5;

    /// <summary>
    /// Parses a CaspOverride from a span.
    /// </summary>
    public static CaspOverride Parse(ReadOnlySpan<byte> data)
    {
        byte region = data[0];
        float layer = BinaryPrimitives.ReadSingleLittleEndian(data[1..]);
        return new CaspOverride(region, layer);
    }

    /// <summary>
    /// Writes this CaspOverride to a span.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        buffer[0] = Region;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[1..], Layer);
    }
}
