using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Helpers;

/// <summary>
/// Cross-platform helper tool service for executing external tools
/// </summary>
public class HelperToolService : IHelperToolService
{
    private readonly ILogger<HelperToolService> _logger;
    private readonly Dictionary<string, HelperToolInfo> _helpers = new();
    private readonly Dictionary<uint, List<HelperToolInfo>> _resourceTypeIndex = new();

    /// <summary>
    /// Virtual method for debug output that can be overridden in tests
    /// </summary>
    /// <param name="message">Debug message to output</param>
    protected virtual void WriteDebug(string message)
    {
        Console.WriteLine(message);
    }

    private static readonly string[] ReservedKeywords =
    {
        "wrapper", "label", "desc", "command", "arguments", "readonly", "ignorewritetimestamp"
    };

    /// <summary>
    /// Initializes a new instance of the HelperToolService class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public HelperToolService(ILogger<HelperToolService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<HelperToolResult> ExecuteAsync(string helperName, string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(helperName);
        ArgumentNullException.ThrowIfNull(args);

        if (!_helpers.TryGetValue(helperName, out var helperInfo))
        {
            var message = $"Helper tool '{helperName}' not found";
            _logger.LogWarning(message);
            return HelperToolResult.Failure(-1, string.Empty, message, TimeSpan.Zero);
        }

        if (!helperInfo.IsAvailable)
        {
            var message = $"Helper tool '{helperName}' is not available";
            _logger.LogWarning(message);
            return HelperToolResult.Failure(-1, string.Empty, message, TimeSpan.Zero);
        }

        return await ExecuteHelperToolAsync(helperInfo, args, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HelperToolResult> ExecuteForResourceAsync(string helperName, IResource resource, string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(helperName);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(args);

        if (!_helpers.TryGetValue(helperName, out var helperInfo))
        {
            var message = $"Helper tool '{helperName}' not found";
            _logger.LogWarning(message);
            return HelperToolResult.Failure(-1, string.Empty, message, TimeSpan.Zero);
        }

        // Check if helper supports this resource type
        // Note: Resource type validation is skipped for now since IResource doesn't expose ResourceType
        // This could be enhanced in the future when IResource interface is extended
        // var resourceType = uint.Parse(resource.ResourceType.ToString("X8"), System.Globalization.NumberStyles.HexNumber);
        // if (helperInfo.SupportedResourceTypes.Count > 0 && !helperInfo.SupportedResourceTypes.Contains(resourceType))
        // {
        //     var message = $"Helper tool '{helperName}' does not support resource type 0x{resourceType:X8}";
        //     _logger.LogWarning(message);
        //     return HelperToolResult.Failure(-1, string.Empty, message, TimeSpan.Zero);
        // }

        // Create temporary file with resource data
        var tempFile = Path.GetTempFileName();
        try
        {
            await using var stream = resource.Stream;
            await using var fileStream = File.Create(tempFile);
            await stream.CopyToAsync(fileStream, cancellationToken);

            // Prepare arguments with resource file
            var processArgs = new List<string> { tempFile };
            processArgs.AddRange(args);

            return await ExecuteHelperToolAsync(helperInfo, processArgs.ToArray(), cancellationToken);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary file: {TempFile}", tempFile);
            }
        }
    }

