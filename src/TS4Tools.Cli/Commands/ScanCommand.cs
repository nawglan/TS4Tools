// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 53-67 (wrapper resolution being audited)
// See Scan/ScanEngine.cs for the classification.

using TS4Tools.Cli.Scan;

namespace TS4Tools.Cli.Commands;

/// <summary>
/// <c>ts4tools scan &lt;path&gt;... [--json f] [--markdown f] [--roundtrip] [--parallel N] [--types ..]</c>.
/// </summary>
internal static class ScanCommand
{
    public static readonly CommandSpec Spec = new(
        "scan",
        "ts4tools scan <path>... [--json <file>] [--markdown <file>] [--roundtrip] [--parallel N] [--types 0xT,0xT] [--all] [--quiet]",
        "Report which resources TS4Tools wrappers cannot handle, per resource type.",
        """
        Every *.package under the given paths (recursive; a path may also be one package) is opened
        and every resource is classified:
          ok                  a TS4Tools wrapper parsed it
          unwrapped           no TS4Tools wrapper for the type (raw bytes only; data is not read)
          unsupported-version the wrapper refused a newer format version (version recorded)
          parse-error         the wrapper threw (message recorded)
          roundtrip-mismatch  with --roundtrip: re-serialised bytes differ from the original
          empty               zero-length resource (e.g. 0xFFE0 deletion record); not a problem
        The summary lists, per type: tag, wrapper, counts per class, the unsupported-version
        histogram and up to 3 sample keys+packages per problem class. Packages that fail to open
        are listed. Exit code 1 if any resource is unwrapped,
        unsupported-version, parse-error or roundtrip-mismatch, or a package failed to open.

        Options:
          --json <file>       Write the full report as JSON.
          --markdown <file>   Write the report as Markdown.
          --roundtrip         Also check that wrapper Data reproduces the original bytes.
          --parallel N        Packages scanned concurrently (default: CPU count).
          --types 0xT,0xT     Only scan these resource types.
          --all               Console table lists every type, not only those with problems.
          --quiet             No progress on stderr.
        """,
        1, int.MaxValue, ["--roundtrip", "--all", "--quiet"], ["--json", "--markdown", "--parallel", "--types"]);

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        var options = new ScanOptions(
            args.Has("--roundtrip"),
            UnpackCommand.ParseParallel(args.Get("--parallel"), Environment.ProcessorCount),
            args.Get("--types") is { } types ? KeyFormat.ParseTypeList(types) : null);

        var registry = ScanEngine.CreateRegistry();
        bool quiet = args.Has("--quiet");
        var lastReport = Stopwatch.StartNew();
        void Progress(int done, int total)
        {
            if (quiet) return;
            if (done == total || lastReport.Elapsed.TotalSeconds >= 5)
            {
                context.Err.WriteLine($"scan: {done}/{total} packages");
                lastReport.Restart();
            }
        }

        var report = await ScanEngine.ScanAsync(args.Positionals, options, registry, Progress, context.CancellationToken)
            .ConfigureAwait(false);

        ScanOutput.WriteConsole(report, context.Out, args.Has("--all"));
        if (args.Get("--json") is { } json)
        {
            CreateParent(json);
            await ScanOutput.WriteJsonAsync(report, json, context.CancellationToken).ConfigureAwait(false);
            context.Out.WriteLine($"JSON report: {json}");
        }
        if (args.Get("--markdown") is { } md)
        {
            CreateParent(md);
            await ScanOutput.WriteMarkdownAsync(report, md, context.CancellationToken).ConfigureAwait(false);
            context.Out.WriteLine($"Markdown report: {md}");
        }
        return report.HasProblems ? ExitCodes.Failure : ExitCodes.Success;
    }

    private static void CreateParent(string file)
    {
        string? parent = Path.GetDirectoryName(Path.GetFullPath(file));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
    }
}
