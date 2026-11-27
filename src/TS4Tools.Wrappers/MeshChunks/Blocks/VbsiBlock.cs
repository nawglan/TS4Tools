// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/VBSI.cs lines 28-338

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Swizzle command applied to a vertex word.
/// Source: VBSI.cs lines 30-35 (SwizzleCmd)
/// </summary>
public enum SwizzleCmd : uint
{
    /// <summary>No swizzle.</summary>
    None = 0x0,

    /// <summary>Swap the bytes of a 32-bit word.</summary>
    Swizzle32 = 0x1,

    /// <summary>Swap the bytes of two 16-bit halves.</summary>
    Swizzle16x2 = 0x2
}

/// <summary>
/// VBSI (Vertex Buffer Swizzle Info) block - per-mesh swizzle segments for vertex buffers.
/// Source: VBSI.cs lines 37-338
/// </summary>
/// <remarks>
/// Unlike other RCOL blocks, VBSI has no leading tag in its data and its resource type is
/// 0xFFFFFFFF (VBSI.cs lines 326-334). The data is an int32 segment count followed by segments;
/// each segment is vertexSize, vertexCount, byteOffset and then <c>vertexSize / 4</c> swizzle
/// commands (no explicit count, VBSI.cs lines 113, 233-240).
/// </remarks>
public sealed class VbsiBlock : RcolBlock
{
    /// <summary>Resource type identifier for VBSI. Source: VBSI.cs line 333.</summary>
    public const uint TypeId = 0xFFFFFFFF;

    /// <inheritdoc/>
    public override string Tag => "VBSI";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>The swizzle segments, normally one per mesh.</summary>
    public List<VbsiSegment> Segments { get; } = [];

    /// <summary>
    /// Creates an empty VBSI block.
    /// </summary>
    public VbsiBlock() : base()
    {
    }

    /// <summary>
    /// Creates a VBSI block from raw data.
    /// </summary>
    public VbsiBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the VBSI block.
    /// Source: VBSI.cs lines 314-317, 233-240
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        var r = new RcolSpanReader(data);
        Segments.Clear();
        int count = r.ReadCount(12);
        for (int i = 0; i < count; i++)
        {
            int vertexSize = r.ReadInt32();
            int vertexCount = r.ReadInt32();
            uint byteOffset = r.ReadUInt32();
            if (vertexSize < 0)
                throw new InvalidDataException($"Invalid VBSI vertex size: {vertexSize}");

            int swizzleCount = vertexSize / 4;
            r.CheckCount(swizzleCount, 4);
            var segment = new VbsiSegment(vertexSize, vertexCount, byteOffset);
            for (int j = 0; j < swizzleCount; j++)
                segment.Swizzles.Add((SwizzleCmd)r.ReadUInt32());
            Segments.Add(segment);
        }
    }

    /// <summary>
    /// Serializes the VBSI block.
    /// Source: VBSI.cs lines 318-324, 241-250
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Segments.Count);
        foreach (var s in Segments)
        {
            w.Write(s.VertexSize);
            w.Write(s.VertexCount);
            w.Write(s.ByteOffset);
            foreach (var cmd in s.Swizzles)
                w.Write((uint)cmd);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a VBSI block with one segment per mesh of an MLOD.
    /// Source: VBSI.cs lines 302-311 (FromMLOD)
    /// </summary>
    /// <param name="mlod">The MLOD block.</param>
    /// <param name="vertexFormatResolver">Resolves a mesh's VRTF (legacy used ChunkReference.GetBlock).</param>
    public static VbsiBlock FromMlod(MlodBlock mlod, Func<MlodMesh, VrtfBlock> vertexFormatResolver)
    {
        ArgumentNullException.ThrowIfNull(mlod);
        ArgumentNullException.ThrowIfNull(vertexFormatResolver);
        var vbsi = new VbsiBlock();
        foreach (var mesh in mlod.Meshes)
            vbsi.Segments.Add(VbsiSegment.FromMesh(mesh, vertexFormatResolver(mesh)));
        return vbsi;
    }
}

/// <summary>
/// One swizzle segment of a VBSI block.
/// Source: VBSI.cs lines 138-284 (SegmentInfo)
/// </summary>
public sealed class VbsiSegment
{
    /// <summary>Vertex size in bytes (the swizzle count on disk is VertexSize / 4).</summary>
    public int VertexSize { get; set; }

    /// <summary>Vertex count.</summary>
    public int VertexCount { get; set; }

    /// <summary>Byte offset of the segment in the vertex buffer.</summary>
    public uint ByteOffset { get; set; }

    /// <summary>Swizzle commands.</summary>
    public List<SwizzleCmd> Swizzles { get; } = [];

    /// <summary>Creates an empty segment.</summary>
    public VbsiSegment()
    {
    }

    /// <summary>Creates a segment with the given header values.</summary>
    public VbsiSegment(int vertexSize, int vertexCount, uint byteOffset)
    {
        VertexSize = vertexSize;
        VertexCount = vertexCount;
        ByteOffset = byteOffset;
    }

    /// <summary>
    /// Builds a segment for a mesh from its vertex format.
    /// Source: VBSI.cs lines 156-206 (SegmentInfo.FromMesh)
    /// </summary>
    public static VbsiSegment FromMesh(MlodMesh mesh, VrtfBlock vrtf)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(vrtf);

        var segment = new VbsiSegment(vrtf.Stride, mesh.VertexCount, mesh.StreamOffset);
        foreach (var layout in vrtf.Layouts)
        {
            switch (layout.Format)
            {
                case ElementFormat.Float1:
                case ElementFormat.UByte4:
                case ElementFormat.UByte4N:
                case ElementFormat.ColorUByte4:
                case ElementFormat.Dec3N:
                case ElementFormat.UDec3N:
                    segment.Swizzles.Add(SwizzleCmd.Swizzle32);
                    break;
                case ElementFormat.Float2:
                    AddRepeated(segment, SwizzleCmd.Swizzle32, 2);
                    break;
                case ElementFormat.Float3:
                    AddRepeated(segment, SwizzleCmd.Swizzle32, 3);
                    break;
                case ElementFormat.Float4:
                    AddRepeated(segment, SwizzleCmd.Swizzle32, 4);
                    break;
                case ElementFormat.Short2:
                case ElementFormat.Short2N:
                case ElementFormat.UShort2N:
                case ElementFormat.Float16x2:
                    segment.Swizzles.Add(SwizzleCmd.Swizzle16x2);
                    break;
                case ElementFormat.Short4:
                case ElementFormat.Short4N:
                case ElementFormat.UShort4N:
                case ElementFormat.Float16x4:
                    AddRepeated(segment, SwizzleCmd.Swizzle16x2, 2);
                    break;
            }
        }
        return segment;
    }

    private static void AddRepeated(VbsiSegment segment, SwizzleCmd cmd, int count)
    {
        for (int i = 0; i < count; i++)
            segment.Swizzles.Add(cmd);
    }
}
