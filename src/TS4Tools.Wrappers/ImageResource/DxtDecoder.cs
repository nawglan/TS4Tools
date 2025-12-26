namespace TS4Tools.Wrappers;

/// <summary>
/// Decoder for DXT (S3 Texture Compression) formats.
/// Implements BC1 (DXT1) and BC3 (DXT5) block decompression.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsSquish.cs
/// - Legacy uses native squish library via P/Invoke
/// - This implementation is pure managed C# based on DirectX/OpenGL specifications
/// - Output is RGBA32 (8 bits per channel, in memory order: R, G, B, A)
/// </summary>
public static class DxtDecoder
{
    /// <summary>
    /// DDS header size in bytes.
    /// </summary>
    private const int DdsHeaderSize = 128;

    /// <summary>
    /// FourCC values for DXT formats.
    /// </summary>
    public const uint FourCcDxt1 = 0x31545844; // "DXT1"
    public const uint FourCcDxt5 = 0x35545844; // "DXT5"

    /// <summary>
    /// Gets the FourCC from a DDS file.
    /// </summary>
    public static uint GetFourCc(ReadOnlySpan<byte> data)
    {
        if (data.Length < 88)
            return 0;

        return (uint)(data[84] | (data[85] << 8) | (data[86] << 16) | (data[87] << 24));
    }

    /// <summary>
    /// Gets image dimensions from a DDS header.
    /// </summary>
    public static (int Width, int Height) GetDimensions(ReadOnlySpan<byte> data)
    {
        if (data.Length < 20)
            return (0, 0);

        var height = data[12] | (data[13] << 8) | (data[14] << 16) | (data[15] << 24);
        var width = data[16] | (data[17] << 8) | (data[18] << 16) | (data[19] << 24);

        return (width, height);
    }

    /// <summary>
    /// Decompresses a DDS texture to RGBA32 pixels.
    /// </summary>
    /// <param name="ddsData">Complete DDS file data including header.</param>
    /// <returns>RGBA32 pixel data (4 bytes per pixel: R, G, B, A), or null if unsupported.</returns>
    public static byte[]? DecompressDds(ReadOnlySpan<byte> ddsData)
    {
        if (ddsData.Length < DdsHeaderSize)
            return null;

        var fourCc = GetFourCc(ddsData);
        var (width, height) = GetDimensions(ddsData);

        if (width <= 0 || height <= 0)
            return null;

        var blockData = ddsData[DdsHeaderSize..];

        return fourCc switch
        {
            FourCcDxt1 => DecompressDxt1(blockData, width, height),
            FourCcDxt5 => DecompressDxt5(blockData, width, height),
            _ => null
        };
    }

    /// <summary>
    /// Decompresses DXT1 (BC1) block data to RGBA32 pixels.
    ///
    /// DXT1 block format (8 bytes per 4x4 pixel block):
    /// - Bytes 0-1: color0 (RGB565, little-endian)
    /// - Bytes 2-3: color1 (RGB565, little-endian)
    /// - Bytes 4-7: 16 x 2-bit color indices
    ///
    /// Color interpolation:
    /// - If color0 > color1: 4 colors (c0, c1, 2/3*c0+1/3*c1, 1/3*c0+2/3*c1)
    /// - If color0 <= color1: 3 colors + transparent (c0, c1, 1/2*c0+1/2*c1, transparent)
    /// </summary>
    public static byte[] DecompressDxt1(ReadOnlySpan<byte> blockData, int width, int height)
    {
        var output = new byte[width * height * 4];
        var blocksX = (width + 3) / 4;
        var blocksY = (height + 3) / 4;
        const int blockSize = 8;

        var blockOffset = 0;
        for (var by = 0; by < blocksY; by++)
        {
            for (var bx = 0; bx < blocksX; bx++)
            {
                if (blockOffset + blockSize > blockData.Length)
                    break;

                var block = blockData.Slice(blockOffset, blockSize);
                DecodeDxt1Block(block, output, bx * 4, by * 4, width, height);
                blockOffset += blockSize;
            }
        }

        return output;
    }

