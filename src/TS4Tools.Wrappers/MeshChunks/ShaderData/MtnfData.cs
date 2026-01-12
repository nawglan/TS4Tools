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

    /// <summary>Raw data preserved for round-tripping when parsing fails.</summary>
    private byte[] _rawData = [];

    /// <summary>Whether structured parsing succeeded.</summary>
    public bool IsStructuredParsed => ShaderData.Count > 0 || _rawData.Length == 0;

    /// <summary>Creates empty MTNF data.</summary>
    public MtnfData() { }

    /// <summary>Creates MTNF data from raw bytes (backwards-compatible constructor).</summary>
    public MtnfData(ReadOnlySpan<byte> data)
    {
        ParseInternal(data);
    }

    /// <summary>Parses MTNF data from raw bytes.</summary>
    public static MtnfData Parse(ReadOnlySpan<byte> data)
    {
        var mtnf = new MtnfData();
        mtnf.ParseInternal(data);
        return mtnf;
    }

    /// <summary>Tries to parse MTNF data, returning false on failure.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out MtnfData? result)
    {
        try
        {
            result = Parse(data);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>Checks if data starts with a valid MTNF/MTRL tag.</summary>
    public static bool IsValidTag(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return false;
        uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data);
        return tag == MtnfTag || tag == MtrlTag;
    }

    /// <summary>Internal parsing implementation.</summary>
    private void ParseInternal(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
        {
            _rawData = data.ToArray();
            return;
        }

        int offset = 0;

        // Read and validate tag
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

        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Store raw data for round-tripping
        if (dataLength > 0 && offset + dataLength <= data.Length)
        {
            _rawData = data.Slice(offset, dataLength).ToArray();
        }
        else if (offset < data.Length)
        {
            _rawData = data[offset..].ToArray();
        }

        // Try to parse shader data entries
        // Header structure: count (4), then count * (field, type, count, offset) = 16 bytes each
        if (offset + 4 > data.Length)
            return;

        // Remember the start of the shader data section (for relative offsets in elements)
        int shaderDataStart = offset;

        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate entry count
        if (entryCount < 0 || entryCount > 1000)
            return;

        // Data offsets in element headers are relative to shader data section start
        for (int i = 0; i < entryCount && offset + 16 <= data.Length; i++)
        {
            try
            {
                var element = ShaderDataElement.Parse(data, ref offset, shaderDataStart);
                ShaderData.Add(element);
            }
            catch (InvalidDataException)
            {
                // Skip invalid entries
                offset += 16;
            }
        }
    }

    /// <summary>Serializes MTNF data to bytes.</summary>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        uint tag = Tag == "MTRL" ? MtrlTag : MtnfTag;
        writer.Write(tag);

        // Write unknown1
        writer.Write(Unknown1);

        // If we have structured data, serialize it; otherwise use raw data
        if (ShaderData.Count > 0)
        {
            // Calculate data section
            using var dataMs = new MemoryStream();
            using var dataWriter = new BinaryWriter(dataMs);

            // Build offset table and data
            var offsets = new List<uint>();
            foreach (var element in ShaderData)
            {
                offsets.Add((uint)dataMs.Position);
                element.WriteData(dataWriter);
            }

            // Calculate data length (header entries + data)
            int headerSize = 4 + ShaderData.Count * 16; // count + entries
            int dataSize = (int)dataMs.Length;
            writer.Write(headerSize + dataSize);

            // Write entry count
            writer.Write(ShaderData.Count);

            // Write entry headers with offsets adjusted for header size
            for (int i = 0; i < ShaderData.Count; i++)
            {
                ShaderData[i].WriteHeader(writer, (uint)(headerSize + offsets[i]));
            }

            // Write data section
            dataMs.Position = 0;
            dataMs.CopyTo(ms);
        }
        else
        {
            // Fallback to raw data
            writer.Write(_rawData.Length);
            writer.Write(_rawData);
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

    /// <summary>Gets a texture reference.</summary>
    public ResourceKey? GetTexture(ShaderFieldType field)
    {
        return GetParameter(field) switch
        {
            ShaderTextureRef t => t.TextureKey,
            ShaderTextureKey t => t.TextureKey,
            _ => null
        };
    }
}
