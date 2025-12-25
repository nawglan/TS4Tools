using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// SimData (DATA) resource containing structured game data.
/// Resource Type: 0x545AC67A
/// </summary>
[ResourceHandler(0x545AC67A)]
public sealed class SimDataResource : TypedResource
{
    /// <summary>
    /// Magic bytes: "DATA"
    /// </summary>
    public const uint Magic = 0x41544144; // "DATA" in little-endian

    private byte[] _rawData = [];

    /// <summary>
    /// Format version.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Offset to table data.
    /// </summary>
    public uint TableOffset { get; private set; }

    /// <summary>
    /// Number of schemas in this SimData.
    /// </summary>
    public int SchemaCount { get; private set; }

    /// <summary>
    /// Number of tables/records in this SimData.
    /// </summary>
    public int TableCount { get; private set; }

    /// <summary>
    /// Raw data size in bytes.
    /// </summary>
    public int DataSize => _rawData.Length;

    /// <summary>
    /// Whether this is a valid SimData file.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Schema definitions found in the SimData.
    /// </summary>
    public IReadOnlyList<SimDataSchema> Schemas => _schemas;
    private readonly List<SimDataSchema> _schemas = [];

    /// <summary>
    /// Creates a new SimData resource by parsing data.
    /// </summary>
    public SimDataResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _schemas.Clear();
        IsValid = false;

        if (data.Length < 16)
            return;

        // Read and validate magic
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (magic != Magic)
            return;

        IsValid = true;
        int offset = 4;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // The exact header structure depends on version
        // Version 0x100: Basic format
        // Version 0x101+: Extended format with more header fields

        if (Version >= 0x100 && data.Length >= 32)
        {
            // Read table offset and counts based on version
            TableOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            SchemaCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            // Parse schema info if available
            if (Version >= 0x101 && data.Length >= offset + 8)
            {
                int schemaOffset = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;

                TableCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;

                // Try to parse schemas (read-only for now)
                TryParseSchemas(data, schemaOffset);
            }
        }
    }

    private void TryParseSchemas(ReadOnlySpan<byte> data, int schemaOffset)
    {
        if (schemaOffset <= 0 || schemaOffset >= data.Length)
            return;

        // Each schema entry is typically:
        // - Name offset (4 bytes)
        // - Name hash (4 bytes)
        // - Schema size (4 bytes)
        // - Column count (4 bytes)
        // etc.

        // For now, just create placeholder schema entries
        for (int i = 0; i < SchemaCount && i < 100; i++)
        {
            _schemas.Add(new SimDataSchema
            {
                Index = i,
                Name = $"Schema_{i}"
            });
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // SimData is complex - just return original data for now
        return _rawData;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        Version = 0;
        TableOffset = 0;
        SchemaCount = 0;
        TableCount = 0;
        IsValid = false;
        _schemas.Clear();
    }

    /// <summary>
    /// Gets the raw data for inspection.
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _rawData;
}

/// <summary>
/// Represents a schema definition in SimData.
/// </summary>
public sealed class SimDataSchema
{
    /// <summary>Index within the SimData.</summary>
    public int Index { get; init; }

    /// <summary>Schema name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Schema name hash.</summary>
    public uint NameHash { get; init; }

    /// <summary>Number of columns/fields.</summary>
    public int ColumnCount { get; init; }
}
