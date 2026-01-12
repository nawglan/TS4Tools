namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// GEOM (Geometry) block - contains full vertex and face data for a mesh.
/// Supports both version 0x05 and 0x0C formats.
/// Resource Type: 0x015A1849
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs
/// </summary>
public sealed class GeomBlock : RcolBlock
{
    /// <summary>Resource type identifier for GEOM.</summary>
    public const uint TypeId = 0x015A1849;

    /// <summary>GEOM version 5 (older format with float weights).</summary>
    public const uint Version5 = 0x00000005;

    /// <summary>GEOM version 12 (newer format with byte weights).</summary>
    public const uint Version12 = 0x0000000C;

    /// <inheritdoc/>
    public override string Tag => "GEOM";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (0x00000005 or 0x0000000C).</summary>
    public uint Version { get; private set; } = Version12;

    /// <summary>Shader type. 0 means no shader/MTNF data.</summary>
    public uint Shader { get; private set; }

    /// <summary>Raw MTNF (material) data when Shader != 0.</summary>
    public byte[]? MtnfData { get; private set; }

    /// <summary>Merge group identifier.</summary>
    public uint MergeGroup { get; private set; }

    /// <summary>Sort order value.</summary>
    public uint SortOrder { get; private set; }

    /// <summary>List of vertex formats defining the structure of each vertex.</summary>
    public GeomVertexFormatList? VertexFormats { get; private set; }

    /// <summary>List of vertices with their element data.</summary>
    public GeomVertexList? Vertices { get; private set; }

    /// <summary>List of triangle faces.</summary>
    public GeomFaceList? Faces { get; private set; }

    /// <summary>Skin index (version 0x05 only).</summary>
    public int SkinIndex { get; private set; }

    /// <summary>Unknown data list 1 (version 0x0C only).</summary>
    public GeomUnknownThingList? UnknownThings { get; private set; }

    /// <summary>Unknown data list 2 (version 0x0C only).</summary>
    public GeomUnknownThing2List? UnknownThings2 { get; private set; }

    /// <summary>List of bone hashes.</summary>
    public GeomBoneHashList? BoneHashes { get; private set; }

    /// <summary>TGI block list for external resource references.</summary>
    public List<RcolTgiBlock> TgiBlocks { get; } = [];

    /// <summary>Number of vertices in this geometry.</summary>
    public int VertexCount => Vertices?.Count ?? 0;

    /// <summary>Number of faces in this geometry.</summary>
    public int FaceCount => Faces?.Count ?? 0;

    /// <summary>
    /// Creates an empty GEOM block.
    /// </summary>
    public GeomBlock() : base()
    {
    }

    /// <summary>
    /// Creates a GEOM block from raw data.
    /// </summary>
    public GeomBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the full GEOM block including vertices and faces.
    /// Source: GEOM.cs Parse method lines 122-177
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid GEOM tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        if (Version != Version5 && Version != Version12)
            throw new InvalidDataException(
                $"Invalid GEOM version: expected 0x{Version5:X8} or 0x{Version12:X8}, got 0x{Version:X8}");

        // Read TGI offset and size
        // Source: GEOM.cs line 132: long tgiPosn = r.ReadUInt32() + s.Position;
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        int tgiPositionBase = pos; // Position after reading offset
        uint tgiSize = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        int tgiAbsolutePos = (int)(tgiPositionBase + tgiOffset);

        // Read shader type
        // Source: GEOM.cs lines 135-145
        Shader = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        if (Shader != 0)
        {
            uint mtnfSize = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
            MtnfData = data.Slice(pos, (int)mtnfSize).ToArray();
            pos += (int)mtnfSize;
        }
        else
        {
            MtnfData = null;
        }

        // Read merge group and sort order
        // Source: GEOM.cs lines 147-148
        MergeGroup = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        SortOrder = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read vertex count
        // Source: GEOM.cs line 150
        int numVertices = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read vertex formats
        // Source: GEOM.cs line 151
        VertexFormats = GeomVertexFormatList.Read(data, ref pos, Version);

        // Read vertex data
        // Source: GEOM.cs line 152
        Vertices = GeomVertexList.Read(data, ref pos, numVertices, VertexFormats);

        // Read face point sizes validation
        // Source: GEOM.cs lines 154-160
        int numFacePointSizes = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        if (numFacePointSizes != 1)
            throw new InvalidDataException(
                $"Invalid GEOM numFacePointSizes: expected 1, got {numFacePointSizes}");

