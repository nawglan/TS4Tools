
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
    public uint Version { get; set; } = 0x00000101;

    /// <summary>Format flags indicating compression mode.</summary>
    public VbufFormat Flags { get; set; }

    /// <summary>Raw reference value to the swizzle info chunk (for compressed data).</summary>
    public uint SwizzleInfoIndex { get; set; }

    /// <summary>Reference to the VBSI swizzle info chunk. Source: VBUF.cs lines 88, 107.</summary>
    public RcolChunkReference SwizzleInfo
    {
        get => new(SwizzleInfoIndex);
        set => SwizzleInfoIndex = value.RawValue;
    }

    /// <summary>Raw vertex buffer data.</summary>
    public byte[] Buffer { get; set; } = [];

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
        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid VBUF tag: expected '{Tag}', got '{tag}'");

        var r = new RcolSpanReader(data, 4);
        Version = r.ReadUInt32();
        Flags = (VbufFormat)r.ReadUInt32();
        SwizzleInfoIndex = r.ReadUInt32();

        // Read remaining data as vertex buffer (VBUF.cs lines 131-132)
        Buffer = r.ReadBytes(r.Remaining);
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

    // ------------------------------------------------------------------
    // Vertex decode / encode helpers working with VRTF and MLOD.
    // Source: VBUF.cs lines 69-76 (colour map), 160-638
    // ------------------------------------------------------------------

    /// <summary>ColorUByte4 blend weight channel order. Source: VBUF.cs lines 69-76.</summary>
    private static readonly int[] ColorUByte4Map = [2, 1, 0, 3];

    /// <summary>Default UV scales used when writing. Source: VBUF.cs line 524.</summary>
    private static readonly float[] DefaultUvScales = [1f / 32767f, 1f / 32767f, 1f / 32767f];

    /// <summary>
    /// Computes the bounding box of a mesh and its geometry states.
    /// Source: VBUF.cs lines 160-193 (GetBoundingBox)
    /// </summary>
    public BoundingBox GetBoundingBox(MlodMesh mesh, VrtfBlock vrtf)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(vrtf);

        bool have = false;
        float minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;

        void Accumulate(VbufVertex[] vertices)
        {
            foreach (var v in vertices)
            {
                var p = v.Position;
                if (p is null || p.Length != 3)
                    continue; // Source: VBUF.cs lines 174-178
                if (!have)
                {
                    minX = maxX = p[0];
                    minY = maxY = p[1];
                    minZ = maxZ = p[2];
                    have = true;
                    continue;
                }
                if (minX > p[0]) minX = p[0];
                if (minY > p[1]) minY = p[1];
                if (minZ > p[2]) minZ = p[2];
                if (maxX < p[0]) maxX = p[0];
                if (maxY < p[1]) maxY = p[1];
                if (maxZ < p[2]) maxZ = p[2];
            }
        }

        Accumulate(GetVertices(mesh, vrtf, null));
        foreach (var geo in mesh.GeometryStates)
            Accumulate(GetVertices(mesh, vrtf, geo, null));

        return have ? new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ) : BoundingBox.Empty;
    }

    /// <summary>
    /// Decodes the vertices of a mesh.
    /// Source: VBUF.cs lines 195-198
    /// </summary>
    public VbufVertex[] GetVertices(MlodMesh mesh, VrtfBlock vrtf, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return GetVertices(vrtf, mesh.StreamOffset, mesh.VertexCount, uvScales);
    }

    /// <summary>
    /// Decodes the vertices of one geometry state of a mesh.
    /// Source: VBUF.cs lines 199-202
    /// </summary>
    public VbufVertex[] GetVertices(MlodMesh mesh, VrtfBlock vrtf, MlodGeometryState geo, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(vrtf);
        return GetVertices(vrtf, mesh.StreamOffset + ((long)geo.MinVertexIndex * vrtf.Stride), geo.VertexCount, uvScales);
    }

    /// <summary>
    /// Decodes <paramref name="count"/> vertices starting at byte <paramref name="offset"/>.
    /// Source: VBUF.cs lines 204-281
    /// </summary>
    public VbufVertex[] GetVertices(VrtfBlock vrtf, long offset, int count, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(vrtf);
        int stride = vrtf.Stride;
        ValidateVertexRange(vrtf, offset, count);

        var position = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Position);
        var normal = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Normal);
        var uv = vrtf.Layouts.Where(x => x.Usage == ElementUsage.UV).ToArray();
        var blendIndices = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.BlendIndex);
        var blendWeights = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.BlendWeight);
        var tangents = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Tangent);
        var color = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Colour);
        bool hasPosition = vrtf.Layouts.Any(x => x.Usage == ElementUsage.Position);
        bool hasNormal = vrtf.Layouts.Any(x => x.Usage == ElementUsage.Normal);
        bool hasBlendIndices = vrtf.Layouts.Any(x => x.Usage == ElementUsage.BlendIndex);
        bool hasBlendWeights = vrtf.Layouts.Any(x => x.Usage == ElementUsage.BlendWeight);
        bool hasTangents = vrtf.Layouts.Any(x => x.Usage == ElementUsage.Tangent);
        bool hasColor = vrtf.Layouts.Any(x => x.Usage == ElementUsage.Colour);

        if (uvScales is null || uvScales.Length == 0)
            uvScales = new float[3];

        var verts = new VbufVertex[count];
        for (int i = 0; i < count; i++)
        {
            var v = new VbufVertex();
            var data = Buffer.AsSpan((int)(offset + ((long)i * stride)), stride);

            if (hasPosition)
                v.Position = ReadFloatData(data, position);
            if (hasNormal)
                v.Normal = ReadFloatData(data, normal);

            v.UV = new float[uv.Length][];
            for (int j = 0; j < uv.Length; j++)
            {
                float scale = j < uvScales.Length && uvScales[j] != 0 ? uvScales[j] : uvScales[0];
                v.UV[j] = ReadUVData(data, uv[j], scale);
            }

            if (hasBlendIndices)
                v.BlendIndices = data.Slice(blendIndices.Offset, blendIndices.Format.ByteSize()).ToArray();
            if (hasBlendWeights)
                v.BlendWeights = ReadFloatData(data, blendWeights);
            if (hasTangents)
                v.Tangents = ReadFloatData(data, tangents);
            if (hasColor)
                v.Color = ReadFloatData(data, color);

            verts[i] = v;
        }
        return verts;
    }

    private void ValidateVertexRange(VrtfBlock vrtf, long offset, int count)
    {
        if (vrtf.Stride <= 0 && count > 0)
            throw new InvalidDataException($"Invalid VRTF stride: {vrtf.Stride}");
        if (count < 0)
            throw new InvalidDataException($"Invalid vertex count: {count}");
        if (offset < 0 || offset + ((long)count * vrtf.Stride) > Buffer.Length)
            throw new InvalidDataException(
                $"Vertex range (offset {offset}, count {count}, stride {vrtf.Stride}) lies outside the {Buffer.Length} byte buffer");
        foreach (var layout in vrtf.Layouts)
        {
            if (layout.Offset + layout.Format.ByteSize() > vrtf.Stride)
                throw new InvalidDataException(
                    $"VRTF element {layout.Usage} at offset {layout.Offset} exceeds stride {vrtf.Stride}");
        }
    }

    /// <summary>
    /// Decodes a UV element.
    /// Source: VBUF.cs lines 282-308 (ReadUVData)
    /// </summary>
    public static float[] ReadUVData(ReadOnlySpan<byte> vertexData, ElementLayout layout, float scale)
    {
        var output = new float[layout.Format.FloatCount()];
        var element = vertexData.Slice(layout.Offset, layout.Format.ByteSize());
        switch (layout.Format)
        {
            case ElementFormat.Short2:
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) * scale;
                break;
            case ElementFormat.Short4:
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) / (float)short.MaxValue;
                break;
            case ElementFormat.Short4DropShadow:
                for (int i = 0; i < output.Length - 1; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) / (float)short.MaxValue;
                output[^1] += BinaryPrimitives.ReadInt16LittleEndian(element[((output.Length - 1) * 2)..]) / 511f;
                break;
            default:
                return ReadFloatData(vertexData, layout);
        }
        return output;
    }

    /// <summary>
    /// Decodes a float element. Formats the legacy code does not decode (UByte4N, Short2N, Short4N,
    /// UShort2N, Dec3N, UDec3N, Float16) yield zeros, exactly as legacy does.
    /// Source: VBUF.cs lines 310-398 (ReadFloatData)
    /// </summary>
    public static float[] ReadFloatData(ReadOnlySpan<byte> vertexData, ElementLayout layout)
    {
        var output = new float[layout.Format.FloatCount()];
        var element = vertexData.Slice(layout.Offset, layout.Format.ByteSize());
        float scalar;
        switch (layout.Format)
        {
            case ElementFormat.Float1:
            case ElementFormat.Float2:
            case ElementFormat.Float3:
            case ElementFormat.Float4:
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadSingleLittleEndian(element[(i * 4)..]);
                break;
            case ElementFormat.ColorUByte4:
                switch (layout.Usage)
                {
                    case ElementUsage.Colour:
                        for (int i = 0; i < output.Length; i++)
                            output[i] += element[i] / (float)byte.MaxValue;
                        break;
                    case ElementUsage.BlendWeight:
                        for (int i = 0; i < output.Length; i++)
                            output[i] += element[ColorUByte4Map[i]] / (float)byte.MaxValue;
                        break;
                    case ElementUsage.Normal:
                    case ElementUsage.Tangent:
                        for (int i = 0; i < output.Length - 1; i++)
                            output[i] += element[2 - i] == 0 ? -1 : (((element[2 - i] + 1) / 128f) - 1);
                        switch (element[3])
                        {
                            case 0: output[^1] = -1f; break;   // -1 determinant
                            case 127: output[^1] = 0f; break;  // no determinant
                            case 255: output[^1] = 1f; break;  // +1 determinant
                        }
                        break;
                }
                break;
            case ElementFormat.Short2:
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) / (float)short.MaxValue;
                break;
            case ElementFormat.Short4:
                scalar = BinaryPrimitives.ReadUInt16LittleEndian(element[6..]);
                if (scalar == 0) scalar = short.MaxValue;
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) / scalar;
                break;
            case ElementFormat.UShort4N:
                scalar = BinaryPrimitives.ReadUInt16LittleEndian(element[6..]);
                if (scalar == 0) scalar = 511;
                for (int i = 0; i < output.Length; i++)
                    output[i] += BinaryPrimitives.ReadInt16LittleEndian(element[(i * 2)..]) / scalar;
                break;
        }
        return output;
    }

    /// <summary>
    /// Replaces the vertices of mesh <paramref name="meshIndex"/>, resizing the buffer and shifting
    /// later meshes that share this buffer.
    /// Source: VBUF.cs lines 400-403
    /// </summary>
    public bool SetVertices(MlodBlock mlod, int meshIndex, VrtfBlock vrtf, VbufVertex[] vertices, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mlod);
        if (meshIndex < 0 || meshIndex >= mlod.Meshes.Count)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));
        return SetVertices(mlod, mlod.Meshes[meshIndex], vrtf, vertices, uvScales);
    }

    /// <summary>
    /// Replaces the vertices of a mesh and updates its vertex count.
    /// Source: VBUF.cs lines 404-409
    /// </summary>
    public bool SetVertices(MlodBlock mlod, MlodMesh mesh, VrtfBlock vrtf, VbufVertex[] vertices, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        bool okay = SetVertices(mlod, mesh.VertexBufferIndex, mesh.StreamOffset, mesh.VertexCount, vrtf, vertices, uvScales);
        mesh.VertexCount = vertices.Length;
        return okay;
    }

    /// <summary>
    /// Replaces the vertices of a geometry state (by index) and updates its vertex count.
    /// Source: VBUF.cs lines 410-413
    /// </summary>
    public bool SetVertices(MlodBlock mlod, MlodMesh mesh, int geoIndex, VrtfBlock vrtf, VbufVertex[] vertices, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (geoIndex < 0 || geoIndex >= mesh.GeometryStates.Count)
            throw new ArgumentOutOfRangeException(nameof(geoIndex));
        ArgumentNullException.ThrowIfNull(vrtf);

        var geo = mesh.GeometryStates[geoIndex];
        long beforeLength = mesh.StreamOffset + ((long)geo.MinVertexIndex * vrtf.Stride);
        bool okay = SetVertices(mlod, mesh.VertexBufferIndex, beforeLength, geo.VertexCount, vrtf, vertices, uvScales);
        mesh.GeometryStates[geoIndex] = mesh.GeometryStates[geoIndex] with { VertexCount = vertices.Length };
        return okay;
    }

    /// <summary>
    /// Source: VBUF.cs lines 419-447
    /// </summary>
    private bool SetVertices(MlodBlock mlod, RcolChunkReference myVbi, long beforeLength, int count,
        VrtfBlock vrtf, VbufVertex[] vertices, float[]? uvScales)
    {
        ArgumentNullException.ThrowIfNull(mlod);
        ArgumentNullException.ThrowIfNull(vrtf);
        ArgumentNullException.ThrowIfNull(vertices);
        if (vrtf.Stride <= 0)
            throw new InvalidDataException($"Invalid VRTF stride: {vrtf.Stride}");
        if (beforeLength < 0 || beforeLength > Buffer.Length || count < 0)
            throw new InvalidDataException(
                $"Vertex range (offset {beforeLength}, count {count}) lies outside the {Buffer.Length} byte buffer");

        foreach (var layout in vrtf.Layouts)
        {
            if (layout.Offset + layout.Format.ByteSize() > vrtf.Stride)
                throw new InvalidDataException(
                    $"VRTF element {layout.Usage} at offset {layout.Offset} exceeds stride {vrtf.Stride}");
        }

        long afterPos = Math.Min(Buffer.Length, beforeLength + ((long)count * vrtf.Stride));

        using var mg = new MemoryStream();
        bool okay = EncodeVertices(mg, vrtf, vertices, uvScales);
        long offset = beforeLength + mg.Length - afterPos;

        var newBuffer = new byte[beforeLength + mg.Length + (Buffer.Length - afterPos)];
        Array.Copy(Buffer, 0, newBuffer, 0, beforeLength);
        mg.ToArray().CopyTo(newBuffer, beforeLength);
        Array.Copy(Buffer, afterPos, newBuffer, beforeLength + mg.Length, Buffer.Length - afterPos);
        Buffer = newBuffer;

        // Source: VBUF.cs lines 440-446 - shift later meshes sharing this buffer
        int voffset = (int)offset / vrtf.Stride;
        if (offset != 0)
        {
            foreach (var m in mlod.Meshes.Where(m => m.VertexBufferIndex.Equals(myVbi) && m.StreamOffset > beforeLength))
            {
                m.StreamOffset = unchecked((uint)(m.StreamOffset + offset));
                for (int g = 0; g < m.GeometryStates.Count; g++)
                {
                    var gs = m.GeometryStates[g];
                    if (gs.MinVertexIndex * vrtf.Stride > beforeLength)
                        m.GeometryStates[g] = gs with { MinVertexIndex = gs.MinVertexIndex + voffset };
                }
            }
        }
        return okay;
    }

    /// <summary>
    /// Encodes vertices into a stream of vrtf.Stride-sized records.
    /// Source: VBUF.cs lines 524-565 (SetVertices(MemoryStream...)). The legacy static position
    /// min/max scratch values it computes are never used by the writer and are not ported.
    /// </summary>
    private static bool EncodeVertices(MemoryStream s, VrtfBlock vrtf, IEnumerable<VbufVertex> vertices, float[]? uvScales)
    {
        bool okay = true;
        byte[] output = new byte[vrtf.Stride];
        var position = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Position);
        var normal = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Normal);
        var uv = vrtf.Layouts.Where(x => x.Usage == ElementUsage.UV).ToArray();
        var blendIndices = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.BlendIndex);
        var blendWeights = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.BlendWeight);
        var tangents = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Tangent);
        var color = vrtf.Layouts.FirstOrDefault(x => x.Usage == ElementUsage.Colour);

        if (uvScales is null || uvScales.Length == 0)
            uvScales = DefaultUvScales;

        foreach (var v in vertices)
        {
            if (v.Position is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.Position))
                WritePositionData(v.Position, position, output);
            if (v.Normal is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.Normal))
                WriteFloatData(v.Normal, normal, output);
            for (int u = 0; u < uv.Length; u++)
            {
                float scale = u < uvScales.Length && uvScales[u] != 0 ? uvScales[u] : uvScales[0];
                if (v.UV is not null && u < v.UV.Length && v.UV[u] is not null)
                {
                    if (!WriteUVData(v.UV[u], uv[u], output, scale))
                        okay = false;
                }
            }
            if (v.BlendIndices is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.BlendIndex))
            {
                int n = Math.Min(v.BlendIndices.Length, blendIndices.Format.ByteSize());
                Array.Copy(v.BlendIndices, 0, output, blendIndices.Offset, n);
            }
            if (v.BlendWeights is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.BlendWeight))
                WriteFloatData(v.BlendWeights, blendWeights, output);
            if (v.Tangents is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.Tangent))
                WriteFloatData(v.Tangents, tangents, output);
            if (v.Color is not null && vrtf.Layouts.Any(x => x.Usage == ElementUsage.Colour))
                WriteFloatData(v.Color, color, output);
            s.Write(output, 0, output.Length);
        }
        return okay;
    }

    private static void PutInt16(byte[] output, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(offset), value);

    /// <summary>Source: VBUF.cs lines 567-585 (WritePositionData)</summary>
    private static void WritePositionData(float[] input, ElementLayout layout, byte[] output)
    {
        switch (layout.Format)
        {
            case ElementFormat.UShort4N:
                // Fixed scalar of 512 (VBUF.cs line 577: "changed to fixed 512 as LoveseatDanishModern had problems")
                const ulong scalar = 512;
                for (int i = 0; i < input.Length; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * scalar)));
                PutInt16(output, layout.Offset + 6, 0);
                break;
            default:
                WriteFloatData(input, layout, output);
                break;
        }
    }

    /// <summary>Source: VBUF.cs lines 587-613 (WriteUVData)</summary>
    private static bool WriteUVData(float[] input, ElementLayout layout, byte[] output, float scale)
    {
        bool okay = true;
        switch (layout.Format)
        {
            case ElementFormat.Short2:
                for (int i = 0; i < input.Length; i++)
                {
                    double r = Math.Round(input[i] / scale);
                    if (unchecked((short)r) != (long)r) okay = false;
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)r));
                }
                break;
            case ElementFormat.Short4:
                for (int i = 0; i < input.Length; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * short.MaxValue)));
                break;
            case ElementFormat.Short4DropShadow:
                for (int i = 0; i < input.Length - 1; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * short.MaxValue)));
                if (input.Length > 0)
                    PutInt16(output, layout.Offset + ((input.Length - 1) * 2), unchecked((short)Math.Round(input[^1] * 511)));
                break;
            default:
                WriteFloatData(input, layout, output);
                break;
        }
        return okay;
    }

    /// <summary>Source: VBUF.cs lines 615-683 (WriteFloatData)</summary>
    private static void WriteFloatData(float[] input, ElementLayout layout, byte[] output)
    {
        ulong scalar;
        double max;
        switch (layout.Format)
        {
            case ElementFormat.Float1:
            case ElementFormat.Float2:
            case ElementFormat.Float3:
            case ElementFormat.Float4:
                for (int i = 0; i < input.Length; i++)
                    BinaryPrimitives.WriteSingleLittleEndian(output.AsSpan(layout.Offset + (i * 4)), input[i]);
                break;
            case ElementFormat.ColorUByte4:
                switch (layout.Usage)
                {
                    case ElementUsage.Colour:
                        for (int i = 0; i < input.Length; i++)
                            output[layout.Offset + i] = unchecked((byte)Math.Round(input[i] * byte.MaxValue));
                        break;
                    case ElementUsage.BlendWeight:
                        for (int i = 0; i < input.Length; i++)
                            output[layout.Offset + ColorUByte4Map[i]] = unchecked((byte)Math.Round(input[i] * byte.MaxValue));
                        break;
                    case ElementUsage.Normal:
                    case ElementUsage.Tangent:
                        // -0.98828125 == (((0.5 + 1) / 128f) - 1)
                        for (int i = 0; i < input.Length - 1; i++)
                            output[layout.Offset + 2 - i] = unchecked((byte)(input[i] < -0.98828125 ? 0 : (Math.Round((input[i] + 1) * 128) - 1)));
                        if (input.Length > 0)
                        {
                            float h = input[^1];
                            if (h == -1) output[layout.Offset + 3] = 0;
                            else if (h == 0) output[layout.Offset + 3] = 127;
                            else if (h == 1) output[layout.Offset + 3] = 255;
                        }
                        break;
                }
                break;
            case ElementFormat.Short2:
                for (int i = 0; i < input.Length; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * short.MaxValue)));
                break;
            case ElementFormat.Short4:
                if (input.Length == 0) break;
                max = Math.Ceiling(input.Max(x => Math.Abs(x)));
                scalar = (ulong)(max < 1 ? short.MaxValue : short.MaxValue / max);
                for (int i = 0; i < input.Length; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * scalar)));
                PutInt16(output, layout.Offset + 6, unchecked((short)scalar));
                break;
            case ElementFormat.UShort4N:
                if (input.Length == 0) break;
                max = Math.Ceiling(input.Max(x => Math.Abs(x)));
                scalar = (ulong)(max < 1.0 ? short.MaxValue : 512);
                for (int i = 0; i < input.Length; i++)
                    PutInt16(output, layout.Offset + (i * 2), unchecked((short)Math.Round(input[i] * scalar)));
                PutInt16(output, layout.Offset + 6, unchecked((short)(scalar == 512 ? 0 : scalar)));
                break;
        }
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

    /// <summary>
    /// Creates an empty VBUF2 block.
    /// </summary>
    public Vbuf2Block() : base() { }

    /// <summary>
    /// Creates a VBUF2 block from raw data.
    /// </summary>
    public Vbuf2Block(ReadOnlySpan<byte> data) : base(data) { }
}
