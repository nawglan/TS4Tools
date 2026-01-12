using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// RLE-compressed DXT5 texture resource.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs
/// Type IDs: 0x3453CF95, 0xBA856C78
/// </summary>
/// <remarks>
/// RLE textures are DXT5-compressed textures with additional run-length encoding
/// for transparent and opaque pixel blocks. Two versions exist:
/// - RLE2 (0x32454C52): Standard format with 5 data streams per mip level
/// - RLES (0x53454C52): Extended format with 6 data streams (includes specular)
/// </remarks>
public sealed class RleResource : TypedResource
{
    /// <summary>Type ID for RLE texture resources (primary).</summary>
    public const uint TypeId = 0x3453CF95;

    /// <summary>Type ID for RLE texture resources (alternate).</summary>
    public const uint TypeIdAlternate = 0xBA856C78;

    /// <summary>DXT5 FourCC signature.</summary>
    public const uint FourCcDxt5 = 0x35545844; // "DXT5"

    /// <summary>RLE2 version signature.</summary>
    public const uint FourCcRle2 = 0x32454C52; // "RLE2"

    /// <summary>RLES version signature.</summary>
    public const uint FourCcRles = 0x53454C52; // "RLES"

    /// <summary>DDS file signature.</summary>
    public const uint DdsSignature = 0x20534444; // "DDS "

    private byte[] _rawData = [];

    /// <summary>RLE format version.</summary>
    public RleVersion Version { get; private set; } = RleVersion.Rle2;

    /// <summary>Image width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>Number of mipmap levels.</summary>
    public int MipCount { get; private set; }

    /// <summary>Mipmap headers containing data stream offsets.</summary>
    public List<MipHeader> MipHeaders { get; private set; } = [];

    /// <summary>Gets the raw RLE data.</summary>
    public ReadOnlyMemory<byte> RawData => _rawData;

    /// <inheritdoc/>
    public RleResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        _rawData = data.ToArray();

        if (data.Length < 16)
        {
            return;
        }

        // Header: FourCC (DXT5) + Version (RLE2/RLES) + Width (2) + Height (2) + MipCount (2) + Reserved (2)
        uint fourCc = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (fourCc != FourCcDxt5)
        {
            // Not a valid RLE resource
            return;
        }

        uint versionFourCc = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        Version = versionFourCc switch
        {
            FourCcRle2 => RleVersion.Rle2,
            FourCcRles => RleVersion.Rles,
            _ => RleVersion.Rle2 // Default to RLE2 if unknown
        };

        Width = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        Height = BinaryPrimitives.ReadUInt16LittleEndian(data[10..]);
        MipCount = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
        // Reserved: data[14..16]

        if (MipCount < 0 || MipCount > 16)
        {
            throw new InvalidDataException($"Invalid mip count: {MipCount}");
        }

        // Parse mip headers
        // RLE2: 5 offsets (20 bytes) per mip
        // RLES: 6 offsets (24 bytes) per mip
        int headerSize = Version == RleVersion.Rles ? 24 : 20;
        int headerOffset = 16;

        MipHeaders.Clear();

        for (int i = 0; i < MipCount; i++)
        {
            if (headerOffset + headerSize > data.Length)
                break;

            var header = new MipHeader
            {
                CommandOffset = BinaryPrimitives.ReadInt32LittleEndian(data[headerOffset..]),
                Offset2 = BinaryPrimitives.ReadInt32LittleEndian(data[(headerOffset + 4)..]),
                Offset3 = BinaryPrimitives.ReadInt32LittleEndian(data[(headerOffset + 8)..]),
                Offset0 = BinaryPrimitives.ReadInt32LittleEndian(data[(headerOffset + 12)..]),
                Offset1 = BinaryPrimitives.ReadInt32LittleEndian(data[(headerOffset + 16)..])
            };

            if (Version == RleVersion.Rles)
            {
                header.Offset4 = BinaryPrimitives.ReadInt32LittleEndian(data[(headerOffset + 20)..]);
            }

            MipHeaders.Add(header);
            headerOffset += headerSize;
        }

