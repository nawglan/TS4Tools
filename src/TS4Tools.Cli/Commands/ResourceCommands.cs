// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1866-1880 (ExportFile: write one resource's decompressed bytes)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1634-1690 (ResourceReplace: package.ReplaceResource(rie, res))
// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 54-88 (SavePackage: in-place save)

using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary>Single-resource <c>extract</c> and <c>replace</c>.</summary>
internal static class ResourceCommands
{
    public static readonly CommandSpec ExtractSpec = new(
        "extract",
        "ts4tools extract <package> <key> <file>",
        "Write one resource's decompressed bytes to a file.",
        """
        <key> is TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII (also 0xT-0xG-0xI, 'T:0x.. G:0x.. I:0x..' or an
        unpack file name). The first index entry with that key is used.
        """,
        3, 3, [], []);

    public static readonly CommandSpec ReplaceSpec = new(
        "replace",
        "ts4tools replace <package> <key> <file> [--output <package>] [--force]",
        "Replace one resource's data with a file's bytes (in place, or into a new package).",
        """
        The resource keeps its compression label (0x5A42 entries are re-compressed). Without
        --output the package is saved in place; otherwise it is copied to --output first and the
        original is left untouched.

        Options:
          --output <package>  Write the result here instead of modifying <package>.
          --force             Overwrite an existing --output package.
        """,
        3, 3, ["--force"], ["--output"]);

    public static async Task<int> ExtractAsync(ParsedArgs args, CliContext context)
    {
        var key = KeyFormat.ParseKey(args.Positionals[1]);
        string file = args.Positionals[2];
        await using var package = await PackageFiles.OpenReadAsync(args.Positionals[0], context.CancellationToken)
            .ConfigureAwait(false);
        var entry = package.Find(key) ?? throw new CliException($"resource {KeyFormat.Compact(key)} not found");
        var data = await package.GetResourceDataAsync(entry, context.CancellationToken).ConfigureAwait(false);
        await using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
        {
            await fs.WriteAsync(data, context.CancellationToken).ConfigureAwait(false);
        }
        context.Out.WriteLine($"Extracted {KeyFormat.Compact(key)} ({data.Length:N0} bytes) to {file}");
        return ExitCodes.Success;
    }

    public static async Task<int> ReplaceAsync(ParsedArgs args, CliContext context)
    {
        var ct = context.CancellationToken;
        string source = args.Positionals[0];
        var key = KeyFormat.ParseKey(args.Positionals[1]);
        string file = args.Positionals[2];
        string? output = args.Get("--output");

        if (!File.Exists(source))
            throw new CliException($"package not found: {source}");
        if (!File.Exists(file))
            throw new CliException($"file not found: {file}");
        byte[] data = await PackCommand.ReadResourceFileAsync(file, ct).ConfigureAwait(false);

        string target = source;
        if (output != null)
        {
            if (string.Equals(Path.GetFullPath(output), Path.GetFullPath(source),
                    OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                throw new UsageException("--output is the same file as <package>; omit --output to replace in place");
            if (File.Exists(output) && !args.Has("--force"))
                throw new CliException($"output package exists: {output} (use --force to overwrite)");
            // Validate the source before copying it.
            await using (var check = await PackageFiles.OpenReadAsync(source, ct).ConfigureAwait(false))
            {
                if (check.Find(key) is null)
                    throw new CliException($"resource {KeyFormat.Compact(key)} not found");
            }
            File.Copy(source, output, overwrite: true);
            target = output;
        }

        DbpfPackage package;
        try
        {
            package = await DbpfPackage.OpenAsync(target, readWrite: true, ct).ConfigureAwait(false);
        }
        catch (PackageFormatException ex)
        {
            throw new CliException($"{target}: not a valid package: {ex.Message}");
        }
        await using (package.ConfigureAwait(false))
        {
            var entry = package.Find(key) ?? throw new CliException($"resource {KeyFormat.Compact(key)} not found");
            package.ReplaceResource(entry, data);
            await package.SaveAsync(ct).ConfigureAwait(false);
        }
        context.Out.WriteLine($"Replaced {KeyFormat.Compact(key)} with {data.Length:N0} bytes from {file} in {target}");
        return ExitCodes.Success;
    }
}
