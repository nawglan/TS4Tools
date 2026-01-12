using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CtptResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CTPTResource.cs
/// - CTPT is a catalog type for terrain paint materials
/// - Type ID: 0xEBCBB16C
/// - Structure: Version (0x02) + CatalogCommon + HashIndicator + Hash01 + Hash02 + Hash03 + MaterialList
/// - Note: Uses uint32 count for MaterialList, not byte count
/// </summary>
public class CtptResourceTests
{
    private static readonly ResourceKey TestKey = new(CtptResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var ctpt = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // CTPT uses version 0x02, not the standard 0x07
        ctpt.Version.Should().Be(CtptResource.DefaultCtptVersion);
        ctpt.CommonBlock.Should().NotBeNull();
        ctpt.CommonBlock.Price.Should().Be(0);
        ctpt.HashIndicator.Should().Be(0x1);
        ctpt.Hash01.Should().Be(CtptResource.FnvSeed);
        ctpt.Hash02.Should().Be(CtptResource.FnvSeed);
        ctpt.Hash03.Should().Be(CtptResource.FnvSeed);
        ctpt.MaterialList.Count.Should().Be(0);
    }

    [Fact]
    public void Version_DefaultsTo0x02()
    {
        var ctpt = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // CTPT specifically uses version 0x02, not the SimpleCatalogResource default of 0x07
        ctpt.Version.Should().Be(0x02);
        ctpt.Version.Should().NotBe(SimpleCatalogResource.DefaultVersion);
    }

    [Fact]
    public void Hashes_DefaultToFnvSeed()
    {
        var ctpt = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // All three hashes should default to FNV-1a seed
        ctpt.Hash01.Should().Be(0x811C9DC5);
        ctpt.Hash02.Should().Be(0x811C9DC5);
        ctpt.Hash03.Should().Be(0x811C9DC5);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 50;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.HashIndicator = 0x2;
        original.Hash01 = 0x12345678;
        original.Hash02 = 0x87654321;
        original.Hash03 = 0xABCDEF01;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CtptResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(50);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.HashIndicator.Should().Be(0x2);
        parsed.Hash01.Should().Be(0x12345678);
        parsed.Hash02.Should().Be(0x87654321);
        parsed.Hash03.Should().Be(0xABCDEF01);
    }

    [Fact]
    public void RoundTrip_WithMaterialList_PreservesList()
    {
        var original = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.MaterialList.Add(new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB));
        original.MaterialList.Add(new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD));
        original.MaterialList.Add(new TgiReference(0x5555555566666666, 0xEEEEEEEE, 0xFFFFFFFF));

        var data = original.Data.ToArray();
        var parsed = new CtptResource(TestKey, data);

        parsed.MaterialList.Count.Should().Be(3);

        parsed.MaterialList[0].Instance.Should().Be(0x1111111122222222);
        parsed.MaterialList[0].Type.Should().Be(0xAAAAAAAA);
        parsed.MaterialList[0].Group.Should().Be(0xBBBBBBBB);

        parsed.MaterialList[1].Instance.Should().Be(0x3333333344444444);
        parsed.MaterialList[1].Type.Should().Be(0xCCCCCCCC);
        parsed.MaterialList[1].Group.Should().Be(0xDDDDDDDD);

        parsed.MaterialList[2].Instance.Should().Be(0x5555555566666666);
        parsed.MaterialList[2].Type.Should().Be(0xEEEEEEEE);
        parsed.MaterialList[2].Group.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void RoundTrip_WithMultipleMaterials_PreservesOrder()
    {
        var original = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Add 5 materials to test order preservation
        for (ulong i = 1; i <= 5; i++)
        {
            original.MaterialList.Add(new TgiReference(i * 0x1000000000000000, (uint)(i * 0x10000000), (uint)i));
        }

        var data = original.Data.ToArray();
        var parsed = new CtptResource(TestKey, data);

        parsed.MaterialList.Count.Should().Be(5);

        for (int i = 0; i < 5; i++)
        {
            ulong expectedInstance = (ulong)(i + 1) * 0x1000000000000000;
            uint expectedType = (uint)((i + 1) * 0x10000000);
            uint expectedGroup = (uint)(i + 1);

            parsed.MaterialList[i].Instance.Should().Be(expectedInstance);
            parsed.MaterialList[i].Type.Should().Be(expectedType);
            parsed.MaterialList[i].Group.Should().Be(expectedGroup);
        }
    }

    [Fact]
    public void RoundTrip_WithTags_PreservesTags()
    {
        var original = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Tags.Add(0x00010001);
        original.CommonBlock.Tags.Add(0x00020002);

        var data = original.Data.ToArray();
        var parsed = new CtptResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(2);
        parsed.CommonBlock.Tags[0].Should().Be(0x00010001);
        parsed.CommonBlock.Tags[1].Should().Be(0x00020002);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CtptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 1234;
        original.CommonBlock.NameHash = 0xAABBCCDD;
        original.CommonBlock.DescriptionHash = 0x11223344;
        original.CommonBlock.ThumbnailHash = 0xDEADBEEFCAFEBABE;
        original.CommonBlock.DevCategoryFlags = 0x12;
        original.CommonBlock.PackId = 5;
        original.CommonBlock.UnlockByHash = 0x55667788;
        original.CommonBlock.UnlockedByHash = 0x99AABBCC;

        // Type-specific fields
        original.HashIndicator = 0x3;
        original.Hash01 = 0x11111111;
        original.Hash02 = 0x22222222;
        original.Hash03 = 0x33333333;

        original.MaterialList.Add(new TgiReference(0x1234567890ABCDEF, 0x12345678, 0x00000001));
        original.MaterialList.Add(new TgiReference(0xFEDCBA0987654321, 0x87654321, 0x00000002));
        original.CommonBlock.Tags.Add(0x00000042);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CtptResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CtptResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CtptResource>();
        ((CtptResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CtptResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CtptResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(CtptResource.DefaultCtptVersion);
        resource.HashIndicator.Should().Be(0x1);
        resource.Hash01.Should().Be(CtptResource.FnvSeed);
    }
}
