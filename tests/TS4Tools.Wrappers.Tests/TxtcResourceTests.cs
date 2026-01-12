using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class TxtcResourceTests
{
    private static readonly ResourceKey TestKey = new(0x033A1435, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(8);
        resource.PatternSize.Should().Be(TxtcPatternSize.Default);
        resource.DataType.Should().Be(TxtcDataType.Body);
        resource.Unknown3.Should().Be(0);
        resource.Unknown4.Should().Be(0);
        resource.SuperBlocks.Should().BeEmpty();
        resource.Entries.Should().BeEmpty();
        resource.TgiBlocks.Should().BeEmpty();
    }

    [Fact]
    public void AddEntryBlock_Works()
    {
        var resource = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var entries = new List<TxtcEntry>
        {
            new TxtcEntryBoolean(0x12345678, 0, true),
            new TxtcEntryUInt32(0x87654321, 1, 42)
        };
        var entryBlock = new TxtcEntryBlock(entries);

        resource.AddEntryBlock(entryBlock);

        resource.Entries.Count.Should().Be(1);
        resource.Entries[0].Entries.Count.Should().Be(2);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddTgiBlock_Works()
    {
        var resource = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var tgi = new ResourceKey(0x1234, 0x5678, 0x9ABC);

        resource.AddTgiBlock(tgi);

        resource.TgiBlocks.Count.Should().Be(1);
        resource.TgiBlocks[0].Should().Be(tgi);
    }

    [Fact]
    public void RoundTrip_Version8_PreservesData()
    {
        var original = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 8;
        original.PatternSize = TxtcPatternSize.Large;
        original.DataType = TxtcDataType.Hair | TxtcDataType.Body;
        original.Unknown3 = 42;
        original.Unknown4 = 99;

        var entries = new List<TxtcEntry>
        {
            new TxtcEntryBoolean(0x655FD973, 0, true),
            new TxtcEntryUInt32(0x182E64EB, 1, 256),
            new TxtcEntrySingle(0x3A3260E6, 2, 1.5f)
        };
        original.AddEntryBlock(new TxtcEntryBlock(entries));

        original.AddTgiBlock(new ResourceKey(0x00B2D882, 0x00000000, 0x1234567890ABCDEF));

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new TxtcResource(TestKey, serialized);

        parsed.Version.Should().Be(8);
        parsed.PatternSize.Should().Be(TxtcPatternSize.Large);
        parsed.DataType.Should().Be(TxtcDataType.Hair | TxtcDataType.Body);
        parsed.Unknown3.Should().Be(42);
        parsed.Unknown4.Should().Be(99);
        parsed.Entries.Count.Should().Be(1);
        parsed.Entries[0].Entries.Count.Should().Be(3);
        parsed.TgiBlocks.Count.Should().Be(1);
        parsed.TgiBlocks[0].Should().Be(new ResourceKey(0x00B2D882, 0x00000000, 0x1234567890ABCDEF));
    }

    [Fact]
    public void RoundTrip_AllEntryTypes_PreservesData()
    {
        var original = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 8;

        var entries = new List<TxtcEntry>
        {
            new TxtcEntryBoolean(1, 0, true),
            new TxtcEntrySByte(2, 0, -42),
            new TxtcEntryByte(3, 0, 200),
            new TxtcEntryTgiIndex(4, 0, 5),
            new TxtcEntryInt16(5, 0, -1000),
            new TxtcEntryUInt16(6, 0, 50000),
            new TxtcEntryInt32(7, 0, -100000),
            new TxtcEntryUInt32(8, 0, 4000000000),
            new TxtcEntryInt64(9, 0, -9223372036854775807L),
            new TxtcEntryUInt64(10, 0, 18446744073709551615UL),
            new TxtcEntrySingle(11, 0, 3.14159f),
            new TxtcEntryRectangle(12, 0, 1.0f, 2.0f, 3.0f, 4.0f),
            new TxtcEntryVector(13, 0, 0.1f, 0.2f, 0.3f, 0.4f),
            new TxtcEntryString(14, 0, "Hello World")
        };
        original.AddEntryBlock(new TxtcEntryBlock(entries));

        var serialized = original.Data;
        var parsed = new TxtcResource(TestKey, serialized);

        parsed.Entries.Count.Should().Be(1);
        parsed.Entries[0].Entries.Count.Should().Be(14);

        var parsedEntries = parsed.Entries[0].Entries;

        (parsedEntries[0] as TxtcEntryBoolean)!.Value.Should().BeTrue();
        (parsedEntries[1] as TxtcEntrySByte)!.Value.Should().Be(-42);
        (parsedEntries[2] as TxtcEntryByte)!.Value.Should().Be(200);
        (parsedEntries[3] as TxtcEntryTgiIndex)!.Value.Should().Be(5);
        (parsedEntries[4] as TxtcEntryInt16)!.Value.Should().Be(-1000);
        (parsedEntries[5] as TxtcEntryUInt16)!.Value.Should().Be(50000);
        (parsedEntries[6] as TxtcEntryInt32)!.Value.Should().Be(-100000);
        (parsedEntries[7] as TxtcEntryUInt32)!.Value.Should().Be(4000000000);
        (parsedEntries[8] as TxtcEntryInt64)!.Value.Should().Be(-9223372036854775807L);
        (parsedEntries[9] as TxtcEntryUInt64)!.Value.Should().Be(18446744073709551615UL);
        (parsedEntries[10] as TxtcEntrySingle)!.Value.Should().BeApproximately(3.14159f, 0.0001f);

        var rect = parsedEntries[11] as TxtcEntryRectangle;
        rect!.X.Should().Be(1.0f);
        rect.Y.Should().Be(2.0f);
        rect.Width.Should().Be(3.0f);
        rect.Height.Should().Be(4.0f);

        var vec = parsedEntries[12] as TxtcEntryVector;
        vec!.X.Should().BeApproximately(0.1f, 0.0001f);
        vec.Y.Should().BeApproximately(0.2f, 0.0001f);
        vec.Z.Should().BeApproximately(0.3f, 0.0001f);
        vec.W.Should().BeApproximately(0.4f, 0.0001f);

        (parsedEntries[13] as TxtcEntryString)!.Value.Should().Be("Hello World");
    }

    [Fact]
    public void ClearEntryBlocks_RemovesAll()
    {
        var resource = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddEntryBlock(new TxtcEntryBlock(new List<TxtcEntry> { new TxtcEntryBoolean(1, 0, true) }));
        resource.AddEntryBlock(new TxtcEntryBlock(new List<TxtcEntry> { new TxtcEntryUInt32(2, 0, 42) }));

        resource.ClearEntryBlocks();

        resource.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Parse_TooShortForHeader_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // Only 5 bytes

        var act = () => new TxtcResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void TxtcPatternSize_Values()
    {
        ((uint)TxtcPatternSize.Default).Should().Be(0);
        ((uint)TxtcPatternSize.Large).Should().Be(1);
    }

    [Fact]
    public void TxtcDataType_IsFlagsEnum()
    {
        var combined = TxtcDataType.Hair | TxtcDataType.Body | TxtcDataType.Accessory;

        combined.HasFlag(TxtcDataType.Hair).Should().BeTrue();
        combined.HasFlag(TxtcDataType.Body).Should().BeTrue();
        combined.HasFlag(TxtcDataType.Accessory).Should().BeTrue();
        combined.HasFlag(TxtcDataType.Scalp).Should().BeFalse();
    }

    [Fact]
    public void EntryRecords_EqualityWorks()
    {
        var entry1 = new TxtcEntryBoolean(0x1234, 0, true);
        var entry2 = new TxtcEntryBoolean(0x1234, 0, true);
        var entry3 = new TxtcEntryBoolean(0x1234, 0, false);

        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
    }

    [Fact]
    public void RoundTrip_MultipleEntryBlocks_PreservesData()
    {
        var original = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 8;

        original.AddEntryBlock(new TxtcEntryBlock(new List<TxtcEntry>
        {
            new TxtcEntryBoolean(1, 0, true),
            new TxtcEntryUInt32(2, 0, 100)
        }));

        original.AddEntryBlock(new TxtcEntryBlock(new List<TxtcEntry>
        {
            new TxtcEntrySingle(3, 0, 2.5f),
            new TxtcEntryString(4, 0, "Test")
        }));

        var serialized = original.Data;
        var parsed = new TxtcResource(TestKey, serialized);

        parsed.Entries.Count.Should().Be(2);
        parsed.Entries[0].Entries.Count.Should().Be(2);
        parsed.Entries[1].Entries.Count.Should().Be(2);

        (parsed.Entries[0].Entries[0] as TxtcEntryBoolean)!.Value.Should().BeTrue();
        (parsed.Entries[0].Entries[1] as TxtcEntryUInt32)!.Value.Should().Be(100);
        (parsed.Entries[1].Entries[0] as TxtcEntrySingle)!.Value.Should().BeApproximately(2.5f, 0.0001f);
        (parsed.Entries[1].Entries[1] as TxtcEntryString)!.Value.Should().Be("Test");
    }

    [Fact]
    public void TgiBlocks_IGTOrder_Works()
    {
        // TGI blocks in TXTC use IGT order (Instance, Group, Type)
        var resource = new TxtcResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Version = 8;
        resource.AddTgiBlock(new ResourceKey(0xAABBCCDD, 0x11223344, 0x5566778899AABBCC));

        var serialized = resource.Data;
        var parsed = new TxtcResource(TestKey, serialized);

        parsed.TgiBlocks.Count.Should().Be(1);
        parsed.TgiBlocks[0].ResourceType.Should().Be(0xAABBCCDD);
        parsed.TgiBlocks[0].ResourceGroup.Should().Be(0x11223344);
        parsed.TgiBlocks[0].Instance.Should().Be(0x5566778899AABBCC);
    }
}
