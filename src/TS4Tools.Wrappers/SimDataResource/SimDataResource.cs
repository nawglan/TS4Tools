using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// SimData (DATA) resource containing structured game data.
/// Resource Type: 0x545AC67A
/// Source: DataResource.cs from legacy s4pi Wrappers/DataResource/
/// </summary>
[ResourceHandler(0x545AC67A)]
public sealed class SimDataResource : TypedResource
{
    /// <summary>
    /// Magic bytes: "DATA"
    /// </summary>
    public const uint Magic = 0x41544144; // "DATA" in little-endian

    /// <summary>
    /// Header size in bytes (6 x uint32 = 24 bytes).
    /// </summary>
    private const int HeaderSize = 24;

    /// <summary>
    /// Structure entry size in bytes (6 x uint32 = 24 bytes).
    /// </summary>
    private const int StructureEntrySize = 24;

    /// <summary>
    /// Field entry size in bytes (5 x uint32 = 20 bytes).
    /// </summary>
    private const int FieldEntrySize = 20;

    /// <summary>
    /// Data entry size in bytes (7 x uint32 = 28 bytes).
    /// </summary>
    private const int DataEntrySize = 28;

    private byte[] _rawData = [];

    /// <summary>
    /// Format version (typically 0x100).
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Position of the data table in the file.
    /// </summary>
    public uint DataTablePosition { get; private set; }

    /// <summary>
    /// Position of the structure table in the file.
    /// </summary>
    public uint StructureTablePosition { get; private set; }

    /// <summary>
    /// Raw data size in bytes.
    /// </summary>
    public int DataSize => _rawData.Length;

    /// <summary>
    /// Whether this is a valid SimData file.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Schema (structure) definitions found in the SimData.
    /// </summary>
    public IReadOnlyList<SimDataSchema> Schemas => _schemas;
    private readonly List<SimDataSchema> _schemas = [];

    /// <summary>
    /// Data tables found in the SimData.
    /// </summary>
    public IReadOnlyList<SimDataTable> Tables => _tables;
    private readonly List<SimDataTable> _tables = [];

    /// <summary>
    /// Number of schemas in this SimData.
    /// </summary>
    public int SchemaCount => _schemas.Count;

    /// <summary>
    /// Number of tables in this SimData.
    /// </summary>
    public int TableCount => _tables.Count;

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
        _tables.Clear();
        IsValid = false;

        if (data.Length < HeaderSize)
            return;

        // Read and validate magic
        // Source: DataResource.cs lines 48-52
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (magic != Magic)
            return;

        // Read version
        // Source: DataResource.cs line 54
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        // Read data table position (relative offset)
        // Source: DataResource.cs lines 56-58
        if (!SimDataReader.TryGetOffset(data, 8, out uint dataTablePos))
            return;
        DataTablePosition = dataTablePos;

        // Read data count
        int dataCount = SimDataReader.ReadInt32(data, 12);
        if (dataCount < 0 || dataCount > 10000)
            return; // Security: prevent unreasonable counts

        // Read structure table position (relative offset)
        // Source: DataResource.cs lines 62-64
        if (!SimDataReader.TryGetOffset(data, 16, out uint structTablePos))
            return;
        StructureTablePosition = structTablePos;

        // Read structure count
        int structCount = SimDataReader.ReadInt32(data, 20);
        if (structCount < 0 || structCount > 10000)
            return; // Security: prevent unreasonable counts

        IsValid = true;

        // Parse structure table (two-pass: headers first, then field tables)
        // Source: DataResource.cs line 70
        ParseStructures(data, (int)structTablePos, structCount);

