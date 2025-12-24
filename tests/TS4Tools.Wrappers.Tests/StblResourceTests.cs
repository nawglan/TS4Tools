using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class StblResourceTests
{
    private static readonly ResourceKey TestKey = new(0x220557DA, 0, 0);

    [Fact]
    public void CreateEmpty_HasNoEntries()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        stbl.Count.Should().Be(0);
        stbl.Version.Should().Be(StblResource.CurrentVersion);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        stbl.Add(0x12345678, "Hello World");

        stbl.Count.Should().Be(1);
        stbl.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrue()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const uint hash = 0xABCDEF01;
        const string value = "Test String";
        stbl.Add(hash, value);

        bool found = stbl.TryGetValue(hash, out string? result);

        found.Should().BeTrue();
        result.Should().Be(value);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        bool found = stbl.TryGetValue(0x12345678, out string? result);

        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrue()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const uint hash = 0x12345678;
        stbl.Add(hash, "value");

        bool removed = stbl.Remove(hash);

        removed.Should().BeTrue();
        stbl.Count.Should().Be(0);
    }

    [Fact]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        bool removed = stbl.Remove(0x12345678);

        removed.Should().BeFalse();
    }

    [Fact]
    public void Indexer_Set_AddsOrUpdates()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        const uint hash = 0x12345678;

        stbl[hash] = "first";
        stbl[hash] = "second";

        stbl.Count.Should().Be(1);
        stbl[hash].Should().Be("second");
    }

    [Fact]
    public void Indexer_Get_NonExisting_ThrowsKeyNotFoundException()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        var act = () => _ = stbl[0x12345678];

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Add(FnvHash.Fnv32("key1"), "First string");
        original.Add(FnvHash.Fnv32("key2"), "Second string");
        original.Add(FnvHash.Fnv32("key3"), "Third string with Unicode: 日本語");

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new StblResource(TestKey, serialized);

        parsed.Count.Should().Be(original.Count);
        parsed.Version.Should().Be(original.Version);

        foreach (var entry in original.Entries)
        {
            parsed.TryGetValue(entry.KeyHash, out string? value).Should().BeTrue();
            value.Should().Be(entry.Value);
        }
    }

    [Fact]
    public void Parse_ValidHeader_ReadsCorrectly()
    {
        // Manually construct a minimal valid STBL
        var data = new byte[]
        {
            // Magic "STBL"
            0x53, 0x54, 0x42, 0x4C,
            // Version (5)
            0x05, 0x00,
            // IsCompressed (0)
            0x00,
            // Entry count (1)
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // Reserved
            0x00, 0x00,
            // String data length (5)
            0x05, 0x00, 0x00, 0x00,
            // Entry 1:
            // KeyHash
            0x78, 0x56, 0x34, 0x12,
            // Flags
            0x00,
            // String length (5)
            0x05, 0x00,
            // String "Hello"
            0x48, 0x65, 0x6C, 0x6C, 0x6F
        };

        var stbl = new StblResource(TestKey, data);

        stbl.Count.Should().Be(1);
        stbl.Version.Should().Be(5);
        stbl.Entries[0].KeyHash.Should().Be(0x12345678);
        stbl.Entries[0].Value.Should().Be("Hello");
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsException()
    {
        var data = new byte[]
        {
            // Invalid magic
            0xFF, 0xFF, 0xFF, 0xFF,
            // Rest of header...
            0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        var act = () => new StblResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TooShort_ThrowsException()
    {
        var data = new byte[] { 0x53, 0x54, 0x42, 0x4C, 0x05 }; // Only 5 bytes

        var act = () => new StblResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x1, "a");
        stbl.Add(0x2, "b");

        stbl.Clear();

        stbl.Count.Should().Be(0);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x12345678, "value");

        stbl.ContainsKey(0x12345678).Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        var stbl = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        stbl.ContainsKey(0x12345678).Should().BeFalse();
    }

    [Fact]
    public void StringEntry_Record_Equality()
    {
        var entry1 = new StringEntry(0x123, 0, "value");
        var entry2 = new StringEntry(0x123, 0, "value");
        var entry3 = new StringEntry(0x123, 1, "value");

        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
    }
}
