using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for SkinBlock (Skeleton/Bone) parsing and serialization.
/// </summary>
public class SkinBlockTests
{
    /// <summary>
    /// Creates a minimal SKIN block with no bones.
    /// </summary>
    private static byte[] CreateSkinBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'S');
        writer.Write((byte)'K');
        writer.Write((byte)'I');
        writer.Write((byte)'N');

        // Header
        writer.Write(0x00000001u); // Version
        writer.Write(0); // Bone count

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a SKIN block with a single bone using identity matrix.
    /// </summary>
    private static byte[] CreateSkinBlockSingleBone()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'S');
        writer.Write((byte)'K');
        writer.Write((byte)'I');
        writer.Write((byte)'N');

        // Header
        writer.Write(0x00000001u); // Version
        writer.Write(1); // Bone count

        // Bone name hash
        writer.Write(0x12345678u);

        // Identity matrix (row-major order)
        // Row 0: Right.X, Up.X, Back.X, Translate.X
        writer.Write(1.0f); // Right.X
        writer.Write(0.0f); // Up.X
        writer.Write(0.0f); // Back.X
        writer.Write(0.0f); // Translate.X

        // Row 1: Right.Y, Up.Y, Back.Y, Translate.Y
        writer.Write(0.0f); // Right.Y
        writer.Write(1.0f); // Up.Y
        writer.Write(0.0f); // Back.Y
        writer.Write(0.0f); // Translate.Y

        // Row 2: Right.Z, Up.Z, Back.Z, Translate.Z
        writer.Write(0.0f); // Right.Z
        writer.Write(0.0f); // Up.Z
        writer.Write(1.0f); // Back.Z
        writer.Write(0.0f); // Translate.Z

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a SKIN block with multiple bones.
    /// </summary>
    private static byte[] CreateSkinBlockMultipleBones()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'S');
        writer.Write((byte)'K');
        writer.Write((byte)'I');
        writer.Write((byte)'N');

        // Header
        writer.Write(0x00000001u); // Version
        writer.Write(3); // Bone count

        // Bone name hashes (all hashes first)
        writer.Write(0xAAAAAAAAu); // Bone 0
        writer.Write(0xBBBBBBBBu); // Bone 1
        writer.Write(0xCCCCCCCCu); // Bone 2

        // Then matrices
        // Bone 0: Identity matrix
        WriteIdentityMatrix(writer);

        // Bone 1: Translated matrix (translate = 1, 2, 3)
        WriteTranslatedMatrix(writer, 1.0f, 2.0f, 3.0f);

        // Bone 2: Different translation
        WriteTranslatedMatrix(writer, 10.0f, 20.0f, 30.0f);

        return ms.ToArray();
    }

    private static void WriteIdentityMatrix(BinaryWriter writer)
    {
        // Row 0
        writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);
        // Row 1
        writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f);
        // Row 2
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f);
    }

    private static void WriteTranslatedMatrix(BinaryWriter writer, float tx, float ty, float tz)
    {
        // Row 0: Right.X, Up.X, Back.X, Translate.X
        writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f); writer.Write(tx);
        // Row 1: Right.Y, Up.Y, Back.Y, Translate.Y
        writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f); writer.Write(ty);
        // Row 2: Right.Z, Up.Z, Back.Z, Translate.Z
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(1.0f); writer.Write(tz);
    }

    #region Parsing Tests

    [Fact]
    public void SkinBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateSkinBlockEmpty();

        // Act
        var block = new SkinBlock(data);

        // Assert
        block.Tag.Should().Be("SKIN");
        block.ResourceType.Should().Be(SkinBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x00000001u);
        block.Bones.Should().BeEmpty();
    }

    [Fact]
    public void SkinBlock_Parse_SingleBone_ParsesCorrectly()
    {
        // Arrange
        var data = CreateSkinBlockSingleBone();

        // Act
        var block = new SkinBlock(data);

        // Assert
        block.Version.Should().Be(0x00000001u);
        block.Bones.Should().HaveCount(1);

        var bone = block.Bones[0];
        bone.NameHash.Should().Be(0x12345678u);
        bone.InverseBindPose.Should().Be(Matrix43.Identity);
    }

    [Fact]
    public void SkinBlock_Parse_MultipleBones_ParsesCorrectly()
    {
        // Arrange
        var data = CreateSkinBlockMultipleBones();

        // Act
        var block = new SkinBlock(data);

        // Assert
        block.Bones.Should().HaveCount(3);

        block.Bones[0].NameHash.Should().Be(0xAAAAAAAAu);
        block.Bones[1].NameHash.Should().Be(0xBBBBBBBBu);
        block.Bones[2].NameHash.Should().Be(0xCCCCCCCCu);

        // First bone is identity
        block.Bones[0].InverseBindPose.Should().Be(Matrix43.Identity);

        // Second bone has translation (1, 2, 3)
        block.Bones[1].InverseBindPose.Translate.X.Should().Be(1.0f);
        block.Bones[1].InverseBindPose.Translate.Y.Should().Be(2.0f);
        block.Bones[1].InverseBindPose.Translate.Z.Should().Be(3.0f);

        // Third bone has translation (10, 20, 30)
        block.Bones[2].InverseBindPose.Translate.X.Should().Be(10.0f);
        block.Bones[2].InverseBindPose.Translate.Y.Should().Be(20.0f);
        block.Bones[2].InverseBindPose.Translate.Z.Should().Be(30.0f);
    }

    [Fact]
    public void SkinBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'K', (byte)'I', (byte)'N', // Wrong tag
            0x01, 0x00, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00  // Bone count
        };

        // Act & Assert
        var action = () => new SkinBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid SKIN tag*");
    }

    [Fact]
    public void SkinBlock_Parse_InvalidBoneCount_ThrowsException()
    {
        // Arrange - create data with invalid bone count
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'S');
        writer.Write((byte)'K');
        writer.Write((byte)'I');
        writer.Write((byte)'N');
        writer.Write(0x00000001u); // Version
        writer.Write(5000); // Invalid bone count (>1000)
        var data = ms.ToArray();

        // Act & Assert
        var action = () => new SkinBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid SKIN bone count*");
    }

    [Fact]
    public void SkinBlock_Parse_NegativeBoneCount_ThrowsException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'S');
        writer.Write((byte)'K');
        writer.Write((byte)'I');
        writer.Write((byte)'N');
        writer.Write(0x00000001u); // Version
        writer.Write(-1); // Negative bone count
        var data = ms.ToArray();

        // Act & Assert
        var action = () => new SkinBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid SKIN bone count*");
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void SkinBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateSkinBlockEmpty();
        var block = new SkinBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void SkinBlock_Serialize_SingleBone_RoundTrips()
    {
        // Arrange
        var originalData = CreateSkinBlockSingleBone();
        var block = new SkinBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void SkinBlock_Serialize_MultipleBones_RoundTrips()
    {
        // Arrange
        var originalData = CreateSkinBlockMultipleBones();
        var block = new SkinBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void SkinBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateSkinBlockMultipleBones();
        var block = new SkinBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new SkinBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.Bones.Should().HaveCount(block.Bones.Count);

        for (int i = 0; i < block.Bones.Count; i++)
        {
            reparsed.Bones[i].NameHash.Should().Be(block.Bones[i].NameHash);
            reparsed.Bones[i].InverseBindPose.Should().Be(block.Bones[i].InverseBindPose);
        }
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void SkinBlock_FindBone_FoundByHash()
    {
        // Arrange
        var data = CreateSkinBlockMultipleBones();
        var block = new SkinBlock(data);

        // Act
        var bone = block.FindBone(0xBBBBBBBBu);

        // Assert
        bone.Should().NotBeNull();
        bone!.NameHash.Should().Be(0xBBBBBBBBu);
        bone.InverseBindPose.Translate.X.Should().Be(1.0f);
    }

    [Fact]
    public void SkinBlock_FindBone_NotFound_ReturnsNull()
    {
        // Arrange
        var data = CreateSkinBlockMultipleBones();
        var block = new SkinBlock(data);

        // Act
        var bone = block.FindBone(0xDEADBEEFu);

        // Assert
        bone.Should().BeNull();
    }

    [Fact]
    public void SkinBlock_FindBone_FirstBone()
    {
        // Arrange
        var data = CreateSkinBlockMultipleBones();
        var block = new SkinBlock(data);

        // Act
        var bone = block.FindBone(0xAAAAAAAAu);

        // Assert
        bone.Should().NotBeNull();
        bone!.InverseBindPose.Should().Be(Matrix43.Identity);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void SkinBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(SkinBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("SKIN").Should().BeTrue();
    }

    [Fact]
    public void SkinBlock_Registry_CreatesSkinBlock()
    {
        // Arrange
        var data = CreateSkinBlockSingleBone();

        // Act
        var block = RcolBlockRegistry.CreateBlock(SkinBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<SkinBlock>();
        ((SkinBlock)block!).Bones.Should().HaveCount(1);
    }

    #endregion

    #region Bone Class Tests

    [Fact]
    public void Bone_Constructor_Default_HasIdentityMatrix()
    {
        // Act
        var bone = new Bone();

        // Assert
        bone.NameHash.Should().Be(0u);
        bone.InverseBindPose.Should().Be(Matrix43.Identity);
    }

    [Fact]
    public void Bone_Constructor_WithValues_SetsCorrectly()
    {
        // Arrange
        var matrix = new Matrix43(
            new MeshVector3(1, 0, 0),
            new MeshVector3(0, 1, 0),
            new MeshVector3(0, 0, 1),
            new MeshVector3(5, 10, 15)
        );

        // Act
        var bone = new Bone(0xABCDEF01u, matrix);

        // Assert
        bone.NameHash.Should().Be(0xABCDEF01u);
        bone.InverseBindPose.Translate.X.Should().Be(5.0f);
        bone.InverseBindPose.Translate.Y.Should().Be(10.0f);
        bone.InverseBindPose.Translate.Z.Should().Be(15.0f);
    }

    [Fact]
    public void Bone_Equals_IdenticalBones_ReturnsTrue()
    {
        // Arrange
        var bone1 = new Bone(0x12345678u, Matrix43.Identity);
        var bone2 = new Bone(0x12345678u, Matrix43.Identity);

        // Assert
        bone1.Equals(bone2).Should().BeTrue();
    }

    [Fact]
    public void Bone_Equals_DifferentHash_ReturnsFalse()
    {
        // Arrange
        var bone1 = new Bone(0x12345678u, Matrix43.Identity);
        var bone2 = new Bone(0x87654321u, Matrix43.Identity);

        // Assert
        bone1.Equals(bone2).Should().BeFalse();
    }

    [Fact]
    public void Bone_Equals_DifferentMatrix_ReturnsFalse()
    {
        // Arrange
        var matrix1 = Matrix43.Identity;
        var matrix2 = new Matrix43(
            new MeshVector3(1, 0, 0),
            new MeshVector3(0, 1, 0),
            new MeshVector3(0, 0, 1),
            new MeshVector3(1, 2, 3)
        );
        var bone1 = new Bone(0x12345678u, matrix1);
        var bone2 = new Bone(0x12345678u, matrix2);

        // Assert
        bone1.Equals(bone2).Should().BeFalse();
    }

    [Fact]
    public void Bone_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var bone = new Bone(0x12345678u, Matrix43.Identity);

        // Assert
        bone.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Bone_GetHashCode_SameForIdenticalBones()
    {
        // Arrange
        var bone1 = new Bone(0x12345678u, Matrix43.Identity);
        var bone2 = new Bone(0x12345678u, Matrix43.Identity);

        // Assert
        bone1.GetHashCode().Should().Be(bone2.GetHashCode());
    }

    [Fact]
    public void Bone_ToString_ContainsHash()
    {
        // Arrange
        var bone = new Bone(0x12345678u, Matrix43.Identity);

        // Act
        var str = bone.ToString();

        // Assert
        str.Should().Contain("12345678");
    }

    #endregion

    #region Matrix43 Tests

    [Fact]
    public void Matrix43_Identity_HasCorrectValues()
    {
        // Act
        var identity = Matrix43.Identity;

        // Assert
        identity.Right.Should().Be(MeshVector3.UnitX);
        identity.Up.Should().Be(MeshVector3.UnitY);
        identity.Back.Should().Be(MeshVector3.UnitZ);
        identity.Translate.Should().Be(MeshVector3.Zero);
    }

    [Fact]
    public void Matrix43_Size_Is48Bytes()
    {
        // Assert
        Matrix43.Size.Should().Be(48);
    }

    [Fact]
    public void Matrix43_Equals_IdenticalMatrices_ReturnsTrue()
    {
        // Arrange
        var m1 = Matrix43.Identity;
        var m2 = Matrix43.Identity;

        // Assert
        m1.Equals(m2).Should().BeTrue();
        (m1 == m2).Should().BeTrue();
    }

    [Fact]
    public void Matrix43_Equals_DifferentMatrices_ReturnsFalse()
    {
        // Arrange
        var m1 = Matrix43.Identity;
        var m2 = new Matrix43(
            MeshVector3.UnitX,
            MeshVector3.UnitY,
            MeshVector3.UnitZ,
            new MeshVector3(1, 2, 3)
        );

        // Assert
        m1.Equals(m2).Should().BeFalse();
        (m1 != m2).Should().BeTrue();
    }

    [Fact]
    public void Matrix43_ReadWrite_RoundTrips()
    {
        // Arrange
        var original = new Matrix43(
            new MeshVector3(1, 2, 3),
            new MeshVector3(4, 5, 6),
            new MeshVector3(7, 8, 9),
            new MeshVector3(10, 11, 12)
        );
        var buffer = new byte[Matrix43.Size];
        int writePos = 0;

        // Act
        original.Write(buffer, ref writePos);
        int readPos = 0;
        var read = Matrix43.Read(buffer, ref readPos);

        // Assert
        writePos.Should().Be(48);
        readPos.Should().Be(48);
        read.Should().Be(original);
    }

    #endregion
}
