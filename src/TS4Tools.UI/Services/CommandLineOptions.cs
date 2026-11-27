namespace TS4Tools.UI.Services;

/// <summary>
/// The parsed command line: an optional package to open plus the legacy options.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 401-545 (CmdLine, CmdLineTest,
/// CmdLineImport, CmdLineHelp). Options are "test", "import" and "help" prefixed with '-' (or '/').
/// </remarks>
public sealed class CommandLineOptions
{
    private static readonly string[] KnownOptions = ["test", "import", "help"];

    /// <summary>Help text listing the command line options.</summary>
    /// <remarks>Source: MainForm.cs lines 537-553 (CmdLineHelp)</remarks>
    public const string HelpText =
        "The following command line options are available:\n\n" +
        "test  --  Enable facilities still undergoing initial testing\n" +
        "import  --  Import a batch of files into a new package\n" +
        "help  --  Display this help\n\n" +
        "Options must be prefixed with '/' or '-'\n\n\n" +
        "A fully-qualified package name can also be supplied on the command line.";

    /// <summary>The package to open, or null.</summary>
    public string? PackagePath { get; private set; }

    /// <summary>Files named after -import.</summary>
    public IReadOnlyList<string> ImportFiles { get; private set; } = [];

    /// <summary>Whether -test was given.</summary>
    public bool Test { get; private set; }

    /// <summary>Whether -help was given.</summary>
    public bool ShowHelp { get; private set; }

    /// <summary>Problems found on the command line (legacy shows each and exits).</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    /// <param name="args">The program arguments.</param>
    /// <param name="fileExists">Tests whether a file exists (replaceable for tests).</param>
    public static CommandLineOptions Parse(IEnumerable<string> args, Func<string, bool>? fileExists = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        fileExists ??= File.Exists;

        var result = new CommandLineOptions();
        var errors = new List<string>();
        var importFiles = new List<string>();
        var queue = new Queue<string>(args);

        while (queue.Count > 0)
        {
            var arg = queue.Dequeue();
            if (arg.Length == 0) continue;

            if (IsOption(arg, fileExists))
            {
                var option = arg[1..].ToLowerInvariant();
                switch (option)
                {
                    case "test":
                        result.Test = true;
                        break;
                    case "help":
                        result.ShowHelp = true;
                        break;
                    case "import":
                        if (queue.Count == 0)
                        {
                            errors.Add("-import requires one or more files");
                        }

                        // Source: MainForm.cs lines 521-535 (files up to the next option)
                        while (queue.Count > 0 && !IsOption(queue.Peek(), fileExists))
                        {
                            var file = queue.Dequeue();
                            if (!fileExists(file))
                            {
                                errors.Add("File not found:\n" + file);
                                continue;
                            }

                            importFiles.Add(file);
                        }

                        break;
                    default:
                        errors.Add($"Invalid command line option: '{arg[1..]}'");
                        break;
                }

                continue;
            }

            // Source: MainForm.cs lines 480-520 (one existing package)
            if (result.PackagePath != null)
            {
                errors.Add("Can only accept one package on command line");
            }
            else if (!fileExists(arg))
            {
                errors.Add("File not found:\n" + arg);
            }
            else
            {
                result.PackagePath = arg;
            }
        }

        result.ImportFiles = importFiles;
        result.Errors = errors;
        return result;
    }

    // A leading '-' always starts an option. A leading '/' does only when it names a known option,
    // because on Linux and macOS every absolute path starts with '/'.
    private static bool IsOption(string arg, Func<string, bool> fileExists)
    {
        if (arg.Length < 2) return false;
        if (arg[0] == '-') return true;
        return arg[0] == '/' && KnownOptions.Contains(arg[1..], StringComparer.OrdinalIgnoreCase) && !fileExists(arg);
    }
}
