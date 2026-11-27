using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for the legacy VBUF.cs vertex decode/encode helpers (GetVertices, GetBoundingBox, SetVertices)
/// and byte-identical VBUF round trips.
/// </summary>
public class VbufVertexCodecTests
{
    private const int Stride = 20;

    /// <summary>VRTF: Float3 position @0, Short2 UV @12, ColorUByte4 colour @16, stride 20.</summary>
    private static VrtfBlock CreateVrtf()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'V'); w.Write((byte)'R'); w.Write((byte)'T'); w.Write((byte)'F');
        w.Write(2u); w.Write(Stride); w.Write(3); w.Write(0u);
        w.Write(new byte[] { (byte)ElementUsage.Position, 0, (byte)ElementFormat.Float3, 0 });
        w.Write(new byte[] { (byte)ElementUsage.UV, 0, (byte)ElementFormat.Short2, 12 });
        w.Write(new byte[] { (byte)ElementUsage.Colour, 0, (byte)ElementFormat.ColorUByte4, 16 });
        return new VrtfBlock(ms.ToArray());
    }

    private static byte[] Vertex(float x, float y, float z, short u, short v, byte r, byte g, byte b, byte a)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(x); w.Write(y); w.Write(z);
        w.Write(u); w.Write(v);
        w.Write(r); w.Write(g); w.Write(b); w.Write(a);
        return ms.ToArray();
    }

    private static VbufBlock CreateVbuf(params byte[][] vertices)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'V'); w.Write((byte)'B'); w.Write((byte)'U'); w.Write((byte)'F');
        w.Write(0x101u);
        w.Write(0u);
        w.Write(0u);
        foreach (var v in vertices) w.Write(v);
        return new VbufBlock(ms.ToArray());
    }

    [Fact]
    public void GetVertices_DecodesFloatShortAndColourElements()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(
            Vertex(1, 2, 3, 100, -200, 255, 128, 0, 255),
            Vertex(-4, 5, 6, 0, 0, 0, 0, 0, 0));

        var verts = vbuf.GetVertices(vrtf, 0, 2, [0.01f]);

        verts.Should().HaveCount(2);
        verts[0].Position.Should().Equal(1f, 2f, 3f);
        verts[0].Normal.Should().BeNull();
        verts[0].UV.Should().HaveCount(1);
        verts[0].UV![0][0].Should().BeApproximately(1f, 1e-5f);
        verts[0].UV![0][1].Should().BeApproximately(-2f, 1e-5f);
        verts[0].Color.Should().NotBeNull();
        verts[0].Color![0].Should().Be(1f);
        verts[0].Color![1].Should().BeApproximately(128f / 255f, 1e-6f);
        verts[1].Position.Should().Equal(-4f, 5f, 6f);
    }

    [Fact]
    public void GetVertices_ByMesh_UsesStreamOffsetAndCount()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(
            Vertex(0, 0, 0, 0, 0, 0, 0, 0, 0),
            Vertex(7, 8, 9, 0, 0, 0, 0, 0, 0));
        var mesh = new MlodMesh { StreamOffset = Stride, VertexCount = 1 };

        var verts = vbuf.GetVertices(mesh, vrtf, null);

        verts.Should().ContainSingle().Which.Position.Should().Equal(7f, 8f, 9f);
    }

    [Fact]
    public void GetVertices_ByGeometryState_UsesMinVertexIndex()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(
            Vertex(0, 0, 0, 0, 0, 0, 0, 0, 0),
            Vertex(1, 1, 1, 0, 0, 0, 0, 0, 0),
            Vertex(2, 2, 2, 0, 0, 0, 0, 0, 0));
        var mesh = new MlodMesh { StreamOffset = 0, VertexCount = 3 };
        var geo = new MlodGeometryState(1, 0, 1, 2, 0);

        var verts = vbuf.GetVertices(mesh, vrtf, geo, null);

        verts.Select(v => v.Position![0]).Should().Equal(1f, 2f);
    }

    [Fact]
    public void GetVertices_RangeOutsideBuffer_Throws()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(Vertex(0, 0, 0, 0, 0, 0, 0, 0, 0));

        var action = () => vbuf.GetVertices(vrtf, 0, 2, null);

        action.Should().Throw<InvalidDataException>().WithMessage("*outside*");
    }

    [Fact]
    public void GetVertices_NegativeOffsetOrCount_Throws()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(Vertex(0, 0, 0, 0, 0, 0, 0, 0, 0));

        ((Action)(() => vbuf.GetVertices(vrtf, -1, 1, null))).Should().Throw<InvalidDataException>();
        ((Action)(() => vbuf.GetVertices(vrtf, 0, -1, null))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void GetBoundingBox_CoversMeshVertices()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(
            Vertex(-1, 5, 0, 0, 0, 0, 0, 0, 0),
            Vertex(3, -2, 9, 0, 0, 0, 0, 0, 0));
        var mesh = new MlodMesh { StreamOffset = 0, VertexCount = 2 };

        vbuf.GetBoundingBox(mesh, vrtf).Should().Be(new BoundingBox(-1, -2, 0, 3, 5, 9));
    }

    [Fact]
    public void GetBoundingBox_NoVertices_ReturnsEmpty()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf();

        vbuf.GetBoundingBox(new MlodMesh(), vrtf).Should().Be(BoundingBox.Empty);
    }

    [Fact]
    public void ReadFloatData_Short4_UsesLastWordAsScalar()
    {
        var layout = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Short4, 0);
        byte[] data = new byte[8];
        BitConverter.GetBytes((short)1000).CopyTo(data, 0);
        BitConverter.GetBytes((short)-500).CopyTo(data, 2);
        BitConverter.GetBytes((short)250).CopyTo(data, 4);
        BitConverter.GetBytes((ushort)1000).CopyTo(data, 6);

        var result = VbufBlock.ReadFloatData(data, layout);

        result.Should().Equal(1f, -0.5f, 0.25f);
    }

    [Fact]
    public void ReadFloatData_ColorNormal_DecodesDeterminant()
    {
        var layout = new ElementLayout(ElementUsage.Normal, 0, ElementFormat.ColorUByte4, 0);

        VbufBlock.ReadFloatData([128, 0, 255, 255], layout).Should().Equal(
            (255 + 1) / 128f - 1, -1f, (128 + 1) / 128f - 1, 1f);
        VbufBlock.ReadFloatData([0, 0, 0, 127], layout)[3].Should().Be(0f);
        VbufBlock.ReadFloatData([0, 0, 0, 0], layout)[3].Should().Be(-1f);
    }

    [Fact]
    public void SetVertices_ThenGetVertices_RoundTrips()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(Vertex(0, 0, 0, 0, 0, 0, 0, 0, 0));
        var mlod = new MlodBlock();
        var mesh = new MlodMesh { StreamOffset = 0, VertexCount = 1 };
        mlod.Meshes.Add(mesh);

        var input = new[]
        {
            new VbufVertex { Position = [1f, 2f, 3f], UV = [[0.5f, -0.5f]], Color = [1f, 0f, 0.5f, 1f] },
            new VbufVertex { Position = [-1f, -2f, -3f], UV = [[0.25f, 0.75f]], Color = [0f, 1f, 0f, 0f] },
        };

        bool okay = vbuf.SetVertices(mlod, 0, vrtf, input, [0.01f]);

        okay.Should().BeTrue();
        mesh.VertexCount.Should().Be(2);
        vbuf.Buffer.Length.Should().Be(2 * Stride);
        var back = vbuf.GetVertices(mesh, vrtf, [0.01f]);
        back[0].Position.Should().Equal(1f, 2f, 3f);
        back[1].Position.Should().Equal(-1f, -2f, -3f);
        back[0].UV![0][0].Should().BeApproximately(0.5f, 1e-5f);
        back[1].UV![0][1].Should().BeApproximately(0.75f, 1e-5f);
        back[0].Color![0].Should().Be(1f);
        back[0].Color![2].Should().BeApproximately(0.5f, 1f / 255f);
    }

    [Fact]
    public void SetVertices_GrowingMesh_ShiftsLaterMeshesSharingTheBuffer()
    {
        var vrtf = CreateVrtf();
        var vbuf = CreateVbuf(
            Vertex(1, 1, 1, 0, 0, 0, 0, 0, 0),
            Vertex(2, 2, 2, 0, 0, 0, 0, 0, 0));
        var mlod = new MlodBlock();
        var shared = new RcolChunkReference(3);
        var first = new MlodMesh { StreamOffset = 0, VertexCount = 1, VertexBufferIndex = shared };
        var second = new MlodMesh { StreamOffset = Stride, VertexCount = 1, VertexBufferIndex = shared };
        var other = new MlodMesh { StreamOffset = Stride, VertexCount = 1, VertexBufferIndex = new RcolChunkReference(9) };
        mlod.Meshes.AddRange([first, second, other]);

        var grown = new[]
        {
            new VbufVertex { Position = [10f, 10f, 10f], UV = [[0f, 0f]], Color = [0f, 0f, 0f, 0f] },
            new VbufVertex { Position = [11f, 11f, 11f], UV = [[0f, 0f]], Color = [0f, 0f, 0f, 0f] },
            new VbufVertex { Position = [12f, 12f, 12f], UV = [[0f, 0f]], Color = [0f, 0f, 0f, 0f] },
        };

        vbuf.SetVertices(mlod, first, vrtf, grown, null);

        first.VertexCount.Should().Be(3);
        vbuf.Buffer.Length.Should().Be(4 * Stride);
        second.StreamOffset.Should().Be(3 * Stride);
        other.StreamOffset.Should().Be(Stride, "meshes using another buffer are untouched");
        vbuf.GetVertices(second, vrtf, null)[0].Position.Should().Equal(2f, 2f, 2f);
    }

    [Fact]
    public void SetVertices_BadMeshIndex_Throws()
    {
        var vbuf = CreateVbuf();

        var action = () => vbuf.SetVertices(new MlodBlock(), 0, CreateVrtf(), [], null);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Serialize_PreservesRawBufferByteIdentical()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'V'); w.Write((byte)'B'); w.Write((byte)'U'); w.Write((byte)'F');
        w.Write(0x101u);
        w.Write((uint)(VbufFormat.Collapsed | VbufFormat.Dynamic));
        w.Write(0x00000007u); // swizzle info: public ref #6
        w.Write(Enumerable.Range(0, 57).Select(i => (byte)(i * 7)).ToArray()); // arbitrary, not stride aligned
        var original = ms.ToArray();

        var block = new VbufBlock(original);

        block.Flags.Should().Be(VbufFormat.Collapsed | VbufFormat.Dynamic);
        block.SwizzleInfo.RawValue.Should().Be(7u);
        block.Buffer.Should().HaveCount(57);
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_Truncated_Throws()
    {
        var action = () => new VbufBlock("VBUF"u8.ToArray().Concat(new byte[6]).ToArray());

        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Vbuf2Block_ParsesAndRoundTripsLikeVbuf()
    {
        var block = CreateVbuf(Vertex(1, 2, 3, 0, 0, 0, 0, 0, 0));
        var shadow = new Vbuf2Block(block.Serialize().Span);

        shadow.ResourceType.Should().Be(Vbuf2Block.TypeId);
        shadow.Serialize().ToArray().Should().Equal(block.Serialize().ToArray());
    }
}
