// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/MTNF.cs

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// MTNF (Material) data block containing shader parameters.
/// This is the inner data block within MATD.
/// Source: MTNF.cs lines 27-166
/// </summary>
public sealed class MtnfData
{
    private const uint MtnfTag = 0x464E544D; // "MTNF" in little-endian
    private const uint MtrlTag = 0x4C52544D; // "MTRL" in little-endian

    /// <summary>The tag read from data ("MTNF" or "MTRL").</summary>
    public string Tag { get; private set; } = "MTNF";

    /// <summary>Unknown field (typically 0).</summary>
    public uint Unknown1 { get; set; }

    /// <summary>List of shader data elements.</summary>
    public List<ShaderDataElement> ShaderData { get; set; } = [];

    /// <summary>MTRL only: first 16-bit unknown after <see cref="Unknown1"/> (MATD.cs line 213).</summary>
    public ushort Unknown2 { get; set; }

    /// <summary>MTRL only: second 16-bit unknown (MATD.cs line 214).</summary>
    public ushort Unknown3 { get; set; }

    /// <summary>Whether structured parsing succeeded (always true for an instance returned by <see cref="Parse"/>).</summary>
    public bool IsStructuredParsed { get; private set; } = true;

    /// <summary>Creates empty MTNF data.</summary>
    public MtnfData() { }

    /// <summary>Creates MTNF data from raw bytes (backwards-compatible constructor).</summary>
    public MtnfData(ReadOnlySpan<byte> data) : this(data, false)
    {
    }

    /// <summary>
    /// Creates MTNF data from raw bytes; <paramref name="geomIndexedTextures"/> selects the GEOM layout in
    /// which texture references are TGI-list indices (ShaderData.cs lines 600-728, RCOLTag "GEOM").
    /// </summary>
    public MtnfData(ReadOnlySpan<byte> data, bool geomIndexedTextures)
    {
        ParseInternal(data, geomIndexedTextures);
    }

    /// <summary>Parses MTNF data from raw bytes.</summary>
    public static MtnfData Parse(ReadOnlySpan<byte> data, bool geomIndexedTextures = false)
    {
        var mtnf = new MtnfData();
        mtnf.ParseInternal(data, geomIndexedTextures);
        return mtnf;
    }

    /// <summary>Tries to parse MTNF data from raw bytes.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out MtnfData? result, bool geomIndexedTextures = false)
    {
        try
        {
            result = Parse(data, geomIndexedTextures);
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or ArgumentException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>Checks if data starts with a valid MTNF or MTRL tag.</summary>
    public static bool IsValidTag(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return false;
        uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data);
        return tag == MtnfTag || tag == MtrlTag;
    }

    /// <summary>
    /// Parses the block.
    /// Source: MTNF.cs lines 63-72 (MTNF: tag, unknown1, data length, shader data list) and
    /// MATD.cs lines 205-216 (MTRL: tag, unknown1, two uint16 unknowns, shader data list - no data length).
    /// The list is ShaderData.cs lines 944-956: an int32 count, count 16-byte headers
    /// (field, data type, count, offset), then the element data; each offset is relative to the start of
    /// the MTNF/MTRL tag (ShaderData.cs lines 262-270).
    /// </summary>
    private void ParseInternal(ReadOnlySpan<byte> data, bool geomIndexedTextures)
    {
        if (data.Length < 12)
            throw new InvalidDataException($"MTNF data too short: {data.Length} bytes");

        int offset = 0;

        uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (tag == MtnfTag)
            Tag = "MTNF";
        else if (tag == MtrlTag)
            Tag = "MTRL";
        else
            throw new InvalidDataException($"Invalid MTNF tag: expected MTNF or MTRL, got 0x{tag:X8}");
        offset += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        int? expectedDataLength = null;
        if (Tag == "MTNF")
        {
            expectedDataLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;
        }
        else
        {
            Unknown2 = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            Unknown3 = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]);
            offset += 4;
        }

        if (offset + 4 > data.Length)
            throw new InvalidDataException("MTNF data too short for shader data count");
        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        if (entryCount < 0 || entryCount > (data.Length - offset) / 16)
            throw new InvalidDataException($"Invalid MTNF shader data count: {entryCount}");

