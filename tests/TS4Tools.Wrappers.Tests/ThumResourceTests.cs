using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="ThumResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs
/// - THUM format specification:
///   - Version: uint32 (usually 4)
///   - Unknown1: uint32
///   - Unknown2: uint64
///   - Unknown3: uint32
///   - SelfReference: TGI in ITG order (16 bytes)
///   - Unknown4: uint32
///   - Unknown5: uint32
///   - Magic: "THUM" (0x4D554854 little-endian)
///   - Float1-8: 8 float32 values
/// - Type ID: 0x16CA6BC4
/// </summary>
public class ThumResourceTests
{
    // THUM resource type ID - from legacy_references/.../THMResource.cs line 196
    private static readonly ResourceKey TestKey = new(0x16CA6BC4, 0, 0);

    private const uint ThumMagic = 0x4D554854; // "THUM"

    [Fact]
    public void CreateEmpty_HasDefaultVersion()
    {
        var thum = new ThumResource(TestKey, ReadOnlyMemory<byte>.Empty);

        thum.Version.Should().Be(4);
        thum.SelfReference.Should().Be(default(ResourceKey));
        thum.Float1.Should().Be(0f);
    }

    [Fact]
    public void Parse_ValidData_ReadsAllFields()
    {
        var data = CreateValidThumData(
            version: 4,
            unknown1: 1,
            unknown2: 2,
            unknown3: 3,
            selfRef: new ResourceKey(0x11111111, 0x22222222, 0x3333333333333333),
            unknown4: 4,
            unknown5: 5,
            floats: [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f]);

        var thum = new ThumResource(TestKey, data);

        thum.Version.Should().Be(4);
        thum.Unknown1.Should().Be(1);
        thum.Unknown2.Should().Be(2);
        thum.Unknown3.Should().Be(3);
        thum.SelfReference.ResourceType.Should().Be(0x11111111);
        thum.SelfReference.ResourceGroup.Should().Be(0x22222222);
        thum.SelfReference.Instance.Should().Be(0x3333333333333333);
        thum.Unknown4.Should().Be(4);
        thum.Unknown5.Should().Be(5);
        thum.Float1.Should().Be(1.0f);
        thum.Float2.Should().Be(2.0f);
        thum.Float3.Should().Be(3.0f);
        thum.Float4.Should().Be(4.0f);
        thum.Float5.Should().Be(5.0f);
        thum.Float6.Should().Be(6.0f);
        thum.Float7.Should().Be(7.0f);
        thum.Float8.Should().Be(8.0f);
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        var data = new byte[80];
        // Don't write THUM magic - leave it as zeros
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0), 4); // version

        var act = () => new ThumResource(TestKey, data);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TooShort_ThrowsInvalidDataException()
    {
        var data = new byte[40]; // Less than minimum 80 bytes

        var act = () => new ThumResource(TestKey, data);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*too small*");
    }

    [Fact]
    public void Serialize_WritesCorrectFormat()
    {
        var thum = new ThumResource(TestKey, ReadOnlyMemory<byte>.Empty)
        {
            Version = 4,
            Unknown1 = 10,
            Unknown2 = 20,
            Unknown3 = 30,
            SelfReference = new ResourceKey(0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCCCCCCCCCC),
            Unknown4 = 40,
            Unknown5 = 50,
            Float1 = 1.5f,
            Float2 = 2.5f,
            Float3 = 3.5f,
            Float4 = 4.5f,
            Float5 = 5.5f,
            Float6 = 6.5f,
            Float7 = 7.5f,
            Float8 = 8.5f
        };

        var serialized = thum.Data;

        serialized.Length.Should().Be(80);

        var span = serialized.Span;

        // Verify header
        BinaryPrimitives.ReadUInt32LittleEndian(span[0..]).Should().Be(4);
        BinaryPrimitives.ReadUInt32LittleEndian(span[4..]).Should().Be(10);
        BinaryPrimitives.ReadUInt64LittleEndian(span[8..]).Should().Be(20);
        BinaryPrimitives.ReadUInt32LittleEndian(span[16..]).Should().Be(30);

        // Verify TGI in ITG order
        BinaryPrimitives.ReadUInt64LittleEndian(span[20..]).Should().Be(0xCCCCCCCCCCCCCCCC); // Instance
        BinaryPrimitives.ReadUInt32LittleEndian(span[28..]).Should().Be(0xAAAAAAAA); // Type
        BinaryPrimitives.ReadUInt32LittleEndian(span[32..]).Should().Be(0xBBBBBBBB); // Group

        BinaryPrimitives.ReadUInt32LittleEndian(span[36..]).Should().Be(40);
        BinaryPrimitives.ReadUInt32LittleEndian(span[40..]).Should().Be(50);

        // Verify magic
        BinaryPrimitives.ReadUInt32LittleEndian(span[44..]).Should().Be(ThumMagic);

        // Verify floats
        BinaryPrimitives.ReadSingleLittleEndian(span[48..]).Should().Be(1.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[52..]).Should().Be(2.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[56..]).Should().Be(3.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[60..]).Should().Be(4.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[64..]).Should().Be(5.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[68..]).Should().Be(6.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[72..]).Should().Be(7.5f);
        BinaryPrimitives.ReadSingleLittleEndian(span[76..]).Should().Be(8.5f);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new ThumResource(TestKey, ReadOnlyMemory<byte>.Empty)
        {
            Version = 4,
            Unknown1 = 100,
            Unknown2 = 200,
            Unknown3 = 300,
            SelfReference = new ResourceKey(0x12345678, 0x87654321, 0xFEDCBA9876543210),
            Unknown4 = 400,
            Unknown5 = 500,
            Float1 = 0.125f,
            Float2 = 0.25f,
            Float3 = 0.375f,
            Float4 = 0.5f,
            Float5 = 0.625f,
            Float6 = 0.75f,
            Float7 = 0.875f,
            Float8 = 1.0f
        };

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ThumResource(TestKey, serialized);

        parsed.Version.Should().Be(original.Version);
        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Unknown2.Should().Be(original.Unknown2);
        parsed.Unknown3.Should().Be(original.Unknown3);
        parsed.SelfReference.Should().Be(original.SelfReference);
        parsed.Unknown4.Should().Be(original.Unknown4);
        parsed.Unknown5.Should().Be(original.Unknown5);
        parsed.Float1.Should().Be(original.Float1);
        parsed.Float2.Should().Be(original.Float2);
        parsed.Float3.Should().Be(original.Float3);
        parsed.Float4.Should().Be(original.Float4);
        parsed.Float5.Should().Be(original.Float5);
        parsed.Float6.Should().Be(original.Float6);
        parsed.Float7.Should().Be(original.Float7);
        parsed.Float8.Should().Be(original.Float8);
    }

    [Fact]
    public void Factory_CreatesResourceCorrectly()
    {
        var factory = new ThumResourceFactory();
        var data = CreateValidThumData();

        var resource = factory.Create(TestKey, data);

        resource.Should().BeOfType<ThumResource>();
        ((ThumResource)resource).Version.Should().Be(4);
    }

    [Fact]
    public void Factory_CreatesEmptyResourceCorrectly()
    {
        var factory = new ThumResourceFactory();

        var resource = factory.CreateEmpty(TestKey);

        resource.Should().BeOfType<ThumResource>();
        ((ThumResource)resource).Version.Should().Be(4);
    }

    /// <summary>
    /// Creates valid THUM data for testing.
    /// </summary>
    private static byte[] CreateValidThumData(
        uint version = 4,
        uint unknown1 = 0,
        ulong unknown2 = 0,
        uint unknown3 = 0,
        ResourceKey? selfRef = null,
        uint unknown4 = 0,
        uint unknown5 = 0,
        float[]? floats = null)
    {
        var data = new byte[80];
        var offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), version);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), unknown1);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), unknown2);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), unknown3);
        offset += 4;

        // ITG order
        var sr = selfRef ?? default;
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), sr.Instance);
        offset += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), sr.ResourceType);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), sr.ResourceGroup);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), unknown4);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), unknown5);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), ThumMagic);
        offset += 4;

        var f = floats ?? [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(offset), f[i]);
            offset += 4;
        }

        return data;
    }
}
