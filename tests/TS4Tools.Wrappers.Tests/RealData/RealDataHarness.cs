// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 64-67 (GetResource: wrapper chosen by type)
// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs (Stream/AsBytes = UnParse output used for round-trip)

using TS4Tools.Package;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.Tests.RealData;

/// <summary>Outcome counters for one resource type across the corpus.</summary>
internal sealed class TypeStats(uint resourceType, string wrapper, string family)
{
    private readonly object _gate = new();

    public uint ResourceType { get; } = resourceType;
    public string Wrapper { get; } = wrapper;
    public string Family { get; } = family;

    /// <summary>Resources of this type present in the scanned packages.</summary>
    public int Present { get; private set; }
    /// <summary>Resources actually parsed (after per-package sampling).</summary>
    public int Sampled { get; private set; }
    public int ReadFailures { get; private set; }
    public int ParseFailures { get; private set; }
    public int SerializeFailures { get; private set; }
    /// <summary>Serialize output differs from the original bytes.</summary>
    public int Mismatches { get; private set; }
    /// <summary>Zero-length resources (not checked: an empty buffer means "new resource" to every wrapper).</summary>
    public int Empty { get; private set; }
    /// <summary>
    /// Versions newer than any legacy layout, refused by the wrapper with
    /// <see cref="UnsupportedResourceVersionException"/> (documented legacy limitation, not a failure).
    /// </summary>
    public int Unsupported { get; private set; }
    /// <summary>Distinct unsupported-version messages (for the report).</summary>
    public SortedSet<string> UnsupportedVersions { get; } = new(StringComparer.Ordinal);
    /// <summary>Mismatches whose output re-parses and re-serializes to itself (stable, but not byte-identical).</summary>
    public int MismatchesReparseStable { get; private set; }
    public HashSet<string> Packages { get; } = new(StringComparer.Ordinal);
    public string? FirstFailure { get; private set; }

    /// <summary>Documented legacy limitation, if any (mismatches are then accepted when re-parse stable).</summary>
    public string? KnownLimitation => KnownLegacyLimitations.Reason(ResourceType);

    /// <summary>Parse failures / stable mismatches accepted as documented legacy limitations.</summary>
    public int LegacyAccepted { get; private set; }

    /// <summary>Resources identical to legacy's predicted output (<see cref="KnownLegacyLimitations.LegacyRewrite"/>).</summary>
    public int LegacyRewritten { get; private set; }

    /// <summary>Failures that the type-family tests assert to be zero.</summary>
    public int Failures =>
        ReadFailures + ParseFailures + SerializeFailures +
        Mismatches - LegacyAccepted;

    public void AddPresent(int n, string package)
    {
        lock (_gate)
        {
            Present += n;
            Packages.Add(package);
        }
    }

    public void Record(Outcome outcome, string? failure)
    {
        lock (_gate)
        {
            Sampled++;
            switch (outcome)
            {
                case Outcome.ReadFailure: ReadFailures++; break;
                case Outcome.ParseFailure: ParseFailures++; break;
                case Outcome.SerializeFailure: SerializeFailures++; break;
                case Outcome.Mismatch: Mismatches++; break;
                case Outcome.MismatchStable: Mismatches++; MismatchesReparseStable++; break;
                case Outcome.Empty: Empty++; break;
                case Outcome.IdenticalToLegacy: LegacyRewritten++; break;
                case Outcome.LegacyParseFailure: ParseFailures++; LegacyAccepted++; break;
                case Outcome.LegacyMismatch: Mismatches++; MismatchesReparseStable++; LegacyAccepted++; break;
                case Outcome.Unsupported:
                    Unsupported++;
                    if (failure is not null) UnsupportedVersions.Add(failure);
                    return;
            }

            if (failure is not null && FirstFailure is null && outcome is not (Outcome.LegacyParseFailure or Outcome.LegacyMismatch))
                FirstFailure = failure;
        }
    }
}

internal enum Outcome
{
    Identical,
    IdenticalToLegacy,
    Empty,
    LegacyParseFailure,
    LegacyMismatch,
    Unsupported,
    ReadFailure,
    ParseFailure,
    SerializeFailure,
    Mismatch,
    MismatchStable,
}

/// <summary>Result of one corpus run.</summary>
internal sealed record CorpusResult(
    IReadOnlyList<string> Packages,
    IReadOnlyList<string> PackageErrors,
    IReadOnlyList<TypeStats> Types,
    int Cap,
    TimeSpan Elapsed,
    int UnwrappedResources);

