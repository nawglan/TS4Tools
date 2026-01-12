using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="A8f7b517CatalogResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/A8F7B517CatalogResource.cs
/// - Type ID: 0xA8F7B517
/// - Extends ObjectCatalogResource with 3 COBJ TGI references
/// </summary>
public class A8f7b517CatalogResourceTests
{
    private static readonly ResourceKey TestKey = new(A8f7b517CatalogResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        A8f7b517CatalogResource.TypeId.Should().Be(0xA8F7B517);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new A8f7b517CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Base class defaults
        resource.Version.Should().Be(ObjectCatalogResource.DefaultVersion);
        resource.CatalogVersion.Should().Be(ObjectCatalogResource.DefaultCatalogVersion);

        // Type-specific defaults
        resource.CobjTgiReference1.Should().Be(TgiReference.Empty);
        resource.CobjTgiReference2.Should().Be(TgiReference.Empty);
        resource.CobjTgiReference3.Should().Be(TgiReference.Empty);
    }

    [Fact]
    public void RoundTrip_TgiReferences_PreservesData()
    {
        var original = new A8f7b517CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CobjTgiReference1 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.CobjTgiReference2 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);
        original.CobjTgiReference3 = new TgiReference(0x3333333333333333, 0x33333333, 0x33333333);

        var data = original.Data.ToArray();
        var parsed = new A8f7b517CatalogResource(TestKey, data);

        parsed.CobjTgiReference1.Instance.Should().Be(0x1111111111111111);
        parsed.CobjTgiReference1.Type.Should().Be(0x11111111);
        parsed.CobjTgiReference1.Group.Should().Be(0x11111111);

        parsed.CobjTgiReference2.Instance.Should().Be(0x2222222222222222);
        parsed.CobjTgiReference2.Type.Should().Be(0x22222222);
        parsed.CobjTgiReference2.Group.Should().Be(0x22222222);

        parsed.CobjTgiReference3.Instance.Should().Be(0x3333333333333333);
        parsed.CobjTgiReference3.Type.Should().Be(0x33333333);
        parsed.CobjTgiReference3.Group.Should().Be(0x33333333);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new A8f7b517CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Base class fields
        original.Version = 5;
        original.CatalogVersion = 9;
        original.CatalogNameHash = 0xDEADBEEF;
        original.CatalogDescHash = 0xCAFEBABE;
        original.CatalogPrice = 1500;
        original.CatalogUnknown1 = 0xAAAAAAAA;
        original.CatalogTagList.Add(0x1234);
        original.CatalogSellingPointList.Add(new ObjectCatalogSellingPoint(10, 100));

        // Type-specific fields
        original.CobjTgiReference1 = new TgiReference(0xAAAAAAAAAAAAAAAA, 0xAAAAAAAA, 0xAAAAAAAA);
        original.CobjTgiReference2 = new TgiReference(0xBBBBBBBBBBBBBBBB, 0xBBBBBBBB, 0xBBBBBBBB);
        original.CobjTgiReference3 = new TgiReference(0xCCCCCCCCCCCCCCCC, 0xCCCCCCCC, 0xCCCCCCCC);

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new A8f7b517CatalogResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void RoundTrip_BaseClassFields_PreservedThroughExtendedType()
    {
        var original = new A8f7b517CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Set base class fields
        original.CatalogNameHash = 0x12345678;
        original.CatalogDescHash = 0x87654321;
        original.CatalogPrice = 999;
        original.CatalogStyleTgiList.Add(new TgiReference(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF));
        original.CatalogTagList.Add(0xABCD);
        original.CatalogSellingPointList.Add(new ObjectCatalogSellingPoint(50, 250));

        var data = original.Data.ToArray();
        var parsed = new A8f7b517CatalogResource(TestKey, data);

        parsed.CatalogNameHash.Should().Be(0x12345678);
        parsed.CatalogDescHash.Should().Be(0x87654321);
        parsed.CatalogPrice.Should().Be(999);
        parsed.CatalogStyleTgiList.Count.Should().Be(1);
        parsed.CatalogTagList.Should().HaveCount(1);
        parsed.CatalogTagList[0].Should().Be(0xABCD);
        parsed.CatalogSellingPointList.Count.Should().Be(1);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new A8f7b517CatalogResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<A8f7b517CatalogResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new A8f7b517CatalogResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as A8f7b517CatalogResource;

        resource.Should().NotBeNull();
        resource!.CobjTgiReference1.Should().Be(TgiReference.Empty);
        resource.CobjTgiReference2.Should().Be(TgiReference.Empty);
        resource.CobjTgiReference3.Should().Be(TgiReference.Empty);
    }
}