        // Add sentinel header for bounds calculations
        if (MipHeaders.Count > 0)
        {
            var first = MipHeaders[0];
            var sentinel = new MipHeader
            {
                CommandOffset = first.Offset2,
                Offset2 = first.Offset3,
                Offset3 = first.Offset0,
                Offset0 = first.Offset1
            };

            if (Version == RleVersion.Rles)
            {
                sentinel.Offset1 = first.Offset4;
                sentinel.Offset4 = data.Length;
            }
            else
            {
                sentinel.Offset1 = data.Length;
            }

            MipHeaders.Add(sentinel);
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        return _rawData;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        Version = RleVersion.Rle2;
        Width = 0;
        Height = 0;
        MipCount = 0;
        MipHeaders.Clear();
    }

    /// <summary>
    /// Decompresses the RLE texture to standard DDS format.
    /// </summary>
    /// <returns>DDS file data, or null if decompression fails.</returns>
    public byte[]? ToDds()
    {
        if (_rawData.Length == 0 || MipHeaders.Count < 2)
            return null;

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Write DDS header
        writer.Write(DdsSignature);
        WriteDdsHeader(writer);

        // Constant block patterns
        Span<byte> fullTransparentAlpha = stackalloc byte[] { 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Span<byte> fullOpaqueAlpha = stackalloc byte[] { 0x00, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        // Decompress each mip level
        for (int i = 0; i < MipCount && i < MipHeaders.Count - 1; i++)
        {
            var mipHeader = MipHeaders[i];
            var nextMipHeader = MipHeaders[i + 1];

            int blockOffset2 = mipHeader.Offset2;
            int blockOffset3 = mipHeader.Offset3;
            int blockOffset0 = mipHeader.Offset0;
            int blockOffset1 = mipHeader.Offset1;
            int blockOffset4 = mipHeader.Offset4;

            for (int commandOffset = mipHeader.CommandOffset;
                commandOffset < nextMipHeader.CommandOffset;
                commandOffset += 2)
            {
                if (commandOffset + 2 > _rawData.Length)
                    break;

                ushort command = BinaryPrimitives.ReadUInt16LittleEndian(_rawData.AsSpan(commandOffset));

                int op = command & 3;
                int count = command >> 2;

                if (op == 0)
                {
                    // Fully transparent blocks
                    for (int j = 0; j < count; j++)
                    {
                        output.Write(fullTransparentAlpha);
                        output.Write(fullTransparentAlpha);
                    }
                }
                else if (op == 1)
                {
                    // Translucent blocks (alpha + color)
                    for (int j = 0; j < count; j++)
                    {
                        if (blockOffset0 + 2 > _rawData.Length ||
                            blockOffset1 + 6 > _rawData.Length ||
                            blockOffset2 + 4 > _rawData.Length ||
                            blockOffset3 + 4 > _rawData.Length)
                            break;

                        writer.Write(_rawData, blockOffset0, 2);
                        writer.Write(_rawData, blockOffset1, 6);
                        writer.Write(_rawData, blockOffset2, 4);
                        writer.Write(_rawData, blockOffset3, 4);

                        blockOffset0 += 2;
                        blockOffset1 += 6;
                        blockOffset2 += 4;
                        blockOffset3 += 4;

                        if (Version == RleVersion.Rles)
                        {
                            blockOffset4 += 16;
                        }
                    }
                }
                else if (op == 2)
                {
                    // Fully opaque blocks (no alpha data, just color)
                    for (int j = 0; j < count; j++)
                    {
                        if (Version == RleVersion.Rles)
                        {
                            // RLES: Still reads from all streams
                            if (blockOffset0 + 2 > _rawData.Length ||
                                blockOffset1 + 6 > _rawData.Length ||
                                blockOffset2 + 4 > _rawData.Length ||
                                blockOffset3 + 4 > _rawData.Length)
                                break;

                            writer.Write(_rawData, blockOffset0, 2);
                            writer.Write(_rawData, blockOffset1, 6);
                            writer.Write(_rawData, blockOffset2, 4);
                            writer.Write(_rawData, blockOffset3, 4);

                            blockOffset0 += 2;
                            blockOffset1 += 6;
                            blockOffset2 += 4;
                            blockOffset3 += 4;
                        }
                        else
                        {
                            // RLE2: Write opaque alpha, then color
                            if (blockOffset2 + 4 > _rawData.Length ||
                                blockOffset3 + 4 > _rawData.Length)
                                break;

                            output.Write(fullOpaqueAlpha);
                            writer.Write(_rawData, blockOffset2, 4);
                            writer.Write(_rawData, blockOffset3, 4);

                            blockOffset2 += 4;
                            blockOffset3 += 4;
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unsupported RLE operation: {op}");
                }
            }
        }

        return output.ToArray();
    }

    private void WriteDdsHeader(BinaryWriter writer)
    {
        // DDS_HEADER structure (124 bytes after signature)
        const uint headerSize = 124;
        const uint headerFlags = 0x00001007 | 0x00020000; // TEXTURE | MIPMAPCOUNT
        const uint pixelFormatSize = 32;
        const uint pixelFormatFlags = 0x00000004; // FourCC
        const uint surfaceFlags = 0x00001000 | 0x00000008; // TEXTURE | COMPLEX

        writer.Write(headerSize);
        writer.Write(headerFlags);
        writer.Write(Height);
        writer.Write(Width);

        // Calculate pitch/linear size for DXT5
        int blockWidth = Math.Max(1, (Width + 3) / 4);
        int blockHeight = Math.Max(1, (Height + 3) / 4);
        uint linearSize = (uint)(blockWidth * blockHeight * 16); // 16 bytes per DXT5 block

        writer.Write(linearSize);
        writer.Write(1); // Depth
        writer.Write(MipCount);

        // Reserved1[11]
        for (int i = 0; i < 11; i++)
            writer.Write(0);

        // PixelFormat structure
        writer.Write(pixelFormatSize);
        writer.Write(pixelFormatFlags);
        writer.Write(FourCcDxt5); // DXT5
        writer.Write(0); // RGBBitCount
        writer.Write(0); // RBitMask
        writer.Write(0); // GBitMask
        writer.Write(0); // BBitMask
        writer.Write(0); // ABitMask

        // Caps
        writer.Write(surfaceFlags);
        writer.Write(0); // Caps2
        writer.Write(0); // Caps3
        writer.Write(0); // Caps4
        writer.Write(0); // Reserved2
    }

    /// <summary>
    /// Whether this resource contains specular data (RLES format).
    /// </summary>
    public bool HasSpecular => Version == RleVersion.Rles;
}

/// <summary>
/// RLE format version.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs lines 922-926
/// </summary>
public enum RleVersion : uint
{
    /// <summary>Standard RLE2 format with 5 data streams.</summary>
    Rle2 = 0x32454C52,

    /// <summary>Extended RLES format with specular data (6 data streams).</summary>
    Rles = 0x53454C52
}

/// <summary>
/// Mipmap header containing data stream offsets.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs lines 825-833
/// </summary>
public sealed class MipHeader
{
    /// <summary>Offset to command data.</summary>
    public int CommandOffset { get; set; }

    /// <summary>Offset to alpha endpoint data.</summary>
    public int Offset0 { get; set; }

    /// <summary>Offset to alpha index data.</summary>
    public int Offset1 { get; set; }

    /// <summary>Offset to color endpoint data.</summary>
    public int Offset2 { get; set; }

    /// <summary>Offset to color index data.</summary>
    public int Offset3 { get; set; }

    /// <summary>Offset to specular data (RLES only).</summary>
    public int Offset4 { get; set; }
}
