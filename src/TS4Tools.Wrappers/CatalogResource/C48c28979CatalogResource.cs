using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Resource type 0x48C28979 catalog resource.
/// Extends ObjectCatalogResource with unknown fields and data blobs.
/// Has version-dependent fields (DataBlob2 only present in version >= 0x19).
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/48C28979CatalogResource.cs lines 37-156
/// </summary>
public sealed class C48c28979CatalogResource : ObjectCatalogResource
{
    /// <summary>
    /// Type ID for 48C28979 catalog resources.
    /// </summary>
    public const uint TypeId = 0x48C28979;

    /// <summary>
    /// Version threshold for DataBlob2.
    /// </summary>
    private const uint DataBlob2VersionThreshold = 0x19;

    /// <summary>
    /// Size of DataBlob1 in bytes.
    /// </summary>
    private const int DataBlob1Size = 29;

    /// <summary>
    /// Size of DataBlob2 in bytes.
    /// </summary>
    private const int DataBlob2Size = 16;

    #region Properties

    /// <summary>
    /// Unknown field 1.
    /// Source: 48C28979CatalogResource.cs line 40
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// Source: 48C28979CatalogResource.cs line 41
    /// </summary>
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// Source: 48C28979CatalogResource.cs line 42
    /// </summary>
    public uint Unknown3 { get; set; }

    /// <summary>
    /// Unknown field 4.
    /// Source: 48C28979CatalogResource.cs line 43
    /// </summary>
    public uint Unknown4 { get; set; }

    /// <summary>
    /// Unknown field 5.
    /// Source: 48C28979CatalogResource.cs line 44
    /// </summary>
    public uint Unknown5 { get; set; }

    /// <summary>
    /// Unknown field 6.
    /// Source: 48C28979CatalogResource.cs line 45
    /// </summary>
    public uint Unknown6 { get; set; }

    /// <summary>
    /// Unknown field 7.
    /// Source: 48C28979CatalogResource.cs line 46
    /// </summary>
    public uint Unknown7 { get; set; }

    /// <summary>
    /// Data blob 1 (29 bytes).
    /// Source: 48C28979CatalogResource.cs line 47
    /// </summary>
    public byte[] DataBlob1 { get; set; } = new byte[DataBlob1Size];

    /// <summary>
    /// Unknown field 8.
    /// Source: 48C28979CatalogResource.cs line 48
    /// </summary>
    public ulong Unknown8 { get; set; }

    /// <summary>
    /// Unknown field 9.
    /// Source: 48C28979CatalogResource.cs line 49
    /// </summary>
    public uint Unknown9 { get; set; }

    /// <summary>
    /// Unknown field 10.
    /// Source: 48C28979CatalogResource.cs line 50
    /// </summary>
    public uint Unknown10 { get; set; }

    /// <summary>
    /// Data blob 2 (16 bytes). Only present if Version >= 0x19.
    /// Source: 48C28979CatalogResource.cs line 52
    /// </summary>
    public byte[] DataBlob2 { get; set; } = new byte[DataBlob2Size];

    /// <summary>
    /// Unknown field 11.
    /// Source: 48C28979CatalogResource.cs line 54
    /// </summary>
    public uint Unknown11 { get; set; }

    /// <summary>
    /// Whether DataBlob2 is present based on version.
    /// </summary>
    public bool HasDataBlob2 => Version >= DataBlob2VersionThreshold;

    #endregion

    /// <summary>
    /// Creates a new 48C28979 catalog resource.
    /// </summary>
    public C48c28979CatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: 48C28979CatalogResource.cs lines 64-79
        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown4 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown5 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown6 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown7 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        DataBlob1 = data.Slice(offset, DataBlob1Size).ToArray();
        offset += DataBlob1Size;

        Unknown8 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        Unknown9 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown10 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // DataBlob2 only present in version >= 0x19
        if (Version >= DataBlob2VersionThreshold)
        {
            DataBlob2 = data.Slice(offset, DataBlob2Size).ToArray();
            offset += DataBlob2Size;
        }

        Unknown11 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Source: 48C28979CatalogResource.cs lines 86-106
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown1);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown2);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown3);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown4);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown5);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown6);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown7);
        offset += 4;

        EnsureDataBlobSize(DataBlob1, DataBlob1Size).CopyTo(buffer[offset..]);
        offset += DataBlob1Size;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], Unknown8);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown9);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown10);
        offset += 4;

        // DataBlob2 only present in version >= 0x19
        if (Version >= DataBlob2VersionThreshold)
        {
            EnsureDataBlobSize(DataBlob2, DataBlob2Size).CopyTo(buffer[offset..]);
            offset += DataBlob2Size;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown11);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        // 7 uints (28) + DataBlob1 (29) + ulong (8) + 2 uints (8) + optional DataBlob2 (16) + uint (4)
        int size = 28 + DataBlob1Size + 8 + 8 + 4;

        if (Version >= DataBlob2VersionThreshold)
            size += DataBlob2Size;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unknown1 = 0;
        Unknown2 = 0;
        Unknown3 = 0;
        Unknown4 = 0;
        Unknown5 = 0;
        Unknown6 = 0;
        Unknown7 = 0;
        DataBlob1 = new byte[DataBlob1Size];
        Unknown8 = 0;
        Unknown9 = 0;
        Unknown10 = 0;
        DataBlob2 = new byte[DataBlob2Size];
        Unknown11 = 0;
    }

    /// <summary>
    /// Ensures a data blob is exactly the expected size.
    /// </summary>
    private static ReadOnlySpan<byte> EnsureDataBlobSize(byte[] blob, int expectedSize)
    {
        if (blob.Length == expectedSize)
            return blob;

        var result = new byte[expectedSize];
        blob.AsSpan(0, Math.Min(blob.Length, expectedSize)).CopyTo(result);
        return result;
    }
}
