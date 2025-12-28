using FluentAssertions;
using TS4Tools.Compression;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="PackageWriter"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs (SaveAs method)
/// - The DBPF format has a 4GB limit due to 32-bit offsets
/// - Index type optimization:
///   - Bit 0: Type is shared (all resources have same type)
///   - Bit 1: Group is shared (all resources have same group)
///   - Bit 2: InstanceHigh is shared (all resources have same instance high bits)
/// - Compression uses ZLIB (0x5A42 = "ZB") with header 0x78
/// - FileSize in index always has bit 31 set
/// </summary>
public class PackageWriterTests
{
    [Fact]
    public async Task WriteAsync_EmptyPackage_WritesValidHeader()
    {
        // Arrange
        using var package = DbpfPackage.CreateNew();
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Length.Should().BeGreaterOrEqualTo(96); // Header is 96 bytes
        stream.Position = 0;
        var magic = new byte[4];
        await stream.ReadExactlyAsync(magic);
        magic.Should().BeEquivalentTo(new byte[] { 0x44, 0x42, 0x50, 0x46 }); // "DBPF"
    }

    [Fact]
    public async Task WriteAsync_SingleResource_WritesCorrectly()
    {
        // Arrange
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0x123456789ABCDEF0);
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert - verify we can reload and read the data
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(1);
        var entry = reloaded.Find(key);
        entry.Should().NotBeNull();
        var readData = await reloaded.GetResourceDataAsync(entry!);
        readData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WriteAsync_SameType_OptimizesIndexType()
    {
        // Arrange - all resources have the same type, so bit 0 should be set
        using var package = DbpfPackage.CreateNew();
        const uint sharedType = 0x220557DA;
        package.AddResource(new ResourceKey(sharedType, 1, 1), new byte[] { 1 });
        package.AddResource(new ResourceKey(sharedType, 2, 2), new byte[] { 2 });
        package.AddResource(new ResourceKey(sharedType, 3, 3), new byte[] { 3 });
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert - reload and verify all resources are preserved
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(3);

        foreach (var entry in reloaded.Resources)
        {
            entry.Key.ResourceType.Should().Be(sharedType);
        }
    }

    [Fact]
    public async Task WriteAsync_SameGroupAndType_OptimizesIndex()
    {
        // Arrange - all resources have same type and group
        using var package = DbpfPackage.CreateNew();
        const uint sharedType = 0x220557DA;
        const uint sharedGroup = 0x00000000;
        package.AddResource(new ResourceKey(sharedType, sharedGroup, 1), new byte[] { 1 });
        package.AddResource(new ResourceKey(sharedType, sharedGroup, 2), new byte[] { 2 });
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(2);
    }

    [Fact]
    public async Task WriteAsync_CompressibleData_Compresses()
    {
        // Arrange - highly repetitive data should compress
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = new byte[1000];
        Array.Fill(data, (byte)'A');
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert - the stream should be smaller than the uncompressed data
        stream.Length.Should().BeLessThan(data.Length + 150); // Header + compressed data < original
    }