        // Parse data table
        // Source: DataResource.cs line 72
        ParseDataTables(data, (int)dataTablePos, dataCount);
    }

    /// <summary>
    /// Parses the structure (schema) table.
    /// Source: DataResource.cs StructureList constructor, lines 321-343
    /// </summary>
    private void ParseStructures(ReadOnlySpan<byte> data, int tablePosition, int count)
    {
        if (tablePosition < 0 || tablePosition >= data.Length)
            return;

        // First pass: parse structure headers
        for (int i = 0; i < count; i++)
        {
            int pos = tablePosition + (i * StructureEntrySize);
            if (pos + StructureEntrySize > data.Length)
                break;

            var schema = ParseStructureEntry(data, pos, i);
            _schemas.Add(schema);
        }

        // Second pass: parse field tables for each structure
        // Source: DataResource.cs lines 337-341
        foreach (var schema in _schemas)
        {
            if (schema.FieldTablePosition != SimDataReader.NullOffset &&
                schema.FieldTablePosition < data.Length)
            {
                ParseFieldTable(data, schema);
            }
        }
    }

    /// <summary>
    /// Parses a single structure entry.
    /// Source: DataResource.cs Structure.Parse, lines 211-223
    /// </summary>
    private static SimDataSchema ParseStructureEntry(ReadOnlySpan<byte> data, int position, int index)
    {
        // Read name position (relative offset)
        SimDataReader.TryGetOffset(data, position, out uint namePos);
        string name = SimDataReader.ReadNullTerminatedString(data, namePos);

        uint nameHash = SimDataReader.ReadUInt32(data, position + 4);
        uint unknown08 = SimDataReader.ReadUInt32(data, position + 8);
        uint size = SimDataReader.ReadUInt32(data, position + 12);

        // Read field table position (relative offset)
        SimDataReader.TryGetOffset(data, position + 16, out uint fieldTablePos);
        uint fieldCount = SimDataReader.ReadUInt32(data, position + 20);

        return new SimDataSchema
        {
            Index = index,
            Name = name,
            NameHash = nameHash,
            Unknown08 = unknown08,
            Size = size,
            FieldTablePosition = fieldTablePos,
            FieldCount = (int)fieldCount,
            Position = (uint)position
        };
    }

    /// <summary>
    /// Parses the field table for a structure.
    /// Source: DataResource.cs Structure.ParseFieldTable, lines 225-236
    /// </summary>
    private static void ParseFieldTable(ReadOnlySpan<byte> data, SimDataSchema schema)
    {
        int pos = (int)schema.FieldTablePosition;

        for (int i = 0; i < schema.FieldCount; i++)
        {
            int fieldPos = pos + (i * FieldEntrySize);
            if (fieldPos + FieldEntrySize > data.Length)
                break;

            var field = ParseFieldEntry(data, fieldPos, i);
            schema.AddField(field);
        }
    }

    /// <summary>
    /// Parses a single field entry.
    /// Source: DataResource.cs Field.Parse, lines 416-427
    /// </summary>
    private static SimDataField ParseFieldEntry(ReadOnlySpan<byte> data, int position, int index)
    {
        // Read name position (relative offset)
        SimDataReader.TryGetOffset(data, position, out uint namePos);
        string name = SimDataReader.ReadNullTerminatedString(data, namePos);

        uint nameHash = SimDataReader.ReadUInt32(data, position + 4);
        uint typeValue = SimDataReader.ReadUInt32(data, position + 8);
        uint dataOffset = SimDataReader.ReadUInt32(data, position + 12);

        // Read unknown10 position (relative offset)
        SimDataReader.TryGetOffset(data, position + 16, out uint unknown10Pos);

        return new SimDataField
        {
            Index = index,
            Name = name,
            NameHash = nameHash,
            Type = (SimDataFieldType)typeValue,
            TypeValue = typeValue,
            DataOffset = dataOffset,
            Unknown10Position = unknown10Pos,
            Position = (uint)position
        };
    }

    /// <summary>
    /// Parses the data table.
    /// Source: DataResource.cs DataList constructor, lines 719-751
    /// </summary>
    private void ParseDataTables(ReadOnlySpan<byte> data, int tablePosition, int count)
    {
        if (tablePosition < 0 || tablePosition >= data.Length)
            return;

        // First pass: parse data entry headers
        var entries = new List<(SimDataTable Table, uint FieldPos)>();

        for (int i = 0; i < count; i++)
        {
            int pos = tablePosition + (i * DataEntrySize);
            if (pos + DataEntrySize > data.Length)
                break;

            var (table, fieldPos) = ParseDataEntry(data, pos, i);
            _tables.Add(table);
            entries.Add((table, fieldPos));
        }

        // Sort by field position to calculate data lengths
        // Source: DataResource.cs lines 742-750
        var sorted = entries.OrderBy(e => e.FieldPos).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var (table, fieldPos) = sorted[i];

            if (fieldPos == SimDataReader.NullOffset || fieldPos >= data.Length)
                continue;

            // Calculate length based on next entry or structure table
            uint nextPos = i < sorted.Count - 1
                ? sorted[i + 1].FieldPos
                : StructureTablePosition;

            if (nextPos <= fieldPos || nextPos > data.Length)
                continue;

            uint length = nextPos - fieldPos;
            table.SetRawData(data.Slice((int)fieldPos, (int)length).ToArray());
        }
    }

    /// <summary>
    /// Parses a single data entry.
    /// Source: DataResource.cs Data.Parse, lines 572-593
    /// </summary>
    private (SimDataTable Table, uint FieldPos) ParseDataEntry(ReadOnlySpan<byte> data, int position, int index)
    {
        // Read name position (relative offset)
        SimDataReader.TryGetOffset(data, position, out uint namePos);
        string name = SimDataReader.ReadNullTerminatedString(data, namePos);

        uint nameHash = SimDataReader.ReadUInt32(data, position + 4);

        // Read structure position (relative offset)
        SimDataReader.TryGetOffset(data, position + 8, out uint structPos);

        // Find the associated schema
        SimDataSchema? schema = null;
        foreach (var s in _schemas)
        {
            if (s.Position == structPos)
            {
                schema = s;
                break;
            }
        }

        uint unknown0C = SimDataReader.ReadUInt32(data, position + 12);
        uint unknown10 = SimDataReader.ReadUInt32(data, position + 16);

        // Read field position (relative offset)
        SimDataReader.TryGetOffset(data, position + 20, out uint fieldPos);
        uint fieldCount = SimDataReader.ReadUInt32(data, position + 24);

        var table = new SimDataTable
        {
            Index = index,
            Name = name,
            NameHash = nameHash,
            Schema = schema,
            Unknown0C = unknown0C,
            Unknown10 = unknown10,
            RowCount = (int)fieldCount,
            Position = (uint)position
        };

        return (table, fieldPos);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // SimData serialization is complex with relative offsets.
        // For now, return the original raw data unchanged.
        // Full serialization would require:
        // 1. Write header with blank offsets
        // 2. Write data table entries
        // 3. Write structure table entries
        // 4. Write field tables
        // 5. Write all name strings
        // 6. Patch all relative offsets
        // Source: DataResource.cs UnParse, lines 80-168
        return _rawData;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        Version = 0x100;
        DataTablePosition = 0;
        StructureTablePosition = 0;
        IsValid = false;
        _schemas.Clear();
        _tables.Clear();
    }

    /// <summary>
    /// Gets the raw data for inspection.
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _rawData;
}

