// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/MTNF.cs

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Utility class for parsing MTNF (Material Info) data embedded within MATD blocks.
/// This provides basic header parsing while preserving raw shader data for round-tripping.
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/s4piRCOLChunks/MTNF.cs
/// MTNF is embedded within MATD blocks (the MtrlData field).
/// Format:
/// - Tag: 4 bytes ("MTNF" or "MTRL")
/// - Unknown1: 4 bytes (uint32)
/// - DataLength: 4 bytes (uint32) - length of shader data
/// - ShaderData: variable length
/// </remarks>
public sealed class MtnfData : IEquatable<MtnfData>
{
    /// <summary>Expected tag for MTNF data.</summary>
    public const uint TagMtnf = 0x464E544D; // "MTNF" little-endian

    /// <summary>Alternative tag for MTRL data.</summary>
    public const uint TagMtrl = 0x4C52544D; // "MTRL" little-endian

    /// <summary>
    /// The tag read from the data ("MTNF" or "MTRL").
    /// </summary>
    public string Tag { get; private set; } = "MTNF";

    /// <summary>
    /// Unknown field preserved for round-tripping.
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Length of the shader data section.
    /// </summary>
    public uint DataLength { get; private set; }

    /// <summary>
    /// Raw shader data preserved for round-tripping.
    /// </summary>
    public byte[] ShaderData { get; private set; } = [];

    /// <summary>
    /// Creates an empty MtnfData.
    /// </summary>
    public MtnfData()
    {
    }

    /// <summary>
    /// Creates MtnfData from raw bytes.
    /// </summary>
    public MtnfData(ReadOnlySpan<byte> data)
    {
        Parse(data);
    }

    /// <summary>
    /// Parses MTNF data from a byte span.
    /// Source: MTNF.cs Parse() lines 63-72
    /// </summary>
    public void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
            throw new InvalidDataException("MTNF data too short for header");

        int pos = 0;

        // Read and validate tag
        uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        if (tag == TagMtnf)
        {
            Tag = "MTNF";
        }
        else if (tag == TagMtrl)
        {
            Tag = "MTRL";
        }
        else
        {
            throw new InvalidDataException($"Invalid MTNF tag: expected 'MTNF' or 'MTRL', got 0x{tag:X8}");
        }

        // Read unknown1
        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read data length
        DataLength = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Store shader data for round-tripping
        if (DataLength > 0 && pos + DataLength <= data.Length)
        {
            ShaderData = data.Slice(pos, (int)DataLength).ToArray();
        }
        else if (pos < data.Length)
        {
            // If length seems wrong, take remaining data
            ShaderData = data[pos..].ToArray();
        }
    }

    /// <summary>
    /// Serializes the MTNF data back to bytes.
    /// Source: MTNF.cs UnParse() lines 74-89
    /// </summary>
    public ReadOnlyMemory<byte> Serialize()
    {
        int size = 12 + ShaderData.Length;
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag
        uint tag = Tag == "MTRL" ? TagMtrl : TagMtnf;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), tag);
        pos += 4;

        // Write unknown1
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Unknown1);
        pos += 4;

        // Write data length
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)ShaderData.Length);
        pos += 4;

        // Write shader data
        if (ShaderData.Length > 0)
        {
            ShaderData.CopyTo(buffer.AsSpan(pos));
        }

        return buffer;
    }

    /// <summary>
    /// Tries to parse MTNF data from a byte span.
    /// Returns false if the data is invalid.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out MtnfData? result)
    {
        try
        {
            result = new MtnfData(data);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Checks if the data starts with a valid MTNF/MTRL tag.
    /// </summary>
    public static bool IsValidTag(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return false;

        uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data);
        return tag == TagMtnf || tag == TagMtrl;
    }

    public bool Equals(MtnfData? other)
    {
        if (other is null) return false;
        if (Tag != other.Tag || Unknown1 != other.Unknown1) return false;
        if (ShaderData.Length != other.ShaderData.Length) return false;
        return ShaderData.AsSpan().SequenceEqual(other.ShaderData);
    }

    public override bool Equals(object? obj) => obj is MtnfData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Tag, Unknown1, ShaderData.Length);
    public override string ToString() => $"MtnfData [{Tag}] Unknown1=0x{Unknown1:X8}, DataLength={ShaderData.Length}";
}
