using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Version-branch tests for <see cref="Wrappers.CasPartResource.CasPartResource"/>.
/// Byte layouts are built independently from the legacy Parse/UnParse so that the
/// resource has an external oracle.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResource.cs
/// Parse lines 115-226, UnParse lines 228-352.
/// </summary>
public class CasPartResourceVersionTests
{
    private static readonly ResourceKey TestKey = new(Wrappers.CasPartResource.CasPartResource.TypeId, 0, 0);

    private static Wrappers.CasPartResource.CasPartResource Load(byte[] data) => new(TestKey, data);

    /// <summary>Builds a CASP byte layout following the legacy UnParse order exactly.</summary>
    private static byte[] Build(
        uint version,
        int flagCount = 2,
        byte unused2 = 0,
        byte usedMaterialCount = 0,
        int overrideCount = 2)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(version);
        w.Write(0u); // tgi offset, patched below
        w.Write(0u); // preset count
        w.Write((byte)8); // 7-bit length: 4 chars * 2 bytes
        w.Write(System.Text.Encoding.BigEndianUnicode.GetBytes("Test"));
        w.Write(1.25f); // sortPriority
        w.Write((ushort)3); // secondarySortIndex
        w.Write(0x11111111u); // propertyID
        w.Write(0x22222222u); // auralMaterialHash
        w.Write((byte)0x05); // parmFlags
        w.Write(0x0102030405060708UL); // excludePartFlags
        if (version >= 36) w.Write(0xAABBCCDD11223344UL);
        else w.Write(0x55667788u);

        w.Write((uint)flagCount);
        for (int i = 0; i < flagCount; i++)
        {
            w.Write((ushort)(0x40 + i));
            if (version >= 37) w.Write(0x00A0B0C0u + (uint)i);
            else w.Write((ushort)(0x1230 + i));
        }

        w.Write(100u); // deprecatedPrice
        w.Write(0x33333333u); // title
        w.Write(0x44444444u); // description
        w.Write((byte)1); // uniqueTextureSpace
        w.Write((int)BodyType.Top);
        w.Write(9); // bodySubType
        w.Write((uint)(AgeGenderFlags.Adult | AgeGenderFlags.Female));
        if (version >= 0x20) w.Write(1u); // reserved1
        if (version >= 34)
        {
            w.Write((short)0x0123); // packID
            w.Write((byte)0x02); // packFlags
            w.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        }
        else
        {
            w.Write(unused2);
            if (unused2 > 0) w.Write((byte)0x77);
        }

        w.Write((byte)2); // swatch colors
        w.Write(unchecked((int)0xFF112233));
        w.Write(unchecked((int)0xFF445566));
        w.Write((byte)0); // buffResKey
        w.Write((byte)1); // variantThumbnailKey
        if (version >= 0x1C) w.Write(0xFEDCBA9876543210UL);
        if (version >= 0x1E)
        {
            w.Write(usedMaterialCount);
            if (usedMaterialCount > 0)
            {
                w.Write(0xA1A1A1A1u);
                w.Write(0xB2B2B2B2u);
                w.Write(0xC3C3C3C3u);
            }
        }

        if (version >= 0x1F) w.Write((uint)OccultTypesDisabled.Alien);
        w.Write((byte)1); // nakedKey
        w.Write((byte)0); // parentKey
        w.Write(-5); // sortLayer

        w.Write((byte)1); // lod blocks
        w.Write((byte)2); // level
        w.Write(0xDEADBEEFu); // unused
        w.Write((byte)1); // assets
        w.Write(10);
        w.Write(20);
        w.Write(30);
        w.Write((byte)2); // lod keys
        w.Write((byte)0);
        w.Write((byte)1);

        w.Write((byte)3); // slot keys
        w.Write(new byte[] { 7, 8, 9 });
        w.Write((byte)1); // diffuseShadowKey
        w.Write((byte)0); // shadowKey
        w.Write((byte)2); // compositionMethod
        w.Write((byte)1); // regionMapKey
        w.Write((byte)overrideCount);
        for (int i = 0; i < overrideCount; i++)
        {
            w.Write((byte)(i + 1));
            w.Write(0.5f * (i + 1));
        }

        w.Write((byte)0); // normalMapKey
        w.Write((byte)1); // specularMapKey
        if (version >= 0x1B) w.Write(0x0BADF00Du); // sharedUVMapSpace
        if (version >= 0x1E) w.Write((byte)1); // emissionMapKey

