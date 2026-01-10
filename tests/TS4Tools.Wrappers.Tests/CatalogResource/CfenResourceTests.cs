using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CfenResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFENResource.cs
/// - Type ID: 0x0418FE2A
/// - Fence catalog type with MODL entries, GP7 references, slot, colors, and unknown lists
/// </summary>
public class CfenResourceTests
{
    private static readonly ResourceKey TestKey = new(CfenResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        CfenResource.TypeId.Should().Be(0x0418FE2A);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.CommonBlock.Should().NotBeNull();
        resource.ModlEntryList01.Count.Should().Be(0);
        resource.ModlEntryList02.Count.Should().Be(0);
        resource.ModlEntryList03.Count.Should().Be(0);
        resource.ModlEntryList04.Count.Should().Be(0);
        resource.ReferenceList.Should().NotBeNull();
        resource.Unk01.Should().Be(0);
        resource.Unk02.Should().Be(0);
        resource.MaterialVariant.Should().Be(0);
        resource.SwatchGrouping.Should().Be(0);
        resource.Slot.Should().Be(TgiReference.Empty);
        resource.UnkList01.Count.Should().Be(0);
        resource.UnkList02.Count.Should().Be(0);
        resource.UnkList03.Count.Should().Be(0);
        resource.Colors.Count.Should().Be(0);
        resource.Unk04.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_BasicFields_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unk01 = 0xAB;
        original.Unk02 = 0x12345678;
        original.MaterialVariant = 0xDEADBEEF;
        original.SwatchGrouping = 0xCAFEBABE12345678;
        original.Unk04 = 0x87654321;

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.Unk01.Should().Be(0xAB);
        parsed.Unk02.Should().Be(0x12345678);
        parsed.MaterialVariant.Should().Be(0xDEADBEEF);
        parsed.SwatchGrouping.Should().Be(0xCAFEBABE12345678);
        parsed.Unk04.Should().Be(0x87654321);
    }

    [Fact]
    public void RoundTrip_Slot_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Slot = new TgiReference(0x1111111111111111, 0x22222222, 0x33333333);

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.Slot.Instance.Should().Be(0x1111111111111111);
        parsed.Slot.Type.Should().Be(0x22222222);
        parsed.Slot.Group.Should().Be(0x33333333);
    }

    [Fact]
    public void RoundTrip_ModlEntryLists_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ModlEntryList01.Add(0x0001, new TgiReference(0xAAAAAAAAAAAAAAAA, 0xAAAAAAAA, 0xAAAAAAAA));
        original.ModlEntryList02.Add(0x0002, new TgiReference(0xBBBBBBBBBBBBBBBB, 0xBBBBBBBB, 0xBBBBBBBB));
        original.ModlEntryList03.Add(0x0003, new TgiReference(0xCCCCCCCCCCCCCCCC, 0xCCCCCCCC, 0xCCCCCCCC));
        original.ModlEntryList04.Add(0x0004, new TgiReference(0xDDDDDDDDDDDDDDDD, 0xDDDDDDDD, 0xDDDDDDDD));

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.ModlEntryList01.Count.Should().Be(1);
        parsed.ModlEntryList01[0].Label.Should().Be(0x0001);
        parsed.ModlEntryList02.Count.Should().Be(1);
        parsed.ModlEntryList02[0].Label.Should().Be(0x0002);
        parsed.ModlEntryList03.Count.Should().Be(1);
        parsed.ModlEntryList03[0].Label.Should().Be(0x0003);
        parsed.ModlEntryList04.Count.Should().Be(1);
        parsed.ModlEntryList04[0].Label.Should().Be(0x0004);
    }

    [Fact]
    public void RoundTrip_Gp7References_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ReferenceList.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.ReferenceList.Ref07 = new TgiReference(0x7777777777777777, 0x77777777, 0x77777777);

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.ReferenceList.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.ReferenceList.Ref07.Instance.Should().Be(0x7777777777777777);
    }

    [Fact]
    public void RoundTrip_UnknownLists_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.UnkList01.Add(0x11111111);
        original.UnkList01.Add(0x22222222);
        original.UnkList02.Add(0x33333333);
        original.UnkList03.Add(0x44444444);
        original.UnkList03.Add(0x55555555);
        original.UnkList03.Add(0x66666666);

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.UnkList01.Count.Should().Be(2);
        parsed.UnkList01[0].Should().Be(0x11111111);
        parsed.UnkList01[1].Should().Be(0x22222222);
        parsed.UnkList02.Count.Should().Be(1);
        parsed.UnkList02[0].Should().Be(0x33333333);
        parsed.UnkList03.Count.Should().Be(3);
        parsed.UnkList03[0].Should().Be(0x44444444);
    }

    [Fact]
    public void RoundTrip_Colors_PreservesData()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF0000FF); // Red
        original.Colors.Add(0xFF00FF00); // Green
        original.Colors.Add(0xFFFF0000); // Blue

        var data = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFF0000FF);
        parsed.Colors[1].Should().Be(0xFF00FF00);
        parsed.Colors[2].Should().Be(0xFFFF0000);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CfenResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block
        original.CommonBlock.Price = 999;
        original.CommonBlock.NameHash = 0xDEADBEEF;

        // Type-specific fields
        original.ModlEntryList01.Add(1, new TgiReference(0x1111111111111111, 0x11111111, 0x11111111));
        original.ReferenceList.Ref03 = new TgiReference(0x3333333333333333, 0x33333333, 0x33333333);
        original.Unk01 = 0xAB;
        original.Unk02 = 0xCDEF0123;
        original.MaterialVariant = 0x45678901;
        original.SwatchGrouping = 0xABCDEF0123456789;
        original.Slot = new TgiReference(0xFEDCBA9876543210, 0xFEDCBA98, 0x76543210);
        original.UnkList01.Add(0x12345678);
        original.Colors.Add(0xFFAABBCC);
        original.Unk04 = 0x98765432;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new CfenResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CfenResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CfenResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CfenResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CfenResource;

        resource.Should().NotBeNull();
        resource!.Slot.Should().Be(TgiReference.Empty);
        resource.Colors.Count.Should().Be(0);
    }
}
