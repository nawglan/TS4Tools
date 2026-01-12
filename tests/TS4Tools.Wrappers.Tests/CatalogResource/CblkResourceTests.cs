using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CblkResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CBLKResource.cs
/// - CBLK is a catalog type for building blocks
/// - Type ID: 0x07936CE0
/// - Structure: Version + CatalogCommon + Unk01 (byte) + Unk02 (byte) + optional CBLKEntryList
/// </summary>
public class CblkResourceTests
{
    private static readonly ResourceKey TestKey = new(CblkResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cblk = new CblkResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cblk.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cblk.CommonBlock.Should().NotBeNull();
        cblk.Unk01.Should().Be(0);
        cblk.Unk02.Should().Be(0);
        cblk.Entries.Should().NotBeNull();
        cblk.Entries!.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CblkResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 200;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.Unk01 = 0x11;
        original.Unk02 = 0x22;

        var data = original.Data.ToArray();
        var parsed = new CblkResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(200);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.Unk01.Should().Be(0x11);
        parsed.Unk02.Should().Be(0x22);
    }

    [Fact]
    public void RoundTrip_WithEntries_PreservesEntries()
    {
        var original = new CblkResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Entries = new CblkEntryList();
        original.Entries.Add(0x11, 0x22, 0x33);
        original.Entries.Add(0xAA, 0xBB, 0xCC);

        var data = original.Data.ToArray();
        var parsed = new CblkResource(TestKey, data);

        parsed.Entries.Should().NotBeNull();
        parsed.Entries!.Count.Should().Be(2);
        parsed.Entries[0].Byte1.Should().Be(0x11);
        parsed.Entries[0].Byte2.Should().Be(0x22);
        parsed.Entries[0].Byte3.Should().Be(0x33);
        parsed.Entries[1].Byte1.Should().Be(0xAA);
        parsed.Entries[1].Byte2.Should().Be(0xBB);
        parsed.Entries[1].Byte3.Should().Be(0xCC);
    }

    [Fact]
    public void RoundTrip_WithoutEntries_PreservesNullEntries()
    {
        var original = new CblkResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Entries = null;

        var data = original.Data.ToArray();
        var parsed = new CblkResource(TestKey, data);

        // When re-parsed, there's no entry data so it remains null
        parsed.Entries.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CblkResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 500;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 5;

        // Type-specific fields
        original.Unk01 = 0xAB;
        original.Unk02 = 0xCD;
        original.Entries = new CblkEntryList();
        original.Entries.Add(0x01, 0x02, 0x03);

        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CblkResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CblkResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CblkResource>();
        ((CblkResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CblkResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CblkResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.Entries.Should().NotBeNull();
    }
}
