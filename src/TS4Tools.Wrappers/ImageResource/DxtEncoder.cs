// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsSquish.cs lines 28-468 (CompressImage and SquishFlags)

namespace TS4Tools.Wrappers;

/// <summary>
/// Managed DXT1/DXT3/DXT5 block compressor.
/// </summary>
/// <remarks>
/// Source: DdsSquish.cs lines 82-95 (CompressImage). The legacy code P/Invokes the native
/// <c>squishinterface_*.dll</c> (libsquish), which this port cannot call cross-platform.
///
/// DEVIATION (documented): libsquish's default colour fit is "cluster fit" (an iterative search for the best
/// two end-points). This port implements libsquish's <c>kColourRangeFit</c> instead: the end-points are
/// the extreme points along the principal axis of the 4x4 block colours, snapped to the RGB565 grid, with
/// each pixel then matched to the closest code in the 4 (or 3) colour palette. Range fit is a documented libsquish
/// mode and is deterministic, but yields slightly higher error than cluster fit, so the compressed bytes
/// are not bit-identical to what legacy produced. The power iteration that finds the principal axis starts from a covariance
/// row rather than (1,1,1) so that symmetric colour pairs (for example pure red and pure blue) keep their axis. Other behaviour follows libsquish:
/// perceptual colour metric (0.2126, 0.7152, 0.0722), DXT1 pixels with alpha below 128 become
/// transparent (3-colour mode), DXT3 alpha is rounded to 4 bits, DXT5 alpha tries the 5-alpha and 7-alpha
/// layouts and keeps the smaller error, edge blocks replicate border pixels.
/// </remarks>
public static class DxtEncoder
{
    private static readonly (float R, float G, float B) Metric = (0.2126f, 0.7152f, 0.0722f);

