// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 96-147 (SaveAs(Stream): every resource rewritten)

using FluentAssertions;
using TS4Tools.Package;
using Xunit;
using Xunit.Abstractions;

namespace TS4Tools.Wrappers.Tests.RealData;

/// <summary>
/// Package-level real-data check: open -> SaveToStreamAsync -> reopen must preserve every resource's decompressed
/// bytes (and keys, in order). The originals are only ever opened read-only; output goes to memory or to a
/// scratch directory under the test output folder (not /tmp, which may be RAM-backed).
/// </summary>
[Trait("Category", "RealData")]
public class PackageRoundTripTests(ITestOutputHelper output)
{
    /// <summary>A large mod package (largest *.package under the mods folder).</summary>
    private static string? LargestMod()
    {
        if (!Directory.Exists(RealDataCorpus.ModsDir)) return null;
        return Directory.EnumerateFiles(RealDataCorpus.ModsDir, "*.package", SearchOption.AllDirectories)
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.Length)
            .ThenBy(f => f.FullName, StringComparer.Ordinal)
            .FirstOrDefault()?.FullName;
    }

    [SkippableFact]
    public async Task SaveToStream_LargestMod_PreservesEveryResource()
    {
        var path = LargestMod();
        Skip.If(path is null, "Mods folder not found");
        await RoundTripAsync(path!, toFile: false);
    }

    [SkippableFact]
    public async Task SaveToStream_GamePackage_PreservesEveryResource()
    {
        // An expansion client delta: mixed resource types (meshes, textures, catalog, CAS), ~250 MB.
        var path = Path.Combine(RealDataCorpus.GameDir, "Delta", "EP01", "ClientDeltaBuild0.package");
        Skip.IfNot(File.Exists(path), "Game package not found");
        await RoundTripAsync(path, toFile: true);
    }

    [SkippableFact]
    public async Task SaveToStream_GameSimulationPackage_PreservesEveryResource()
    {
        var path = Path.Combine(RealDataCorpus.GameDir, "Delta", "EP01", "SimulationDeltaBuild0.package");
        Skip.IfNot(File.Exists(path), "Game package not found");
        await RoundTripAsync(path, toFile: false);
    }

    private async Task RoundTripAsync(string path, bool toFile)
    {
        var sw = Stopwatch.StartNew();
        var scratch = Path.Combine(AppContext.BaseDirectory, "realdata-scratch", Guid.NewGuid().ToString("N"));
        Stream target = toFile
            ? new FileStream(Path.Combine(Directory.CreateDirectory(scratch).FullName, "roundtrip.package"),
                FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1 << 16, FileOptions.DeleteOnClose)
            : new MemoryStream();

        try
        {
            await using var original = await DbpfPackage.OpenAsync(path);
            original.ResourceCount.Should().BeGreaterThan(0);
            await original.SaveToStreamAsync(target);

            target.Position = 0;
            await using var reopened = await DbpfPackage.OpenAsync(target, leaveOpen: true);
            reopened.ResourceCount.Should().Be(original.ResourceCount);

            var mismatches = new List<string>();
            for (var i = 0; i < original.ResourceCount; i++)
            {
                var a = original.Resources[i];
                var b = reopened.Resources[i];
                if (a.Key != b.Key)
                {
                    mismatches.Add($"#{i}: key {a.Key} became {b.Key}");
                    continue;
                }

                var da = await original.GetResourceDataAsync(a);
                var db = await reopened.GetResourceDataAsync(b);
                if (!da.Span.SequenceEqual(db.Span))
                    mismatches.Add($"#{i} {a.Key}: {da.Length} bytes became {db.Length} bytes");
                if (mismatches.Count >= 20) break;
            }

            output.WriteLine($"{RealDataCorpus.Display(path)}: {original.ResourceCount} resources, " +
                             $"{new FileInfo(path).Length / (1024 * 1024)} MB -> {target.Length / (1024 * 1024)} MB, {sw.Elapsed.TotalSeconds:F1}s");
            mismatches.Should().BeEmpty("open -> save -> reopen must preserve every resource");
        }
        finally
        {
            await target.DisposeAsync();
            try { if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
