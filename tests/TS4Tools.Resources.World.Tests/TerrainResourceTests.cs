using AutoFixture.Xunit2;
using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for the TerrainResource class.
/// </summary>
public sealed class TerrainResourceTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Theory]
    [AutoData]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly(uint version, uint layerIndexCount)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var minBounds = new TerrainBounds(10, 20, 30);
        var maxBounds = new TerrainBounds(100, 200, 300);

        // Act
        var resource = new TerrainResource(key, version, layerIndexCount, minBounds, maxBounds);
        _disposables.Add(resource);

        // Assert
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(version);
        resource.LayerIndexCount.Should().Be(layerIndexCount);
        resource.MinBounds.Should().Be(minBounds);
        resource.MaxBounds.Should().Be(maxBounds);
        resource.Vertices.Should().BeEmpty();
        resource.Passes.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TerrainResource(null!, 1, 0);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [AutoData]
    public void AddVertex_WithValidVertex_ShouldAddVertex(float x, float y, float z, float u, float v, uint layerIndex)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var vertex = new TerrainVertex { X = x, Y = y, Z = z, U = u, V = v, LayerIndex = layerIndex };

        // Act
        resource.AddVertex(vertex);

        // Assert
        resource.Vertices.Should().Contain(vertex);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddVertex_WithNullVertex_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddVertex(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("vertex");
    }

    [Theory]
    [AutoData]
    public void RemoveVertex_WithExistingVertex_ShouldReturnTrueAndRemoveVertex(float x, float y, float z, float u, float v, uint layerIndex)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var vertex = new TerrainVertex { X = x, Y = y, Z = z, U = u, V = v, LayerIndex = layerIndex };
        resource.AddVertex(vertex);

        // Act
        var result = resource.RemoveVertex(vertex);

        // Assert
        result.Should().BeTrue();
        resource.Vertices.Should().NotContain(vertex);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void RemoveVertex_WithNonExistingVertex_ShouldReturnFalse(float x, float y, float z, float u, float v, uint layerIndex)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var vertex = new TerrainVertex { X = x, Y = y, Z = z, U = u, V = v, LayerIndex = layerIndex };

        // Act
        var result = resource.RemoveVertex(vertex);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveVertex_WithNullVertex_ShouldReturnFalse()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act
        var result = resource.RemoveVertex(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public void AddPass_WithValidPass_ShouldAddPass(uint[] indices, uint materialId)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var pass = new TerrainPass { Indices = indices, MaterialId = materialId };

        // Act
        resource.AddPass(pass);

        // Assert
        resource.Passes.Should().Contain(pass);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddPass_WithNullPass_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddPass(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pass");
    }

    [Theory]
    [AutoData]
    public void RemovePass_WithExistingPass_ShouldReturnTrueAndRemovePass(uint[] indices, uint materialId)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var pass = new TerrainPass { Indices = indices, MaterialId = materialId };
        resource.AddPass(pass);

        // Act
        var result = resource.RemovePass(pass);

        // Assert
        result.Should().BeTrue();
        resource.Passes.Should().NotContain(pass);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void RemovePass_WithNonExistingPass_ShouldReturnFalse(uint[] indices, uint materialId)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);
        var pass = new TerrainPass { Indices = indices, MaterialId = materialId };

        // Act
        var result = resource.RemovePass(pass);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemovePass_WithNullPass_ShouldReturnFalse()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act
        var result = resource.RemovePass(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.LoadFromStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SaveToStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithValidStream_ShouldSaveSuccessfully(uint[] indices, uint materialId)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1, 2);
        _disposables.Add(resource);

        var vertex = new TerrainVertex { X = 1.0f, Y = 2.0f, Z = 3.0f, U = 0.5f, V = 0.7f, LayerIndex = 1 };
        var pass = new TerrainPass { Indices = indices, MaterialId = materialId };
        resource.AddVertex(vertex);
        resource.AddPass(pass);

        using var stream = new MemoryStream();

        // Act
        await resource.SaveToStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(uint version, uint[] indices, uint materialId)
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, version, 2);
        _disposables.Add(resource);

        var vertex = new TerrainVertex { X = 1.0f, Y = 2.0f, Z = 3.0f, U = 0.5f, V = 0.7f, LayerIndex = 1 };
        var pass = new TerrainPass { Indices = indices, MaterialId = materialId };
        resource.AddVertex(vertex);
        resource.AddPass(pass);

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain($"Version: {version}");
        result.Should().Contain("Vertices: 1");
        result.Should().Contain("Passes: 1");
    }

    [Fact]
    public void Dispose_ShouldClearCollections()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);
        resource.AddVertex(new TerrainVertex { X = 1, Y = 2, Z = 3 });
        resource.AddPass(new TerrainPass { Indices = new uint[] { 1, 2, 3 }, MaterialId = 100 });

        // Act
        resource.Dispose();

        // Assert
        resource.Vertices.Should().BeEmpty();
        resource.Passes.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);

        // Act & Assert
        Action act = () =>
        {
            resource.Dispose();
            resource.Dispose();
            resource.Dispose();
        };

        act.Should().NotThrow();
    }
}

/// <summary>
/// Unit tests for the TerrainVertex class.
/// </summary>
public sealed class TerrainVertexTests
{
    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(float x, float y, float z, float u, float v, uint layerIndex)
    {
        // Arrange
        var vertex = new TerrainVertex
        {
            X = x,
            Y = y,
            Z = z,
            U = u,
            V = v,
            LayerIndex = layerIndex
        };

        // Act
        var result = vertex.ToString();

        // Assert
        result.Should().Contain($"({x}, {y}, {z})");
        result.Should().Contain($"UV({u}, {v})");
        result.Should().Contain($"Layer: {layerIndex}");
    }
}

/// <summary>
/// Unit tests for the TerrainPass class.
/// </summary>
public sealed class TerrainPassTests
{
    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(uint[] indices, uint materialId)
    {
        // Arrange
        var pass = new TerrainPass
        {
            Indices = indices,
            MaterialId = materialId
        };

        // Act
        var result = pass.ToString();

        // Assert
        result.Should().Contain($"Indices: {indices.Length}");
        result.Should().Contain($"Material: {materialId}");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyIndices()
    {
        // Act
        var pass = new TerrainPass();

        // Assert
        pass.Indices.Should().BeEmpty();
        pass.MaterialId.Should().Be(0);
    }
}
