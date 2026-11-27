using FluentAssertions;
using TS4Tools.Compression;
using TS4Tools.Package;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Package behaviour that must match legacy s4pi.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs
///   - New package header: setIndexsize(new PackageIndex().Size) = 4, setIndexversion = 3 at 60,
///     index position 0 at 40 and 96 at 64 (lines 527-545, 601-605)
///   - AddResource accepts a null stream (lines 464-471); new entries have Compressed = 0 (PackageIndex.cs lines 74-87)
///   - packedChunk only compresses a dirty resource if Compressed != 0 (line 570); SaveAs labels the result
///     0x5A42 if smaller, else 0 (lines 127-138)
///   - SaveAs(Stream) does not reset any state (lines 96-147); OpenPackage uses FileShare.ReadWrite (line 239)
///   - Header fields Magic/Unused1/Unused2/IndexRecordPositionLow/Unused3/Unused4/Unused5 (lines 263-338)
/// - Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs — every setter raises OnElementChanged
/// - Source: legacy_references/Sims4Tools/s4pi/Package/PackageIndex.cs — Sort(string) line 123, TGI indexer lines 132-143
/// </summary>
public class PackageCorrectnessTests
{
    private static async Task<MemoryStream> SavedPackageAsync(params (ResourceKey Key, byte[] Data, ushort Compression)[] resources)
    {
        using var package = DbpfPackage.CreateNew();
        foreach (var (key, data, compression) in resources)
        {
            var entry = package.AddResource(key, data)!;
            package.SetCompression(entry, compression);
        }
        var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        return ms;
    }

    private static byte[] Compressible(int size) => Enumerable.Range(0, size).Select(i => (byte)(i % 7)).ToArray();

    #region Short reads (item 6)

