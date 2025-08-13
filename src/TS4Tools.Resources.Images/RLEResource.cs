using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Modern .NET 9 implementation of RLE (Run-Length Encoded) compressed image resources.
/// Supports RLE2, RLES, and other RLE format variations used in The Sims 4.
/// </summary>
public sealed class RLEResource : IRLEResource, IDisposable
{
    private readonly ILogger<RLEResource> _logger;
    private byte[] _data = Array.Empty<byte>();
    private RLEHeader _header;
    private MipHeader[] _mipHeaders = Array.Empty<MipHeader>();
    private bool _disposed;

    /// <summary>
    /// Magic signature for RLE resources.
    /// </summary>
    public const uint RLESignature = 0x534c4452; // "RDLS"

    /// <inheritdoc />
    public int Width => (int)_header.Width;

    /// <inheritdoc />
    public int Height => (int)_header.Height;

    /// <inheritdoc />
    public uint MipCount => _header.MipCount;

    /// <inheritdoc />
    public RLEVersion Version => (RLEVersion)_header.Version;

    /// <inheritdoc />
    public uint PixelFormat => _header.PixelFormat;

    #region IResource Implementation

    /// <summary>
    /// Gets the API version that was requested when this resource was created.
    /// </summary>
    public int RequestedApiVersion { get; private set; } = 1;

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the resource content as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RLEResource));
            return new MemoryStream(_data, writable: false);
        }
    }

    /// <summary>
    /// Gets the resource content as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RLEResource));
            return _data.ToArray();
        }
    }

    /// <summary>
    /// Event raised when the resource is modified.
    /// </summary>
