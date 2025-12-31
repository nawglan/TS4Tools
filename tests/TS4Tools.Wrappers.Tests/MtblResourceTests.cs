using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="MtblResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/MTBLResource.cs
///
/// Format:
/// - Magic: "MTBL" (4 bytes, 0x4C42544D little-endian)
/// - Version: uint32
/// - EntryCount: int32
/// - Entries: MtblEntry[] (56 bytes each)
/// </summary>
public class MtblResourceTests
{
    private static readonly ResourceKey TestKey = new(0x81CA1A10, 0, 0);
    private const uint MtblMagic = 0x4C42544D; // "MTBL" in little-endian
    private const int EntrySize = 56;

    /// <summary>
    /// Creates valid MTBL resource data.
    /// </summary>
    private static byte[] CreateValidData(
        uint version = 1,
        MtblEntry[]? entries = null)
    {
        entries ??= [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic
        writer.Write(MtblMagic);

        // Version
        writer.Write(version);

        // Entry count
        writer.Write(entries.Length);

        // Entries (56 bytes each)
        foreach (var entry in entries)
        {
            writer.Write(entry.ModelIid);
            writer.Write(entry.BaseFileNameHash);
            writer.Write((byte)entry.WidthAndMappingFlags);
            writer.Write(entry.MinimumWallHeight);
            writer.Write(entry.NumberOfLevels);
            writer.Write(entry.Unused);
            writer.Write(entry.ThumbnailBoundsMinX);
            writer.Write(entry.ThumbnailBoundsMinZ);
            writer.Write(entry.ThumbnailBoundsMinY);
            writer.Write(entry.ThumbnailBoundsMaxX);
            writer.Write(entry.ThumbnailBoundsMaxZ);
            writer.Write(entry.ThumbnailBoundsMaxY);
            writer.Write((uint)entry.ModelFlags);
            writer.Write(entry.VfxHash);
        }

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var entries = new MtblEntry[]
        {
            new(
                ModelIid: 0x1234567890ABCDEF,
                BaseFileNameHash: 0xFEDCBA0987654321,
                WidthAndMappingFlags: WidthAndMappingFlags.IsArchway | WidthAndMappingFlags.NoOpaque,
                MinimumWallHeight: 5,
                NumberOfLevels: 3,
                Unused: 0,
                ThumbnailBoundsMinX: 1.0f,
                ThumbnailBoundsMinZ: 2.0f,
                ThumbnailBoundsMinY: 3.0f,
                ThumbnailBoundsMaxX: 4.0f,
                ThumbnailBoundsMaxZ: 5.0f,
                ThumbnailBoundsMaxY: 6.0f,
                ModelFlags: ModelFlags.UsesInstancedShader | ModelFlags.VerticalScaling,
                VfxHash: 0xCAFEBABEDEADBEEF),
        };
        var data = CreateValidData(version: 2, entries: entries);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(2);
        resource.Entries.Should().HaveCount(1);
        var entry = resource.Entries[0];
        entry.ModelIid.Should().Be(0x1234567890ABCDEF);
        entry.BaseFileNameHash.Should().Be(0xFEDCBA0987654321);
        entry.WidthAndMappingFlags.Should().Be(WidthAndMappingFlags.IsArchway | WidthAndMappingFlags.NoOpaque);
        entry.MinimumWallHeight.Should().Be(5);
        entry.NumberOfLevels.Should().Be(3);
        entry.Unused.Should().Be(0);
        entry.ThumbnailBoundsMinX.Should().Be(1.0f);
        entry.ThumbnailBoundsMinZ.Should().Be(2.0f);
        entry.ThumbnailBoundsMinY.Should().Be(3.0f);
        entry.ThumbnailBoundsMaxX.Should().Be(4.0f);
        entry.ThumbnailBoundsMaxZ.Should().Be(5.0f);
        entry.ThumbnailBoundsMaxY.Should().Be(6.0f);
        entry.ModelFlags.Should().Be(ModelFlags.UsesInstancedShader | ModelFlags.VerticalScaling);
        entry.VfxHash.Should().Be(0xCAFEBABEDEADBEEF);
    }

    [Fact]
    public void Parse_MultipleEntries_ParsesAll()
    {
        // Arrange
        var entries = new MtblEntry[]
        {
            new(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100),
            new(3, 4, WidthAndMappingFlags.WidthCutout1, 1, 2, 0, 2, 2, 2, 3, 3, 3, ModelFlags.IsPortal, 200),
            new(5, 6, WidthAndMappingFlags.WidthCutout2, 2, 3, 0, 4, 4, 4, 5, 5, 5, ModelFlags.UsesCutout, 300),
        };
        var data = CreateValidData(entries: entries);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Entries.Should().HaveCount(3);
        resource.Entries[0].ModelIid.Should().Be(1);
        resource.Entries[1].ModelIid.Should().Be(3);
        resource.Entries[2].ModelIid.Should().Be(5);
        resource.Entries[1].ModelFlags.Should().Be(ModelFlags.IsPortal);
    }

    [Fact]
    public void Parse_EmptyEntries_ParsesCorrectly()
    {
        // Arrange
        var data = CreateValidData(entries: []);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Entries.Should().BeEmpty();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidData();
        data[0] = 0x00; // Corrupt magic

        // Act
        Action act = () => _ = new MtblResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TruncatedHeader_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[8]; // Too short for header

        // Act
        Action act = () => _ = new MtblResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_TruncatedEntry_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidData(entries: [new MtblEntry()]);
        // Truncate to just header + partial entry
        var truncated = data[..20];

        // Act
        Action act = () => _ = new MtblResource(TestKey, truncated);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_InvalidEntryCount_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidData();
        // Set entry count to a negative value
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), -1);

