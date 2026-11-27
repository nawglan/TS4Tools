// Hand-rolled argument parsing for the ts4tools CLI. There is no legacy equivalent: s4pe is a GUI only.

namespace TS4Tools.Cli;

/// <summary>Bad command-line usage; reported with exit code 2.</summary>
internal sealed class UsageException(string message) : Exception(message);

/// <summary>A runtime failure with a message meant for the user; reported with exit code 1.</summary>
internal sealed class CliException(string message) : Exception(message);

/// <summary>Exit codes shared by every command.</summary>
internal static class ExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;
    public const int Usage = 2;
}

/// <summary>Static description of a command's arguments.</summary>
/// <param name="Name">Command name.</param>
/// <param name="Usage">One-line usage synopsis.</param>
/// <param name="Summary">One-line description for the command list.</param>
/// <param name="Help">Full help text (options, examples).</param>
/// <param name="MinPositionals">Minimum positional arguments.</param>
/// <param name="MaxPositionals">Maximum positional arguments (int.MaxValue for unbounded).</param>
/// <param name="Flags">Boolean options, e.g. "--json".</param>
/// <param name="ValueOptions">Options that take a value, e.g. "--type".</param>
internal sealed record CommandSpec(
    string Name,
    string Usage,
    string Summary,
    string Help,
    int MinPositionals,
    int MaxPositionals,
    IReadOnlyCollection<string> Flags,
    IReadOnlyCollection<string> ValueOptions);

/// <summary>Parsed arguments of one command invocation.</summary>
internal sealed class ParsedArgs
{
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public List<string> Positionals { get; } = [];

    public bool HelpRequested { get; private set; }

    public bool Has(string flag) => _flags.Contains(flag);

    public string? Get(string option) => _values.GetValueOrDefault(option);

    /// <summary>
    /// Parses <paramref name="args"/> against <paramref name="spec"/>. Options may appear anywhere; values are given as
    /// <c>--opt value</c> or <c>--opt=value</c>; <c>--</c> ends option parsing.
    /// </summary>
    /// <exception cref="UsageException">Unknown option, missing value, repeated option or wrong positional count.</exception>
    public static ParsedArgs Parse(CommandSpec spec, IReadOnlyList<string> args)
    {
        var result = new ParsedArgs();
        bool optionsEnded = false;
        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            if (optionsEnded || arg == "-" || !arg.StartsWith('-'))
            {
                result.Positionals.Add(arg);
                continue;
            }
            if (arg == "--")
            {
                optionsEnded = true;
                continue;
            }
            if (arg is "-h" or "--help")
            {
                result.HelpRequested = true;
                continue;
            }

            string name = arg;
            string? inlineValue = null;
            int eq = arg.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                name = arg[..eq];
                inlineValue = arg[(eq + 1)..];
            }

            if (spec.Flags.Contains(name))
            {
                if (inlineValue != null)
                    throw new UsageException($"option {name} does not take a value");
                result._flags.Add(name);
            }
            else if (spec.ValueOptions.Contains(name))
            {
                string value;
                if (inlineValue != null)
                    value = inlineValue;
                else if (i + 1 < args.Count)
                    value = args[++i];
                else
                    throw new UsageException($"option {name} requires a value");
                if (!result._values.TryAdd(name, value))
                    throw new UsageException($"option {name} given more than once");
            }
            else
            {
                throw new UsageException($"unknown option '{name}' for '{spec.Name}'");
            }
        }

        if (result.HelpRequested)
            return result;

        if (result.Positionals.Count < spec.MinPositionals)
            throw new UsageException($"'{spec.Name}' needs {Describe(spec)}; usage: {spec.Usage}");
        if (result.Positionals.Count > spec.MaxPositionals)
            throw new UsageException($"too many arguments for '{spec.Name}'; usage: {spec.Usage}");
        return result;
    }

    private static string Describe(CommandSpec spec) =>
        spec.MinPositionals == spec.MaxPositionals
            ? $"{spec.MinPositionals} argument(s)"
            : $"at least {spec.MinPositionals} argument(s)";
}