    /// <inheritdoc />
    public bool IsHelperToolAvailable(string helperName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(helperName);
        return _helpers.TryGetValue(helperName, out var helper) && helper.IsAvailable;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableHelperTools()
    {
        return _helpers.Values
            .Where(h => h.IsAvailable)
            .Select(h => h.Id)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<HelperToolInfo> GetHelpersForResourceType(uint resourceType)
    {
        if (_resourceTypeIndex.TryGetValue(resourceType, out var helpers))
        {
            return helpers.Where(h => h.IsAvailable).ToList();
        }

        return Array.Empty<HelperToolInfo>();
    }

    /// <inheritdoc />
    public async Task ReloadHelpersAsync()
    {
        try
        {
            // DEBUG: Add debugging for cross-platform failures
            WriteDebug("=== DEBUG: ReloadHelpersAsync ===");
            WriteDebug($"Operating System: {Environment.OSVersion}");
            WriteDebug($"Current Directory: {Environment.CurrentDirectory}");

            _helpers.Clear();
            _resourceTypeIndex.Clear();

            await LoadHelpersFromDirectoryAsync();
            BuildResourceTypeIndex();

            WriteDebug($"Total helpers loaded: {_helpers.Count}");
            foreach (var helper in _helpers)
            {
                WriteDebug($"  Helper: {helper.Key} -> {helper.Value.Label} (ResourceTypes: {helper.Value.SupportedResourceTypes.Count})");
            }
            WriteDebug("=== END DEBUG: ReloadHelpersAsync ===");

            _logger.LogInformation("Loaded {Count} helper tools", _helpers.Count);
        }
        catch (Exception ex)
        {
            WriteDebug($"=== ERROR in ReloadHelpersAsync: {ex.GetType().Name}: {ex.Message} ===");
            WriteDebug($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task LoadHelpersFromDirectoryAsync()
    {
        // Look for .helper files in common locations
        var searchPaths = GetHelperSearchPathsCore();

        WriteDebug("=== DEBUG: LoadHelpersFromDirectoryAsync ===");
        WriteDebug($"Search paths: [{string.Join(", ", searchPaths)}]");

        foreach (var searchPath in searchPaths)
        {
            WriteDebug($"Checking search path: '{searchPath}'");
            if (!Directory.Exists(searchPath))
            {
                WriteDebug($"  Directory does not exist");
                _logger.LogDebug("Helper directory not found: {Path}", searchPath);
                continue;
            }

            WriteDebug($"  Directory exists, searching for *.helper files");
            _logger.LogDebug("Searching for helpers in: {Path}", searchPath);

            var helperFiles = Directory.GetFiles(searchPath, "*.helper", SearchOption.AllDirectories);
            WriteDebug($"  Found {helperFiles.Length} helper files: [{string.Join(", ", helperFiles)}]");

            foreach (var helperFile in helperFiles)
            {
                WriteDebug($"  Processing helper file: '{helperFile}'");
                try
                {
                    var helperInfo = await ParseHelperFileAsync(helperFile);
                    if (helperInfo != null)
                    {
                        _helpers[helperInfo.Id] = helperInfo;
                        WriteDebug($"    Successfully loaded helper: {helperInfo.Id} - {helperInfo.Label}");
                        _logger.LogDebug("Loaded helper: {Id} - {Label}", helperInfo.Id, helperInfo.Label);
                    }
                    else
                    {
                        WriteDebug($"    Helper parsing returned null");
                    }
                }
                catch (Exception ex)
                {
                    WriteDebug($"    Exception parsing helper file: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to parse helper file: {File}", helperFile);
                }
            }
        }
        WriteDebug("=== END DEBUG: LoadHelpersFromDirectoryAsync ===");
    }

    /// <summary>
    /// Gets the directories to search for helper tools
    /// </summary>
    /// <returns>Enumerable of directory paths to search</returns>
    protected virtual IEnumerable<string> GetHelperSearchPathsCore()
    {
        // Current assembly directory + Helpers
        var assemblyDir = Path.GetDirectoryName(typeof(HelperToolService).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            yield return Path.Combine(assemblyDir, "Helpers");
        }

        // Application directory + Helpers
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(appDir))
        {
            yield return Path.Combine(appDir, "Helpers");
        }

        // Working directory + Helpers
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Helpers");

        // Legacy Sims4Tools location (for migration compatibility)
        var legacyPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Sims4Tools", "s4pe", "Helpers");
        if (Directory.Exists(legacyPath))
        {
            yield return legacyPath;
        }
    }

    private async Task<HelperToolInfo?> ParseHelperFileAsync(string filePath)
    {
        var properties = new Dictionary<string, string>();
        var resourceTypes = new List<uint>();

        // DEBUG: Add debugging to understand cross-platform parsing failures
        Console.WriteLine($"=== DEBUG: Parsing helper file: {filePath} ===");

        // Read entire file content and process comments first
        var fileContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        Console.WriteLine("Original file content:");
        Console.WriteLine($"[{fileContent}]");

        var processedContent = RemoveComments(fileContent);
        Console.WriteLine("Processed content after comment removal:");
        Console.WriteLine($"[{processedContent}]");

        using var reader = new StringReader(processedContent);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            Console.WriteLine($"Processing line: '{line}' (original length: {line?.Length ?? 0})");

            if (string.IsNullOrEmpty(line))
            {
                Console.WriteLine("  -> Skipped: empty line");
                continue;
            }

            // Parse key-value pairs
            var colonIndex = line.IndexOf(':');
            Console.WriteLine($"  -> Colon found at index: {colonIndex}");
            if (colonIndex <= 0)
            {
                Console.WriteLine($"  -> Skipped: no colon found or at start (colonIndex={colonIndex})");
                continue;
            }

            var key = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();
            Console.WriteLine($"  -> Parsed: '{key}' = '{value}' (key length: {key.Length}, value length: {value.Length})");

            if (string.Equals(key, "ResourceType", StringComparison.OrdinalIgnoreCase))
            {
                // Parse resource types
                var types = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"    -> ResourceType parts: [{string.Join(", ", types)}] (count: {types.Length})");
                foreach (var type in types)
                {
                    Console.WriteLine($"    -> Attempting to parse ResourceType: '{type}' (length: {type.Length})");
                    if (TryParseResourceType(type, out var resourceType))
                    {
                        resourceTypes.Add(resourceType);
                        Console.WriteLine($"    -> SUCCESS: Added ResourceType: 0x{resourceType:X8}");
                    }
                    else
                    {
                        Console.WriteLine($"    -> FAILED to parse ResourceType: '{type}'");
                    }
                }
            }
            else
            {
                properties[key] = value;
                Console.WriteLine($"    -> Added property: '{key}' = '{value}'");
            }
        }

        Console.WriteLine($"Final parsed properties: {properties.Count} items");
        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.Key} = '{prop.Value}'");
        }
        Console.WriteLine($"Final parsed resource types: [{string.Join(", ", resourceTypes.Select(rt => $"0x{rt:X8}"))}]");

        // Must have required fields
        if (!properties.ContainsKey("Label") || !properties.ContainsKey("Command"))
        {
            Console.WriteLine($"ERROR: Missing required fields. Has Label: {properties.ContainsKey("Label")}, Has Command: {properties.ContainsKey("Command")}");
            _logger?.LogWarning("Helper file missing required fields (Label, Command): {File}", filePath);
            return null;
        }

        var id = properties.GetValueOrDefault("file", Path.GetFileNameWithoutExtension(filePath));
        var label = properties["Label"];
        var command = properties["Command"];
        var arguments = properties.GetValueOrDefault("Arguments", string.Empty);
        var description = properties.GetValueOrDefault("Desc", string.Empty);
        var isReadOnly = bool.Parse(properties.GetValueOrDefault("ReadOnly", "false"));
        var ignoreWriteTimestamp = bool.Parse(properties.GetValueOrDefault("IgnoreWriteTimestamp", "false"));

        Console.WriteLine($"Creating HelperToolInfo with ID: '{id}', Label: '{label}', Command: '{command}'");

        // Resolve executable path
        var executablePath = ResolveExecutablePath(command, filePath);
        var isAvailable = !string.IsNullOrEmpty(executablePath) && File.Exists(executablePath);

        Console.WriteLine($"Executable path: '{executablePath}', IsAvailable: {isAvailable}");
        Console.WriteLine("=== END DEBUG ===");

        return new HelperToolInfo
        {
            Id = id,
            Label = label,
            Description = description,
            Command = command,
            Arguments = arguments,
            SupportedResourceTypes = resourceTypes,
            IsReadOnly = isReadOnly,
            IgnoreWriteTimestamp = ignoreWriteTimestamp,
            ExecutablePath = executablePath,
            IsAvailable = isAvailable,
            Properties = properties
        };
    }