        long tgiPos = ms.Position;
        ms.Position = 4;
        w.Write((uint)(tgiPos - 8));
        ms.Position = tgiPos;
        w.Write((byte)2); // tgi count
        for (int i = 0; i < 2; i++)
        {
            w.Write(0x1000000000000000UL + (ulong)i); // instance
            w.Write(0x00000001u + (uint)i); // group
            w.Write(0x00B2D882u); // type
        }

        w.Flush();
        return ms.ToArray();
    }

    // 37 is legacy's newest branch (CASPartResource.cs line 137). Later versions are refused
    // (see CasRealDataRegressionTests.NewerThanLegacyVersion_Throws): real game files at 0x28+ use
    // layouts legacy does not know.
    public static TheoryData<uint> Versions =>
        new() { 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 34, 35, 36, 37 };

    [Theory]
    [MemberData(nameof(Versions))]
    public void RoundTrip_EveryVersion_IsByteIdentical(uint version)
    {
        var bytes = Build(version);

        var casp = Load(bytes);

        casp.Version.Should().Be(version);
        casp.Name.Should().Be("Test");
        casp.Data.ToArray().Should().Equal(bytes);
    }

    [Theory]
    [InlineData(0x1Bu)]
    [InlineData(0x20u)]
    [InlineData(37u)]
    public void RoundTrip_NoFlagsNoOverrides_IsByteIdentical(uint version)
    {
        var bytes = Build(version, flagCount: 0, overrideCount: 0);

        Load(bytes).Data.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void V27_NoVoiceHash_NoMaterialSets()
    {
        var casp = Load(Build(0x1B));

        casp.VoiceEffectHash.Should().Be(0);
        casp.UsedMaterialCount.Should().Be(0);
        casp.EmissionMapKey.Should().Be(0);
        casp.HideForOccultFlags.Should().Be(default);
        casp.SharedUvMapSpace.Should().Be(0x0BADF00Du);
    }

    [Fact]
    public void V28_ReadsVoiceEffectHash()
    {
        var casp = Load(Build(0x1C));

        casp.VoiceEffectHash.Should().Be(0xFEDCBA9876543210UL);
        casp.EmissionMapKey.Should().Be(0);
    }

    [Fact]
    public void V30_WithUsedMaterials_ReadsMaterialSetsAndEmissionKey()
    {
        var bytes = Build(0x1E, usedMaterialCount: 3);

        var casp = Load(bytes);

        casp.UsedMaterialCount.Should().Be(3);
        casp.MaterialSetUpperBodyHash.Should().Be(0xA1A1A1A1u);
        casp.MaterialSetLowerBodyHash.Should().Be(0xB2B2B2B2u);
        casp.MaterialSetShoesHash.Should().Be(0xC3C3C3C3u);
        casp.EmissionMapKey.Should().Be(1);
        casp.Data.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void V30_ZeroUsedMaterials_OmitsHashes()
    {
        var bytes = Build(0x1E, usedMaterialCount: 0);

        var casp = Load(bytes);

        casp.UsedMaterialCount.Should().Be(0);
        casp.MaterialSetUpperBodyHash.Should().Be(0);
        casp.Data.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void V31_ReadsHideForOccultFlags()
    {
        var casp = Load(Build(0x1F));

        casp.HideForOccultFlags.Should().Be(OccultTypesDisabled.Alien);
        Load(Build(0x1E)).HideForOccultFlags.Should().Be(default);
    }

    [Fact]
    public void V32_ReadsReserved1()
    {
        Load(Build(0x20)).Reserved1.Should().Be(1u);
        Load(Build(0x1F)).Reserved1.Should().Be(0u);
    }

    [Fact]
    public void BeforeV34_ReadsUnused2AndConditionalUnused3()
    {
        var bytes = Build(0x21, unused2: 4);

        var casp = Load(bytes);

        casp.Unused2.Should().Be(4);
        casp.Unused3.Should().Be(0x77);
        casp.PackId.Should().Be(0);
        casp.Data.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void V34_ReadsPackInfoAndReserved2()
    {
        var casp = Load(Build(34));

        casp.PackId.Should().Be(0x0123);
        casp.PackFlags.Should().Be((PackFlag)2);
        casp.Reserved2.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9);
        casp.Unused2.Should().Be(0);
    }

    [Fact]
    public void ExcludeModifierRegionFlags_Is32BitBeforeV36And64BitFromV36()
    {
        Load(Build(35)).ExcludeModifierRegionFlags.Should().Be(0x55667788UL);
        Load(Build(36)).ExcludeModifierRegionFlags.Should().Be(0xAABBCCDD11223344UL);
    }

    [Fact]
    public void FlagList_Uses16BitBeforeV37And32BitFromV37()
    {
        var old = Load(Build(36));
        old.FlagList.Should().Equal(new CaspFlag(0x40, 0x1230), new CaspFlag(0x41, 0x1231));

        var current = Load(Build(37));
        current.FlagList.Should().Equal(new CaspFlag(0x40, 0x00A0B0C0), new CaspFlag(0x41, 0x00A0B0C1));
    }

    [Fact]
    public void Overrides_ParsedAsList()
    {
        var casp = Load(Build(37));

        casp.Overrides.Should().Equal(new CaspOverride(1, 0.5f), new CaspOverride(2, 1.0f));
    }

    [Fact]
    public void LodBlocksSlotKeysAndTgis_ParsedAtEveryVersion()
    {
        var casp = Load(Build(37));

        casp.LodBlocks.Should().ContainSingle();
        casp.LodBlocks[0].Unused.Should().Be(0xDEADBEEFu);
        casp.SlotKeys.Should().Equal(7, 8, 9);
        casp.TgiBlocks.Should().HaveCount(2);
        casp.TgiBlocks[1].Instance.Should().Be(0x1000000000000001UL);
        casp.SortLayer.Should().Be(-5);
    }

    [Fact]
    public void ModifiedFields_RoundTripAtLatestVersion()
    {
        var casp = Load(Build(37, usedMaterialCount: 1));
        casp.ExcludeModifierRegionFlags = ulong.MaxValue;
        casp.FlagList.Add(new CaspFlag(0xFFFF, 0xFFFFFFFF));
        casp.PackId = -2;
        casp.EmissionMapKey = 3;

        var parsed = Load(casp.Data.ToArray());

        parsed.ExcludeModifierRegionFlags.Should().Be(ulong.MaxValue);
        parsed.FlagList[^1].Should().Be(new CaspFlag(0xFFFF, 0xFFFFFFFF));
        parsed.PackId.Should().Be(-2);
        parsed.EmissionMapKey.Should().Be(3);
    }

    [Fact]
    public void Serialize_InvalidReserved2Length_Throws()
    {
        var casp = Load(Build(34));
        casp.Reserved2 = new byte[3];

        var act = () => casp.Data;

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0x1Bu)]
    [InlineData(0x1Eu)]
    [InlineData(37u)]
    public void Parse_TruncatedAtAnyPoint_Throws(uint version)
    {
        var bytes = Build(version, usedMaterialCount: 1);

        for (int len = 1; len < bytes.Length; len++)
        {
            var truncated = bytes[..len];
            var act = () => Load(truncated);
            act.Should().Throw<Exception>($"data truncated to {len} of {bytes.Length} bytes").Which
                .Should().Match<Exception>(e => e is InvalidDataException || e is ArgumentException || e is IndexOutOfRangeException);
        }
    }

    [Fact]
    public void Parse_NonZeroPresetCount_Throws()
    {
        var bytes = Build(37);
        bytes[8] = 1;

        var act = () => Load(bytes);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_TgiOffsetBeyondData_Throws()
    {
        var bytes = Build(37);
        BitConverter.GetBytes((uint)bytes.Length).CopyTo(bytes, 4);

        var act = () => Load(bytes);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_ExcessiveFlagCount_Throws()
    {
        var bytes = Build(37);
        // flag count follows: version(4) tgi(4) preset(4) name(1+8) sort(4) sec(2) prop(4) aural(4) parm(1) excl(8) mod(8)
        int flagCountOffset = 4 + 4 + 4 + 9 + 4 + 2 + 4 + 4 + 1 + 8 + 8;
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(bytes, flagCountOffset);

        var act = () => Load(bytes);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_OverrideCountExceedsData_Throws()
    {
        var bytes = Build(0x1B, overrideCount: 0);
        // Corrupt the TGI offset so the override count (255) is followed by too little data
        var casp = Load(bytes);
        casp.Overrides.Should().BeEmpty();

        var truncated = Build(0x1B, overrideCount: 200)[..^40];
        var act = () => Load(truncated);

        act.Should().Throw<Exception>();
    }
}
