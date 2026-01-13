
namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Pack display options for catalog items.
/// Source: CatalogCommon.cs lines 562-566
/// </summary>
[Flags]
public enum PackDisplayOption : byte
{
    /// <summary>
    /// No special display options.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hide the pack icon in the catalog UI.
    /// </summary>
    HidePackIcon = 1
}

/// <summary>
/// Common block shared by all catalog resources.
/// Contains name hash, price, tags, selling points, and other shared properties.
/// Source: CatalogCommon.cs lines 36-558
/// </summary>
public sealed class CatalogCommon
{
    /// <summary>
    /// Default/recommended version for new resources.
    /// </summary>
    public const uint DefaultVersion = 0x0B; // Version 11

    /// <summary>
    /// FNV-32 offset basis used as default for empty hashes.
    /// </summary>
    public const uint FnvOffsetBasis = 0x811C9DC5;

    #region Properties

    /// <summary>
    /// Common block format version (0x09, 0x0A, 0x0B, etc.).
    /// </summary>
    public uint CommonBlockVersion { get; set; } = DefaultVersion;

    /// <summary>
    /// FNV-32 hash of the display name string key.
    /// </summary>
    public uint NameHash { get; set; }

    /// <summary>
    /// FNV-32 hash of the description string key.
    /// </summary>
    public uint DescriptionHash { get; set; }

    /// <summary>
    /// Price in Simoleons.
    /// </summary>
    public uint Price { get; set; }

    /// <summary>
    /// Instance ID of the thumbnail image.
    /// </summary>
    public ulong ThumbnailHash { get; set; }

    /// <summary>
    /// Developer category flags.
    /// </summary>
    public uint DevCategoryFlags { get; set; }

    /// <summary>
    /// Product style TGI references.
    /// </summary>
    public TgiReferenceList ProductStyles { get; set; } = new();

    /// <summary>
    /// Pack identifier (version 10+).
    /// </summary>
    public short PackId { get; set; }

    /// <summary>
    /// Pack display options (version 10+).
    /// </summary>
    public PackDisplayOption PackOptions { get; set; }

    /// <summary>
    /// Reserved bytes, always 9 zeros (version 10+).
    /// </summary>
    public byte[] ReservedBytes { get; set; } = new byte[9];

    /// <summary>
    /// Legacy unused field (version &lt; 10).
    /// </summary>
    public byte Unused2 { get; set; } = 1;

    /// <summary>
    /// Legacy unused field (version &lt; 10, only if Unused2 > 0).
    /// </summary>
    public byte Unused3 { get; set; }

    /// <summary>
    /// Category tags for filtering.
    /// </summary>
    public CatalogTagList Tags { get; set; } = new();

    /// <summary>
    /// Selling points (commodity, amount pairs).
    /// </summary>
    public SellingPointList SellingPoints { get; set; } = new();

    /// <summary>
    /// Hash of item that unlocks this item.
    /// </summary>
    public uint UnlockByHash { get; set; }

    /// <summary>
    /// Hash of item unlocked by this item.
    /// </summary>
    public uint UnlockedByHash { get; set; }

    /// <summary>
    /// Sort priority for swatch colors.
    /// </summary>
    public ushort SwatchColorsSortPriority { get; set; } = 0xFFFF;

    /// <summary>
    /// Instance ID of variant thumbnail image.
    /// </summary>
    public ulong VarientThumbImageHash { get; set; }

    #endregion

    /// <summary>
    /// Creates a new empty CatalogCommon with default values.
    /// </summary>
    public CatalogCommon()
    {
    }

    /// <summary>
    /// Parses CatalogCommon from binary data.
    /// Source: CatalogCommon.cs lines 342-389
    /// </summary>
    /// <param name="data">The data span positioned at the common block.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed CatalogCommon.</returns>
    public static CatalogCommon Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        var common = new CatalogCommon();
        int offset = 0;

