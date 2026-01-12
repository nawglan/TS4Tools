using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="TrimResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TRIMResource.cs
///
/// Format:
/// - Magic: "TRIM" (4 bytes)
/// - Version: uint32 (3 or 4+)
/// - EntryCount: int32
/// - Entries: TrimEntry3[] (if version == 3) or TrimEntry4[] (if version >= 4)
/// - MaterialSetKey: TGI block (Type, Group, Instance - 16 bytes)
/// - HasFootprint: byte
/// </summary>
public class TrimResourceTests
{
    private static readonly ResourceKey TestKey = new(0x76BCF80C, 0, 0);
    private const uint TrimMagic = 0x4D495254; // "TRIM" in little-endian

    /// <summary>
    /// Creates valid TRIM resource data for version 4 (4-float entries).
    /// </summary>
    private static byte[] CreateValidDataV4(
        uint version = 4,
        TrimEntry[]? entries = null,
        uint materialType = 0,
        uint materialGroup = 0,
        ulong materialInstance = 0,
        byte hasFootprint = 0)
    {
        entries ??= [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic
        writer.Write(TrimMagic);

        // Version
        writer.Write(version);

        // Entry count
        writer.Write(entries.Length);

        // Entries (4 floats each for v4+)
        foreach (var entry in entries)
        {
            writer.Write(entry.X);
            writer.Write(entry.Y);
            writer.Write(entry.V);
            writer.Write(entry.MappingMode);
        }

        // TGI block (default order: Type, Group, Instance)
        writer.Write(materialType);
        writer.Write(materialGroup);
        writer.Write(materialInstance);

        // HasFootprint
        writer.Write(hasFootprint);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates valid TRIM resource data for version 3 (3-float entries).
    /// </summary>
    private static byte[] CreateValidDataV3(
        TrimEntry[]? entries = null,
        uint materialType = 0,
        uint materialGroup = 0,
        ulong materialInstance = 0,
        byte hasFootprint = 0)
    {
        entries ??= [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic
        writer.Write(TrimMagic);

        // Version 3
        writer.Write(3u);

        // Entry count
        writer.Write(entries.Length);

        // Entries (3 floats each for v3)
        foreach (var entry in entries)
        {
            writer.Write(entry.X);
            writer.Write(entry.Y);
            writer.Write(entry.V);
            // No mapping mode for v3
        }

        // TGI block
        writer.Write(materialType);
        writer.Write(materialGroup);
        writer.Write(materialInstance);

        // HasFootprint
        writer.Write(hasFootprint);

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidDataV4_ExtractsFields()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f, 4.0f),
            new(5.0f, 6.0f, 7.0f, 8.0f),
        };
        var data = CreateValidDataV4(
            version: 4,
            entries: entries,
            materialType: 0x12345678,
            materialGroup: 0x87654321,
            materialInstance: 0xDEADBEEFCAFEBABE,
            hasFootprint: 1);

        // Act
        var resource = new TrimResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(4);
        resource.Entries.Should().HaveCount(2);
        resource.Entries[0].X.Should().Be(1.0f);
        resource.Entries[0].Y.Should().Be(2.0f);
        resource.Entries[0].V.Should().Be(3.0f);
        resource.Entries[0].MappingMode.Should().Be(4.0f);
        resource.Entries[1].X.Should().Be(5.0f);
        resource.Entries[1].Y.Should().Be(6.0f);
        resource.Entries[1].V.Should().Be(7.0f);
        resource.Entries[1].MappingMode.Should().Be(8.0f);
        resource.MaterialSetKey.ResourceType.Should().Be(0x12345678);
        resource.MaterialSetKey.ResourceGroup.Should().Be(0x87654321);
        resource.MaterialSetKey.Instance.Should().Be(0xDEADBEEFCAFEBABE);
        resource.HasFootprint.Should().Be(1);
    }

    [Fact]
    public void Parse_ValidDataV3_ExtractsFields()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f),
            new(4.0f, 5.0f, 6.0f),
            new(7.0f, 8.0f, 9.0f),
        };
        var data = CreateValidDataV3(
            entries: entries,
            materialType: 0xABCDEF01,
            materialGroup: 0x12345678,
            materialInstance: 0x1234567890ABCDEF,
            hasFootprint: 0);

        // Act
        var resource = new TrimResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(3);
        resource.Entries.Should().HaveCount(3);
        resource.Entries[0].X.Should().Be(1.0f);
        resource.Entries[0].Y.Should().Be(2.0f);
        resource.Entries[0].V.Should().Be(3.0f);
        resource.Entries[0].MappingMode.Should().Be(0f); // V3 doesn't have mapping mode
        resource.Entries[2].X.Should().Be(7.0f);
        resource.MaterialSetKey.ResourceType.Should().Be(0xABCDEF01);
        resource.HasFootprint.Should().Be(0);
    }

    [Fact]
    public void Parse_EmptyEntries_ParsesCorrectly()
    {
        // Arrange
        var data = CreateValidDataV4(entries: []);

        // Act
        var resource = new TrimResource(TestKey, data);

        // Assert
        resource.Entries.Should().BeEmpty();
        resource.Version.Should().Be(4);
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidDataV4();
        data[0] = 0x00; // Corrupt magic

        // Act
        Action act = () => _ = new TrimResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[8]; // Too short

        // Act
        Action act = () => _ = new TrimResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_InvalidEntryCount_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidDataV4();
        // Set entry count to a negative value
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), -1);

        // Act
        Action act = () => _ = new TrimResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*Invalid entry count*");
    }

    [Fact]
    public void Serialize_V4_RoundTrip_PreservesData()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.5f, 2.5f, 3.5f, 4.5f),
            new(5.5f, 6.5f, 7.5f, 8.5f),
        };
        var data = CreateValidDataV4(
            version: 4,
            entries: entries,
            materialType: 111,
            materialGroup: 222,
            materialInstance: 333,
            hasFootprint: 1);
        var resource = new TrimResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Serialize_V3_RoundTrip_PreservesData()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f),
            new(4.0f, 5.0f, 6.0f),
        };
        var data = CreateValidDataV3(
            entries: entries,
            materialType: 100,
            materialGroup: 200,
            materialInstance: 300,
            hasFootprint: 0);
        var resource = new TrimResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void AddEntry_AddsToList()
    {
        // Arrange
        var data = CreateValidDataV4(entries: []);
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.AddEntry(new TrimEntry(1.0f, 2.0f, 3.0f, 4.0f));
        resource.AddEntry(new TrimEntry(5.0f, 6.0f, 7.0f, 8.0f));

        // Assert
        resource.Entries.Should().HaveCount(2);
        resource.Entries[0].X.Should().Be(1.0f);
        resource.Entries[1].X.Should().Be(5.0f);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveEntry_RemovesFromList()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f, 4.0f),
            new(5.0f, 6.0f, 7.0f, 8.0f),
        };
        var data = CreateValidDataV4(entries: entries);
        var resource = new TrimResource(TestKey, data);

        // Act
        var result = resource.RemoveEntry(new TrimEntry(1.0f, 2.0f, 3.0f, 4.0f));

        // Assert
        result.Should().BeTrue();
        resource.Entries.Should().HaveCount(1);
        resource.Entries[0].X.Should().Be(5.0f);
    }

    [Fact]
    public void ClearEntries_RemovesAllEntries()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f, 4.0f),
            new(5.0f, 6.0f, 7.0f, 8.0f),
        };
        var data = CreateValidDataV4(entries: entries);
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.ClearEntries();

        // Assert
        resource.Entries.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetEntry_UpdatesEntry()
    {
        // Arrange
        var entries = new TrimEntry[]
        {
            new(1.0f, 2.0f, 3.0f, 4.0f),
        };
        var data = CreateValidDataV4(entries: entries);
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.SetEntry(0, new TrimEntry(10.0f, 20.0f, 30.0f, 40.0f));

        // Assert
        resource.Entries[0].X.Should().Be(10.0f);
        resource.Entries[0].Y.Should().Be(20.0f);
        resource.Entries[0].V.Should().Be(30.0f);
        resource.Entries[0].MappingMode.Should().Be(40.0f);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetEntry_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidDataV4(entries: []);
        var resource = new TrimResource(TestKey, data);

        // Act
        Action act = () => resource.SetEntry(0, new TrimEntry(1, 2, 3, 4));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidDataV4();
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.Version = 5;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.Version.Should().Be(5);
    }

    [Fact]
    public void MaterialSetKey_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidDataV4();
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.MaterialSetKey = new ResourceKey(1, 2, 3);

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.MaterialSetKey.ResourceType.Should().Be(1);
        resource.MaterialSetKey.ResourceGroup.Should().Be(2);
        resource.MaterialSetKey.Instance.Should().Be(3);
    }

    [Fact]
    public void HasFootprint_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidDataV4();
        var resource = new TrimResource(TestKey, data);

        // Act
        resource.HasFootprint = 1;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.HasFootprint.Should().Be(1);
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new TrimResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(4); // Default version is 4
        resource.Entries.Should().BeEmpty();
        resource.MaterialSetKey.Should().Be(default(ResourceKey));
        resource.HasFootprint.Should().Be(0);
    }

    [Fact]
    public void Serialize_ModifiedValues_ProducesValidData()
    {
        // Arrange
        var data = CreateValidDataV4();
        var resource = new TrimResource(TestKey, data);
        resource.Version = 5;
        resource.MaterialSetKey = new ResourceKey(100, 200, 300);
        resource.HasFootprint = 1;
        resource.AddEntry(new TrimEntry(1.0f, 2.0f, 3.0f, 4.0f));

        // Act
        var serialized = resource.Data;
        var reloaded = new TrimResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(5);
        reloaded.MaterialSetKey.ResourceType.Should().Be(100);
        reloaded.MaterialSetKey.ResourceGroup.Should().Be(200);
        reloaded.MaterialSetKey.Instance.Should().Be(300);
        reloaded.HasFootprint.Should().Be(1);
        reloaded.Entries.Should().HaveCount(1);
        reloaded.Entries[0].X.Should().Be(1.0f);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Parse_DifferentVersions_ParsesCorrectly(uint version)
    {
        // Arrange
        var entries = new TrimEntry[] { new(1.0f, 2.0f, 3.0f, 4.0f) };
        var data = version == 3
            ? CreateValidDataV3(entries: entries)
            : CreateValidDataV4(version: version, entries: entries);

        // Act
        var resource = new TrimResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(version);
        resource.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void TrimEntry_Equality_WorksCorrectly()
    {
        // Arrange
        var entry1 = new TrimEntry(1.0f, 2.0f, 3.0f, 4.0f);
        var entry2 = new TrimEntry(1.0f, 2.0f, 3.0f, 4.0f);
        var entry3 = new TrimEntry(1.0f, 2.0f, 3.0f, 5.0f);

        // Assert
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
    }

    [Fact]
    public void Parse_ManyEntries_ParsesCorrectly()
    {
        // Arrange
        var entries = Enumerable.Range(0, 100)
            .Select(i => new TrimEntry(i, i * 2, i * 3, i * 4))
            .ToArray();
        var data = CreateValidDataV4(entries: entries);

        // Act
        var resource = new TrimResource(TestKey, data);

        // Assert
        resource.Entries.Should().HaveCount(100);
        resource.Entries[50].X.Should().Be(50.0f);
        resource.Entries[50].Y.Should().Be(100.0f);
        resource.Entries[50].V.Should().Be(150.0f);
        resource.Entries[50].MappingMode.Should().Be(200.0f);
    }
}
