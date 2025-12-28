using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="CasPartResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResource.cs
/// - CAS Part defines Create-A-Sim clothing and accessory parts
/// - Type ID: 0x034AEECB
/// - Complex structure with version-gated fields (v27-v37+)
/// </summary>
public class CasPartResourceTests
{
    private static readonly ResourceKey TestKey = new(Wrappers.CasPartResource.CasPartResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var casp = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);

        casp.Version.Should().Be(Wrappers.CasPartResource.CasPartResource.DefaultVersion);
        casp.PresetCount.Should().Be(0);
        casp.Name.Should().Be(string.Empty);
        casp.FlagList.Should().NotBeNull();
        casp.FlagList.Count.Should().Be(0);
        casp.SwatchColors.Should().NotBeNull();
        casp.SwatchColors.Count.Should().Be(0);
        casp.LodBlocks.Should().NotBeNull();
        casp.LodBlocks.Count.Should().Be(0);
        casp.Overrides.Should().NotBeNull();
        casp.Overrides.Count.Should().Be(0);
        casp.TgiBlocks.Should().NotBeNull();
        casp.TgiBlocks.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Name = "Test Part";
        original.SortPriority = 1.5f;
        original.SecondarySortIndex = 10;
        original.PropertyId = 0x12345678;
        original.AuralMaterialHash = 0xDEADBEEF;
        original.ParmFlags = ParmFlag.ShowInUI | ParmFlag.AllowForRandom;
        original.BodyType = BodyType.Hat;
        original.AgeGender = AgeGenderFlags.Adult | AgeGenderFlags.Female;

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.Name.Should().Be("Test Part");
        parsed.SortPriority.Should().Be(1.5f);
        parsed.SecondarySortIndex.Should().Be(10);
        parsed.PropertyId.Should().Be(0x12345678);
        parsed.AuralMaterialHash.Should().Be(0xDEADBEEF);
        parsed.ParmFlags.Should().Be(ParmFlag.ShowInUI | ParmFlag.AllowForRandom);
        parsed.BodyType.Should().Be(BodyType.Hat);
        parsed.AgeGender.Should().Be(AgeGenderFlags.Adult | AgeGenderFlags.Female);
    }

    [Fact]
    public void RoundTrip_WithSwatchColors_PreservesColors()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.SwatchColors.Add(new SwatchColor(unchecked((int)0xFF0000FF))); // Red
        original.SwatchColors.Add(new SwatchColor(unchecked((int)0xFF00FF00))); // Green
        original.SwatchColors.Add(new SwatchColor(unchecked((int)0xFFFF0000))); // Blue

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.SwatchColors.Count.Should().Be(3);
        parsed.SwatchColors[0].Argb.Should().Be(unchecked((int)0xFF0000FF));
        parsed.SwatchColors[1].Argb.Should().Be(unchecked((int)0xFF00FF00));
        parsed.SwatchColors[2].Argb.Should().Be(unchecked((int)0xFFFF0000));
    }

    [Fact]
    public void RoundTrip_WithFlags_PreservesFlags()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.FlagList.Add(new CaspFlag(0x0041, 0x00000001)); // Color category, value 1
        original.FlagList.Add(new CaspFlag(0x0042, 0x00000002)); // Style category, value 2

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.FlagList.Count.Should().Be(2);
        parsed.FlagList[0].Category.Should().Be(0x0041);
        parsed.FlagList[0].Value.Should().Be(0x00000001);
        parsed.FlagList[1].Category.Should().Be(0x0042);
        parsed.FlagList[1].Value.Should().Be(0x00000002);
    }

    [Fact]
    public void RoundTrip_WithTgiBlocks_PreservesTgis()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TgiBlocks.Add(new CaspTgiBlock(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB));
        original.TgiBlocks.Add(new CaspTgiBlock(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD));

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.TgiBlocks.Count.Should().Be(2);
        parsed.TgiBlocks[0].Instance.Should().Be(0x1111111122222222);
        parsed.TgiBlocks[0].Group.Should().Be(0xAAAAAAAA);
        parsed.TgiBlocks[0].Type.Should().Be(0xBBBBBBBB);
        parsed.TgiBlocks[1].Instance.Should().Be(0x3333333344444444);
        parsed.TgiBlocks[1].Group.Should().Be(0xCCCCCCCC);
        parsed.TgiBlocks[1].Type.Should().Be(0xDDDDDDDD);
    }

    [Fact]
    public void RoundTrip_WithOverrides_PreservesOverrides()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Overrides.Add(new CaspOverride(1, 0.5f));
        original.Overrides.Add(new CaspOverride(2, 0.75f));

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.Overrides.Count.Should().Be(2);
        parsed.Overrides[0].Region.Should().Be(1);
        parsed.Overrides[0].Layer.Should().BeApproximately(0.5f, 0.001f);
        parsed.Overrides[1].Region.Should().Be(2);
        parsed.Overrides[1].Layer.Should().BeApproximately(0.75f, 0.001f);
    }

    [Fact]
    public void RoundTrip_VersionGatedFields_V37_PreservesAll()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 37;

        // Version 27+ fields
        original.SharedUvMapSpace = 0x12345678;

        // Version 28+ fields
        original.VoiceEffectHash = 0xFEDCBA9876543210;

        // Version 30+ fields
        original.UsedMaterialCount = 3;
        original.MaterialSetUpperBodyHash = 0x11111111;
        original.MaterialSetLowerBodyHash = 0x22222222;
        original.MaterialSetShoesHash = 0x33333333;
        original.EmissionMapKey = 5;

        // Version 31+ fields
        original.HideForOccultFlags = OccultTypesDisabled.Alien;

        // Version 32+ fields
        original.Reserved1 = 1;

        // Version 34+ fields
        original.PackId = 42;
        original.PackFlags = PackFlag.HidePackIcon;

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.SharedUvMapSpace.Should().Be(0x12345678);
        parsed.VoiceEffectHash.Should().Be(0xFEDCBA9876543210);
        parsed.UsedMaterialCount.Should().Be(3);
        parsed.MaterialSetUpperBodyHash.Should().Be(0x11111111);
        parsed.MaterialSetLowerBodyHash.Should().Be(0x22222222);
        parsed.MaterialSetShoesHash.Should().Be(0x33333333);
        parsed.EmissionMapKey.Should().Be(5);
        parsed.HideForOccultFlags.Should().Be(OccultTypesDisabled.Alien);
        parsed.Reserved1.Should().Be(1);
        parsed.PackId.Should().Be(42);
        parsed.PackFlags.Should().Be(PackFlag.HidePackIcon);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Name = "Test CAS Part";
        original.SortPriority = 100.5f;
        original.SecondarySortIndex = 5;
        original.PropertyId = 0xAABBCCDD;
        original.AuralMaterialHash = 0x11223344;
        original.ParmFlags = ParmFlag.ShowInUI | ParmFlag.AllowForRandom | ParmFlag.DefaultForBodyType;
        original.ExcludePartFlags = ExcludePartFlag.Hat | ExcludePartFlag.Hair;
        original.ExcludeModifierRegionFlags = 0x1234567890ABCDEF;
        original.DeprecatedPrice = 500;
        original.PartTitleKey = 0xDEADBEEF;
        original.PartDescriptionKey = 0xCAFEBABE;
        original.UniqueTextureSpace = 1;
        original.BodyType = BodyType.Top;
        original.BodySubType = 2;
        original.AgeGender = AgeGenderFlags.Teen | AgeGenderFlags.YoungAdult | AgeGenderFlags.Adult | AgeGenderFlags.Male;
        original.SortLayer = 10;
        original.CompositionMethod = 2;
        original.RegionMapKey = 3;
        original.NormalMapKey = 4;
        original.SpecularMapKey = 5;

        original.SwatchColors.Add(new SwatchColor(unchecked((int)0xFFAABBCC)));
        original.FlagList.Add(new CaspFlag(0x0041, 0x00001234));
        original.Overrides.Add(new CaspOverride(1, 0.5f));
        original.TgiBlocks.Add(new CaspTgiBlock(0x1234567890ABCDEF, 0x12345678, 0x00000001));

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CasPartResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<Wrappers.CasPartResource.CasPartResource>();
        ((Wrappers.CasPartResource.CasPartResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CasPartResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as Wrappers.CasPartResource.CasPartResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(Wrappers.CasPartResource.CasPartResource.DefaultVersion);
        resource.Name.Should().Be(string.Empty);
    }
}
