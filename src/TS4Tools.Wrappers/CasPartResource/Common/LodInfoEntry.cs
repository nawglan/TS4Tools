using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents a LOD info entry containing level, assets, and key references.
/// Source: legacy_references/.../Handlers/LODInfoEntry.cs
/// </summary>
public sealed class LodInfoEntry
{

    /// <summary>
    /// The LOD level.
    /// </summary>
    public byte Level { get; set; }

    /// <summary>
    /// Unused field (preserved for round-trip compatibility).
    /// </summary>
    public uint Unused { get; set; }

    /// <summary>
    /// List of LOD assets for this entry.
    /// </summary>
    public LodAssetList Assets { get; set; } = new();

    /// <summary>
    /// List of byte indices referencing the TGI block list.
    /// </summary>
    public List<byte> LodKeyIndices { get; set; } = [];

    /// <summary>
    /// Creates an empty LodInfoEntry.
    /// </summary>
    public LodInfoEntry()
    {
    }

    /// <summary>
    /// Parses a LodInfoEntry from a span.
    /// Format: byte level + uint32 unused + LodAssetList + byte count + byte[]
    /// </summary>
    public static LodInfoEntry Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;

        var entry = new LodInfoEntry
        {
            Level = data[offset++],
            Unused = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..])
        };
        offset += 4;

        // Parse LOD asset list
        entry.Assets = LodAssetList.Parse(data[offset..], out int assetBytes);
        offset += assetBytes;

        // Parse LOD key indices
        byte keyCount = data[offset++];
        entry.LodKeyIndices = new List<byte>(keyCount);
        for (int i = 0; i < keyCount; i++)
        {
            if (offset >= data.Length)
            {
                throw new InvalidDataException($"Truncated LOD key index data at index {i}");
            }
            entry.LodKeyIndices.Add(data[offset++]);
        }

        bytesRead = offset;
        return entry;
    }

    /// <summary>
    /// Writes this entry to a span.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;

        buffer[offset++] = Level;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unused);
        offset += 4;

        offset += Assets.WriteTo(buffer[offset..]);

        if (LodKeyIndices.Count > 255)
        {
            throw new InvalidOperationException($"LOD key index count {LodKeyIndices.Count} exceeds byte max (255)");
        }

        buffer[offset++] = (byte)LodKeyIndices.Count;
        foreach (byte key in LodKeyIndices)
        {
            buffer[offset++] = key;
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize()
    {
        // level (1) + unused (4) + assets + key count (1) + keys
        return 1 + 4 + Assets.GetSerializedSize() + 1 + LodKeyIndices.Count;
    }
}
