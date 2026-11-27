// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 288-488 (ToBitMap, GetSkinBitMap, OutputType)
// Source: legacy_references/Sims4Tools/s4pe Helpers/DMAPImageHelper/Program.cs lines 33-87 (/export and /exportRobe)

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>Which pixel channel set to extract from a deformer map.</summary>
/// <remarks>Source: DeformerMapResource.cs lines 290-294 (enum OutputType).</remarks>
public enum DeformerMapImageType
{
    /// <summary>The skin-tight deformation (first three bytes of each pixel).</summary>
    Skin,

    /// <summary>The robe deformation (bytes 3-5 when present, copy or zero otherwise).</summary>
    Robe,
}

/// <summary>
/// A decoded deformer map image: 24-bit pixels, three bytes per pixel, row-major, channel order as stored in the DMAP.
/// </summary>
/// <param name="Width">Width in pixels (MaxCol - MinCol + 1).</param>
/// <param name="Height">Height in pixels (MaxRow - MinRow + 1).</param>
/// <param name="Pixels">3 bytes per pixel, <c>Width * Height * 3</c> bytes.</param>
public sealed record DeformerMapImage(int Width, int Height, byte[] Pixels);

/// <summary>
/// Converts <see cref="DeformerMapResource"/> scan-line data to and from raw images / 24-bit BMP files.
/// </summary>
/// <remarks>
/// Source: DeformerMapResource.ToBitMap (lines 296-488). Legacy decodes each scan line either from raw pixels
/// (6 bytes per pixel when the robe channel is present, otherwise 3) or from an indexed RLE stream, and writes a 24-bit BMP whose
/// rows follow scan-line order and whose bytes are the stored channel values (no RGB/BGR swap). That behaviour is kept.
/// Differences: the BMP file-size and image-size fields are filled in (legacy writes zero), and every index/offset taken
/// from the file is range-checked (legacy would throw IndexOutOfRange or divide by zero on bad data).
/// Legacy has no image import; <see cref="ImportFromImage"/> is the inverse of <see cref="ToImage"/> using uncompressed scan lines, which
/// is the form the legacy parser reads when <c>IsCompressed</c> is false.
/// </remarks>
public static class DeformerMapImageConverter
{
    private const int MaxImageDimension = 8192;

    /// <summary>
    /// Extracts the skin or robe image.
    /// </summary>
    /// <returns>The image, or null when <c>MaxCol == 0</c>. Legacy uses that test for "empty", so a map with a single column (MinCol = MaxCol = 0) is also reported as empty.</returns>
    /// <exception cref="InvalidDataException">The scan-line data is inconsistent with the map's declared size.</exception>
    public static DeformerMapImage? ToImage(DeformerMapResource map, DeformerMapImageType type)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (map.MaxCol == 0)
            return null;

        long widthL = (long)map.MaxCol - map.MinCol + 1;
        long heightL = (long)map.MaxRow - map.MinRow + 1;
        if (widthL <= 0 || heightL <= 0 || widthL > MaxImageDimension || heightL > MaxImageDimension)
            throw new InvalidDataException($"Invalid deformer map size {widthL}x{heightL}.");

        int width = (int)widthL;
        int height = (int)heightL;
        if (map.ScanLines.Count < height)
            throw new InvalidDataException($"Deformer map declares {height} rows but has {map.ScanLines.Count} scan lines.");

        var skin = new byte[width * height * 3];
        var robe = new byte[width * height * 3];
        int destSkin = 0;
        int destRobe = 0;

