namespace TS4Tools.Resources.Images;

/// <summary>
/// Represents the header of a DDS (DirectDraw Surface) file.
/// </summary>
public readonly record struct DdsHeader
{
    /// <summary>
    /// DDS magic number: "DDS " (0x20534444).
    /// </summary>
    public const uint DdsMagic = 0x20534444;

    /// <summary>
    /// Size of the DDS header structure in bytes (124).
    /// </summary>
    public const uint HeaderSize = 124;

    /// <summary>
    /// Size of the header structure. This value must be 124.
    /// </summary>
    public uint Size { get; init; } = HeaderSize;

    /// <summary>
    /// Flags to indicate which members contain valid data.
    /// </summary>
    public DdsFlags Flags { get; init; }

    /// <summary>
    /// Surface height (in pixels).
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    /// Surface width (in pixels).
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    /// The pitch or number of bytes per scan line in an uncompressed texture.
    /// </summary>
    public uint PitchOrLinearSize { get; init; }

    /// <summary>
    /// Depth of a volume texture (in pixels), otherwise unused.
    /// </summary>
    public uint Depth { get; init; }

    /// <summary>
    /// Number of mipmap levels, otherwise unused.
    /// </summary>
    public uint MipMapCount { get; init; }

    /// <summary>
    /// Unused reserved fields.
    /// </summary>
    public IReadOnlyList<uint> Reserved1 { get; init; } = new uint[11];

    /// <summary>
    /// The pixel format of the surface.
    /// </summary>
    public DdsPixelFormat PixelFormat { get; init; }

    /// <summary>
    /// Specifies the complexity of the surfaces stored.
    /// </summary>
    public DdsCaps Caps { get; init; }

    /// <summary>
    /// Additional detail about the surfaces stored.
    /// </summary>
    public DdsCaps2 Caps2 { get; init; }

    /// <summary>
    /// Unused.
    /// </summary>
    public uint Caps3 { get; init; }

    /// <summary>
    /// Unused.
    /// </summary>
    public uint Caps4 { get; init; }

    /// <summary>
    /// Unused.
    /// </summary>
    public uint Reserved2 { get; init; }

    /// <summary>
    /// Initializes a new instance of the DdsHeader struct.
    /// </summary>
    public DdsHeader()
    {
        Size = HeaderSize;
        Flags = DdsFlags.None;
        Height = 0;
        Width = 0;
        PitchOrLinearSize = 0;
        Depth = 0;
        MipMapCount = 0;
        Reserved1 = new uint[11];
        PixelFormat = new DdsPixelFormat();
        Caps = DdsCaps.None;
        Caps2 = DdsCaps2.None;
        Caps3 = 0;
        Caps4 = 0;
        Reserved2 = 0;
    }

    /// <summary>
    /// Creates a basic DDS header for uncompressed RGBA data.
    /// </summary>
    /// <param name="width">Width of the image in pixels.</param>
    /// <param name="height">Height of the image in pixels.</param>
    /// <param name="mipMapCount">Number of mipmap levels (0 or 1 for no mipmaps).</param>
    /// <returns>A DDS header configured for uncompressed RGBA data.</returns>
    public static DdsHeader CreateForRGBA32(uint width, uint height, uint mipMapCount = 1) => new()
    {
        Size = HeaderSize,
        Flags = DdsFlags.Caps | DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat | DdsFlags.Pitch,
        Height = height,
        Width = width,
        PitchOrLinearSize = width * 4, // 4 bytes per pixel for RGBA32
        Depth = 0,
        MipMapCount = mipMapCount,
        Reserved1 = new uint[11],
        PixelFormat = DdsPixelFormat.CreateForRGBA32(),
        Caps = DdsCaps.Texture | (mipMapCount > 1 ? DdsCaps.MipMap : 0),
        Caps2 = 0,
        Caps3 = 0,
        Caps4 = 0,
        Reserved2 = 0
    };

    /// <summary>
    /// Creates a DDS header for compressed texture data.
    /// </summary>
    /// <param name="width">Width of the image in pixels.</param>
    /// <param name="height">Height of the image in pixels.</param>
    /// <param name="fourCC">The compression format.</param>
    /// <param name="linearSize">Size of the compressed image data.</param>
    /// <param name="mipMapCount">Number of mipmap levels (0 or 1 for no mipmaps).</param>
    /// <returns>A DDS header configured for compressed data.</returns>
    public static DdsHeader CreateForCompressed(uint width, uint height, DdsFourCC fourCC, uint linearSize, uint mipMapCount = 1) => new()
    {
        Size = HeaderSize,
        Flags = DdsFlags.Caps | DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat | DdsFlags.LinearSize | (mipMapCount > 1 ? DdsFlags.MipMapCount : 0),
        Height = height,
        Width = width,
        PitchOrLinearSize = linearSize,
        Depth = 0,
        MipMapCount = mipMapCount,
        Reserved1 = new uint[11],
        PixelFormat = DdsPixelFormat.CreateForFourCC(fourCC),
        Caps = DdsCaps.Texture | (mipMapCount > 1 ? DdsCaps.MipMap : 0),
        Caps2 = 0,
        Caps3 = 0,
        Caps4 = 0,
        Reserved2 = 0
    };
}

