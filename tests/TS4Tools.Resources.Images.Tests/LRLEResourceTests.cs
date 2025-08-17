using System.Buffers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TS4Tools.Resources.Common;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for the LRLEResource class.
/// Tests cover LRLE (Lossless Run-Length Encoded) image resource functionality.
/// </summary>
public sealed class LRLEResourceTests : IDisposable
{
    private readonly List<LRLEResource> _disposables;

    public LRLEResourceTests()
    {
        _disposables = new List<LRLEResource>();
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
        GC.SuppressFinalize(this);
    }

    private LRLEResource TrackResource(LRLEResource resource)
    {
        _disposables.Add(resource);
        return resource;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithApiVersionOnly_ShouldInitializeCorrectly()
    {
        // Act
        var resource = TrackResource(new LRLEResource());

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(1);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.MipCount.Should().Be(0);
        resource.Version.Should().Be(LRLEVersion.Unknown);
        resource.Magic.Should().Be(0x454C524C); // 'LRLE'
        resource.ColorCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldInitializeWithDefaults()
    {
        // Act
        var resource = TrackResource(new LRLEResource());

        // Assert
        resource.Should().NotBeNull();
        resource.AsBytes.Should().NotBeNull();
        resource.AsBytes.Should().BeEmpty();
        resource.Stream.Should().NotBeNull();
        resource.Stream.Length.Should().Be(0);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var resource = new LRLEResource();
        resource.Dispose();

        // Act & Assert
        resource.Invoking(r => _ = r.AsBytes)
            .Should().Throw<ObjectDisposedException>();

        resource.Invoking(r => _ = r.Stream)
            .Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ContentFields_ShouldContainExpectedFields()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act
        var fields = resource.ContentFields;

        // Assert
        fields.Should().NotBeNull();
        fields.Should().Contain("Version");
        fields.Should().Contain("Width");
        fields.Should().Contain("Height");
        fields.Should().Contain("Magic");
        fields.Should().Contain("MipCount");
        fields.Should().Contain("ColorCount");
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("Width")]
    [InlineData("Height")]
    [InlineData("Magic")]
    [InlineData("MipCount")]
    [InlineData("ColorCount")]
    public void Indexer_ByName_ShouldReturnCorrectTypedValue(string fieldName)
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act
        var value = resource[fieldName];

        // Assert
        value.Should().NotBeNull();
        value.Type.Should().NotBeNull();
        value.Value.Should().NotBeNull();
    }

    [Fact]
    public void Indexer_ByName_WithInvalidField_ShouldThrowArgumentException()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act & Assert
        resource.Invoking(r => _ = r["InvalidField"])
            .Should().Throw<ArgumentException>()
            .WithMessage("Unknown field name: InvalidField*");
    }

    [Theory]
    [InlineData(0)] // Version
    [InlineData(1)] // Width
    [InlineData(2)] // Height
    [InlineData(3)] // Magic
    [InlineData(4)] // MipCount
    [InlineData(5)] // ColorCount
    public void Indexer_ByIndex_ShouldReturnCorrectTypedValue(int index)
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act
        var value = resource[index];

        // Assert
        value.Should().NotBeNull();
        value.Type.Should().NotBeNull();
        value.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Indexer_ByIndex_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act & Assert
        resource.Invoking(r => _ = r[index])
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_Set_ShouldThrowNotSupportedException()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var typedValue = new TypedValue(typeof(uint), 123u);

        // Act & Assert
        resource.Invoking(r => r["Version"] = typedValue)
            .Should().Throw<NotSupportedException>()
            .WithMessage("LRLE resource fields are read-only");

