using FluentAssertions;
using TS4Tools.Compression;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for in-place save correctness and clean-resource fidelity.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs
/// - SavePackage() lines 81-86: after an in-place save the header is re-read and the
///   index is discarded (index = null) so entries are re-parsed with their new offsets.
/// - packedChunk() lines 574-583: clean (unmodified) resources are copied as their raw
///   stored bytes; only dirty resources are (re)compressed (lines 559-573).
/// </summary>
public sealed class PackageSaveFidelityTests : IDisposable
{
    private const uint TypeA = 0x220557DA;
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ts4tools-tests-" + Guid.NewGuid().ToString("N"));

    public PackageSaveFidelityTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static byte[] Compressible(int seed, int length)
    {
        var data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = (byte)('A' + ((i / 16 + seed) % 26));
        return data;
    }

    /// <summary>Creates a package file with three ZLIB-compressed resources.</summary>
    private async Task<(string Path, Dictionary<ResourceKey, byte[]> Data)> CreatePackageFileAsync()
    {
        var path = Path.Combine(_tempDir, Guid.NewGuid().ToString("N") + ".package");
        var data = new Dictionary<ResourceKey, byte[]>
        {
            [new ResourceKey(TypeA, 0, 1)] = Compressible(1, 4000),
            [new ResourceKey(TypeA, 0, 2)] = Compressible(2, 6000),
            [new ResourceKey(TypeA, 0, 3)] = Compressible(3, 8000),
        };

        using var package = DbpfPackage.CreateNew();
        foreach (var (key, bytes) in data)
            package.AddResource(key, bytes);
        await package.SaveAsAsync(path);
        return (path, data);
    }

    [Fact]
    public async Task SaveAsync_ThenReadUncachedResource_ReturnsCorrectData()
    {
        var (path, data) = await CreatePackageFileAsync();
        var key1 = new ResourceKey(TypeA, 0, 1);
        var key2 = new ResourceKey(TypeA, 0, 2);
        var key3 = new ResourceKey(TypeA, 0, 3);
        var replacement = Compressible(9, 20000); // different size => later offsets shift

        await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
        {
            package.ReplaceResource(package.Find(key1)!, replacement);
            package.DeleteResource(package.Find(key2)!);

            await package.SaveAsync();

            package.IsDirty.Should().BeFalse();
            package.ResourceCount.Should().Be(2, "deleted entries must not survive an in-place save");
            package.Find(key2).Should().BeNull();

            // key3 was never read before the save, so it must come from its NEW offset
            var read3 = await package.GetResourceDataAsync(package.Find(key3)!);
            read3.ToArray().Should().Equal(data[key3]);

            var read1 = await package.GetResourceDataAsync(package.Find(key1)!);
            read1.ToArray().Should().Equal(replacement);
        }
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTempFileBehind()
    {
        var (path, _) = await CreatePackageFileAsync();

        await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
        {
            package.ReplaceResource(package.Find(new ResourceKey(TypeA, 0, 1))!, Compressible(5, 3000));
            await package.SaveAsync();
        }

        Directory.GetFiles(_tempDir).Should().Equal(path);
    }

    [Fact]
    public async Task SaveAsync_Cancelled_LeavesOriginalIntact()
    {
        var (path, _) = await CreatePackageFileAsync();
        var before = await File.ReadAllBytesAsync(path);

        await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
        {
            package.ReplaceResource(package.Find(new ResourceKey(TypeA, 0, 1))!, Compressible(5, 3000));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var act = async () => await package.SaveAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        (await File.ReadAllBytesAsync(path)).Should().Equal(before);
        Directory.GetFiles(_tempDir).Should().Equal(path);
    }

    [Fact]
    public async Task SaveAsync_Twice_ProducesValidPackage()
    {
        var (path, data) = await CreatePackageFileAsync();
        var key1 = new ResourceKey(TypeA, 0, 1);
        var replacement = Compressible(7, 15000);

        await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
        {
            package.ReplaceResource(package.Find(key1)!, replacement);
            await package.SaveAsync();

            package.ReplaceResource(package.Find(key1)!, data[key1]);
            await package.SaveAsync();
        }

        await using var reloaded = await DbpfPackage.OpenAsync(path);
        reloaded.ResourceCount.Should().Be(data.Count);
        foreach (var (key, expected) in data)
        {
            var actual = await reloaded.GetResourceDataAsync(reloaded.Find(key)!);
            actual.ToArray().Should().Equal(expected, $"resource {key} must survive two saves");
        }
    }

    /// <summary>
    /// Builds a raw DBPF 2.1 package with full (type 0) index entries.
    /// </summary>
    private static byte[] BuildRawPackage(IReadOnlyList<(ResourceKey Key, byte[] Stored, uint MemSize, ushort Compression)> entries)
    {
        using var ms = new MemoryStream();
        var header = new byte[PackageLimits.HeaderSize];
        ms.Write(header);

        var offsets = new List<uint>();
        foreach (var e in entries)
        {
            offsets.Add((uint)ms.Position);
            ms.Write(e.Stored);
        }

        long indexPos = ms.Position;
        var buf = new byte[4];
        void W32(uint v) { BinaryPrimitives.WriteUInt32LittleEndian(buf, v); ms.Write(buf); }

        W32(0); // index type: nothing shared
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            W32(e.Key.ResourceType);
            W32(e.Key.ResourceGroup);
            W32((uint)(e.Key.Instance >> 32));
            W32((uint)e.Key.Instance);
            W32(offsets[i]);
            W32((uint)e.Stored.Length | 0x80000000);
            W32(e.MemSize);
            BinaryPrimitives.WriteUInt16LittleEndian(buf, e.Compression);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2), 1);
            ms.Write(buf);
        }
        int indexSize = (int)(ms.Position - indexPos);

