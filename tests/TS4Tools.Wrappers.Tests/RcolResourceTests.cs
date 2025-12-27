using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RcolResource parsing and serialization.
/// </summary>
public class RcolResourceTests
{
    /// <summary>
    /// Creates a minimal valid RCOL resource with one chunk.
    /// </summary>
    private static byte[] CreateMinimalRcol(uint version = 3, int publicChunks = 1, uint unused = 0,
        uint chunkType = RcolConstants.Modl, byte[]? chunkData = null)
    {
        chunkData ??= "MODL\x00\x00\x00\x00TEST"u8.ToArray(); // Minimal MODL-like data

        // Header: 5 x uint32 = 20 bytes
        // Chunk TGI: 1 x 16 bytes = 16 bytes
        // External TGIs: 0 x 16 bytes = 0 bytes
        // Chunk Index: 1 x 8 bytes = 8 bytes
        // Total header size: 44 bytes
        // Chunk data starts at offset 44

        int headerSize = 20 + 16 + 0 + 8; // 44 bytes
        var buffer = new byte[headerSize + chunkData.Length];
        var span = buffer.AsSpan();

        int pos = 0;

        // Header
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], version);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], publicChunks);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], unused);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 0); // External count
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 1); // Chunk count
        pos += 4;

        // Chunk TGI (ITG order)
        BinaryPrimitives.WriteUInt64LittleEndian(span[pos..], 0x123456789ABCDEF0); // Instance
        pos += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], chunkType); // Type
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0x00000001); // Group
        pos += 4;

        // Chunk index
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], (uint)headerSize); // Position
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], chunkData.Length); // Length
        pos += 4;

        // Chunk data
        chunkData.CopyTo(span[pos..]);

        return buffer;
    }

    [Fact]
    public void Parse_MinimalRcol_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMinimalRcol();
        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Version.Should().Be(3);
        resource.PublicChunksCount.Should().Be(1);
        resource.ChunkCount.Should().Be(1);
        resource.ExternalResourceCount.Should().Be(0);
    }

    [Fact]
    public void Parse_MinimalRcol_ParsesChunk()
    {
        // Arrange
        var data = CreateMinimalRcol();
        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, data);

        // Assert
        resource.Chunks.Should().HaveCount(1);
        var chunk = resource.Chunks[0];
        chunk.TgiBlock.ResourceType.Should().Be(RcolConstants.Modl);
        chunk.TgiBlock.Instance.Should().Be(0x123456789ABCDEF0);
        chunk.TgiBlock.ResourceGroup.Should().Be(0x00000001);
        chunk.Block.Tag.Should().Be("MODL");
        chunk.Block.IsKnownType.Should().BeFalse(); // UnknownRcolBlock
    }

    [Fact]
    public void Serialize_AfterParse_RoundTripsCorrectly()
    {
        // Arrange
        var originalData = CreateMinimalRcol();
        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);
        var resource = new RcolResource(key, originalData);

        // Act
        var serialized = resource.Data;

        // Assert
        serialized.ToArray().Should().Equal(originalData);
    }

    [Fact]
    public void Parse_TooShortData_ReturnsInvalid()
    {
        // Arrange
        var data = new byte[10]; // Too short for header
        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, data);

        // Assert
        resource.IsValid.Should().BeFalse();
        resource.ChunkCount.Should().Be(0);
    }

    [Fact]
    public void Parse_EmptyData_InitializesWithDefaults()
    {
        // Arrange
        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, ReadOnlyMemory<byte>.Empty);

        // Assert - Empty data calls InitializeDefaults which sets up an empty but valid resource
        resource.IsValid.Should().BeTrue();
        resource.ChunkCount.Should().Be(0);
        resource.ExternalResourceCount.Should().Be(0);
    }

    [Fact]
    public void Parse_WithExternalResources_ParsesExternals()
    {
        // Arrange - Create RCOL with 2 external resources
        var chunkData = "MODL\x00\x00\x00\x00TEST"u8.ToArray();

        // Header size: 20 + 16 (chunk TGI) + 32 (2 external TGIs) + 8 (index) = 76 bytes
        int headerSize = 20 + 16 + 32 + 8;
        var buffer = new byte[headerSize + chunkData.Length];
        var span = buffer.AsSpan();

        int pos = 0;

        // Header
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 3); // Version
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 1); // Public chunks
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0); // Unused
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 2); // External count
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 1); // Chunk count
        pos += 4;

        // Chunk TGI
        BinaryPrimitives.WriteUInt64LittleEndian(span[pos..], 0x1111111111111111);
        pos += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], RcolConstants.Modl);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0);
        pos += 4;

        // External TGI 1
        BinaryPrimitives.WriteUInt64LittleEndian(span[pos..], 0x2222222222222222);
        pos += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], RcolConstants.Matd);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0);
        pos += 4;

        // External TGI 2
        BinaryPrimitives.WriteUInt64LittleEndian(span[pos..], 0x3333333333333333);
        pos += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], RcolConstants.Mlod);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0);
        pos += 4;

        // Chunk index
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], (uint)headerSize);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], chunkData.Length);
        pos += 4;

        // Chunk data
        chunkData.CopyTo(span[pos..]);

        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, buffer);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.ExternalResourceCount.Should().Be(2);
        resource.ExternalResources[0].Instance.Should().Be(0x2222222222222222);
        resource.ExternalResources[0].ResourceType.Should().Be(RcolConstants.Matd);
        resource.ExternalResources[1].Instance.Should().Be(0x3333333333333333);
        resource.ExternalResources[1].ResourceType.Should().Be(RcolConstants.Mlod);
    }

    [Fact]
    public void Parse_SingleChunkZeroPosition_HandlesSpecialCase()
    {
        // This tests the special case from GenericRCOLResource.cs lines 86-95
        // When there's a single chunk with Position=0, data starts after the index

        var chunkData = "MODL\x00\x00\x00\x00DATA"u8.ToArray();

        // Header: 20 bytes
        // Chunk TGI: 16 bytes
        // External TGIs: 0 bytes
        // Chunk Index: 8 bytes (with Position=0, Length=0)
        // Total header: 44 bytes
        int headerSize = 20 + 16 + 0 + 8;
        var buffer = new byte[headerSize + chunkData.Length];
        var span = buffer.AsSpan();

        int pos = 0;

        // Header
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 3);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 0);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 1);
        pos += 4;

        // Chunk TGI
        BinaryPrimitives.WriteUInt64LittleEndian(span[pos..], 0xABCDABCDABCDABCD);
        pos += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], RcolConstants.Modl);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0);
        pos += 4;

        // Chunk index with Position=0 (special case)
        BinaryPrimitives.WriteUInt32LittleEndian(span[pos..], 0); // Position = 0
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(span[pos..], 0); // Length = 0 (will be calculated)
        pos += 4;

        // Chunk data
        chunkData.CopyTo(span[pos..]);

        var key = new ResourceKey(RcolConstants.Modl, 0, 0x1234);

        // Act
        var resource = new RcolResource(key, buffer);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Chunks.Should().HaveCount(1);
        resource.Chunks[0].Block.Data.Length.Should().Be(chunkData.Length);
        resource.Chunks[0].Block.Tag.Should().Be("MODL");
    }
}