/// <summary>
/// Provides methods for reading and writing DDS headers.
/// </summary>
public static class DdsHeaderExtensions
{
    /// <summary>
    /// Reads a DDS header from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The parsed DDS header.</returns>
    /// <exception cref="InvalidDataException">Thrown when the stream does not contain a valid DDS header.</exception>
    public static DdsHeader ReadDdsHeader(this Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Read and validate magic number
        uint magic = reader.ReadUInt32();
        if (magic != DdsHeader.DdsMagic)
            throw new InvalidDataException($"Invalid DDS magic number: 0x{magic:X8}. Expected: 0x{DdsHeader.DdsMagic:X8}");

        // Read header
        uint size = reader.ReadUInt32();
        if (size != DdsHeader.HeaderSize)
            throw new InvalidDataException($"Invalid DDS header size: {size}. Expected: {DdsHeader.HeaderSize}");

        var flags = (DdsFlags)reader.ReadUInt32();
        uint height = reader.ReadUInt32();
        uint width = reader.ReadUInt32();
        uint pitchOrLinearSize = reader.ReadUInt32();
        uint depth = reader.ReadUInt32();
        uint mipMapCount = reader.ReadUInt32();

        // Read reserved fields
        var reserved1 = new uint[11];
        for (int i = 0; i < 11; i++)
        {
            reserved1[i] = reader.ReadUInt32();
        }

        // Read pixel format
        var pixelFormat = ReadPixelFormat(reader);

        var caps = (DdsCaps)reader.ReadUInt32();
        var caps2 = (DdsCaps2)reader.ReadUInt32();
        uint caps3 = reader.ReadUInt32();
        uint caps4 = reader.ReadUInt32();
        uint reserved2 = reader.ReadUInt32();

        return new DdsHeader
        {
            Size = size,
            Flags = flags,
            Height = height,
            Width = width,
            PitchOrLinearSize = pitchOrLinearSize,
            Depth = depth,
            MipMapCount = mipMapCount,
            Reserved1 = reserved1,
            PixelFormat = pixelFormat,
            Caps = caps,
            Caps2 = caps2,
            Caps3 = caps3,
            Caps4 = caps4,
            Reserved2 = reserved2
        };
    }

    /// <summary>
    /// Writes a DDS header to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="header">The header to write.</param>
    public static void WriteDdsHeader(this Stream stream, DdsHeader header)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Write magic number
        writer.Write(DdsHeader.DdsMagic);

        // Write header
        writer.Write(header.Size);
        writer.Write((uint)header.Flags);
        writer.Write(header.Height);
        writer.Write(header.Width);
        writer.Write(header.PitchOrLinearSize);
        writer.Write(header.Depth);
        writer.Write(header.MipMapCount);

        // Write reserved fields
        for (int i = 0; i < 11; i++)
        {
            writer.Write(i < header.Reserved1.Count ? header.Reserved1[i] : 0u);
        }

        // Write pixel format
        WritePixelFormat(writer, header.PixelFormat);

        writer.Write((uint)header.Caps);
        writer.Write((uint)header.Caps2);
        writer.Write(header.Caps3);
        writer.Write(header.Caps4);
        writer.Write(header.Reserved2);
    }

    private static DdsPixelFormat ReadPixelFormat(BinaryReader reader)
    {
        uint size = reader.ReadUInt32();
        if (size != 32)
            throw new InvalidDataException($"Invalid pixel format size: {size}. Expected: 32");

        var flags = (DdsPixelFormatFlags)reader.ReadUInt32();
        var fourCC = (DdsFourCC)reader.ReadUInt32();
        uint rgbBitCount = reader.ReadUInt32();
        uint redBitMask = reader.ReadUInt32();
        uint greenBitMask = reader.ReadUInt32();
        uint blueBitMask = reader.ReadUInt32();
        uint alphaBitMask = reader.ReadUInt32();

        return new DdsPixelFormat
        {
            Size = size,
            Flags = flags,
            FourCC = fourCC,
            RgbBitCount = rgbBitCount,
            RedBitMask = redBitMask,
            GreenBitMask = greenBitMask,
            BlueBitMask = blueBitMask,
            AlphaBitMask = alphaBitMask
        };
    }

    private static void WritePixelFormat(BinaryWriter writer, DdsPixelFormat pixelFormat)
    {
        writer.Write(pixelFormat.Size);
        writer.Write((uint)pixelFormat.Flags);
        writer.Write((uint)pixelFormat.FourCC);
        writer.Write(pixelFormat.RgbBitCount);
        writer.Write(pixelFormat.RedBitMask);
        writer.Write(pixelFormat.GreenBitMask);
        writer.Write(pixelFormat.BlueBitMask);
        writer.Write(pixelFormat.AlphaBitMask);
    }
}
