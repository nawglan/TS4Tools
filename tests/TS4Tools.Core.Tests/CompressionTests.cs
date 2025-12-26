using FluentAssertions;
using TS4Tools.Compression;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="Compressor"/> and <see cref="Decompressor"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Compression.cs
/// - Sims 4 uses ZLIB compression (header 0x78 9C or 0x78 DA)
/// - Legacy also supports RefPack (header with 0xFB byte) for Sims 3 compatibility
/// - ZLIB header byte 0x78 indicates DEFLATE compression
/// - Compression type in index: 0x5A42 ("ZB") for ZLIB, 0x0000 for none
/// - Legacy uses SharpZipLib's InflaterInputStream for decompression
/// - Our implementation uses .NET's built-in DeflateStream wrapped in ZLIB handling
/// </summary>
public class CompressionTests
{
    [Fact]
    public void Compress_EmptyData_ReturnsEmptyNotCompressed()
    {
        var empty = ReadOnlySpan<byte>.Empty;

        var (data, isCompressed) = Compressor.Compress(empty);

        data.Should().BeEmpty();
        isCompressed.Should().BeFalse();
    }

    [Fact]
    public void Compress_SmallData_ReturnsOriginalOrSmaller()
    {
        // Small random data usually doesn't compress well due to ZLIB header overhead.
        // The compressor should return the original data if compression doesn't help.
        var small = new byte[] { 1, 2, 3, 4, 5 };

        var (data, isCompressed) = Compressor.Compress(small);

        // Either compressed or returned original - both are valid outcomes
        if (isCompressed)
        {
            // If compressed, should be valid ZLIB (though rare for small data)
            data[0].Should().Be(0x78, "compressed data should have ZLIB header");
            // Round-trip should work
            var decompressed = Decompressor.Decompress(data, small.Length);
            decompressed.Should().BeEquivalentTo(small);
        }
        else
        {
            // If not compressed, should return original data unchanged
            data.Should().BeEquivalentTo(small);
        }
    }

    [Fact]
    public void Compress_CompressibleData_ReducesSize()
    {
        // Highly repetitive data compresses well
        var repetitive = new byte[1000];
        Array.Fill(repetitive, (byte)'A');

        var (compressed, isCompressed) = Compressor.Compress(repetitive);

        isCompressed.Should().BeTrue();
        compressed.Length.Should().BeLessThan(repetitive.Length);
    }

    [Fact]
    public void CompressAndDecompress_RoundTrip_PreservesData()
    {
        // Use data that is guaranteed to compress (repetitive pattern)
        var original = new byte[500];
        Array.Fill(original, (byte)0xAB);

        var (compressed, isCompressed) = Compressor.Compress(original);

        // Repetitive data should always compress
        isCompressed.Should().BeTrue("repetitive data should always compress");
        compressed.Length.Should().BeLessThan(original.Length);

        var decompressed = Decompressor.Decompress(compressed, original.Length);
        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CompressAndDecompress_SequentialData_PreservesData()
    {
        // Sequential data compresses reasonably well
        var original = new byte[500];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i % 256);
        }

        var (compressed, isCompressed) = Compressor.Compress(original);

        // Sequential data should compress (has patterns)
        isCompressed.Should().BeTrue("sequential data should compress");

        var decompressed = Decompressor.Decompress(compressed, original.Length);
        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Decompress_ZlibData_Works()
    {
        // Use repetitive data to guarantee compression
        var original = new byte[200];
        Array.Fill(original, (byte)0xCC);

        var (compressed, isCompressed) = Compressor.Compress(original);

        // Repetitive data must compress
        isCompressed.Should().BeTrue("repetitive data should always compress");

        // Verify ZLIB header (0x78 = deflate with 32K window)
        compressed[0].Should().Be(0x78, "ZLIB data should start with 0x78 header");

        var decompressed = Decompressor.Decompress(compressed, original.Length);
        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Decompress_TooShort_ThrowsException()
    {
        var tooShort = new byte[] { 0x78 };

        var act = () => Decompressor.Decompress(tooShort, 100);

        act.Should().Throw<PackageFormatException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Decompress_UnknownFormat_ThrowsException()
    {
        var unknown = new byte[] { 0xFF, 0xFE, 0x00, 0x00 };

        var act = () => Decompressor.Decompress(unknown, 100);

        act.Should().Throw<PackageFormatException>()
            .WithMessage("*Unknown compression*");
    }

    [Fact]
    public void Compressor_Constants_AreCorrect()
    {
        Compressor.ZlibCompressionType.Should().Be(0x5A42); // "ZB"
        Compressor.NoCompression.Should().Be(0x0000);
    }
}
