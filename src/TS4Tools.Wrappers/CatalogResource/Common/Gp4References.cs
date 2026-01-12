namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A fixed set of 4 TGI references used in color catalog resources.
/// Source: CatalogCommon.cs lines 1024-1107 (Gp4references class)
/// </summary>
public sealed class Gp4References
{
    /// <summary>
    /// The size of a serialized Gp4References structure in bytes.
    /// </summary>
    public const int SerializedSize = TgiReference.SerializedSize * 4; // 64 bytes

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
    /// Creates an empty Gp4References with all empty references.
    /// </summary>
    public Gp4References()
    {
        Ref01 = TgiReference.Empty;
        Ref02 = TgiReference.Empty;
        Ref03 = TgiReference.Empty;
        Ref04 = TgiReference.Empty;
    }

    /// <summary>
    /// Parses Gp4References from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the references.</param>
    /// <returns>The parsed references.</returns>
    public static Gp4References Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        var refs = new Gp4References();

        refs.Ref01 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref02 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref03 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        refs.Ref04 = TgiReference.Parse(data[offset..]);

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
    }
}