        // Source: ShaderData.cs lines 947-955 - data length is the sum of the element sizes
        int dataLength = 0;
        ShaderData.Clear();
        for (int i = 0; i < entryCount; i++)
        {
            var element = ShaderDataElement.Parse(data, ref offset, 0, geomIndexedTextures);
            ShaderData.Add(element);
            dataLength += element.Count * 4;
        }

        if (expectedDataLength is not null && expectedDataLength != dataLength)
            throw new InvalidDataException(
                $"Expected 0x{expectedDataLength:X8} bytes of MTNF data, read 0x{dataLength:X8}");

        IsStructuredParsed = true;
    }

    /// <summary>
    /// Serializes the block.
    /// Source: MTNF.cs lines 74-88 and MATD.cs lines 218-228 (header), ShaderData.cs lines 958-964 and
    /// 315-331 (count, headers, then data in list order; each header's offset is patched to the data
    /// position relative to the tag).
    /// </summary>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Tag == "MTRL" ? MtrlTag : MtnfTag);
        writer.Write(Unknown1);

        long dataLengthPos = -1;
        if (Tag == "MTRL")
        {
            writer.Write(Unknown2);
            writer.Write(Unknown3);
        }
        else
        {
            dataLengthPos = ms.Position;
            writer.Write(0u); // data length, patched below
        }

        writer.Write(ShaderData.Count);
        var offsetPositions = new long[ShaderData.Count];
        for (int i = 0; i < ShaderData.Count; i++)
        {
            offsetPositions[i] = ms.Position + 12;
            ShaderData[i].WriteHeader(writer, 0);
        }

        long dataPos = ms.Position;
        for (int i = 0; i < ShaderData.Count; i++)
        {
            long pos = ms.Position;
            ms.Position = offsetPositions[i];
            writer.Write((uint)pos);
            ms.Position = pos;
            ShaderData[i].WriteData(writer);
        }

        if (dataLengthPos >= 0)
        {
            long end = ms.Position;
            ms.Position = dataLengthPos;
            writer.Write((uint)(end - dataPos));
            ms.Position = end;
        }

        return ms.ToArray();
    }

    /// <summary>Gets the number of shader parameters.</summary>
    public int Count => ShaderData.Count;

    /// <summary>Gets a shader parameter by field type.</summary>
    public ShaderDataElement? GetParameter(ShaderFieldType field)
    {
        return ShaderData.Find(e => e.Field == field);
    }

    /// <summary>Gets a float parameter value.</summary>
    public float? GetFloat(ShaderFieldType field)
    {
        return GetParameter(field) is ShaderFloat f ? f.Value : null;
    }

    /// <summary>Gets a float3 parameter (e.g., Diffuse color).</summary>
    public (float X, float Y, float Z)? GetFloat3(ShaderFieldType field)
    {
        return GetParameter(field) is ShaderFloat3 f ? (f.X, f.Y, f.Z) : null;
    }

    /// <summary>
    /// Gets a texture reference. Index-based (GEOM) references resolve through <paramref name="parentTgiBlocks"/>;
    /// null is returned when the index is out of range or no list was supplied.
    /// </summary>
    public ResourceKey? GetTexture(ShaderFieldType field, IReadOnlyList<RcolTgiBlock>? parentTgiBlocks)
    {
        if (GetParameter(field) is ShaderTextureRef { IsIndexed: true } r)
            return parentTgiBlocks is not null && r.Index >= 0 && r.Index < parentTgiBlocks.Count
                ? parentTgiBlocks[r.Index].ToResourceKey()
                : null;
        return GetTexture(field);
    }

    /// <summary>Gets a texture reference (null for index-based references; use the TGI-list overload).</summary>
    public ResourceKey? GetTexture(ShaderFieldType field)
    {
        return GetParameter(field) switch
        {
            ShaderTextureRef { IsIndexed: true } => null,
            ShaderTextureRef t => t.TextureKey,
            ShaderTextureKey t => t.TextureKey,
            _ => null
        };
    }
}
