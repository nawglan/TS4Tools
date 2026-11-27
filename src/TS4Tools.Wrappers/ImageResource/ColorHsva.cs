// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/RGBHSV.cs lines 28-267

namespace TS4Tools.Wrappers;

/// <summary>
/// A colour in hue/saturation/value/alpha form, each channel stored as a byte (0-255).
/// </summary>
/// <remarks>
/// Source: RGBHSV.cs lines 33-247 (struct ColorHSVA). Floats range 0.0-1.0 and bytes 0-255,
/// always scaling by 255 as the legacy comment states.
/// </remarks>
public readonly record struct ColorHsva(byte H, byte S, byte V, byte A)
{
    /// <summary>
    /// Converts to a packed ARGB value (alpha in the high byte).
    /// Source: RGBHSV.cs lines 54-84 (ToARGB).
    /// </summary>
    public uint ToArgb()
    {
        float r = 0, g = 0, b = 0;
        float h = H / 255.0f;
        float s = S / 255.0f;
        float v = V / 255.0f;

        if (s == 0)
        {
            r = g = b = v; // grey
        }
        else
        {
            h *= 6; // bring hue to a number between 0 and 6
            int i = (int)MathF.Floor(h);
            float f = h - i;
            float p = v * (1 - s);
            float q = v * (1 - (s * f));
            float t = v * (1 - (s * (1 - f)));
            switch (i)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
                default: break; // h == 1.0 exactly (i == 6) leaves black, as legacy does
            }
        }

        return ((uint)A << 24)
            | ((uint)MathF.Round(r * 255.0f) << 16)
            | ((uint)MathF.Round(g * 255.0f) << 8)
            | (uint)MathF.Round(b * 255.0f);
    }

    /// <summary>
    /// Converts a packed ARGB value to HSVA.
    /// Source: RGBHSV.cs lines 91-122 (explicit operator ColorHSVA).
    /// </summary>
    public static ColorHsva FromArgb(uint argb)
    {
        float r = ((argb >> 16) & 0xFF) / 255.0f;
        float g = ((argb >> 8) & 0xFF) / 255.0f;
        float b = (argb & 0xFF) / 255.0f;

        float maxColor = MathF.Max(r, MathF.Max(g, b));
        float minColor = MathF.Min(r, MathF.Min(g, b));
        float v = maxColor;

        float s = v == 0 ? 0 : (maxColor - minColor) / maxColor;
        float h;
        if (s == 0)
        {
            h = 0;
        }
        else
        {
            if (r == maxColor) h = (g - b) / (maxColor - minColor);
            else if (g == maxColor) h = 2.0f + ((b - r) / (maxColor - minColor));
            else h = 4.0f + ((r - g) / (maxColor - minColor));
            h /= 6.0f; // bring it to a number between 0 and 1
            if (h < 0) h++;
        }

        return new ColorHsva(
            (byte)Math.Round(h * 255.0),
            (byte)Math.Round(s * 255.0),
            (byte)Math.Round(v * 255.0),
            (byte)(argb >> 24));
    }

    /// <summary>
    /// Returns this colour adjusted by an <see cref="HsvShift"/>.
    /// Source: RGBHSV.cs lines 160-171 (HSVShift method). Hue wraps; saturation and value clamp to 0-1.
    /// </summary>
    public ColorHsva Shift(HsvShift shift)
    {
        float h = (H / 255.0f) + shift.H;
        if (h < 0) h += MathF.Floor(MathF.Abs(h)) + 1;
        if (h > 1) h -= MathF.Floor(MathF.Abs(h)) + 1;
        return new ColorHsva(
            (byte)MathF.Round(h * 255.0f),
            (byte)MathF.Round(Math.Max(0, Math.Min(1, (S / 255.0f) + shift.S)) * 255.0f),
            (byte)MathF.Round(Math.Max(0, Math.Min(1, (V / 255.0f) + shift.V)) * 255.0f),
            A);
    }

    /// <summary>
    /// Converts an array of ARGB values to HSVA.
    /// Source: RGBHSV.cs lines 178-184 (ToArrayColorHSVA).
    /// </summary>
    public static ColorHsva[] FromArgbArray(ReadOnlySpan<uint> argb)
    {
        var result = new ColorHsva[argb.Length];
        for (int i = 0; i < argb.Length; i++)
            result[i] = FromArgb(argb[i]);
        return result;
    }

    /// <summary>
    /// Converts an HSVA image back to ARGB, optionally applying a shift first.
    /// Source: RGBHSV.cs lines 192-198 (ToArrayARGB).
    /// </summary>
    public static uint[] ToArgbArray(ReadOnlySpan<ColorHsva> hsva, HsvShift shift = default)
    {
        var result = new uint[hsva.Length];
        for (int i = 0; i < hsva.Length; i++)
            result[i] = shift.IsEmpty ? hsva[i].ToArgb() : hsva[i].Shift(shift).ToArgb();
        return result;
    }
}

/// <summary>
/// Describes a hue, saturation and value shift to apply to an image.
/// </summary>
/// <remarks>Source: RGBHSV.cs lines 204-267 (struct HSVShift). Values are floats in the 0.0-1.0 domain.</remarks>
public readonly record struct HsvShift(float H, float S, float V)
{
    /// <summary>True when the shift is zero in every channel (legacy's IsEmpty).</summary>
    public bool IsEmpty => H == 0 && S == 0 && V == 0;

    /// <summary>An empty (no-op) shift.</summary>
    public static HsvShift Empty => default;
}
