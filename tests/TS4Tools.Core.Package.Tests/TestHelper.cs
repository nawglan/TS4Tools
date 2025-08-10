using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.Package.Compression;

namespace TS4Tools.Core.Package.Tests;

/// <summary>
/// Helper class for creating test instances and mocks for package tests.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a mock compression service for testing.
    /// </summary>
    /// <returns>A mock compression service that passes data through unmodified.</returns>
    public static ICompressionService CreateMockCompressionService()
    {
        return new MockCompressionService();
    }

    /// <summary>
    /// Creates a real compression service instance for integration testing.
    /// </summary>
    /// <returns>A real compression service instance.</returns>
    public static ICompressionService CreateRealCompressionService()
    {
        var logger = Substitute.For<ILogger<ZlibCompressionService>>();
        return new ZlibCompressionService(logger);
    }
}

/// <summary>
/// Simple mock compression service for testing that simulates compression by removing half the data.
/// </summary>
internal class MockCompressionService : ICompressionService
{
    public byte[] Compress(ReadOnlySpan<byte> data)
    {
        // Simulate compression by returning a smaller array
        var result = new byte[Math.Max(1, data.Length / 2)];
        data.Slice(0, result.Length).CopyTo(result);
        return result;
    }

    public Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Compress(data.Span));
    }

    public byte[] Decompress(ReadOnlySpan<byte> compressedData, int originalSize)
    {
        // Simulate decompression by returning data padded to original size
        var result = new byte[originalSize];
        compressedData.CopyTo(result);
        return result;
    }

    public Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData, int originalSize, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Decompress(compressedData.Span, originalSize));
    }

    public bool IsCompressed(ReadOnlySpan<byte> data)
    {
        return false;
    }

    public double CalculateCompressionRatio(int originalSize, int compressedSize)
    {
        return originalSize == 0 ? 1.0 : (double)compressedSize / originalSize;
    }
}