    private static string RemoveComments(string content)
    {
        Console.WriteLine("=== DEBUG: RemoveComments ===");
        Console.WriteLine($"Input content: [{content}]");

        var result = new StringBuilder();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        Console.WriteLine($"Split into {lines.Length} lines");

        var inBlockComment = false;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var line = rawLine;
            Console.WriteLine($"Line {lineIndex}: '{rawLine}' (inBlockComment: {inBlockComment})");

            if (inBlockComment)
            {
                var commentEndIndex = line.IndexOf("*/", StringComparison.Ordinal);
                Console.WriteLine($"  Looking for */ in block comment, found at: {commentEndIndex}");
                if (commentEndIndex >= 0)
                {
                    line = line.Substring(commentEndIndex + 2);
                    inBlockComment = false;
                    Console.WriteLine($"  Exited block comment, remaining line: '{line}'");
                }
                else
                {
                    // Entire line is in block comment
                    Console.WriteLine($"  Entire line is in block comment, adding empty line");
                    result.AppendLine();
                    continue;
                }
            }

            // Process the line for comments
            var processedLine = new StringBuilder();
            var i = 0;

            while (i < line.Length)
            {
                // Check for block comment start
                if (i < line.Length - 1 && line[i] == '/' && line[i + 1] == '*')
                {
                    Console.WriteLine($"  Found /* at position {i}");
                    var commentEndIndex = line.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    Console.WriteLine($"    Looking for closing */ from position {i + 2}, found at: {commentEndIndex}");
                    if (commentEndIndex >= 0)
                    {
                        // Inline block comment - skip over it
                        i = commentEndIndex + 2;
                        Console.WriteLine($"    Skipped inline block comment, continuing from position {i}");
                    }
                    else
                    {
                        // Block comment starts but doesn't end on this line
                        inBlockComment = true;
                        Console.WriteLine($"    Block comment starts, setting inBlockComment=true");
                        break;
                    }
                }
                // Check for single-line comments
                else if ((i < line.Length - 1 && line[i] == '/' && line[i + 1] == '/') ||
                         line[i] == '#' ||
                         line[i] == ';')
                {
                    // Rest of line is comment
                    Console.WriteLine($"  Found single-line comment at position {i} ('{line[i]}{(i < line.Length - 1 ? line[i + 1].ToString() : "")}')");
                    break;
                }
                else
                {
                    processedLine.Append(line[i]);
                    i++;
                }
            }

            var finalLine = processedLine.ToString();
            Console.WriteLine($"  Final processed line: '{finalLine}'");
            result.AppendLine(finalLine);
        }

