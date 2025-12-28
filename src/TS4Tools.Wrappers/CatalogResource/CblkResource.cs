using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Block (CBLK) resource.
/// Used for block/brick building elements.
/// Resource Type: 0x07936CE0
/// Source: CBLKResource.cs lines 27-285
/// </summary>
[ResourceHandler(0x07936CE0)]
public sealed class CblkResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CBLK resources.
    /// </summary>
    public const uint TypeId = 0x07936CE0;

    /// <summary>
    /// Default version for CBLK resources.
    /// </summary>
    public new const uint DefaultVersion = 0x05;

    #region Properties

    /// <summary>
    /// Unknown field 1 (byte).
    /// </summary>
    public byte Unk01 { get; set; }

    /// <summary>
    /// Unknown field 2 (byte).
    /// </summary>
    public byte Unk02 { get; set; }

    /// <summary>
    /// Optional entry list. May be null if not present in data.
    /// </summary>
    public CblkEntryList? Entries { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CBLK resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CblkResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CBLKResource.cs lines 97-113
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unk01 (byte)
        Unk01 = data[offset++];

        // Unk02 (byte)
        Unk02 = data[offset++];

        // CBLKEntryList (optional - only present if more data remains)
        if (offset < data.Length)
        {
            Entries = CblkEntryList.Parse(data[offset..], out int entryBytes);
            offset += entryBytes;
        }
        else
        {
            Entries = null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CBLKResource.cs lines 115-133
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unk01
        buffer[offset++] = Unk01;

        // Unk02
        buffer[offset++] = Unk02;

        // Entries (optional)
        if (Entries != null)
        {
            offset += Entries.WriteTo(buffer[offset..]);
        }
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // Unk01 (1) + Unk02 (1) = 2
        size += 2;

        // Entries (optional)
        if (Entries != null)
        {
            size += Entries.GetSerializedSize();
        }

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unk01 = 0;
        Unk02 = 0;
        Entries = new CblkEntryList();
    }
}
