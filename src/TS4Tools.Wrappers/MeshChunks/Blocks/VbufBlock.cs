
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// VBUF (Vertex Buffer) block - contains raw vertex data.
/// The vertex format is defined by a corresponding VRTF block.
/// Resource Type: 0x01D0E6FB
/// Source: s4pi Wrappers/MeshChunks/VBUF.cs
/// </summary>
public class VbufBlock : RcolBlock
{
    /// <summary>Resource type identifier for VBUF.</summary>
    public const uint TypeId = 0x01D0E6FB;

    /// <summary>Resource type identifier for VBUF2 (shadow variant).</summary>
    public const uint TypeId2 = 0x0229684B;

    /// <inheritdoc/>
    public override string Tag => "VBUF";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 0x00000101).</summary>
    public uint Version { get; private set; } = 0x00000101;

    /// <summary>Format flags indicating compression mode.</summary>
    public VbufFormat Flags { get; private set; }

    /// <summary>Reference to swizzle info chunk (for compressed data).</summary>
    public uint SwizzleInfoIndex { get; private set; }

    /// <summary>Raw vertex buffer data.</summary>
    public byte[] Buffer { get; private set; } = [];

    /// <summary>
    /// Creates an empty VBUF block.
    /// </summary>
    public VbufBlock() : base()
    {
    }

    /// <summary>
    /// Creates a VBUF block from raw data.
    /// </summary>
    public VbufBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the VBUF block data.
    /// Source: s4pi Wrappers/MeshChunks/VBUF.cs Parse() lines 120-133
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid VBUF tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Flags = (VbufFormat)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        SwizzleInfoIndex = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read remaining data as vertex buffer
        int bufferSize = data.Length - pos;
        if (bufferSize < 0)
            throw new InvalidDataException("Invalid VBUF buffer size");

        Buffer = data[pos..].ToArray();
    }

    /// <summary>
    /// Serializes the VBUF block back to bytes.
    /// Source: s4pi Wrappers/MeshChunks/VBUF.cs UnParse() lines 135-148
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        int size = 16 + Buffer.Length;
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag
        buffer[pos++] = (byte)'V';
        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'U';
        buffer[pos++] = (byte)'F';

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Version);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)Flags);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), SwizzleInfoIndex);
        pos += 4;

        // Write vertex data
        Buffer.CopyTo(buffer.AsSpan(pos));

        return buffer;
    }

    /// <summary>
    /// Gets the number of vertices in this buffer given a vertex stride.
    /// </summary>
    public int GetVertexCount(int stride) =>
        stride > 0 ? Buffer.Length / stride : 0;

    /// <summary>
    /// Gets raw vertex data for a specific vertex.
    /// </summary>
    public ReadOnlySpan<byte> GetVertexData(int vertexIndex, int stride) =>
        Buffer.AsSpan(vertexIndex * stride, stride);

    /// <summary>
    /// Reads a float value from a vertex at the specified element offset.
    /// </summary>
    public float ReadFloat(int vertexIndex, int stride, int elementOffset)
    {
        int pos = (vertexIndex * stride) + elementOffset;
        return BinaryPrimitives.ReadSingleLittleEndian(Buffer.AsSpan(pos));
    }

    /// <summary>
    /// Reads a MeshVector3 (position/normal) from a vertex.
    /// </summary>
    public MeshVector3 ReadVector3(int vertexIndex, int stride, int elementOffset)
    {
        int pos = (vertexIndex * stride) + elementOffset;
        float x = BinaryPrimitives.ReadSingleLittleEndian(Buffer.AsSpan(pos));
        float y = BinaryPrimitives.ReadSingleLittleEndian(Buffer.AsSpan(pos + 4));
        float z = BinaryPrimitives.ReadSingleLittleEndian(Buffer.AsSpan(pos + 8));
        return new MeshVector3(x, y, z);
    }
}

/// <summary>
/// VBUF2 (Shadow Vertex Buffer) variant - inherits all parsing from VBUF.
/// Resource Type: 0x0229684B
/// </summary>
public sealed class Vbuf2Block : VbufBlock
{
    /// <summary>Resource type identifier for VBUF2.</summary>
    public new const uint TypeId = 0x0229684B;

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    public Vbuf2Block() : base() { }
    public Vbuf2Block(ReadOnlySpan<byte> data) : base(data) { }
}
