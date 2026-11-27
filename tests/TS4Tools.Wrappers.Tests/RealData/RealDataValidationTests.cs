// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 64-67 (wrapper per type, as exercised here)

using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace TS4Tools.Wrappers.Tests.RealData;

/// <summary>
/// Real-data validation: every registered wrapper must parse real game/mod resources and write them back
/// byte-identically. Skipped when neither the game install nor the mods folder is present.
/// </summary>
[Trait("Category", "RealData")]
public class RealDataValidationTests(ITestOutputHelper output)
{
    private static async Task<CorpusResult> CorpusAsync()
    {
        Skip.IfNot(RealDataCorpus.Available, "Sims 4 install / mods folder not found");
        var result = await RealDataHarness.GetAsync();
        Skip.If(result.Packages.Count == 0, "No corpus packages found");
        return result;
    }

    private async Task AssertFamilyAsync(string family)
    {
        var result = await CorpusAsync();
        var types = result.Types.Where(t => t.Family == family).ToList();
        Skip.If(types.Count == 0, $"No {family} resources in corpus");

        foreach (var t in types)
        {
            output.WriteLine(
                $"0x{t.ResourceType:X8} {t.Wrapper}: sampled {t.Sampled}/{t.Present}, read {t.ReadFailures}, parse {t.ParseFailures}, " +
                $"write {t.SerializeFailures}, unsupported {t.Unsupported}, mismatch {t.Mismatches} (stable {t.MismatchesReparseStable}), fail {t.Failures}");
            if (t.FirstFailure is not null)
                output.WriteLine("    first: " + t.FirstFailure);
        }

        var failing = types.Where(t => t.Failures > 0)
            .Select(t => $"0x{t.ResourceType:X8} {t.Wrapper}: {t.Failures} failures; first: {t.FirstFailure}")
            .ToList();
        failing.Should().BeEmpty($"every {family} resource must parse and round-trip byte-identically");
    }

    [SkippableFact]
    public Task Catalog_ResourcesRoundTrip() => AssertFamilyAsync("Catalog");

    [SkippableFact]
    public Task Cas_ResourcesRoundTrip() => AssertFamilyAsync("CAS");

    [SkippableFact]
    public Task Rcol_ResourcesRoundTrip() => AssertFamilyAsync("Rcol");

    [SkippableFact]
    public Task Image_ResourcesRoundTrip() => AssertFamilyAsync("Image");

    [SkippableFact]
    public Task Data_ResourcesRoundTrip() => AssertFamilyAsync("Data");

    [SkippableFact]
    public Task AnimationMisc_ResourcesRoundTrip() => AssertFamilyAsync("Animation/Misc");

    [SkippableFact]
    public async Task Corpus_AllPackagesOpen()
    {
        var result = await CorpusAsync();
        result.PackageErrors.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task Summary_WritesReport()
    {
        var result = await CorpusAsync();
        var report = RealDataHarness.Report(result);

        var outPath = Path.Combine(AppContext.BaseDirectory, "real-data-validation.md");
        await File.WriteAllTextAsync(outPath, report);
        output.WriteLine($"Report: {outPath} (corpus run {result.Elapsed.TotalSeconds:F1}s)");

        if (RealDataCorpus.WriteDocs)
        {
            var docs = FindRepoFile("docs");
            if (docs is not null)
                await File.WriteAllTextAsync(Path.Combine(docs, "real-data-validation.md"), report);
        }

        output.WriteLine(report);
        result.Types.Should().NotBeEmpty();
    }

    private static string? FindRepoFile(string name)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TS4Tools.sln")))
                return Path.Combine(dir.FullName, name);
        }

        return null;
    }
}