        for (int row = 0; row < height; row++)
        {
            var scan = map.ScanLines[row];
            int pixelSize = scan.RobeChannel == RobeChannel.Present ? 6 : 3;

            if (!scan.IsCompressed)
            {
                var px = scan.UncompressedPixels;
                if (px.Length < width * pixelSize)
                    throw new InvalidDataException($"Scan line {row} has too little pixel data.");

                for (int j = 0; j < width; j++)
                {
                    int o = j * pixelSize;
                    skin[destSkin++] = px[o];
                    skin[destSkin++] = px[o + 1];
                    skin[destSkin++] = px[o + 2];
                    WriteRobe(scan.RobeChannel, robe, ref destRobe, px, o);
                }
            }
            else
            {
                if (scan.NumIndexes < 2 || scan.PixelPosIndexes.Length < scan.NumIndexes || scan.DataPosIndexes.Length < scan.NumIndexes)
                    throw new InvalidDataException($"Scan line {row} has an invalid index table.");

                var rle = scan.RleArrayOfPixels;
                for (int j = 0; j < width; j++)
                {
                    // Cache increment for indexing in pixel space (1 entry was added for the remainder of the division)
                    uint step = 1U + (uint)(width / (scan.NumIndexes - 1));
                    uint idx = (uint)j / step;
                    if (idx >= scan.NumIndexes)
                        throw new InvalidDataException($"Scan line {row} index out of range.");

                    uint pixelPosX = scan.PixelPosIndexes[idx];
                    uint dataPos = (uint)scan.DataPosIndexes[idx] * (uint)(pixelSize + 1); // +1 for the run length byte
                    if (dataPos >= rle.Length)
                        throw new InvalidDataException($"Scan line {row} data position out of range.");

                    uint runLength = rle[dataPos];

                    // Unwind runs until the run covering pixel j is found
                    while (j >= pixelPosX + runLength)
                    {
                        pixelPosX += runLength;
                        dataPos += (uint)(1 + pixelSize);
                        if (dataPos >= rle.Length)
                            throw new InvalidDataException($"Scan line {row} run data is truncated.");
                        runLength = rle[dataPos];
                    }

                    uint pixelStart = dataPos + 1;
                    if (pixelStart + (uint)pixelSize > rle.Length)
                        throw new InvalidDataException($"Scan line {row} pixel data is truncated.");

                    skin[destSkin++] = rle[pixelStart];
                    skin[destSkin++] = rle[pixelStart + 1];
                    skin[destSkin++] = rle[pixelStart + 2];
                    WriteRobe(scan.RobeChannel, robe, ref destRobe, rle, (int)pixelStart);
                }
            }
        }

