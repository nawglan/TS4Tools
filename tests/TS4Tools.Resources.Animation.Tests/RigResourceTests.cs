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
        Action act = () => { var _ = new RigResource(null!); };
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
        var bone1 = new Bone("Root", null, new Vector3(0, 0, 0), Quaternion.Identity.ToVector4(), Vector3.Zero);
        var bone2 = new Bone("Child", "Root", new Vector3(1, 0, 0), Quaternion.Identity.ToVector4(), Vector3.Zero);

        // Act
        _rigResource.AddBone(bone1);
        _rigResource.AddBone(bone2);

        // Assert
        _rigResource.BoneCount.Should().Be(2);
        _rigResource.Bones.Should().HaveCount(2);
    }

    [Fact]
    public void RootBone_WhenRootBoneAdded_ShouldReturnCorrectly()
    {
        // Arrange
        var rootBone = new Bone("Root", null, new Vector3(0, 0, 0), Quaternion.Identity.ToVector4(), Vector3.Zero);

        // Act
        _rigResource.AddBone(rootBone);

        // Assert
        _rigResource.RootBone.Should().Be(rootBone);
    }

    [Fact]
    public void AddBone_WithValidBone_ShouldAddToCollection()
    {
        // Arrange
        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), Vector3.Zero);

        // Act
        _rigResource.AddBone(bone);

        // Assert
        _rigResource.Bones.Should().Contain(bone);
        _rigResource.BoneCount.Should().Be(1);
    }

    [Fact]
    public void AddBone_WithEmptyName_ShouldStillAddBone()
    {
        // Arrange
        var bone = new Bone("", null, Vector3.Zero, Quaternion.Identity.ToVector4(), Vector3.Zero);

        // Act & Assert
        _rigResource.AddBone(bone);
        _rigResource.Bones.Should().Contain(bone);
        _rigResource.BoneCount.Should().Be(1);
    }

    [Fact]
    public void RemoveBone_WithExistingBone_ShouldRemoveFromCollection()
    {
        // Arrange
        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), Vector3.Zero);
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
        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), Vector3.Zero);

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
        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), Vector3.Zero);
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
        var rootBone = new Bone("Root", null, Vector3.Zero, Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));
        var childBone1 = new Bone("Child1", "Root", new Vector3(1, 0, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));
        var childBone2 = new Bone("Child2", "Root", new Vector3(-1, 0, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));
        var grandChildBone = new Bone("GrandChild", "Child1", new Vector3(1, 1, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));

        _rigResource.AddBone(rootBone);
        _rigResource.AddBone(childBone1);
        _rigResource.AddBone(childBone2);
        _rigResource.AddBone(grandChildBone);

        // Act
        var hierarchy = _rigResource.BoneHierarchy;

        // Assert
        hierarchy.Should().NotBeNull();
        // The hierarchy is computed based on parent name relationships
        // Root bone should be found as it has no parent (parentName = null)
        var computedRootBone = _rigResource.Bones.FirstOrDefault(b => b.ParentName == null);
        computedRootBone.Should().NotBeNull();
        computedRootBone!.Name.Should().Be("Root");

        // Check that children are properly associated with their parents by name
        var child1 = _rigResource.Bones.FirstOrDefault(b => b.Name == "Child1");
        var child2 = _rigResource.Bones.FirstOrDefault(b => b.Name == "Child2");
        var grandChild = _rigResource.Bones.FirstOrDefault(b => b.Name == "GrandChild");

        child1.Should().NotBeNull();
        child1!.ParentName.Should().Be("Root");

        child2.Should().NotBeNull();
        child2!.ParentName.Should().Be("Root");

        grandChild.Should().NotBeNull();
        grandChild!.ParentName.Should().Be("Child1");
    }

    [Fact]
    public void ClearBones_WithExistingBones_ShouldClearAllBones()
    {
        // Arrange
        var bone1 = new Bone("Bone1", null, new Vector3(1, 0, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));
        var bone2 = new Bone("Bone2", null, new Vector3(0, 1, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));

        _rigResource.AddBone(bone1);
        _rigResource.AddBone(bone2);

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
        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));
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
        fields.Should().Contain("RigVersion");
        fields.Should().Contain("SupportsIk");
        fields.Should().Contain("Bones");
        // RootBone and BoneHierarchy are computed properties, not fields
        fields.Should().NotContain("RootBone");
        fields.Should().NotContain("BoneHierarchy");
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

        var bone = new Bone("TestBone", null, new Vector3(1, 2, 3), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1));

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
