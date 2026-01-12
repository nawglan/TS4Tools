using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Deformer Map Resource containing positional or normal deltas for sim deformation.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs
/// Type ID: 0xDB43E069
/// </summary>
public sealed class DeformerMapResource : TypedResource
{
    /// <summary>Type ID for Deformer Map resources.</summary>
    public const uint TypeId = 0xDB43E069;

    private const int MaxScanLines = 8192;
    private const int MaxPixelDataSize = 16 * 1024 * 1024; // 16 MB max

    /// <summary>Resource version.</summary>
    public uint Version { get; set; } = 1;

    /// <summary>Doubled width of the deformer map.</summary>
    public uint DoubledWidth { get; set; }

    /// <summary>Height of the deformer map.</summary>
    public uint Height { get; set; }

    /// <summary>Age and gender flags for this deformer.</summary>
    public AgeGenderFlags AgeGender { get; set; }

    /// <summary>Physique type this deformer applies to.</summary>
    public Physiques Physique { get; set; }

    /// <summary>Whether this contains shape or normal deformations.</summary>
    public ShapeOrNormals IsShapeOrNormals { get; set; }

    /// <summary>Minimum column index.</summary>
    public uint MinCol { get; set; }

    /// <summary>Maximum column index.</summary>
    public uint MaxCol { get; set; }

    /// <summary>Minimum row index.</summary>
    public uint MinRow { get; set; }

    /// <summary>Maximum row index.</summary>
    public uint MaxRow { get; set; }

    /// <summary>Robe channel presence flag.</summary>
    public RobeChannel HasRobeChannel { get; set; }

    /// <summary>Array of scan lines containing pixel data.</summary>
    public List<ScanLine> ScanLines { get; set; } = [];

    /// <inheritdoc/>
    public DeformerMapResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        if (data.Length < 27) // Minimum header size
            throw new InvalidDataException($"DeformerMapResource data too short: {data.Length} bytes");

        int offset = 0;
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        DoubledWidth = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        Height = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        AgeGender = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        Physique = (Physiques)data[offset++];
        IsShapeOrNormals = (ShapeOrNormals)data[offset++];
        MinCol = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        MaxCol = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        MinRow = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        MaxRow = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        HasRobeChannel = (RobeChannel)data[offset++];

        int totalBytes = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        ScanLines.Clear();

        if (totalBytes == 0)
        {
            return;
        }

        int width = (int)(MaxCol - MinCol + 1);
        if (width <= 0 || width > 8192)
            throw new InvalidDataException($"Invalid width: {width}");

        uint numScanLines = MaxRow - MinRow + 1;
        if (numScanLines > MaxScanLines)
            throw new InvalidDataException($"Too many scan lines: {numScanLines}");

        for (int i = 0; i < numScanLines; i++)
        {
            var scanLine = ScanLine.Parse(data, ref offset, width);
            ScanLines.Add(scanLine);
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = GetSerializedSize();
        var buffer = new byte[size];
        var span = buffer.AsSpan();

        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Version);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], DoubledWidth);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Height);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)AgeGender);
        offset += 4;
        span[offset++] = (byte)Physique;
        span[offset++] = (byte)IsShapeOrNormals;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MinCol);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MaxCol);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MinRow);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MaxRow);
        offset += 4;
        span[offset++] = (byte)HasRobeChannel;
        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], ScanLines.Count);
        offset += 4;

        foreach (var scanLine in ScanLines)
        {
            scanLine.Serialize(span, ref offset);
        }

        return buffer;
    }

    private int GetSerializedSize()
    {
        // Header: version(4) + doubledWidth(4) + height(4) + ageGender(4) + physique(1) + shapeOrNormals(1)
        //         + minCol(4) + maxCol(4) + minRow(4) + maxRow(4) + robeChannel(1) + totalBytes(4) = 39
        int size = 39;

        foreach (var scanLine in ScanLines)
        {
            size += scanLine.GetSerializedSize();
        }

        return size;
    }
}

/// <summary>
/// Physique types for body blend deformation.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 125-138
/// </summary>
public enum Physiques : byte
{
    /// <summary>Heavy body type.</summary>
    Heavy = 0,
    /// <summary>Fit body type.</summary>
    Fit = 1,
    /// <summary>Lean body type.</summary>
    Lean = 2,
    /// <summary>Bony body type.</summary>
    Bony = 3,
    /// <summary>Pregnant body type.</summary>
    Pregnant = 4,
    /// <summary>Wide hips body type.</summary>
    HipsWide = 5,
    /// <summary>Narrow hips body type.</summary>
    HipsNarrow = 6,
    /// <summary>Wide waist body type.</summary>
    WaistWide = 7,
    /// <summary>Narrow waist body type.</summary>
    WaistNarrow = 8,
    /// <summary>Used for sculpts/modifiers, not a physique.</summary>
    Ignore = 9,
    /// <summary>Average deformation map applied for a given age.</summary>
    Average = 100
}

/// <summary>
/// Whether the deformer contains shape or normal deltas.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 140-144
/// </summary>
public enum ShapeOrNormals : byte
{
    /// <summary>Contains positional deltas.</summary>
    Shape = 0,
    /// <summary>Contains normal deltas.</summary>
    Normals = 1
}