/// <summary>
/// Represents a schema (structure) definition in SimData.
/// Source: DataResource.cs Structure class, lines 188-317
/// </summary>
public sealed class SimDataSchema
{
    private readonly List<SimDataField> _fields = [];

    /// <summary>Index within the SimData.</summary>
    public int Index { get; init; }

    /// <summary>Schema name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Schema name hash (FNV32).</summary>
    public uint NameHash { get; init; }

    /// <summary>Unknown value at offset 0x08.</summary>
    public uint Unknown08 { get; init; }

    /// <summary>Byte size of data for this schema.</summary>
    public uint Size { get; init; }

    /// <summary>Position of the field table in the file.</summary>
    public uint FieldTablePosition { get; init; }

    /// <summary>Number of fields in this schema.</summary>
    public int FieldCount { get; init; }

    /// <summary>Position of this entry in the file.</summary>
    internal uint Position { get; init; }

    /// <summary>Fields (columns) defined by this schema.</summary>
    public IReadOnlyList<SimDataField> Fields => _fields;

    /// <summary>Number of columns/fields.</summary>
    public int ColumnCount => _fields.Count;

    internal void AddField(SimDataField field) => _fields.Add(field);
}

/// <summary>
/// Represents a field (column) definition within a schema.
/// Source: DataResource.cs Field class, lines 397-495
/// </summary>
public sealed class SimDataField
{
    /// <summary>Index within the schema.</summary>
    public int Index { get; init; }