#pragma warning disable CS0067 // Event is never used - required by IResource interface
    public event EventHandler? ResourceChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        nameof(Version),
        nameof(Width),
        nameof(Height),
        nameof(PixelFormat)
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field by index.
    /// </summary>
    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    /// <summary>
    /// Gets or sets a content field by name.
    /// </summary>
    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    #endregion

    #region Content Field Helpers

    private TypedValue GetFieldByIndex(int index)
    {
        return index switch
        {
            0 => new TypedValue(typeof(uint), Version),
            1 => new TypedValue(typeof(uint), Width),
            2 => new TypedValue(typeof(uint), Height),
            3 => new TypedValue(typeof(uint), PixelFormat),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        // RLE resources are typically read-only once loaded
        throw new NotSupportedException("RLE resource fields are read-only");
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(Width) => new TypedValue(typeof(uint), Width),
            nameof(Height) => new TypedValue(typeof(uint), Height),
            nameof(PixelFormat) => new TypedValue(typeof(uint), PixelFormat),
            _ => throw new ArgumentException($"Unknown field name: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        // RLE resources are typically read-only once loaded
        throw new NotSupportedException("RLE resource fields are read-only");
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="RLEResource"/> class.
    /// </summary>
    /// <param name="apiVersion">The API version.</param>
    /// <param name="stream">Optional stream containing RLE data to parse.</param>
    /// <param name="logger">Logger for this resource.</param>
    public RLEResource(int apiVersion, Stream? stream, ILogger<RLEResource>? logger = null)
    {
        RequestedApiVersion = apiVersion;
        _logger = logger ?? NullLogger<RLEResource>.Instance;

        if (stream is not null)
        {
            ParseRLEData(stream);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> ToDDSAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await Task.Run(ToDDS, cancellationToken);
    }

    /// <inheritdoc />
    public Stream ToDDS()
    {
        ThrowIfDisposed();

        if (_data.Length == 0)
        {
            _logger.LogWarning("Attempting to convert empty RLE resource to DDS");
            return new MemoryStream();
        }

        try
        {
            return ConvertRLEToDDS();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert RLE resource to DDS format");
            throw new InvalidOperationException("Failed to convert RLE resource to DDS format", ex);
        }
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetRawData()
    {
        ThrowIfDisposed();
        return _data.AsSpan();
    }

    /// <inheritdoc />
    public async Task SetDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            // Read data asynchronously into memory
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            ParseRLEData(memoryStream);

            _logger.LogDebug("Successfully loaded RLE data: {Width}x{Height}, {MipCount} mips, Version: {Version}",
                Width, Height, MipCount, Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set RLE data from stream");
            throw new InvalidOperationException("Failed to set RLE data from stream", ex);
        }
    }

    /// <summary>
    /// Parses RLE data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing RLE data.</param>
    private void ParseRLEData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.Length == 0)
        {
            _logger.LogDebug("Empty RLE stream provided");
            _data = Array.Empty<byte>();
            return;
        }

        stream.Position = 0;

        // Read all data into memory for processing
        _data = new byte[stream.Length];
        stream.ReadExactly(_data);

        if (_data.Length != stream.Length)
        {
            throw new InvalidDataException($"Failed to read complete RLE data. Expected: {stream.Length}, Read: {_data.Length}");
        }

        // Parse the RLE header
        var headerSpan = _data.AsSpan();
        _header = ParseRLEHeader(headerSpan);

        // Parse mip headers
        var mipHeadersSpan = headerSpan.Slice(Marshal.SizeOf<RLEHeader>());
        _mipHeaders = ParseMipHeaders(mipHeadersSpan, _header);

        _logger.LogDebug("Parsed RLE resource: {Width}x{Height}, Version: {Version}, MipCount: {MipCount}, Format: 0x{PixelFormat:X8}",
            Width, Height, Version, MipCount, PixelFormat);
    }

    /// <summary>
    /// Parses the RLE header from raw data.
    /// </summary>
    /// <param name="data">The raw RLE data.</param>
    /// <returns>Parsed RLE header.</returns>
    private static RLEHeader ParseRLEHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < Marshal.SizeOf<RLEHeader>())
        {
            throw new InvalidDataException($"RLE data too short for header. Expected: {Marshal.SizeOf<RLEHeader>()}, Got: {data.Length}");
        }

        var header = MemoryMarshal.Read<RLEHeader>(data);

        // Validate RLE version
        var version = (RLEVersion)header.Version;
        if (version != RLEVersion.RLE2 && version != RLEVersion.RLES)
        {
            throw new NotSupportedException($"Unsupported RLE version: 0x{header.Version:X8}");
        }

        return header;
    }

    /// <summary>
    /// Parses mip headers from RLE data.
    /// </summary>
    /// <param name="data">The data following the RLE header.</param>
    /// <param name="header">The parsed RLE header.</param>
    /// <returns>Array of parsed mip headers.</returns>
    private static MipHeader[] ParseMipHeaders(ReadOnlySpan<byte> data, RLEHeader header)
    {
        var mipHeaders = new MipHeader[header.MipCount + 1]; // +1 for end marker
        var mipHeaderSize = GetMipHeaderSize(header);

        for (int i = 0; i < header.MipCount; i++)
        {
            if (data.Length < (i + 1) * mipHeaderSize)
            {
                throw new InvalidDataException($"Insufficient data for mip header {i}");
            }

            var mipData = data.Slice(i * mipHeaderSize, mipHeaderSize);
            mipHeaders[i] = ParseSingleMipHeader(mipData, header);
        }

        // Create end marker mip header
        mipHeaders[header.MipCount] = CreateEndMarkerMipHeader(mipHeaders[0], header);

        return mipHeaders;
    }

    /// <summary>
    /// Gets the size of a mip header based on the RLE format.
    /// </summary>
    /// <param name="header">The RLE header.</param>
    /// <returns>Size of mip header in bytes.</returns>
    private static int GetMipHeaderSize(RLEHeader header)
    {
        return header.PixelFormat == (uint)RLEPixelFormat.L8 ? 8 : // L8 format: CommandOffset + Offset0
               (RLEVersion)header.Version == RLEVersion.RLES ? 24 :            // RLES: 6 offsets
               20;                                                  // RLE2: 5 offsets
    }

    /// <summary>
    /// Parses a single mip header.
    /// </summary>
    /// <param name="data">The mip header data.</param>
    /// <param name="rleHeader">The RLE header.</param>
    /// <returns>Parsed mip header.</returns>
    private static MipHeader ParseSingleMipHeader(ReadOnlySpan<byte> data, RLEHeader rleHeader)
    {
        var mipHeader = new MipHeader();

        if (rleHeader.PixelFormat == (uint)RLEPixelFormat.L8)
        {
            // L8 format: CommandOffset + Offset0
            mipHeader.CommandOffset = BinaryPrimitives.ReadInt32LittleEndian(data);
            mipHeader.Offset0 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4));
        }
        else
        {
            // DXT formats: CommandOffset + Offset2 + Offset3 + Offset0 + Offset1 [+ Offset4 for RLES]
            mipHeader.CommandOffset = BinaryPrimitives.ReadInt32LittleEndian(data);
            mipHeader.Offset2 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4));
            mipHeader.Offset3 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(8));
            mipHeader.Offset0 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12));
            mipHeader.Offset1 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(16));

            if (rleHeader.Version == (int)RLEVersion.RLES)
            {
                mipHeader.Offset4 = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(20));
            }
        }

        return mipHeader;
    }

    /// <summary>
    /// Creates an end marker mip header.
    /// </summary>
    /// <param name="firstMip">The first mip header.</param>
    /// <param name="rleHeader">The RLE header.</param>
    /// <returns>End marker mip header.</returns>
    private static MipHeader CreateEndMarkerMipHeader(MipHeader firstMip, RLEHeader rleHeader)
    {
        var endMarker = new MipHeader();

        if (rleHeader.PixelFormat == (uint)RLEPixelFormat.L8)
        {
            endMarker.CommandOffset = firstMip.Offset0;
            // Offset0 will be set to data length during processing
        }
        else
        {
            endMarker.CommandOffset = firstMip.Offset2;
            endMarker.Offset2 = firstMip.Offset3;
            endMarker.Offset3 = firstMip.Offset0;
            endMarker.Offset0 = firstMip.Offset1;

            if ((RLEVersion)rleHeader.Version == RLEVersion.RLES)
            {
                endMarker.Offset1 = firstMip.Offset4;
                // Offset4 will be set to data length during processing
            }
            // else Offset1 will be set to data length during processing
        }

        return endMarker;
    }

    /// <summary>
    /// Converts RLE data to DDS format.
    /// </summary>
    /// <returns>Stream containing DDS data.</returns>
    private Stream ConvertRLEToDDS()
    {
        var ddsStream = new MemoryStream();
        using var writer = new BinaryWriter(ddsStream, System.Text.Encoding.UTF8, true);

        // Write DDS signature
        writer.Write(RLESignature);

        // Write RLE header as DDS header (they share format)
        WriteRLEHeaderAsDDS(writer);

        // Process each mip level
        for (int i = 0; i < _header.MipCount; i++)
        {
            ProcessMipLevel(writer, i);
        }

        ddsStream.Position = 0;
        return ddsStream;
    }

    /// <summary>
    /// Writes the RLE header in DDS format.
    /// </summary>
    /// <param name="writer">Binary writer for output.</param>
    private void WriteRLEHeaderAsDDS(BinaryWriter writer)
    {
        // Write header fields in DDS format
        writer.Write(_header.Width);
        writer.Write(_header.Height);
        writer.Write(_header.MipCount);
        writer.Write(_header.PixelFormat);
        // Add additional DDS header fields as needed
    }

    /// <summary>
    /// Processes a single mip level and writes DXT data.
    /// </summary>
    /// <param name="writer">Binary writer for output.</param>
    /// <param name="mipIndex">Index of the mip level to process.</param>
    private void ProcessMipLevel(BinaryWriter writer, int mipIndex)
    {
        var mipHeader = _mipHeaders[mipIndex];
        var nextMipHeader = _mipHeaders[mipIndex + 1];

        if (_header.PixelFormat == (uint)RLEPixelFormat.L8)
        {
            ProcessL8MipLevel(writer, mipHeader, nextMipHeader);
        }
        else
        {
            ProcessDXTMipLevel(writer, mipHeader, nextMipHeader);
        }
    }

    /// <summary>
    /// Processes L8 format mip level.
    /// </summary>
    private void ProcessL8MipLevel(BinaryWriter writer, MipHeader mipHeader, MipHeader nextMipHeader)
    {
        // Implementation for L8 format processing
        // This involves decoding RLE commands and writing DXT blocks
        _logger.LogDebug("Processing L8 mip level from offset {CommandOffset} to {NextCommandOffset}",
            mipHeader.CommandOffset, nextMipHeader.CommandOffset);

        // Process RLE commands for L8 format
        ProcessRLECommands(writer, mipHeader.CommandOffset, nextMipHeader.CommandOffset, mipHeader.Offset0);
    }

    /// <summary>
    /// Processes DXT format mip level.
    /// </summary>
    private void ProcessDXTMipLevel(BinaryWriter writer, MipHeader mipHeader, MipHeader nextMipHeader)
    {
        _logger.LogDebug("Processing DXT mip level from offset {CommandOffset} to {NextCommandOffset}",
            mipHeader.CommandOffset, nextMipHeader.CommandOffset);

        // Process RLE commands for DXT format
        ProcessRLECommands(writer, mipHeader.CommandOffset, nextMipHeader.CommandOffset, mipHeader.Offset0);
    }

    /// <summary>
    /// Processes RLE commands and writes decompressed data.
    /// </summary>
    /// <param name="writer">Binary writer for output.</param>
    /// <param name="commandStart">Start offset of commands.</param>
    /// <param name="commandEnd">End offset of commands.</param>
    /// <param name="blockOffset">Starting block data offset.</param>
    private void ProcessRLECommands(BinaryWriter writer, int commandStart, int commandEnd, int blockOffset)
    {
        var currentBlockOffset = blockOffset;

        for (var commandOffset = commandStart; commandOffset < commandEnd; commandOffset += 2)
        {
            if (commandOffset + 1 >= _data.Length)
            {
                _logger.LogWarning("Command offset {Offset} exceeds data length {Length}", commandOffset, _data.Length);
                break;
            }

            var command = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(commandOffset));
            var operation = (uint)(command & 3);
            var count = (uint)(command >> 2);

            currentBlockOffset = ProcessRLECommand(writer, operation, count, currentBlockOffset);
        }
    }

    /// <summary>
    /// Processes a single RLE command.
    /// </summary>
    /// <param name="writer">Binary writer for output.</param>
    /// <param name="operation">RLE operation type.</param>
    /// <param name="count">Number of blocks to process.</param>
    /// <param name="blockOffset">Current block data offset.</param>
    /// <returns>Updated block offset.</returns>
    private int ProcessRLECommand(BinaryWriter writer, uint operation, uint count, int blockOffset)
    {
        switch (operation)
        {
            case 0: // Zero blocks
                WriteZeroBlocks(writer, count);
                break;

            case 1: // Data blocks
                blockOffset = WriteDataBlocks(writer, count, blockOffset);
                break;

            case 2: // Repeated block
                blockOffset = WriteRepeatedBlock(writer, count, blockOffset);
                break;

            default:
                _logger.LogWarning("Unknown RLE operation: {Operation}", operation);
                break;
        }

        return blockOffset;
    }

    /// <summary>
    /// Writes zero-filled blocks.
    /// </summary>
    private static void WriteZeroBlocks(BinaryWriter writer, uint count)
    {
        var zeroBlock = ArrayPool<byte>.Shared.Rent(16); // DXT block size
        try
        {
            Array.Clear(zeroBlock, 0, 16);
            for (uint i = 0; i < count; i++)
            {
                writer.Write(zeroBlock, 0, 16);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(zeroBlock);
        }
    }

    /// <summary>
    /// Writes data blocks from RLE data.
    /// </summary>
    private int WriteDataBlocks(BinaryWriter writer, uint count, int blockOffset)
    {
        const int blockSize = 16; // DXT block size

        for (uint i = 0; i < count; i++)
        {
            if (blockOffset + blockSize <= _data.Length)
            {
                writer.Write(_data, blockOffset, blockSize);
                blockOffset += blockSize;
            }
            else
            {
                _logger.LogWarning("Block offset {Offset} exceeds data bounds", blockOffset);
                WriteZeroBlocks(writer, 1);
            }
        }

        return blockOffset;
    }

    /// <summary>
    /// Writes a repeated block.
    /// </summary>
    private int WriteRepeatedBlock(BinaryWriter writer, uint count, int blockOffset)
    {
        const int blockSize = 16;

        if (blockOffset + blockSize <= _data.Length)
        {
            var blockData = new byte[blockSize];
            Array.Copy(_data, blockOffset, blockData, 0, blockSize);

            for (uint i = 0; i < count; i++)
            {
                writer.Write(blockData);
            }

            return blockOffset + blockSize;
        }
        else
        {
            _logger.LogWarning("Repeated block offset {Offset} exceeds data bounds", blockOffset);
            WriteZeroBlocks(writer, count);
            return blockOffset;
        }
    }

    /// <summary>
    /// Throws if the resource has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _data = Array.Empty<byte>();
            _mipHeaders = Array.Empty<MipHeader>();
            _disposed = true;
            _logger.LogDebug("RLEResource disposed");
        }
    }

    /// <summary>
    /// RLE header structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RLEHeader
    {
        public uint Width;
        public uint Height;
        public uint MipCount;
        public int Version;  // Changed from RLEVersion to int for marshaling
        public uint PixelFormat;
        // Additional fields as needed based on format analysis
    }

    /// <summary>
    /// Mip header structure for RLE data.
    /// </summary>
    private struct MipHeader
    {
        public int CommandOffset;
        public int Offset0;
        public int Offset1;
        public int Offset2;
        public int Offset3;
        public int Offset4; // Only used in RLES format
    }
}
