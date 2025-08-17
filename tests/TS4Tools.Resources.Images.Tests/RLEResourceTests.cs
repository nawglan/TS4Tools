using System.Buffers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TS4Tools.Resources.Common;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for the RLEResource class.
/// </summary>
public sealed class RLEResourceTests : IDisposable
{
    private readonly List<RLEResource> _disposables;

    public RLEResourceTests()
    {
        _disposables = new List<RLEResource>();
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

    private RLEResource TrackResource(RLEResource resource)
    {
        _disposables.Add(resource);
        return resource;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithApiVersionOnly_ShouldInitializeCorrectly()
    {
        // Act
        var resource = TrackResource(new RLEResource(2, null));

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(2);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.MipCount.Should().Be(0);
        resource.Version.Should().Be(RLEVersion.Unknown);
    }

    [Fact]
    public void Constructor_WithNegativeApiVersion_ShouldStillInitialize()
    {
        // Act & Assert - The current implementation doesn't validate API version
        var action = () => new RLEResource(-1, null);
        action.Should().NotThrow();

        var resource = TrackResource(new RLEResource(-1, null));
        resource.RequestedApiVersion.Should().Be(-1);
    }

    [Fact]
    public void Constructor_WithValidStream_ShouldParseRLEData()
    {
        // Arrange
        var testRLEData = TestRLEDataGenerator.CreateValidRLE2Data();

        // Act
        using var stream = new MemoryStream(testRLEData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(1);
        resource.Width.Should().BeGreaterThan(0);
        resource.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new RLEResource(1, null);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptyStream_ShouldHandleGracefully()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act & Assert
        var action = () => new RLEResource(1, emptyStream);
        action.Should().NotThrow();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Width_WithValidRLEData_ShouldReturnCorrectValue()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateRLEWithKnownDimensions(64, 64);
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        resource.Width.Should().Be(64);
    }

    [Fact]
    public void Height_WithValidRLEData_ShouldReturnCorrectValue()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateRLEWithKnownDimensions(32, 128);
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        resource.Height.Should().Be(128);
    }

    [Fact]
    public void Version_WithRLE2Data_ShouldReturnRLE2()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        resource.Version.Should().Be(RLEVersion.RLE2);
    }

    [Fact]
    public void Version_WithRLESData_ShouldReturnRLES()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLESData();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        resource.Version.Should().Be(RLEVersion.RLES);
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_WithValidResource_ShouldReturnStreamWithData()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var originalStream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, originalStream));

        // Act
        var stream = resource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public void AsBytes_WithValidResource_ShouldReturnByteArray()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act
        var bytes = resource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ResourceChanged_InitialValue_ShouldBeFalse()
    {
        // Arrange & Act
        var resource = TrackResource(new RLEResource(1, null));
        bool eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void ContentFields_ShouldNotBeNull()
    {
        // Arrange & Act
        var resource = TrackResource(new RLEResource(1, null));

        // Assert
        resource.ContentFields.Should().NotBeNull();
    }

    [Fact]
    public void ContentFields_IndexAccess_ShouldReturnCorrectValues()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        var versionField = resource[0];
        versionField.Value.Should().Be(resource.Version);

        var widthField = resource[1];
        widthField.Value.Should().Be((uint)resource.Width);

        var heightField = resource[2];
        heightField.Value.Should().Be((uint)resource.Height);
    }

    [Fact]
    public void ContentFields_NameAccess_ShouldReturnCorrectValues()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act & Assert
        var versionField = resource[nameof(RLEResource.Version)];
        versionField.Value.Should().Be(resource.Version);

        var widthField = resource[nameof(RLEResource.Width)];
        widthField.Value.Should().Be((uint)resource.Width);
    }

    [Fact]
    public void ContentFields_SetValue_ShouldThrowNotSupportedException()
    {
        // Arrange
        var resource = TrackResource(new RLEResource(1, null));

        // Act & Assert
        var action = () => resource[0] = new TypedValue(typeof(uint), 42u);
        action.Should().Throw<NotSupportedException>()
            .WithMessage("RLE resource fields are read-only");
    }

    [Fact]
    public void ContentFields_InvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var resource = TrackResource(new RLEResource(1, null));

        // Act & Assert
        var action = () => resource[99];
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ContentFields_InvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var resource = TrackResource(new RLEResource(1, null));

        // Act & Assert
        var action = () => resource["InvalidFieldName"];
        action.Should().Throw<ArgumentException>()
            .WithMessage("Unknown field name: InvalidFieldName*");
    }

    #endregion

    #region RLE Specific Tests

    [Fact]
    public async Task ToDDSAsync_WithValidRLE2Data_ShouldReturnDDSData()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act
        var ddsStream = await resource.ToDDSAsync();

        // Assert
        ddsStream.Should().NotBeNull();
        ddsStream.Length.Should().BeGreaterThan(0);

        // The RLE implementation should produce some kind of output stream
        // The actual DDS conversion is complex and requires real RLE data
        // For now, just verify we get a stream back
        ddsStream.Should().BeOfType<MemoryStream>();
    }

    [Fact]
    public void ToDDS_WithValidRLE2Data_ShouldReturnDDSData()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act
        var ddsStream = resource.ToDDS();

        // Assert
        ddsStream.Should().NotBeNull();
        ddsStream.Length.Should().BeGreaterThan(0);
        ddsStream.Should().BeOfType<MemoryStream>();
    }

    [Fact]
    public void GetRawData_ShouldReturnOriginalData()
    {
        // Arrange
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);
        var resource = TrackResource(new RLEResource(1, stream));

        // Act
        var rawData = resource.GetRawData();

        // Assert
        rawData.Length.Should().BeGreaterThan(0);
        rawData.ToArray().Should().Equal(testData);
    }

    [Fact]
    public async Task SetDataAsync_WithValidData_ShouldUpdateResource()
    {
        // Arrange
        var resource = TrackResource(new RLEResource(1, null));
        var testData = TestRLEDataGenerator.CreateValidRLE2Data();
        using var stream = new MemoryStream(testData);

        // Act
        await resource.SetDataAsync(stream);

        // Assert
        resource.Width.Should().BeGreaterThan(0);
        resource.Height.Should().BeGreaterThan(0);
        resource.Version.Should().Be(RLEVersion.RLE2);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var resource = new RLEResource(1, null);

        // Act & Assert
        var action = () => resource.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var resource = new RLEResource(1, null);

        // Act & Assert
        resource.Dispose();
        var action = () => resource.Dispose();
        action.Should().NotThrow();
    }

    #endregion
}

