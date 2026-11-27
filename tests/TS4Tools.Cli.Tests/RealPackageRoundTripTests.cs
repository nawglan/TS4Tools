using FluentAssertions;
using TS4Tools.Cli.Commands;
using TS4Tools.Package;
using Xunit;
using Xunit.Abstractions;

namespace TS4Tools.Cli.Tests;

/// <summary>
/// unpack -> pack on real game packages, verified resource-for-resource. Skipped when the game is not installed.
/// The game folder is only read; output goes to artifacts/cli-tests/ in the repository (gitignored).
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 96-147 (SaveAs relabels every rewritten chunk
///   0x5A42/0x0000; there is no RefPack compressor, so 0xFFFF chunks cannot be reproduced)
/// </summary>
[Trait("Category", "RealData")]
public class RealPackageRoundTripTests(ITestOutputHelper output)
{
    private const string GamePath = "/mnt/ai/SteamLibrary/steamapps/common/The Sims 4";

    private static string GameRoot =>
        Environment.GetEnvironmentVariable("TS4TOOLS_GAME_DIR") is { Length: > 0 } configured ? configured : GamePath;

    private static string ArtifactsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TS4Tools.sln")))
            dir = dir.Parent;
        string root = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(root, "artifacts", "cli-tests", Guid.NewGuid().ToString("N"));
    }

    /// <summary>The smallest Delta package with at least <paramref name="minResources"/> resources matching the filter.</summary>
    private static async Task<string?> FindDeltaPackageAsync(int minResources, Func<DbpfPackage, bool> filter)
    {
        string delta = Path.Combine(GameRoot, "Delta");
        if (!Directory.Exists(delta)) return null;
        foreach (var file in Directory.EnumerateFiles(delta, "*.package", SearchOption.AllDirectories)
                     .Select(p => new FileInfo(p)).OrderBy(f => f.Length).ThenBy(f => f.FullName, StringComparer.Ordinal))
        {
            await using var p = await DbpfPackage.OpenAsync(file.FullName);
            if (p.ResourceCount >= minResources && filter(p)) return file.FullName;
        }
        return null;
    }

    private async Task<CompareCommand.CompareResult> UnpackPackCompareAsync(string original)
    {
        string work = ArtifactsDir();
        try
        {
            string dir = Path.Combine(work, "unpacked");
            string rebuilt = Path.Combine(work, "rebuilt.package");
            var sw = Stopwatch.StartNew();
            var unpack = await CliRunner.RunAsync("unpack", original, dir);
            unpack.ExitCode.Should().Be(0, unpack.Err);
            var pack = await CliRunner.RunAsync("pack", dir, rebuilt);
            pack.ExitCode.Should().Be(0, pack.Err);
            output.WriteLine($"{Path.GetRelativePath(GameRoot, original)}: unpack+pack {sw.Elapsed.TotalSeconds:F1}s");
            output.WriteLine(pack.Out + pack.Err);
            var result = await CompareCommand.CompareAsync(original, rebuilt, 8, CancellationToken.None);
            foreach (var d in result.Differences.Take(10)) output.WriteLine(d);
            return result;
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch (IOException) { /* best effort */ }
        }
    }

    [SkippableFact]
    public async Task UnpackPack_SmallestDeltaPackage_IsIdentical()
    {
        // Smallest Delta package with a reasonable mix of resources, and no RefPack chunks (those are re-encoded).
        var path = await FindDeltaPackageAsync(20, p => p.Resources.All(e => e.CompressionType != 0xFFFF));
        Skip.If(path is null, "Sims 4 Delta packages not found");

        var result = await UnpackPackCompareAsync(path!);

        result.Identical.Should().BeTrue(string.Join("\n", result.Differences.Take(10)));
        result.Resources.Should().BeGreaterThanOrEqualTo(20);
    }

    [SkippableFact]
    public async Task UnpackPack_DeltaPackageWithDeletionRecords_KeepsThem()
    {
        var path = await FindDeltaPackageAsync(1, p => p.Resources.Any(e => e.CompressionType == 0xFFE0) &&
                                                      p.Resources.All(e => e.CompressionType != 0xFFFF));
        Skip.If(path is null, "No Delta package with 0xFFE0 deletion records found");

        var result = await UnpackPackCompareAsync(path!);

        result.Identical.Should().BeTrue(string.Join("\n", result.Differences.Take(10)));
    }

    [SkippableFact]
    public async Task UnpackPack_RefPackPackage_OnlyCompressionLabelChanges()
    {
        var path = await FindDeltaPackageAsync(1, p => p.Resources.Any(e => e.CompressionType == 0xFFFF));
        Skip.If(path is null, "No Delta package with RefPack (0xFFFF) entries found");

        var result = await UnpackPackCompareAsync(path!);

        // Data, keys, order and header survive; only the 0xFFFF labels become 0x5A42 (as legacy SaveAs does).
        result.CompressionChanges.Keys.Should().Equal("0xFFFF -> 0x5A42");
        result.DifferenceCount.Should().Be(result.CompressionChanges.Values.Sum());
    }
}
