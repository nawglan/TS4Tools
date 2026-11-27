// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 649-673 (GetResource: decompressed resource bytes)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1796-1880 (ExportBatch/ExportFile: decompressed bytes, one file per
//   resource, named by TGIN; here the S4_ prefix and resource name are dropped and a manifest is added)

using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary>
/// <c>ts4tools unpack &lt;package&gt; &lt;outdir&gt; [--force]</c>: writes every resource (decompressed) to
/// <c>outdir/resources/</c> and the header/index details to <c>outdir/manifest.json</c>.
/// </summary>
internal static class UnpackCommand
{
    public static readonly CommandSpec Spec = new(
        "unpack",
        "ts4tools unpack <package> <outdir> [--force] [--parallel N]",
        "Extract every resource (decompressed) plus a manifest.json that 'pack' can rebuild from.",
        """
        Layout:
          <outdir>/manifest.json
          <outdir>/resources/<TTTTTTTT>_<GGGGGGGG>_<IIIIIIIIIIIIIIII>[.<n>]<ext>
        <ext> comes from the resource type (e.g. .stbl; unknown types use .bnry). The .<n> suffix
        (1, 2, ...) only appears on the second and later entries that share a key.

        Options:
          --force        Allow a non-empty <outdir>; its manifest.json and resources/ are replaced.
          --parallel N   Resources decompressed/written concurrently (default: CPU count, max 16).
        """,
        2, 2, ["--force"], ["--parallel"]);

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        string packagePath = args.Positionals[0];
        string outDir = args.Positionals[1];
        int parallel = ParseParallel(args.Get("--parallel"), Math.Min(Environment.ProcessorCount, 16));
        var ct = context.CancellationToken;

        string manifestPath = Path.Combine(outDir, PackageManifest.FileName);
        string resourcesDir = Path.Combine(outDir, PackageManifest.ResourcesDirectory);
        if (Directory.Exists(outDir) && Directory.EnumerateFileSystemEntries(outDir).Any())
        {
            if (!args.Has("--force"))
                throw new CliException($"output directory is not empty: {outDir} (use --force to replace its contents)");
            // Only remove what unpack itself writes, never anything else in the directory.
            if (Directory.Exists(resourcesDir)) Directory.Delete(resourcesDir, recursive: true);
            if (File.Exists(manifestPath)) File.Delete(manifestPath);
        }

        var sw = Stopwatch.StartNew();
        await using var package = await PackageFiles.OpenReadAsync(packagePath, ct).ConfigureAwait(false);
        Directory.CreateDirectory(resourcesDir);

        // File names are assigned in index order so duplicate suffixes are deterministic.
        var entries = package.Resources;
        var names = new string[entries.Count];
        var seen = new Dictionary<ResourceKey, int>();
        for (int i = 0; i < entries.Count; i++)
        {
            var key = entries[i].Key;
            int dup = seen.TryGetValue(key, out int n) ? n : 0;
            seen[key] = dup + 1;
            names[i] = KeyFormat.FileName(key, dup);
        }

        var hashes = new string[entries.Count];
        long totalBytes = 0;
        await Parallel.ForEachAsync(Enumerable.Range(0, entries.Count),
            new ParallelOptions { MaxDegreeOfParallelism = parallel, CancellationToken = ct },
            async (i, token) =>
            {
                var data = await package.GetResourceDataAsync(entries[i], token).ConfigureAwait(false);
                await using (var fs = new FileStream(Path.Combine(resourcesDir, names[i]), FileMode.CreateNew,
                                 FileAccess.Write, FileShare.None, 65536, useAsync: true))
                {
                    await fs.WriteAsync(data, token).ConfigureAwait(false);
                }
                hashes[i] = Convert.ToHexStringLower(SHA256.HashData(data.Span));
                Interlocked.Add(ref totalBytes, data.Length);
            }).ConfigureAwait(false);

        var manifest = new PackageManifest
        {
            Source = Path.GetFileName(packagePath),
            Header = ManifestHeader.From(package),
        };
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            manifest.Resources.Add(new ManifestResource
            {
                Type = KeyFormat.Hex32(e.Key.ResourceType),
                Group = KeyFormat.Hex32(e.Key.ResourceGroup),
                Instance = KeyFormat.Hex64(e.Key.Instance),
                File = PackageManifest.ResourcesDirectory + "/" + names[i],
                CompressionType = KeyFormat.Hex16(e.CompressionType),
                Unknown2 = DbpfPackage.GetUnknown2(e),
                MemSize = e.MemorySize,
                Sha256 = hashes[i],
            });
        }
        await manifest.WriteAsync(manifestPath, ct).ConfigureAwait(false);

        context.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Unpacked {entries.Count} resources ({totalBytes:N0} bytes) to {outDir} in {sw.Elapsed.TotalSeconds:F1}s"));
        return ExitCodes.Success;
    }

    /// <exception cref="UsageException">Not a positive integer.</exception>
    internal static int ParseParallel(string? text, int defaultValue)
    {
        if (text is null) return defaultValue;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 256)
            throw new UsageException($"--parallel must be an integer from 1 to 256, got '{text}'");
        return n;
    }
}
