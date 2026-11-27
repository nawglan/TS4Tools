using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MlodBlock. Byte layouts follow legacy MLOD.cs: tag, version, int32 mesh count, then per mesh a
/// uint32 size and the body (name, 4 chunk refs, packed primitive type/flags, stream offset, start vertex,
/// start index, min vertex index, vertex count, primitive count, bounding box, skin ref, joint list,
/// scale/offset ref, geometry state list, then ParentName+MirrorPlane when version &gt; 0x201 and Unknown1
/// when version &gt; 0x203).
/// </summary>
public class MlodBlockTests
{
    private static byte[] MeshBody(uint version, uint name, uint[] joints, (uint Name, int Start, int Min, int Count, int Prims)[] states)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(name);
        w.Write(0x00000001u); // material: public ref #0
        w.Write(0x00000002u); // vrtf
        w.Write(0x00000003u); // vbuf
        w.Write(0x00000004u); // ibuf
        w.Write(0x00005003u); // packed: primitive type 3 (TriangleList) | flags (0x10 ShadowCaster | 0x40 Pickable) << 8
        w.Write(0x100u);      // stream offset
        w.Write(5);           // start vertex
        w.Write(6);           // start index
        w.Write(7);           // min vertex index
        w.Write(8);           // vertex count
        w.Write(9);           // primitive count
        w.Write(-1f); w.Write(-2f); w.Write(-3f); w.Write(1f); w.Write(2f); w.Write(3f); // bbox
        w.Write(0x00000005u); // skin
        w.Write(joints.Length);
        foreach (var j in joints) w.Write(j);
        w.Write(0x00000006u); // scale offset
        w.Write(states.Length);
        foreach (var s in states)
        {
            w.Write(s.Name); w.Write(s.Start); w.Write(s.Min); w.Write(s.Count); w.Write(s.Prims);
        }
        if (version > 0x201)
        {
            w.Write(0xAABBCCDDu); // parent name
            w.Write(0.5f); w.Write(0.25f); w.Write(0.125f); w.Write(1f); // mirror plane
        }
        if (version > 0x203)
            w.Write(0x77u); // unknown1
        return ms.ToArray();
    }

    private static byte[] CreateMlod(uint version, int meshCount)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'M'); w.Write((byte)'L'); w.Write((byte)'O'); w.Write((byte)'D');
        w.Write(version);
        w.Write(meshCount);
        for (int i = 0; i < meshCount; i++)
        {
            var body = MeshBody(version, (uint)(0x1000 + i), i == 0 ? [10u, 11u, 12u] : [],
                i == 0 ? [(0xAAAAu, 1, 2, 3, 4), (0xBBBBu, 5, 6, 7, 8)] : []);
            w.Write((uint)body.Length);
            w.Write(body);
        }
        return ms.ToArray();
    }

    [Fact]
    public void Parse_Empty_ParsesHeader()
    {
        var block = new MlodBlock(CreateMlod(0x202, 0));

        block.Tag.Should().Be("MLOD");
        block.ResourceType.Should().Be(0x01D10F34u);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x202u);
        block.MeshCount.Should().Be(0);
    }

    [Fact]
    public void Parse_Meshes_ReadsEveryField()
    {
        var block = new MlodBlock(CreateMlod(0x204, 2));

        block.Meshes.Should().HaveCount(2);
        var m = block.Meshes[0];
        m.Name.Should().Be(0x1000u);
        m.MaterialIndex.Should().Be(new RcolChunkReference(1));
        m.VertexFormatIndex.RawValue.Should().Be(2u);
        m.VertexBufferIndex.RawValue.Should().Be(3u);
        m.IndexBufferIndex.RawValue.Should().Be(4u);
        m.PrimitiveType.Should().Be(ModelPrimitiveType.TriangleList);
        m.Flags.Should().Be(MeshOption.ShadowCaster | MeshOption.Pickable);
        m.IsShadowCaster.Should().BeTrue();
        m.StreamOffset.Should().Be(0x100u);
        m.StartVertex.Should().Be(5);
        m.StartIndex.Should().Be(6);
        m.MinVertexIndex.Should().Be(7);
        m.VertexCount.Should().Be(8);
        m.PrimitiveCount.Should().Be(9);
        m.Bounds.Should().Be(new BoundingBox(-1, -2, -3, 1, 2, 3));
        m.SkinControllerIndex.RawValue.Should().Be(5u);
        m.JointReferences.Should().Equal(10u, 11u, 12u);
        m.ScaleOffsetIndex.RawValue.Should().Be(6u);
        m.GeometryStates.Should().HaveCount(2);
        m.GeometryStates[1].Should().Be(new MlodGeometryState(0xBBBB, 5, 6, 7, 8));
        m.ParentName.Should().Be(0xAABBCCDDu);
        m.MirrorPlane.Should().Be(new MeshVector4(0.5f, 0.25f, 0.125f, 1f));
        m.Unknown1.Should().Be(0x77u);

        block.Meshes[1].JointReferences.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0x200u)]
    [InlineData(0x201u)]
    [InlineData(0x202u)]
    [InlineData(0x203u)]
    [InlineData(0x204u)]
    [InlineData(0x205u)]
    public void Serialize_AllVersions_RoundTripsByteIdentical(uint version)
    {
        var original = CreateMlod(version, 3);
        var block = new MlodBlock(original);

        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_OlderVersion_LeavesVersionGatedFieldsDefault()
    {
        var block = new MlodBlock(CreateMlod(0x201, 1));

        block.Meshes[0].ParentName.Should().Be(0);
        block.Meshes[0].MirrorPlane.Should().Be(MeshVector4.Zero);
        block.Meshes[0].Unknown1.Should().Be(0);
    }

    [Fact]
    public void Serialize_AfterEdit_RewritesMeshSizeAndReparses()
    {
        var block = new MlodBlock(CreateMlod(0x202, 1));
        block.Meshes[0].JointReferences.Add(99);
        block.Meshes[0].GeometryStates.RemoveAt(0);
        block.Meshes[0].VertexCount = 123;

        var reparsed = new MlodBlock(block.Serialize().Span);

        reparsed.Meshes[0].JointReferences.Should().Equal(10u, 11u, 12u, 99u);
        reparsed.Meshes[0].GeometryStates.Should().ContainSingle();
        reparsed.Meshes[0].VertexCount.Should().Be(123);
    }

    [Fact]
    public void Parse_InvalidTag_Throws()
    {
        var data = CreateMlod(0x202, 0);
        data[0] = (byte)'X';

        var action = () => new MlodBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid MLOD tag*");
    }

    [Fact]
    public void Parse_MeshCountLargerThanData_Throws()
    {
        var data = CreateMlod(0x202, 0);
        BitConverter.GetBytes(1000).CopyTo(data, 8);

        var action = () => new MlodBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Parse_NegativeMeshCount_Throws()
    {
        var data = CreateMlod(0x202, 0);
        BitConverter.GetBytes(-1).CopyTo(data, 8);

        var action = () => new MlodBlock(data);

        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_Truncated_Throws()
    {
        var data = CreateMlod(0x204, 1);

        for (int len = 0; len < data.Length; len++)
        {
            var prefix = data.AsSpan(0, len).ToArray();
            var action = () => new MlodBlock(prefix);
            action.Should().Throw<InvalidDataException>($"prefix of {len} bytes is truncated");
        }
    }

    [Fact]
    public void Parse_MeshSizeMismatch_Throws()
    {
        var data = CreateMlod(0x202, 1);
        // The mesh size field sits right after the 12 byte header
        BitConverter.GetBytes(BitConverter.ToUInt32(data, 12) + 4).CopyTo(data, 12);

        var action = () => new MlodBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*size mismatch*");
    }

    [Fact]
    public void Parse_HugeJointCount_Throws()
    {
        var data = CreateMlod(0x202, 1);
        // joint count follows 12 header + 4 size + 4*5 refs/name + 4 packed + 4*6 ints + 24 bbox + 4 skin = offset 92
        BitConverter.GetBytes(int.MaxValue).CopyTo(data, 12 + 4 + 20 + 4 + 24 + 24 + 4);

        var action = () => new MlodBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Registry_CreatesMlodBlock()
    {
        RcolBlockRegistry.IsRegistered(MlodBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("MLOD").Should().BeTrue();

        RcolBlockRegistry.CreateBlock(MlodBlock.TypeId, CreateMlod(0x202, 1)).Should().BeOfType<MlodBlock>();
    }

    [Fact]
    public void EmptyConstructor_SerializesToDefaultHeader()
    {
        var block = new MlodBlock();

        block.Version.Should().Be(0x202u);
        block.Serialize().ToArray().Should().Equal(CreateMlod(0x202, 0));
    }

    /// <summary>
    /// Regression (real data): MLOD 0x206 meshes are longer than the newest legacy layout (MLOD.cs lines 353-360
    /// would throw "Expected end"); they are refused as unsupported, while the same mismatch at 0x205 stays a
    /// format error.
    /// </summary>
    [Theory]
    [InlineData(0x206u, true)]
    [InlineData(0x205u, false)]
    public void Parse_MeshLongerThanLegacyLayout_RefusedOnlyForNewerVersions(uint version, bool unsupported)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("MLOD"u8);
        w.Write(version);
        w.Write(1);
        var body = MeshBody(version, 0x1000, [], []);
        w.Write((uint)body.Length + 48); // real 0x206 meshes carry 48 bytes the legacy layout does not read
        w.Write(body);
        w.Write(new byte[48]);

        var action = () => new MlodBlock(ms.ToArray());

        if (unsupported)
            action.Should().Throw<UnsupportedResourceVersionException>();
        else
            action.Should().Throw<InvalidDataException>().Which.Should().NotBeOfType<UnsupportedResourceVersionException>();
    }
}
