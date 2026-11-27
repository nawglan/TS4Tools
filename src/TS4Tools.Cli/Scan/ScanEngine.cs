// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 53-67 (GetResource: wrapper chosen by type,
//   default wrapper when none is registered) - the scan classifies each resource by what that lookup and parse yield.
// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 649-673 (GetResource: decompressed bytes fed to the wrapper)

using TS4Tools.Package;
using TS4Tools.Resources;
using TS4Tools.Wrappers;

namespace TS4Tools.Cli.Scan;

/// <summary>How one resource fared.</summary>
internal enum ScanClass
{
    Ok = 0,
    Unwrapped = 1,
    UnsupportedVersion = 2,
    ParseError = 3,
    RoundtripMismatch = 4,

    /// <summary>Zero-length resource (e.g. a 0xFFE0 deletion record in a Delta package): nothing to parse, not a problem.</summary>
    Empty = 5,
}

/// <summary>Scan settings.</summary>
/// <param name="Roundtrip">Also compare the wrapper's re-serialised Data with the original bytes.</param>
/// <param name="Parallelism">Packages scanned concurrently.</param>
/// <param name="Types">Only these resource types (null = all).</param>
internal sealed record ScanOptions(bool Roundtrip, int Parallelism, IReadOnlySet<uint>? Types);

/// <summary>A sample resource for a problem class.</summary>
internal sealed record ScanSample(string Package, ResourceKey Key, string Detail) : IComparable<ScanSample>
{
    public int CompareTo(ScanSample? other)
    {
        if (other is null) return 1;
        int c = string.CompareOrdinal(Package, other.Package);
        return c != 0 ? c : Key.CompareTo(other.Key);
    }
}

/// <summary>A package that could not be opened (or failed while being read).</summary>
internal sealed record ScanPackageFailure(string Package, string Message);

/// <summary>Per-resource-type tallies.</summary>
internal sealed class TypeStats(uint resourceType)
{
    public const int MaxSamples = 3;
    private const int MaxDistinctMessages = 12;
    internal const string OtherMessages = "(other messages)";

    public uint ResourceType { get; } = resourceType;
    public string Wrapper { get; set; } = string.Empty;
    public long Total { get; private set; }
    public long[] Counts { get; } = new long[6];

    /// <summary>Unsupported versions: (wrapper resource name, version, max supported) -> count.</summary>
    public Dictionary<(string Resource, uint Version, uint MaxSupported), long> Versions { get; } = [];

    /// <summary>Parse-error messages (digits normalised to '#') -> count.</summary>
    public Dictionary<string, long> Messages { get; } = new(StringComparer.Ordinal);

    /// <summary>Up to <see cref="MaxSamples"/> samples per problem class (the lowest by package, then key).</summary>
    public List<ScanSample>[] Samples { get; } = [[], [], [], [], [], []];

    public long Problems => Total - Counts[(int)ScanClass.Ok] - Counts[(int)ScanClass.Empty];

    public void Add(ScanClass cls, string package, ResourceKey key, string? detail)
    {
        Total++;
        Counts[(int)cls]++;
        if (cls is not (ScanClass.Ok or ScanClass.Empty))
            AddSample(cls, new ScanSample(package, key, detail ?? string.Empty));
    }

    public void AddMessage(string normalised, long count = 1)
    {
        if (Messages.ContainsKey(normalised) || Messages.Count < MaxDistinctMessages)
            Messages[normalised] = Messages.GetValueOrDefault(normalised) + count;
        else
            Messages[OtherMessages] = Messages.GetValueOrDefault(OtherMessages) + count;
    }

    private void AddSample(ScanClass cls, ScanSample sample)
    {
        var list = Samples[(int)cls];
        if (list.Count >= MaxSamples && sample.CompareTo(list[^1]) >= 0) return;
        int at = list.BinarySearch(sample);
        list.Insert(at < 0 ? ~at : at, sample);
        if (list.Count > MaxSamples) list.RemoveAt(list.Count - 1);
    }

    public void Merge(TypeStats other)
    {
        Total += other.Total;
        for (int i = 0; i < Counts.Length; i++) Counts[i] += other.Counts[i];
        if (string.IsNullOrEmpty(Wrapper)) Wrapper = other.Wrapper;
        foreach (var (k, v) in other.Versions) Versions[k] = Versions.GetValueOrDefault(k) + v;
        foreach (var (k, v) in other.Messages)
        {
            if (k == OtherMessages) Messages[k] = Messages.GetValueOrDefault(k) + v;
            else AddMessage(k, v);
        }
        for (int c = 0; c < Samples.Length; c++)
            foreach (var s in other.Samples[c]) AddSample((ScanClass)c, s);
    }
}

