// Command dispatcher for the ts4tools CLI. No legacy equivalent: s4pe (legacy_references/Sims4Tools/s4pe/) is GUI-only;
// each command documents the legacy code its behaviour follows.

using TS4Tools.Cli.Commands;

namespace TS4Tools.Cli;

/// <summary>Output streams and cancellation shared by the commands.</summary>
internal sealed record CliContext(TextWriter Out, TextWriter Err, CancellationToken CancellationToken)
{
    public void Warn(string message) => Err.WriteLine("warning: " + message);
}

/// <summary>Entry point logic: parses the command name, dispatches and maps errors to exit codes.</summary>
internal static class CliApp
{
    private delegate Task<int> CommandHandler(ParsedArgs args, CliContext context);

    private static readonly (CommandSpec Spec, CommandHandler Handler)[] Commands =
    [
        (InfoCommand.Spec, InfoCommand.RunAsync),
        (ListCommand.Spec, ListCommand.RunAsync),
        (UnpackCommand.Spec, UnpackCommand.RunAsync),
        (PackCommand.Spec, PackCommand.RunAsync),
        (CompareCommand.Spec, CompareCommand.RunAsync),
        (ScanCommand.Spec, ScanCommand.RunAsync),
        (ResourceCommands.ExtractSpec, ResourceCommands.ExtractAsync),
        (ResourceCommands.ReplaceSpec, ResourceCommands.ReplaceAsync),
    ];

    public static async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        var context = new CliContext(stdout, stderr, cancellationToken);
        if (args.Count == 0)
        {
            stderr.Write(GeneralHelp());
            return ExitCodes.Usage;
        }

        string name = args[0];
        if (name is "-h" or "--help" or "help")
        {
            if (name == "help" && args.Count > 1)
            {
                var target = Find(args[1]);
                if (target is null)
                    return UsageError(stderr, $"unknown command '{args[1]}'");
                stdout.Write(CommandHelp(target.Value.Spec));
                return ExitCodes.Success;
            }
            stdout.Write(GeneralHelp());
            return ExitCodes.Success;
        }
        if (name == "--version")
        {
            stdout.WriteLine("ts4tools " + (typeof(CliApp).Assembly.GetName().Version?.ToString() ?? "0.0.0"));
            return ExitCodes.Success;
        }

        var command = Find(name);
        if (command is null)
            return UsageError(stderr, $"unknown command '{name}'");

        var (spec, handler) = command.Value;
        try
        {
            var parsed = ParsedArgs.Parse(spec, args.Skip(1).ToList());
            if (parsed.HelpRequested)
            {
                stdout.Write(CommandHelp(spec));
                return ExitCodes.Success;
            }
            return await handler(parsed, context).ConfigureAwait(false);
        }
        catch (UsageException ex)
        {
            return UsageError(stderr, ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stderr.WriteLine("error: cancelled");
            return ExitCodes.Failure;
        }
        catch (Exception ex) when (ex is CliException or IOException or UnauthorizedAccessException
                                       or PackageFormatException or ResourceFormatException)
        {
            stderr.WriteLine("error: " + ex.Message);
            return ExitCodes.Failure;
        }
    }

    private static (CommandSpec Spec, CommandHandler Handler)? Find(string name)
    {
        foreach (var c in Commands)
            if (c.Spec.Name == name)
                return c;
        return null;
    }

    private static int UsageError(TextWriter stderr, string message)
    {
        stderr.WriteLine("error: " + message);
        stderr.WriteLine("Run 'ts4tools --help' for usage.");
        return ExitCodes.Usage;
    }

    public static string GeneralHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ts4tools - headless Sims 4 .package tool (TS4Tools)");
        sb.AppendLine();
        sb.AppendLine("Usage: ts4tools <command> [arguments] [options]");
        sb.AppendLine("       ts4tools <command> --help");
        sb.AppendLine();
        sb.AppendLine("Commands:");
        foreach (var (spec, _) in Commands)
            sb.Append("  ").Append(spec.Name.PadRight(9)).Append(' ').AppendLine(spec.Summary);
        sb.AppendLine();
        sb.AppendLine("Resource keys are written TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII (hex); 0xT-0xG-0xI and");
        sb.AppendLine("'T:0x.. G:0x.. I:0x..' are also accepted.");
        sb.AppendLine();
        sb.AppendLine("Exit codes: 0 success, 1 failure (or issues found by scan/compare), 2 usage error.");
        return sb.ToString();
    }

    public static string CommandHelp(CommandSpec spec)
    {
        var sb = new StringBuilder();
        sb.Append("Usage: ").AppendLine(spec.Usage);
        sb.AppendLine();
        sb.AppendLine(spec.Summary);
        if (!string.IsNullOrEmpty(spec.Help))
        {
            sb.AppendLine();
            sb.Append(spec.Help.TrimEnd()).AppendLine();
        }
        return sb.ToString();
    }
}
