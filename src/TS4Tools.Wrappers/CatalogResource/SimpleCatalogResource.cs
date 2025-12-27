using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Abstract base class for simple catalog resources that have only:
/// - Version (uint)
/// - CatalogCommon block
/// - Type-specific fields
///
/// This is distinct from AbstractCatalogResource which includes additional
/// aural materials, placement, and object behavior fields used by COBJ/CCOL.
/// Most catalog types (CFTR, CFLT, CFLR, CWAL, etc.) use this simpler pattern.
///
/// Source: CFTRResource.cs lines 115-145 (Parse/UnParse pattern)
/// </summary>
public abstract class SimpleCatalogResource : TypedResource
{
    /// <summary>
    /// Default version for new resources.
    /// </summary>
    public const uint DefaultVersion = 0x07;

    #region Properties

    /// <summary>
    /// Resource format version.
    /// </summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Common catalog block shared by all catalog types.
    /// </summary>
    public CatalogCommon CommonBlock { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new SimpleCatalogResource.
    /// </summary>
    protected SimpleCatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected sealed override void Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        // Version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Common block
        CommonBlock = CatalogCommon.Parse(data[offset..], out int commonBytes);
        offset += commonBytes;

        // Type-specific fields
        ParseTypeSpecific(data, ref offset);
    }

    /// <inheritdoc/>
    protected sealed override ReadOnlyMemory<byte> Serialize()
    {
        int size = CalculateSerializedSize();
        var buffer = new byte[size];
        int offset = 0;

        // Version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        // Common block
        offset += CommonBlock.WriteTo(buffer.AsSpan(offset));

        // Type-specific fields
        SerializeTypeSpecific(buffer.AsSpan(), ref offset);

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        CommonBlock = new CatalogCommon();
        InitializeTypeSpecificDefaults();
    }

    /// <summary>
    /// Parses type-specific fields after the common block.
    /// Override this in derived classes to parse additional fields.
    /// </summary>
    /// <param name="data">The full data span.</param>
    /// <param name="offset">The current offset (after version + common block). Update this as you read.</param>
    protected abstract void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset);

    /// <summary>
    /// Serializes type-specific fields after the common block.
    /// Override this in derived classes to serialize additional fields.
    /// </summary>
    /// <param name="buffer">The full buffer span.</param>
    /// <param name="offset">The current offset (after version + common block). Update this as you write.</param>
    protected abstract void SerializeTypeSpecific(Span<byte> buffer, ref int offset);

    /// <summary>
    /// Gets the size in bytes of the type-specific fields when serialized.
    /// Override this in derived classes.
    /// </summary>
    protected abstract int GetTypeSpecificSerializedSize();

    /// <summary>
    /// Initializes type-specific defaults for a new empty resource.
    /// Override this in derived classes.
    /// </summary>
    protected virtual void InitializeTypeSpecificDefaults()
    {
        // Default implementation does nothing
    }

    private int CalculateSerializedSize()
    {
        int size = 0;

        // Version
        size += 4;

        // Common block
        size += CommonBlock.GetSerializedSize();

        // Type-specific fields
        size += GetTypeSpecificSerializedSize();

        return size;
    }
}
