using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Geometry;

namespace TS4Tools.Resources.Geometry.Tests;

/// <summary>
/// Unit tests for MeshResourceFactory.
/// Tests factory creation, resource type detection, validation, and error handling.
/// </summary>
public class MeshResourceFactoryTests
{
    private readonly ILogger<MeshResourceFactory> _logger;
    private readonly MeshResourceFactory _factory;

    public MeshResourceFactoryTests()
    {
        _logger = Substitute.For<ILogger<MeshResourceFactory>>();
        _factory = new MeshResourceFactory(_logger);
    }

    [Fact]
    public void Constructor_WithValidLogger_SetsProperties()
    {
        var factory = new MeshResourceFactory(_logger);

        factory.Should().NotBeNull();
        factory.ResourceTypes.Should().Contain(0x01661233u);
        factory.SupportedResourceTypes.Should().Contain("MESH");
    }

    [Fact]
    public void Constructor_WithNullLogger_CreatesFactory()
    {
        var factory = new MeshResourceFactory(null);

        factory.Should().NotBeNull();
        factory.ResourceTypes.Should().Contain(0x01661233u);
    }

    [Fact]
    public void ResourceTypes_ContainsExpectedValue()
    {
        _factory.ResourceTypes.Should().Contain(0x01661233u);
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidStream_ReturnsMeshResource()
    {
        // Arrange
        var stream = CreateValidMeshStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MeshResource>();
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
        var stream = CreateCorruptedMeshStream();

        // Act
        var act = async () => await _factory.CreateResourceAsync(1, stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithMinimalValidData_ReturnsResource()
    {
        // Arrange
        var stream = CreateMinimalValidMeshStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MeshResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_LogsCreationStart()
    {
        // Arrange
        var stream = CreateValidMeshStream();

        // Act
        await _factory.CreateResourceAsync(1, stream);

        // Assert
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Creating mesh resource")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceAsync_LogsCreationSuccess()
    {
        // Arrange
        var stream = CreateValidMeshStream();

        // Act
        await _factory.CreateResourceAsync(1, stream);

        // Assert
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Created mesh resource")),
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
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to create mesh resource")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceAsync_CanHandleMultipleCalls()
    {
        // Arrange
        var stream1 = CreateValidMeshStream();
        var stream2 = CreateValidMeshStream();

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
        var stream = CreateValidMeshStream();
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
        var stream = seekable ? CreateValidMeshStream() : CreateNonSeekableMeshStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
    }

    private static MemoryStream CreateValidMeshStream()
    {
        var data = new List<byte>();

        // Mesh data format expected by ParseMeshData:
        // [4 bytes] Vertex count
        // [4 bytes] Index count
        // [4 bytes] Has normals flag
        // [4 bytes] Has UV coordinates flag
        // [Variable] Vertex data (3 floats per vertex)
        // [Variable] Normal data (3 floats per vertex, if present)
        // [Variable] UV data (2 floats per vertex, if present)
        // [Variable] Index data (ushort per index)

        data.AddRange(BitConverter.GetBytes(3)); // Vertex count
        data.AddRange(BitConverter.GetBytes(3)); // Index count
        data.AddRange(BitConverter.GetBytes(0)); // Has normals flag (false)
        data.AddRange(BitConverter.GetBytes(0)); // Has UV coordinates flag (false)

        // Vertex data (3 vertices * 3 floats = 9 floats)
        for (int i = 0; i < 9; i++)
        {
            data.AddRange(BitConverter.GetBytes((float)i));
        }

        // Index data (3 indices as ushort)
        data.AddRange(BitConverter.GetBytes((ushort)0));
        data.AddRange(BitConverter.GetBytes((ushort)1));
        data.AddRange(BitConverter.GetBytes((ushort)2));

        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateMinimalValidMeshStream()
    {
        var data = new List<byte>();

        // Minimal mesh data
        data.AddRange(BitConverter.GetBytes(0)); // Vertex count
        data.AddRange(BitConverter.GetBytes(0)); // Index count
        data.AddRange(BitConverter.GetBytes(0)); // Has normals flag (false)
        data.AddRange(BitConverter.GetBytes(0)); // Has UV coordinates flag (false)

        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateCorruptedMeshStream()
    {
        var data = new List<byte>();

        // Corrupted mesh data - invalid vertex count
        data.AddRange(BitConverter.GetBytes(-1)); // Invalid negative vertex count
        data.AddRange(BitConverter.GetBytes(0)); // Index count
        data.AddRange(BitConverter.GetBytes(0)); // Has normals flag (false)
        data.AddRange(BitConverter.GetBytes(0)); // Has UV coordinates flag (false)

        return new MemoryStream(data.ToArray());
    }

    private static NonSeekableMeshMemoryStream CreateNonSeekableMeshStream()
    {
        var data = CreateValidMeshStream().ToArray();
        return new NonSeekableMeshMemoryStream(data);
    }
}

/// <summary>
/// A memory stream that reports as non-seekable for testing mesh resources.
/// </summary>
internal sealed class NonSeekableMeshMemoryStream : MemoryStream
{
    public NonSeekableMeshMemoryStream(byte[] buffer) : base(buffer) { }

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
