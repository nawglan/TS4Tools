using System.IO.Compression;

namespace TS4Tools.Compression;

/// <summary>
/// Handles compression of resource data.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Package/Compression.cs
///
/// Compression logic - CompressStream() and _compress() (lines 189-230):
/// - Uses DeflaterOutputStream from SharpZipLib
/// - Only compresses if result is smaller than input (lines 216-226)
/// - New implementation uses .NET ZLibStream
///
/// Compression type marker 0x5A42 ("ZB") is used in index entries
/// to indicate ZLIB-compressed resources.
/// </remarks>
internal static class Compressor
{
    /// <summary>
    /// Compression type for ZLIB.
    /// </summary>
    public const ushort ZlibCompressionType = 0x5A42; // "ZB"

    /// <summary>
    /// No compression.
    /// </summary>
    public const ushort NoCompression = 0x0000;

    /// <summary>
    /// Compresses data using ZLIB.
    /// </summary>
    /// <param name="data">The uncompressed data.</param>
    /// <returns>The compressed data, or the original data if compression doesn't reduce size.</returns>
    public static (byte[] Data, bool IsCompressed) Compress(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return ([], false);

        using var outputStream = new MemoryStream();
        using (var zlibStream = new ZLibStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlibStream.Write(data);
        }

        var compressed = outputStream.ToArray();

        // Only use compression if it actually reduces size
        if (compressed.Length < data.Length)
        {
            return (compressed, true);
        }
        else
        {
            return (data.ToArray(), false);
        }
    }
}
