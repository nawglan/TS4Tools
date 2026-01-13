namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Defines the semantic usage type for GEOM vertex elements.
/// Note: This is different from ElementUsage in VertexEnums.cs which uses VRTF values.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 245-255
/// </summary>
public enum GeomUsageType : uint
{
    /// <summary>Vertex position (3 floats: X, Y, Z).</summary>
    Position = 0x01,

    /// <summary>Surface normal vector (3 floats: X, Y, Z).</summary>
    Normal = 0x02,

    /// <summary>Texture coordinate (2 floats: U, V).</summary>
    UV = 0x03,

    /// <summary>Bone assignment index (uint).</summary>
    BoneAssignment = 0x04,

    /// <summary>Bone weights. v5: 4 floats; v12: 4 bytes.</summary>
    Weights = 0x05,

    /// <summary>Tangent normal vector (3 floats: X, Y, Z).</summary>
    TangentNormal = 0x06,

    /// <summary>Vertex color (ARGB int).</summary>
    Color = 0x07,

    /// <summary>Vertex ID (uint).</summary>
    VertexID = 0x0A
}

/// <summary>
/// Defines the format of a single vertex attribute in a GEOM block.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 312-436
/// </summary>
public sealed class GeomVertexFormat : IEquatable<GeomVertexFormat>
{
    // Expected data types - legacy uses same table for both v5 and v12
    // Source: GEOM.cs lines 284-296 (expectedDataType0C used for both versions in Parse)
    private static readonly uint[] ExpectedDataType =
    [
        0, // Unknown (index 0)
        1, // Position
        1, // Normal
        1, // UV
        2, // BoneAssignment
        2, // Weights (same dataType for both versions, only size differs)
        1, // TangentNormal
        3, // Color
        0, // Unknown (index 8)
        0, // Unknown (index 9)
        4  // VertexID
    ];

    // Expected element sizes for version 0x0C (v5 has different Weights size)
    // Source: GEOM.cs lines 298-310 (expectedElementSize0C)
    private static readonly byte[] ExpectedElementSizeV12 =
    [
        0,  // Unknown (index 0)
        12, // Position (3 floats)
        12, // Normal (3 floats)
        8,  // UV (2 floats)
        4,  // BoneAssignment (uint)
        4,  // Weights (4 bytes in v12)
        12, // TangentNormal (3 floats)
        4,  // Color (int)
        0,  // Unknown (index 8)
        0,  // Unknown (index 9)
        4   // VertexID (uint)
    ];

    // Expected element sizes for version 0x05 (Weights uses floats = 16 bytes)
    // Source: GEOM.cs lines 270-282 (expectedElementSize05) - but note legacy uses v0C for validation!
    private static readonly byte[] ExpectedElementSizeV5 =
    [
        0,  // Unknown (index 0)
        12, // Position (3 floats)
        12, // Normal (3 floats)
        8,  // UV (2 floats)
        4,  // BoneAssignment (uint)
        16, // Weights (4 floats in v5)
        12, // TangentNormal (3 floats)
        4,  // Color (int)
        0,  // Unknown (index 8)
        0,  // Unknown (index 9)
        4   // VertexID (uint)
    ];

    /// <summary>Size in bytes when serialized (usage:4 + dataType:4 + elementSize:1 = 9).</summary>
    public const int Size = 9;

    /// <summary>The GEOM version this format belongs to.</summary>
    public uint Version { get; }

    /// <summary>The usage type of this vertex element.</summary>
    public GeomUsageType Usage { get; }

    /// <summary>
    /// Creates a new vertex format.
    /// </summary>
    public GeomVertexFormat(uint version, GeomUsageType usage)
    {
        Version = version;
        Usage = usage;
    }

    /// <summary>
    /// Reads a vertex format from the span.
    /// Source: GEOM.cs lines 330-363
    /// </summary>
    public static GeomVertexFormat Read(ReadOnlySpan<byte> data, ref int position, uint version)
    {
        var usage = (GeomUsageType)BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        // Validate usage
        uint usageIndex = (uint)usage;
        if (usage == 0 || usageIndex >= ExpectedDataType.Length)
            throw new InvalidDataException($"Invalid GEOM usage type: 0x{usageIndex:X8}");

        // Read and validate data type
        uint dataType = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        if (dataType != ExpectedDataType[usageIndex])
            throw new InvalidDataException(
                $"Invalid GEOM data type for {usage}: expected 0x{ExpectedDataType[usageIndex]:X8}, got 0x{dataType:X8}");

        // Read and validate element size
        byte elementSize = data[position++];

        // Note: For Weights in v5, the actual size is 16 (4 floats), but legacy uses the same table
        // The parsing logic handles this differently based on version
        byte expectedSize = GetExpectedElementSize(usage, version);
        if (elementSize != expectedSize)
            throw new InvalidDataException(
                $"Invalid GEOM element size for {usage}: expected {expectedSize}, got {elementSize}");

        return new GeomVertexFormat(version, usage);
    }

