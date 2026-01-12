using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="RoofStyleResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/RoofStyleResource.cs
/// - Type ID: 0x91EDBD3E
/// - Extends ObjectCatalogResource with roof-specific TGI references
/// </summary>
public class RoofStyleResourceTests
{
    private static readonly ResourceKey TestKey = new(RoofStyleResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        RoofStyleResource.TypeId.Should().Be(0x91EDBD3E);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new RoofStyleResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Base class defaults
        resource.Version.Should().Be(ObjectCatalogResource.DefaultVersion);
        resource.CatalogVersion.Should().Be(ObjectCatalogResource.DefaultCatalogVersion);

        // Type-specific defaults
        resource.Unknown1.Should().Be(0);
        resource.CrmtTgiReference.Should().Be(TgiReference.Empty);
        resource.CrtrTgiReference1.Should().Be(TgiReference.Empty);
        resource.CrtrTgiReference2.Should().Be(TgiReference.Empty);
        resource.ToolTgiReference.Should().Be(TgiReference.Empty);
        resource.Unknown2.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_UnknownFields_PreservesData()
    {
        var original = new RoofStyleResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown1 = 0xDEADBEEF;
        original.Unknown2 = 0xCAFEBABE;

        var data = original.Data.ToArray();
        var parsed = new RoofStyleResource(TestKey, data);

        parsed.Unknown1.Should().Be(0xDEADBEEF);
        parsed.Unknown2.Should().Be(0xCAFEBABE);
    }

    [Fact]
    public void RoundTrip_TgiReferences_PreservesData()
    {
        var original = new RoofStyleResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CrmtTgiReference = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.CrtrTgiReference1 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);
        original.CrtrTgiReference2 = new TgiReference(0x3333333333333333, 0x33333333, 0x33333333);
        original.ToolTgiReference = new TgiReference(0x4444444444444444, 0x44444444, 0x44444444);

        var data = original.Data.ToArray();
        var parsed = new RoofStyleResource(TestKey, data);

        parsed.CrmtTgiReference.Instance.Should().Be(0x1111111111111111);
        parsed.CrtrTgiReference1.Instance.Should().Be(0x2222222222222222);
        parsed.CrtrTgiReference2.Instance.Should().Be(0x3333333333333333);
        parsed.ToolTgiReference.Instance.Should().Be(0x4444444444444444);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new RoofStyleResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Base class fields
        original.Version = 5;
        original.CatalogVersion = 9;
        original.CatalogNameHash = 0xDEADBEEF;
        original.CatalogPrice = 1500;
        original.CatalogTagList.Add(0x1234);

        // Type-specific fields
        original.Unknown1 = 0xAAAAAAAA;
        original.CrmtTgiReference = new TgiReference(0xBBBBBBBBBBBBBBBB, 0xBBBBBBBB, 0xBBBBBBBB);
        original.CrtrTgiReference1 = new TgiReference(0xCCCCCCCCCCCCCCCC, 0xCCCCCCCC, 0xCCCCCCCC);
        original.CrtrTgiReference2 = new TgiReference(0xDDDDDDDDDDDDDDDD, 0xDDDDDDDD, 0xDDDDDDDD);
        original.ToolTgiReference = new TgiReference(0xEEEEEEEEEEEEEEEE, 0xEEEEEEEE, 0xEEEEEEEE);
        original.Unknown2 = 0xFFFFFFFF;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new RoofStyleResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new RoofStyleResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<RoofStyleResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new RoofStyleResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as RoofStyleResource;

        resource.Should().NotBeNull();
        resource!.Unknown1.Should().Be(0);
        resource.CrmtTgiReference.Should().Be(TgiReference.Empty);
    }
}