    /// <summary>
    /// Decompresses DXT5 (BC3) block data to RGBA32 pixels.
    ///
    /// DXT5 block format (16 bytes per 4x4 pixel block):
    /// - Byte 0: alpha0
    /// - Byte 1: alpha1
    /// - Bytes 2-7: 16 x 3-bit alpha indices (48 bits)
    /// - Bytes 8-15: DXT1 color block
    ///
    /// Alpha interpolation:
    /// - If alpha0 > alpha1: 8 alphas (a0, a1, then 6 interpolated)
    /// - If alpha0 <= alpha1: 6 alphas + 0 + 255
    /// </summary>
    public static byte[] DecompressDxt5(ReadOnlySpan<byte> blockData, int width, int height)
    {
        var output = new byte[width * height * 4];
        var blocksX = (width + 3) / 4;
        var blocksY = (height + 3) / 4;
        const int blockSize = 16;

        var blockOffset = 0;
        for (var by = 0; by < blocksY; by++)
        {
            for (var bx = 0; bx < blocksX; bx++)
            {
                if (blockOffset + blockSize > blockData.Length)
                    break;

                var block = blockData.Slice(blockOffset, blockSize);
                DecodeDxt5Block(block, output, bx * 4, by * 4, width, height);
                blockOffset += blockSize;
            }
        }

        return output;
    }

    /// <summary>
    /// Decodes a single DXT1 block (4x4 pixels).
    /// </summary>
    private static void DecodeDxt1Block(ReadOnlySpan<byte> block, byte[] output, int x, int y, int width, int height)
    {
        // Read colors (RGB565, little-endian)
        var c0 = (ushort)(block[0] | (block[1] << 8));
        var c1 = (ushort)(block[2] | (block[3] << 8));

        // Decode RGB565 to RGB888
        Span<byte> colors = stackalloc byte[16]; // 4 colors x 4 bytes (RGBA)
        DecodeRgb565(c0, colors[..4]);
        DecodeRgb565(c1, colors.Slice(4, 4));

        // Interpolate remaining colors
        if (c0 > c1)
        {
            // 4-color mode: interpolate 2 additional colors
            for (var i = 0; i < 3; i++)
            {
                colors[8 + i] = (byte)((2 * colors[i] + colors[4 + i] + 1) / 3);
                colors[12 + i] = (byte)((colors[i] + 2 * colors[4 + i] + 1) / 3);
            }
            colors[11] = 255; // alpha
            colors[15] = 255; // alpha
        }
        else
        {
            // 3-color + transparent mode
            for (var i = 0; i < 3; i++)
            {
                colors[8 + i] = (byte)((colors[i] + colors[4 + i]) / 2);
            }
            colors[11] = 255; // alpha
            // Color 3 is transparent black
            colors[12] = 0;
            colors[13] = 0;
            colors[14] = 0;
            colors[15] = 0;
        }

        // Read indices (32 bits = 16 x 2-bit indices)
        var indices = (uint)(block[4] | (block[5] << 8) | (block[6] << 16) | (block[7] << 24));

        // Write pixels
        for (var py = 0; py < 4; py++)
        {
            var destY = y + py;
            if (destY >= height) break;

            for (var px = 0; px < 4; px++)
            {
                var destX = x + px;
                if (destX >= width) continue;

                var colorIndex = (int)((indices >> ((py * 4 + px) * 2)) & 0x3);
                var destOffset = (destY * width + destX) * 4;

                output[destOffset] = colors[colorIndex * 4];
                output[destOffset + 1] = colors[colorIndex * 4 + 1];
                output[destOffset + 2] = colors[colorIndex * 4 + 2];
                output[destOffset + 3] = colors[colorIndex * 4 + 3];
            }
        }
    }

