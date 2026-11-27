// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 263-338 (header fields compared) and 649-673
//   (GetResource: decompressed bytes compared); legacy has no package comparison tool.

using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary>
/// <c>ts4tools compare &lt;a&gt; &lt;b&gt;</c>: resource-for-resource comparison (header fields, keys in order,
/// compression labels, Unknown2, decompressed bytes). Used to verify an unpack/pack round trip.
/// </summary>
internal static class CompareCommand
{
    public static readonly CommandSpec Spec = new(
        "compare",
        "ts4tools compare <package-a> <package-b> [--parallel N]",
        "Compare two packages resource-for-resource; exit 1 if they differ.",
        """
        Compares the header fields (version, user version, unused1-5, timestamps), the index in order
        (keys, compression labels, Unknown2) and every resource's decompressed bytes. Index layout
        (offsets, index type, compressed sizes) is not compared.

        Options:
          --parallel N   Resources compared concurrently (default: CPU count, max 16).
        """,
        2, 2, [], ["--parallel"]);

    /// <summary>Outcome of a comparison.</summary>
    internal sealed record CompareResult(int Resources, IReadOnlyList<string> Differences, int DifferenceCount,
        IReadOnlyDictionary<string, int> CompressionChanges)
    {
        public bool Identical => DifferenceCount == 0;
    }

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        int parallel = UnpackCommand.ParseParallel(args.Get("--parallel"), Math.Min(Environment.ProcessorCount, 16));
        var sw = Stopwatch.StartNew();
        var result = await CompareAsync(args.Positionals[0], args.Positionals[1], parallel, context.CancellationToken)
            .ConfigureAwait(false);
        foreach (var d in result.Differences)
            context.Out.WriteLine(d);
        if (result.DifferenceCount > result.Differences.Count)
            context.Out.WriteLine($"... {result.DifferenceCount - result.Differences.Count} more difference(s)");
        foreach (var (change, count) in result.CompressionChanges)
            context.Out.WriteLine($"compression {change}: {count} resource(s)");
        context.Out.WriteLine(result.Identical
            ? $"IDENTICAL: {result.Resources} resources, header fields equal ({sw.Elapsed.TotalSeconds:F1}s)"
            : $"DIFFERENT: {result.DifferenceCount} difference(s) over {result.Resources} resources ({sw.Elapsed.TotalSeconds:F1}s)");
        return result.Identical ? ExitCodes.Success : ExitCodes.Failure;
    }

    internal static async Task<CompareResult> CompareAsync(string pathA, string pathB, int parallel, CancellationToken ct)
    {
        const int MaxListed = 50;
        await using var a = await PackageFiles.OpenReadAsync(pathA, ct).ConfigureAwait(false);
        await using var b = await PackageFiles.OpenReadAsync(pathB, ct).ConfigureAwait(false);

        var differences = new ConcurrentQueue<string>();
        int count = 0;
        void Diff(string message)
        {
            if (Interlocked.Increment(ref count) <= MaxListed) differences.Enqueue(message);
        }

        var ha = ManifestHeader.From(a);
        var hb = ManifestHeader.From(b);
        void Field<T>(string name, T x, T y)
        {
            if (!EqualityComparer<T>.Default.Equals(x, y)) Diff($"header {name}: {x} != {y}");
        }
        Field("magic", ha.Magic, hb.Magic);
        Field("major", ha.Major, hb.Major);
        Field("minor", ha.Minor, hb.Minor);
        Field("userVersionMajor", ha.UserVersionMajor, hb.UserVersionMajor);
        Field("userVersionMinor", ha.UserVersionMinor, hb.UserVersionMinor);
        Field("unused1", ha.Unused1, hb.Unused1);
        Field("creationTime", ha.CreationTime, hb.CreationTime);
        Field("updatedTime", ha.UpdatedTime, hb.UpdatedTime);
        Field("unused2", ha.Unused2, hb.Unused2);
        Field("unused3", ha.Unused3, hb.Unused3);
        Field("unused4", ha.Unused4, hb.Unused4);
        Field("unused5", ha.Unused5, hb.Unused5);

        var ea = a.Resources;
        var eb = b.Resources;
        if (ea.Count != eb.Count)
            Diff($"resource count: {ea.Count} != {eb.Count}");
        int n = Math.Min(ea.Count, eb.Count);
        var compressionChanges = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

        await Parallel.ForEachAsync(Enumerable.Range(0, n),
            new ParallelOptions { MaxDegreeOfParallelism = parallel, CancellationToken = ct },
            async (i, token) =>
            {
                var x = ea[i];
                var y = eb[i];
                if (x.Key != y.Key)
                {
                    Diff($"#{i}: key {KeyFormat.Compact(x.Key)} != {KeyFormat.Compact(y.Key)}");
                    return;
                }
                string where = $"#{i} {KeyFormat.Compact(x.Key)}";
                if (x.CompressionType != y.CompressionType)
                {
                    Diff($"{where}: compression {KeyFormat.Hex16(x.CompressionType)} != {KeyFormat.Hex16(y.CompressionType)}");
                    compressionChanges.AddOrUpdate(
                        $"{KeyFormat.Hex16(x.CompressionType)} -> {KeyFormat.Hex16(y.CompressionType)}", 1, (_, c) => c + 1);
                }
                if (DbpfPackage.GetUnknown2(x) != DbpfPackage.GetUnknown2(y))
                    Diff($"{where}: unknown2 {DbpfPackage.GetUnknown2(x)} != {DbpfPackage.GetUnknown2(y)}");
                var dx = await a.GetResourceDataAsync(x, token).ConfigureAwait(false);
                var dy = await b.GetResourceDataAsync(y, token).ConfigureAwait(false);
                if (!dx.Span.SequenceEqual(dy.Span))
                    Diff($"{where}: data differs ({dx.Length} vs {dy.Length} bytes)");
            }).ConfigureAwait(false);

        var listed = differences.ToList();
        listed.Sort(StringComparer.Ordinal);
        return new CompareResult(Math.Max(ea.Count, eb.Count), listed, count,
            compressionChanges.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}
