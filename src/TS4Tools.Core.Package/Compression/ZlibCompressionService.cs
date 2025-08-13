using System.Buffers;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Package.Compression;

/// <summary>
/// Default implementation of compression service using ZLIB/DEFLATE compression
/// compatible with Sims 4 package format.
/// </summary>
public sealed class ZlibCompressionService : ICompressionService
{
    private readonly ILogger<ZlibCompressionService> _logger;

    /// <summary>
    /// ZLIB header bytes - valid combinations for ZLIB streams.
    /// </summary>
    private static readonly byte[][] ZlibHeaders = [
        [0x78, 0x01], // No compression/low
        [0x78, 0x5E], // Fast compression
        [0x78, 0x9C], // Default compression
        [0x78, 0xDA]  // Best compression
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="ZlibCompressionService"/> class.
    /// </summary>
    /// <param name="logger">Logger for the compression service operations.</param>
    public ZlibCompressionService(ILogger<ZlibCompressionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public byte[] Compress(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return [];
        }

        _logger.LogDebug("Compressing {Size} bytes using ZLIB", data.Length);

        using var output = new MemoryStream();
        using var zlibStream = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true);

        zlibStream.Write(data);
        zlibStream.Close(); // Ensure all data is flushed

        var compressed = output.ToArray();
        var ratio = CalculateCompressionRatio(data.Length, compressed.Length);

        _logger.LogDebug("Compression completed: {OriginalSize} -> {CompressedSize} bytes (ratio: {Ratio:P2})",
            data.Length, compressed.Length, ratio);

        return compressed;
    }

    /// <inheritdoc />
    public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (data.IsEmpty)
        {
            return [];
        }

        _logger.LogDebug("Compressing {Size} bytes using ZLIB (async)", data.Length);

        using var output = new MemoryStream();
        await using var zlibStream = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true);

        await zlibStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await zlibStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        zlibStream.Close();

        var compressed = output.ToArray();
        var ratio = CalculateCompressionRatio(data.Length, compressed.Length);

        _logger.LogDebug("Async compression completed: {OriginalSize} -> {CompressedSize} bytes (ratio: {Ratio:P2})",
            data.Length, compressed.Length, ratio);

        return compressed;
    }

    /// <inheritdoc />
    public byte[] Decompress(ReadOnlySpan<byte> compressedData, int originalSize)
    {
        if (compressedData.IsEmpty)
        {
            return [];
        }

        if (!IsCompressed(compressedData))
        {
            _logger.LogWarning("Data does not appear to be ZLIB compressed, returning as-is");
            return compressedData.ToArray();
        }

        _logger.LogDebug("Decompressing {CompressedSize} bytes to expected {OriginalSize} bytes",
            compressedData.Length, originalSize);

        using var input = new MemoryStream(compressedData.ToArray());
        using var zlibStream = new ZLibStream(input, CompressionMode.Decompress);

        var decompressed = new byte[originalSize];
        var totalRead = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            while (totalRead < originalSize)
            {
                var bytesToRead = Math.Min(buffer.Length, originalSize - totalRead);
                var bytesRead = zlibStream.Read(buffer, 0, bytesToRead);
                if (bytesRead == 0)
                {
                    break; // End of stream
                }

                buffer.AsSpan(0, bytesRead).CopyTo(decompressed.AsSpan(totalRead));
                totalRead += bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (totalRead != originalSize)
        {
            _logger.LogWarning("Decompressed size mismatch: expected {Expected}, got {Actual}",
                originalSize, totalRead);
        }

        _logger.LogDebug("Decompression completed: {CompressedSize} -> {DecompressedSize} bytes",
            compressedData.Length, totalRead);

        return decompressed.AsSpan(0, totalRead).ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData, int originalSize, CancellationToken cancellationToken = default)
    {
        if (compressedData.IsEmpty)
        {
            return [];
        }

        if (!IsCompressed(compressedData.Span))
        {
            _logger.LogWarning("Data does not appear to be ZLIB compressed, returning as-is");
            return compressedData.ToArray();
        }

        _logger.LogDebug("Decompressing {CompressedSize} bytes to expected {OriginalSize} bytes (async)",
            compressedData.Length, originalSize);

        using var input = new MemoryStream(compressedData.ToArray());
        await using var zlibStream = new ZLibStream(input, CompressionMode.Decompress);

        var decompressed = new byte[originalSize];
        var totalRead = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            while (totalRead < originalSize)
            {
                var bytesToRead = Math.Min(buffer.Length, originalSize - totalRead);
                var bytesRead = await zlibStream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    break; // End of stream
                }

                buffer.AsSpan(0, bytesRead).CopyTo(decompressed.AsSpan(totalRead));
                totalRead += bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (totalRead != originalSize)
        {
            _logger.LogWarning("Decompressed size mismatch: expected {Expected}, got {Actual}",
                originalSize, totalRead);
        }

        _logger.LogDebug("Async decompression completed: {CompressedSize} -> {DecompressedSize} bytes",
            compressedData.Length, totalRead);

        return decompressed.AsSpan(0, totalRead).ToArray();
    }

    /// <inheritdoc />
    public bool IsCompressed(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
        {
            return false;
        }

        var header = data[..2];

        foreach (var validHeader in ZlibHeaders)
        {
            if (header.SequenceEqual(validHeader))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public double CalculateCompressionRatio(int originalSize, int compressedSize)
    {
        if (originalSize <= 0)
        {
            return 0.0;
        }

        return (double)compressedSize / originalSize;
    }
}