/// <summary>
/// Helper class for generating test RLE data.
/// </summary>
internal static class TestRLEDataGenerator
{
    public static byte[] CreateValidRLE2Data()
    {
        return CreateRLEWithKnownDimensions(64, 64);
    }

    public static byte[] CreateValidRLESData()
    {
        return CreateRLEData(32, 32, RLEVersion.RLES);
    }

    public static byte[] CreateRLEWithKnownDimensions(uint width, uint height)
    {
        return CreateRLEData(width, height, RLEVersion.RLE2);
    }

    public static byte[] CreateRLEWithMips()
    {
        return CreateRLEData(64, 64, RLEVersion.RLE2, mipCount: 3);
    }

    private static byte[] CreateRLEData(uint width, uint height, RLEVersion version, uint mipCount = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write RLE header
        writer.Write(width);
        writer.Write(height);
        writer.Write(mipCount);
        writer.Write((int)version);
        writer.Write(0x15u); // DXT5 pixel format

        // Write mip headers
        for (int i = 0; i < mipCount; i++)
        {
            writer.Write(0); // CommandOffset
            writer.Write(0); // Offset0
            writer.Write(0); // Offset1
            writer.Write(0); // Offset2
            writer.Write(0); // Offset3

            if (version == RLEVersion.RLES)
            {
                writer.Write(0); // Offset4 for RLES
            }
        }

        // Write minimal RLE data (empty for now)
        var pixelDataSize = (int)(width * height / 2); // DXT5 compressed size approximation
        writer.Write(new byte[pixelDataSize]);

        return stream.ToArray();
    }
}