    [Fact]
    public async Task Open_StreamReturningShortReads_LoadsCorrectly()
    {
        var data = Compressible(5000);
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, data, Compressor.ZlibCompressionType));

        await using var package = await DbpfPackage.OpenAsync(new TrickleStream(saved.ToArray(), maxChunk: 7));

        package.ResourceCount.Should().Be(1);
        (await package.GetResourceDataAsync(package.Find(key)!)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task Open_TruncatedHeader_ThrowsPackageFormatException()
    {
        using var saved = await SavedPackageAsync();
        var truncated = saved.ToArray()[..50];

        var act = async () => await DbpfPackage.OpenAsync(new MemoryStream(truncated));

        (await act.Should().ThrowAsync<PackageFormatException>()).WithInnerException<EndOfStreamException>();
    }

    #endregion

    #region Compression semantics (item 7)

    [Fact]
    public async Task AddResource_DefaultsToUncompressed_LikeLegacy()
    {
        var data = Compressible(4000);
        var key = new ResourceKey(1, 2, 3);
        using var package = DbpfPackage.CreateNew();
        package.AddResource(key, data)!.CompressionType.Should().Be(0);
        using var ms = new MemoryStream();

        await package.SaveToStreamAsync(ms);

        ms.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(ms, leaveOpen: true);
        var entry = reloaded.Find(key)!;
        entry.CompressionType.Should().Be(0);
        entry.FileSize.Should().Be((uint)data.Length);
        entry.IsCompressed.Should().BeFalse();
    }

    [Fact]
    public async Task SetCompression_NonZero_CompressesAndLabelsZlib()
    {
        var data = Compressible(4000);
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, data, (ushort)0xFFFF));

        await using var reloaded = await DbpfPackage.OpenAsync(saved);
        var entry = reloaded.Find(key)!;

        entry.CompressionType.Should().Be(Compressor.ZlibCompressionType);
        entry.FileSize.Should().BeLessThan((uint)data.Length);
        (await reloaded.GetResourceDataAsync(entry)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task SetCompression_IncompressibleData_StoredUncompressedWithZeroLabel()
    {
        var data = new byte[300];
        new Random(5).NextBytes(data);
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, data, Compressor.ZlibCompressionType));

        await using var reloaded = await DbpfPackage.OpenAsync(saved);

        reloaded.Find(key)!.CompressionType.Should().Be(0);
        reloaded.Find(key)!.FileSize.Should().Be(300);
    }

    [Fact]
    public async Task ChangingCompression_OnCleanEntry_ReencodesOnSave()
    {
        var data = Compressible(4000);
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, data, Compressor.ZlibCompressionType));
        await using var package = await DbpfPackage.OpenAsync(saved);
        var entry = (IMutableResourceIndexEntry)package.Find(key)!;
        entry.IsCompressed.Should().BeTrue();
        package.IsDirty.Should().BeFalse();

        entry.CompressionType = 0;

        package.IsDirty.Should().BeTrue();
        using var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(ms);
        var re = reloaded.Find(key)!;
        re.IsCompressed.Should().BeFalse();
        re.CompressionType.Should().Be(0);
        (await reloaded.GetResourceDataAsync(re)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task ChangingCompression_OnCleanUncompressedEntry_CompressesOnSave()
    {
        var data = Compressible(4000);
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, data, (ushort)0));
        await using var package = await DbpfPackage.OpenAsync(saved);

        package.SetCompression(package.Find(key)!, Compressor.ZlibCompressionType);
        using var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);

        ms.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(ms);
        reloaded.Find(key)!.IsCompressed.Should().BeTrue();
        (await reloaded.GetResourceDataAsync(reloaded.Find(key)!)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task ReplaceResource_KeepsEntryCompressionFlag()
    {
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, Compressible(4000), Compressor.ZlibCompressionType));
        await using var package = await DbpfPackage.OpenAsync(saved);
        var replacement = Compressible(8000);

        package.ReplaceResource(package.Find(key)!, replacement);
        using var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);

        ms.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(ms);
        reloaded.Find(key)!.IsCompressed.Should().BeTrue();
        (await reloaded.GetResourceDataAsync(reloaded.Find(key)!)).ToArray().Should().Equal(replacement);
    }

    #endregion

    #region Header (items 8, 9)

    [Fact]
    public void CreateNew_HeaderMatchesLegacyNewPackage()
    {
        using var package = DbpfPackage.CreateNew();

        package.Magic.ToArray().Should().Equal((byte)'D', (byte)'B', (byte)'P', (byte)'F');
        package.MajorVersion.Should().Be(2);
        package.MinorVersion.Should().Be(1);
        package.HeaderIndexSize.Should().Be(4, "legacy writes new PackageIndex().Size = 4");
        package.Unused4.Should().Be(3);
        package.IndexRecordPositionLow.Should().Be(0);
        package.HeaderIndexPosition.Should().Be(96);
        package.Unused1.Should().Be(0);
        package.Unused2.Should().Be(0);
        package.Unused3.Should().Be(0);
        package.Unused5.Length.Should().Be(28);
        package.IndexType.Should().Be(0);
    }

    [Fact]
    public void CreateNew_UnsupportedMajor_Throws()
    {
        var act = () => DbpfPackage.CreateNew(3);

        act.Should().Throw<PackageFormatException>().WithMessage("*major*");
        DbpfPackage.CreateNew(2).MajorVersion.Should().Be(2);
    }

    [Fact]
    public async Task Open_ExposesHeaderFieldsAndIndexType()
    {
        var bytes = (await SavedPackageAsync((new ResourceKey(5, 6, 7), new byte[] { 1, 2 }, (ushort)0))).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 0x11);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x20), 0x22);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(48), 0x33);
        bytes[68] = 0x44;

        await using var package = await DbpfPackage.OpenAsync(new MemoryStream(bytes));

        package.Unused1.Should().Be(0x11);
        package.Unused2.Should().Be(0x22);
        package.Unused3.Should().Be(0x33);
        package.Unused5.Span[0].Should().Be(0x44);
        package.IndexType.Should().Be(0x07, "a single resource shares type, group and instance-high");
    }

    [Fact]
    public async Task SaveEmptyPackage_RoundTrips()
    {
        using var saved = await SavedPackageAsync();

        await using var package = await DbpfPackage.OpenAsync(saved);

        package.ResourceCount.Should().Be(0);
    }

    #endregion

    #region Dirty tracking (item 10)

    [Fact]
    public async Task SettingKeyOrIsDeleted_MarksPackageDirty()
    {
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, new byte[] { 1, 2, 3 }, (ushort)0));
        await using var package = await DbpfPackage.OpenAsync(saved);
        var entry = (IMutableResourceIndexEntry)package.Find(key)!;
        package.IsDirty.Should().BeFalse();

        entry.Key = key;
        package.IsDirty.Should().BeFalse("setting the same value is not a change");

        entry.Key = new ResourceKey(9, 9, 9);
        package.IsDirty.Should().BeTrue();

        await using var package2 = await DbpfPackage.OpenAsync(new MemoryStream(saved.ToArray()));
        ((IMutableResourceIndexEntry)package2.Find(key)!).IsDeleted = true;
        package2.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task SaveToStreamAsync_DoesNotClearDirty()
    {
        using var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(1, 2, 3), new byte[] { 1 });
        using var ms = new MemoryStream();

        await package.SaveToStreamAsync(ms);

        package.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsAsync_DoesNotClearDirty_SaveAsyncDoes()
    {
        var dir = Directory.CreateTempSubdirectory("ts4tools-dirty-");
        try
        {
            var original = Path.Combine(dir.FullName, "a.package");
            await File.WriteAllBytesAsync(original, (await SavedPackageAsync((new ResourceKey(1, 2, 3), new byte[] { 1 }, (ushort)0))).ToArray());

            await using var package = await DbpfPackage.OpenAsync(original, readWrite: true);
            package.AddResource(new ResourceKey(4, 5, 6), new byte[] { 2 });

            await package.SaveAsAsync(Path.Combine(dir.FullName, "b.package"));
            package.IsDirty.Should().BeTrue();

            await package.SaveAsync();
            package.IsDirty.Should().BeFalse();
            package.ResourceCount.Should().Be(2);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    #endregion

    #region Memory, concurrency, sharing (item 11)

    [Fact]
    public async Task GetResourceDataAsync_DoesNotCacheCleanReads()
    {
        var key = new ResourceKey(1, 2, 3);
        using var saved = await SavedPackageAsync((key, Compressible(1000), Compressor.ZlibCompressionType));
        await using var package = await DbpfPackage.OpenAsync(saved);
        var entry = (ResourceIndexEntry)package.Find(key)!;

        await package.GetResourceDataAsync(entry);

        entry.ResourceData.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentReads_ReturnCorrectData()
    {
        var resources = Enumerable.Range(0, 40)
            .Select(i => (Key: new ResourceKey((uint)i + 1, 0, (ulong)i), Data: Enumerable.Repeat((byte)i, 2000 + i * 13).ToArray(),
                Compression: i % 2 == 0 ? Compressor.ZlibCompressionType : (ushort)0))
            .ToArray();
        using var saved = await SavedPackageAsync(resources);
        await using var package = await DbpfPackage.OpenAsync(saved);

        var tasks = Enumerable.Range(0, 400).Select(n => Task.Run(async () =>
        {
            var r = resources[n % resources.Length];
            var data = await package.GetResourceDataAsync(package.Find(r.Key)!);
            return data.Span.SequenceEqual(r.Data);
        }));

        (await Task.WhenAll(tasks)).Should().AllSatisfy(ok => ok.Should().BeTrue());
    }

    [Fact]
    public async Task OpenReadWrite_AllowsOtherReaders()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-share-{Guid.NewGuid():N}.package");
        try
        {
            await File.WriteAllBytesAsync(path, (await SavedPackageAsync((new ResourceKey(1, 2, 3), new byte[] { 1 }, (ushort)0))).ToArray());

            await using var writer = await DbpfPackage.OpenAsync(path, readWrite: true);
            await using var reader = await DbpfPackage.OpenAsync(path);

            reader.ResourceCount.Should().Be(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Add/replace overloads (item 12)

    [Fact]
    public async Task AddResource_KeyOnly_CreatesEmptyEntry()
    {
        var key = new ResourceKey(1, 2, 3);
        using var package = DbpfPackage.CreateNew();

        var entry = package.AddResource(key);

        entry.Should().NotBeNull();
        entry!.MemorySize.Should().Be(0);
        package.AddResource(key).Should().BeNull("duplicates are rejected by default");
        (await package.GetResourceDataAsync(entry)).Length.Should().Be(0);

        using var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(ms);
        reloaded.Find(key)!.MemorySize.Should().Be(0);
    }

    [Fact]
    public async Task AddAndReplaceResource_WithIResource_UseItsData()
    {
        var key = new ResourceKey(1, 2, 3);
        using var package = DbpfPackage.CreateNew();
#pragma warning disable CA1859 // deliberately exercise the interface overloads
        IMutablePackage mutable = package;
#pragma warning restore CA1859

        var entry = mutable.AddResource(key, new DefaultResource(key, new byte[] { 1, 2 }))!;
        (await package.GetResourceDataAsync(entry)).ToArray().Should().Equal(1, 2);

        mutable.ReplaceResource(entry, new DefaultResource(key, new byte[] { 3, 4, 5 }));
        (await package.GetResourceDataAsync(entry)).ToArray().Should().Equal(3, 4, 5);
        entry.MemorySize.Should().Be(3);
    }

    #endregion

    #region Index helpers (item 13)

    [Fact]
    public void FindByTgi_MatchesKey_SkipsDeleted()
    {
        using var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(new ResourceKey(1, 2, 3), new byte[] { 1 })!;

        package.Find(1, 2, 3).Should().BeSameAs(entry);
        package.Find(1, 2, 4).Should().BeNull();

        package.DeleteResource(entry);
        package.Find(1, 2, 3).Should().BeNull();
    }

    [Theory]
    [InlineData("ResourceType")]
    [InlineData("instance")]
    [InlineData("Memsize")]
    [InlineData("Key")]
    public void SortResources_SortsByNamedField(string field)
    {
        using var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(3, 0, 30), new byte[3]);
        package.AddResource(new ResourceKey(1, 0, 10), new byte[1]);
        package.AddResource(new ResourceKey(2, 0, 20), new byte[2]);

        package.SortResources(field);

        package.Resources.Select(r => r.Key.ResourceType).Should().Equal(1u, 2u, 3u);
    }

    [Fact]
    public void SortResources_UnknownField_Throws()
    {
        using var package = DbpfPackage.CreateNew();

        var act = () => package.SortResources("Bogus");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    /// <summary>A seekable stream that returns at most <c>maxChunk</c> bytes per read.</summary>
    private sealed class TrickleStream(byte[] data, int maxChunk) : MemoryStream(data, writable: false)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, maxChunk));
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, maxChunk)]);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, maxChunk)], cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            base.ReadAsync(buffer, offset, Math.Min(count, maxChunk), cancellationToken);
    }
}
