using FluentAssertions;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class RigResourceTests : IDisposable
{
    private readonly RigResource _rigResource;

    public RigResourceTests()
    {
        _rigResource = new RigResource();
    }

    public void Dispose()
    {
        _rigResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var resource = new RigResource();

        // Assert
        resource.Should().NotBeNull();
        resource.RigName.Should().BeEmpty();
        resource.BoneCount.Should().Be(0);
        resource.RootBone.Should().BeNull();
        resource.Bones.Should().NotBeNull().And.BeEmpty();
        resource.BoneHierarchy.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new RigResource(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Constructor_WithStream_ShouldInitializeFromStream()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var resource = new RigResource(stream);
        resource.Should().NotBeNull();
    }

    [Fact]
    public void RigName_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const string expectedName = "TestRig";

        // Act
        _rigResource.RigName = expectedName;

        // Assert
        _rigResource.RigName.Should().Be(expectedName);
    }

    [Fact]
    public void BoneCount_WhenBonesAdded_ShouldReflectCount()
    {
        // Arrange
        var bone1 = new Bone("Root", new Vector3(0, 0, 0), Quaternion.Identity, null);
        var bone2 = new Bone("Child", new Vector3(1, 0, 0), Quaternion.Identity, bone1);

        // Act
        _rigResource.AddBone(bone1);
        _rigResource.AddBone(bone2);

        // Assert
        _rigResource.BoneCount.Should().Be(2);
        _rigResource.Bones.Should().HaveCount(2);
    }

    [Fact]
    public void RootBone_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var rootBone = new Bone("Root", new Vector3(0, 0, 0), Quaternion.Identity, null);

        // Act
        _rigResource.RootBone = rootBone;

        // Assert
        _rigResource.RootBone.Should().Be(rootBone);
    }

    [Fact]
    public void AddBone_WithValidBone_ShouldAddToCollection()
    {
        // Arrange
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);

        // Act
        _rigResource.AddBone(bone);

        // Assert
        _rigResource.Bones.Should().Contain(bone);
        _rigResource.BoneCount.Should().Be(1);
    }

    [Fact]
    public void AddBone_WithNullBone_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => _rigResource.AddBone(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bone");
    }

    [Fact]
    public void RemoveBone_WithExistingBone_ShouldRemoveFromCollection()
    {
        // Arrange
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);
        _rigResource.AddBone(bone);

        // Act
        var result = _rigResource.RemoveBone(bone);

        // Assert
        result.Should().BeTrue();
        _rigResource.Bones.Should().NotContain(bone);
        _rigResource.BoneCount.Should().Be(0);
    }

    [Fact]
    public void RemoveBone_WithNonExistingBone_ShouldReturnFalse()
    {
        // Arrange
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);

        // Act
        var result = _rigResource.RemoveBone(bone);

        // Assert
        result.Should().BeFalse();
        _rigResource.BoneCount.Should().Be(0);
    }

    [Fact]
    public void FindBoneByName_WithExistingBone_ShouldReturnBone()
    {
        // Arrange
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);
        _rigResource.AddBone(bone);

        // Act
        var foundBone = _rigResource.FindBoneByName("TestBone");

        // Assert
        foundBone.Should().Be(bone);
    }

    [Fact]
    public void FindBoneByName_WithNonExistingBone_ShouldReturnNull()
    {
        // Arrange & Act
        var foundBone = _rigResource.FindBoneByName("NonExistingBone");

        // Assert
        foundBone.Should().BeNull();
    }

    [Fact]
    public void FindBoneByName_WithNullOrEmptyName_ShouldReturnNull()
    {
        // Act & Assert
        _rigResource.FindBoneByName(null!).Should().BeNull();
        _rigResource.FindBoneByName(string.Empty).Should().BeNull();
        _rigResource.FindBoneByName(" ").Should().BeNull();
    }

    [Fact]
    public void GetBoneHierarchy_WithHierarchicalBones_ShouldReturnCorrectStructure()
    {
        // Arrange
        var rootBone = new Bone("Root", new Vector3(0, 0, 0), Quaternion.Identity, null);
        var childBone1 = new Bone("Child1", new Vector3(1, 0, 0), Quaternion.Identity, rootBone);
        var childBone2 = new Bone("Child2", new Vector3(-1, 0, 0), Quaternion.Identity, rootBone);
        var grandChildBone = new Bone("GrandChild", new Vector3(1, 1, 0), Quaternion.Identity, childBone1);

        _rigResource.AddBone(rootBone);
        _rigResource.AddBone(childBone1);
        _rigResource.AddBone(childBone2);
        _rigResource.AddBone(grandChildBone);
        _rigResource.RootBone = rootBone;

        // Act
        var hierarchy = _rigResource.GetBoneHierarchy();

        // Assert
        hierarchy.Should().NotBeNull();
        hierarchy.Should().ContainKey(rootBone);
        hierarchy[rootBone].Should().HaveCount(2);
        hierarchy[rootBone].Should().Contain(childBone1);
        hierarchy[rootBone].Should().Contain(childBone2);
        
        hierarchy.Should().ContainKey(childBone1);
        hierarchy[childBone1].Should().HaveCount(1);
        hierarchy[childBone1].Should().Contain(grandChildBone);

        hierarchy.Should().ContainKey(childBone2);
        hierarchy[childBone2].Should().BeEmpty();

        hierarchy.Should().ContainKey(grandChildBone);
        hierarchy[grandChildBone].Should().BeEmpty();
    }

    [Fact]
    public void ClearBones_WithExistingBones_ShouldClearAllBones()
    {
        // Arrange
        var bone1 = new Bone("Bone1", new Vector3(1, 0, 0), Quaternion.Identity, null);
        var bone2 = new Bone("Bone2", new Vector3(0, 1, 0), Quaternion.Identity, null);
        
        _rigResource.AddBone(bone1);
        _rigResource.AddBone(bone2);
        _rigResource.RootBone = bone1;

        // Act
        _rigResource.ClearBones();

        // Assert
        _rigResource.Bones.Should().BeEmpty();
        _rigResource.BoneCount.Should().Be(0);
        _rigResource.RootBone.Should().BeNull();
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        await FluentActions.Awaiting(() => _rigResource.LoadFromStreamAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithEmptyStream_ShouldHandleGracefully()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _rigResource.LoadFromStreamAsync(stream))
            .Should().NotThrowAsync();
    }

    [Fact]
    public void AsBytes_ShouldReturnValidByteArray()
    {
        // Arrange
        _rigResource.RigName = "TestRig";
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);
        _rigResource.AddBone(bone);

        // Act
        var bytes = _rigResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Act
        var fields = _rigResource.ContentFields;

        // Assert
        fields.Should().NotBeNull();
        fields.Should().Contain("RigName");
        fields.Should().Contain("BoneCount");
        fields.Should().Contain("RootBone");
        fields.Should().Contain("Bones");
        fields.Should().Contain("BoneHierarchy");
    }

    [Fact]
    public void Indexer_WithValidFieldName_ShouldReturnCorrectValue()
    {
        // Arrange
        _rigResource.RigName = "TestRig";

        // Act
        var value = _rigResource["RigName"];

        // Assert
        value.Value.Should().Be("TestRig");
    }

    [Fact]
    public void Indexer_WithInvalidFieldName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Action act = () => _ = _rigResource["InvalidField"];
        act.Should().Throw<ArgumentException>()
           .WithMessage("Unknown field: InvalidField*")
           .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void ResourceChanged_WhenBoneAdded_ShouldFireEvent()
    {
        // Arrange
        var eventFired = false;
        _rigResource.ResourceChanged += (_, _) => eventFired = true;
        
        var bone = new Bone("TestBone", new Vector3(1, 2, 3), Quaternion.Identity, null);

        // Act
        _rigResource.AddBone(bone);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ResourceChanged_WhenRigNameChanged_ShouldFireEvent()
    {
        // Arrange
        var eventFired = false;
        _rigResource.ResourceChanged += (_, _) => eventFired = true;

        // Act
        _rigResource.RigName = "NewRig";

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldDisposeResourceCorrectly()
    {
        // Arrange
        var resource = new RigResource();

        // Act
        resource.Dispose();

        // Assert - Should throw when accessing disposed resource's Stream
        Action act = () => _ = resource.Stream;
        act.Should().Throw<ObjectDisposedException>();
    }
}
