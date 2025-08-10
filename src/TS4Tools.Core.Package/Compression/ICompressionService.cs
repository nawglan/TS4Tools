using System.IO.Compression;

namespace TS4Tools.Core.Package.Compression;

/// <summary>
/// Provides compression and decompression services for Sims 4 package resources.
/// Supports ZLIB/DEFLATE compression as used in Sims 4 package files.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Compresses the specified data using ZLIB compression.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>The compressed data.</returns>
    byte[] Compress(ReadOnlySpan<byte> data);

    /// <summary>
    /// Compresses the specified data using ZLIB compression.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compressed data.</returns>
    Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses the specified ZLIB-compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="originalSize">The expected size of the decompressed data.</param>
    /// <returns>The decompressed data.</returns>
    byte[] Decompress(ReadOnlySpan<byte> compressedData, int originalSize);

    /// <summary>
    /// Decompresses the specified ZLIB-compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="originalSize">The expected size of the decompressed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData, int originalSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the provided data is ZLIB compressed by checking the header.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data appears to be ZLIB compressed.</returns>
    bool IsCompressed(ReadOnlySpan<byte> data);

    /// <summary>
    /// Calculates the compression ratio for the given data.
    /// </summary>
    /// <param name="originalSize">The original uncompressed size.</param>
    /// <param name="compressedSize">The compressed size.</param>
    /// <returns>The compression ratio (0.0 to 1.0, where lower is better compression).</returns>
    double CalculateCompressionRatio(int originalSize, int compressedSize);
}