        // Read fixed header fields
        common.CommonBlockVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.DescriptionHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.Price = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.ThumbnailHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        common.DevCategoryFlags = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // ProductStyles TGI list (byte count prefix)
        common.ProductStyles = TgiReferenceList.Parse(data[offset..], out int stylesBytes);
        offset += stylesBytes;

        // Version-specific fields
        if (common.CommonBlockVersion >= 10)
        {
            common.PackId = BinaryPrimitives.ReadInt16LittleEndian(data[offset..]);
            offset += 2;

            common.PackOptions = (PackDisplayOption)data[offset++];

            common.ReservedBytes = data.Slice(offset, 9).ToArray();
            offset += 9;
        }
        else
        {
            common.Unused2 = data[offset++];
            if (common.Unused2 > 0)
            {
                common.Unused3 = data[offset++];
            }
        }

        // Tags - version 11+ uses uint32, earlier uses uint16
        if (common.CommonBlockVersion >= 11)
        {
            common.Tags = CatalogTagList.ParseV11(data[offset..], out int tagsBytes);
            offset += tagsBytes;
        }
        else
        {
            common.Tags = CatalogTagList.ParseLegacy(data[offset..], out int tagsBytes);
            offset += tagsBytes;
        }

        // Selling points
        common.SellingPoints = SellingPointList.Parse(data[offset..], out int sellingBytes);
        offset += sellingBytes;

        // Remaining fixed fields
        common.UnlockByHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.UnlockedByHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        common.SwatchColorsSortPriority = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        common.VarientThumbImageHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        bytesRead = offset;
        return common;
    }

    /// <summary>
    /// Calculates the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize()
    {
        int size = 0;

        // Fixed header: version(4) + nameHash(4) + descHash(4) + price(4) + thumbHash(8) + devFlags(4) = 28
        size += 28;

        // ProductStyles
        size += ProductStyles.GetSerializedSize();

        // Version-specific
        if (CommonBlockVersion >= 10)
        {
            size += 2 + 1 + 9; // packId + packFlags + reserved
        }
        else
        {
            size += 1; // unused2
            if (Unused2 > 0)
                size += 1; // unused3
        }

        // Tags
        if (CommonBlockVersion >= 11)
            size += Tags.GetSerializedSizeV11();
        else
            size += Tags.GetSerializedSizeLegacy();

        // Selling points
        size += SellingPoints.GetSerializedSize();

        // Remaining: unlockBy(4) + unlockedBy(4) + swatchSort(2) + varientThumb(8) = 18
        size += 18;

        return size;
    }

    /// <summary>
    /// Writes the CatalogCommon to a buffer.
    /// Source: CatalogCommon.cs lines 391-441
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;

        // Fixed header fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], CommonBlockVersion);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], NameHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], DescriptionHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Price);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], ThumbnailHash);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], DevCategoryFlags);
        offset += 4;

        // ProductStyles
        offset += ProductStyles.WriteTo(buffer[offset..]);

        // Version-specific fields
        if (CommonBlockVersion >= 10)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer[offset..], PackId);
            offset += 2;

            buffer[offset++] = (byte)PackOptions;

            ReservedBytes ??= new byte[9];
            ReservedBytes.CopyTo(buffer[offset..]);
            offset += 9;
        }
        else
        {
            buffer[offset++] = Unused2;
            if (Unused2 > 0)
            {
                buffer[offset++] = Unused3;
            }
        }

        // Tags
        if (CommonBlockVersion >= 11)
            offset += Tags.WriteToV11(buffer[offset..]);
        else
            offset += Tags.WriteToLegacy(buffer[offset..]);

        // Selling points
        offset += SellingPoints.WriteTo(buffer[offset..]);

        // Remaining fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], UnlockByHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], UnlockedByHash);
        offset += 4;

        BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], SwatchColorsSortPriority);
        offset += 2;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], VarientThumbImageHash);
        offset += 8;

        return offset;
    }
}