        resource.Invoking(r => r[0] = typedValue)
            .Should().Throw<NotSupportedException>()
            .WithMessage("LRLE resource fields are read-only");
    }

    #endregion

    #region SetDataAsync Tests

    [Fact]
    public async Task SetDataAsync_WithValidLRLEData_ShouldLoadCorrectly()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEData(64, 64, LRLEVersion.Version2);
        using var stream = new MemoryStream(lrleData);

        // Act
        await resource.SetDataAsync(stream);

        // Assert
        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
        resource.Version.Should().Be(LRLEVersion.Version2);
        resource.Magic.Should().Be(0x454C524C); // 'LRLE'
        resource.AsBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetDataAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());

        // Act & Assert
        await resource.Invoking(async r => await r.SetDataAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetDataAsync_WithInvalidMagic_ShouldThrowInvalidDataException()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var invalidData = new byte[16];
        // Set invalid magic number
        BitConverter.GetBytes(0x12345678).CopyTo(invalidData, 0);
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        await resource.Invoking(async r => await r.SetDataAsync(stream))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*magic number*");
    }

    [Fact]
    public async Task SetDataAsync_WithTooSmallData_ShouldThrowInvalidDataException()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var tooSmallData = new byte[8]; // Less than minimum header size
        using var stream = new MemoryStream(tooSmallData);

        // Act & Assert
        await resource.Invoking(async r => await r.SetDataAsync(stream))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*insufficient data*");
    }

    #endregion

    #region ToBitmapAsync Tests

    [Fact]
    public async Task ToBitmapAsync_WithValidData_ShouldReturnBitmapStream()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEData(32, 32, LRLEVersion.Version2);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act
        using var bitmapStream = await resource.ToBitmapAsync(0); // Explicitly call the interface method

        // Assert
        bitmapStream.Should().NotBeNull();
        bitmapStream.Length.Should().BeGreaterThan(0);
        bitmapStream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task ToBitmapAsync_WithSpecificMipLevel_ShouldReturnCorrectSize()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEDataWithMips(64, 64, 3);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act
        using var level0Stream = await resource.ToBitmapAsync(0);
        using var level1Stream = await resource.ToBitmapAsync(1);

        // Assert
        level0Stream.Should().NotBeNull();
        level0Stream.Length.Should().BeGreaterThan(0);
        level1Stream.Should().NotBeNull();
        level1Stream.Length.Should().BeGreaterThan(0);
        // Mip level 1 should generally be smaller than level 0
        level1Stream.Length.Should().BeLessOrEqualTo(level0Stream.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task ToBitmapAsync_WithInvalidMipLevel_ShouldThrowArgumentOutOfRangeException(int mipLevel)
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEData(32, 32, LRLEVersion.Version2);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act & Assert
        await resource.Invoking(async r => await r.ToBitmapAsync(mipLevel))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Synchronous ToBitmap Tests

    [Fact]
    public async Task ToBitmap_WithValidData_ShouldReturnBitmapStream()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEData(32, 32, LRLEVersion.Version2);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act
        using var bitmapStream = resource.ToBitmap();

        // Assert
        bitmapStream.Should().NotBeNull();
        bitmapStream.Length.Should().BeGreaterThan(0);
        bitmapStream.CanRead.Should().BeTrue();
    }

    #endregion

    #region GetRawData Tests

    [Fact]
    public async Task GetRawData_WithValidData_ShouldReturnCorrectSpan()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEData(16, 16, LRLEVersion.Version2);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act
        var rawData = resource.GetRawData();

        // Assert
        // Note: Suppressing CS8602 because ReadOnlySpan<byte> is a value type that cannot be null,
        // but the compiler's static analysis incorrectly infers it could be null due to the
        // complex data flow through the LRLEResource implementation. ReadOnlySpan<T> is a ref struct
        // that represents a contiguous region of memory and cannot hold a null reference.
        // The .Length property access is always safe on ReadOnlySpan<byte>.
#pragma warning disable CS8602 // Dereference of a possibly null reference - ReadOnlySpan<byte> cannot actually be null
        rawData.Length.Should().BeGreaterThan(0);