    /// <summary>
    /// Gets the expected element size in bytes for a usage type and GEOM version.
    /// Source: GEOM.cs lines 270-282 (v5) and 298-310 (v12)
    /// </summary>
    public static byte GetExpectedElementSize(GeomUsageType usage, uint version)
    {
        var sizeTable = version == 0x00000005 ? ExpectedElementSizeV5 : ExpectedElementSizeV12;
        return sizeTable[(uint)usage];
    }

    /// <summary>
    /// Writes the vertex format to a span at the specified position.
    /// Source: GEOM.cs lines 365-390
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        uint usageIndex = (uint)Usage;

        BinaryPrimitives.WriteUInt32LittleEndian(data[position..], usageIndex);
        position += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(data[position..], ExpectedDataType[usageIndex]);
        position += 4;

        data[position++] = GetExpectedElementSize(Usage, Version);
    }

    /// <summary>
    /// Writes the vertex format to a binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        uint usageIndex = (uint)Usage;
        writer.Write(usageIndex);
        writer.Write(ExpectedDataType[usageIndex]);
        writer.Write(GetExpectedElementSize(Usage, Version));
    }

    /// <summary>
    /// Compares this vertex format with another for equality.
    /// </summary>
    public bool Equals(GeomVertexFormat? other)
    {
        if (other is null) return false;
        return Usage == other.Usage;
    }

    /// <summary>
    /// Compares this vertex format with an object for equality.
    /// </summary>
    public override bool Equals(object? obj) => obj is GeomVertexFormat other && Equals(other);

    /// <summary>
    /// Gets a hash code for this vertex format.
    /// </summary>
    public override int GetHashCode() => Usage.GetHashCode();

    /// <summary>
    /// Returns a string representation of this vertex format.
    /// </summary>
    public override string ToString() => Usage.ToString();
}

/// <summary>
/// A list of vertex formats that define the structure of each vertex in a GEOM block.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 414-436
/// </summary>
public sealed class GeomVertexFormatList
{
    private readonly List<GeomVertexFormat> _formats = [];

    /// <summary>The GEOM version.</summary>
    public uint Version { get; }

    /// <summary>The formats in this list.</summary>
    public IReadOnlyList<GeomVertexFormat> Formats => _formats;

    /// <summary>Number of formats.</summary>
    public int Count => _formats.Count;

    /// <summary>
    /// Creates an empty format list.
    /// </summary>
    public GeomVertexFormatList(uint version)
    {
        Version = version;
    }

    /// <summary>
    /// Creates a format list from existing formats.
    /// </summary>
    public GeomVertexFormatList(uint version, IEnumerable<GeomVertexFormat> formats)
    {
        Version = version;
        _formats.AddRange(formats);
    }

    /// <summary>
    /// Reads the format list from the span.
    /// Source: GEOM.cs VertexFormatList constructor lines 420-421
    /// </summary>
    public static GeomVertexFormatList Read(ReadOnlySpan<byte> data, ref int position, uint version)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        var list = new GeomVertexFormatList(version);
        for (int i = 0; i < count; i++)
        {
            list._formats.Add(GeomVertexFormat.Read(data, ref position, version));
        }

        return list;
    }

    /// <summary>
    /// Writes the format list to the binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(_formats.Count);
        foreach (var format in _formats)
        {
            format.Write(writer);
        }
    }

    /// <summary>
    /// Calculates the total size in bytes of a single vertex based on this format list.
    /// </summary>
    public int CalculateVertexSize()
    {
        int size = 0;
        foreach (var format in _formats)
        {
            size += GeomVertexFormat.GetExpectedElementSize(format.Usage, Version);
        }
        return size;
    }

    /// <summary>
    /// Gets a format by index.
    /// </summary>
    public GeomVertexFormat this[int index] => _formats[index];
}
