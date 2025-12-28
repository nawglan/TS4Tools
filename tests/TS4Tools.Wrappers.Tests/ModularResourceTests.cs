using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class ModularResourceTests
{
    private static readonly ResourceKey TestKey = new(0xCF9A4ACE, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Unknown1.Should().Be(0);
        resource.Unknown2.Should().Be(0);
        resource.TgiIndexes.Should().BeEmpty();
        resource.TgiBlocks.Should().BeEmpty();
    }

    [Fact]
    public void AddIndex_IncreasesCount()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.AddIndex(0);
        resource.AddIndex(1);
        resource.AddIndex(2);

        resource.TgiIndexes.Count.Should().Be(3);
        resource.IsDirty.Should().BeTrue();
        resource.TgiIndexes[0].Should().Be(0);
        resource.TgiIndexes[1].Should().Be(1);
        resource.TgiIndexes[2].Should().Be(2);
    }

    [Fact]
    public void AddTgiBlock_IncreasesCount()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var tgi1 = new ResourceKey(0x00000001, 0x00000002, 0x0000000000000003);
        var tgi2 = new ResourceKey(0x00000004, 0x00000005, 0x0000000000000006);

        resource.AddTgiBlock(tgi1);
        resource.AddTgiBlock(tgi2);

        resource.TgiBlocks.Count.Should().Be(2);
        resource.IsDirty.Should().BeTrue();
        resource.TgiBlocks[0].Should().Be(tgi1);
        resource.TgiBlocks[1].Should().Be(tgi2);
    }

    [Fact]
    public void ClearIndexes_RemovesAllIndexes()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddIndex(0);
        resource.AddIndex(1);

        resource.ClearIndexes();

        resource.TgiIndexes.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ClearTgiBlocks_RemovesAllBlocks()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddTgiBlock(new ResourceKey(1, 2, 3));
        resource.AddTgiBlock(new ResourceKey(4, 5, 6));

        resource.ClearTgiBlocks();

        resource.TgiBlocks.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void GetTgiBlockAtIndex_ReturnsCorrectBlock()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var tgi0 = new ResourceKey(0x00000001, 0x00000002, 0x0000000000000003);
        var tgi1 = new ResourceKey(0x00000004, 0x00000005, 0x0000000000000006);

        resource.AddTgiBlock(tgi0);
        resource.AddTgiBlock(tgi1);
        resource.AddIndex(1); // Points to tgi1
        resource.AddIndex(0); // Points to tgi0

        resource.GetTgiBlockAtIndex(0).Should().Be(tgi1);
        resource.GetTgiBlockAtIndex(1).Should().Be(tgi0);
    }

    [Fact]
    public void GetTgiBlockAtIndex_InvalidIndex_ReturnsNull()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddTgiBlock(new ResourceKey(1, 2, 3));
        resource.AddIndex(0);

        resource.GetTgiBlockAtIndex(-1).Should().BeNull();
        resource.GetTgiBlockAtIndex(5).Should().BeNull();
    }

    [Fact]
    public void GetTgiBlockAtIndex_InvalidBlockReference_ReturnsNull()
    {
        var resource = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddTgiBlock(new ResourceKey(1, 2, 3));
        resource.AddIndex(99); // Invalid block index

        resource.GetTgiBlockAtIndex(0).Should().BeNull();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new ModularResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown1 = 0x1234;
        original.Unknown2 = 0x5678;
        original.AddTgiBlock(new ResourceKey(0x00000001, 0x00000002, 0x0000000000000003));
        original.AddTgiBlock(new ResourceKey(0x00000004, 0x00000005, 0x0000000000000006));
        original.AddIndex(0);
        original.AddIndex(1);
        original.AddIndex(0);

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ModularResource(TestKey, serialized);

        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Unknown2.Should().Be(original.Unknown2);
        parsed.TgiIndexes.Count.Should().Be(original.TgiIndexes.Count);
        parsed.TgiBlocks.Count.Should().Be(original.TgiBlocks.Count);

        for (int i = 0; i < original.TgiIndexes.Count; i++)
        {
            parsed.TgiIndexes[i].Should().Be(original.TgiIndexes[i]);
        }

        for (int i = 0; i < original.TgiBlocks.Count; i++)
        {
            parsed.TgiBlocks[i].Should().Be(original.TgiBlocks[i]);
        }
    }

    [Fact]
    public void Parse_ValidData_ReadsCorrectly()
    {
        // Manually construct valid Modular resource data:
        // unknown1 (2), tgiOffset (4), tgiSize (4), unknown2 (2), count (2), indexes[], TGI blocks
        var data = new byte[]
        {
            // unknown1 = 0x0102
            0x02, 0x01,
            // tgiOffset = 10 (offset from after this field to TGI blocks)
            // After offset field: tgiSize(4) + unknown2(2) + count(2) + indexes(2) = 10
            0x0A, 0x00, 0x00, 0x00,
            // tgiSize = 16 (1 TGI block)
            0x10, 0x00, 0x00, 0x00,
            // unknown2 = 0x0304
            0x04, 0x03,
            // count = 1
            0x01, 0x00,
            // index[0] = 0
            0x00, 0x00,
            // TGI block: Type=0x12345678, Group=0x9ABCDEF0, Instance=0x1122334455667788
            0x78, 0x56, 0x34, 0x12,
            0xF0, 0xDE, 0xBC, 0x9A,
            0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11
        };

        var resource = new ModularResource(TestKey, data);

        resource.Unknown1.Should().Be(0x0102);
        resource.Unknown2.Should().Be(0x0304);
        resource.TgiIndexes.Count.Should().Be(1);
        resource.TgiIndexes[0].Should().Be(0);
        resource.TgiBlocks.Count.Should().Be(1);
        resource.TgiBlocks[0].ResourceType.Should().Be(0x12345678u);
        resource.TgiBlocks[0].ResourceGroup.Should().Be(0x9ABCDEF0u);
        resource.TgiBlocks[0].Instance.Should().Be(0x1122334455667788ul);
    }

    [Fact]
    public void Parse_TooShortForHeader_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // Only 5 bytes

        var act = () => new ModularResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_NoTgiBlocks_Works()
    {
        // Create a minimal valid resource with no TGI blocks
        var data = new byte[]
        {
            // unknown1 = 0
            0x00, 0x00,
            // tgiOffset = 6 (offset from after this field to where TGI blocks would be)
            0x06, 0x00, 0x00, 0x00,
            // tgiSize = 0 (no TGI blocks)
            0x00, 0x00, 0x00, 0x00,
            // unknown2 = 0
            0x00, 0x00,
            // count = 0
            0x00, 0x00
        };

        var resource = new ModularResource(TestKey, data);

        resource.Unknown1.Should().Be(0);
        resource.Unknown2.Should().Be(0);
        resource.TgiIndexes.Should().BeEmpty();
        resource.TgiBlocks.Should().BeEmpty();
    }
}
