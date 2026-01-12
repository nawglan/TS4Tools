namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of LOD assets with byte count prefix.
/// Source: legacy_references/.../Handlers/LODInfoEntry.cs (LODAssetList inner class)
/// </summary>
public sealed class LodAssetList : List<LodAsset>
{

    /// <summary>
    /// Creates an empty LodAssetList.
    /// </summary>
    public LodAssetList()
    {
    }

    /// <summary>
    /// Creates a LodAssetList with the specified assets.
    /// </summary>
    public LodAssetList(IEnumerable<LodAsset> assets) : base(assets)
    {
    }

    /// <summary>
    /// Parses a LodAssetList from a span.
    /// Format: byte count + (count * 12 bytes)
    /// </summary>
    public static LodAssetList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];
        var list = new LodAssetList();
        list.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            if (offset + LodAsset.SerializedSize > data.Length)
            {
                throw new InvalidDataException($"Truncated LOD asset data at index {i}");
            }

            list.Add(LodAsset.Parse(data[offset..]));
            offset += LodAsset.SerializedSize;
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Writes this list to a span.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        if (Count > 255)
        {
            throw new InvalidOperationException($"LodAssetList count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;

        foreach (var asset in this)
        {
            asset.WriteTo(buffer[offset..]);
            offset += LodAsset.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize() => 1 + (Count * LodAsset.SerializedSize);
}
