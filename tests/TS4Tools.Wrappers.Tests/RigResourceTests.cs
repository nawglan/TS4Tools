using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RigResource parsing and serialization.
/// </summary>
public class RigResourceTests
{
    /// <summary>
    /// Creates a minimal Clear format RIG with version 4.
    /// </summary>
    private static byte[] CreateClearFormatRig()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version
        writer.Write(4u); // Major
        writer.Write(2u); // Minor

        // Bone count
        writer.Write(1); // 1 bone

        // Bone 1: Position, Orientation, Scaling, Name, indices, hash, unknown2
        writer.Write(1.0f); // Position.X
        writer.Write(2.0f); // Position.Y
        writer.Write(3.0f); // Position.Z
        writer.Write(0.0f); // Orientation.X (quaternion)
        writer.Write(0.0f); // Orientation.Y
        writer.Write(0.0f); // Orientation.Z
        writer.Write(1.0f); // Orientation.W
        writer.Write(1.0f); // Scaling.X
        writer.Write(1.0f); // Scaling.Y
        writer.Write(1.0f); // Scaling.Z

        // Name: "root"
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes("root");
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write(-1); // OpposingBoneIndex
        writer.Write(-1); // ParentBoneIndex
        writer.Write(0x12345678u); // Hash
        writer.Write(0u); // Unknown2

        // Skeleton name (version 4+)
        byte[] skelNameBytes = System.Text.Encoding.UTF8.GetBytes("TestSkeleton");
        writer.Write(skelNameBytes.Length);
        writer.Write(skelNameBytes);

        // IK chain count (version 4+)
        writer.Write(0); // No IK chains

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a Clear format RIG with IK chains.
    /// </summary>
    private static byte[] CreateClearFormatRigWithIkChains()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version
        writer.Write(4u); // Major
        writer.Write(2u); // Minor

        // Bone count = 0 (for simplicity)
        writer.Write(0);

        // Skeleton name
        byte[] skelNameBytes = System.Text.Encoding.UTF8.GetBytes("IKTest");
        writer.Write(skelNameBytes.Length);
        writer.Write(skelNameBytes);

        // IK chain count
        writer.Write(1); // 1 IK chain

        // IK chain 1
        writer.Write(2); // Bone count in chain
        writer.Write(0); // Bone 0
        writer.Write(1); // Bone 1

        // 11 info node indices (version 4+)
        for (int i = 0; i < 11; i++)
            writer.Write(i); // InfoNode indices

        writer.Write(100); // PoleVectorIndex
        writer.Write(101); // SlotInfoIndex (version 4+)
        writer.Write(102); // SlotOffsetIndex
        writer.Write(103); // RootIndex

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a RawGranny format RIG (passthrough data).
    /// </summary>
    private static byte[] CreateRawGrannyRig()
    {
        // Just some arbitrary bytes that don't match Clear format detection
        return [0x47, 0x52, 0x41, 0x4E, 0x4E, 0x59, 0x32, 0x00, 0x01, 0x02, 0x03, 0x04];
    }

    /// <summary>
    /// Creates a WrappedGranny format RIG.
    /// </summary>
    private static byte[] CreateWrappedGrannyRig()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic: 0x8EAF13DE followed by 0x00000000
        writer.Write(0x8EAF13DEu);
        writer.Write(0x00000000u);
        writer.Write([0x01, 0x02, 0x03, 0x04]); // Some data

