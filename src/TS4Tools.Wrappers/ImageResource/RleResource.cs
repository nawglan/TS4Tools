// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs lines 26-947
// Source: legacy_references/Sims4Tools/s4pe Helpers/RLEDDSHelper/Import.cs and RLESDDSHelper/ImportRLES.cs (DDS -> RLE2/RLES import)

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

            // Security: offsets come from the file and are used to index _rawData
            if (header.CommandOffset < 0 || header.Offset0 < 0 || header.Offset1 < 0 || header.Offset2 < 0
                || header.Offset3 < 0 || header.Offset4 < 0 || header.CommandOffset > data.Length
                || header.Offset0 > data.Length || header.Offset1 > data.Length || header.Offset2 > data.Length
                || header.Offset3 > data.Length || header.Offset4 > data.Length)
            {
                throw new InvalidDataException($"RLE mip header {i} has an offset outside the resource.");
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
    /// <returns>DDS file data, or null if the resource has no parsed header.</returns>
    /// <exception cref="InvalidDataException">The command or data streams are inconsistent (legacy throws NotSupportedException/InvalidOperationException).</exception>
    /// <remarks>
    /// Source: RLEResource.cs lines 105-293 (ToDDS). Branch comparison with legacy:
    /// RLE2 op1 = alpha0(2)+alpha1(6)+colour0(4)+colour1(4); op2 = fixed opaque alpha + colour0/colour1;
    /// RLES op1 = the same four streams and stream 4 advances 16 bytes (not copied); RLES op2 reads all four streams
    /// (stream 4 is NOT advanced). These match the legacy code. Differences fixed here: truncated data now throws
    /// instead of silently dropping blocks, and each mip's final stream offsets are checked against the next mip
    /// header (legacy lines 187-194 / 281-289) so corrupt files are rejected.
    /// </remarks>
    public byte[]? ToDds() => BuildDds(block4: false);

    /// <summary>
    /// Extracts the RLES specular stream (stream 4) as a DXT5 DDS.
    /// </summary>
    /// <returns>DDS file data, or null for RLE2 (no specular stream) or an unparsed resource.</returns>
    /// <remarks>
    /// Source: RLEResource.cs lines 295-398 (ToBlock4DDS). Translucent blocks output the 16 bytes of stream 4;
    /// transparent blocks output the transparent pattern; opaque blocks output the opaque pattern twice (fully opaque alpha
    /// and colour index bits 0xFF, as legacy does).
    /// </remarks>
    public byte[]? ToBlock4Dds() => Version == RleVersion.Rle2 ? null : BuildDds(block4: true);

    private byte[]? BuildDds(bool block4)
    {
        if (_rawData.Length == 0 || MipHeaders.Count < 2)
            return null;

        const long MaxOutput = 256L * 1024 * 1024;

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Write DDS header
        writer.Write(DdsSignature);
        WriteDdsHeader(writer);

        // Constant block patterns (legacy lines 112-115)
        ReadOnlySpan<byte> fullTransparentAlpha = [0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        ReadOnlySpan<byte> fullOpaqueAlpha = [0x00, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        bool rles = Version == RleVersion.Rles;

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

            if (mipHeader.CommandOffset < 0 || nextMipHeader.CommandOffset > _rawData.Length)
                throw new InvalidDataException("RLE command offset out of range.");

            for (int commandOffset = mipHeader.CommandOffset;
                commandOffset < nextMipHeader.CommandOffset;
                commandOffset += 2)
            {
                if (commandOffset + 2 > _rawData.Length)
                    throw new InvalidDataException("RLE command stream is truncated.");

                ushort command = BinaryPrimitives.ReadUInt16LittleEndian(_rawData.AsSpan(commandOffset));

                int op = command & 3;
                int count = command >> 2;

                if (output.Length + ((long)count * 16) > MaxOutput)
                    throw new InvalidDataException("RLE data expands beyond the supported maximum size.");

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
                    // Translucent blocks (alpha + colour; RLES also has 16 bytes of stream 4)
                    for (int j = 0; j < count; j++)
                    {
                        Require(blockOffset0, 2);
                        Require(blockOffset1, 6);
                        Require(blockOffset2, 4);
                        Require(blockOffset3, 4);

                        if (block4)
                        {
                            Require(blockOffset4, 16);
                            writer.Write(_rawData, blockOffset4, 16);
                        }
                        else
                        {
                            writer.Write(_rawData, blockOffset0, 2);
                            writer.Write(_rawData, blockOffset1, 6);
                            writer.Write(_rawData, blockOffset2, 4);
                            writer.Write(_rawData, blockOffset3, 4);
                        }

                        blockOffset0 += 2;
                        blockOffset1 += 6;
                        blockOffset2 += 4;
                        blockOffset3 += 4;

                        if (rles)
                            blockOffset4 += 16;
                    }
                }
                else if (op == 2)
                {
                    // Fully opaque blocks
                    for (int j = 0; j < count; j++)
                    {
                        if (rles)
                        {
                            // RLES: all four streams are consumed; stream 4 is not
                            Require(blockOffset0, 2);
                            Require(blockOffset1, 6);
                            Require(blockOffset2, 4);
                            Require(blockOffset3, 4);

                            if (block4)
                            {
                                output.Write(fullOpaqueAlpha);
                                output.Write(fullOpaqueAlpha);
                            }
                            else
                            {
                                writer.Write(_rawData, blockOffset0, 2);
                                writer.Write(_rawData, blockOffset1, 6);
                                writer.Write(_rawData, blockOffset2, 4);
                                writer.Write(_rawData, blockOffset3, 4);
                            }

                            blockOffset0 += 2;
                            blockOffset1 += 6;
                            blockOffset2 += 4;
                            blockOffset3 += 4;
                        }
                        else
                        {
                            // RLE2: write opaque alpha, then colour
                            Require(blockOffset2, 4);
                            Require(blockOffset3, 4);

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

            if (blockOffset0 != nextMipHeader.Offset0 ||
                blockOffset1 != nextMipHeader.Offset1 ||
                blockOffset2 != nextMipHeader.Offset2 ||
                blockOffset3 != nextMipHeader.Offset3 ||
                (rles && blockOffset4 != nextMipHeader.Offset4))
            {
                throw new InvalidDataException($"RLE stream offsets are inconsistent at mip level {i}.");
            }
        }

        return output.ToArray();
    }

    private void Require(int offset, int length)
    {
        if (offset < 0 || offset > _rawData.Length - length)
            throw new InvalidDataException("RLE data stream is truncated or out of range.");
    }

    /// <summary>
    /// Replaces this resource's data with the RLE encoding of a DXT5 DDS.
    /// </summary>
    /// <param name="ddsData">A complete DXT5 DDS file, including every mip level that should be encoded.</param>
    /// <param name="version">RLE2 (default) or RLES.</param>
    /// <exception cref="InvalidDataException">The DDS is not a valid DXT5 DDS or its data is truncated.</exception>
    /// <remarks>
    /// Source: RLEResource.cs lines 400-684 (ImportToRLE) and the s4pe helpers RLEDDSHelper/Import.cs,
    /// RLESDDSHelper/ImportRLES.cs. The mip chain is encoded as blocks classified as transparent (no non-zero alpha),
    /// opaque (all alpha 255) or translucent, in runs of up to 0x3FFF blocks. RLES additionally emits 16 bytes of 0xFF
    /// per translucent block into stream 4 (legacy lines 631-633). Legacy corner cases handled differently: a DDS whose
    /// mip count is 0 is treated as 1 level (legacy would emit no levels and fail), and truncated mip data throws rather than
    /// reading past the end.
    /// </remarks>
    public void ImportToRle(ReadOnlySpan<byte> ddsData, RleVersion version = RleVersion.Rle2)
    {
        _rawData = EncodeFromDds(ddsData, version);
        Parse(_rawData);
        OnChanged();
    }

    /// <summary>
    /// Builds RLE data from a DXT5 DDS without needing an existing resource.
    /// </summary>
    /// <remarks>See <see cref="ImportToRle"/>.</remarks>
    public static byte[] EncodeFromDds(ReadOnlySpan<byte> ddsData, RleVersion version = RleVersion.Rle2)
    {
        // RLEInfo.Parse (legacy lines 860-882)
        if (ddsData.Length < 128)
            throw new InvalidDataException("DDS data is shorter than the 128 byte header.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(ddsData) != DdsSignature)
            throw new InvalidDataException("Not a DDS file (bad signature).");
        if (BinaryPrimitives.ReadUInt32LittleEndian(ddsData[4..]) != 124)
            throw new InvalidDataException("Unexpected DDS header size.");
        if ((BinaryPrimitives.ReadUInt32LittleEndian(ddsData[8..]) & 0x1007) != 0x1007)
            throw new InvalidDataException("DDS header flags do not describe a texture.");

        uint heightRaw = BinaryPrimitives.ReadUInt32LittleEndian(ddsData[12..]);
        uint widthRaw = BinaryPrimitives.ReadUInt32LittleEndian(ddsData[16..]);
        if (heightRaw == 0 || widthRaw == 0 || heightRaw > ushort.MaxValue || widthRaw > ushort.MaxValue)
            throw new InvalidDataException("Invalid width or height.");

        uint depth = BinaryPrimitives.ReadUInt32LittleEndian(ddsData[24..]);
        if (depth > 1)
            throw new InvalidDataException($"Expected depth 1 or 0, read {depth}.");

        uint mipCountRaw = BinaryPrimitives.ReadUInt32LittleEndian(ddsData[28..]);
        if (mipCountRaw > 16)
            throw new InvalidDataException($"Expected mip map count of at most 16, read {mipCountRaw}.");

        uint fourCc = BinaryPrimitives.ReadUInt32LittleEndian(ddsData[84..]);
        if (fourCc != FourCcDxt5)
            throw new InvalidDataException($"Not a DXT5 format DDS, read 0x{fourCc:X8}.");

        int width = (int)widthRaw;
        int height = (int)heightRaw;
        int mipCount = (int)Math.Max(1, mipCountRaw);
        bool rles = version == RleVersion.Rles;
        int headerSize = rles ? 24 : 20;

        var commands = new MemoryStream();
        var block0 = new MemoryStream();
        var block1 = new MemoryStream();
        var block2 = new MemoryStream();
        var block3 = new MemoryStream();
        var block4 = new MemoryStream();
        var mipOffsets = new int[mipCount][];

        try
        {
            var commandWriter = new BinaryWriter(commands);
            int dataPos = 128;
            ReadOnlySpan<byte> opaqueFill = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

            for (int mip = 0; mip < mipCount; mip++)
            {
                mipOffsets[mip] =
                [
                    (int)commands.Length, (int)block2.Length, (int)block3.Length,
                    (int)block0.Length, (int)block1.Length, (int)block4.Length,
                ];

                int mipWidth = Math.Max(4, width >> mip);
                int mipHeight = Math.Max(4, height >> mip);
                int mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                if (dataPos > ddsData.Length - mipSize)
                    throw new InvalidDataException($"DDS data is truncated at mip level {mip}.");

                var mipData = ddsData.Slice(dataPos, mipSize);
                dataPos += mipSize;

                int blockCount = mipSize / 16;
                var classes = new byte[blockCount]; // 0 transparent, 2 opaque, 1 translucent
                for (int b = 0; b < blockCount; b++)
                    classes[b] = ClassifyBlock(mipData.Slice(b * 16, 16));

                int index = 0;
                while (index < blockCount)
                {
                    byte cls = classes[index];
                    int run = 1;
                    while (index + run < blockCount && run < 0x3FFF && classes[index + run] == cls)
                        run++;

                    if (cls == 2)
                    {
                        for (int k = 0; k < run; k++)
                        {
                            var block = mipData.Slice((index + k) * 16, 16);
                            if (rles)
                            {
                                block0.Write(block[..2]);
                                block1.Write(block.Slice(2, 6));
                            }

                            block2.Write(block.Slice(8, 4));
                            block3.Write(block.Slice(12, 4));
                        }
                    }
                    else if (cls == 1)
                    {
                        for (int k = 0; k < run; k++)
                        {
                            var block = mipData.Slice((index + k) * 16, 16);
                            block0.Write(block[..2]);
                            block1.Write(block.Slice(2, 6));
                            block2.Write(block.Slice(8, 4));
                            block3.Write(block.Slice(12, 4));
                            if (rles)
                            {
                                block4.Write(opaqueFill);
                                block4.Write(opaqueFill);
                            }
                        }
                    }

                    commandWriter.Write((ushort)((run << 2) | cls));
                    index += run;
                }
            }

            // Assemble: header, mip headers, command, block2, block3, block0, block1[, block4]
            int dataOffset = 16 + (headerSize * mipCount);
            int commandBase = dataOffset;
            int block2Base = commandBase + (int)commands.Length;
            int block3Base = block2Base + (int)block2.Length;
            int block0Base = block3Base + (int)block3.Length;
            int block1Base = block0Base + (int)block0.Length;
            int block4Base = block1Base + (int)block1.Length;

            using var result = new MemoryStream();
            using var w = new BinaryWriter(result);
            w.Write(FourCcDxt5);
            w.Write(rles ? FourCcRles : FourCcRle2);
            w.Write((ushort)width);
            w.Write((ushort)height);
            w.Write((ushort)mipCount);
            w.Write((ushort)0);

            for (int mip = 0; mip < mipCount; mip++)
            {
                var o = mipOffsets[mip];
                w.Write(o[0] + commandBase);
                w.Write(o[1] + block2Base);
                w.Write(o[2] + block3Base);
                w.Write(o[3] + block0Base);
                w.Write(o[4] + block1Base);
                if (rles)
                    w.Write(o[5] + block4Base);
            }

            w.Write(commands.ToArray());
            w.Write(block2.ToArray());
            w.Write(block3.ToArray());
            w.Write(block0.ToArray());
            w.Write(block1.ToArray());
            if (rles)
                w.Write(block4.ToArray());

            return result.ToArray();
        }
        finally
        {
            commands.Dispose();
            block0.Dispose();
            block1.Dispose();
            block2.Dispose();
            block3.Dispose();
            block4.Dispose();
        }
    }

    /// <summary>
    /// Classifies a DXT5 block by its alpha: 0 = no pixel has non-zero alpha, 2 = every pixel is 255, 1 = anything else.
    /// Source: RLEResource.cs lines 686-760 (UnpackAlpha, TestAlphaAll, TestAlphaAny) and the loops at lines 454-517.
    /// </summary>
    private static byte ClassifyBlock(ReadOnlySpan<byte> block)
    {
        Span<byte> alpha = stackalloc byte[8];
        alpha[0] = block[0];
        alpha[1] = block[1];

        if (alpha[0] > alpha[1])
        {
            for (int i = 1; i <= 6; i++)
                alpha[1 + i] = (byte)((((7 - i) * alpha[0]) + (i * alpha[1]) + 3) / 7);
        }
        else
        {
            for (int i = 1; i <= 4; i++)
                alpha[1 + i] = (byte)((((5 - i) * alpha[0]) + (i * alpha[1]) + 2) / 5);
            alpha[6] = 0x00;
            alpha[7] = 0xFF;
        }

        ulong bits = 0;
        for (int i = 7; i >= 2; i--)
        {
            bits <<= 8;
            bits |= block[i];
        }

        bool anyNonZero = false;
        bool allOpaque = true;
        for (int i = 0; i < 16; i++)
        {
            byte a = alpha[(int)(bits & 7)];
            if (a != 0) anyNonZero = true;
            if (a != 0xFF) allOpaque = false;
            bits >>= 3;
        }

        if (!anyNonZero) return 0;
        return allOpaque ? (byte)2 : (byte)1;
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
