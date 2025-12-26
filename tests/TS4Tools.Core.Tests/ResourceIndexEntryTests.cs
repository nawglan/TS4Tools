using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="ResourceIndexEntry"/> and <see cref="ResourceIndexEntrySerializer"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs
/// - Index entry binary format (32 bytes when fully expanded):
///   - Offset 4-7: ResourceType (uint32)
///   - Offset 8-11: ResourceGroup (uint32)
///   - Offset 12-15: Instance high bits (uint32)
///   - Offset 16-19: Instance low bits (uint32)
///   - Offset 20-23: ChunkOffset (uint32)
///   - Offset 24-27: FileSize (uint32 with bit 31 always set)
///   - Offset 28-31: MemSize (uint32)
///   - Offset 32-33: Compressed type (uint16)
///   - Offset 34-35: Unknown2 (uint16, always 0x0001)
/// - Index type flags optimize storage:
///   - Bit 0: Type stored in header
///   - Bit 1: Group stored in header
///   - Bit 2: InstanceHigh stored in header
/// </summary>
public class ResourceIndexEntryTests
{
    [Fact]
    public void Constructor_Default_InitializesWithDefaults()
    {
        // Arrange & Act
        var entry = new ResourceIndexEntry();

        // Assert
        entry.Key.Should().Be(default(ResourceKey));
        entry.ChunkOffset.Should().Be(0);
        entry.FileSize.Should().Be(0);
        entry.MemorySize.Should().Be(0);
        entry.CompressionType.Should().Be(0);
        entry.Unknown2.Should().Be(1); // Default per legacy spec
        entry.IsDeleted.Should().BeFalse();
        entry.IsCompressed.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithKey_SetsKey()
    {
        // Arrange
        var key = new ResourceKey(0x220557DA, 0x12345678, 0xABCDEF0123456789);

        // Act
        var entry = new ResourceIndexEntry(key);

        // Assert
        entry.Key.Should().Be(key);
        entry.ChunkOffset.Should().Be(0xFFFFFFFF); // Not yet written marker
    }

    [Fact]
    public void IsCompressed_WhenFileSizeNotEqualMemorySize_ReturnsTrue()
    {
        // Arrange
        var entry = new ResourceIndexEntry
        {
            FileSize = 100,
            MemorySize = 200
        };

        // Act & Assert
        entry.IsCompressed.Should().BeTrue();
    }

    [Fact]
    public void IsCompressed_WhenFileSizeEqualsMemorySize_ReturnsFalse()
    {
        // Arrange
        var entry = new ResourceIndexEntry
        {
            FileSize = 100,
            MemorySize = 100
        };

        // Act & Assert
        entry.IsCompressed.Should().BeFalse();
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var key = new ResourceKey(0x220557DA, 0x12345678, 0xABCDEF0123456789);
        var original = new ResourceIndexEntry(key)
        {
            ChunkOffset = 1000,
            FileSize = 500,
            MemorySize = 1000,
            CompressionType = 0x5A42,
            Unknown2 = 1
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Key.Should().Be(original.Key);
        clone.ChunkOffset.Should().Be(original.ChunkOffset);
        clone.FileSize.Should().Be(original.FileSize);
        clone.MemorySize.Should().Be(original.MemorySize);
        clone.CompressionType.Should().Be(original.CompressionType);
        clone.Unknown2.Should().Be(original.Unknown2);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var key = new ResourceKey(0x220557DA, 0, 0);
        var original = new ResourceIndexEntry(key)
        {
            FileSize = 100
        };

        // Act
        var clone = original.Clone();
        clone.FileSize = 999;

        // Assert
        original.FileSize.Should().Be(100);
        clone.FileSize.Should().Be(999);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var key = new ResourceKey(0x220557DA, 0x12345678, 0xABCDEF0123456789);
        var entry = new ResourceIndexEntry(key)
        {
            ChunkOffset = 0x1000,
            FileSize = 100,
            MemorySize = 200
        };

        // Act
        var result = entry.ToString();

        // Assert
        result.Should().Contain("0x00001000"); // ChunkOffset
        result.Should().Contain("100/200"); // FileSize/MemorySize
        result.Should().Contain("True"); // IsCompressed
    }
}

/// <summary>
/// Tests for <see cref="ResourceIndexEntrySerializer"/>.
/// </summary>
public class ResourceIndexEntrySerializerTests
{
    [Fact]
    public void FullEntrySize_Is32Bytes()
    {
        // Per legacy spec, full entry is 32 bytes
        ResourceIndexEntrySerializer.FullEntrySize.Should().Be(32);
    }

    [Fact]
    public void BaseEntrySize_Is20Bytes()
    {
        // Base size: InstanceLow(4) + ChunkOffset(4) + FileSize(4) + MemSize(4) + Compressed(2) + Unknown2(2) = 20
        ResourceIndexEntrySerializer.BaseEntrySize.Should().Be(20);
    }

    [Fact]
    public void GetEntrySize_NoFlags_ReturnsFullSize()
    {
        // When no bits set, all fields are in entry
        // 20 (base) + 4 (Type) + 4 (Group) + 4 (InstanceHigh) = 32
        var size = ResourceIndexEntrySerializer.GetEntrySize(0);
        size.Should().Be(32);
    }

    [Fact]
    public void GetEntrySize_TypeInHeader_Returns28()
    {
        // Bit 0 set: Type in header
        // 20 (base) + 4 (Group) + 4 (InstanceHigh) = 28
        var size = ResourceIndexEntrySerializer.GetEntrySize(0x01);
        size.Should().Be(28);
    }

    [Fact]
    public void GetEntrySize_TypeAndGroupInHeader_Returns24()
    {
        // Bits 0 and 1 set: Type and Group in header
        // 20 (base) + 4 (InstanceHigh) = 24
        var size = ResourceIndexEntrySerializer.GetEntrySize(0x03);
        size.Should().Be(24);
    }

    [Fact]
    public void GetEntrySize_AllInHeader_Returns20()
    {
        // Bits 0, 1, 2 set: Type, Group, InstanceHigh in header
        // 20 (base) = 20
        var size = ResourceIndexEntrySerializer.GetEntrySize(0x07);
        size.Should().Be(20);
    }

    [Fact]
    public void GetHeaderFieldCount_NoFlags_Returns1()
    {
        // Only indexType itself
        var count = ResourceIndexEntrySerializer.GetHeaderFieldCount(0);
        count.Should().Be(1);
    }

    [Fact]
    public void GetHeaderFieldCount_AllFlags_Returns4()
    {
        // indexType + Type + Group + InstanceHigh
        var count = ResourceIndexEntrySerializer.GetHeaderFieldCount(0x07);
        count.Should().Be(4);
    }

    [Fact]
    public void Read_FullEntry_ParsesCorrectly()
    {
        // Arrange - construct a full entry (32 bytes)
        var data = new byte[32];
        // Type (4 bytes)
        data[0] = 0xDA; data[1] = 0x57; data[2] = 0x05; data[3] = 0x22; // 0x220557DA
        // Group (4 bytes)
        data[4] = 0x78; data[5] = 0x56; data[6] = 0x34; data[7] = 0x12; // 0x12345678
        // InstanceHigh (4 bytes)
        data[8] = 0xEF; data[9] = 0xCD; data[10] = 0xAB; data[11] = 0x89; // 0x89ABCDEF
        // InstanceLow (4 bytes)
        data[12] = 0x01; data[13] = 0x23; data[14] = 0x45; data[15] = 0x67; // 0x67452301
        // ChunkOffset (4 bytes)
        data[16] = 0x00; data[17] = 0x10; data[18] = 0x00; data[19] = 0x00; // 0x00001000
        // FileSize (4 bytes) - bit 31 set
        data[20] = 0x64; data[21] = 0x00; data[22] = 0x00; data[23] = 0x80; // 100 | 0x80000000
        // MemSize (4 bytes)
        data[24] = 0xC8; data[25] = 0x00; data[26] = 0x00; data[27] = 0x00; // 200
        // Compressed (2 bytes)
        data[28] = 0x42; data[29] = 0x5A; // 0x5A42 (ZB)
        // Unknown2 (2 bytes)
        data[30] = 0x01; data[31] = 0x00; // 1

        // Act
        var entry = ResourceIndexEntrySerializer.Read(data, 0, 0, 0, 0);

        // Assert
        entry.Key.ResourceType.Should().Be(0x220557DA);
        entry.Key.ResourceGroup.Should().Be(0x12345678);
        entry.Key.Instance.Should().Be(0x89ABCDEF67452301);
        entry.ChunkOffset.Should().Be(0x00001000);
        entry.FileSize.Should().Be(100);
        entry.MemorySize.Should().Be(200);
        entry.CompressionType.Should().Be(0x5A42);
        entry.Unknown2.Should().Be(1);
        entry.IsCompressed.Should().BeTrue();
    }

    [Fact]
    public void Read_WithHeaderType_UsesHeaderValue()
    {
        // Arrange - entry without Type (28 bytes, bit 0 set)
        var data = new byte[28];
        // Group (4 bytes)
        data[0] = 0x78; data[1] = 0x56; data[2] = 0x34; data[3] = 0x12;
        // InstanceHigh (4 bytes)
        data[4] = 0xEF; data[5] = 0xCD; data[6] = 0xAB; data[7] = 0x89;
        // InstanceLow (4 bytes)
        data[8] = 0x01; data[9] = 0x23; data[10] = 0x45; data[11] = 0x67;
        // ChunkOffset (4 bytes)
        data[12] = 0x00; data[13] = 0x10; data[14] = 0x00; data[15] = 0x00;
        // FileSize (4 bytes)
        data[16] = 0x64; data[17] = 0x00; data[18] = 0x00; data[19] = 0x80;
        // MemSize (4 bytes)
        data[20] = 0xC8; data[21] = 0x00; data[22] = 0x00; data[23] = 0x00;
        // Compressed (2 bytes)
        data[24] = 0x00; data[25] = 0x00;
        // Unknown2 (2 bytes)
        data[26] = 0x01; data[27] = 0x00;

        // Act - Type from header
        var entry = ResourceIndexEntrySerializer.Read(data, 0x01, 0x220557DA, 0, 0);

        // Assert
        entry.Key.ResourceType.Should().Be(0x220557DA);
        entry.Key.ResourceGroup.Should().Be(0x12345678);
    }

    [Fact]
    public void Read_WithAllHeaderValues_UsesAllHeaderValues()
    {
        // Arrange - entry with all key parts in header (20 bytes base)
        var data = new byte[20];
        // InstanceLow (4 bytes)
        data[0] = 0x01; data[1] = 0x23; data[2] = 0x45; data[3] = 0x67;
        // ChunkOffset (4 bytes)
        data[4] = 0x00; data[5] = 0x10; data[6] = 0x00; data[7] = 0x00;
        // FileSize (4 bytes)
        data[8] = 0x64; data[9] = 0x00; data[10] = 0x00; data[11] = 0x80;
        // MemSize (4 bytes)
        data[12] = 0xC8; data[13] = 0x00; data[14] = 0x00; data[15] = 0x00;
        // Compressed (2 bytes)
        data[16] = 0x00; data[17] = 0x00;
        // Unknown2 (2 bytes)
        data[18] = 0x01; data[19] = 0x00;

        // Act - All key parts from header (indexType = 0x07)
        var entry = ResourceIndexEntrySerializer.Read(data, 0x07, 0x220557DA, 0x12345678, 0x89ABCDEF);

        // Assert
        entry.Key.ResourceType.Should().Be(0x220557DA);
        entry.Key.ResourceGroup.Should().Be(0x12345678);
        entry.Key.Instance.Should().Be(0x89ABCDEF67452301);
    }
}
