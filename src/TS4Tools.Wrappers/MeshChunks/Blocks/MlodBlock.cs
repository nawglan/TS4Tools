// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/MLOD.cs lines 32-708

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// MLOD (Model LOD) block - contains mesh definitions with buffer references.
/// Resource Type: 0x01D10F34
/// Source: MLOD.cs lines 32-80 (block), 221-635 (Mesh), 83-219 (GeometryState), 636-663 (MeshList)
/// </summary>
/// <remarks>
/// Layout: tag, version, int32 mesh count, then per mesh a uint32 byte size followed by the mesh body.
/// Mesh fields <c>ParentName</c>/<c>MirrorPlane</c> exist for version &gt; 0x201 and <c>Unknown1</c>
/// for version &gt; 0x203 (MLOD.cs lines 349-357, 387-395).
/// </remarks>
public sealed class MlodBlock : RcolBlock
{
    /// <summary>Resource type identifier for MLOD.</summary>
    public const uint TypeId = 0x01D10F34;

    /// <summary>Default version. Source: MLOD.cs line 37.</summary>
    public const uint DefaultVersion = 0x00000202;

    /// <summary>First version that stores ParentName and MirrorPlane.</summary>
    public const uint ParentNameMinVersion = 0x00000202;

    /// <summary>First version that stores Unknown1.</summary>
    public const uint Unknown1MinVersion = 0x00000204;

    /// <inheritdoc/>
    public override string Tag => "MLOD";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 0x00000202).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>The meshes in this LOD.</summary>
    public List<MlodMesh> Meshes { get; } = [];

    /// <summary>Number of meshes in this LOD.</summary>
    public int MeshCount => Meshes.Count;

    /// <summary>
    /// Creates an empty MLOD block.
    /// </summary>
    public MlodBlock() : base()
    {
    }

    /// <summary>
    /// Creates an MLOD block from raw data.
    /// </summary>
    public MlodBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the MLOD block.
    /// Source: MLOD.cs lines 60-68
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MLOD tag: expected '{Tag}', got '{tag}'");

        var r = new RcolSpanReader(data, 4);
        Version = r.ReadUInt32();

        Meshes.Clear();
        int count = r.ReadCount(4);
        for (int i = 0; i < count; i++)
            Meshes.Add(MlodMesh.Parse(ref r, Version));
    }

    /// <summary>
    /// Serializes the MLOD block.
    /// Source: MLOD.cs lines 70-79
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'M');
        w.Write((byte)'L');
        w.Write((byte)'O');
        w.Write((byte)'D');
        w.Write(Version);
        w.Write(Meshes.Count);
        foreach (var mesh in Meshes)
            mesh.Write(w, ms, Version);
        return ms.ToArray();
    }
}

/// <summary>
/// A geometry state within a mesh.
/// Source: MLOD.cs lines 83-207 (GeometryState)
/// </summary>
public readonly record struct MlodGeometryState(
    uint Name, int StartIndex, int MinVertexIndex, int VertexCount, int PrimitiveCount)
{
    /// <summary>Serialized size in bytes (five 4-byte fields).</summary>
    public const int Size = 20;
}

/// <summary>
/// A mesh entry within an MLOD block.
/// Source: MLOD.cs lines 221-635 (Mesh)
/// </summary>
public sealed class MlodMesh
{
    /// <summary>Mesh name hash.</summary>
    public uint Name { get; set; }

    /// <summary>Reference to the material (MATD/MTST) chunk.</summary>
    public RcolChunkReference MaterialIndex { get; set; }

    /// <summary>Reference to the VRTF chunk.</summary>
    public RcolChunkReference VertexFormatIndex { get; set; }

    /// <summary>Reference to the VBUF chunk.</summary>
    public RcolChunkReference VertexBufferIndex { get; set; }

    /// <summary>Reference to the IBUF chunk.</summary>
    public RcolChunkReference IndexBufferIndex { get; set; }

    /// <summary>Primitive type (low byte of the packed word, MLOD.cs lines 335-337).</summary>
    public ModelPrimitiveType PrimitiveType { get; set; }

    /// <summary>Mesh flags (upper 24 bits of the packed word, MLOD.cs lines 335-337).</summary>
    public MeshOption Flags { get; set; }

    /// <summary>Stream offset into the vertex buffer.</summary>
    public uint StreamOffset { get; set; }

    /// <summary>First vertex.</summary>
    public int StartVertex { get; set; }

    /// <summary>First index.</summary>
    public int StartIndex { get; set; }

    /// <summary>Minimum vertex index.</summary>
    public int MinVertexIndex { get; set; }

    /// <summary>Vertex count.</summary>
    public int VertexCount { get; set; }

    /// <summary>Primitive count.</summary>
    public int PrimitiveCount { get; set; }

    /// <summary>Bounding box.</summary>
    public BoundingBox Bounds { get; set; }

    /// <summary>Reference to the SKIN chunk.</summary>
    public RcolChunkReference SkinControllerIndex { get; set; }

    /// <summary>Joint references.</summary>
    public List<uint> JointReferences { get; } = [];

    /// <summary>Reference to the scale/offset chunk.</summary>
    public RcolChunkReference ScaleOffsetIndex { get; set; }

