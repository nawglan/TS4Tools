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
        _helpers.Clear();
        _resourceTypeIndex.Clear();

        await LoadHelpersFromDirectoryAsync();
        BuildResourceTypeIndex();

        _logger.LogInformation("Loaded {Count} helper tools", _helpers.Count);
    }

    private async Task LoadHelpersFromDirectoryAsync()
    {
        // Look for .helper files in common locations
        var searchPaths = GetHelperSearchPathsCore();

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                _logger.LogDebug("Helper directory not found: {Path}", searchPath);
                continue;
            }

            _logger.LogDebug("Searching for helpers in: {Path}", searchPath);

            var helperFiles = Directory.GetFiles(searchPath, "*.helper", SearchOption.AllDirectories);
            foreach (var helperFile in helperFiles)
            {
                try
                {
                    var helperInfo = await ParseHelperFileAsync(helperFile);
                    if (helperInfo != null)
                    {
                        _helpers[helperInfo.Id] = helperInfo;
                        _logger.LogDebug("Loaded helper: {Id} - {Label}", helperInfo.Id, helperInfo.Label);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse helper file: {File}", helperFile);
                }
            }
        }
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

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        string? line;
        var inCommentBlock = false;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            line = line.Trim();

            // Handle comment blocks
            if (inCommentBlock)
            {
                var commentEnd = line.IndexOf("*/", StringComparison.Ordinal);
                if (commentEnd > -1)
                {
                    // Block comment ends on this line - keep the part after */
                    line = line.Substring(commentEnd + 2).Trim();
                    inCommentBlock = false;
                    // If nothing left after removing comment, skip this line
                    if (string.IsNullOrEmpty(line))
                        continue;
                }
                else
                {
                    // Still in comment block
                    continue;
                }
            }

            // Handle single-line comments (start of line)
            if (line.StartsWith("//") || line.StartsWith("#") || line.StartsWith(";") || string.IsNullOrEmpty(line))
                continue;

            // Handle inline single-line comments
            var commentMarkers = new[] { "#", ";", "//" };
            foreach (var marker in commentMarkers)
            {
                var commentIndex = line.IndexOf(marker, StringComparison.Ordinal);
                if (commentIndex > -1)
                {
                    line = line.Substring(0, commentIndex).Trim();
                    break;
                }
            }

            // Handle comment block start/end (including inline)
            while (true)
            {
                var commentStart = line.IndexOf("/*", StringComparison.Ordinal);
                if (commentStart > -1)
                {
                    var beforeComment = line.Substring(0, commentStart);
                    var afterComment = line.Substring(commentStart + 2);

                    var commentEnd = afterComment.IndexOf("*/", StringComparison.Ordinal);
                    if (commentEnd > -1)
                    {
                        // Inline block comment
                        line = beforeComment + afterComment.Substring(commentEnd + 2);
                        continue; // Check for more block comments on the same line
                    }
                    else
                    {
                        // Block comment starts but doesn't end on this line
                        line = beforeComment.Trim();
                        inCommentBlock = true;
                        break;
                    }
                }
                break;
            }

            if (string.IsNullOrEmpty(line))
                continue;

            // Parse key-value pairs
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var key = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();

            if (string.Equals(key, "ResourceType", StringComparison.OrdinalIgnoreCase))
            {
                // Parse resource types
                var types = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var type in types)
                {
                    if (TryParseResourceType(type, out var resourceType))
                    {
                        resourceTypes.Add(resourceType);
                    }
                }
            }
            else
            {
                properties[key] = value;
            }
        }

        // Must have required fields
        if (!properties.ContainsKey("Label") || !properties.ContainsKey("Command"))
        {
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

        // Resolve executable path
        var executablePath = ResolveExecutablePath(command, filePath);
        var isAvailable = !string.IsNullOrEmpty(executablePath) && File.Exists(executablePath);

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

    private static bool TryParseResourceType(string input, out uint resourceType)
    {
        resourceType = 0;

        if (string.IsNullOrEmpty(input))
            return false;

        // Handle hex format (0x12345678)
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(input.Substring(2), NumberStyles.HexNumber, null, out resourceType);
        }

        // Handle direct hex
        if (input.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
        {
            return uint.TryParse(input, NumberStyles.HexNumber, null, out resourceType);
        }

        // Handle decimal
        return uint.TryParse(input, out resourceType);
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