        return new DeformerMapImage(width, height, type == DeformerMapImageType.Robe ? robe : skin);
    }

    private static void WriteRobe(RobeChannel channel, byte[] robe, ref int dest, byte[] src, int o)
    {
        switch (channel)
        {
            case RobeChannel.Present:
                robe[dest++] = src[o + 3];
                robe[dest++] = src[o + 4];
                robe[dest++] = src[o + 5];
                break;
            case RobeChannel.Dropped:
                robe[dest++] = 0;
                robe[dest++] = 0;
                robe[dest++] = 0;
                break;
            case RobeChannel.IsCopy:
                robe[dest++] = src[o];
                robe[dest++] = src[o + 1];
                robe[dest++] = src[o + 2];
                break;
            default:
                dest += 3; // unknown channel value: leave zeros
                break;
        }
    }

    /// <summary>
    /// Produces a 24-bit uncompressed BMP file of the skin or robe image.
    /// </summary>
    /// <returns>BMP bytes, or null when the map is empty.</returns>
    /// <remarks>Source: DeformerMapResource.cs lines 440-486 (BMP header and padded rows).</remarks>
    public static byte[]? ToBitmap(DeformerMapResource map, DeformerMapImageType type)
    {
        var image = ToImage(map, type);
        if (image is null)
            return null;

        int width = image.Width;
        int height = image.Height;
        int bytesPerLine = ((width * 24) + 7) / 8;
        int padding = (4 - (bytesPerLine % 4)) % 4;
        int imageSize = (bytesPerLine + padding) * height;

        var bmp = new byte[54 + imageSize];
        var span = bmp.AsSpan();
        BinaryPrimitives.WriteUInt16LittleEndian(span, 0x4D42); // "BM"
        BinaryPrimitives.WriteInt32LittleEndian(span[2..], bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(span[10..], 54);
        BinaryPrimitives.WriteInt32LittleEndian(span[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(span[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[22..], height);
        BinaryPrimitives.WriteUInt16LittleEndian(span[26..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(span[28..], 24);
        BinaryPrimitives.WriteInt32LittleEndian(span[34..], imageSize);

        int dest = 54;
        int src = 0;
        for (int row = 0; row < height; row++)
        {
            image.Pixels.AsSpan(src, width * 3).CopyTo(span[dest..]);
            src += width * 3;
            dest += (width * 3) + padding;
        }

        return bmp;
    }

    /// <summary>
    /// Replaces the map's pixel data with <paramref name="width"/> x <paramref name="height"/> uncompressed scan lines.
    /// </summary>
    /// <param name="map">The map to update. Version, age/gender, physique and DoubledWidth/Height are left untouched.</param>
    /// <param name="skinPixels">Skin-tight pixels, 3 bytes per pixel.</param>
    /// <param name="robePixels">Optional robe pixels (3 bytes per pixel). When empty the robe channel is marked <see cref="RobeChannel.Dropped"/>.</param>
    /// <param name="width">Image width (1-8192).</param>
    /// <param name="height">Image height (1-8192).</param>
    /// <remarks>
    /// Not in legacy (which only exports). Mirrors the layout the legacy parser reads for uncompressed scan lines
    /// (DeformerMapResource.cs lines 217-224): with a robe channel each pixel is 6 bytes (skin then robe), otherwise 3.
    /// MinCol/MinRow become 0 and MaxCol/MaxRow become width-1/height-1.
    /// </remarks>
    public static void ImportFromImage(DeformerMapResource map, ReadOnlySpan<byte> skinPixels, ReadOnlySpan<byte> robePixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (width < 1 || height < 1 || width > MaxImageDimension || height > MaxImageDimension)
            throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be between 1 and 8192.");

        int expected = width * height * 3;
        if (skinPixels.Length != expected)
            throw new ArgumentException($"Skin pixel data must be {expected} bytes.", nameof(skinPixels));

        bool hasRobe = !robePixels.IsEmpty;
        if (hasRobe && robePixels.Length != expected)
            throw new ArgumentException($"Robe pixel data must be {expected} bytes.", nameof(robePixels));

        int pixelSize = hasRobe ? 6 : 3;
        var lines = new List<ScanLine>(height);
        for (int row = 0; row < height; row++)
        {
            var data = new byte[width * pixelSize];
            for (int x = 0; x < width; x++)
            {
                int src = ((row * width) + x) * 3;
                skinPixels.Slice(src, 3).CopyTo(data.AsSpan(x * pixelSize));
                if (hasRobe)
                    robePixels.Slice(src, 3).CopyTo(data.AsSpan((x * pixelSize) + 3));
            }

            lines.Add(new ScanLine
            {
                ScanLineDataSize = (ushort)Math.Min(ushort.MaxValue, 4 + data.Length),
                IsCompressed = false,
                RobeChannel = hasRobe ? RobeChannel.Present : RobeChannel.Dropped,
                UncompressedPixels = data,
                Width = width,
            });
        }

        map.MinCol = 0;
        map.MaxCol = (uint)(width - 1);
        map.MinRow = 0;
        map.MaxRow = (uint)(height - 1);
        map.HasRobeChannel = hasRobe ? RobeChannel.Present : RobeChannel.Dropped;
        map.ScanLines = lines;
    }

    /// <summary>
    /// Imports a 24-bit uncompressed BMP, as produced by <see cref="ToBitmap"/>, as the skin image.
    /// Rows are taken in file order (the order <see cref="ToBitmap"/> writes), whether the height is positive or negative.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a 24-bit uncompressed BMP.</exception>
    public static void ImportFromBitmap(DeformerMapResource map, ReadOnlySpan<byte> bmp)
    {
        if (bmp.Length < 54 || BinaryPrimitives.ReadUInt16LittleEndian(bmp) != 0x4D42)
            throw new InvalidDataException("Not a BMP file.");

        int dataOffset = BinaryPrimitives.ReadInt32LittleEndian(bmp[10..]);
        int width = BinaryPrimitives.ReadInt32LittleEndian(bmp[18..]);
        int height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bmp[22..]));
        int bits = BinaryPrimitives.ReadUInt16LittleEndian(bmp[28..]);
        int compression = BinaryPrimitives.ReadInt32LittleEndian(bmp[30..]);

        if (bits != 24 || compression != 0)
            throw new InvalidDataException("Only 24-bit uncompressed BMP files are supported.");
        if (width < 1 || height < 1 || width > MaxImageDimension || height > MaxImageDimension)
            throw new InvalidDataException("Invalid BMP dimensions.");

        int bytesPerLine = width * 3;
        int stride = (bytesPerLine + 3) & ~3;
        if (dataOffset < 54 || (long)dataOffset + ((long)stride * height) - (stride - bytesPerLine) > bmp.Length)
            throw new InvalidDataException("BMP pixel data is truncated.");

        var pixels = new byte[width * height * 3];
        for (int row = 0; row < height; row++)
            bmp.Slice(dataOffset + (row * stride), bytesPerLine).CopyTo(pixels.AsSpan(row * bytesPerLine));

        ImportFromImage(map, pixels, default, width, height);
    }
}
