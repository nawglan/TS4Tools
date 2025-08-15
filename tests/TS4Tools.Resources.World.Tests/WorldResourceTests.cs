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

    #region ContentFields Tests

    [Fact]
    public void ContentFields_ReturnsExpectedFieldCount()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act
        var contentFields = resource.ContentFields;

        // Assert
        contentFields.Should().HaveCount(15);
    }

    [Fact]
    public void ContentFields_ReturnsExpectedFieldNames()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        var expectedFields = new[]
        {
            nameof(WorldResource.Version),
            nameof(WorldResource.IsEditable),
            nameof(WorldResource.ObjectManagers),
            nameof(WorldResource.SceneObjects),
            nameof(WorldResource.IsDirty),
            nameof(WorldResource.ObjectManagerCount),
            nameof(WorldResource.SceneObjectCount),
            nameof(WorldResource.HasObjectManagers),
            nameof(WorldResource.HasSceneObjects),
            nameof(WorldResource.TotalObjectCount),
            nameof(WorldResource.IsEmpty),
            nameof(WorldResource.ResourceSize),
            nameof(WorldResource.LastModified),
            nameof(WorldResource.MemoryUsage),
            nameof(WorldResource.ResourceComplexity)
        };

        // Act
        var contentFields = resource.ContentFields;

        // Assert
        contentFields.Should().BeEquivalentTo(expectedFields);
    }

    [Theory]
    [InlineData(nameof(WorldResource.Version), typeof(uint))]
    [InlineData(nameof(WorldResource.IsEditable), typeof(bool))]
    [InlineData(nameof(WorldResource.IsDirty), typeof(bool))]
    [InlineData(nameof(WorldResource.ObjectManagerCount), typeof(int))]
    [InlineData(nameof(WorldResource.SceneObjectCount), typeof(int))]
    [InlineData(nameof(WorldResource.HasObjectManagers), typeof(bool))]
    [InlineData(nameof(WorldResource.HasSceneObjects), typeof(bool))]
    [InlineData(nameof(WorldResource.TotalObjectCount), typeof(int))]
    [InlineData(nameof(WorldResource.IsEmpty), typeof(bool))]
    [InlineData(nameof(WorldResource.ResourceSize), typeof(long))]
    [InlineData(nameof(WorldResource.LastModified), typeof(DateTime))]
    [InlineData(nameof(WorldResource.MemoryUsage), typeof(long))]
    [InlineData(nameof(WorldResource.ResourceComplexity), typeof(string))]
    public void StringIndexer_ReturnsCorrectTypeForField(string fieldName, Type expectedType)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act
        var typedValue = resource[fieldName];

        // Assert
        typedValue.Type.Should().Be(expectedType);
        typedValue.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, nameof(WorldResource.Version))]
    [InlineData(1, nameof(WorldResource.IsEditable))]
    [InlineData(2, nameof(WorldResource.ObjectManagers))]
    [InlineData(3, nameof(WorldResource.SceneObjects))]
    [InlineData(4, nameof(WorldResource.IsDirty))]
    [InlineData(5, nameof(WorldResource.ObjectManagerCount))]
    [InlineData(6, nameof(WorldResource.SceneObjectCount))]
    [InlineData(7, nameof(WorldResource.HasObjectManagers))]
    [InlineData(8, nameof(WorldResource.HasSceneObjects))]
    [InlineData(9, nameof(WorldResource.TotalObjectCount))]
    [InlineData(10, nameof(WorldResource.IsEmpty))]
    [InlineData(11, nameof(WorldResource.ResourceSize))]
    [InlineData(12, nameof(WorldResource.LastModified))]
    [InlineData(13, nameof(WorldResource.MemoryUsage))]
    [InlineData(14, nameof(WorldResource.ResourceComplexity))]
    public void IntegerIndexer_ReturnsExpectedField(int index, string expectedField)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act
        var indexValue = resource[index];
        var fieldValue = resource[expectedField];

        // Assert
        indexValue.Should().BeEquivalentTo(fieldValue);
    }

    [Fact]
    public void StringIndexer_WithInvalidField_ThrowsArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act & Assert
        var action = () => resource["InvalidField"];
        action.Should().Throw<ArgumentException>()
            .WithMessage("Unknown field: InvalidField*");
    }

    [Fact]
    public void IntegerIndexer_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act & Assert
        var action = () => resource[15];
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Index must be 0-14, got 15*");
    }

    [Fact]
    public void StringIndexer_SetValue_ThrowsNotSupportedException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        var typedValue = new TypedValue(typeof(uint), 42U);

        // Act & Assert
        var action = () => resource[nameof(WorldResource.Version)] = typedValue;
        action.Should().Throw<NotSupportedException>()
            .WithMessage("World resource fields are read-only via string indexer");
    }

    [Fact]
    public void IntegerIndexer_SetValue_ThrowsNotSupportedException()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        var typedValue = new TypedValue(typeof(uint), 42U);

        // Act & Assert
        var action = () => resource[0] = typedValue;
        action.Should().Throw<NotSupportedException>()
            .WithMessage("World resource fields are read-only via integer indexer");
    }

    [Fact]
    public void EmptyResource_ContentFields_ReturnsCorrectValues()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act & Assert
        resource[nameof(WorldResource.ObjectManagerCount)].Value.Should().Be(0);
        resource[nameof(WorldResource.SceneObjectCount)].Value.Should().Be(0);
        resource[nameof(WorldResource.HasObjectManagers)].Value.Should().Be(false);
        resource[nameof(WorldResource.HasSceneObjects)].Value.Should().Be(false);
        resource[nameof(WorldResource.TotalObjectCount)].Value.Should().Be(0);
        resource[nameof(WorldResource.IsEmpty)].Value.Should().Be(true);
        resource[nameof(WorldResource.ResourceComplexity)].Value.Should().Be("Empty");
    }

    [Fact]
    public void PopulatedResource_ContentFields_ReturnsCorrectValues()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        var manager = new ObjectManager(1UL, "TestManager");
        var sceneObject = new SceneObject(2UL, "TestObject");
        
        resource.AddObjectManager(manager);
        resource.AddSceneObject(sceneObject);

        // Act & Assert
        resource[nameof(WorldResource.ObjectManagerCount)].Value.Should().Be(1);
        resource[nameof(WorldResource.SceneObjectCount)].Value.Should().Be(1);
        resource[nameof(WorldResource.HasObjectManagers)].Value.Should().Be(true);
        resource[nameof(WorldResource.HasSceneObjects)].Value.Should().Be(true);
        resource[nameof(WorldResource.TotalObjectCount)].Value.Should().Be(2);
        resource[nameof(WorldResource.IsEmpty)].Value.Should().Be(false);
        resource[nameof(WorldResource.ResourceComplexity)].Value.Should().Be("Simple");
    }

    [Theory]
    [InlineData(0, "Empty")]
    [InlineData(5, "Simple")]
    [InlineData(50, "Moderate")]
    [InlineData(200, "Complex")]
    [InlineData(1000, "Very Complex")]
    public void ResourceComplexity_ReturnsCorrectValue(int totalObjects, string expectedComplexity)
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        
        // Add managers and objects to reach the desired total
        for (int i = 0; i < totalObjects / 2; i++)
        {
            resource.AddObjectManager(new ObjectManager((ulong)i, $"Manager{i}"));
        }
        
        for (int i = 0; i < (totalObjects + 1) / 2; i++)
        {
            resource.AddSceneObject(new SceneObject((ulong)(i + 1000), $"Object{i}"));
        }

        // Act
        var complexity = resource[nameof(WorldResource.ResourceComplexity)].Value;

        // Assert
        complexity.Should().Be(expectedComplexity);
    }

    [Fact]
    public void ResourceSize_ReturnsPositiveValue()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        resource.AddObjectManager(new ObjectManager(1UL, "TestManager"));

        // Act
        var size = (long)resource[nameof(WorldResource.ResourceSize)].Value!;

        // Assert
        size.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MemoryUsage_ReturnsPositiveValue()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        resource.AddObjectManager(new ObjectManager(1UL, "TestManager"));

        // Act
        var usage = (long)resource[nameof(WorldResource.MemoryUsage)].Value!;

        // Assert
        usage.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LastModified_UpdatesWhenResourceChanges()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);
        var initialTime = (DateTime)resource[nameof(WorldResource.LastModified)].Value!;
        
        // Wait a moment to ensure time difference
        Thread.Sleep(10);

        // Act
        resource.AddObjectManager(new ObjectManager(1UL, "TestManager"));
        var updatedTime = (DateTime)resource[nameof(WorldResource.LastModified)].Value!;

        // Assert
        updatedTime.Should().BeAfter(initialTime);
    }

    [Fact]
    public void CollectionFields_ReturnCorrectTypes()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        using var resource = new WorldResource(key, 1);

        // Act & Assert
        resource[nameof(WorldResource.ObjectManagers)].Type
            .Should().Be(typeof(IReadOnlyList<ObjectManager>));
        resource[nameof(WorldResource.SceneObjects)].Type
            .Should().Be(typeof(IReadOnlyList<SceneObject>));
    }

    #endregion
}