        byte facePointSize = data[pos++];
        if (facePointSize != 2)
            throw new InvalidDataException(
                $"Invalid GEOM facePointSize: expected 2, got {facePointSize}");

        // Read faces
        // Source: GEOM.cs line 162
        Faces = GeomFaceList.Read(data, ref pos);

        // Read version-specific data
        // Source: GEOM.cs lines 163-171
        if (Version == Version5)
        {
            SkinIndex = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;
            UnknownThings = null;
            UnknownThings2 = null;
        }
        else // Version12
        {
            SkinIndex = 0;
            UnknownThings = GeomUnknownThingList.Read(data, ref pos);
            UnknownThings2 = GeomUnknownThing2List.Read(data, ref pos);
        }

        // Read bone hashes
        // Source: GEOM.cs line 172
        BoneHashes = GeomBoneHashList.Read(data, ref pos);

        // Read TGI blocks
        // Source: GEOM.cs line 174
        ParseTgiBlocks(data, tgiAbsolutePos, tgiSize);
    }

    /// <summary>
    /// Parses the TGI block list from the specified position.
    /// </summary>
    private void ParseTgiBlocks(ReadOnlySpan<byte> data, int tgiPos, uint tgiSize)
    {
        TgiBlocks.Clear();

        if (tgiPos >= data.Length)
            return;

        int blockCount = (int)(tgiSize / RcolTgiBlock.Size);

        for (int i = 0; i < blockCount && tgiPos + RcolTgiBlock.Size <= data.Length; i++)
        {
            TgiBlocks.Add(RcolTgiBlock.Read(data[tgiPos..]));
            tgiPos += RcolTgiBlock.Size;
        }
    }

    /// <summary>
    /// Serializes the GEOM block back to bytes.
    /// Source: GEOM.cs UnParse method lines 179-236
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Write version
        writer.Write(Version);

        // Reserve space for TGI offset and size
        long tgiOffsetPos = ms.Position;
        writer.Write(0u); // TGI offset placeholder
        writer.Write(0u); // TGI size placeholder

        // Write shader type and MTNF data
        // Source: GEOM.cs lines 191-198
        writer.Write(Shader);
        if (Shader != 0 && MtnfData != null)
        {
            writer.Write(MtnfData.Length);
            writer.Write(MtnfData);
        }

        // Write merge group and sort order
        writer.Write(MergeGroup);
        writer.Write(SortOrder);

        // Write vertex count
        int vertexCount = Vertices?.Count ?? 0;
        writer.Write(vertexCount);

        // Write vertex formats
        // Source: GEOM.cs lines 205-206
        if (VertexFormats == null)
        {
            writer.Write(0); // count = 0
        }
        else
        {
            VertexFormats.Write(writer);
        }

        // Write vertex data
        // Source: GEOM.cs lines 207-208
        Vertices?.Write(writer);

        // Write face point size validation values
        // Source: GEOM.cs lines 209-210
        writer.Write(1); // numFacePointSizes
        writer.Write((byte)2); // facePointSize

        // Write faces
        // Source: GEOM.cs lines 211-212
        if (Faces == null)
        {
            writer.Write(0); // count = 0
        }
        else
        {
            Faces.Write(writer);
        }

        // Write version-specific data
        // Source: GEOM.cs lines 213-223
        if (Version == Version5)
        {
            writer.Write(SkinIndex);
        }
        else // Version12
        {
            if (UnknownThings == null)
            {
                writer.Write(0); // count = 0
            }
            else
            {
                UnknownThings.Write(writer);
            }

            if (UnknownThings2 == null)
            {
                writer.Write(0); // count = 0
            }
            else
            {
                UnknownThings2.Write(writer);
            }
        }

        // Write bone hashes
        // Source: GEOM.cs lines 224-225
        if (BoneHashes == null)
        {
            writer.Write(0); // count = 0
        }
        else
        {
            BoneHashes.Write(writer);
        }

        // Record TGI position
        long tgiStartPos = ms.Position;

        // Write TGI blocks
        // Source: GEOM.cs line 233
        var tgiBuffer = new byte[RcolTgiBlock.Size];
        foreach (var tgi in TgiBlocks)
        {
            tgi.Write(tgiBuffer);
            writer.Write(tgiBuffer);
        }

        // Patch TGI offset and size
        // Offset is relative to position after reading the offset value
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        uint tgiSize = (uint)(TgiBlocks.Count * RcolTgiBlock.Size);

        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(tgiSize);

        return ms.ToArray();
    }
}
