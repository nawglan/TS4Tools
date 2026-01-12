using System.Text;
using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

public class BoneResourceTests
{
    private static ResourceKey TestKey => new(BoneResource.TypeId, 0, 0x12345678);

    [Fact]
    public void EmptyResource_ShouldNotBeValid()
    {
        // Arrange & Act
        var resource = new BoneResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TruncatedHeader_ShouldNotBeValid()
    {
        // Arrange - Less than 8 bytes
        byte[] truncatedData = new byte[4];

        // Act
        var resource = new BoneResource(TestKey, truncatedData);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MinimalValidResource_NoBones_ShouldParse()
    {
        // Arrange - Version + name count (0) + matrix count (0)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(1u); // Version
        writer.Write(0); // Name count
        writer.Write(0); // Matrix count

        byte[] data = ms.ToArray();

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Version.Should().Be(1);
        resource.Bones.Should().BeEmpty();
    }

    [Fact]
    public void ResourceWithOneBone_ShouldParseCorrectly()
    {
        // Arrange
        byte[] data = CreateBoneResourceData(1, "TestBone");

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Version.Should().Be(1);
        resource.Bones.Should().HaveCount(1);
        resource.Bones[0].Name.Should().Be("TestBone");
    }

    [Fact]
    public void ResourceWithMultipleBones_ShouldParseCorrectly()
    {
        // Arrange
        byte[] data = CreateBoneResourceData(3, "Bone1", "Bone2", "Bone3");

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Bones.Should().HaveCount(3);
        resource.Bones[0].Name.Should().Be("Bone1");
        resource.Bones[1].Name.Should().Be("Bone2");
        resource.Bones[2].Name.Should().Be("Bone3");
    }

    [Fact]
    public void MatrixValues_ShouldBePreserved()
    {
        // Arrange - Create a resource with specific matrix values
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(2u); // Version
        writer.Write(1); // Name count

        // Write name "Test" in BigEndianUnicode with 7-bit length prefix
        WriteBigEndianUnicodeString(ms, "Test");

        writer.Write(1); // Matrix count

        // Write matrix values (4 rows x 3 floats)
        // Right row
        writer.Write(1.0f);
        writer.Write(0.0f);
        writer.Write(0.0f);
        // Up row
        writer.Write(0.0f);
        writer.Write(1.0f);
        writer.Write(0.0f);
        // Back row
        writer.Write(0.0f);
        writer.Write(0.0f);
        writer.Write(1.0f);
        // Translate row
        writer.Write(10.0f);
        writer.Write(20.0f);
        writer.Write(30.0f);

        byte[] data = ms.ToArray();

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Bones.Should().HaveCount(1);

        var matrix = resource.Bones[0].InverseBindPose;
        matrix.Right.X.Should().Be(1.0f);
        matrix.Right.Y.Should().Be(0.0f);
        matrix.Right.Z.Should().Be(0.0f);
        matrix.Up.X.Should().Be(0.0f);
        matrix.Up.Y.Should().Be(1.0f);
        matrix.Up.Z.Should().Be(0.0f);
        matrix.Back.X.Should().Be(0.0f);
        matrix.Back.Y.Should().Be(0.0f);
        matrix.Back.Z.Should().Be(1.0f);
        matrix.Translate.X.Should().Be(10.0f);
        matrix.Translate.Y.Should().Be(20.0f);
        matrix.Translate.Z.Should().Be(30.0f);
    }

    [Fact]
    public void MismatchedCounts_ShouldNotBeValid()
    {
        // Arrange - Name count differs from matrix count
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(1u); // Version
        writer.Write(1); // Name count
        WriteBigEndianUnicodeString(ms, "Test");
        writer.Write(2); // Matrix count (mismatch!)

        byte[] data = ms.ToArray();

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveData()
    {
        // Arrange
        byte[] originalData = CreateBoneResourceData(2, "Root", "Spine");
        var resource = new BoneResource(TestKey, originalData);
        resource.IsValid.Should().BeTrue();

        // Act
        var serialized = resource.Data;
        var reparsed = new BoneResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.Version.Should().Be(resource.Version);
        reparsed.Bones.Should().HaveCount(resource.Bones.Count);

        for (int i = 0; i < resource.Bones.Count; i++)
        {
            reparsed.Bones[i].Name.Should().Be(resource.Bones[i].Name);
            reparsed.Bones[i].InverseBindPose.Should().Be(resource.Bones[i].InverseBindPose);
        }
    }

    [Fact]
    public void RoundTrip_EmptyResource_ShouldWork()
    {
        // Arrange
        var resource = new BoneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Version = 3;

        // Act
        var serialized = resource.Data;
        var reparsed = new BoneResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.Version.Should().Be(3);
        reparsed.Bones.Should().BeEmpty();
    }

    [Fact]
    public void AddBone_ShouldAddToList()
    {
        // Arrange
        var resource = new BoneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Version = 1;

        // Act
        resource.AddBone("NewBone");

        // Assert
        resource.Bones.Should().HaveCount(1);
        resource.Bones[0].Name.Should().Be("NewBone");
        resource.Bones[0].InverseBindPose.Should().Be(BoneMatrix4x3.Identity);
    }

    [Fact]
    public void AddBoneWithMatrix_ShouldPreserveMatrix()
    {
        // Arrange
        var resource = new BoneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var customMatrix = new BoneMatrix4x3(
            new BoneMatrixRow(2f, 0f, 0f),
            new BoneMatrixRow(0f, 2f, 0f),
            new BoneMatrixRow(0f, 0f, 2f),
            new BoneMatrixRow(5f, 10f, 15f));

        // Act
        resource.AddBone(new BoneData("CustomBone", customMatrix));

        // Assert
        resource.Bones[0].InverseBindPose.Should().Be(customMatrix);
    }

    [Fact]
    public void ClearBones_ShouldRemoveAll()
    {
        // Arrange
        var resource = new BoneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddBone("Bone1");
        resource.AddBone("Bone2");

        // Act
        resource.ClearBones();

        // Assert
        resource.Bones.Should().BeEmpty();
    }

    [Fact]
    public void TypeId_ShouldMatchExpectedValue()
    {
        // Assert
        BoneResource.TypeId.Should().Be(0x00AE6C67);
    }

    [Fact]
    public void UnicodeNames_ShouldBePreserved()
    {
        // Arrange - Test with non-ASCII characters
        byte[] data = CreateBoneResourceData(1, "Bone\u00E9"); // e with acute accent

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Bones[0].Name.Should().Be("Bone\u00E9");
    }

    [Fact]
    public void UnreasonableBoneCount_ShouldNotBeValid()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(1u); // Version
        writer.Write(100001); // Unreasonable count

        byte[] data = ms.ToArray();

        // Act
        var resource = new BoneResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    // Helper method to create valid bone resource data
    private static byte[] CreateBoneResourceData(uint version, params string[] boneNames)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(version);
        writer.Write(boneNames.Length);

        foreach (var name in boneNames)
        {
            WriteBigEndianUnicodeString(ms, name);
        }

        writer.Write(boneNames.Length);

        // Write identity matrices for each bone
        for (int i = 0; i < boneNames.Length; i++)
        {
            // Identity matrix (4 rows x 3 floats)
            // Right: (1, 0, 0)
            writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f);
            // Up: (0, 1, 0)
            writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f);
            // Back: (0, 0, 1)
            writer.Write(0.0f); writer.Write(0.0f); writer.Write(1.0f);
            // Translate: (0, 0, 0)
            writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);
        }

        return ms.ToArray();
    }

    private static void WriteBigEndianUnicodeString(Stream s, string value)
    {
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(value ?? string.Empty);

        // Write 7-bit encoded length
        int length = bytes.Length;
        do
        {
            byte b = (byte)(length & 0x7F);
            length >>= 7;
            if (length > 0)
                b |= 0x80;
            s.WriteByte(b);
        } while (length > 0);

        // Write the bytes
        s.Write(bytes, 0, bytes.Length);
    }
}