/// <summary>
/// Robe channel presence in deformer map.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 149-154
/// </summary>
public enum RobeChannel : byte
{
    /// <summary>Robe channel is present.</summary>
    Present = 0,
    /// <summary>Robe channel is dropped.</summary>
    Dropped = 1,
    /// <summary>Robe data not present but is same as skin tight data.</summary>
    IsCopy = 2
}

/// <summary>
/// A single scan line in a deformer map, containing either compressed or uncompressed pixel data.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 156-286
/// </summary>
public sealed class ScanLine
{
    /// <summary>Size of the scan line data in bytes.</summary>
    public ushort ScanLineDataSize { get; set; }

    /// <summary>Whether the scan line data is RLE compressed.</summary>
    public bool IsCompressed { get; set; }

    /// <summary>Robe channel status for this scan line.</summary>
    public RobeChannel RobeChannel { get; set; }

    /// <summary>Uncompressed pixel data (only valid if not compressed).</summary>
    public byte[] UncompressedPixels { get; set; } = [];

    /// <summary>Number of index entries for RLE decoding.</summary>
    public byte NumIndexes { get; set; }

    /// <summary>Pixel position indexes for RLE decoding.</summary>
    public ushort[] PixelPosIndexes { get; set; } = [];

    /// <summary>Data position indexes for RLE decoding.</summary>
    public ushort[] DataPosIndexes { get; set; } = [];

    /// <summary>RLE compressed pixel array.</summary>
    public byte[] RleArrayOfPixels { get; set; } = [];

    /// <summary>Width of the scan line in pixels (used for parsing).</summary>
    internal int Width { get; set; }

    /// <summary>
    /// Parses a scan line from binary data.
    /// </summary>
    public static ScanLine Parse(ReadOnlySpan<byte> data, ref int offset, int width)
    {
        var scanLine = new ScanLine { Width = width };

        if (offset + 4 > data.Length)
            throw new InvalidDataException("Unexpected end of data while parsing scan line header");

        scanLine.ScanLineDataSize = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;
        scanLine.IsCompressed = data[offset++] != 0;
        scanLine.RobeChannel = (RobeChannel)data[offset++];

        if (!scanLine.IsCompressed)
        {
            // Uncompressed: 6 bytes per pixel if robe present, 3 bytes otherwise
            int pixelSize = scanLine.RobeChannel == RobeChannel.Present ? 6 : 3;
            int dataSize = width * pixelSize;

            if (offset + dataSize > data.Length)
                throw new InvalidDataException("Unexpected end of data while parsing uncompressed pixels");

            scanLine.UncompressedPixels = data.Slice(offset, dataSize).ToArray();
            offset += dataSize;
        }
        else
        {
            // Compressed RLE data
            if (offset + 1 > data.Length)
                throw new InvalidDataException("Unexpected end of data while parsing compressed scan line");

            scanLine.NumIndexes = data[offset++];

            if (offset + (scanLine.NumIndexes * 4) > data.Length)
                throw new InvalidDataException("Unexpected end of data while parsing scan line indexes");

            scanLine.PixelPosIndexes = new ushort[scanLine.NumIndexes];
            scanLine.DataPosIndexes = new ushort[scanLine.NumIndexes];

            for (int i = 0; i < scanLine.NumIndexes; i++)
            {
                scanLine.PixelPosIndexes[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
                offset += 2;
            }

            for (int i = 0; i < scanLine.NumIndexes; i++)
            {
                scanLine.DataPosIndexes[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
                offset += 2;
            }

            // Calculate RLE data size: total - header (4) - numIndexes (1) - indexes (4 * numIndexes)
            uint headerDataSize = 4U + 1U + (4U * scanLine.NumIndexes);
            int rleDataSize = scanLine.ScanLineDataSize - (int)headerDataSize;

            if (rleDataSize < 0)
                throw new InvalidDataException("Invalid scan line data size");

            if (offset + rleDataSize > data.Length)
                throw new InvalidDataException("Unexpected end of data while parsing RLE pixels");

            scanLine.RleArrayOfPixels = data.Slice(offset, rleDataSize).ToArray();
            offset += rleDataSize;
        }

        return scanLine;
    }

    /// <summary>
    /// Serializes this scan line to binary data.
    /// </summary>
    public void Serialize(Span<byte> buffer, ref int offset)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], ScanLineDataSize);
        offset += 2;
        buffer[offset++] = IsCompressed ? (byte)1 : (byte)0;
        buffer[offset++] = (byte)RobeChannel;

        if (!IsCompressed)
        {
            UncompressedPixels.CopyTo(buffer[offset..]);
            offset += UncompressedPixels.Length;
        }
        else
        {
            buffer[offset++] = NumIndexes;

            for (int i = 0; i < NumIndexes; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], PixelPosIndexes[i]);
                offset += 2;
            }

            for (int i = 0; i < NumIndexes; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], DataPosIndexes[i]);
                offset += 2;
            }

            RleArrayOfPixels.CopyTo(buffer[offset..]);
            offset += RleArrayOfPixels.Length;
        }
    }

    /// <summary>
    /// Gets the serialized size of this scan line.
    /// </summary>
    public int GetSerializedSize()
    {
        // Header: size(2) + isCompressed(1) + robeChannel(1) = 4
        int size = 4;

        if (!IsCompressed)
        {
            size += UncompressedPixels.Length;
        }
        else
        {
            // numIndexes(1) + pixelPosIndexes(2*n) + dataPosIndexes(2*n) + rleData
            size += 1 + (NumIndexes * 4) + RleArrayOfPixels.Length;
        }

        return size;
    }
}
