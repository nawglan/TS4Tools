using FluentAssertions;
using TS4Tools.Compression;
using Xunit;

namespace TS4Tools.Core.Tests;

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
    public void Compress_SmallData_MayNotCompress()
    {
        // Small random data usually doesn't compress well
        var small = new byte[] { 1, 2, 3, 4, 5 };

        var (data, isCompressed) = Compressor.Compress(small);

        // Either compressed or returned original
        if (!isCompressed)
        {
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
        var original = new byte[500];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i % 256);
        }

        var (compressed, isCompressed) = Compressor.Compress(original);

        if (isCompressed)
        {
            var decompressed = Decompressor.Decompress(compressed, original.Length);
            decompressed.Should().BeEquivalentTo(original);
        }
    }

    [Fact]
    public void Decompress_ZlibData_Works()
    {
        // Create test data and compress it
        var original = new byte[200];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)((i * 17) % 256);
        }

        var (compressed, isCompressed) = Compressor.Compress(original);

        if (isCompressed)
        {
            // Verify ZLIB header (0x78)
            compressed[0].Should().Be(0x78);

            var decompressed = Decompressor.Decompress(compressed, original.Length);
            decompressed.Should().BeEquivalentTo(original);
        }
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
