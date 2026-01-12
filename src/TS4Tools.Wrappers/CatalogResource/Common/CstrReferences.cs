namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A fixed set of 6 TGI references used in stair catalog resources.
/// Source: CSTRResource.cs lines 16-115 (CSTR_references class)
/// </summary>
public sealed class CstrReferences
{
    /// <summary>
    /// The size of a serialized CstrReferences structure in bytes.
    /// </summary>
    public const int SerializedSize = TgiReference.SerializedSize * 6; // 96 bytes

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
    /// Creates an empty CstrReferences with all empty references.
    /// </summary>
    public CstrReferences()
    {
        Ref01 = TgiReference.Empty;
        Ref02 = TgiReference.Empty;
        Ref03 = TgiReference.Empty;
        Ref04 = TgiReference.Empty;
        Ref05 = TgiReference.Empty;
        Ref06 = TgiReference.Empty;
    }

    /// <summary>
    /// Parses CstrReferences from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the references.</param>
    /// <returns>The parsed references.</returns>
    public static CstrReferences Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        var refs = new CstrReferences();

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
    }
}
