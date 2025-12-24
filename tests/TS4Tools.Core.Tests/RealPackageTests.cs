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
            package.ResourceCount.Should().BeGreaterOrEqualTo(0);
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
}