    /// <summary>
    /// Compresses RGBA32 pixels (R, G, B, A byte order) to DXT block data (no DDS header).
    /// </summary>
    /// <param name="rgba">Pixel data, 4 bytes per pixel, row-major, <paramref name="width"/> x <paramref name="height"/>.</param>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    /// <param name="format">Must be <see cref="DdsFormat.Dxt1"/>, <see cref="DdsFormat.Dxt3"/> or <see cref="DdsFormat.Dxt5"/>.</param>
    /// <returns>The compressed blocks, row-major (blocksX * blocksY * block size bytes).</returns>
    /// <exception cref="ArgumentException">The format is not a DXT format, or the buffer is too small.</exception>
    public static byte[] Compress(ReadOnlySpan<byte> rgba, int width, int height, DdsFormat format)
    {
        if (format is not (DdsFormat.Dxt1 or DdsFormat.Dxt3 or DdsFormat.Dxt5))
            throw new ArgumentException("Only DXT1, DXT3 and DXT5 are block compressed.", nameof(format));
        if (width <= 0 || height <= 0 || width > DdsHeader.MaxDimension || height > DdsHeader.MaxDimension)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid image dimensions.");
        if (rgba.Length < (long)width * height * 4)
            throw new ArgumentException("Pixel buffer is smaller than width * height * 4.", nameof(rgba));

        int blockSize = format == DdsFormat.Dxt1 ? 8 : 16;
        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        var output = new byte[blocksX * blocksY * blockSize];

        Span<byte> block = stackalloc byte[64];
        int outOffset = 0;
        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                // Gather the 4x4 block, replicating edge pixels (libsquish CompressImage).
                for (int py = 0; py < 4; py++)
                {
                    int sy = Math.Min((by * 4) + py, height - 1);
                    for (int px = 0; px < 4; px++)
                    {
                        int sx = Math.Min((bx * 4) + px, width - 1);
                        rgba.Slice(((sy * width) + sx) * 4, 4).CopyTo(block[(((py * 4) + px) * 4)..]);
                    }
                }

                CompressBlock(block, output.AsSpan(outOffset, blockSize), format);
                outOffset += blockSize;
            }
        }

        return output;
    }

    /// <summary>Compresses a single 4x4 block (64 bytes RGBA) into <paramref name="target"/>.</summary>
    internal static void CompressBlock(ReadOnlySpan<byte> rgba, Span<byte> target, DdsFormat format)
    {
        int colourOffset = 0;
        if (format == DdsFormat.Dxt3)
        {
            CompressAlphaDxt3(rgba, target);
            colourOffset = 8;
        }
        else if (format == DdsFormat.Dxt5)
        {
            CompressAlphaDxt5(rgba, target);
            colourOffset = 8;
        }

        CompressColour(rgba, target[colourOffset..], format == DdsFormat.Dxt1);
    }

    // ------------------------------------------------------------------ alpha

    private static void CompressAlphaDxt3(ReadOnlySpan<byte> rgba, Span<byte> block)
    {
        for (int i = 0; i < 8; i++)
        {
            int lo = Quantise4(rgba[(2 * i * 4) + 3]);
            int hi = Quantise4(rgba[(((2 * i) + 1) * 4) + 3]);
            block[i] = (byte)(lo | (hi << 4));
        }
    }

    private static int Quantise4(byte alpha) => Math.Min(15, (int)MathF.Floor((alpha * (15.0f / 255.0f)) + 0.5f));

    private static void CompressAlphaDxt5(ReadOnlySpan<byte> rgba, Span<byte> block)
    {
        int min5 = 255, max5 = 0, min7 = 255, max7 = 0;
        for (int i = 0; i < 16; i++)
        {
            int v = rgba[(i * 4) + 3];
            min7 = Math.Min(min7, v);
            max7 = Math.Max(max7, v);
            if (v != 0) min5 = Math.Min(min5, v);
            if (v != 255) max5 = Math.Max(max5, v);
        }

        if (min5 > max5) min5 = max5;
        if (min7 > max7) min7 = max7;

        FixRange(ref min5, ref max5, 5);
        FixRange(ref min7, ref max7, 7);

        Span<int> codes5 = stackalloc int[8];
        codes5[0] = min5;
        codes5[1] = max5;
        for (int i = 1; i < 5; i++) codes5[1 + i] = (((5 - i) * min5) + (i * max5)) / 5;
        codes5[6] = 0;
        codes5[7] = 255;

        Span<int> codes7 = stackalloc int[8];
        codes7[0] = min7;
        codes7[1] = max7;
        for (int i = 1; i < 7; i++) codes7[1 + i] = (((7 - i) * min7) + (i * max7)) / 7;

        Span<byte> indices5 = stackalloc byte[16];
        Span<byte> indices7 = stackalloc byte[16];
        int err5 = FitCodes(rgba, codes5, indices5);
        int err7 = FitCodes(rgba, codes7, indices7);

        if (err5 <= err7)
            WriteAlphaBlock5(min5, max5, indices5, block);
        else
            WriteAlphaBlock7(min7, max7, indices7, block);
    }

    private static void FixRange(ref int min, ref int max, int steps)
    {
        if (max - min < steps)
            max = Math.Min(min + steps, 255);
        if (max - min < steps)
            min = Math.Max(0, max - steps);
    }

    private static int FitCodes(ReadOnlySpan<byte> rgba, ReadOnlySpan<int> codes, Span<byte> indices)
    {
        int err = 0;
        for (int i = 0; i < 16; i++)
        {
            int value = rgba[(i * 4) + 3];
            int least = int.MaxValue;
            int index = 0;
            for (int j = 0; j < 8; j++)
            {
                int dist = value - codes[j];
                dist *= dist;
                if (dist < least)
                {
                    least = dist;
                    index = j;
                }
            }

            indices[i] = (byte)index;
            err += least;
        }

        return err;
    }

    private static void WriteAlphaBlock5(int alpha0, int alpha1, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        if (alpha0 > alpha1)
        {
            Span<byte> swapped = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
            {
                byte index = indices[i];
                swapped[i] = index == 0 ? (byte)1 : index == 1 ? (byte)0 : index <= 5 ? (byte)(7 - index) : index;
            }

            WriteAlphaBlock(alpha1, alpha0, swapped, block);
        }
        else
        {
            WriteAlphaBlock(alpha0, alpha1, indices, block);
        }
    }

    private static void WriteAlphaBlock7(int alpha0, int alpha1, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        if (alpha0 < alpha1)
        {
            Span<byte> swapped = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
            {
                byte index = indices[i];
                swapped[i] = index == 0 ? (byte)1 : index == 1 ? (byte)0 : (byte)(9 - index);
            }

            WriteAlphaBlock(alpha1, alpha0, swapped, block);
        }
        else
        {
            WriteAlphaBlock(alpha0, alpha1, indices, block);
        }
    }

    private static void WriteAlphaBlock(int alpha0, int alpha1, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        block[0] = (byte)alpha0;
        block[1] = (byte)alpha1;
        for (int i = 0; i < 2; i++)
        {
            int value = 0;
            for (int j = 7; j >= 0; j--)
                value = (value << 3) | indices[(i * 8) + j];
            block[2 + (i * 3)] = (byte)(value & 0xFF);
            block[3 + (i * 3)] = (byte)((value >> 8) & 0xFF);
            block[4 + (i * 3)] = (byte)((value >> 16) & 0xFF);
        }
    }

    // ------------------------------------------------------------------ colour (range fit)

    private static void CompressColour(ReadOnlySpan<byte> rgba, Span<byte> block, bool isDxt1)
    {
        // ColourSet: unique opaque colours (DXT1 treats alpha < 128 as transparent)
        Span<float> points = stackalloc float[16 * 3];
        Span<int> remap = stackalloc int[16];
        int count = 0;
        bool transparent = false;

        for (int i = 0; i < 16; i++)
        {
            if (isDxt1 && rgba[(i * 4) + 3] < 128)
            {
                remap[i] = -1;
                transparent = true;
                continue;
            }

            int match = -1;
            for (int j = 0; j < i; j++)
            {
                if (remap[j] < 0)
                    continue;
                if (rgba[i * 4] == rgba[j * 4] && rgba[(i * 4) + 1] == rgba[(j * 4) + 1] && rgba[(i * 4) + 2] == rgba[(j * 4) + 2])
                {
                    match = remap[j];
                    break;
                }
            }

            if (match >= 0)
            {
                remap[i] = match;
            }
            else
            {
                remap[i] = count;
                points[count * 3] = rgba[i * 4] / 255.0f;
                points[(count * 3) + 1] = rgba[(i * 4) + 1] / 255.0f;
                points[(count * 3) + 2] = rgba[(i * 4) + 2] / 255.0f;
                count++;
            }
        }

        // RangeFit: principal component of the (equally weighted) points
        var principle = PrincipleComponent(points, count);

        // Start/end are the extreme points along the principal axis
        (float R, float G, float B) start = (0, 0, 0), end = (0, 0, 0);
        if (count > 0)
        {
            start = end = (points[0], points[1], points[2]);
            float min = Dot(start, principle);
            float max = min;
            for (int i = 1; i < count; i++)
            {
                var p = (points[i * 3], points[(i * 3) + 1], points[(i * 3) + 2]);
                float d = Dot(p, principle);
                if (d < min)
                {
                    start = p;
                    min = d;
                }
                else if (d > max)
                {
                    end = p;
                    max = d;
                }
            }
        }

        // Snap to the 565 grid
        start = SnapToGrid(start);
        end = SnapToGrid(end);

        Span<byte> closest = stackalloc byte[16];
        Span<byte> indices = stackalloc byte[16];
        float bestError = float.MaxValue;
        Span<byte> best = stackalloc byte[8];
        bool haveBest = false;

        // 3-colour palette (DXT1 only)
        if (isDxt1)
        {
            Span<(float, float, float)> codes3 =
            [
                start,
                end,
                (((start.R) + end.R) * 0.5f, ((start.G) + end.G) * 0.5f, ((start.B) + end.B) * 0.5f),
            ];
            float err = MatchPoints(points, count, codes3, closest);
            RemapIndices(remap, closest, indices);
            bestError = err;
            WriteColourBlock3(start, end, indices, best);
            haveBest = true;
        }

        if (!isDxt1 || !transparent)
        {
            Span<(float, float, float)> codes4 =
            [
                start,
                end,
                (((2 * start.R) + end.R) / 3.0f, ((2 * start.G) + end.G) / 3.0f, ((2 * start.B) + end.B) / 3.0f),
                ((start.R + (2 * end.R)) / 3.0f, (start.G + (2 * end.G)) / 3.0f, (start.B + (2 * end.B)) / 3.0f),
            ];
            float err = MatchPoints(points, count, codes4, closest);
            if (!haveBest || err < bestError)
            {
                RemapIndices(remap, closest, indices);
                WriteColourBlock4(start, end, indices, best);
            }
        }

        best.CopyTo(block);
    }

    private static (float, float, float) SnapToGrid((float R, float G, float B) c)
    {
        float r = MathF.Truncate((Math.Clamp(c.R, 0f, 1f) * 31.0f) + 0.5f) / 31.0f;
        float g = MathF.Truncate((Math.Clamp(c.G, 0f, 1f) * 63.0f) + 0.5f) / 63.0f;
        float b = MathF.Truncate((Math.Clamp(c.B, 0f, 1f) * 31.0f) + 0.5f) / 31.0f;
        return (r, g, b);
    }

    private static float MatchPoints(ReadOnlySpan<float> points, int count, ReadOnlySpan<(float R, float G, float B)> codes, Span<byte> closest)
    {
        float error = 0;
        for (int i = 0; i < count; i++)
        {
            float bestDist = float.MaxValue;
            int bestIndex = 0;
            for (int j = 0; j < codes.Length; j++)
            {
                float dr = (points[i * 3] - codes[j].R) * Metric.R;
                float dg = (points[(i * 3) + 1] - codes[j].G) * Metric.G;
                float db = (points[(i * 3) + 2] - codes[j].B) * Metric.B;
                float dist = (dr * dr) + (dg * dg) + (db * db);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = j;
                }
            }

            closest[i] = (byte)bestIndex;
            error += bestDist;
        }

        return error;
    }

    private static void RemapIndices(ReadOnlySpan<int> remap, ReadOnlySpan<byte> source, Span<byte> target)
    {
        for (int i = 0; i < 16; i++)
        {
            int j = remap[i];
            target[i] = j < 0 ? (byte)3 : source[j];
        }
    }

    private static int To565((float R, float G, float B) c)
    {
        int r = Math.Min(31, (int)((c.R * 31.0f) + 0.5f));
        int g = Math.Min(63, (int)((c.G * 63.0f) + 0.5f));
        int b = Math.Min(31, (int)((c.B * 31.0f) + 0.5f));
        return (r << 11) | (g << 5) | b;
    }

    private static void WriteColourBlock3((float R, float G, float B) start, (float R, float G, float B) end, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        int a = To565(start);
        int b = To565(end);

        Span<byte> remapped = stackalloc byte[16];
        if (a > b)
        {
            // 3-colour mode needs a <= b: swap and exchange indices 0 and 1
            (a, b) = (b, a);
            for (int i = 0; i < 16; i++)
                remapped[i] = indices[i] == 0 ? (byte)1 : indices[i] == 1 ? (byte)0 : indices[i];
        }
        else
        {
            indices.CopyTo(remapped);
        }

        WriteColourBlock(a, b, remapped, block);
    }

    private static void WriteColourBlock4((float R, float G, float B) start, (float R, float G, float B) end, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        int a = To565(start);
        int b = To565(end);

        Span<byte> remapped = stackalloc byte[16];
        if (a < b)
        {
            // 4-colour mode needs a > b: swap and exchange 0<->1, 2<->3 (indices ^ 1)
            (a, b) = (b, a);
            for (int i = 0; i < 16; i++)
                remapped[i] = (byte)((indices[i] ^ 0x1) & 0x3);
        }
        else if (a == b)
        {
            remapped.Clear();
        }
        else
        {
            indices.CopyTo(remapped);
        }

        WriteColourBlock(a, b, remapped, block);
    }

    private static void WriteColourBlock(int a, int b, ReadOnlySpan<byte> indices, Span<byte> block)
    {
        block[0] = (byte)(a & 0xFF);
        block[1] = (byte)(a >> 8);
        block[2] = (byte)(b & 0xFF);
        block[3] = (byte)(b >> 8);
        for (int i = 0; i < 4; i++)
        {
            byte packed = 0;
            for (int j = 0; j < 4; j++)
                packed |= (byte)(indices[(4 * i) + j] << (2 * j));
            block[4 + i] = packed;
        }
    }

    private static float Dot((float R, float G, float B) a, (float R, float G, float B) b) =>
        (a.R * b.R) + (a.G * b.G) + (a.B * b.B);

    /// <summary>
    /// Power-iteration principal component of the covariance (libsquish ComputePrincipleComponent, 8 iterations).
    /// </summary>
    private static (float R, float G, float B) PrincipleComponent(ReadOnlySpan<float> points, int count)
    {
        if (count == 0)
            return (1, 1, 1);

        float cr = 0, cg = 0, cb = 0;
        for (int i = 0; i < count; i++)
        {
            cr += points[i * 3];
            cg += points[(i * 3) + 1];
            cb += points[(i * 3) + 2];
        }

        cr /= count;
        cg /= count;
        cb /= count;

        float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        for (int i = 0; i < count; i++)
        {
            float a = points[i * 3] - cr;
            float b = points[(i * 3) + 1] - cg;
            float c = points[(i * 3) + 2] - cb;
            xx += a * a;
            xy += a * b;
            xz += a * c;
            yy += b * b;
            yz += b * c;
            zz += c * c;
        }

        // Deviation from libsquish's all-ones start vector: for symmetric data such as two opposed colours the
        // covariance maps (1,1,1) to zero and the axis is lost. Start from the covariance row with the largest
        // diagonal (always in the dominant eigenspace) instead, then power-iterate.
        float vx, vy, vz;
        if (xx >= yy && xx >= zz)
            (vx, vy, vz) = (xx, xy, xz);
        else if (yy >= zz)
            (vx, vy, vz) = (xy, yy, yz);
        else
            (vx, vy, vz) = (xz, yz, zz);

        if (vx == 0 && vy == 0 && vz == 0)
            return (1, 1, 1); // all points identical

        for (int i = 0; i < 8; i++)
        {
            float wx = (xx * vx) + (xy * vy) + (xz * vz);
            float wy = (xy * vx) + (yy * vy) + (yz * vz);
            float wz = (xz * vx) + (yz * vy) + (zz * vz);
            float m = Math.Abs(wx) >= Math.Abs(wy) && Math.Abs(wx) >= Math.Abs(wz) ? wx : Math.Abs(wy) >= Math.Abs(wz) ? wy : wz;
            if (m == 0)
                break;
            vx = wx / m;
            vy = wy / m;
            vz = wz / m;
        }

        return (vx, vy, vz);
    }
}