        var finalResult = result.ToString();
        Console.WriteLine($"Final result: [{finalResult}]");
        Console.WriteLine("=== END DEBUG: RemoveComments ===");
        return finalResult;
    }

    private static bool TryParseResourceType(string input, out uint resourceType)
    {
        resourceType = 0;
        Console.WriteLine($"      -> TryParseResourceType: input='{input}' (length: {input?.Length ?? 0})");

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine($"      -> FAILED: input is null or empty");
            return false;
        }

        // Handle hex format (0x12345678)
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"      -> Attempting hex parse with 0x prefix: '{input.Substring(2)}'");
            var result = uint.TryParse(input.Substring(2), NumberStyles.HexNumber, null, out resourceType);
            Console.WriteLine($"      -> Hex parse result: {result}, value: 0x{resourceType:X8}");
            return result;
        }

        // Handle direct hex
        if (input.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
        {
            Console.WriteLine($"      -> Attempting direct hex parse: '{input}'");
            var result = uint.TryParse(input, NumberStyles.HexNumber, null, out resourceType);
            Console.WriteLine($"      -> Direct hex parse result: {result}, value: 0x{resourceType:X8}");
            return result;
        }

        // Handle decimal
        Console.WriteLine($"      -> Attempting decimal parse: '{input}'");
        var decimalResult = uint.TryParse(input, out resourceType);
        Console.WriteLine($"      -> Decimal parse result: {decimalResult}, value: {resourceType}");
        return decimalResult;
    }

    private static string? ResolveExecutablePath(string command, string helperFilePath)
    {
        // If it's already an absolute path and exists, use it
        if (Path.IsPathRooted(command) && File.Exists(command))
        {
            return command;
        }

        // Try relative to helper file directory
        var helperDir = Path.GetDirectoryName(helperFilePath);
        if (!string.IsNullOrEmpty(helperDir))
        {
            var relativePath = Path.Combine(helperDir, command);
            if (File.Exists(relativePath))
            {
                return Path.GetFullPath(relativePath);
            }
        }

        // Try PATH environment variable for system commands
        if (!command.Contains(Path.DirectorySeparatorChar) && !command.Contains(Path.AltDirectorySeparatorChar))
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                var paths = pathVar.Split(Path.PathSeparator);
                foreach (var path in paths)
                {
                    if (string.IsNullOrEmpty(path))
                        continue;

                    var fullPath = Path.Combine(path, command);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }

                    // Try with .exe extension on Windows
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var exePath = fullPath + ".exe";
                        if (File.Exists(exePath))
                        {
                            return exePath;
                        }
                    }
                }
            }
        }

        return null;
    }

    private void BuildResourceTypeIndex()
    {
        foreach (var helper in _helpers.Values)
        {
            foreach (var resourceType in helper.SupportedResourceTypes)
            {
                if (!_resourceTypeIndex.TryGetValue(resourceType, out var list))
                {
                    list = new List<HelperToolInfo>();
                    _resourceTypeIndex[resourceType] = list;
                }

                list.Add(helper);
            }
        }
    }

    private async Task<HelperToolResult> ExecuteHelperToolAsync(HelperToolInfo helperInfo, string[] args, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = helperInfo.ExecutablePath ?? helperInfo.Command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Prepare arguments
            if (!string.IsNullOrEmpty(helperInfo.Arguments))
            {
                var formattedArgs = FormatArguments(helperInfo.Arguments, args);
                if (!string.IsNullOrEmpty(formattedArgs))
                {
                    processStartInfo.Arguments = formattedArgs;
                }
            }
            else if (args.Length > 0)
            {
                processStartInfo.Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\""));
            }

            _logger.LogDebug("Executing helper tool: {Command} {Arguments}",
                processStartInfo.FileName, processStartInfo.Arguments);

            using var process = new Process { StartInfo = processStartInfo };

            var outputBuffer = new StringBuilder();
            var errorBuffer = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuffer.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuffer.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var executionTime = DateTime.UtcNow - startTime;
            var exitCode = process.ExitCode;
            var standardOutput = outputBuffer.ToString();
            var standardError = errorBuffer.ToString();

            _logger.LogDebug("Helper tool completed with exit code {ExitCode} in {ExecutionTime}ms",
                exitCode, executionTime.TotalMilliseconds);

            return exitCode == 0
                ? HelperToolResult.Success(exitCode, standardOutput, standardError, executionTime)
                : HelperToolResult.Failure(exitCode, standardOutput, standardError, executionTime);
        }
        catch (Exception ex)
        {
            var executionTime = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to execute helper tool: {Command}", helperInfo.Command);
            return HelperToolResult.FromException(ex, executionTime);
        }
    }

    private static string FormatArguments(string argumentTemplate, string[] args)
    {
        var result = argumentTemplate;

        // Replace {} placeholders with arguments
        for (int i = 0; i < args.Length; i++)
        {
            result = result.Replace("{}", $"\"{args[i]}\"", StringComparison.Ordinal);
            result = result.Replace($"{{{i}}}", $"\"{args[i]}\"", StringComparison.Ordinal);
        }

        return result;
    }
}
