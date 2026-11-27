// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs, MLOD.cs, s4piRCOLChunks/MATD.cs, RSLT.cs,
// GenericRCOLResource/GenericRCOLResource.cs (layouts reproduced from real game/mod files; no game data included)

using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Regression tests for RCOL/mesh layout bugs found by the real-data harness. Each builds synthetic bytes in the
/// layout observed in real packages.
/// </summary>
public class RealLayoutRegressionTests
{
    /// <summary>Minimal v0x0C GEOM: no shader, no vertices/faces, and the given TGI list.</summary>
    private static byte[] GeomWithTgis(params (uint Type, uint Group, ulong Instance)[] tgis)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("GEOM"u8);
        w.Write(0x0Cu);
        long tgiOffsetPos = ms.Position;
        w.Write(0u); // tgi offset
        w.Write(0u); // tgi size
        w.Write(0u); // shader
        w.Write(0u); // merge group
        w.Write(0u); // sort order
        w.Write(0);  // vertex count
        w.Write(0);  // vertex format count
        w.Write(1);  // face point sizes
        w.Write((byte)2);
        w.Write(0);  // faces
        w.Write(0);  // unknown things
        w.Write(0);  // unknown things 2
        w.Write(0);  // bone hashes
        long tgiStart = ms.Position;
        w.Write(tgis.Length);
        foreach (var (type, group, instance) in tgis)
        {
            // TGI order (legacy TGIBlock default, TGIBlock.cs line 37)
            w.Write(type);
            w.Write(group);
            w.Write(instance);
        }

        long end = ms.Position;
        ms.Position = tgiOffsetPos;
        w.Write((uint)(tgiStart - (tgiOffsetPos + 4)));
        w.Write((uint)(end - tgiStart)); // size includes the count
        return ms.ToArray();
    }

    /// <summary>
    /// GEOM's TGI list is count + entries in TGI order, and its size covers the count
    /// (GEOM.cs line 174, DependentList.cs lines 673-723). It was read without the count, in ITG order, and
    /// written without the count.
    /// </summary>
    [Fact]
    public void Geom_TgiList_CountAndTgiOrder_RoundTrips()
    {
        var data = GeomWithTgis((0x00B2D882, 0, 0xABB5D82B88615C03), (0x00B2D882, 0, 0xF5A46F56BD4D5205));

        var block = new GeomBlock(data);

        block.TgiBlocks.Should().HaveCount(2);
        block.TgiBlocks[0].ResourceType.Should().Be(0x00B2D882u);
        block.TgiBlocks[0].Instance.Should().Be(0xABB5D82B88615C03);
        block.Serialize().ToArray().Should().Equal(data);
    }

    /// <summary>
    /// MATD (version &lt; 0x103) with an empty MTRL: tag, unknown1, two uint16 unknowns, count 0
    /// (MATD.cs lines 205-228). The MTRL was parsed with the MTNF layout and its unknowns rewritten as a
    /// data length of 4.
    /// </summary>
    [Fact]
    public void Matd_EmptyMtrl_RoundTrips()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("MATD"u8);
        w.Write(0x103u);
        w.Write(0xFDCA6891u);
        w.Write(0x21FE207Du);
        w.Write(16u); // MTRL length
        w.Write(0);   // isVideoSurface
        w.Write(0);   // isPaintingSurface
        w.Write("MTRL"u8);
        w.Write(0u);
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write(0);   // no shader data
        var data = ms.ToArray();

        var block = new MatdBlock(data);

        block.MaterialData.Should().NotBeNull();
        block.MaterialData!.Tag.Should().Be("MTRL");
        block.Serialize().ToArray().Should().Equal(data);
    }

    /// <summary>
    /// For a single-chunk RCOL legacy ignores the stored index entry and takes everything after the index as the
    /// chunk (GenericRCOLResource.cs lines 86-89). Real mod slot-adjust files store a length 4 bytes short; the
    /// chunk must still parse (legacy then writes the real length).
    /// </summary>
    [Fact]
    public void Rcol_SingleChunk_StoredLengthShort_UsesDataToEnd()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(3u);   // version
        w.Write(1);    // public chunks
        w.Write(0u);   // unused
        w.Write(0);    // external resources
        w.Write(1);    // chunks
        w.Write(0x9239ADCB049D13FEUL); // chunk instance
        w.Write(0x0355E0A6u);
        w.Write(0u);
        w.Write(0x2Cu); // position
        w.Write(48);    // stored length: 4 bytes short of the 52-byte chunk
        w.Write(1u);    // BOND version
        w.Write(1);     // one adjustment
        w.Write(0xF46C89F8u);
        for (int i = 0; i < 9; i++) w.Write(0f); // offset, scale, quaternion x/y/z
        w.Write(1f);
        var data = ms.ToArray();

        var resource = new RcolResource(new ResourceKey(0x0355E0A6, 0, 0), data);

        resource.Chunks.Should().HaveCount(1);
        resource.Chunks[0].Block.Should().BeOfType<BondBlock>();
        var written = resource.Data.ToArray();
        written.Length.Should().Be(data.Length);
        BitConverter.ToInt32(written, 0x28).Should().Be(52);
    }
}