        // Act
        Action act = () => _ = new MtblResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*Invalid entry count*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var entries = new MtblEntry[]
        {
            new(0x1111, 0x2222, WidthAndMappingFlags.DiagonalCutoutMappingInUse, 10, 5, 0,
                1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f,
                ModelFlags.ShareTerrainLightmap, 0x3333),
        };
        var data = CreateValidData(version: 3, entries: entries);
        var resource = new MtblResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void AddEntry_AddsToList()
    {
        // Arrange
        var data = CreateValidData(entries: []);
        var resource = new MtblResource(TestKey, data);
        var newEntry = new MtblEntry(100, 200, WidthAndMappingFlags.None, 1, 2, 0,
            0, 0, 0, 1, 1, 1, ModelFlags.None, 300);

        // Act
        resource.AddEntry(newEntry);

        // Assert
        resource.Entries.Should().HaveCount(1);
        resource.Entries[0].ModelIid.Should().Be(100);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveEntry_RemovesFromList()
    {
        // Arrange
        var entry1 = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var entry2 = new MtblEntry(3, 4, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 200);
        var data = CreateValidData(entries: [entry1, entry2]);
        var resource = new MtblResource(TestKey, data);

        // Act
        var result = resource.RemoveEntry(entry1);

        // Assert
        result.Should().BeTrue();
        resource.Entries.Should().HaveCount(1);
        resource.Entries[0].ModelIid.Should().Be(3);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveEntry_NotFound_ReturnsFalse()
    {
        // Arrange
        var entry = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var data = CreateValidData(entries: [entry]);
        var resource = new MtblResource(TestKey, data);
        var notFoundEntry = new MtblEntry(99, 99, WidthAndMappingFlags.None, 0, 0, 0, 0, 0, 0, 0, 0, 0, ModelFlags.None, 0);

        // Act
        var result = resource.RemoveEntry(notFoundEntry);

        // Assert
        result.Should().BeFalse();
        resource.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void ClearEntries_RemovesAllEntries()
    {
        // Arrange
        var entries = new MtblEntry[]
        {
            new(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100),
            new(3, 4, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 200),
        };
        var data = CreateValidData(entries: entries);
        var resource = new MtblResource(TestKey, data);

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
        var entry = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var data = CreateValidData(entries: [entry]);
        var resource = new MtblResource(TestKey, data);
        var newEntry = new MtblEntry(99, 88, WidthAndMappingFlags.IsArchway, 5, 3, 0,
            10, 20, 30, 40, 50, 60, ModelFlags.UsesCutout, 77);

        // Act
        resource.SetEntry(0, newEntry);

        // Assert
        resource.Entries[0].ModelIid.Should().Be(99);
        resource.Entries[0].BaseFileNameHash.Should().Be(88);
        resource.Entries[0].WidthAndMappingFlags.Should().Be(WidthAndMappingFlags.IsArchway);
        resource.Entries[0].ModelFlags.Should().Be(ModelFlags.UsesCutout);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetEntry_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidData(entries: []);
        var resource = new MtblResource(TestKey, data);
        var entry = new MtblEntry();

        // Act
        Action act = () => resource.SetEntry(0, entry);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetEntry_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidData(entries: [new MtblEntry()]);
        var resource = new MtblResource(TestKey, data);

        // Act
        Action act = () => resource.SetEntry(-1, new MtblEntry());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new MtblResource(TestKey, data);

        // Act
        resource.Version = 5;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.Version.Should().Be(5);
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new MtblResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(0);
        resource.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ModifiedValues_ProducesValidData()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new MtblResource(TestKey, data);
        resource.Version = 10;
        resource.AddEntry(new MtblEntry(111, 222, WidthAndMappingFlags.SingleTextureCutout,
            5, 2, 0, 1, 2, 3, 4, 5, 6, ModelFlags.HorizontalScaling, 333));

        // Act
        var serialized = resource.Data;
        var reloaded = new MtblResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(10);
        reloaded.Entries.Should().HaveCount(1);
        reloaded.Entries[0].ModelIid.Should().Be(111);
        reloaded.Entries[0].WidthAndMappingFlags.Should().Be(WidthAndMappingFlags.SingleTextureCutout);
        reloaded.Entries[0].ModelFlags.Should().Be(ModelFlags.HorizontalScaling);
    }

    [Fact]
    public void MtblEntry_Equality_WorksCorrectly()
    {
        // Arrange
        var entry1 = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var entry2 = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var entry3 = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 999);

        // Assert
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
    }

    [Fact]
    public void Parse_ManyEntries_ParsesCorrectly()
    {
        // Arrange
        var entries = Enumerable.Range(0, 100)
            .Select(i => new MtblEntry(
                (ulong)i, (ulong)(i * 2), WidthAndMappingFlags.None,
                (byte)(i % 256), (byte)(i % 10), 0,
                i, i * 2, i * 3, i * 4, i * 5, i * 6,
                ModelFlags.None, (ulong)(i * 100)))
            .ToArray();
        var data = CreateValidData(entries: entries);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Entries.Should().HaveCount(100);
        resource.Entries[50].ModelIid.Should().Be(50);
        resource.Entries[50].BaseFileNameHash.Should().Be(100);
        resource.Entries[50].ThumbnailBoundsMinX.Should().Be(50.0f);
        resource.Entries[50].VfxHash.Should().Be(5000);
    }

    [Theory]
    [InlineData(WidthAndMappingFlags.None)]
    [InlineData(WidthAndMappingFlags.WidthCutout1)]
    [InlineData(WidthAndMappingFlags.WidthCutout2)]
    [InlineData(WidthAndMappingFlags.WidthCutout3)]
    [InlineData(WidthAndMappingFlags.NoOpaque)]
    [InlineData(WidthAndMappingFlags.IsArchway)]
    [InlineData(WidthAndMappingFlags.SingleTextureCutout)]
    [InlineData(WidthAndMappingFlags.DiagonalCutoutMappingInUse)]
    [InlineData(WidthAndMappingFlags.WidthCutout1 | WidthAndMappingFlags.IsArchway)]
    public void Parse_WidthAndMappingFlags_PreservesAllFlags(WidthAndMappingFlags flags)
    {
        // Arrange
        var entry = new MtblEntry(1, 2, flags, 0, 1, 0, 0, 0, 0, 1, 1, 1, ModelFlags.None, 100);
        var data = CreateValidData(entries: [entry]);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Entries[0].WidthAndMappingFlags.Should().Be(flags);
    }

    [Theory]
    [InlineData(ModelFlags.None)]
    [InlineData(ModelFlags.UsesInstancedShader)]
    [InlineData(ModelFlags.VerticalScaling)]
    [InlineData(ModelFlags.RequiresProceduralLightmap)]
    [InlineData(ModelFlags.UsesTreeInstanceShader)]
    [InlineData(ModelFlags.HorizontalScaling)]
    [InlineData(ModelFlags.IsPortal)]
    [InlineData(ModelFlags.UsesCounterCutout)]
    [InlineData(ModelFlags.ShareTerrainLightmap)]
    [InlineData(ModelFlags.UsesWallLightmap)]
    [InlineData(ModelFlags.UsesCutout)]
    [InlineData(ModelFlags.InstanceWithFullTransform)]
    [InlineData(ModelFlags.UsesInstancedShader | ModelFlags.VerticalScaling | ModelFlags.IsPortal)]
    public void Parse_ModelFlags_PreservesAllFlags(ModelFlags flags)
    {
        // Arrange
        var entry = new MtblEntry(1, 2, WidthAndMappingFlags.None, 0, 1, 0, 0, 0, 0, 1, 1, 1, flags, 100);
        var data = CreateValidData(entries: [entry]);

        // Act
        var resource = new MtblResource(TestKey, data);

        // Assert
        resource.Entries[0].ModelFlags.Should().Be(flags);
    }

    [Fact]
    public void Serialize_AllFieldTypes_RoundTripsCorrectly()
    {
        // Arrange - test with extreme values
        var entry = new MtblEntry(
            ModelIid: ulong.MaxValue,
            BaseFileNameHash: ulong.MaxValue,
            WidthAndMappingFlags: (WidthAndMappingFlags)0x7F, // All 7 flags set
            MinimumWallHeight: byte.MaxValue,
            NumberOfLevels: byte.MaxValue,
            Unused: byte.MaxValue,
            ThumbnailBoundsMinX: float.MinValue,
            ThumbnailBoundsMinZ: float.MaxValue,
            ThumbnailBoundsMinY: float.Epsilon,
            ThumbnailBoundsMaxX: float.NegativeInfinity,
            ThumbnailBoundsMaxZ: float.PositiveInfinity,
            ThumbnailBoundsMaxY: 0.0f,
            ModelFlags: (ModelFlags)0x7FF, // All 11 flags set
            VfxHash: ulong.MaxValue);
        var data = CreateValidData(version: uint.MaxValue, entries: [entry]);

        // Act
        var resource = new MtblResource(TestKey, data);
        var serialized = resource.Data.ToArray();
        var reloaded = new MtblResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(uint.MaxValue);
        var reloadedEntry = reloaded.Entries[0];
        reloadedEntry.ModelIid.Should().Be(ulong.MaxValue);
        reloadedEntry.BaseFileNameHash.Should().Be(ulong.MaxValue);
        reloadedEntry.MinimumWallHeight.Should().Be(byte.MaxValue);
        reloadedEntry.ThumbnailBoundsMinX.Should().Be(float.MinValue);
        reloadedEntry.ThumbnailBoundsMaxX.Should().Be(float.NegativeInfinity);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        // Arrange
        var factory = new MtblResourceFactory();
        var data = CreateValidData();

        // Act
        var resource = factory.Create(TestKey, data);

        // Assert
        resource.Should().BeOfType<MtblResource>();
        ((MtblResource)resource).Version.Should().Be(1);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsDefaultResource()
    {
        // Arrange
        var factory = new MtblResourceFactory();

        // Act
        var resource = factory.CreateEmpty(TestKey);

        // Assert
        resource.Should().BeOfType<MtblResource>();
        var mtbl = (MtblResource)resource;
        mtbl.Version.Should().Be(0);
        mtbl.Entries.Should().BeEmpty();
    }
}
