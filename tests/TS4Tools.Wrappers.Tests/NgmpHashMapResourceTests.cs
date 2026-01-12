using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class NgmpHashMapResourceTests
{
    private static readonly ResourceKey TestKey = new(0xF3A38370, 0, 0);

    [Fact]
    public void CreateEmpty_HasNoEntries()
    {
        var resource = new NgmpHashMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Count.Should().Be(0);
        resource.Version.Should().Be(NgmpHashMapResource.CurrentVersion);
        resource.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var resource = new NgmpHashMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Add(0x123456789ABCDEF0, 0xFEDCBA0987654321);

        resource.Count.Should().Be(1);
        resource.IsDirty.Should().BeTrue();
        resource.Entries[0].NameHash.Should().Be(0x123456789ABCDEF0);
        resource.Entries[0].Instance.Should().Be(0xFEDCBA0987654321);
    }

    [Fact]
    public void Add_MultiplePairs_PreservesOrder()
    {
        var resource = new NgmpHashMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Add(0x1111111111111111, 0xAAAAAAAAAAAAAAAA);
        resource.Add(0x2222222222222222, 0xBBBBBBBBBBBBBBBB);
        resource.Add(0x3333333333333333, 0xCCCCCCCCCCCCCCCC);

        resource.Count.Should().Be(3);
        resource.Entries[0].NameHash.Should().Be(0x1111111111111111);
        resource.Entries[1].NameHash.Should().Be(0x2222222222222222);
        resource.Entries[2].NameHash.Should().Be(0x3333333333333333);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var resource = new NgmpHashMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x1, 0x2);
        resource.Add(0x3, 0x4);

        resource.Clear();

        resource.Count.Should().Be(0);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new NgmpHashMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Add(0x1234567890ABCDEF, 0xFEDCBA0987654321);
        original.Add(0xAAAABBBBCCCCDDDD, 0x1111222233334444);
        original.Add(0x0000000000000001, 0xFFFFFFFFFFFFFFFF);

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new NgmpHashMapResource(TestKey, serialized);

        parsed.Count.Should().Be(original.Count);
        parsed.Version.Should().Be(original.Version);

        for (int i = 0; i < original.Count; i++)
        {
            parsed.Entries[i].NameHash.Should().Be(original.Entries[i].NameHash);
            parsed.Entries[i].Instance.Should().Be(original.Entries[i].Instance);
        }
    }

    [Fact]
    public void Parse_ValidData_ReadsCorrectly()
    {
        // Manually construct valid NGMP data:
        // Version (1), Count (2), then 2 pairs
        var data = new byte[]
        {
            // Version (1)
            0x01, 0x00, 0x00, 0x00,
            // Count (2)
            0x02, 0x00, 0x00, 0x00,
            // Pair 1: NameHash (0x1234567890ABCDEF), Instance (0xFEDCBA0987654321)
            0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12,
            0x21, 0x43, 0x65, 0x87, 0x09, 0xBA, 0xDC, 0xFE,
            // Pair 2: NameHash (0x0000000000000001), Instance (0x0000000000000002)
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        var resource = new NgmpHashMapResource(TestKey, data);

        resource.Version.Should().Be(1);
        resource.Count.Should().Be(2);
        resource.Entries[0].NameHash.Should().Be(0x1234567890ABCDEF);
        resource.Entries[0].Instance.Should().Be(0xFEDCBA0987654321);
        resource.Entries[1].NameHash.Should().Be(0x0000000000000001);
        resource.Entries[1].Instance.Should().Be(0x0000000000000002);
    }

    [Fact]
    public void Parse_InvalidVersion_ThrowsException()
    {
        var data = new byte[]
        {
            // Invalid version (99)
            0x63, 0x00, 0x00, 0x00,
            // Count (0)
            0x00, 0x00, 0x00, 0x00
        };

        var act = () => new NgmpHashMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*version*");
    }

    [Fact]
    public void Parse_TooShortForHeader_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x00, 0x00 }; // Only 3 bytes

        var act = () => new NgmpHashMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_TooShortForEntries_ThrowsException()
    {
        var data = new byte[]
        {
            // Version (1)
            0x01, 0x00, 0x00, 0x00,
            // Count (2) - but no data for entries
            0x02, 0x00, 0x00, 0x00
        };

        var act = () => new NgmpHashMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_NegativeCount_ThrowsException()
    {
        var data = new byte[]
        {
            // Version (1)
            0x01, 0x00, 0x00, 0x00,
            // Count (-1)
            0xFF, 0xFF, 0xFF, 0xFF
        };

        var act = () => new NgmpHashMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*count*");
    }

    [Fact]
    public void NgmpPair_Equality_Works()
    {
        var pair1 = new NgmpPair(0x123, 0x456);
        var pair2 = new NgmpPair(0x123, 0x456);
        var pair3 = new NgmpPair(0x123, 0x789);

        pair1.Should().Be(pair2);
        pair1.Should().NotBe(pair3);
    }
}
