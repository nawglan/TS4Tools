
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Defines the layout of a single vertex element within a vertex buffer.
/// Source: s4pi Wrappers/MeshChunks/VRTF.cs ElementLayout class lines 180-291
/// </summary>
public readonly struct ElementLayout : IEquatable<ElementLayout>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 4;

    /// <summary>Semantic usage of this element (Position, Normal, UV, etc.).</summary>
    public ElementUsage Usage { get; }

    /// <summary>Index for multiple elements of the same usage (e.g., UV0, UV1).</summary>
    public byte UsageIndex { get; }

    /// <summary>Binary format of the element data.</summary>
    public ElementFormat Format { get; }

    /// <summary>Byte offset within the vertex stride.</summary>
    public byte Offset { get; }

    public ElementLayout(ElementUsage usage, byte usageIndex, ElementFormat format, byte offset)
    {
        Usage = usage;
        UsageIndex = usageIndex;
        Format = format;
        Offset = offset;
    }

    /// <summary>
    /// Reads an ElementLayout from the span at the specified position.
    /// </summary>
    public static ElementLayout Read(ReadOnlySpan<byte> data, ref int position)
    {
        var usage = (ElementUsage)data[position];
        var usageIndex = data[position + 1];
        var format = (ElementFormat)data[position + 2];
        var offset = data[position + 3];
        position += Size;
        return new ElementLayout(usage, usageIndex, format, offset);
    }

    /// <summary>
    /// Writes the layout to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        data[position] = (byte)Usage;
        data[position + 1] = UsageIndex;
        data[position + 2] = (byte)Format;
        data[position + 3] = Offset;
        position += Size;
    }

    public bool Equals(ElementLayout other) =>
        Usage == other.Usage && UsageIndex == other.UsageIndex &&
        Format == other.Format && Offset == other.Offset;

    public override bool Equals(object? obj) => obj is ElementLayout other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Usage, UsageIndex, Format, Offset);
    public static bool operator ==(ElementLayout left, ElementLayout right) => left.Equals(right);
    public static bool operator !=(ElementLayout left, ElementLayout right) => !left.Equals(right);

    public override string ToString() =>
        $"{Usage}[{UsageIndex}]: {Format} @ offset 0x{Offset:X2}";
}

/// <summary>
/// VRTF (Vertex Format) block - defines the layout of vertices in a vertex buffer.
/// Resource Type: 0x01D0E723
/// Source: s4pi Wrappers/MeshChunks/VRTF.cs
/// </summary>
public sealed class VrtfBlock : RcolBlock
{
    /// <summary>Resource type identifier for VRTF.</summary>
    public const uint TypeId = 0x01D0E723;

    /// <inheritdoc/>
    public override string Tag => "VRTF";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; private set; } = 0x00000002;

    /// <summary>Size in bytes of a single vertex (stride).</summary>
    public int Stride { get; private set; }

    /// <summary>Whether the format uses extended encoding.</summary>
    public bool ExtendedFormat { get; private set; }

    /// <summary>List of element layouts defining the vertex structure.</summary>
    public List<ElementLayout> Layouts { get; } = [];

    /// <summary>
    /// Creates an empty VRTF block.
    /// </summary>
    public VrtfBlock() : base()
    {
    }

    /// <summary>
    /// Creates a VRTF block from raw data.
    /// </summary>
    public VrtfBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the VRTF block data.
    /// Source: s4pi Wrappers/MeshChunks/VRTF.cs Parse() lines 344-360
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid VRTF tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Stride = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        int layoutCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        // Validate count
        if (layoutCount < 0 || layoutCount > 100)
            throw new InvalidDataException($"Invalid VRTF layout count: {layoutCount}");

        ExtendedFormat = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]) != 0;
        pos += 4;

        // Read layouts
        Layouts.Clear();
        for (int i = 0; i < layoutCount; i++)
        {
            Layouts.Add(ElementLayout.Read(data, ref pos));
        }
    }

    /// <summary>
    /// Serializes the VRTF block back to bytes.
    /// Source: s4pi Wrappers/MeshChunks/VRTF.cs UnParse() lines 361-374
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        int size = 20 + (Layouts.Count * ElementLayout.Size);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag
        buffer[pos++] = (byte)'V';
        buffer[pos++] = (byte)'R';
        buffer[pos++] = (byte)'T';
        buffer[pos++] = (byte)'F';

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Version);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), Stride);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), Layouts.Count);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), ExtendedFormat ? 1u : 0u);
        pos += 4;

        // Write layouts
        foreach (var layout in Layouts)
        {
            layout.Write(buffer, ref pos);
        }

        return buffer;
    }

    /// <summary>
    /// Creates a default VRTF for sun shadow meshes.
    /// Source: s4pi Wrappers/MeshChunks/VRTF.cs CreateDefaultForSunShadow() lines 46-52
    /// </summary>
    public static VrtfBlock CreateDefaultForSunShadow()
    {
        var v = new VrtfBlock();
        v.Layouts.Add(new ElementLayout(ElementUsage.Position, 0, ElementFormat.Short4, 0));
        v.Stride = v.Layouts.Sum(x => x.Format.ByteSize());
        return v;
    }

    /// <summary>
    /// Creates a default VRTF for drop shadow meshes.
    /// Source: s4pi Wrappers/MeshChunks/VRTF.cs CreateDefaultForDropShadow() lines 53-65
    /// </summary>
    public static VrtfBlock CreateDefaultForDropShadow()
    {
        var v = new VrtfBlock();
        v.Layouts.Add(new ElementLayout(ElementUsage.Position, 0, ElementFormat.UShort4N, 0));
        int offset = v.Layouts.Sum(x => x.Format.ByteSize());
        v.Layouts.Add(new ElementLayout(ElementUsage.UV, 0, ElementFormat.Short4DropShadow, (byte)offset));
        v.Stride = v.Layouts.Sum(x => x.Format.ByteSize());
        return v;
    }

    /// <summary>
    /// Finds a layout by usage type.
    /// </summary>
    public ElementLayout? FindLayout(ElementUsage usage) =>
        Layouts.FirstOrDefault(x => x.Usage == usage);

    /// <summary>
    /// Finds all layouts with the specified usage.
    /// </summary>
    public IEnumerable<ElementLayout> FindLayouts(ElementUsage usage) =>
        Layouts.Where(x => x.Usage == usage);
}
