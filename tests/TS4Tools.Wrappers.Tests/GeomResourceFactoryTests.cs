using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for GeomResourceFactory (legacy MeshChunks/GeomResource.cs GeometryResource: an RCOL with
/// exactly one GEOM chunk).
/// </summary>
public class GeomResourceFactoryTests
{
    private static readonly ResourceKey Key = new(0x015A1849, 0, 0x99);

    private static byte[] BuildGeomRcol(uint chunkType = 0x015A1849, int chunkCount = 1)
    {
        var rcol = new RcolResource(Key, ReadOnlyMemory<byte>.Empty);
        for (int i = 0; i < chunkCount; i++)
            rcol.AddChunk(new RcolTgiBlock((ulong)(i + 1), chunkType, 0), new GeomBlock());
        return rcol.Data.ToArray();
    }

    [Fact]
    public void Create_SingleGeomChunk_ReturnsRcolWithGeomBlock()
    {
        var data = BuildGeomRcol();

        var resource = new GeomResourceFactory().Create(Key, data);

        var rcol = resource.Should().BeOfType<RcolResource>().Subject;
        rcol.ChunkCount.Should().Be(1);
        rcol.Chunks[0].Block.Should().BeOfType<GeomBlock>();
        rcol.IsDirty.Should().BeFalse();
        rcol.Data.ToArray().Should().Equal(data, "byte-identical round trip");
    }

    [Fact]
    public void Create_ChunkWithZeroTgiType_IsStillParsedAsGeom()
    {
        // Single chunk, TGI type 0 -> legacy infers the type from the tag; either way the chunk is GEOM
        var data = BuildGeomRcol(chunkType: 0);

        var rcol = (RcolResource)new GeomResourceFactory().Create(Key, data);

        rcol.Chunks[0].Block.Should().BeOfType<GeomBlock>();
        rcol.Chunks[0].TgiBlock.ResourceType.Should().Be(0x015A1849u);
    }

    [Fact]
    public void Create_ChunkWithOtherType_IsReparsedAsGeom()
    {
        // Chunk typed as unknown: generic RCOL yields an UnknownRcolBlock, GEOM wrapper re-reads it
        var data = BuildGeomRcol(chunkType: 0x12345678);

        var rcol = (RcolResource)new GeomResourceFactory().Create(Key, data);

        rcol.Chunks[0].Block.Should().BeOfType<GeomBlock>();
        rcol.IsDirty.Should().BeFalse();
        rcol.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void Create_TwoChunks_Throws()
    {
        var data = BuildGeomRcol(chunkCount: 2);

        var action = () => new GeomResourceFactory().Create(Key, data);

        action.Should().Throw<ResourceFormatException>().WithMessage("*Expected one chunk, found 2*");
    }

    [Fact]
    public void Create_ZeroChunks_Throws()
    {
        var data = new byte[20]; // header only: version 0, no externals, no chunks

        var action = () => new GeomResourceFactory().Create(Key, data);

        action.Should().Throw<ResourceFormatException>().WithMessage("*found 0*");
    }

    [Fact]
    public void Create_EmptyData_BehavesLikeCreateEmpty()
    {
        var resource = new GeomResourceFactory().Create(Key, ReadOnlyMemory<byte>.Empty);

        ((RcolResource)resource).ChunkCount.Should().Be(1);
    }

    [Fact]
    public void Create_NonGeomChunkData_Throws()
    {
        var rcol = new RcolResource(Key, ReadOnlyMemory<byte>.Empty);
        rcol.AddChunk(new RcolTgiBlock(1, 0x12345678, 0), new UnknownRcolBlock(0x12345678, "ABCD", "ABCDxxxx"u8));

        var action = () => new GeomResourceFactory().Create(Key, rcol.Data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid GEOM tag*");
    }

    [Fact]
    public void CreateEmpty_BuildsSingleGeomChunkLikeLegacy()
    {
        var resource = new GeomResourceFactory().CreateEmpty(Key);

        var rcol = resource.Should().BeOfType<RcolResource>().Subject;
        rcol.Version.Should().Be(0);
        rcol.PublicChunksCount.Should().Be(0);
        rcol.ExternalResourceCount.Should().Be(0);
        rcol.ChunkCount.Should().Be(1);
        rcol.Chunks[0].TgiBlock.Should().Be(new RcolTgiBlock(0, 0x015A1849, 0));
        rcol.IsDirty.Should().BeFalse();

        var reparsed = (RcolResource)new GeomResourceFactory().Create(Key, rcol.Data);
        reparsed.Chunks[0].Block.Should().BeOfType<GeomBlock>();
    }

    [Fact]
    public void Registry_ChoosesGeomFactory()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(GeomResourceFactory).Assembly);

        registry.GetFactory(0x015A1849).Should().BeOfType<GeomResourceFactory>();
        registry.GetFactory(RcolConstants.Modl).Should().BeOfType<RcolResourceFactory>();
        (registry.GetFactory(0x02D5DF13) is RcolResourceFactory).Should().BeFalse();
        (registry.GetFactory(0x0344D438) is RcolResourceFactory).Should().BeFalse();
    }
}