/// <summary>Aggregated scan result.</summary>
internal sealed class ScanReport
{
    public List<string> Roots { get; } = [];
    public bool Roundtrip { get; init; }
    public IReadOnlySet<uint>? TypeFilter { get; init; }
    public int PackagesScanned { get; set; }
    public long Resources => Types.Values.Sum(t => t.Total);
    public SortedDictionary<uint, TypeStats> Types { get; } = [];
    public List<ScanPackageFailure> FailedPackages { get; } = [];
    public TimeSpan Elapsed { get; set; }

    public long Count(ScanClass cls) => Types.Values.Sum(t => t.Counts[(int)cls]);

    public bool HasProblems => FailedPackages.Count > 0 || Types.Values.Any(t => t.Problems > 0);
}

/// <summary>
/// Opens every package (streamed, parallel across packages), resolves each resource's wrapper and classifies it.
/// No resource data is retained: each resource is read, parsed, optionally re-serialised, then released.
/// </summary>
internal static partial class ScanEngine
{
    /// <summary>A registry with exactly the TS4Tools wrappers (no plugins), default factory for everything else.</summary>
    public static ResourceHandlerRegistry CreateRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(UnsupportedResourceVersionException).Assembly);
        return registry;
    }

    /// <summary>Finds *.package files under each root (a root may also be a single package file).</summary>
    /// <exception cref="CliException">A root does not exist.</exception>
    public static List<(string FullPath, string Display)> FindPackages(IReadOnlyList<string> roots)
    {
        var result = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var root in roots)
        {
            string full = Path.GetFullPath(root);
            string prefix = roots.Count > 1 ? DisplayName(full) + "/" : string.Empty;
            if (File.Exists(full))
            {
                if (seen.Add(full)) result.Add((full, prefix.Length > 0 ? prefix.TrimEnd('/') : Path.GetFileName(full)));
                continue;
            }
            if (!Directory.Exists(full))
                throw new CliException($"path not found: {root}");
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };
            foreach (var file in Directory.EnumerateFiles(full, "*.package", options))
            {
                if (!seen.Add(file)) continue;
                result.Add((file, prefix + Path.GetRelativePath(full, file).Replace('\\', '/')));
            }
        }
        result.Sort((a, b) => string.CompareOrdinal(a.Item2, b.Item2));
        return result;
    }

    private static string DisplayName(string fullPath)
    {
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
        return string.IsNullOrEmpty(name) ? fullPath : name;
    }

    public static async Task<ScanReport> ScanAsync(
        IReadOnlyList<string> roots,
        ScanOptions options,
        IResourceHandlerRegistry registry,
        Action<int, int>? progress,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var packages = FindPackages(roots);
        var report = new ScanReport { Roundtrip = options.Roundtrip, TypeFilter = options.Types };
        report.Roots.AddRange(roots.Select(r => DisplayName(Path.GetFullPath(r))));

        // Largest first, so the big packages do not form a long single-threaded tail.
        var order = packages
            .Select(p => (p.FullPath, p.Display, Length: SafeLength(p.FullPath)))
            .OrderByDescending(p => p.Length)
            .ThenBy(p => p.Display, StringComparer.Ordinal)
            .ToList();

        var gate = new object();
        int done = 0;
        await Parallel.ForEachAsync(order,
            new ParallelOptions { MaxDegreeOfParallelism = options.Parallelism, CancellationToken = cancellationToken },
            async (p, token) =>
            {
                var local = new Dictionary<uint, TypeStats>();
                string? failure = await ScanPackageAsync(p.FullPath, p.Display, options, registry, local, token)
                    .ConfigureAwait(false);
                lock (gate)
                {
                    foreach (var (type, stats) in local)
                    {
                        if (report.Types.TryGetValue(type, out var global)) global.Merge(stats);
                        else report.Types[type] = stats;
                    }
                    if (failure != null) report.FailedPackages.Add(new ScanPackageFailure(p.Display, failure));
                    report.PackagesScanned++;
                    done++;
                    progress?.Invoke(done, order.Count);
                }
            }).ConfigureAwait(false);

        report.FailedPackages.Sort((a, b) => string.CompareOrdinal(a.Package, b.Package));
        report.Elapsed = sw.Elapsed;
        return report;
    }

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (IOException) { return 0; }
    }

    /// <summary>Scans one package into <paramref name="stats"/>; returns a failure message if it could not be read.</summary>
    internal static async Task<string?> ScanPackageAsync(
        string path,
        string display,
        ScanOptions options,
        IResourceHandlerRegistry registry,
        Dictionary<uint, TypeStats> stats,
        CancellationToken cancellationToken)
    {
        DbpfPackage package;
        try
        {
            package = await DbpfPackage.OpenAsync(path, readWrite: false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is PackageFormatException or IOException or UnauthorizedAccessException)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }

        await using (package.ConfigureAwait(false))
        {
            foreach (var entry in package.Resources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                uint type = entry.Key.ResourceType;
                if (options.Types != null && !options.Types.Contains(type)) continue;

                var factory = registry.GetFactory(type);
                if (!stats.TryGetValue(type, out var ts))
                {
                    ts = new TypeStats(type) { Wrapper = factory is null ? "(default)" : WrapperName(factory) };
                    stats[type] = ts;
                }

                if (entry.MemorySize == 0)
                {
                    ts.Add(ScanClass.Empty, display, entry.Key, null);
                    continue;
                }

                if (factory is null)
                {
                    // No TS4Tools wrapper: the data is never parsed, so it is not even read.
                    ts.Add(ScanClass.Unwrapped, display, entry.Key, null);
                    continue;
                }

                var (cls, detail) = await ClassifyAsync(package, entry, factory, options.Roundtrip, ts, cancellationToken)
                    .ConfigureAwait(false);
                ts.Add(cls, display, entry.Key, detail);
            }
        }
        return null;
    }

    private static async Task<(ScanClass, string?)> ClassifyAsync(
        DbpfPackage package,
        IResourceIndexEntry entry,
        IResourceFactory factory,
        bool roundtrip,
        TypeStats stats,
        CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> data;
        try
        {
            data = await package.GetResourceDataAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ParseError(stats, "read: " + Describe(ex));
        }

        IResource resource;
        try
        {
            resource = factory.Create(entry.Key, data);
        }
        catch (UnsupportedResourceVersionException ex)
        {
            var k = (ex.ResourceName, ex.Version, ex.MaxSupportedVersion);
            stats.Versions[k] = stats.Versions.GetValueOrDefault(k) + 1;
            return (ScanClass.UnsupportedVersion,
                $"{ex.ResourceName} version 0x{ex.Version:X} (max 0x{ex.MaxSupportedVersion:X})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ParseError(stats, Describe(ex));
        }

        using (resource)
        {
            if (!roundtrip) return (ScanClass.Ok, null);
            ReadOnlyMemory<byte> written;
            try
            {
                written = resource.Data;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return (ScanClass.RoundtripMismatch, "serialise threw " + Describe(ex));
            }
            if (written.Span.SequenceEqual(data.Span)) return (ScanClass.Ok, null);
            int at = written.Span.CommonPrefixLength(data.Span);
            return (ScanClass.RoundtripMismatch,
                $"{data.Length} bytes in, {written.Length} out, first difference at 0x{at:X}");
        }
    }

    private static (ScanClass, string?) ParseError(TypeStats stats, string message)
    {
        stats.AddMessage(Normalise(message));
        return (ScanClass.ParseError, message);
    }

    private static string Describe(Exception ex)
    {
        string message = ex.Message.ReplaceLineEndings(" ");
        if (message.Length > 200) message = message[..200] + "...";
        return $"{ex.GetType().Name}: {message}";
    }

    /// <summary>Groups messages that differ only in numbers (offsets, counts, versions).</summary>
    internal static string Normalise(string message) => DigitsRegex().Replace(message, "#");

    internal static string WrapperName(IResourceFactory factory)
    {
        string name = factory.GetType().Name;
        return name.EndsWith("Factory", StringComparison.Ordinal) && name.Length > 7 ? name[..^7] : name;
    }

    [GeneratedRegex(@"(0x)?[0-9A-Fa-f]*[0-9][0-9A-Fa-f]*", RegexOptions.CultureInvariant)]
    private static partial Regex DigitsRegex();
}
