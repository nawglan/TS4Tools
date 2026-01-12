using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="CasPartResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResourceTS4.cs
/// - CAS Part defines Create-A-Sim clothing and accessory parts
/// - Type ID: 0x034AEECB
/// - Legacy supports versions 27 (0x1B) and 28 (0x1C)
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
        casp.Overrides.Should().Be(0);
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
        original.FlagList.Add(new CaspFlag(0x0041, 0x0001)); // Color category, value 1
        original.FlagList.Add(new CaspFlag(0x0042, 0x0002)); // Style category, value 2

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.FlagList.Count.Should().Be(2);
        parsed.FlagList[0].Category.Should().Be(0x0041);
        parsed.FlagList[0].Value.Should().Be(0x0001);
        parsed.FlagList[1].Category.Should().Be(0x0042);
        parsed.FlagList[1].Value.Should().Be(0x0002);
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
    public void RoundTrip_WithOverrides_PreservesValue()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Overrides = 5;

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.Overrides.Should().Be(5);
    }

    [Fact]
    public void RoundTrip_VersionGatedFields_V28_PreservesAll()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x1C; // 28

        // Version 27+ fields
        original.SharedUvMapSpace = 0x12345678;

        // Version 28+ fields
        original.VoiceEffectHash = 0xFEDCBA9876543210;

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.SharedUvMapSpace.Should().Be(0x12345678);
        parsed.VoiceEffectHash.Should().Be(0xFEDCBA9876543210);
    }

    [Fact]
    public void RoundTrip_Version27_NoVoiceEffectHash()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x1B; // 27

        // Version 27+ fields only
        original.SharedUvMapSpace = 0x12345678;

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.SharedUvMapSpace.Should().Be(0x12345678);
        parsed.VoiceEffectHash.Should().Be(0); // Not present in v27
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
        original.ExcludeModifierRegionFlags = 0x12345678; // 32-bit in legacy
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
        original.Overrides = 1;

        original.SwatchColors.Add(new SwatchColor(unchecked((int)0xFFAABBCC)));
        original.FlagList.Add(new CaspFlag(0x0041, 0x1234));
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

    [Fact]
    public void ExcludeModifierRegionFlags_Is32Bit()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ExcludeModifierRegionFlags = 0xFFFFFFFF; // Max 32-bit value

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.ExcludeModifierRegionFlags.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void FlagValue_Is16Bit()
    {
        var original = new Wrappers.CasPartResource.CasPartResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.FlagList.Add(new CaspFlag(0xFFFF, 0xFFFF)); // Max 16-bit values

        var data = original.Data.ToArray();
        var parsed = new Wrappers.CasPartResource.CasPartResource(TestKey, data);

        parsed.FlagList[0].Category.Should().Be(0xFFFF);
        parsed.FlagList[0].Value.Should().Be(0xFFFF);
    }
}
