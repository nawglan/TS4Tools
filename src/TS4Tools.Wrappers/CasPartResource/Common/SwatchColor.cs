
namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Represents an ARGB swatch color (4 bytes).
/// Source: legacy_references/.../Handlers/SwatchColor.cs
/// </summary>
public readonly record struct SwatchColor(int Argb)
{
    /// <summary>
    /// Size in bytes when serialized.
    /// </summary>
    public const int SerializedSize = 4;

    /// <summary>
    /// Alpha component (0-255).
    /// </summary>
    public byte A => (byte)((Argb >> 24) & 0xFF);

    /// <summary>
    /// Red component (0-255).
    /// </summary>
    public byte R => (byte)((Argb >> 16) & 0xFF);

    /// <summary>
    /// Green component (0-255).
    /// </summary>
    public byte G => (byte)((Argb >> 8) & 0xFF);

    /// <summary>
    /// Blue component (0-255).
    /// </summary>
    public byte B => (byte)(Argb & 0xFF);

    /// <summary>
    /// Creates a SwatchColor from ARGB components.
    /// </summary>
    public static SwatchColor FromArgb(byte a, byte r, byte g, byte b)
    {
        return new SwatchColor((a << 24) | (r << 16) | (g << 8) | b);
    }

    /// <summary>
    /// Parses a SwatchColor from a span.
    /// </summary>
    public static SwatchColor Parse(ReadOnlySpan<byte> data)
    {
        int argb = BinaryPrimitives.ReadInt32LittleEndian(data);
        return new SwatchColor(argb);
    }

    /// <summary>
    /// Writes this SwatchColor to a span.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Argb);
    }

    /// <summary>
    /// Gets the hex representation of the color (#AARRGGBB).
    /// </summary>
    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}
