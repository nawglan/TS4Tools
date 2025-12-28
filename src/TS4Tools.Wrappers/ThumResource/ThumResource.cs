using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// THUM metadata resource containing thumbnail positioning/scaling data.
/// </summary>
/// <remarks>
/// NOT the actual thumbnail image - see ThumbnailResource for image data.
/// Contains a "THUM" magic marker and 8 float values for transform data.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs lines 28-199
///
/// Binary format (76 bytes total):
///   0: uint32   Version (usually 4)
///   4: uint32   Unknown1
///   8: uint64   Unknown2
///  16: uint32   Unknown3
///  20: TGIBlock SelfReference (ITG order: Instance 8 bytes, Type 4 bytes, Group 4 bytes = 16 bytes)
///  36: uint32   Unknown4
///  40: uint32   Unknown5
///  44: uint32   Magic ("THUM" = 0x4D554854)
///  48: float32[8] Transform floats (32 bytes)
/// </remarks>
public sealed class ThumResource : TypedResource
{
    /// <summary>
    /// "THUM" magic marker.
    /// </summary>
    private const uint ThumMagic = 0x4D554854;

    /// <summary>
    /// Default version for new resources.
    /// </summary>
    private const uint DefaultVersion = 4;

    /// <summary>
    /// Expected minimum size of a valid THUM resource.
    /// </summary>
    private const int MinimumSize = 80; // 48 header + 32 floats

    /// <summary>
    /// Version number of the resource format.
    /// </summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public ulong Unknown2 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// </summary>
    public uint Unknown3 { get; set; }

    /// <summary>
    /// Self-reference TGI block (stored in ITG order).
    /// </summary>
    public ResourceKey SelfReference { get; set; }

    /// <summary>
    /// Unknown field 4.
    /// </summary>
    public uint Unknown4 { get; set; }

    /// <summary>
    /// Unknown field 5.
    /// </summary>
    public uint Unknown5 { get; set; }

    /// <summary>
    /// Transform float 1.
    /// </summary>
    public float Float1 { get; set; }

    /// <summary>
    /// Transform float 2.
    /// </summary>
    public float Float2 { get; set; }

    /// <summary>
    /// Transform float 3.
    /// </summary>
    public float Float3 { get; set; }

    /// <summary>
    /// Transform float 4.
    /// </summary>
    public float Float4 { get; set; }

    /// <summary>
    /// Transform float 5.
    /// </summary>
    public float Float5 { get; set; }

    /// <summary>
    /// Transform float 6.
    /// </summary>
    public float Float6 { get; set; }

    /// <summary>
    /// Transform float 7.
    /// </summary>
    public float Float7 { get; set; }

    /// <summary>
    /// Transform float 8.
    /// </summary>
    public float Float8 { get; set; }

    /// <summary>
    /// Creates a new ThumResource by parsing data.
    /// </summary>
    public ThumResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: THMResource.cs lines 70-99
        if (data.Length < MinimumSize)
        {
            throw new InvalidDataException(
                $"THUM resource too small: expected at least {MinimumSize} bytes, got {data.Length}");
        }

        var offset = 0;

        // Header fields
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown2 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // TGI in ITG order: Instance (8), Type (4), Group (4)
        var instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
        var resourceType = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        var group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        SelfReference = new ResourceKey(resourceType, group, instance);

        Unknown4 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown5 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate magic
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (magic != ThumMagic)
        {
            throw new InvalidDataException(
                $"Invalid THUM magic: expected 0x{ThumMagic:X8} ('THUM'), got 0x{magic:X8}");
        }

        // Read 8 transform floats
        Float1 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float2 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float3 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float4 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float5 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float6 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float7 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        Float8 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: THMResource.cs lines 103-129
        var buffer = new byte[MinimumSize];
        var offset = 0;

        // Header fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown1);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), Unknown2);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown3);
        offset += 4;

        // TGI in ITG order: Instance (8), Type (4), Group (4)
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), SelfReference.Instance);
        offset += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), SelfReference.ResourceType);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), SelfReference.ResourceGroup);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown4);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown5);
        offset += 4;

        // Magic
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), ThumMagic);
        offset += 4;

        // Transform floats
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float1);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float2);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float3);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float4);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float5);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float6);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float7);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Float8);

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        Unknown1 = 0;
        Unknown2 = 0;
        Unknown3 = 0;
        SelfReference = default;
        Unknown4 = 0;
        Unknown5 = 0;
        Float1 = Float2 = Float3 = Float4 = Float5 = Float6 = Float7 = Float8 = 0f;
    }
}
