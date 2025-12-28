namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A fixed set of 9 TGI references used in color catalog resources.
/// Source: CatalogCommon.cs lines 1108-1265 (Gp9references class)
/// </summary>
public sealed class Gp9References
{
    /// <summary>
    /// The size of a serialized Gp9References structure in bytes.
    /// </summary>
    public const int SerializedSize = TgiReference.SerializedSize * 9; // 144 bytes

    /// <summary>
    /// Reference 1.
    /// </summary>
    public TgiReference Ref01 { get; set; }

    /// <summary>
    /// Reference 2.
    /// </summary>
    public TgiReference Ref02 { get; set; }

    /// <summary>
    /// Reference 3.
    /// </summary>
    public TgiReference Ref03 { get; set; }

    /// <summary>
    /// Reference 4.
    /// </summary>
    public TgiReference Ref04 { get; set; }

    /// <summary>
    /// Reference 5.
    /// </summary>
    public TgiReference Ref05 { get; set; }

    /// <summary>
    /// Reference 6.
    /// </summary>
    public TgiReference Ref06 { get; set; }

    /// <summary>
    /// Reference 7.
    /// </summary>
    public TgiReference Ref07 { get; set; }

    /// <summary>
    /// Reference 8.
    /// </summary>
    public TgiReference Ref08 { get; set; }

    /// <summary>
    /// Reference 9.
    /// </summary>
    public TgiReference Ref09 { get; set; }

    /// <summary>
    /// Creates an empty Gp9References with all empty references.
    /// </summary>
    public Gp9References()
    {
        Ref01 = TgiReference.Empty;
        Ref02 = TgiReference.Empty;
        Ref03 = TgiReference.Empty;
        Ref04 = TgiReference.Empty;
        Ref05 = TgiReference.Empty;
        Ref06 = TgiReference.Empty;
        Ref07 = TgiReference.Empty;
        Ref08 = TgiReference.Empty;
        Ref09 = TgiReference.Empty;
    }

    /// <summary>
    /// Parses Gp9References from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the references.</param>
    /// <returns>The parsed references.</returns>
    public static Gp9References Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        var refs = new Gp9References();

        refs.Ref01 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref02 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref03 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref04 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref05 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref06 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref07 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref08 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref09 = TgiReference.Parse(data[offset..]);

        return refs;
    }

    /// <summary>
    /// Writes the references to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    public void WriteTo(Span<byte> buffer)
    {
        int offset = 0;

        Ref01.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref02.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref03.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref04.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref05.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref06.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref07.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref08.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        Ref09.WriteTo(buffer[offset..]);
    }
}
