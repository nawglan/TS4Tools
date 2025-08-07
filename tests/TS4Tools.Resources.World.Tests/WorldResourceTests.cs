using AutoFixture.Xunit2;
using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for the WorldResource class.
/// </summary>
public sealed class WorldResourceTests : IDisposable
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
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly(uint version)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new WorldResource(key, version);
        _disposables.Add(resource);

        // Assert
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(version);
        resource.ObjectManagers.Should().BeEmpty();
        resource.SceneObjects.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new WorldResource(null!, 1);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [AutoData]
    public void AddObjectManager_WithValidManager_ShouldAddManager(string name, uint managerType, byte[] data)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, 1);
        _disposables.Add(resource);

        var manager = new ObjectManager
        {
            Name = name,
            ManagerType = managerType,
            Data = data
        };

        // Act
        resource.AddObjectManager(manager);

        // Assert
        resource.ObjectManagers.Should().ContainSingle();
        resource.ObjectManagers.Should().Contain(manager);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddObjectManager_WithNullManager_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, 1);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddObjectManager(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("manager");
    }

    [Theory]
    [AutoData]
    public void RemoveObjectManager_WithExistingManager_ShouldReturnTrueAndRemoveManager(string name, uint managerType, byte[] data)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, 1);
        _disposables.Add(resource);

        var manager = new ObjectManager
        {
            Name = name,
            ManagerType = managerType,
            Data = data
        };
        resource.AddObjectManager(manager);

        // Act
        var result = resource.RemoveObjectManager(manager);

        // Assert
        result.Should().BeTrue();
        resource.ObjectManagers.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void RemoveObjectManager_WithNonExistingManager_ShouldReturnFalse(string name, uint managerType, byte[] data)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);

        // Act
        var result = resource.RemoveObjectManager(managerName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveObjectManager_WithNullOrEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);

        // Act & Assert
        resource.RemoveObjectManager(null!).Should().BeFalse();
        resource.RemoveObjectManager(string.Empty).Should().BeFalse();
        resource.RemoveObjectManager("   ").Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public void AddSceneObject_WithValidObject_ShouldAddObject(uint objectId, float x, float y, float z)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);
        var sceneObject = new SceneObject(objectId, new Vector3(x, y, z));

        // Act
        resource.AddSceneObject(sceneObject);

        // Assert
        resource.SceneObjects.Should().Contain(sceneObject);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddSceneObject_WithNullObject_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddSceneObject(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sceneObject");
    }

    [Theory]
    [AutoData]
    public void RemoveSceneObject_WithExistingObject_ShouldReturnTrueAndRemoveObject(uint objectId, float x, float y, float z)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);
        var sceneObject = new SceneObject(objectId, new Vector3(x, y, z));
        resource.AddSceneObject(sceneObject);

        // Act
        var result = resource.RemoveSceneObject(sceneObject);

        // Assert
        result.Should().BeTrue();
        resource.SceneObjects.Should().NotContain(sceneObject);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void RemoveSceneObject_WithNonExistingObject_ShouldReturnFalse(uint objectId, float x, float y, float z)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);
        var sceneObject = new SceneObject(objectId, new Vector3(x, y, z));

        // Act
        var result = resource.RemoveSceneObject(sceneObject);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveSceneObject_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        _disposables.Add(resource);

        // Act
        var result = resource.RemoveSceneObject(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException(string worldName)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, worldName, 1);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.LoadFromStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithNullStream_ShouldThrowArgumentNullException(string worldName)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, worldName, 1);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SaveToStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithValidStream_ShouldLoadSuccessfully(string worldName, byte[] testData)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, worldName, 1);
        _disposables.Add(resource);

        using var stream = new MemoryStream(testData);

        // Act
        await resource.LoadFromStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithValidStream_ShouldSaveSuccessfully(string worldName, string managerName, byte[] managerData, uint objectId)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, worldName, 1);
        _disposables.Add(resource);

        resource.AddObjectManager(managerName, managerData);
        resource.AddSceneObject(new SceneObject(objectId, new Vector3(1.0f, 2.0f, 3.0f)));

        using var stream = new MemoryStream();

        // Act
        await resource.SaveToStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(string worldName, uint version, string managerName, byte[] managerData, uint objectId)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, worldName, version);
        _disposables.Add(resource);

        resource.AddObjectManager(managerName, managerData);
        resource.AddSceneObject(new SceneObject(objectId, new Vector3(1.0f, 2.0f, 3.0f)));

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain(worldName);
        result.Should().Contain(version.ToString());
        result.Should().Contain("Objects: 1");
        result.Should().Contain("Managers: 1");
    }

    [Fact]
    public void Dispose_ShouldClearCollections()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);
        resource.AddObjectManager("Test", new byte[10]);
        resource.AddSceneObject(new SceneObject(1, new Vector3(1, 2, 3)));

        // Act
        resource.Dispose();

        // Assert
        resource.ObjectManagers.Should().BeEmpty();
        resource.SceneObjects.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, "Test World", 1);

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
