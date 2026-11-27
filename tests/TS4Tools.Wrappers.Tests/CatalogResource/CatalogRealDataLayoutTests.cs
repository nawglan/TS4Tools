// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CBLKResource.cs lines 97-180
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CWALResource.cs lines 110-307
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFTRResource.cs lines 39, 115-145
// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectDefinitionResource.cs lines 298-353
// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DependentList.cs lines 124-127, 167-170

using FluentAssertions;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Regression tests for catalog layout bugs found by the real-data harness
/// (tests/TS4Tools.Wrappers.Tests/RealData). Each test rebuilds, with synthetic values, the byte layout
/// observed in real game packages; no game data is embedded.
/// </summary>
public class CatalogRealDataLayoutTests
{
    /// <summary>Version (uint32) followed by a default CatalogCommon block (version 0x0B).</summary>
    private static List<byte> Header(uint version)
    {
        var common = new CatalogCommon();
        var buffer = new byte[common.GetSerializedSize()];
        common.WriteTo(buffer);
        var bytes = new List<byte>(BitConverter.GetBytes(version));
        bytes.AddRange(buffer);
        return bytes;
    }

    private static void AddTgi(List<byte> bytes, ulong instance, uint type, uint group)
    {
        bytes.AddRange(BitConverter.GetBytes(instance));
        bytes.AddRange(BitConverter.GetBytes(type));
        bytes.AddRange(BitConverter.GetBytes(group));
    }

    /// <summary>
    /// CBLKEntryList is a DependentList without a ReadCount override, so its count is an Int32; it was read
    /// as one byte, leaving 3 bytes of every real CBLK behind on write.
    /// </summary>
    [Fact]
    public void Cblk_EntryListHasInt32Count_RoundTripsByteIdentical()
    {
        var bytes = Header(0x05);
        bytes.AddRange([0x00, 0x01]); // unk01, unk02
        bytes.AddRange(BitConverter.GetBytes(2)); // Int32 count
        bytes.AddRange([0x01, 0x00, 0x03, 0x02, 0x03, 0x00]);

        var cblk = new CblkResource(new ResourceKey(CblkResource.TypeId, 0, 0), bytes.ToArray());

        cblk.Entries!.Count.Should().Be(2);
        cblk.Entries[1].Should().Be(new CblkEntry(0x02, 0x03, 0x00));
        cblk.Data.ToArray().Should().Equal(bytes);
    }

    /// <summary>
    /// WallMATDEntryList and WallImgGroupList also use the DependentList Int32 count; reading one byte
    /// misaligned every real CWAL and truncated its colors and trailing instance on write.
    /// </summary>
    [Fact]
    public void Cwal_ListsHaveInt32Counts_RoundTripsByteIdentical()
    {
        var bytes = Header(0x08);
        bytes.AddRange(BitConverter.GetBytes(1)); // MATD list count
        bytes.Add(0x02); // MainWallHeight label
        AddTgi(bytes, 0x1122334455667788, 0x01D0E75D, 0);
        bytes.AddRange(BitConverter.GetBytes(1)); // image group count
        bytes.Add(0x01); // CornerWallHeight
        AddTgi(bytes, 0xAAAA, 0x00B2D882, 0);
        AddTgi(bytes, 0xBBBB, 0x00B2D882, 0);
        AddTgi(bytes, 0xCCCC, 0x00B2D882, 0);
        bytes.AddRange(BitConverter.GetBytes(0x5DE7D001u)); // unk01
        bytes.Add(1); // color count (byte)
        bytes.AddRange([0xC2, 0xC2, 0xC2, 0xFF]);
        bytes.AddRange(BitConverter.GetBytes(0x0000000000014706UL)); // unkIID01

        var cwal = new CwalResource(new ResourceKey(0xD5F0F921, 0, 0), bytes.ToArray());

        cwal.MaterialList.Count.Should().Be(1);
        cwal.CornerTextures.Count.Should().Be(1);
        cwal.Colors.Count.Should().Be(1);
        cwal.Data.ToArray().Should().Equal(bytes);
    }