    /// <summary>
    /// Decodes a single DXT5 block (4x4 pixels).
    /// </summary>
    private static void DecodeDxt5Block(ReadOnlySpan<byte> block, byte[] output, int x, int y, int width, int height)
    {
        // Decode alpha block (bytes 0-7)
        var alpha0 = block[0];
        var alpha1 = block[1];

        Span<byte> alphas = stackalloc byte[8];
        alphas[0] = alpha0;
        alphas[1] = alpha1;

        if (alpha0 > alpha1)
        {
            // 8-alpha mode: interpolate 6 additional alphas
            for (var i = 0; i < 6; i++)
            {
                alphas[2 + i] = (byte)(((6 - i) * alpha0 + (i + 1) * alpha1 + 3) / 7);
            }
        }
        else
        {
            // 6-alpha mode: interpolate 4 additional alphas, then 0 and 255
            for (var i = 0; i < 4; i++)
            {
                alphas[2 + i] = (byte)(((4 - i) * alpha0 + (i + 1) * alpha1 + 2) / 5);
            }
            alphas[6] = 0;
            alphas[7] = 255;
        }

        // Read alpha indices (48 bits = 16 x 3-bit indices)
        // Stored as 6 bytes
        var alphaIndices = (ulong)block[2] | ((ulong)block[3] << 8) | ((ulong)block[4] << 16) |
                          ((ulong)block[5] << 24) | ((ulong)block[6] << 32) | ((ulong)block[7] << 40);

        // Decode color block (bytes 8-15) - same as DXT1
        var colorBlock = block[8..];

        var c0 = (ushort)(colorBlock[0] | (colorBlock[1] << 8));
        var c1 = (ushort)(colorBlock[2] | (colorBlock[3] << 8));

        Span<byte> colors = stackalloc byte[16];
        DecodeRgb565(c0, colors[..4]);
        DecodeRgb565(c1, colors.Slice(4, 4));

        // DXT5 always uses 4-color mode for the color block
        for (var i = 0; i < 3; i++)
        {
            colors[8 + i] = (byte)((2 * colors[i] + colors[4 + i] + 1) / 3);
            colors[12 + i] = (byte)((colors[i] + 2 * colors[4 + i] + 1) / 3);
        }
        colors[11] = 255;
        colors[15] = 255;

        // Read color indices
        var colorIndices = (uint)(colorBlock[4] | (colorBlock[5] << 8) | (colorBlock[6] << 16) | (colorBlock[7] << 24));

        // Write pixels
        for (var py = 0; py < 4; py++)
        {
            var destY = y + py;
            if (destY >= height) break;

            for (var px = 0; px < 4; px++)
            {
                var destX = x + px;
                if (destX >= width) continue;

                var pixelIndex = py * 4 + px;
                var colorIndex = (int)((colorIndices >> (pixelIndex * 2)) & 0x3);
                var alphaIndex = (int)((alphaIndices >> (pixelIndex * 3)) & 0x7);

                var destOffset = (destY * width + destX) * 4;

                output[destOffset] = colors[colorIndex * 4];
                output[destOffset + 1] = colors[colorIndex * 4 + 1];
                output[destOffset + 2] = colors[colorIndex * 4 + 2];
                output[destOffset + 3] = alphas[alphaIndex];
            }
        }
    }

    /// <summary>
    /// Decodes RGB565 to RGBA8888.
    /// </summary>
    private static void DecodeRgb565(ushort color, Span<byte> rgba)
    {
        // Extract components (5-6-5 bits)
        var r5 = (color >> 11) & 0x1F;
        var g6 = (color >> 5) & 0x3F;
        var b5 = color & 0x1F;

        // Expand to 8 bits with proper rounding
        // R: 5 bits -> 8 bits: (r5 << 3) | (r5 >> 2)
        // G: 6 bits -> 8 bits: (g6 << 2) | (g6 >> 4)
        // B: 5 bits -> 8 bits: (b5 << 3) | (b5 >> 2)
        rgba[0] = (byte)((r5 << 3) | (r5 >> 2));
        rgba[1] = (byte)((g6 << 2) | (g6 >> 4));
        rgba[2] = (byte)((b5 << 3) | (b5 >> 2));
        rgba[3] = 255;
    }
}
