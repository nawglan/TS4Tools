using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for TrackMaskBlock (TkMk). Layout follows legacy TrackMask.cs: tag, version; version &lt; 0x201:
/// 48 unused bytes; otherwise rig key (ITG TGI), blend weight float and 28 unused bytes; then an int32
/// count of floats. No trailing data is allowed.
/// </summary>
public class TrackMaskBlockTests
{
    private static byte[] CreateTkMk(uint version, float[] masks, byte[]? trailing = null)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'T'); w.Write((byte)'k'); w.Write((byte)'M'); w.Write((byte)'k');
        w.Write(version);
        if (version < 0x201)
        {
            w.Write(Enumerable.Range(0, 48).Select(i => (byte)i).ToArray());
        }
        else
        {
            w.Write(0x1122334455667788UL); // instance
            w.Write(0x8EAF13DEu);           // type
            w.Write(0x00000042u);           // group
            w.Write(0.75f);                 // vertex anim blend weight
            w.Write(Enumerable.Range(100, 28).Select(i => (byte)i).ToArray());
        }
        w.Write(masks.Length);
        foreach (var m in masks) w.Write(m);
        if (trailing is not null) w.Write(trailing);
        return ms.ToArray();
    }

    [Fact]
    public void Parse_Version200_ReadsLegacyUnusedBlock()
    {
        var block = new TrackMaskBlock(CreateTkMk(0x200, [1f, 0.5f]));

        block.Tag.Should().Be("TkMk");
        block.ResourceType.Should().Be(0x033260E3u);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x200u);
        block.Unused.Should().HaveCount(48);
        block.RigKey.Should().Be(default(RcolTgiBlock));
        block.TrackMasks.Should().Equal(1f, 0.5f);
    }

    [Fact]
    public void Parse_Version201_ReadsRigKeyAndWeight()
    {
        var block = new TrackMaskBlock(CreateTkMk(0x201, [0f, 1f, 0.25f]));

        block.RigKey.Should().Be(new RcolTgiBlock(0x1122334455667788UL, 0x8EAF13DEu, 0x42u));
        block.VertexAnimBlendWeight.Should().Be(0.75f);
        block.Unused.Should().HaveCount(28);
        block.Unused[0].Should().Be(100);
        block.TrackMasks.Should().Equal(0f, 1f, 0.25f);
    }

    [Theory]
    [InlineData(0x200u, 0)]
    [InlineData(0x200u, 5)]
    [InlineData(0x201u, 0)]
    [InlineData(0x201u, 3)]
    [InlineData(0x202u, 2)]
    public void Serialize_RoundTripsByteIdentical(uint version, int count)
    {
        var original = CreateTkMk(version, Enumerable.Range(0, count).Select(i => i / 4f).ToArray());

        new TrackMaskBlock(original).Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Serialize_ResizesUnusedBlockLikeLegacy()
    {
        var block = new TrackMaskBlock(CreateTkMk(0x201, [1f]));
        block.Unused = [1, 2, 3];

        var reparsed = new TrackMaskBlock(block.Serialize().Span);

        reparsed.Unused.Should().HaveCount(28);
        reparsed.Unused.Take(3).Should().Equal(1, 2, 3);
        reparsed.Unused.Skip(3).Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void Parse_TrailingData_Throws()
    {
        var data = CreateTkMk(0x201, [1f], trailing: [1]);

        var action = () => new TrackMaskBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*trailing*");
    }

    [Fact]
    public void Parse_InvalidTag_Throws()
    {
        var data = CreateTkMk(0x200, []);
        data[0] = (byte)'X';

        var action = () => new TrackMaskBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid TkMk tag*");
    }

    [Fact]
    public void Parse_CountLargerThanData_Throws()
    {
        var data = CreateTkMk(0x200, [1f]);
        BitConverter.GetBytes(100000).CopyTo(data, 8 + 48);

        var action = () => new TrackMaskBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Parse_Truncated_Throws()
    {
        foreach (uint version in new[] { 0x200u, 0x201u })
        {
            var data = CreateTkMk(version, [1f, 2f]);
            for (int len = 0; len < data.Length; len++)
            {
                var prefix = data.AsSpan(0, len).ToArray();
                var action = () => new TrackMaskBlock(prefix);
                action.Should().Throw<InvalidDataException>($"version {version:X} prefix of {len} bytes is truncated");
            }
        }
    }

    [Fact]
    public void Registry_ResolvesTkMkByTypeAndTag()
    {
        RcolBlockRegistry.IsRegistered(RcolConstants.TkMk).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("TkMk").Should().BeTrue();

        var data = CreateTkMk(0x201, [1f]);

        RcolBlockRegistry.CreateBlock(RcolConstants.TkMk, data).Should().BeOfType<TrackMaskBlock>();
        RcolBlockRegistry.CreateBlock(0, data).Should().BeOfType<TrackMaskBlock>("tag fallback");
    }

    [Fact]
    public void InsideRcolResource_ChunkParsesAsTrackMask()
    {
        var chunk = CreateTkMk(0x201, [0.5f]);
        var key = new ResourceKey(RcolConstants.TkMk, 0, 1);
        var rcol = new RcolResource(key, ReadOnlyMemory<byte>.Empty);
        rcol.AddChunk(new RcolTgiBlock(1, RcolConstants.TkMk, 0), new TrackMaskBlock(chunk));

        var reparsed = new RcolResource(key, rcol.Data);

        reparsed.Chunks.Should().ContainSingle().Which.Block.Should().BeOfType<TrackMaskBlock>();
        reparsed.Data.ToArray().Should().Equal(rcol.Data.ToArray());
    }
}