    /// <summary>
    /// Legacy declares and writes CFTR unk01 as a UInt32; every real CFTR stores 4 bytes there, so writing a
    /// single byte dropped 3 bytes from each.
    /// </summary>
    [Fact]
    public void Cftr_Unk01IsUInt32_RoundTripsByteIdentical()
    {
        var bytes = Header(0x07);
        AddTgi(bytes, 0x4A82991DCA359DD9, 0x76BCF80C, 0); // trimRef
        bytes.AddRange(BitConverter.GetBytes(0xF4BD1CE9u)); // materialVariant
        AddTgi(bytes, 0x4A82991DCA359DD9, 0x01661233, 0); // modlRef
        bytes.AddRange(BitConverter.GetBytes(0x19D9279A48FC7836UL)); // swatchGrouping
        bytes.Add(1);
        bytes.AddRange([0x98, 0x93, 0x89, 0xFF]);
        bytes.AddRange(BitConverter.GetBytes(1u)); // unk01

        var cftr = new CftrResource(new ResourceKey(0xE7ADA79D, 0, 0), bytes.ToArray());

        cftr.Unk01.Should().Be(1u);
        cftr.Data.ToArray().Should().Equal(bytes);
    }

    /// <summary>
    /// Each swapped-ITG entry of an OBJD TGI property is 16 bytes (ReadTGIBlock); the size calculation used
    /// 20, so every real OBJD grew by 4 zero bytes per reference.
    /// </summary>
    [Fact]
    public void ObjectDefinition_TgiListEntriesAre16Bytes_RoundTripsByteIdentical()
    {
        var bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes((ushort)2)); // version
        bytes.AddRange(BitConverter.GetBytes(26u)); // table position
        bytes.AddRange(BitConverter.GetBytes(4)); // one entry (count * 4)
        bytes.AddRange(BitConverter.GetBytes(0xEE95BBC5947E2D32UL)); // swapped instance
        bytes.AddRange(BitConverter.GetBytes(0x8EAF13DEu));
        bytes.AddRange(BitConverter.GetBytes(0u));
        bytes.AddRange(BitConverter.GetBytes((ushort)1)); // property count
        bytes.AddRange(BitConverter.GetBytes((uint)ObjectDefinitionPropertyId.Rig));
        bytes.AddRange(BitConverter.GetBytes(6u));

        var objd = new ObjectDefinitionResource(new ResourceKey(0xC0DB5AE7, 0, 0), bytes.ToArray());

