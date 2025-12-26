using System.IO.Compression;

namespace TS4Tools.Compression;

/// <summary>
/// Handles decompression of resource data.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Package/Compression.cs
///
/// Compression detection (lines 49-60):
/// - byte[0] == 0x78: ZLIB/DEFLATE (Sims 4 primary format)
/// - byte[1] == 0xFB: RefPack/QFS (legacy EA format)
///
/// ZLIB decompression (lines 62-75):
/// - Uses InflaterInputStream from SharpZipLib
/// - New implementation uses .NET ZLibStream
///
/// RefPack decompression - OldDecompress() (lines 92-168):
/// - Size encoding: compressionType != 0x80 → 3-byte size, else 4-byte size (lines 96-104)
/// - Control byte ranges:
///   - 0x00-0x7F: 2-byte command (lines 110-121)
///   - 0x80-0xBF: 3-byte command (lines 123-135)
///   - 0xC0-0xDF: 4-byte command (lines 136-149)
///   - 0xE0-0xFB: Plain text run (lines 150-156)
///   - 0xFC-0xFF: End marker (lines 158-164)
/// </remarks>
internal static class Decompressor
{
    /// <summary>
    /// Decompresses resource data.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="expectedSize">The expected uncompressed size.</param>
    /// <returns>The decompressed data.</returns>
    public static byte[] Decompress(byte[] compressedData, int expectedSize)
    {
        if (compressedData.Length < 2)
            throw new PackageFormatException("Compressed data too short.");

        // Detect compression type from header bytes
        byte byte0 = compressedData[0];
        byte byte1 = compressedData[1];

        if (byte0 == 0x78)
        {
            // ZLIB/DEFLATE compression (most common in Sims 4)
            return DecompressZlib(compressedData, expectedSize);
        }
        else if (byte1 == 0xFB)
        {
            // RefPack/QFS compression (legacy EA format)
            return DecompressRefPack(compressedData, byte0);
        }
        else
        {
            throw new PackageFormatException(
                $"Unknown compression format: header bytes 0x{byte0:X2} 0x{byte1:X2}");
        }
    }

    /// <summary>
    /// Decompresses ZLIB/DEFLATE data.
    /// </summary>
    private static byte[] DecompressZlib(byte[] compressedData, int expectedSize)
    {
        // ZLIB format has 2-byte header, then DEFLATE data
        using var compressedStream = new MemoryStream(compressedData);

        // Skip ZLIB header (2 bytes) - ZLibStream handles this automatically
        using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);

        var result = new byte[expectedSize];
        int totalRead = 0;

        while (totalRead < expectedSize)
        {
            int bytesRead = zlibStream.Read(result, totalRead, expectedSize - totalRead);
            if (bytesRead == 0)
                throw new PackageFormatException(
                    $"Unexpected end of compressed data. Expected {expectedSize} bytes, got {totalRead}.");
            totalRead += bytesRead;
        }

