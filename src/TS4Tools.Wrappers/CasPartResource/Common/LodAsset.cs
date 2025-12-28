
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a LOD asset entry (12 bytes).
/// Source: legacy_references/.../Handlers/LODInfoEntry.cs (LodAssets inner class)
/// </summary>
public readonly record struct LodAsset(int Sorting, int SpecLevel, int CastShadow)
{
    /// <summary>
    /// Size in bytes when serialized.
    /// </summary>
    public const int SerializedSize = 12;

    /// <summary>
    /// Parses a LodAsset from a span.
    /// </summary>
    public static LodAsset Parse(ReadOnlySpan<byte> data)
    {
        int sorting = BinaryPrimitives.ReadInt32LittleEndian(data);
        int specLevel = BinaryPrimitives.ReadInt32LittleEndian(data[4..]);
        int castShadow = BinaryPrimitives.ReadInt32LittleEndian(data[8..]);
        return new LodAsset(sorting, specLevel, castShadow);
    }

    /// <summary>
    /// Writes this LodAsset to a span.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Sorting);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], SpecLevel);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], CastShadow);
    }
}