/// <summary>
/// Runs every registered wrapper over real resources and checks parse + byte-identical round-trip.
/// </summary>
internal static class RealDataHarness
{
    private static readonly Lazy<Task<CorpusResult>> Shared = new(() => Task.Run(RunAsync));

    /// <summary>Registry populated exactly like the application: discovery over the Wrappers assembly.</summary>
    public static ResourceHandlerRegistry CreateRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(StblResourceFactory).Assembly);
        return registry;
    }

    /// <summary>The corpus result, computed once per test run.</summary>
    public static Task<CorpusResult> GetAsync() => Shared.Value;

    private static async Task<CorpusResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var registry = CreateRegistry();
        var packages = RealDataCorpus.Packages();
        var cap = RealDataCorpus.Cap;
        var stats = new ConcurrentDictionary<uint, TypeStats>();
        var packageErrors = new ConcurrentBag<string>();
        var unwrapped = 0;

        await Parallel.ForEachAsync(
            packages,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount / 2, 1, 6) },
            async (path, ct) =>
            {
                var display = RealDataCorpus.Display(path);
                try
                {
                    var n = await ScanPackageAsync(path, display, registry, cap, stats, ct).ConfigureAwait(false);
                    Interlocked.Add(ref unwrapped, n);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    packageErrors.Add($"{display}: {ex.GetType().Name}: {ex.Message}");
                }
            }).ConfigureAwait(false);

        sw.Stop();
        return new CorpusResult(
            packages.Select(RealDataCorpus.Display).ToList(),
            packageErrors.OrderBy(s => s, StringComparer.Ordinal).ToList(),
            stats.Values.OrderBy(s => s.ResourceType).ToList(),
            cap,
            sw.Elapsed,
            unwrapped);
    }

    private static async Task<int> ScanPackageAsync(
        string path, string display, ResourceHandlerRegistry registry, int cap,
        ConcurrentDictionary<uint, TypeStats> stats, CancellationToken ct)
    {
        await using var package = await DbpfPackage.OpenAsync(path, readWrite: false, ct).ConfigureAwait(false);
        var unwrapped = 0;

        foreach (var group in package.Resources.Where(r => !r.IsDeleted).GroupBy(r => r.Key.ResourceType))
        {
            var factory = registry.GetFactory(group.Key);
            if (factory is not null && !RealDataCorpus.IncludeType(group.Key))
                continue;
            var entries = group.ToList();
            if (factory is null)
            {
                unwrapped += entries.Count;
                continue;
            }

            var ts = stats.GetOrAdd(group.Key, t => new TypeStats(t, factory.GetType().Name, FamilyOf(factory.GetType())));
            ts.AddPresent(entries.Count, display);

            foreach (var entry in Sample(entries, cap))
            {
                ct.ThrowIfCancellationRequested();
                var (outcome, failure) = await CheckAsync(package, entry, factory, display, ct).ConfigureAwait(false);
                ts.Record(outcome, failure);
            }
        }

        return unwrapped;
    }

    /// <summary>Evenly spaced deterministic sample (keeps variety across groups/instances in index order).</summary>
    private static IEnumerable<IResourceIndexEntry> Sample(List<IResourceIndexEntry> entries, int cap)
    {
        if (entries.Count <= cap)
            return entries;
        var step = (double)entries.Count / cap;
        return Enumerable.Range(0, cap).Select(i => entries[(int)(i * step)]);
    }

    private static async Task<(Outcome, string?)> CheckAsync(
        DbpfPackage package, IResourceIndexEntry entry, IResourceFactory factory, string display, CancellationToken ct)
    {
        var where = $"{display} {entry.Key}";
        byte[] original;
        try
        {
            original = (await package.GetResourceDataAsync(entry, ct).ConfigureAwait(false)).ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            return (Outcome.ReadFailure, $"{where}: read: {Describe(ex)}");
        }

        // A zero-length resource is the "create new" input of every wrapper (TypedResource calls
        // InitializeDefaults; legacy AResource does the same when handed no stream), so its output is
        // the default layout and cannot equal the empty input. Count it separately.
        if (original.Length == 0)
            return (Outcome.Empty, null);

        var result = Check(entry.Key, original, factory, where);
        if (result.Outcome is not (Outcome.Identical or Outcome.IdenticalToLegacy))
            RealDataCorpus.Dump(entry.Key, original);
        return result;
    }

    /// <summary>Parses <paramref name="original"/>, serializes it, and classifies the result.</summary>
    public static (Outcome Outcome, string? Failure) Check(ResourceKey key, byte[] original, IResourceFactory factory, string where)
    {
        IResource resource;
        try
        {
            resource = factory.Create(key, original);
        }
        catch (UnsupportedResourceVersionException ex)
        {
            return (Outcome.Unsupported, ex.Message);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            var outcome = KnownLegacyLimitations.AcceptsParseFailure(key.ResourceType) ? Outcome.LegacyParseFailure : Outcome.ParseFailure;
            return (outcome, $"{where} ({original.Length} bytes): parse: {Describe(ex)}");
        }

        using (resource)
        {
            byte[] written;
            try
            {
                written = resource.Data.ToArray();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return (Outcome.SerializeFailure, $"{where}: serialize: {Describe(ex)}");
            }

            if (written.AsSpan().SequenceEqual(original))
                return (Outcome.Identical, null);

            // Where legacy is known to normalize specific fields, compare against its predicted output.
            var legacyExpected = KnownLegacyLimitations.LegacyRewrite(FamilyOf(factory.GetType()), original, written);
            if (!ReferenceEquals(legacyExpected, original) && written.AsSpan().SequenceEqual(legacyExpected))
                return (Outcome.IdenticalToLegacy, null);

            var diff = FirstDifference(original, written);
            var detail = $"{where}: round-trip mismatch: original {original.Length} bytes, written {written.Length} bytes, first difference at 0x{diff:X}";

            // Classify: does our own output re-parse and re-serialize to itself?
            try
            {
                using var again = factory.Create(key, written);
                var rewritten = again.Data.ToArray();
                if (rewritten.AsSpan().SequenceEqual(written))
                {
                    var stable = KnownLegacyLimitations.AcceptsMismatch(key) ? Outcome.LegacyMismatch : Outcome.MismatchStable;
                    return (stable, detail + " (re-parse stable)");
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return (Outcome.Mismatch, detail + $" (re-parse of output failed: {Describe(ex)})");
            }

            return (Outcome.Mismatch, detail + " (re-parse unstable)");
        }
    }

    private static int FirstDifference(byte[] a, byte[] b)
    {
        var i = a.AsSpan().CommonPrefixLength(b);
        return i;
    }

    private static string Describe(Exception ex)
    {
        var msg = $"{ex.GetType().Name}: {ex.Message}";
        var frame = ex.StackTrace?.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Contains("TS4Tools.", StringComparison.Ordinal));
        return frame is null ? msg : $"{msg} [{frame}]";
    }

    /// <summary>Groups factories into the type families used by the per-family tests.</summary>
    public static string FamilyOf(Type factoryType)
    {
        var name = factoryType.Name;
        if (factoryType.Namespace?.EndsWith(".CatalogResource", StringComparison.Ordinal) == true)
            return "Catalog";
        return name switch
        {
            "CasPartResourceFactory" or "BoneResourceFactory" or "DeformerMapResourceFactory" or
            "GeomListResourceFactory" or "SimOutfitResourceFactory" or "SkinToneResourceFactory" or
            "StyleLookResourceFactory" or "UserCAStPresetResourceFactory" => "CAS",
            "RcolResourceFactory" or "GeomResourceFactory" or "ModularResourceFactory" => "Rcol",
            "ImageResourceFactory" or "RleResourceFactory" or "ThumbnailResourceFactory" or
            "ThumResourceFactory" or "TxtcResourceFactory" => "Image",
            "TextResourceFactory" or "StblResourceFactory" or "SimDataResourceFactory" or
            "NameMapResourceFactory" or "NgmpHashMapResourceFactory" or "ObjKeyResourceFactory" or
            "ScriptResourceFactory" or "ComplateResourceFactory" => "Data",
            _ => "Animation/Misc",
        };
    }

    /// <summary>Renders the markdown report.</summary>
    public static string Report(CorpusResult result)
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;
        sb.AppendLine("# Real-data validation");
        sb.AppendLine();
        sb.AppendLine("Generated by `tests/TS4Tools.Wrappers.Tests/RealData` (`RealDataValidationTests.Summary_WritesReport`).");
        sb.AppendLine("Every resource type with a registered wrapper is parsed from real game and mod packages and");
        sb.AppendLine("serialized back; the output must be byte-identical to the decompressed original. Regenerate with");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("TS4TOOLS_REALDATA_WRITE_DOCS=1 dotnet test tests/TS4Tools.Wrappers.Tests --filter FullyQualifiedName~RealData");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(ci, $"Sample cap: {(result.Cap == int.MaxValue ? "none (full run)" : result.Cap.ToString(ci))} resources per type per package ");
        sb.AppendLine("(`TS4TOOLS_REALDATA_CAP`, 0 = all).");
        sb.AppendLine();
        sb.AppendLine("Columns: *present* = resources of the type in the scanned packages; *sampled* = parsed;");
        sb.AppendLine("*read* = decompression failures; *parse*/*write* = exceptions; *empty* = zero-length resources (not checked); *unsupp.* = versions newer than any legacy");
        sb.AppendLine("layout, refused by the wrapper (see below); *mismatch* = output differs from the");
        sb.AppendLine("original (*stable* = of those, how many re-parse to identical output); *fail* = what the family tests assert to be 0.");
        sb.AppendLine();
        sb.AppendLine("## Packages");
        sb.AppendLine();
        foreach (var p in result.Packages)
            sb.AppendLine(ci, $"- `{p}`");
        if (result.PackageErrors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Package errors:");
            foreach (var e in result.PackageErrors)
                sb.AppendLine(ci, $"- {e}");
        }

        sb.AppendLine();
        sb.AppendLine(ci, $"Resources without a registered wrapper (not checked): {result.UnwrappedResources}.");
        sb.AppendLine();
        sb.AppendLine("## Results per type");
        sb.AppendLine();
        sb.AppendLine("| Type | Wrapper | Family | Pkgs | Present | Sampled | Read | Parse | Write | Empty | Unsupp. | Legacy (rewrite/accepted) | Mismatch (stable) | Fail | First failure |");
        sb.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var t in result.Types)
        {
            var first = t.Failures > 0 || t.Mismatches > 0 ? Escape(t.FirstFailure ?? "") : "";
            sb.AppendLine(ci,
                $"| `0x{t.ResourceType:X8}` | {t.Wrapper.Replace("ResourceFactory", "", StringComparison.Ordinal)} | {t.Family} | {t.Packages.Count} | {t.Present} | {t.Sampled} | {t.ReadFailures} | {t.ParseFailures} | {t.SerializeFailures} | {t.Empty} | {t.Unsupported} | {t.LegacyRewritten}/{t.LegacyAccepted} | {t.Mismatches} ({t.MismatchesReparseStable}) | {t.Failures} | {first} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Documented legacy limitations");
        sb.AppendLine();
        sb.AppendLine("Inherited legacy behaviour (column *legacy*: resources equal to legacy's predicted rewrite / accepted");
        sb.AppendLine("legacy failures). Accepted mismatches must still be re-parse stable:");
        sb.AppendLine();
        sb.AppendLine(ci, $"- {KnownLegacyLimitations.RcolRewriteReason}");
        foreach (var (type, limitation) in KnownLegacyLimitations.All.OrderBy(kv => kv.Key))
            sb.AppendLine(ci, $"- `0x{type:X8}`: {limitation.Reason}");

        sb.AppendLine();
        sb.AppendLine("Versions refused as newer than any legacy layout (`UnsupportedResourceVersionException`; the");
        sb.AppendLine("wrapper cites the legacy code in each check):");
        sb.AppendLine();
        var unsupported = result.Types.Where(t => t.Unsupported > 0).ToList();
        if (unsupported.Count == 0)
            sb.AppendLine("- None.");
        foreach (var t in unsupported)
        {
            sb.AppendLine(ci, $"- `0x{t.ResourceType:X8}` {t.Wrapper.Replace("ResourceFactory", "", StringComparison.Ordinal)}: {t.Unsupported} resources");
            foreach (var m in t.UnsupportedVersions)
                sb.AppendLine(ci, $"  - {m}");
        }

        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine(ci, $"- Types checked: {result.Types.Count}");
        sb.AppendLine(ci, $"- Resources sampled: {result.Types.Sum(t => t.Sampled)}");
        sb.AppendLine(ci, $"- Refused (unsupported newer versions): {result.Types.Sum(t => t.Unsupported)}");
        sb.AppendLine(ci, $"- Failures: {result.Types.Sum(t => t.Failures)}");
        return sb.ToString();
    }

    private static string Escape(string s) => s.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
