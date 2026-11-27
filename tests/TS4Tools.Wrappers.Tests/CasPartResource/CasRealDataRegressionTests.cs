// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/GEOMListResource.cs lines 61-118, 181-191
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 82-120
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/StyleLookResource.cs lines 61-109
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResource.cs lines 134-137
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SkinToneResource.cs lines 85-92
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SimOutfitResource.cs lines 66-123

using FluentAssertions;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Regression tests for CAS layout bugs found by the real-data harness (RealData/). Each test builds
/// synthetic bytes with the layout observed in real game files; no game data is embedded.
/// </summary>
public class CasRealDataRegressionTests
{
    /// <summary>
    /// GEOM list (v3, as in game data): the header object count is not the reference block count, and
    /// objectLength is ReferenceBlockList.Size (which already includes the 4-byte count) + 4. Both were
    /// rewritten wrongly before.
    /// </summary>
    [Fact]
    public void GeomList_RealLayout_RoundTripsByteIdentical()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(3u);   // context version
        w.Write(1u);   // public key count
        w.Write(0u);   // external key count
        w.Write(0u);   // delay-load key count
        w.Write(1u);   // object count (1, while there are 2 reference blocks below)
        w.Write(0x2050AFB836E67F18ul); w.Write(GeomListResource.TypeId); w.Write(0u); // ITG
        long posField = ms.Position;
        w.Write(0u); // object position (patched)
        w.Write(0u); // object length (patched)
        w.Write(1u); // object version
        long blockStart = ms.Position;
        w.Write(2); // block count
        for (int b = 0; b < 2; b++)
        {
            w.Write(7u + (uint)b); // region
            w.Write(10000f);        // layer
            w.Write(false);         // isReplacement
            w.Write(1);             // tgi count
            w.Write(0x007D6DD1015A1849ul + (ulong)b); w.Write(0x015A1849u); w.Write(0u);
        }

        long end = ms.Position;
        ms.Position = posField;
        w.Write((uint)(posField + 8));               // objectPosition = position after pos+len fields
        w.Write((uint)(end - blockStart + 4));       // objectLength = Size (count + blocks) + 4
        w.Flush();
        byte[] data = ms.ToArray();

        var resource = new GeomListResource(new ResourceKey(GeomListResource.TypeId, 0, 1), data);

        resource.IsValid.Should().BeTrue();
        resource.ObjectCount.Should().Be(1);
        resource.ReferenceBlocks.Should().HaveCount(2);
        resource.Data.ToArray().Should().Equal(data);
    }

    /// <summary>
    /// Deformer map (v5, as in game data): the int after the robe-channel byte is the byte size of the
    /// scan-line data, not the scan-line count (legacy only tests it for zero, line 83).
    /// </summary>
    [Fact]
    public void DeformerMap_TotalBytesField_IsScanLineByteSize()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(5u);          // version
        w.Write(0xA0u);       // doubled width
        w.Write(0xA0u);       // height
        w.Write(0x00107800u); // age/gender
        w.Write((byte)0);     // physique
        w.Write((byte)1);     // shape
        w.Write(0u); w.Write(1u); // min/max col -> width 2
        w.Write(0u); w.Write(1u); // min/max row -> 2 scan lines
        w.Write((byte)RobeChannel.Dropped); // robe channel
        w.Write(2 * (2 + 1 + 1 + 6)); // total bytes of the scan lines below
        for (int i = 0; i < 2; i++)
        {
            w.Write((ushort)10);   // scan-line data size
            w.Write(false);        // uncompressed
            w.Write((byte)RobeChannel.Dropped); // 3 bytes per pixel
            w.Write(new byte[] { 0x80, 0x81, 0x82, 0x83, 0x84, 0x85 }); // 2 pixels x 3 bytes
        }

        w.Flush();
        byte[] data = ms.ToArray();

        var resource = new DeformerMapResource(new ResourceKey(0xDB43E069, 0, 1), data);

        resource.ScanLines.Should().HaveCount(2);
        resource.Data.ToArray().Should().Equal(data);
    }

    private static byte[] StyleLookV10(bool withTrailingByte)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(0x0Au);              // version
        w.Write(0x00002004u);        // age/gender
        w.Write(0x0000000000004755ul); // grouping id
        w.Write((byte)1);            // unknown1
        w.Write(0x1111111111111111ul); // sim outfit
        w.Write(0x2222222222222222ul); // texture
        w.Write(0x3333333333333333ul); // simdata
        w.Write(0x44444444u);        // name hash
        w.Write(0u);                 // desc hash
        w.Write(new byte[14]);       // unknown2 blob
        w.Write(0x000140E0u);        // unknown3
        w.Write(0xD8D5C1186BA97FDCul);
        w.Write(4); w.Write("Idle"u8.ToArray());
        w.Write(0x92FD3D571BD80A40ul);
        w.Write(4); w.Write("Pose"u8.ToArray());
        w.Write((byte)1); w.Write(0xFF181818u); // one swatch colour
        w.Write(1); w.Write((ushort)0x46); w.Write((ushort)0x4E); // one 16-bit flag
        if (withTrailingByte)
            w.Write((byte)1);
        w.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Style look v0x0A (game data): one byte follows the flag list. Legacy stops reading after the flag
    /// list (StyleLookResource.cs line 81) and drops it; it is now kept.
    /// </summary>
    [Fact]
    public void StyleLook_V10TrailingByte_IsPreserved()
    {
        byte[] data = StyleLookV10(withTrailingByte: true);

        var resource = new StyleLookResource(new ResourceKey(StyleLookResource.TypeId, 0, 1), data);

        resource.IsValid.Should().BeTrue();
        resource.AnimationStateName2.Should().Be("Pose");
        resource.TrailingData.ToArray().Should().Equal((byte)1);
        resource.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void StyleLook_V10WithoutTrailingByte_RoundTrips()
    {
        byte[] data = StyleLookV10(withTrailingByte: false);

        var resource = new StyleLookResource(new ResourceKey(StyleLookResource.TypeId, 0, 1), data);

        resource.TrailingData.IsEmpty.Should().BeTrue();
        resource.Data.ToArray().Should().Equal(data);
    }

    /// <summary>
    /// Versions newer than any legacy layout are refused instead of being misparsed and rewritten
    /// corrupted (versions taken from current game data).
    /// </summary>
    [Theory]
    [InlineData(0x034AEECBu, 0x33u)]
    [InlineData(0x034AEECBu, 0x28u)]
    [InlineData(0x0354796Au, 0x0Cu)]
    [InlineData(0x025ED6F4u, 0x26u)]
    [InlineData(0x71BDB8A2u, 0x0Cu)]
    public void NewerThanLegacyVersion_Throws(uint type, uint version)
    {
        byte[] data = new byte[256];
        BitConverter.GetBytes(version).CopyTo(data, 0);
        var key = new ResourceKey(type, 0, 1);

        Action act = type switch
        {
            0x034AEECBu => () => _ = new Wrappers.CasPartResource.CasPartResource(key, data),
            0x0354796Au => () => _ = new SkinToneResource(key, data),
            0x025ED6F4u => () => _ = new SimOutfitResource(key, data),
            _ => () => _ = new StyleLookResource(key, data),
        };

        act.Should().Throw<UnsupportedResourceVersionException>().Which.Version.Should().Be(version);
    }
}
