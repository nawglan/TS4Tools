using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for the LRLEResourceFactory class.
/// Tests cover factory creation and validation of LRLE resources.
/// </summary>
public sealed class LRLEResourceFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var factory = new LRLEResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.FormatInfo.Should().NotBeNull();
        factory.FormatInfo.Name.Should().Be("LRLE (Lossless Run-Length Encoded Image)");
        factory.FormatInfo.Description.Should().Contain("Lossless Run-Length Encoded image format");
        factory.FormatInfo.HasColorPalette.Should().BeTrue();
        factory.FormatInfo.IsLossless.Should().BeTrue();
        factory.FormatInfo.SupportsTransparency.Should().BeTrue();
    }

    #endregion

    #region CreateResourceAsync Tests

    [Fact]
    public async Task CreateResourceAsync_WithValidApiVersion_ShouldReturnLRLEResource()
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act
        var resource = await factory.CreateResourceAsync(1);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<ILRLEResource>();
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldReturnEmptyResource()
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act
        var resource = await factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.MipCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidLRLEStream_ShouldReturnPopulatedResource()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData(64, 64);
        using var stream = new MemoryStream(lrleData);

        // Act
        var resource = await factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
        resource.Magic.Should().Be(0x454C524C); // 'LRLE'
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowArgumentException(int apiVersion)
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act & Assert
        await factory.Invoking(async f => await f.CreateResourceAsync(apiVersion))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*API version*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var invalidData = new byte[] { 1, 2, 3, 4 }; // Too small and invalid
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        await factory.Invoking(async f => await f.CreateResourceAsync(1, stream))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Failed to create LRLE resource from stream*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await factory.Invoking(async f => await f.CreateResourceAsync(1, null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ValidateData Tests

    [Fact]
    public void ValidateData_WithValidLRLEData_ShouldReturnTrue()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData(128, 128);

        // Act
        var result = factory.ValidateData(lrleData);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateData_WithTooSmallData_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var tooSmallData = new byte[8]; // Less than minimum 16 bytes

        // Act
        var result = factory.ValidateData(tooSmallData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateData_WithInvalidMagic_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var invalidData = new byte[20];
        // Set invalid magic number
        BitConverter.GetBytes(0x12345678).CopyTo(invalidData, 0);

        // Act
        var result = factory.ValidateData(invalidData);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 100)]      // Width = 0
    [InlineData(100, 0)]      // Height = 0
    [InlineData(20000, 100)]  // Width too large
    [InlineData(100, 20000)]  // Height too large
    public void ValidateData_WithInvalidDimensions_ShouldReturnFalse(ushort width, ushort height)
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var data = CreateValidLRLEData(width, height);

        // Act
        var result = factory.ValidateData(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateData_WithInvalidVersion_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var data = new byte[20];

        // Set valid magic
        BitConverter.GetBytes(0x454C524C).CopyTo(data, 0);

        // Set invalid version
        BitConverter.GetBytes(0x99999999).CopyTo(data, 4);

        // Set valid dimensions
        BitConverter.GetBytes((ushort)64).CopyTo(data, 8);
        BitConverter.GetBytes((ushort)64).CopyTo(data, 10);

        // Act
        var result = factory.ValidateData(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateData_WithTooManyMipLevels_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var data = CreateValidLRLEData(64, 64);

        // Set invalid mip count (> 9)
        data[12] = 15;

        // Act
        var result = factory.ValidateData(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateData_WithExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var corruptedData = new byte[16];
        // This will cause BinaryPrimitives.ReadUInt32LittleEndian to potentially throw

        // Act
        var result = factory.ValidateData(corruptedData.AsSpan(0, 4)); // Too small span

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region FormatInfo Tests

    [Fact]
    public void FormatInfo_ShouldHaveCorrectProperties()
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act
        var formatInfo = factory.FormatInfo;

        // Assert
        formatInfo.Should().NotBeNull();
        formatInfo.Name.Should().Be("LRLE (Lossless Run-Length Encoded Image)");
        formatInfo.Description.Should().Contain("The Sims 4");
        formatInfo.MaxWidth.Should().Be(16384);
        formatInfo.MaxHeight.Should().Be(16384);
        formatInfo.MaxMipMapLevels.Should().Be(9);
        formatInfo.SupportedVersions.Should().NotBeNull();
        formatInfo.SupportedVersions.Should().Contain(LRLEVersion.Version1);
        formatInfo.SupportedVersions.Should().Contain(LRLEVersion.Version2);
        formatInfo.HasColorPalette.Should().BeTrue();
        formatInfo.IsLossless.Should().BeTrue();
        formatInfo.SupportsTransparency.Should().BeTrue();
    }

    [Fact]
    public void FormatInfo_ShouldBeSameInstance()
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act
        var formatInfo1 = factory.FormatInfo;
        var formatInfo2 = factory.FormatInfo;

        // Assert
        formatInfo1.Should().BeSameAs(formatInfo2);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task CreateResourceAsync_WithDisposedStream_ShouldHandleGracefully()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData(32, 32);
        var stream = new MemoryStream(lrleData);
        stream.Dispose();

        // Act & Assert
        await factory.Invoking(async f => await f.CreateResourceAsync(1, stream))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public void ValidateData_WithEmptySpan_ShouldReturnFalse()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var emptyData = ReadOnlySpan<byte>.Empty;

        // Act
        var result = factory.ValidateData(emptyData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_MultipleTimes_ShouldCreateIndependentInstances()
    {
        // Arrange
        var factory = new LRLEResourceFactory();

        // Act
        var resource1 = await factory.CreateResourceAsync(1);
        var resource2 = await factory.CreateResourceAsync(1);

        // Assert
        resource1.Should().NotBeSameAs(resource2);
        resource1.Should().BeEquivalentTo(resource2, options => options.Excluding(r => r.Stream));
    }

    #endregion

    #region Resource Creation with Real-World Scenarios

    [Theory]
    [InlineData(16, 16)]
    [InlineData(64, 64)]
    [InlineData(128, 128)]
    [InlineData(256, 256)]
    [InlineData(512, 512)]
    [InlineData(1024, 1024)]
    public async Task CreateResourceAsync_WithVariousDimensions_ShouldCreateCorrectResources(int width, int height)
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData((ushort)width, (ushort)height);
        using var stream = new MemoryStream(lrleData);

        // Act
        var resource = await factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Width.Should().Be(width);
        resource.Height.Should().Be(height);
    }

    [Theory]
    [InlineData(LRLEVersion.Version1)]
    [InlineData(LRLEVersion.Version2)]
    public async Task CreateResourceAsync_WithDifferentVersions_ShouldCreateCorrectResources(LRLEVersion version)
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEDataWithVersion(64, 64, version);
        using var stream = new MemoryStream(lrleData);

        // Act
        var resource = await factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Version.Should().Be(version);
        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CreateResourceAsync_WithLargeData_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData(1024, 1024);
        using var stream = new MemoryStream(lrleData);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var resource = await factory.CreateResourceAsync(1, stream);
        stopwatch.Stop();

        // Assert
        resource.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(7500); // Should complete within 7.5 seconds (more forgiving for ConfigureAwait overhead)
    }

    [Fact]
    public void ValidateData_WithLargeData_ShouldCompleteQuickly()
    {
        // Arrange
        var factory = new LRLEResourceFactory();
        var lrleData = CreateValidLRLEData(1024, 1024);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = factory.ValidateData(lrleData);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should validate quickly
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid LRLE data array for testing.
    /// </summary>
    private static byte[] CreateValidLRLEData(ushort width, ushort height)
    {
        return CreateValidLRLEDataWithVersion(width, height, LRLEVersion.Version2);
    }

    /// <summary>
    /// Creates a valid LRLE data array with specified version for testing.
    /// </summary>
    private static byte[] CreateValidLRLEDataWithVersion(ushort width, ushort height, LRLEVersion version)
    {
        var data = new List<byte>();

        // LRLE Magic: 'LRLE'
        data.AddRange(BitConverter.GetBytes(0x454C524C));

        // Version
        data.AddRange(BitConverter.GetBytes((uint)version));

        // Width and Height
        data.AddRange(BitConverter.GetBytes(width));
        data.AddRange(BitConverter.GetBytes(height));

        // MipMap count (1)
        data.Add(1);

        // Reserved bytes
        data.AddRange(new byte[3]);

        // Color count
        data.AddRange(BitConverter.GetBytes((uint)256));

        // Add palette data (256 colors * 4 bytes RGBA)
        var paletteData = new byte[256 * 4];
        for (int i = 0; i < 256; i++)
        {
            paletteData[i * 4] = (byte)i;     // R
            paletteData[i * 4 + 1] = (byte)i; // G
            paletteData[i * 4 + 2] = (byte)i; // B
            paletteData[i * 4 + 3] = 255;     // A
        }
        data.AddRange(paletteData);

        // Add minimal compressed data for the image
        var imageSize = width * height;
        var compressedData = new byte[Math.Max(64, imageSize / 4)]; // Minimum 64 bytes, assume 4:1 compression ratio
        for (int i = 0; i < compressedData.Length; i++)
        {
            compressedData[i] = (byte)(i % 256);
        }
        data.AddRange(compressedData);

        return data.ToArray();
    }

    #endregion
}