    /// <summary>Geometry states.</summary>
    public List<MlodGeometryState> GeometryStates { get; } = [];

    /// <summary>Parent name (block version &gt; 0x201).</summary>
    public uint ParentName { get; set; }

    /// <summary>Mirror plane (block version &gt; 0x201).</summary>
    public MeshVector4 MirrorPlane { get; set; }

    /// <summary>Unknown value (block version &gt; 0x203).</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Whether this mesh casts shadows. Source: MLOD.cs line 519.</summary>
    public bool IsShadowCaster => (Flags & MeshOption.ShadowCaster) != 0;

    /// <summary>
    /// Parses a mesh. Source: MLOD.cs lines 324-362
    /// </summary>
    /// <summary>Newest MLOD version whose meshes match the newest legacy layout (validated on real game data).</summary>
    internal const uint MaxLegacyCompatibleVersion = 0x00000205;

    internal static MlodMesh Parse(ref RcolSpanReader r, uint version)
    {
        uint expectedSize = r.ReadUInt32();
        int start = r.Position;

        var m = new MlodMesh
        {
            Name = r.ReadUInt32(),
            MaterialIndex = new RcolChunkReference(r.ReadUInt32()),
            VertexFormatIndex = new RcolChunkReference(r.ReadUInt32()),
            VertexBufferIndex = new RcolChunkReference(r.ReadUInt32()),
            IndexBufferIndex = new RcolChunkReference(r.ReadUInt32()),
        };
        uint val = r.ReadUInt32();
        m.PrimitiveType = (ModelPrimitiveType)(val & 0x000000FF);
        m.Flags = (MeshOption)(val >> 8);
        m.StreamOffset = r.ReadUInt32();
        m.StartVertex = r.ReadInt32();
        m.StartIndex = r.ReadInt32();
        m.MinVertexIndex = r.ReadInt32();
        m.VertexCount = r.ReadInt32();
        m.PrimitiveCount = r.ReadInt32();
        m.Bounds = r.ReadBoundingBox();
        m.SkinControllerIndex = new RcolChunkReference(r.ReadUInt32());

        int jointCount = r.ReadCount(4);
        for (int i = 0; i < jointCount; i++)
            m.JointReferences.Add(r.ReadUInt32());

        m.ScaleOffsetIndex = new RcolChunkReference(r.ReadUInt32());

        int stateCount = r.ReadCount(MlodGeometryState.Size);
        for (int i = 0; i < stateCount; i++)
        {
            m.GeometryStates.Add(new MlodGeometryState(
                r.ReadUInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32()));
        }

        if (version > 0x00000201)
        {
            m.ParentName = r.ReadUInt32();
            m.MirrorPlane = r.ReadVector4();
        }

        if (version > 0x00000203)
            m.Unknown1 = r.ReadUInt32();

        // Source: MLOD.cs lines 357-360 - legacy throws when the mesh does not end where its size says.
        // Its newest layout branch is "> 0x203" (lines 353-356); real files up to 0x205 fit it, while 0x206
        // meshes carry extra data no legacy layout describes, so they are refused as unsupported.
        long actualSize = r.Position - start;
        if (actualSize != expectedSize)
        {
            if (version > MaxLegacyCompatibleVersion)
                throw new UnsupportedResourceVersionException("MLOD", version, MaxLegacyCompatibleVersion);
            throw new InvalidDataException($"MLOD mesh size mismatch: expected {expectedSize}, actual {actualSize}");
        }

        return m;
    }

    /// <summary>
    /// Writes a mesh. Source: MLOD.cs lines 364-401
    /// </summary>
    internal void Write(BinaryWriter w, MemoryStream ms, uint version)
    {
        long sizeOffset = ms.Position;
        w.Write(0u);
        long start = ms.Position;

        w.Write(Name);
        w.Write(MaterialIndex.RawValue);
        w.Write(VertexFormatIndex.RawValue);
        w.Write(VertexBufferIndex.RawValue);
        w.Write(IndexBufferIndex.RawValue);
        w.Write((uint)PrimitiveType | ((uint)Flags << 8));
        w.Write(StreamOffset);
        w.Write(StartVertex);
        w.Write(StartIndex);
        w.Write(MinVertexIndex);
        w.Write(VertexCount);
        w.Write(PrimitiveCount);
        w.Write(Bounds);
        w.Write(SkinControllerIndex.RawValue);
        w.Write(JointReferences.Count);
        foreach (uint joint in JointReferences)
            w.Write(joint);
        w.Write(ScaleOffsetIndex.RawValue);
        w.Write(GeometryStates.Count);
        foreach (var gs in GeometryStates)
        {
            w.Write(gs.Name);
            w.Write(gs.StartIndex);
            w.Write(gs.MinVertexIndex);
            w.Write(gs.VertexCount);
            w.Write(gs.PrimitiveCount);
        }

        if (version > 0x00000201)
        {
            w.Write(ParentName);
            w.Write(MirrorPlane);
        }

        if (version > 0x00000203)
            w.Write(Unknown1);

        long end = ms.Position;
        ms.Position = sizeOffset;
        w.Write((uint)(end - start));
        ms.Position = end;
    }
}