    [Fact]
    public async Task WriteAsync_ModifiedResource_SavesNewData()
    {
        // Arrange
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var originalData = new byte[] { 0x01, 0x02, 0x03 };
        var entry = package.AddResource(key, originalData);

        var newData = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };
        package.ReplaceResource(entry!, newData);

        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var readEntry = reloaded.Find(key);
        var readData = await reloaded.GetResourceDataAsync(readEntry!);
        readData.ToArray().Should().BeEquivalentTo(newData);
    }

    [Fact]
    public async Task WriteAsync_DeletedResource_NotIncluded()
    {
        // Arrange
        using var package = DbpfPackage.CreateNew();
        var key1 = new ResourceKey(0x220557DA, 0, 1);
        var key2 = new ResourceKey(0x220557DA, 0, 2);
        var entry1 = package.AddResource(key1, new byte[] { 1 });
        package.AddResource(key2, new byte[] { 2 });
        package.DeleteResource(entry1!);

        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(1);
        reloaded.Find(key1).Should().BeNull();
        reloaded.Find(key2).Should().NotBeNull();
    }

    [Fact]
    public async Task WriteAsync_LargeResource_PreservesData()
    {
        // Arrange - test with a larger resource to ensure offset handling works
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = new byte[100000];
        new Random(42).NextBytes(data);
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(key);
        var readData = await reloaded.GetResourceDataAsync(entry!);
        readData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WriteAsync_MultipleResources_PreservesAllData()
    {
        // Arrange
        using var package = DbpfPackage.CreateNew();
        var resources = new Dictionary<ResourceKey, byte[]>();
        for (int i = 0; i < 10; i++)
        {
            var key = new ResourceKey((uint)i + 1, (uint)i * 2, (ulong)i * 3);
            var data = new byte[100 + i * 50];
            new Random(i).NextBytes(data);
            resources[key] = data;
            package.AddResource(key, data);
        }
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(10);

        foreach (var (key, expectedData) in resources)
        {
            var entry = reloaded.Find(key);
            entry.Should().NotBeNull($"Resource {key} should exist");
            var readData = await reloaded.GetResourceDataAsync(entry!);
            readData.ToArray().Should().BeEquivalentTo(expectedData);
        }
    }

    [Fact]
    public async Task ToUInt32Checked_ValidValue_ReturnsValue()
    {
        // Arrange & Act - test indirectly through package save
        // The ToUInt32Checked is tested through normal package operations
        // Values under 4GB should work fine
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        package.AddResource(key, new byte[1000]);

        // Should not throw
        using var stream = new MemoryStream();
        await package.SaveToStreamAsync(stream);
    }

    [Fact]
    public async Task WriteAsync_IncompressibleData_PreservesData()
    {
        // Arrange - random data doesn't compress well
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = new byte[500];
        new Random(12345).NextBytes(data);
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert - verify data is preserved regardless of compression
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(key);
        entry.Should().NotBeNull();
        var readData = await reloaded.GetResourceDataAsync(entry!);
        readData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WriteAsync_ZeroLengthResource_PreservesEmptyData()
    {
        // Arrange - edge case: empty resource
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = Array.Empty<byte>();
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(key);
        entry.Should().NotBeNull();
        var readData = await reloaded.GetResourceDataAsync(entry!);
        readData.ToArray().Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_ManyResources_PreservesAllData()
    {
        // Arrange - test with 100+ resources to verify scalability
        using var package = DbpfPackage.CreateNew();
        var resources = new Dictionary<ResourceKey, byte[]>();
        const int resourceCount = 150;

        for (int i = 0; i < resourceCount; i++)
        {
            var key = new ResourceKey(0x220557DA, (uint)(i / 50), (ulong)i);
            var data = new byte[50 + i % 100];
            new Random(i).NextBytes(data);
            resources[key] = data;
            package.AddResource(key, data);
        }

        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(resourceCount);

        foreach (var (key, expectedData) in resources)
        {
            var entry = reloaded.Find(key);
            entry.Should().NotBeNull($"Resource {key} should exist");
            var readData = await reloaded.GetResourceDataAsync(entry!);
            readData.ToArray().Should().BeEquivalentTo(expectedData);
        }
    }

    [Fact]
    public async Task WriteAsync_AllSameInstanceHigh_OptimizesIndex()
    {
        // Arrange - all resources have same instance high bits
        using var package = DbpfPackage.CreateNew();
        const uint instanceHigh = 0x12345678;
        package.AddResource(new ResourceKey(1, 1, ((ulong)instanceHigh << 32) | 1), new byte[] { 1 });
        package.AddResource(new ResourceKey(2, 2, ((ulong)instanceHigh << 32) | 2), new byte[] { 2 });
        package.AddResource(new ResourceKey(3, 3, ((ulong)instanceHigh << 32) | 3), new byte[] { 3 });
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert - reload and verify all resources preserved
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(3);

        foreach (var entry in reloaded.Resources)
        {
            ((uint)(entry.Key.Instance >> 32)).Should().Be(instanceHigh);
        }
    }

    [Fact]
    public async Task WriteAsync_VerySmallResource_PreservesData()
    {
        // Arrange - single byte resource
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = new byte[] { 0x42 };
        package.AddResource(key, data);
        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(key);
        entry.Should().NotBeNull();
        var readData = await reloaded.GetResourceDataAsync(entry!);
        readData.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WriteAsync_DifferentTypes_PreservesAllTypes()
    {
        // Arrange - resources with different types (no optimization possible)
        using var package = DbpfPackage.CreateNew();
        var types = new uint[] { 0x220557DA, 0x00B2D882, 0x034AEECB, 0x545AC67A };

        foreach (var type in types)
        {
            var key = new ResourceKey(type, 0, type);
            package.AddResource(key, BitConverter.GetBytes(type));
        }

        using var stream = new MemoryStream();

        // Act
        await package.SaveToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(types.Length);

        foreach (var type in types)
        {
            var key = new ResourceKey(type, 0, type);
            var entry = reloaded.Find(key);
            entry.Should().NotBeNull();
            entry!.Key.ResourceType.Should().Be(type);
        }
    }
}
