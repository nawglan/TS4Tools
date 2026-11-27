using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for VbsiBlock. Layout follows legacy VBSI.cs: no tag; int32 segment count; each segment is
/// vertexSize, vertexCount, byteOffset followed by vertexSize/4 swizzle commands (no count of their own).
/// ResourceType is 0xFFFFFFFF.
/// </summary>
public class VbsiBlockTests
{
    private static byte[] CreateVbsi(params (int Size, int Count, uint Offset, uint[] Swizzles)[] segments)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(segments.Length);
        foreach (var s in segments)
        {
            w.Write(s.Size);
            w.Write(s.Count);
            w.Write(s.Offset);
            foreach (var cmd in s.Swizzles) w.Write(cmd);
        }
        return ms.ToArray();
    }

    [Fact]
    public void TypeId_MatchesLegacy()
    {
        VbsiBlock.TypeId.Should().Be(0xFFFFFFFFu);
        new VbsiBlock().ResourceType.Should().Be(0xFFFFFFFFu);
        new VbsiBlock().Tag.Should().Be("VBSI");
    }

    [Fact]
    public void Parse_Segments_ReadsSwizzles()
    {
        var data = CreateVbsi((12, 100, 0, [1u, 1u, 1u]), (8, 5, 1200, [2u, 0u]));

        var block = new VbsiBlock(data);

        block.Segments.Should().HaveCount(2);
        block.Segments[0].VertexSize.Should().Be(12);
        block.Segments[0].VertexCount.Should().Be(100);
        block.Segments[0].Swizzles.Should().Equal(SwizzleCmd.Swizzle32, SwizzleCmd.Swizzle32, SwizzleCmd.Swizzle32);
        block.Segments[1].ByteOffset.Should().Be(1200u);
        block.Segments[1].Swizzles.Should().Equal(SwizzleCmd.Swizzle16x2, SwizzleCmd.None);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Serialize_RoundTripsByteIdentical(int segmentCount)
    {
        var segments = Enumerable.Range(0, segmentCount)
            .Select(i => (Size: 16, Count: 10 * (i + 1), Offset: (uint)(i * 160), Swizzles: new uint[] { 1, 1, 2, (uint)(i % 3) }))
            .ToArray();
        var original = CreateVbsi(segments);

        new VbsiBlock(original).Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_EmptySegmentWithZeroVertexSize_Works()
    {
        var original = CreateVbsi((0, 0, 0, []));

        var block = new VbsiBlock(original);

        block.Segments.Should().ContainSingle().Which.Swizzles.Should().BeEmpty();
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_Truncated_Throws()
    {
        var data = CreateVbsi((12, 100, 0, [1u, 1u, 1u]));

        for (int len = 0; len < data.Length; len++)
        {
            var prefix = data.AsSpan(0, len).ToArray();
            var action = () => new VbsiBlock(prefix);
            action.Should().Throw<InvalidDataException>($"prefix of {len} bytes is truncated");
        }
    }

    [Fact]
    public void Parse_SegmentCountLargerThanData_Throws()
    {
        var data = CreateVbsi();
        BitConverter.GetBytes(1_000_000).CopyTo(data, 0);

        var action = () => new VbsiBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Parse_NegativeVertexSize_Throws()
    {
        var data = CreateVbsi((12, 1, 0, [1u, 1u, 1u]));
        BitConverter.GetBytes(-4).CopyTo(data, 4);

        var action = () => new VbsiBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid VBSI vertex size*");
    }

    [Fact]
    public void FromMesh_BuildsSwizzlesFromVertexFormat()
    {
        // VRTF: Float3 position (12) + Short4N tangent-like (8) + Float2 (8)... stride 28
        var vrtf = new VrtfBlock();
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write((byte)'V'); w.Write((byte)'R'); w.Write((byte)'T'); w.Write((byte)'F');
            w.Write(2u); w.Write(28); w.Write(3); w.Write(0u);
            w.Write(new byte[] { (byte)ElementUsage.Position, 0, (byte)ElementFormat.Float3, 0 });
            w.Write(new byte[] { (byte)ElementUsage.Tangent, 0, (byte)ElementFormat.Short4N, 12 });
            w.Write(new byte[] { (byte)ElementUsage.UV, 0, (byte)ElementFormat.Float2, 20 });
            vrtf = new VrtfBlock(ms.ToArray());
        }
        var mesh = new MlodMesh { VertexCount = 42, StreamOffset = 64 };

        var segment = VbsiSegment.FromMesh(mesh, vrtf);

        segment.VertexSize.Should().Be(28);
        segment.VertexCount.Should().Be(42);
        segment.ByteOffset.Should().Be(64u);
        segment.Swizzles.Should().Equal(
            SwizzleCmd.Swizzle32, SwizzleCmd.Swizzle32, SwizzleCmd.Swizzle32,
            SwizzleCmd.Swizzle16x2, SwizzleCmd.Swizzle16x2,
            SwizzleCmd.Swizzle32, SwizzleCmd.Swizzle32);
        segment.Swizzles.Count.Should().Be(segment.VertexSize / 4);
    }

    [Fact]
    public void FromMlod_CreatesOneSegmentPerMesh()
    {
        var mlod = new MlodBlock();
        mlod.Meshes.Add(new MlodMesh { VertexCount = 1 });
        mlod.Meshes.Add(new MlodMesh { VertexCount = 2 });
        var empty = new VrtfBlock();

        var vbsi = VbsiBlock.FromMlod(mlod, _ => empty);

        vbsi.Segments.Should().HaveCount(2);
        vbsi.Segments[1].VertexCount.Should().Be(2);
    }

    [Fact]
    public void Registry_ResolvesByLegacyTypeId()
    {
        RcolBlockRegistry.IsRegistered(0xFFFFFFFF).Should().BeTrue();
        RcolBlockRegistry.IsRegistered(0x01D10F3B).Should().BeFalse("0x01D10F3B was an invented id");

        RcolBlockRegistry.CreateBlock(0xFFFFFFFF, CreateVbsi((4, 1, 0, [1u]))).Should().BeOfType<VbsiBlock>();
    }
}
