using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="C48c28979CatalogResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/48C28979CatalogResource.cs
/// - Type ID: 0x48C28979
/// - Extends ObjectCatalogResource with unknown fields and data blobs
/// - DataBlob2 only present in version >= 0x19
/// </summary>
public class C48c28979CatalogResourceTests
{
    private static readonly ResourceKey TestKey = new(C48c28979CatalogResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        C48c28979CatalogResource.TypeId.Should().Be(0x48C28979);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Base class defaults
        resource.Version.Should().Be(ObjectCatalogResource.DefaultVersion);

        // Type-specific defaults
        resource.Unknown1.Should().Be(0);
        resource.Unknown2.Should().Be(0);
        resource.Unknown3.Should().Be(0);
        resource.Unknown4.Should().Be(0);
        resource.Unknown5.Should().Be(0);
        resource.Unknown6.Should().Be(0);
        resource.Unknown7.Should().Be(0);
        resource.DataBlob1.Should().HaveCount(29);
        resource.Unknown8.Should().Be(0);
        resource.Unknown9.Should().Be(0);
        resource.Unknown10.Should().Be(0);
        resource.DataBlob2.Should().HaveCount(16);
        resource.Unknown11.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_UnknownFields_PreservesData()
    {
        var original = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown1 = 0x11111111;
        original.Unknown2 = 0x22222222;
        original.Unknown3 = 0x33333333;
        original.Unknown4 = 0x44444444;
        original.Unknown5 = 0x55555555;
        original.Unknown6 = 0x66666666;
        original.Unknown7 = 0x77777777;
        original.Unknown8 = 0x8888888888888888;
        original.Unknown9 = 0x99999999;
        original.Unknown10 = 0xAAAAAAAA;
        original.Unknown11 = 0xBBBBBBBB;

        var data = original.Data.ToArray();
        var parsed = new C48c28979CatalogResource(TestKey, data);

        parsed.Unknown1.Should().Be(0x11111111);
        parsed.Unknown2.Should().Be(0x22222222);
        parsed.Unknown3.Should().Be(0x33333333);
        parsed.Unknown4.Should().Be(0x44444444);
        parsed.Unknown5.Should().Be(0x55555555);
        parsed.Unknown6.Should().Be(0x66666666);
        parsed.Unknown7.Should().Be(0x77777777);
        parsed.Unknown8.Should().Be(0x8888888888888888);
        parsed.Unknown9.Should().Be(0x99999999);
        parsed.Unknown10.Should().Be(0xAAAAAAAA);
        parsed.Unknown11.Should().Be(0xBBBBBBBB);
    }

    [Fact]
    public void RoundTrip_DataBlob1_PreservesData()
    {
        var original = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var blob1 = new byte[29];
        for (int i = 0; i < 29; i++) blob1[i] = (byte)(i + 1);
        original.DataBlob1 = blob1;

        var data = original.Data.ToArray();
        var parsed = new C48c28979CatalogResource(TestKey, data);

        parsed.DataBlob1.Should().BeEquivalentTo(blob1);
    }

    [Fact]
    public void RoundTrip_DataBlob2_WithHighVersion_PreservesData()
    {
        var original = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x19; // Enable DataBlob2

        var blob2 = new byte[16];
        for (int i = 0; i < 16; i++) blob2[i] = (byte)(100 + i);
        original.DataBlob2 = blob2;

        var data = original.Data.ToArray();
        var parsed = new C48c28979CatalogResource(TestKey, data);

        parsed.Version.Should().Be(0x19);
        parsed.HasDataBlob2.Should().BeTrue();
        parsed.DataBlob2.Should().BeEquivalentTo(blob2);
    }

    [Fact]
    public void HasDataBlob2_IsFalseForLowVersion()
    {
        var resource = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Version = 0x18; // Below threshold

        resource.HasDataBlob2.Should().BeFalse();
    }

    [Fact]
    public void HasDataBlob2_IsTrueForHighVersion()
    {
        var resource = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Version = 0x19; // At threshold

        resource.HasDataBlob2.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_FullResource_LowVersion_ByteForByte()
    {
        var original = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x18; // No DataBlob2

        // Base class fields
        original.CatalogNameHash = 0xDEADBEEF;
        original.CatalogPrice = 1500;

        // Type-specific fields
        original.Unknown1 = 0xAAAAAAAA;
        original.Unknown7 = 0xBBBBBBBB;
        var blob1 = new byte[29];
        blob1[0] = 0xAB;
        blob1[28] = 0xCD;
        original.DataBlob1 = blob1;
        original.Unknown8 = 0xCCCCCCCCCCCCCCCC;
        original.Unknown11 = 0xDDDDDDDD;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new C48c28979CatalogResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void RoundTrip_FullResource_HighVersion_ByteForByte()
    {
        var original = new C48c28979CatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x19; // With DataBlob2

        // Base class fields
        original.CatalogNameHash = 0xDEADBEEF;
        original.CatalogPrice = 1500;

        // Type-specific fields
        original.Unknown1 = 0xAAAAAAAA;
        var blob1 = new byte[29];
        blob1[0] = 0xAB;
        original.DataBlob1 = blob1;
        var blob2 = new byte[16];
        blob2[0] = 0xEF;
        original.DataBlob2 = blob2;
        original.Unknown11 = 0xDDDDDDDD;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new C48c28979CatalogResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new C48c28979CatalogResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<C48c28979CatalogResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new C48c28979CatalogResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as C48c28979CatalogResource;

        resource.Should().NotBeNull();
        resource!.Unknown1.Should().Be(0);
        resource.DataBlob1.Should().HaveCount(29);
    }
}