#pragma warning restore CS8602 // Dereference of a possibly null reference
    }

    [Fact]
    public async Task GetRawData_WithSpecificMipLevel_ShouldReturnCorrectData()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var lrleData = CreateValidLRLEDataWithMips(64, 64, 2);
        using var inputStream = new MemoryStream(lrleData);
        await resource.SetDataAsync(inputStream);

        // Act
        var level0Data = resource.GetRawData(0);
        var level1Data = resource.GetRawData(1);

        // Assert
        level0Data.Length.Should().BeGreaterThan(0);
        level1Data.Length.Should().BeGreaterThan(0);
        // Level 1 should generally be smaller than level 0
        level1Data.Length.Should().BeLessOrEqualTo(level0Data.Length);
    }

    #endregion

    #region CreateFromImageAsync Tests

    [Fact]
    public async Task CreateFromImageAsync_WithValidBitmap_ShouldCreateLRLEData()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var bitmapData = CreateValidBitmapData(32, 32);
        using var bitmapStream = new MemoryStream(bitmapData);

        // Act
        await resource.CreateFromImageAsync(bitmapStream, generateMipmaps: false);

        // Assert
        resource.Width.Should().Be(32);
        resource.Height.Should().Be(32);
        resource.Magic.Should().Be(0x454C524C);
        resource.AsBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateFromImageAsync_WithMipmapGeneration_ShouldCreateMultipleLevels()
    {
        // Arrange
        var resource = TrackResource(new LRLEResource());
        var bitmapData = CreateValidBitmapData(64, 64);
        using var bitmapStream = new MemoryStream(bitmapData);

        // Act
        await resource.CreateFromImageAsync(bitmapStream, generateMipmaps: true);

        // Assert
        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
        resource.MipCount.Should().BeGreaterThan(1);
        resource.AsBytes.Should().NotBeEmpty();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var resource = new LRLEResource();

        // Act & Assert
        resource.Invoking(r => r.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var resource = new LRLEResource();

        // Act & Assert
        resource.Invoking(r =>
        {
            r.Dispose();
            r.Dispose();
            r.Dispose();
        }).Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid LRLE data array for testing.
    /// </summary>
    private static byte[] CreateValidLRLEData(ushort width, ushort height, LRLEVersion version)
    {
        var data = new List<byte>();

        // LRLE Magic: 'LRLE'
        data.AddRange(BitConverter.GetBytes(0x454C524C));

        // Version
        data.AddRange(BitConverter.GetBytes((uint)version));

        // Width and Height
        data.AddRange(BitConverter.GetBytes(width));
        data.AddRange(BitConverter.GetBytes(height));

        // MipMap count
        data.Add(1);

        // Reserved bytes
        data.AddRange(new byte[3]);

        // Color count
        data.AddRange(BitConverter.GetBytes((uint)256));

        // Add minimal palette data (256 colors * 4 bytes RGBA)
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
        var compressedData = new byte[imageSize / 4]; // Assume 4:1 compression ratio
        for (int i = 0; i < compressedData.Length; i++)
        {
            compressedData[i] = (byte)(i % 256);
        }
        data.AddRange(compressedData);

        return data.ToArray();
    }

    /// <summary>
    /// Creates a valid LRLE data array with multiple mip levels for testing.
    /// </summary>
    private static byte[] CreateValidLRLEDataWithMips(ushort width, ushort height, byte mipCount)
    {
        var data = new List<byte>();

        // LRLE Magic: 'LRLE'
        data.AddRange(BitConverter.GetBytes(0x454C524C));

        // Version
        data.AddRange(BitConverter.GetBytes((uint)LRLEVersion.Version2));

        // Width and Height
        data.AddRange(BitConverter.GetBytes(width));
        data.AddRange(BitConverter.GetBytes(height));

        // MipMap count
        data.Add(mipCount);

        // Reserved bytes
        data.AddRange(new byte[3]);

        // Color count
        data.AddRange(BitConverter.GetBytes((uint)256));

        // Add palette data
        var paletteData = new byte[256 * 4];
        for (int i = 0; i < 256; i++)
        {
            paletteData[i * 4] = (byte)i;
            paletteData[i * 4 + 1] = (byte)i;
            paletteData[i * 4 + 2] = (byte)i;
            paletteData[i * 4 + 3] = 255;
        }
        data.AddRange(paletteData);

        // Add compressed data for each mip level
        for (int mip = 0; mip < mipCount; mip++)
        {
            var mipWidth = Math.Max(1, width >> mip);
            var mipHeight = Math.Max(1, height >> mip);
            var mipSize = mipWidth * mipHeight;
            var compressedSize = Math.Max(16, mipSize / 4);

            var mipData = new byte[compressedSize];
            for (int i = 0; i < mipData.Length; i++)
            {
                mipData[i] = (byte)((i + mip) % 256);
            }
            data.AddRange(mipData);
        }

        return data.ToArray();
    }

    /// <summary>
    /// Creates a valid bitmap data array for testing.
    /// </summary>
    private static byte[] CreateValidBitmapData(int width, int height)
    {
        // Create a proper PNG image using ImageSharp to ensure valid format
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height);

        // Fill with a simple gradient pattern
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)((x * 255) / width);
                byte g = (byte)((y * 255) / height);
                byte b = (byte)((x + y) * 128 / (width + height));
                image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(r, g, b, 255);
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    #endregion
}