        return ms.ToArray();
    }

    [Fact]
    public void RigResource_Parse_ClearFormat_ParsesCorrectly()
    {
        // Arrange
        var data = CreateClearFormatRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        // Act
        var resource = new RigResource(key, data);

        // Assert
        resource.Format.Should().Be(RigFormat.Clear);
        resource.Major.Should().Be(4);
        resource.Minor.Should().Be(2);
        resource.Bones.Should().HaveCount(1);
        resource.Bones[0].Name.Should().Be("root");
        resource.Bones[0].Hash.Should().Be(0x12345678u);
        resource.Bones[0].Position.X.Should().Be(1.0f);
        resource.Bones[0].Position.Y.Should().Be(2.0f);
        resource.Bones[0].Position.Z.Should().Be(3.0f);
        resource.Bones[0].ParentBoneIndex.Should().Be(-1);
        resource.SkeletonName.Should().Be("TestSkeleton");
        resource.IkChains.Should().BeEmpty();
    }

    [Fact]
    public void RigResource_Parse_ClearFormatWithIkChains_ParsesCorrectly()
    {
        // Arrange
        var data = CreateClearFormatRigWithIkChains();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        // Act
        var resource = new RigResource(key, data);

        // Assert
        resource.Format.Should().Be(RigFormat.Clear);
        resource.SkeletonName.Should().Be("IKTest");
        resource.IkChains.Should().HaveCount(1);
        resource.IkChains[0].Bones.Should().HaveCount(2);
        resource.IkChains[0].Bones[0].Should().Be(0);
        resource.IkChains[0].Bones[1].Should().Be(1);
        resource.IkChains[0].PoleVectorIndex.Should().Be(100);
        resource.IkChains[0].SlotInfoIndex.Should().Be(101);
        resource.IkChains[0].SlotOffsetIndex.Should().Be(102);
        resource.IkChains[0].RootIndex.Should().Be(103);
    }

    [Fact]
    public void RigResource_Parse_RawGrannyFormat_DetectsCorrectly()
    {
        // Arrange
        var data = CreateRawGrannyRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        // Act
        var resource = new RigResource(key, data);

        // Assert
        resource.Format.Should().Be(RigFormat.RawGranny);
        resource.RawGrannyData.ToArray().Should().BeEquivalentTo(data);
        resource.Bones.Should().BeEmpty();
    }

    [Fact]
    public void RigResource_Parse_WrappedGrannyFormat_DetectsCorrectly()
    {
        // Arrange
        var data = CreateWrappedGrannyRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        // Act
        var resource = new RigResource(key, data);

        // Assert
        resource.Format.Should().Be(RigFormat.WrappedGranny);
        resource.RawGrannyData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public void RigResource_Serialize_ClearFormat_RoundTrips()
    {
        // Arrange
        var originalData = CreateClearFormatRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);
        var resource = new RigResource(key, originalData);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RigResource_Serialize_RawGrannyFormat_RoundTrips()
    {
        // Arrange
        var originalData = CreateRawGrannyRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);
        var resource = new RigResource(key, originalData);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RigResource_EmptyData_InitializesDefaults()
    {
        // Arrange
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        // Act
        var resource = new RigResource(key, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Format.Should().Be(RigFormat.Clear);
        resource.Major.Should().Be(4);
        resource.Minor.Should().Be(2);
        resource.Bones.Should().BeEmpty();
        resource.IkChains.Should().BeEmpty();
    }

    [Fact]
    public void RigResource_AddBone_AddsBoneToList()
    {
        // Arrange
        var key = new ResourceKey(RigResource.TypeId, 0, 0);
        var resource = new RigResource(key, ReadOnlyMemory<byte>.Empty);
        var bone = new RigBone
        {
            Name = "TestBone",
            Hash = 0xABCD1234
        };

        // Act
        resource.AddBone(bone);

        // Assert
        resource.Bones.Should().HaveCount(1);
        resource.Bones[0].Name.Should().Be("TestBone");
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RigResource_FindBone_FindsByName()
    {
        // Arrange
        var data = CreateClearFormatRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);
        var resource = new RigResource(key, data);

        // Act
        var bone = resource.FindBone("root");

        // Assert
        bone.Should().NotBeNull();
        bone!.Name.Should().Be("root");
    }

    [Fact]
    public void RigResource_FindBone_FindsByHash()
    {
        // Arrange
        var data = CreateClearFormatRig();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);
        var resource = new RigResource(key, data);

        // Act
        var bone = resource.FindBone(0x12345678u);

        // Assert
        bone.Should().NotBeNull();
        bone!.Hash.Should().Be(0x12345678u);
    }

    [Fact]
    public void RigBone_Equals_IdenticalBones_ReturnsTrue()
    {
        // Arrange
        var bone1 = new RigBone
        {
            Name = "Test",
            Hash = 0x1234,
            Position = new MeshVector3(1, 2, 3)
        };
        var bone2 = new RigBone
        {
            Name = "Test",
            Hash = 0x1234,
            Position = new MeshVector3(1, 2, 3)
        };

        // Assert
        bone1.Equals(bone2).Should().BeTrue();
    }

    [Fact]
    public void IkChain_Equals_IdenticalChains_ReturnsTrue()
    {
        // Arrange
        var chain1 = new IkChain { PoleVectorIndex = 10, RootIndex = 20 };
        chain1.Bones.Add(1);
        chain1.Bones.Add(2);

        var chain2 = new IkChain { PoleVectorIndex = 10, RootIndex = 20 };
        chain2.Bones.Add(1);
        chain2.Bones.Add(2);

        // Assert
        chain1.Equals(chain2).Should().BeTrue();
    }

    /// <summary>
    /// Regression (real data): game rigs are Clear format major 3 / minor 1 with no skeleton name but a
    /// trailing IK chain list. Legacy reads the list when (major &gt;= 4 || isTS4) and isTS4 is always true
    /// (RigResource.cs lines 33, 93-96, 139-149); the port skipped it for major 3 and dropped 4 bytes.
    /// </summary>
    [Fact]
    public void RigResource_Major3_RealLayout_RoundTripsWithIkChains()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(3u); // Major
        writer.Write(1u); // Minor
        writer.Write(1);  // bone count
        for (int f = 0; f < 10; f++) writer.Write(f == 6 ? 1.0f : 0.0f); // position, orientation, scaling
        writer.Write(7);
        writer.Write("_FX_INT"u8.ToArray());
        writer.Write(9);   // opposing
        writer.Write(1);   // parent
        writer.Write(0xCEA49514u); // hash
        writer.Write(0x913Cu);     // unknown2
        // no skeleton name for major 3 (isTS4)
        writer.Write(1);   // IK chain count
        writer.Write(2);   // chain bone count
        writer.Write(0);
        writer.Write(0);
        writer.Write(5);   // pole vector
        writer.Write(6);   // slot offset
        writer.Write(7);   // root
        var data = ms.ToArray();

        var resource = new RigResource(new ResourceKey(0x8EAF13DE, 0, 0), data);

        resource.Major.Should().Be(3);
        resource.IkChains.Should().HaveCount(1);
        resource.IkChains[0].RootIndex.Should().Be(7);
        resource.Data.ToArray().Should().Equal(data);
    }

    /// <summary>
    /// Legacy RigResource.cs lines 94, 213 and 226-227 read/write the bone and skeleton names as
    /// <c>new String(r.ReadChars(r.ReadInt32()))</c> / <c>w.Write(Name.Length); w.Write(Name.ToCharArray())</c>:
    /// an Int32 char count followed by the chars as UTF-8, not a byte count.
    /// </summary>
    [Fact]
    public void RigResource_NonAsciiNames_UseCharCountPrefix()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(4u); // Major
        writer.Write(2u); // Minor
        writer.Write(1); // bone count
        foreach (float f in new[] { 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f })
            writer.Write(f); // Position, Orientation, Scaling
        writer.Write(1); // bone name: 1 char ...
        writer.Write(new byte[] { 0xC3, 0xA9 }); // ... "é" as UTF-8
        writer.Write(-1); // OpposingBoneIndex
        writer.Write(-1); // ParentBoneIndex
        writer.Write(0x12345678u); // Hash
        writer.Write(0u); // Unknown2
        writer.Write(1); // skeleton name: 1 char ...
        writer.Write(new byte[] { 0xC3, 0xA9 }); // ... "é"
        writer.Write(0); // IK chain count
        var data = ms.ToArray();

        var resource = new RigResource(new ResourceKey(RigResource.TypeId, 0, 0), data);

        resource.Bones.Should().ContainSingle().Which.Name.Should().Be("é");
        resource.Bones[0].Hash.Should().Be(0x12345678u);
        resource.SkeletonName.Should().Be("é");
        resource.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void RigResource_NewNonAsciiNames_SerializeCharCount()
    {
        var resource = new RigResource(new ResourceKey(RigResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty);
        resource.SkeletonName = "é";
        resource.AddBone(new RigBone { Name = "é", Hash = 1 });

        byte[] serialized = resource.Data.ToArray();

        byte[] charCountThenUtf8 = [0x01, 0x00, 0x00, 0x00, 0xC3, 0xA9];
        int first = serialized.AsSpan().IndexOf(charCountThenUtf8);
        first.Should().BeGreaterThan(0, "the bone name is written as char count 1 + C3 A9");
        serialized.AsSpan(first + 1).IndexOf(charCountThenUtf8).Should().BeGreaterThan(0,
            "the skeleton name is written the same way");
        var parsed = new RigResource(new ResourceKey(RigResource.TypeId, 0, 0), serialized);
        parsed.Bones[0].Name.Should().Be("é");
        parsed.SkeletonName.Should().Be("é");
    }
}
