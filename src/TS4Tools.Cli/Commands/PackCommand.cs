// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 96-147 (SaveAs: header written verbatim, index rebuilt,
//   dirty resources compressed only when Compressed != 0 and kept only if smaller), lines 464-471 (AddResource)
// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 810-826 (ImportStream: new resources, "compress" option)

using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary>
/// <c>ts4tools pack &lt;dir&gt; &lt;package&gt; [--force] [--no-compress-new]</c>: rebuilds a package from an
/// <c>unpack</c> directory (or from a bare <c>resources/</c> folder of key-named files).
/// </summary>
internal static class PackCommand
{
    public static readonly CommandSpec Spec = new(
        "pack",
        "ts4tools pack <dir> <package> [--force] [--no-compress-new]",
        "Rebuild a package from an unpack directory (manifest.json + resources/).",
        """
        With <dir>/manifest.json:
          - entries are written in manifest order from the current file contents, so edited files
            are picked up automatically; header fields, compression labels and Unknown2 are restored;
          - files under resources/ that the manifest does not list are ADDED, their key parsed from
            the file name (TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII[.n][.ext]);
          - manifest entries whose file is missing are DROPPED (with a warning).
        Without a manifest every file in <dir>/resources/ is packed, keyed by its file name.

        Compression: an entry labelled 0x5A42 is ZLIB-compressed (kept only if smaller, as in s4pi),
        0x0000 is stored, 0xFFE0 empty deletion records keep their label, and RefPack (0xFFFF)
        entries are re-encoded as ZLIB (no RefPack compressor exists in s4pi).

        Options:
          --force            Overwrite <package> if it exists.
          --no-compress-new  Store added files uncompressed (default: ZLIB, 0x5A42).
        """,
        2, 2, ["--force", "--no-compress-new"], []);

    /// <summary>Counts reported after a pack.</summary>
    internal sealed record PackResult(int Written, int FromManifest, int Edited, int Added, int Dropped, int Reencoded);

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        string dir = args.Positionals[0];
        string output = args.Positionals[1];
        if (File.Exists(output) && !args.Has("--force"))
            throw new CliException($"output package exists: {output} (use --force to overwrite)");