        objd.Rig.Should().ContainSingle();
        objd.Data.ToArray().Should().Equal(bytes);
    }

    /// <summary>
    /// Real game OBJDs carry property IDs legacy does not know (e.g. 0x6ED6C8E9). Legacy skips their
    /// data (<c>default: break</c>, ObjectDefinitionResource.cs lines 180-181, 278-279) and so drops it on
    /// save; the port keeps it verbatim (extent = up to the next property's data).
    /// </summary>
    [Fact]
    public void ObjectDefinition_UnknownPropertyIds_KeepTheirData()
    {
        var bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes((ushort)2)); // version
        bytes.AddRange(BitConverter.GetBytes(19u)); // table position
        bytes.AddRange(BitConverter.GetBytes(0x0102030405060708UL)); // TuningId @ 6
        bytes.AddRange(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE }); // unknown property @ 14
        bytes.AddRange(BitConverter.GetBytes((ushort)2)); // property count
        bytes.AddRange(BitConverter.GetBytes((uint)ObjectDefinitionPropertyId.TuningId));
        bytes.AddRange(BitConverter.GetBytes(6u));
        bytes.AddRange(BitConverter.GetBytes(0x6ED6C8E9u));
        bytes.AddRange(BitConverter.GetBytes(14u));

        var objd = new ObjectDefinitionResource(new ResourceKey(0xC0DB5AE7, 0, 0), bytes.ToArray());

        objd.TuningId.Should().Be(0x0102030405060708UL);
        objd.UnknownProperties[(ObjectDefinitionPropertyId)0x6ED6C8E9].Should().Equal(0xAA, 0xBB, 0xCC, 0xDD, 0xEE);
        objd.Data.ToArray().Should().Equal(bytes);
    }

    /// <summary>
    /// Versions newer than the legacy layout (real game files: CFEN 0x0C, CFND 0x07, CSTR 0x0B, CTPT 0x03)
    /// are refused instead of being parsed with the old layout and rewritten without their new fields.
    /// </summary>
    [Fact]
    public void SimpleCatalog_VersionNewerThanLegacy_ThrowsUnsupported()
    {
        static byte[] WithVersion(SimpleCatalogResource r, uint version)
        {
            r.Version = version;
            return r.Data.ToArray();
        }

        var cfen = WithVersion(new CfenResource(new ResourceKey(0x0418FE2A, 0, 0), default), 0x0C);
        var cfnd = WithVersion(new CfndResource(new ResourceKey(0x2FAE983E, 0, 0), default), 0x07);
        var cstr = WithVersion(new CstrResource(new ResourceKey(0x9A20CD1C, 0, 0), default), 0x0B);
        var ctpt = WithVersion(new CtptResource(new ResourceKey(0xEBCBB16C, 0, 0), default), 0x03);

        FluentActions.Invoking(() => new CfenResource(new ResourceKey(0x0418FE2A, 0, 0), cfen)).Should().Throw<UnsupportedResourceVersionException>();
        FluentActions.Invoking(() => new CfndResource(new ResourceKey(0x2FAE983E, 0, 0), cfnd)).Should().Throw<UnsupportedResourceVersionException>();
        FluentActions.Invoking(() => new CstrResource(new ResourceKey(0x9A20CD1C, 0, 0), cstr)).Should().Throw<UnsupportedResourceVersionException>();
        FluentActions.Invoking(() => new CtptResource(new ResourceKey(0xEBCBB16C, 0, 0), ctpt)).Should().Throw<UnsupportedResourceVersionException>();
    }

    /// <summary>New resources use the legacy default version, which the version guard accepts.</summary>
    [Fact]
    public void SimpleCatalog_NewResources_UseLegacyDefaultVersionAndRoundTrip()
    {
        var cfen = new CfenResource(new ResourceKey(0x0418FE2A, 0, 0), default);
        var cfnd = new CfndResource(new ResourceKey(0x2FAE983E, 0, 0), default);
        var cstr = new CstrResource(new ResourceKey(0x9A20CD1C, 0, 0), default);

        cfen.Version.Should().Be(0x0Au);
        cfnd.Version.Should().Be(0x06u);
        cstr.Version.Should().Be(0x0Au);
        new CfndResource(cfnd.Key, cfnd.Data).Data.ToArray().Should().Equal(cfnd.Data.ToArray());
    }

    /// <summary>
    /// COBJ/CCOL version 0x1A and aural properties version 5 (current game data) have no legacy layout
    /// (AbstractCatalogResource.cs line 33, lines 503-532) and are refused.
    /// </summary>
    [Fact]
    public void AbstractCatalog_NewerVersions_ThrowUnsupported()
    {
        var key = new ResourceKey(0x319E4F1D, 0, 0);
        var v1A = new CobjResource(key, default) { Version = 0x1A }.Data.ToArray();
        var aural5 = new CobjResource(key, default) { Version = 0x19, AuralPropertiesVersion = 5 }.Data.ToArray();
        var ok = new CobjResource(key, default) { Version = 0x19, AuralPropertiesVersion = 4 }.Data.ToArray();

        FluentActions.Invoking(() => new CobjResource(key, v1A)).Should().Throw<UnsupportedResourceVersionException>();
        FluentActions.Invoking(() => new CobjResource(key, aural5)).Should().Throw<UnsupportedResourceVersionException>();
        new CobjResource(key, ok).Data.ToArray().Should().Equal(ok);
    }
}