    /// <summary>Field name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Field name hash (FNV32).</summary>
    public uint NameHash { get; init; }

    /// <summary>Field data type.</summary>
    public SimDataFieldType Type { get; init; }

    /// <summary>Raw type value (for unknown types).</summary>
    public uint TypeValue { get; init; }

    /// <summary>Offset of this field's data within a row.</summary>
    public uint DataOffset { get; init; }

    /// <summary>Unknown position at offset 0x10.</summary>
    public uint Unknown10Position { get; init; }

    /// <summary>Position of this entry in the file.</summary>
    internal uint Position { get; init; }

    /// <summary>Gets the byte size for this field's type.</summary>
    public int DataSize => Type.GetSize();
}

/// <summary>
/// Represents a data table (record) in SimData.
/// Source: DataResource.cs Data class, lines 546-704
/// </summary>
public sealed class SimDataTable
{
    private byte[] _rawData = [];

    /// <summary>Index within the SimData.</summary>
    public int Index { get; init; }

    /// <summary>Table name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Table name hash (FNV32).</summary>
    public uint NameHash { get; init; }

    /// <summary>Schema that defines this table's structure.</summary>
    public SimDataSchema? Schema { get; init; }

    /// <summary>Unknown value at offset 0x0C.</summary>
    public uint Unknown0C { get; init; }

    /// <summary>Unknown value at offset 0x10.</summary>
    public uint Unknown10 { get; init; }

    /// <summary>Number of rows in this table.</summary>
    public int RowCount { get; init; }

    /// <summary>Position of this entry in the file.</summary>
    internal uint Position { get; init; }

    /// <summary>Raw field data for this table.</summary>
    public ReadOnlyMemory<byte> RawData => _rawData;

    internal void SetRawData(byte[] data) => _rawData = data;

    /// <summary>
    /// Reads a field value from the raw data at the specified row and field.
    /// </summary>
    /// <param name="rowIndex">The row index (0-based).</param>
    /// <param name="field">The field to read.</param>
    /// <returns>The raw bytes for the field value, or empty if out of bounds.</returns>
    public ReadOnlySpan<byte> GetFieldBytes(int rowIndex, SimDataField field)
    {
        if (Schema == null || rowIndex < 0 || rowIndex >= RowCount)
            return ReadOnlySpan<byte>.Empty;

        int rowSize = (int)Schema.Size;
        int offset = (rowIndex * rowSize) + (int)field.DataOffset;
        int size = field.DataSize;

        if (offset < 0 || offset + size > _rawData.Length)
            return ReadOnlySpan<byte>.Empty;

        return _rawData.AsSpan(offset, size);
    }

    /// <summary>
    /// Reads a uint32 field value.
    /// </summary>
    public uint GetUInt32(int rowIndex, SimDataField field)
    {
        var bytes = GetFieldBytes(rowIndex, field);
        if (bytes.Length < 4)
            return 0;
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    /// <summary>
    /// Reads a float field value.
    /// </summary>
    public float GetFloat(int rowIndex, SimDataField field)
    {
        var bytes = GetFieldBytes(rowIndex, field);
        if (bytes.Length < 4)
            return 0;
        return BinaryPrimitives.ReadSingleLittleEndian(bytes);
    }

    /// <summary>
    /// Reads a boolean field value.
    /// </summary>
    public bool GetBoolean(int rowIndex, SimDataField field)
    {
        return GetUInt32(rowIndex, field) != 0;
    }
}
