using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class NameMapResourceTests
{
    private static readonly ResourceKey TestKey = new(0x0166038C, 0, 0);

    [Fact]
    public void CreateEmpty_HasNoEntries()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        nameMap.Count.Should().Be(0);
        nameMap.Version.Should().Be(NameMapResource.CurrentVersion);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        nameMap.Add(0x123456789ABCDEF0, "test_name");

        nameMap.Count.Should().Be(1);
        nameMap.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrue()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const ulong hash = 0xABCDEF0123456789;
        const string name = "object_name";
        nameMap.Add(hash, name);

        bool found = nameMap.TryGetValue(hash, out string? result);

        found.Should().BeTrue();
        result.Should().Be(name);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        bool found = nameMap.TryGetValue(0x123, out string? result);

        found.Should().BeFalse();
    }

    [Fact]
    public void Indexer_Set_AddsOrUpdates()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const ulong hash = 0x123456789ABCDEF0;

        nameMap[hash] = "first";
        nameMap[hash] = "second";

        nameMap.Count.Should().Be(1);
        nameMap[hash].Should().Be("second");
    }

    [Fact]
    public void Indexer_Get_NonExisting_ThrowsKeyNotFoundException()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        var act = () => _ = nameMap[0x123];

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrue()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const ulong hash = 0x123;
        nameMap.Add(hash, "value");

        bool removed = nameMap.Remove(hash);

        removed.Should().BeTrue();
        nameMap.Count.Should().Be(0);
    }

    [Fact]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        bool removed = nameMap.Remove(0x123);

        removed.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Add(0x1234567890ABCDEF, "first_object");
        original.Add(0xFEDCBA0987654321, "second_object");
        original.Add(0xAAAABBBBCCCCDDDD, "unicode_テスト");

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new NameMapResource(TestKey, serialized);

        parsed.Count.Should().Be(original.Count);
        parsed.Version.Should().Be(original.Version);

        foreach (var (hash, name) in original)
        {
            parsed.TryGetValue(hash, out string? value).Should().BeTrue();
            value.Should().Be(name);
        }
    }

    [Fact]
    public void Parse_ValidData_ReadsCorrectly()
    {
        // Manually construct a minimal valid NameMap
        // Version (1), Count (1), Hash (8 bytes), Length (4), Chars
        var data = new byte[]
        {
            // Version (1)
            0x01, 0x00, 0x00, 0x00,
            // Count (1)
            0x01, 0x00, 0x00, 0x00,
            // Hash
            0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12,
            // String length (4)
            0x04, 0x00, 0x00, 0x00,
            // Chars "test" (Unicode, 2 bytes each)
            0x74, 0x00, // 't'
            0x65, 0x00, // 'e'
            0x73, 0x00, // 's'
            0x74, 0x00  // 't'
        };

        var nameMap = new NameMapResource(TestKey, data);

        nameMap.Count.Should().Be(1);
        nameMap.Version.Should().Be(1);
        nameMap.TryGetValue(0x1234567890ABCDEF, out string? name).Should().BeTrue();
        name.Should().Be("test");
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

        var act = () => new NameMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*version*");
    }

    [Fact]
    public void Parse_TooShort_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x00, 0x00 }; // Only 3 bytes

        var act = () => new NameMapResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x1, "a");
        nameMap.Add(0x2, "b");

        nameMap.Clear();

        nameMap.Count.Should().Be(0);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x123, "value");

        nameMap.ContainsKey(0x123).Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        nameMap.ContainsKey(0x123).Should().BeFalse();
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x1, "a");
        nameMap.Add(0x2, "b");
        nameMap.Add(0x3, "c");

        nameMap.Keys.Should().HaveCount(3);
        nameMap.Keys.Should().Contain(0x1UL).And.Contain(0x2UL).And.Contain(0x3UL);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x1, "a");
        nameMap.Add(0x2, "b");
        nameMap.Add(0x3, "c");

        nameMap.Values.Should().HaveCount(3);
        nameMap.Values.Should().Contain("a").And.Contain("b").And.Contain("c");
    }

    [Fact]
    public void Enumeration_Works()
    {
        var nameMap = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x1, "one");
        nameMap.Add(0x2, "two");

        var items = nameMap.ToList();

        items.Should().HaveCount(2);
    }
}
