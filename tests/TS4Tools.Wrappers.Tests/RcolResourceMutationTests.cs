using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for the editable chunk / external resource lists of RcolResource
/// (legacy ChunkEntryList and Resources, GenericRCOLResource.cs lines 664-704) and for factory registration.
/// </summary>
public class RcolResourceMutationTests
{
    private static readonly ResourceKey Key = new(RcolConstants.Modl, 0, 0x1234);

    private static byte[] Modl(int lods = 0)
    {
        var block = new ModlBlock();
        for (int i = 0; i < lods; i++)
            block.Entries.Add(new ModlLodEntry(new RcolChunkReference((uint)(i + 1)), LodInfo.None, LodId.HighDetail, 0, 1));
        return block.Serialize().ToArray();
    }

    private static RcolResource NewResource() => new(Key, ReadOnlyMemory<byte>.Empty);

    [Fact]
    public void AddChunk_MarksDirtyRaisesChangedAndSerializesOffsets()
    {
        var resource = NewResource();
        int changed = 0;
        resource.Changed += (_, _) => changed++;
        resource.IsDirty.Should().BeFalse();

        int a = resource.AddChunk(new RcolTgiBlock(1, RcolConstants.Modl, 0), new ModlBlock(Modl(0)));
        int b = resource.AddChunk(new RcolTgiBlock(2, RcolConstants.Modl, 0), new ModlBlock(Modl(2)));

        a.Should().Be(0);
        b.Should().Be(1);
        resource.IsDirty.Should().BeTrue();
        changed.Should().Be(2);
        resource.ChunkCount.Should().Be(2);

        // Header(20) + 2 TGI(32) + index(16) = 68, which is already 4-byte aligned
        var data = resource.Data.ToArray();
        var reparsed = new RcolResource(Key, data);
        reparsed.ChunkCount.Should().Be(2);
        reparsed.Chunks[0].Position.Should().Be(68u);
        reparsed.Chunks[0].Length.Should().Be(Modl(0).Length);
        reparsed.Chunks[1].Position.Should().Be((uint)(68 + Modl(0).Length));
        reparsed.Chunks[1].Block.Should().BeOfType<ModlBlock>().Which.Entries.Should().HaveCount(2);
        reparsed.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void Serialize_PadsChunksToFourByteAlignment()
    {
        var resource = NewResource();
        resource.AddChunk(new RcolTgiBlock(1, 0xAAAAAAAA, 0), new UnknownRcolBlock(0xAAAAAAAA, "ABCD", [(byte)'A', (byte)'B', (byte)'C', (byte)'D', 5]));
        resource.AddChunk(new RcolTgiBlock(2, 0xBBBBBBBB, 0), new UnknownRcolBlock(0xBBBBBBBB, "WXYZ", [(byte)'W', (byte)'X', (byte)'Y', (byte)'Z']));

        var reparsed = new RcolResource(Key, resource.Data);

        reparsed.Chunks[0].Length.Should().Be(5);
        reparsed.Chunks[1].Position.Should().Be(reparsed.Chunks[0].Position + 8, "5 byte chunk is padded to 8");
        reparsed.Chunks[1].Position.Should().Be(reparsed.Chunks[1].Position / 4 * 4);
    }

    [Fact]
    public void InsertRemoveReplaceChunk_UpdateList()
    {
        var resource = NewResource();
        resource.AddChunk(new RcolTgiBlock(1, RcolConstants.Modl, 0), new ModlBlock(Modl(0)));
        resource.AddChunk(new RcolTgiBlock(3, RcolConstants.Modl, 0), new ModlBlock(Modl(0)));

        resource.InsertChunk(1, new RcolTgiBlock(2, RcolConstants.Modl, 0), new ModlBlock(Modl(1)));
        resource.Chunks.Select(c => c.TgiBlock.Instance).Should().Equal(1UL, 2UL, 3UL);

        resource.ReplaceChunk(0, new RcolTgiBlock(10, RcolConstants.Modl, 5), new ModlBlock(Modl(3)));
        resource.Chunks[0].TgiBlock.Instance.Should().Be(10);
        ((ModlBlock)resource.Chunks[0].Block).Entries.Should().HaveCount(3);

        resource.RemoveChunkAt(1);
        resource.Chunks.Select(c => c.TgiBlock.Instance).Should().Equal(10UL, 3UL);

        resource.RemoveChunk(resource.Chunks[0]).Should().BeTrue();
        resource.RemoveChunk(resource.Chunks[0]).Should().BeTrue();
        resource.ChunkCount.Should().Be(0);
    }

    [Fact]
    public void RemoveChunk_ForeignEntry_ReturnsFalse()
    {
        var resource = NewResource();
        var foreign = new RcolChunkEntry(default, new ModlBlock());

        resource.RemoveChunk(foreign).Should().BeFalse();
        resource.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void RemovingAllChunks_StillSerializesHeader()
    {
        var original = new RcolResource(Key, BuildRcol(1));
        original.RemoveChunkAt(0);

        var data = original.Data.ToArray();

        data.Length.Should().Be(20);
        var reparsed = new RcolResource(Key, data);
        reparsed.ChunkCount.Should().Be(0);
        reparsed.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChunkEdits_OutOfRange_Throw()
    {
        var resource = NewResource();
        var block = new ModlBlock();

        ((Action)(() => resource.InsertChunk(1, default, block))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.InsertChunk(-1, default, block))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.RemoveChunkAt(0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.ReplaceChunk(0, default, block))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.AddChunk(default, null!))).Should().Throw<ArgumentNullException>();
        resource.IsDirty.Should().BeFalse("failed edits must not dirty the resource");
    }

    [Fact]
    public void ExternalResources_AddInsertReplaceRemove()
    {
        var resource = NewResource();

        resource.AddExternalResource(new RcolTgiBlock(1, 0x00B2D882, 0)).Should().Be(0);
        resource.AddExternalResource(new RcolTgiBlock(3, 0x00B2D882, 0));
        resource.InsertExternalResource(1, new RcolTgiBlock(2, 0x00B2D882, 0));
        resource.ExternalResources.Select(e => e.Instance).Should().Equal(1UL, 2UL, 3UL);

        resource.ReplaceExternalResource(2, new RcolTgiBlock(30, 1, 2));
        resource.ExternalResources[2].Should().Be(new RcolTgiBlock(30, 1, 2));

        resource.RemoveExternalResourceAt(0);
        resource.ExternalResourceCount.Should().Be(2);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ExternalResources_Serialize_PlacesTgisBeforeIndexAndShiftsChunkOffsets()
    {
        var resource = NewResource();
        resource.AddChunk(new RcolTgiBlock(1, RcolConstants.Modl, 0), new ModlBlock(Modl(0)));
        uint before = new RcolResource(Key, resource.Data).Chunks[0].Position;

        resource.AddExternalResource(new RcolTgiBlock(0xABCDEF, 0x00B2D882, 7));
        resource.AddExternalResource(new RcolTgiBlock(0x123456, 0x00B2D882, 8));

        var reparsed = new RcolResource(Key, resource.Data);
        reparsed.ExternalResources.Should().Equal(
            new RcolTgiBlock(0xABCDEF, 0x00B2D882, 7), new RcolTgiBlock(0x123456, 0x00B2D882, 8));
        reparsed.Chunks[0].Position.Should().Be(before + 32);
        reparsed.Chunks[0].Block.Should().BeOfType<ModlBlock>();
    }

    [Fact]
    public void ExternalResources_OutOfRange_Throw()
    {
        var resource = NewResource();

        ((Action)(() => resource.InsertExternalResource(1, default))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.RemoveExternalResourceAt(0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => resource.ReplaceExternalResource(0, default))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void HeaderProperties_AreEditableAndDirty()
    {
        var resource = new RcolResource(Key, BuildRcol(1));

        resource.Version = 5;
        resource.PublicChunksCount = 0;
        resource.Unused = 9;

        resource.IsDirty.Should().BeTrue();
        var reparsed = new RcolResource(Key, resource.Data);
        reparsed.Version.Should().Be(5);
        reparsed.PublicChunksCount.Should().Be(0);
        reparsed.Unused.Should().Be(9);
    }

    [Fact]
    public void SettingHeaderPropertyToSameValue_DoesNotDirty()
    {
        var resource = new RcolResource(Key, BuildRcol(1));

        resource.Version = resource.Version;

        resource.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void MarkChanged_AfterInPlaceBlockEdit_SerializesNewBlockBytes()
    {
        var resource = new RcolResource(Key, BuildRcol(1));
        var modl = (ModlBlock)resource.Chunks[0].Block;
        modl.Entries.Add(new ModlLodEntry(new RcolChunkReference(2), LodInfo.None, LodId.LowDetail, 1, 2));
        resource.MarkChanged();

        var reparsed = new RcolResource(Key, resource.Data);

        resource.IsDirty.Should().BeTrue();
        ((ModlBlock)reparsed.Chunks[0].Block).Entries.Should().ContainSingle();
    }

    [Fact]
    public void Edit_OnUnparsedResource_Throws()
    {
        var resource = new RcolResource(Key, new byte[10]);

        resource.IsValid.Should().BeFalse();
        var action = () => resource.AddExternalResource(default);

        action.Should().Throw<InvalidOperationException>();
        resource.Data.ToArray().Should().Equal(new byte[10], "invalid data is preserved verbatim");
    }

    [Fact]
    public void ParsedRcol_RoundTripsByteIdenticalWithExternals()
    {
        var original = BuildRcol(2, externals: 3);

        new RcolResource(Key, original).Data.ToArray().Should().Equal(original);
    }

    // ------------------------------------------------------------------
    // Factory registration (RCOLResources.txt: only entries marked Y are standalone)
    // ------------------------------------------------------------------

    private static IEnumerable<uint> HandlerTypes(Type factory) =>
        factory.GetCustomAttributes<ResourceHandlerAttribute>().Select(a => a.ResourceType);

    [Fact]
    public void RcolResourceFactory_DoesNotRegisterJazzTypes()
    {
        var types = HandlerTypes(typeof(RcolResourceFactory)).ToList();

        types.Should().NotContain(0x02D5DF13u, "legacy treats it as the Text JAZZ tuning resource");
        types.Should().NotContain(0x0344D438u, "never standalone in legacy");
    }

    [Fact]
    public void RcolResourceFactory_RegistersExactlyTheStandaloneTypes()
    {
        var types = HandlerTypes(typeof(RcolResourceFactory)).ToList();

        types.Should().BeEquivalentTo(RcolConstants.StandaloneTypes);
        types.Should().BeEquivalentTo(new uint[]
        {
            0x01661233, 0x01D0E75D, 0x01D10F34, 0x02019972, 0x021D7E8C, 0x033260E3,
            0x0355E0A6, 0x03B4C61D, 0x63A33EA7, 0x736884F1, 0xD3044521, 0xD382BF57,
        });
    }

    [Fact]
    public void StandaloneTypes_ExcludeEmbeddedOnlyAndJazzTypes()
    {
        RcolConstants.StandaloneTypes.Should().NotContain([
            RcolConstants.Geom, RcolConstants.Vbuf, RcolConstants.Ibuf, RcolConstants.Vrtf, RcolConstants.Skin,
            RcolConstants.VbufShadow, RcolConstants.IbufShadow, RcolConstants.JazzStateMachine,
            RcolConstants.JazzStopAnimationNode]);
    }

    [Fact]
    public void GeomResourceFactory_RegistersGeomType()
    {
        HandlerTypes(typeof(GeomResourceFactory)).Should().Equal(0x015A1849u);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static byte[] BuildRcol(int chunks, int externals = 0)
    {
        var resource = NewResource();
        for (int i = 0; i < chunks; i++)
            resource.AddChunk(new RcolTgiBlock((ulong)(i + 1), RcolConstants.Modl, 0), new ModlBlock(Modl(i)));
        for (int i = 0; i < externals; i++)
            resource.AddExternalResource(new RcolTgiBlock((ulong)(0x100 + i), 0x00B2D882, 1));
        return resource.Data.ToArray();
    }
}
