using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="DbpfPackage"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs
/// - DBPF (Database Packed File) format specification:
///   - Magic: "DBPF" (0x46504244 little-endian)
///   - Major Version: 2 (uint32)
///   - Minor Version: 1 (uint32)
///   - Header size: 96 bytes
///   - Index position: stored at offset 64 (uint32)
///   - Index count: at offset 36 (int32)
///   - Index size: at offset 44 (int32)
/// - The 4GB limit is due to 32-bit offsets in the index
/// - Resource index entry size varies based on index type flags:
///   - Full entry: 32 bytes (Type, Group, InstanceHigh, InstanceLow, ChunkOffset, FileSize, MemSize, Compressed, Unknown2)
///   - Optimized: shared Type/Group/InstanceHigh in header
/// - FileSize bit 31 is always set (mask with 0x7FFFFFFF)
/// </summary>
public class DbpfPackageTests
{
    [Fact]
    public void CreateNew_CreatesEmptyPackage()
    {
        using var package = DbpfPackage.CreateNew();

        package.ResourceCount.Should().Be(0);
        package.IsDirty.Should().BeFalse();
        package.IsReadOnly.Should().BeFalse();
        package.MajorVersion.Should().Be(2);
        package.MinorVersion.Should().Be(1);
    }

    [Fact]
    public void AddResource_IncreasesCount()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x00B2D882, 0, 0x00000000001234FF);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var entry = package.AddResource(key, data);

        entry.Should().NotBeNull();
        package.ResourceCount.Should().Be(1);
        package.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddResource_DuplicateKey_ReturnsNull()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var data = new byte[] { 1, 2, 3 };

        package.AddResource(key, data);
        var duplicate = package.AddResource(key, data);

        duplicate.Should().BeNull();
        package.ResourceCount.Should().Be(1);
    }

    [Fact]
    public void AddResource_DuplicateKeyAllowed_AddsEntry()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var data = new byte[] { 1, 2, 3 };

        package.AddResource(key, data);
        var second = package.AddResource(key, data, rejectDuplicates: false);

        second.Should().NotBeNull();
        package.ResourceCount.Should().Be(2);
    }

    [Fact]
    public void Find_ExistingKey_ReturnsEntry()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        package.AddResource(key, new byte[] { 1, 2, 3 });

        var found = package.Find(key);

        found.Should().NotBeNull();
        found!.Key.Should().Be(key);
    }

    [Fact]
    public void Find_NonExistingKey_ReturnsNull()
    {
        using var package = DbpfPackage.CreateNew();

        var found = package.Find(new ResourceKey(1, 2, 3));

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetResourceDataAsync_ReturnsData()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var entry = package.AddResource(key, data);

        var retrieved = await package.GetResourceDataAsync(entry!);

        retrieved.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public void DeleteResource_MarksAsDeleted()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var entry = package.AddResource(key, new byte[] { 1, 2, 3 });

        package.DeleteResource(entry!);
        var found = package.Find(key);

        found.Should().BeNull(); // Deleted resources are not found
    }

    [Fact]
    public void ReplaceResource_UpdatesData()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var entry = package.AddResource(key, new byte[] { 1, 2, 3 });
        var newData = new byte[] { 4, 5, 6, 7 };

        package.ReplaceResource(entry!, newData);

        entry!.MemorySize.Should().Be(4);
    }

    [Fact]
    public async Task SaveAndReload_PreservesData()
    {
        var key = new ResourceKey(0x00B2D882, 0x00000001, 0x00000000FFFFFFFF);
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        using var stream = new MemoryStream();

        // Create and save
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(key, data);
            await package.SaveToStreamAsync(stream);
        }

        // Reload
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        reloaded.ResourceCount.Should().Be(1);
        var entry = reloaded.Find(key);
        entry.Should().NotBeNull();

        var retrievedData = await reloaded.GetResourceDataAsync(entry!);
        retrievedData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task SaveAndReload_WithMultipleResources_PreservesAll()
    {
        var resources = new Dictionary<ResourceKey, byte[]>
        {
            { new ResourceKey(1, 0, 100), new byte[] { 1, 2, 3 } },
            { new ResourceKey(1, 0, 200), new byte[] { 4, 5, 6, 7 } },
            { new ResourceKey(2, 1, 100), new byte[] { 8, 9 } },
            { new ResourceKey(3, 2, 300), GenerateTestData(1000) }, // Larger to test compression
        };

        using var stream = new MemoryStream();

        // Create and save
        using (var package = DbpfPackage.CreateNew())
        {
            foreach (var (key, data) in resources)
            {
                package.AddResource(key, data);
            }
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        reloaded.ResourceCount.Should().Be(resources.Count);

        foreach (var (key, expectedData) in resources)
        {
            var entry = reloaded.Find(key);
            entry.Should().NotBeNull($"Resource {key} should exist");

            var actualData = await reloaded.GetResourceDataAsync(entry!);
            actualData.ToArray().Should().BeEquivalentTo(expectedData);
        }
    }

    [Fact]
    public async Task OpenAsync_InvalidMagic_ThrowsException()
    {
        var badData = new byte[96];
        Array.Fill(badData, (byte)0xFF);

        using var stream = new MemoryStream(badData);

        var act = async () => await DbpfPackage.OpenAsync(stream);

        await act.Should().ThrowAsync<PackageFormatException>()
            .WithMessage("*magic*");
    }

    [Fact]
    public async Task OpenAsync_TruncatedHeader_ThrowsException()
    {
        var shortData = new byte[50]; // Less than 96-byte header

        using var stream = new MemoryStream(shortData);

        var act = async () => await DbpfPackage.OpenAsync(stream);

        await act.Should().ThrowAsync<PackageFormatException>()
            .WithMessage("*end of file*header*");
    }

    [Fact]
    public async Task OpenAsync_OversizedIndexSize_ThrowsPackageFormatException()
    {
        // Create a package header with an invalid index size (> MaxResourceSize)
        var header = CreateHeaderWithIndexSize(PackageLimits.MaxResourceSize + 1, indexCount: 1);
        using var stream = new MemoryStream(header);

        var act = async () => await DbpfPackage.OpenAsync(stream);

        await act.Should().ThrowAsync<PackageFormatException>()
            .WithMessage("*index size*");
    }

    [Fact]
    public async Task OpenAsync_IndexSizeExceedsFileLength_ThrowsPackageFormatException()
    {
        // Create a package header where index size claims more bytes than file contains
        // Header is 96 bytes, index position is at 96, but we claim 1000 bytes
        var header = CreateHeaderWithIndexSize(1000, indexCount: 1);
        using var stream = new MemoryStream(header);

        var act = async () => await DbpfPackage.OpenAsync(stream);

        await act.Should().ThrowAsync<PackageFormatException>()
            .WithMessage("*exceeds*");
    }

    private static byte[] GenerateTestData(int size)
    {
        var data = new byte[size];
        // Fill with repeating pattern (good for compression)
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)(i % 256);
        }
        return data;
    }

    /// <summary>
    /// Creates a valid DBPF header with a specified index size for testing validation.
    /// </summary>
    private static byte[] CreateHeaderWithIndexSize(int indexSize, int indexCount)
    {
        var header = new byte[PackageLimits.HeaderSize];

        // DBPF magic
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0), PackageLimits.Magic);

        // Version 2.1
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), 1);

        // Index count at offset 36
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(36), indexCount);

        // Index size at offset 44
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(44), indexSize);

        // Index type flags at offset 60
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(60), 3);

        // Index position at offset 64 (right after header)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(64), PackageLimits.HeaderSize);

        return header;
    }
}