        var sw = Stopwatch.StartNew();
        var result = await PackAsync(dir, output, compressNew: !args.Has("--no-compress-new"), context).ConfigureAwait(false);
        context.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Packed {result.Written} resources into {output} in {sw.Elapsed.TotalSeconds:F1}s " +
            $"({result.FromManifest} from manifest, {result.Edited} edited, {result.Added} added, {result.Dropped} dropped)"));
        return ExitCodes.Success;
    }

    internal static async Task<PackResult> PackAsync(string dir, string output, bool compressNew, CliContext context)
    {
        var ct = context.CancellationToken;
        if (!Directory.Exists(dir))
            throw new CliException($"directory not found: {dir}");

        string fullDir = Path.GetFullPath(dir);
        string manifestPath = Path.Combine(fullDir, PackageManifest.FileName);
        string resourcesDir = Path.Combine(fullDir, PackageManifest.ResourcesDirectory);
        PackageManifest? manifest = File.Exists(manifestPath)
            ? await PackageManifest.ReadAsync(manifestPath, ct).ConfigureAwait(false)
            : null;
        if (manifest is null && !Directory.Exists(resourcesDir))
            throw new CliException($"{dir} has neither {PackageManifest.FileName} nor a {PackageManifest.ResourcesDirectory}/ directory");

        var pathComparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var used = new HashSet<string>(pathComparer);
        int fromManifest = 0, edited = 0, added = 0, dropped = 0, reencoded = 0;

        using var package = DbpfPackage.CreateNew(manifest?.Header.Major ?? 2);
        manifest?.Header.ApplyTo(package);

        if (manifest != null)
        {
            foreach (var res in manifest.Resources)
            {
                ct.ThrowIfCancellationRequested();
                var key = res.GetKey();
                var (compression, unknown2) = res.GetIndexFields();
                string full = ResolveInside(fullDir, res.File);
                used.Add(full);
                if (!File.Exists(full))
                {
                    context.Warn($"{res.File}: file missing, entry {KeyFormat.Compact(key)} dropped");
                    dropped++;
                    continue;
                }

                byte[] data = await ReadResourceFileAsync(full, ct).ConfigureAwait(false);
                if (res.Sha256 is { Length: > 0 } sha &&
                    !string.Equals(sha, Convert.ToHexStringLower(SHA256.HashData(data)), StringComparison.OrdinalIgnoreCase))
                    edited++;

                var entry = package.AddResource(key, data, rejectDuplicates: false)!;
                if (compression == RefPackCompressionType && data.Length > 0)
                {
                    compression = ZlibCompressionType;
                    reencoded++;
                }
                package.SetCompression(entry, compression);
                package.SetUnknown2(entry, unknown2);
                fromManifest++;
            }
            if (reencoded > 0)
                context.Warn($"{reencoded} RefPack (0xFFFF) resource(s) re-encoded as ZLIB (0x5A42)");
        }

        if (Directory.Exists(resourcesDir))
        {
            var extras = new List<(ResourceKey Key, int Dup, string Name, string Path)>();
            foreach (var file in Directory.EnumerateFiles(resourcesDir))
            {
                string full = Path.GetFullPath(file);
                if (used.Contains(full)) continue;
                string name = Path.GetFileName(file);
                if (!KeyFormat.TryParseFileName(name, out var key, out int dup))
                {
                    context.Warn($"{PackageManifest.ResourcesDirectory}/{name}: not named TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII[.n][.ext], skipped");
                    continue;
                }
                extras.Add((key, dup, name, full));
            }
            extras.Sort((a, b) =>
            {
                int c = a.Key.CompareTo(b.Key);
                if (c != 0) return c;
                c = a.Dup.CompareTo(b.Dup);
                return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
            });

            foreach (var (key, _, name, full) in extras)
            {
                ct.ThrowIfCancellationRequested();
                byte[] data = await ReadResourceFileAsync(full, ct).ConfigureAwait(false);
                if (manifest != null && package.Find(key) != null)
                    context.Warn($"{PackageManifest.ResourcesDirectory}/{name}: key {KeyFormat.Compact(key)} already in the package, added as a duplicate");
                var entry = package.AddResource(key, data, rejectDuplicates: false)!;
                if (compressNew)
                    package.SetCompression(entry, ZlibCompressionType);
                added++;
                if (manifest != null)
                    context.Err.WriteLine($"added: {PackageManifest.ResourcesDirectory}/{name}");
            }
        }

        if (package.ResourceCount > PackageLimits.MaxResourceCount)
            throw new CliException($"too many resources ({package.ResourceCount}, max {PackageLimits.MaxResourceCount})");

        await SaveAtomicallyAsync(package, output, ct).ConfigureAwait(false);
        return new PackResult(package.ResourceCount, fromManifest, edited, added, dropped, reencoded);
    }

    // Index compression labels (Core's Compressor constants are internal).
    private const ushort RefPackCompressionType = 0xFFFF;
    internal const ushort ZlibCompressionType = 0x5A42;

    /// <summary>Resolves a manifest-relative path, refusing anything that escapes <paramref name="root"/>.</summary>
    /// <exception cref="CliException">Rooted path or path outside the directory.</exception>
    internal static string ResolveInside(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new CliException($"manifest file path '{relative}' must be relative to the unpack directory");
        string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        string prefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            throw new CliException($"manifest file path '{relative}' points outside the unpack directory");
        return full;
    }

    /// <exception cref="CliException">File larger than a package resource may be.</exception>
    internal static async Task<byte[]> ReadResourceFileAsync(string path, CancellationToken cancellationToken)
    {
        long length = new FileInfo(path).Length;
        if (length > PackageLimits.MaxResourceSize)
            throw new CliException($"{path}: {length:N0} bytes exceeds the {PackageLimits.MaxResourceSize:N0}-byte resource limit");
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes to a temporary file next to <paramref name="output"/> and moves it into place.</summary>
    internal static async Task SaveAtomicallyAsync(DbpfPackage package, string output, CancellationToken cancellationToken)
    {
        string fullOutput = Path.GetFullPath(output);
        string? parent = Path.GetDirectoryName(fullOutput);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        string temp = fullOutput + ".tmp-" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            await package.SaveAsAsync(temp, cancellationToken).ConfigureAwait(false);
            File.Move(temp, fullOutput, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