        var bytes = ms.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, PackageLimits.Magic);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 2);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(36), entries.Count);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(44), indexSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(60), 3);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(64), (int)indexPos);
        return bytes;
    }

    [Fact]
    public async Task SaveToStreamAsync_CleanResources_PreservesStoredBytesAndCompressionType()
    {
        // RefPack chunk: type byte 0x10, marker 0xFB, 3-byte BE size (8),
        // plain-text run 0xE1 (8 literals), end marker 0xFC.
        byte[] refPackPlain = "REFPACK!"u8.ToArray();
        byte[] refPackStored = [0x10, 0xFB, 0x00, 0x00, 0x08, 0xE1, .. refPackPlain, 0xFC];

        byte[] zlibPlain = Compressible(4, 3000);
        var (zlibStored, compressed) = Compressor.Compress(zlibPlain);
        compressed.Should().BeTrue();

        byte[] rawPlain = [1, 2, 3, 4, 5];

        var keyRef = new ResourceKey(TypeA, 1, 10);
        var keyZlib = new ResourceKey(TypeA, 2, 20);
        var keyRaw = new ResourceKey(TypeA, 3, 30);

        var original = BuildRawPackage(
        [
            (keyRef, refPackStored, (uint)refPackPlain.Length, 0xFFFF),
            (keyZlib, zlibStored, (uint)zlibPlain.Length, Compressor.ZlibCompressionType),
            (keyRaw, rawPlain, (uint)rawPlain.Length, Compressor.NoCompression),
        ]);

        using var source = new MemoryStream(original);
        await using var package = await DbpfPackage.OpenAsync(source, leaveOpen: true);

        // Reading (and so caching) a resource must not make it "dirty" for saving purposes
        (await package.GetResourceDataAsync(package.Find(keyRef)!)).ToArray().Should().Equal(refPackPlain);

        using var output = new MemoryStream();
        await package.SaveToStreamAsync(output);

        output.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(output, leaveOpen: true);

        var expected = new Dictionary<ResourceKey, (byte[] Stored, ushort Compression, byte[] Plain)>
        {
            [keyRef] = (refPackStored, 0xFFFF, refPackPlain),
            [keyZlib] = (zlibStored, Compressor.ZlibCompressionType, zlibPlain),
            [keyRaw] = (rawPlain, Compressor.NoCompression, rawPlain),
        };

        var outBytes = output.ToArray();
        foreach (var (key, exp) in expected)
        {
            var entry = (ResourceIndexEntry)reloaded.Find(key)!;
            entry.CompressionType.Should().Be(exp.Compression, $"compression label of {key} must be preserved");
            entry.FileSize.Should().Be((uint)exp.Stored.Length);
            entry.MemorySize.Should().Be((uint)exp.Plain.Length);
            outBytes.AsSpan((int)entry.ChunkOffset, (int)entry.FileSize).ToArray()
                .Should().Equal(exp.Stored, $"stored bytes of clean resource {key} must be copied verbatim");
            (await reloaded.GetResourceDataAsync(entry)).ToArray().Should().Equal(exp.Plain);
        }
    }
}
