namespace TS4Tools.Wrappers;

/// <summary>
/// Decoder for Sims 4 DST (shuffled texture) format.
/// DST files are DXT textures with block data shuffled for compression efficiency.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs
/// - DST1 is shuffled DXT1 (BC1): 8-byte blocks split into two halves
/// - DST5 is shuffled DXT5 (BC3): 16-byte blocks split into four parts
/// - The DDS header (128 bytes) is preserved, only the data section is shuffled
/// </summary>
public static class DstDecoder
{
    /// <summary>
    /// DDS header size in bytes.
    /// </summary>
    private const int DdsHeaderSize = 128;

    /// <summary>
    /// DST1 FourCC value.
    /// </summary>
    public const uint FourCcDst1 = 0x31545344; // "DST1"

    /// <summary>
    /// DST5 FourCC value.
    /// </summary>
    public const uint FourCcDst5 = 0x35545344; // "DST5"

    /// <summary>
    /// DXT1 FourCC value.
    /// </summary>
    public const uint FourCcDxt1 = 0x31545844; // "DXT1"

    /// <summary>
    /// DXT5 FourCC value.
    /// </summary>
    public const uint FourCcDxt5 = 0x35545844; // "DXT5"

    /// <summary>
    /// Detects the DST format from the FourCC in the DDS header.
    /// </summary>
    /// <param name="data">The DST file data including header.</param>
    /// <returns>The FourCC value, or 0 if invalid.</returns>
    public static uint GetFourCc(ReadOnlySpan<byte> data)
    {
        // FourCC is at offset 84 in DDS header (after magic + header size + flags + height + width + pitch + depth + mipmaps + reserved[11] + pixel format size + pixel format flags)
        if (data.Length < 88)
            return 0;

        return (uint)(data[84] | (data[85] << 8) | (data[86] << 16) | (data[87] << 24));
    }

    /// <summary>
    /// Checks if the data is a DST format.
    /// </summary>
    public static bool IsDst(ReadOnlySpan<byte> data)
    {
        var fourCc = GetFourCc(data);
        return fourCc == FourCcDst1 || fourCc == FourCcDst5;
    }

    /// <summary>
    /// Converts DST data to standard DDS format by unshuffling the blocks.
    /// </summary>
    /// <param name="dstData">The DST file data including header.</param>
    /// <returns>Unshuffled DDS data, or null if format is not recognized.</returns>
    public static byte[]? UnshuffleToDds(ReadOnlySpan<byte> dstData)
    {
        if (dstData.Length < DdsHeaderSize)
            return null;

        var fourCc = GetFourCc(dstData);

        return fourCc switch
        {
            FourCcDst1 => UnshuffleDst1(dstData),
            FourCcDst5 => UnshuffleDst5(dstData),
            _ => null
        };
    }

    /// <summary>
    /// Unshuffles DST1 (shuffled DXT1) data to standard DXT1 DDS.
    ///
    /// DST1 shuffle algorithm:
    /// - Original DXT1: blocks are 8 bytes each [color0, color1, indices]
    /// - DST1: first halves (4 bytes) of all blocks, then second halves of all blocks
    /// - Unshuffle: interleave the two halves back together
    /// </summary>
    private static byte[] UnshuffleDst1(ReadOnlySpan<byte> data)
    {
        var dataSize = data.Length - DdsHeaderSize;
        if (dataSize <= 0 || dataSize % 8 != 0)
            return data.ToArray(); // Return as-is if invalid

        var result = new byte[data.Length];

        // Copy header and update FourCC from DST1 to DXT1
        data[..DdsHeaderSize].CopyTo(result);
        SetFourCc(result, FourCcDxt1);

        var blockData = data[DdsHeaderSize..];
        var halfSize = dataSize / 2;
        var blockCount = halfSize / 4;

        var outputOffset = DdsHeaderSize;
        var firstHalfOffset = 0;
        var secondHalfOffset = halfSize;

        // Interleave: 4 bytes from first half, 4 bytes from second half
        for (var i = 0; i < blockCount; i++)
        {
            blockData.Slice(firstHalfOffset, 4).CopyTo(result.AsSpan(outputOffset));
            blockData.Slice(secondHalfOffset, 4).CopyTo(result.AsSpan(outputOffset + 4));

            outputOffset += 8;
            firstHalfOffset += 4;
            secondHalfOffset += 4;
        }

        return result;
    }

    /// <summary>
    /// Unshuffles DST5 (shuffled DXT5) data to standard DXT5 DDS.
    ///
    /// DST5 shuffle algorithm:
    /// - Original DXT5: blocks are 16 bytes each [alpha0, alpha1, alphaIndices(6), color0, color1, colorIndices(4)]
    /// - DST5 splits into 4 sections:
    ///   - Section 0: alpha endpoints (2 bytes per block) = 1/8 of data
    ///   - Section 2: color endpoints (4 bytes per block) = 1/4 of data
    ///   - Section 1: alpha indices (6 bytes per block) = 6/16 of data
    ///   - Section 3: color indices (4 bytes per block) = remaining
    /// - Unshuffle: reconstruct as [2 from s0, 6 from s1, 4 from s2, 4 from s3]
    /// </summary>
    private static byte[] UnshuffleDst5(ReadOnlySpan<byte> data)
    {
        var dataSize = data.Length - DdsHeaderSize;
        if (dataSize <= 0 || dataSize % 16 != 0)
            return data.ToArray(); // Return as-is if invalid

        var result = new byte[data.Length];

        // Copy header and update FourCC from DST5 to DXT5
        data[..DdsHeaderSize].CopyTo(result);
        SetFourCc(result, FourCcDxt5);

        var blockData = data[DdsHeaderSize..];

        // Calculate section offsets (matching legacy algorithm)
        var offset0 = 0;                           // Alpha endpoints: 2 bytes per block
        var offset2 = dataSize >> 3;               // Color endpoints: 4 bytes per block (starts at 1/8)
        var offset1 = offset2 + (dataSize >> 2);   // Alpha indices: 6 bytes per block (starts at 1/8 + 1/4)
        var offset3 = offset1 + (6 * dataSize >> 4); // Color indices: 4 bytes per block

        var blockCount = (offset2 - offset0) / 2;  // Number of blocks

        var outputOffset = DdsHeaderSize;

        for (var i = 0; i < blockCount; i++)
        {
            // Write: 2 bytes alpha endpoints, 6 bytes alpha indices, 4 bytes color endpoints, 4 bytes color indices
            blockData.Slice(offset0, 2).CopyTo(result.AsSpan(outputOffset));
            blockData.Slice(offset1, 6).CopyTo(result.AsSpan(outputOffset + 2));
            blockData.Slice(offset2, 4).CopyTo(result.AsSpan(outputOffset + 8));
            blockData.Slice(offset3, 4).CopyTo(result.AsSpan(outputOffset + 12));

            outputOffset += 16;
            offset0 += 2;
            offset1 += 6;
            offset2 += 4;
            offset3 += 4;
        }

        return result;
    }

    /// <summary>
    /// Updates the FourCC value in a DDS header.
    /// </summary>
    private static void SetFourCc(Span<byte> data, uint fourCc)
    {
        if (data.Length < 88)
            return;

        data[84] = (byte)(fourCc & 0xFF);
        data[85] = (byte)((fourCc >> 8) & 0xFF);
        data[86] = (byte)((fourCc >> 16) & 0xFF);
        data[87] = (byte)((fourCc >> 24) & 0xFF);
    }
}
