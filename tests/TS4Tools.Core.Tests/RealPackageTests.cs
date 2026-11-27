using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Integration tests that run against real Sims 4 package files.
/// These tests are skipped if the game is not installed.
/// </summary>
public class RealPackageTests
{
    private const string GamePath = "/mnt/ai/SteamLibrary/steamapps/common/The Sims 4";
    private const string DeltaPath = $"{GamePath}/Delta";

    // Large third-party mod (~140 MB) used for full open -> save -> reopen round-trips.
    private const string ModPackagePath = "/mnt/ai/sims_mods/test-mod.package";

    private static bool GameInstalled => Directory.Exists(GamePath);
    private static bool DeltaExists => Directory.Exists(DeltaPath);

    [SkippableFact]
    public async Task OpenAsync_DeltaPackage_ReadsSuccessfully()
    {
        Skip.IfNot(DeltaExists, "Sims 4 Delta directory not found");

        var packageFiles = Directory.GetFiles(DeltaPath, "*.package", SearchOption.AllDirectories)
            .Take(1)
            .ToList();

        Skip.If(packageFiles.Count == 0, "No package files found in Delta directory");

        var path = packageFiles[0];
        await using var package = await DbpfPackage.OpenAsync(path);

        package.MajorVersion.Should().Be(2);
        package.MinorVersion.Should().Be(1);
        package.ResourceCount.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task OpenAsync_MultiplePackages_AllReadSuccessfully()
    {
        Skip.IfNot(DeltaExists, "Sims 4 Delta directory not found");

        var packageFiles = Directory.GetFiles(DeltaPath, "*.package", SearchOption.AllDirectories)
            .Take(5) // Test first 5 packages
            .ToList();

        Skip.If(packageFiles.Count == 0, "No package files found");

        foreach (var path in packageFiles)
        {
            await using var package = await DbpfPackage.OpenAsync(path);

            package.MajorVersion.Should().Be(2);
            package.ResourceCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [SkippableFact]
    public async Task GetResourceDataAsync_RealResource_DecompressesSuccessfully()
    {
        Skip.IfNot(DeltaExists, "Sims 4 Delta directory not found");

        var packageFile = Directory.GetFiles(DeltaPath, "*.package", SearchOption.AllDirectories)
            .FirstOrDefault();

        Skip.If(packageFile == null, "No package files found");

        await using var package = await DbpfPackage.OpenAsync(packageFile);

        Skip.If(package.ResourceCount == 0, "Package has no resources");

        // Try to read the first resource
        var entry = package.Resources[0];
        var data = await package.GetResourceDataAsync(entry);

        // Data should be decompressed to MemorySize
        data.Length.Should().Be((int)entry.MemorySize);
    }

    [SkippableFact]
    public async Task OpenAsync_AllDeltaPackages_ListsResourceCounts()
    {
        Skip.IfNot(DeltaExists, "Sims 4 Delta directory not found");

        var packageFiles = Directory.GetFiles(DeltaPath, "*.package", SearchOption.AllDirectories);

        Skip.If(packageFiles.Length == 0, "No package files found");

        var results = new List<(string Name, int Count)>();

        foreach (var path in packageFiles)
        {
            try
            {
                await using var package = await DbpfPackage.OpenAsync(path);
                results.Add((Path.GetFileName(path), package.ResourceCount));
            }
            catch (PackageFormatException ex)
            {
                throw new InvalidOperationException($"Failed to open {path}: {ex.Message}", ex);
            }
        }

        // At least some packages should have resources
        results.Should().Contain(r => r.Count > 0);
    }

    [SkippableFact]
    public async Task RoundTrip_RealPackage_PreservesData()
    {
        Skip.IfNot(DeltaExists, "Sims 4 Delta directory not found");

        var packageFile = Directory.GetFiles(DeltaPath, "*.package", SearchOption.AllDirectories)
            .FirstOrDefault();

        Skip.If(packageFile == null, "No package files found");

        // Read original
        Dictionary<ResourceKey, byte[]> originalData = [];
        await using (var original = await DbpfPackage.OpenAsync(packageFile))
        {
            Skip.If(original.ResourceCount == 0, "Package has no resources");

            // Store first 10 resources for comparison
            foreach (var entry in original.Resources.Take(10))
            {
                var data = await original.GetResourceDataAsync(entry);
                originalData[entry.Key] = data.ToArray();
            }
        }

        // Save to memory stream
        using var stream = new MemoryStream();
        await using (var original = await DbpfPackage.OpenAsync(packageFile))
        {
            // Create new package with same resources
            using var newPackage = DbpfPackage.CreateNew();
            foreach (var entry in original.Resources.Take(10))
            {
                var data = await original.GetResourceDataAsync(entry);
                newPackage.AddResource(entry.Key, data.ToArray());
            }
            await newPackage.SaveToStreamAsync(stream);
        }

        // Read back and compare
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        reloaded.ResourceCount.Should().Be(originalData.Count);

        foreach (var (key, expectedData) in originalData)
        {
            var entry = reloaded.Find(key);
            entry.Should().NotBeNull($"Resource {key} should exist after round-trip");

            var actualData = await reloaded.GetResourceDataAsync(entry!);
            actualData.ToArray().Should().BeEquivalentTo(expectedData,
                $"Resource {key} data should match after round-trip");
        }
    }

    private sealed record EntrySnapshot(ResourceKey Key, uint FileSize, uint MemorySize, ushort Compression, byte[] Hash);

    private static async Task<List<EntrySnapshot>> SnapshotAsync(DbpfPackage package)
    {
        var result = new List<EntrySnapshot>(package.ResourceCount);
        foreach (var e in package.Resources.Cast<ResourceIndexEntry>())
        {
            var data = await package.GetResourceDataAsync(e);
            result.Add(new EntrySnapshot(e.Key, e.FileSize, e.MemorySize, e.CompressionType, SHA256.HashData(data.Span)));
        }
        return result;
    }

    /// <summary>
    /// Opens a real mod package read-write, saves it in place unchanged, then modifies one
    /// resource and saves again in the same session. Every resource must survive intact.
    /// Works on a temp copy; the original file is never opened for writing.
    /// </summary>
    [SkippableFact]
    public async Task SaveAsync_RealModPackage_InPlaceRoundTripPreservesEveryResource()
    {
        Skip.IfNot(File.Exists(ModPackagePath), "Test mod package not found");

        var tempDir = Path.Combine(Path.GetTempPath(), "ts4tools-real-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var copyPath = Path.Combine(tempDir, Path.GetFileName(ModPackagePath));
        try
        {
            File.Copy(ModPackagePath, copyPath);

            List<EntrySnapshot> original;
            await using (var ro = await DbpfPackage.OpenAsync(ModPackagePath))
                original = await SnapshotAsync(ro);
            original.Should().NotBeEmpty();

            var modifiedIndex = original.FindIndex(s => s.MemorySize > 0);
            byte[] replacement;

            await using (var package = await DbpfPackage.OpenAsync(copyPath, readWrite: true))
            {
                // 1. Unchanged in-place save: stored chunks must be copied verbatim
                await package.SaveAsync();
                new FileInfo(copyPath).Length.Should().Be(new FileInfo(ModPackagePath).Length,
                    "an unmodified save must not change the package size");

                var afterClean = package.Resources.Cast<ResourceIndexEntry>().ToList();
                afterClean.Should().HaveCount(original.Count);
                for (int i = 0; i < original.Count; i++)
                {
                    afterClean[i].Key.Should().Be(original[i].Key);
                    afterClean[i].FileSize.Should().Be(original[i].FileSize);
                    afterClean[i].MemorySize.Should().Be(original[i].MemorySize);
                    afterClean[i].CompressionType.Should().Be(original[i].Compression);
                }

                // 2. Modify one resource and save again in the same session
                var target = package.Resources[modifiedIndex];
                var data = (await package.GetResourceDataAsync(target)).ToArray();
                replacement = [.. data, .. "TS4Tools-roundtrip"u8];
                package.ReplaceResource(target, replacement);
                await package.SaveAsync();
            }

            await using var reloaded = await DbpfPackage.OpenAsync(copyPath);
            var final = await SnapshotAsync(reloaded);
            final.Should().HaveCount(original.Count);
            for (int i = 0; i < original.Count; i++)
            {
                final[i].Key.Should().Be(original[i].Key);
                var expectedHash = i == modifiedIndex ? SHA256.HashData(replacement) : original[i].Hash;
                final[i].Hash.Should().Equal(expectedHash, $"resource #{i} {original[i].Key} must survive two in-place saves");
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
