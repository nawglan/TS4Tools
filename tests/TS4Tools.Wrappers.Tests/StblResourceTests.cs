using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="StblResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/StblResource/StblResource.cs
/// - STBL format specification:
///   - Magic: "STBL" (0x4C425453 little-endian)
///   - Version: uint16 (current = 5)
///   - IsCompressed: byte
///   - NumEntries: uint64
///   - Reserved: 2 bytes
///   - StringLength: uint32
///   - Entries: KeyHash (uint32), Flags (byte), StringLength (uint16), String (UTF-8)
/// - The legacy uses BinaryReader/BinaryWriter for parsing.
/// - Type ID: 0x220557DA
/// </summary>
public class StblResourceTests
{
    // STBL resource type ID - verified from legacy s4pi wrapper registration
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

    /// <summary>
    /// Regression (real data): the header's string-data length is the string bytes plus one terminator
    /// per entry (legacy StblResource.cs lines 141-152, EntrySize = Length + 1), as stored in the game's
    /// Strings_ENG_US.package. It was written without the per-entry +1.
    /// </summary>
    [Fact]
    public void Data_RealLayout_StringLengthIncludesTerminatorPerEntry_RoundTrips()
    {
        byte[] a = Encoding.UTF8.GetBytes("abc");
        byte[] b = Encoding.UTF8.GetBytes("\u00e9"); // 2 UTF-8 bytes
        var data = new List<byte>();
        data.AddRange("STBL"u8.ToArray());
        data.AddRange(BitConverter.GetBytes((ushort)5));
        data.Add(0);
        data.AddRange(BitConverter.GetBytes(2UL));
        data.AddRange(new byte[2]);
        data.AddRange(BitConverter.GetBytes((uint)(a.Length + 1 + b.Length + 1)));
        foreach (var (hash, str) in new[] { (0x11111111u, a), (0x22222222u, b) })
        {
            data.AddRange(BitConverter.GetBytes(hash));
            data.Add(0);
            data.AddRange(BitConverter.GetBytes((ushort)str.Length));
            data.AddRange(str);
        }

        var stbl = new StblResource(TestKey, data.ToArray());

        stbl.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void Parse_EntryCountLargerThanData_ThrowsBeforeAllocating()
    {
        // 21-byte header claiming 0x7FFFFFC0 entries and no entry data.
        var data = new byte[21];
        "STBL"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), StblResource.CurrentVersion);
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(7), 0x7FFFFFC0);

        var act = () => new StblResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*STBL entry count*");
    }
}