        return result;
    }

    /// <summary>
    /// Decompresses RefPack/QFS data (legacy EA compression).
    /// </summary>
    private static byte[] DecompressRefPack(byte[] compressed, byte compressionType)
    {
        int offset = 1; // Skip the already-read byte1 (0xFB marker)
        bool isLargeSize = compressionType != 0x80;

        // Validate we have enough bytes for the header
        int headerBytesNeeded = isLargeSize ? 4 : 5; // 1 (already read) + 3 or 4 for size
        if (compressed.Length < headerBytesNeeded)
            throw new PackageFormatException(
                $"RefPack data too short for header. Need {headerBytesNeeded} bytes, got {compressed.Length}.");

        // Read uncompressed size (big-endian, 3 or 4 bytes depending on type)
        int uncompressedSize;
        if (isLargeSize)
        {
            // 3-byte size (compression type encodes high bits)
            uncompressedSize =
                (compressed[offset] << 16) |
                (compressed[offset + 1] << 8) |
                compressed[offset + 2];
            offset += 3;
        }
        else
        {
            // 4-byte size
            uncompressedSize =
                (compressed[offset] << 24) |
                (compressed[offset + 1] << 16) |
                (compressed[offset + 2] << 8) |
                compressed[offset + 3];
            offset += 4;
        }

        // Validate uncompressed size before allocation to prevent OOM attacks
        if (uncompressedSize < 0 || uncompressedSize > PackageLimits.MaxResourceSize)
            throw new PackageFormatException(
                $"RefPack uncompressed size {uncompressedSize} exceeds maximum allowed {PackageLimits.MaxResourceSize}.");

        var result = new byte[uncompressedSize];
        int resultPos = 0;

        while (resultPos < uncompressedSize && offset < compressed.Length)
        {
            byte controlByte = compressed[offset++];

            if (controlByte <= 0x7F)
            {
                // 2-byte command: copy 0-3 plain + 3-10 back-reference
                if (offset >= compressed.Length) break;
                byte byte1 = compressed[offset++];

                int plainCount = controlByte & 0x03;
                int copyCount = ((controlByte >> 2) & 0x07) + 3;
                int copyOffset = ((controlByte & 0x60) << 3) + byte1 + 1;

                CopyPlain(compressed, ref offset, result, ref resultPos, plainCount);
                CopyBackReference(result, ref resultPos, copyOffset, copyCount);
            }
            else if (controlByte <= 0xBF)
            {
                // 3-byte command: copy 0-3 plain + 4-67 back-reference
                if (offset + 1 >= compressed.Length) break;
                byte byte1 = compressed[offset++];
                byte byte2 = compressed[offset++];

                int plainCount = (byte1 >> 6) & 0x03;
                int copyCount = (controlByte & 0x3F) + 4;
                int copyOffset = ((byte1 & 0x3F) << 8) + byte2 + 1;

                CopyPlain(compressed, ref offset, result, ref resultPos, plainCount);
                CopyBackReference(result, ref resultPos, copyOffset, copyCount);
            }
            else if (controlByte <= 0xDF)
            {
                // 4-byte command: copy 0-3 plain + 5-1028 back-reference
                if (offset + 2 >= compressed.Length) break;
                byte byte1 = compressed[offset++];
                byte byte2 = compressed[offset++];
                byte byte3 = compressed[offset++];

                int plainCount = controlByte & 0x03;
                int copyCount = ((controlByte & 0x0C) << 6) + byte3 + 5;
                int copyOffset = ((controlByte & 0x10) << 12) + (byte1 << 8) + byte2 + 1;

                CopyPlain(compressed, ref offset, result, ref resultPos, plainCount);
                CopyBackReference(result, ref resultPos, copyOffset, copyCount);
            }
            else if (controlByte <= 0xFB)
            {
                // Plain text run: 4-112 bytes
                int plainCount = ((controlByte & 0x1F) << 2) + 4;
                CopyPlain(compressed, ref offset, result, ref resultPos, plainCount);
            }
            else
            {
                // End marker: 0-3 final bytes
                int plainCount = controlByte & 0x03;
                CopyPlain(compressed, ref offset, result, ref resultPos, plainCount);
            }
        }

        if (resultPos != uncompressedSize)
            throw new PackageFormatException(
                $"RefPack decompression size mismatch. Expected {uncompressedSize}, got {resultPos}.");

        return result;
    }

    private static void CopyPlain(
        byte[] source, ref int sourceOffset,
        byte[] dest, ref int destOffset,
        int count)
    {
        for (int i = 0; i < count && sourceOffset < source.Length && destOffset < dest.Length; i++)
        {
            dest[destOffset++] = source[sourceOffset++];
        }
    }

    private static void CopyBackReference(
        byte[] buffer, ref int position,
        int offset, int count)
    {
        int sourcePos = position - offset;
        for (int i = 0; i < count && position < buffer.Length; i++)
        {
            buffer[position++] = buffer[sourcePos + i];
        }
    }
}
