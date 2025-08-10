using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Geometry;

namespace TS4Tools.Resources.Geometry.Tests;

/// <summary>
/// Unit tests for GeometryResourceFactory.
/// Tests factory creation, resource type detection, validation, and error handling.
/// </summary>
public class GeometryResourceFactoryTests
{
    private readonly ILogger<GeometryResourceFactory> _logger;
    private readonly GeometryResourceFactory _factory;

    public GeometryResourceFactoryTests()
    {
        _logger = Substitute.For<ILogger<GeometryResourceFactory>>();
        _factory = new GeometryResourceFactory(_logger);
    }

    [Fact]
    public void Constructor_WithValidLogger_SetsProperties()
    {
        var factory = new GeometryResourceFactory(_logger);

        factory.Should().NotBeNull();
        factory.ResourceTypes.Should().Contain(0x015A1849u);
        factory.SupportedResourceTypes.Should().Contain("GEOM");
    }

    [Fact]
    public void ResourceTypes_ContainsExpectedTypes()
    {
        _factory.ResourceTypes.Should().Contain(0x015A1849u);
    }

    [Fact]
    public void SupportedResourceTypes_ContainsExpectedTypes()
    {
        _factory.SupportedResourceTypes.Should().Contain("GEOM");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidStream_ReturnsGeometryResource()
    {
        // Arrange
        var stream = CreateValidGeometryStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GeometryResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _factory.CreateResourceAsync(1, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task CreateResourceAsync_WithEmptyStream_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var act = async () => await _factory.CreateResourceAsync(1, stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream([0x00, 0x01, 0x02, 0x03]);

        // Act
        var act = async () => await _factory.CreateResourceAsync(1, stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithCorruptedData_ThrowsInvalidOperationException()
    {
        // Arrange
        var stream = CreateCorruptedGeometryStream();

        // Act
        var act = async () => await _factory.CreateResourceAsync(1, stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithMinimalValidData_ReturnsResource()
    {
        // Arrange
        var stream = CreateMinimalValidGeometryStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GeometryResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithLargeValidData_ReturnsResource()
    {
        // Arrange
        var stream = CreateLargeValidGeometryStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GeometryResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_LogsCreationStart()
    {
        // Arrange
        var stream = CreateValidGeometryStream();

        // Act
        await _factory.CreateResourceAsync(1, stream);

        // Assert
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Creating geometry resource")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceAsync_LogsCreationSuccess()
    {
        // Arrange
        var stream = CreateValidGeometryStream();

        // Act
        await _factory.CreateResourceAsync(1, stream);

        // Assert
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Created geometry resource")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceAsync_LogsErrorOnFailure()
    {
        // Arrange
        using var stream = new MemoryStream([0x00, 0x01, 0x02, 0x03]);

        // Act
        try
        {
            await _factory.CreateResourceAsync(1, stream);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to create geometry resource")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceAsync_CanHandleMultipleCalls()
    {
        // Arrange
        var stream1 = CreateValidGeometryStream();
        var stream2 = CreateValidGeometryStream();

        // Act
        var result1 = await _factory.CreateResourceAsync(1, stream1);
        var result2 = await _factory.CreateResourceAsync(1, stream2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().NotBeSameAs(result2);
    }

    [Fact]
    public async Task CreateResourceAsync_ResetsStreamPositionAfterReading()
    {
        // Arrange
        var stream = CreateValidGeometryStream();
        var originalPosition = stream.Position;

        // Act
        await _factory.CreateResourceAsync(1, stream);

        // Assert
        stream.Position.Should().Be(originalPosition);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateResourceAsync_WorksWithSeekableAndNonSeekableStreams(bool seekable)
    {
        // Arrange
        var stream = seekable ? CreateValidGeometryStream() : CreateNonSeekableStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
    }

    private static MemoryStream CreateValidGeometryStream()
    {
        var data = new List<byte>();

        // GEOM header - simplified to minimal working format
        data.AddRange(BitConverter.GetBytes(0x47454F4Du)); // "GEOM" tag
        data.AddRange(BitConverter.GetBytes(0x00000005u)); // Version 5 (supported)
        data.AddRange(BitConverter.GetBytes(0u)); // TGI offset
        data.AddRange(BitConverter.GetBytes(0u)); // TGI size
        data.AddRange(BitConverter.GetBytes((uint)ShaderType.None)); // Shader type (0)

        // Geometry properties
        data.AddRange(BitConverter.GetBytes(0u)); // MergeGroup
        data.AddRange(BitConverter.GetBytes(0u)); // SortOrder

        // Vertex data - minimal
        data.AddRange(BitConverter.GetBytes(0)); // VertexCount (0 = no vertices)
        data.AddRange(BitConverter.GetBytes(0)); // Format count (0 = no formats)

        // Face data
        data.AddRange(BitConverter.GetBytes(1u)); // Number of submeshes (must be 1)
        data.Add(2); // Face point size (must be 2)
        data.AddRange(BitConverter.GetBytes(0)); // Face count (0 = no faces)

        // Version 5 specific data
        data.AddRange(BitConverter.GetBytes(0)); // SkinIndex

        // Bone hashes
        data.AddRange(BitConverter.GetBytes(0)); // Bone hash count (0 = no bones)

        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateMinimalValidGeometryStream()
    {
        var data = new List<byte>();

        // Minimal GEOM header - same as valid but even simpler
        data.AddRange(BitConverter.GetBytes(0x47454F4Du)); // "GEOM" tag
        data.AddRange(BitConverter.GetBytes(0x00000005u)); // Version 5 (supported)
        data.AddRange(BitConverter.GetBytes(0u)); // TGI offset
        data.AddRange(BitConverter.GetBytes(0u)); // TGI size
        data.AddRange(BitConverter.GetBytes((uint)ShaderType.None)); // Shader type (0)

        // Geometry properties
        data.AddRange(BitConverter.GetBytes(0u)); // MergeGroup
        data.AddRange(BitConverter.GetBytes(0u)); // SortOrder

        // Vertex data - no vertices
        data.AddRange(BitConverter.GetBytes(0)); // VertexCount (0)
        data.AddRange(BitConverter.GetBytes(0)); // Format count (0)

        // Face data
        data.AddRange(BitConverter.GetBytes(1u)); // Number of submeshes (must be 1)
        data.Add(2); // Face point size (must be 2)
        data.AddRange(BitConverter.GetBytes(0)); // Face count (0)

        // Version 5 specific data
        data.AddRange(BitConverter.GetBytes(0)); // SkinIndex

        // Bone hashes
        data.AddRange(BitConverter.GetBytes(0)); // Bone hash count (0)

        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateLargeValidGeometryStream()
    {
        // Just create a larger version of the valid stream by duplicating it multiple times
        var baseData = CreateValidGeometryStream().ToArray();
        var largeData = new List<byte>();

        // Add the first valid stream
        largeData.AddRange(baseData);

        // Add some padding bytes to make it "large"
        for (int i = 0; i < 1000; i++)
        {
            largeData.AddRange(BitConverter.GetBytes((float)i));
        }

        return new MemoryStream(largeData.ToArray());
    }

    private static MemoryStream CreateCorruptedGeometryStream()
    {
        var data = new List<byte>();

        // Corrupted GEOM header
        data.AddRange(BitConverter.GetBytes(0x4D4F4547u)); // "GEOM" in big-endian (wrong order)
        data.AddRange(BitConverter.GetBytes(1u)); // Version
        data.AddRange(BitConverter.GetBytes((uint)ShaderType.None)); // Shader type
        data.AddRange(BitConverter.GetBytes(3u)); // Vertex count
        data.AddRange(BitConverter.GetBytes(1u)); // Face count
        data.AddRange(BitConverter.GetBytes(1u)); // Vertex format count

        // Vertex format
        data.AddRange(BitConverter.GetBytes((uint)UsageType.Position));
        data.AddRange(BitConverter.GetBytes((uint)DataType.Float));
        data.Add(3); // SubCount
        data.AddRange(BitConverter.GetBytes(0u)); // SubOffset

        // Truncated data - not enough for 3 vertices
        data.AddRange(BitConverter.GetBytes(1.0f));
        data.AddRange(BitConverter.GetBytes(2.0f));

        return new MemoryStream(data.ToArray());
    }

    private static Stream CreateNonSeekableStream()
    {
        var data = CreateValidGeometryStream().ToArray();
        return new NonSeekableMemoryStream(data);
    }
}

/// <summary>
/// A memory stream that reports as non-seekable for testing purposes.
/// </summary>
internal class NonSeekableMemoryStream : MemoryStream
{
    public NonSeekableMemoryStream(byte[] buffer) : base(buffer) { }

    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin loc)
    {
        throw new NotSupportedException("Stream does not support seeking.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Stream does not support seeking.");
    }

    public override long Position
    {
        get => base.Position;
        set => throw new NotSupportedException("Stream does not support seeking.");
    }
}
