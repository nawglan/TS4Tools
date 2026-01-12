namespace TS4Tools.UI.Services;

/// <summary>
/// Manages external helper programs for editing resources.
/// Parses *.helper configuration files and executes external editors.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs
/// Helper file format: legacy_references/Sims4Tools/s4pe/Helpers/Helpers.txt
/// </remarks>
public sealed class HelperManager
{
    private static readonly string[] ReservedKeywords = ["wrapper", "label", "desc", "command", "arguments", "readonly", "ignorewritetimestamp"];
    private static readonly string[] CommentMarkers = ["#", ";", "//"];

    private static HelperManager? _instance;
    private static readonly object Lock = new();

    private readonly List<HelperDefinition> _allHelpers = [];
    private readonly string _helpersFolder;
    private bool _loaded;

    /// <summary>
    /// Gets the singleton instance of the HelperManager.
    /// </summary>
    public static HelperManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new HelperManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Gets all loaded helper definitions.
    /// </summary>
    public IReadOnlyList<HelperDefinition> AllHelpers
    {
        get
        {
            EnsureLoaded();
            return _allHelpers;
        }
    }

    private HelperManager()
    {
        // Helpers folder is next to the executable
        var appDir = AppContext.BaseDirectory;
        _helpersFolder = Path.Combine(appDir, "Helpers");
    }

    /// <summary>
    /// Reloads all helper definitions from disk.
    /// </summary>
    public void Reload()
    {
        lock (Lock)
        {
            _allHelpers.Clear();
            _loaded = false;
            EnsureLoaded();
        }
    }

    /// <summary>
    /// Gets helpers that match the given resource type and wrapper name.
    /// </summary>
    /// <param name="resourceType">The resource type ID.</param>
    /// <param name="resourceGroup">The resource group ID.</param>
    /// <param name="instance">The resource instance ID.</param>
    /// <param name="wrapperName">The wrapper class name (e.g., "StblResource").</param>
    /// <returns>List of matching helpers.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 140-174
    /// </remarks>
    public List<HelperInstance> GetHelpersForResource(uint resourceType, uint resourceGroup, ulong instance, string? wrapperName)
    {
        EnsureLoaded();

        var result = new List<HelperInstance>();
        var wrapperLower = wrapperName?.ToLowerInvariant() ?? "";

        foreach (var helper in _allHelpers)
        {
            if (HelperMatchesResource(helper, resourceType, resourceGroup, instance, wrapperLower))
            {
                result.Add(new HelperInstance(helper, resourceType, resourceGroup, instance));
            }
        }

        return result;
    }

    private static bool HelperMatchesResource(HelperDefinition helper, uint resourceType, uint resourceGroup, ulong instance, string wrapperLower)
    {
        // Check wrapper match
        if (helper.Wrappers.Count > 0)
        {
            if (helper.Wrappers.Contains("*"))
                return true;

            if (!string.IsNullOrEmpty(wrapperLower) && helper.Wrappers.Any(w => string.Equals(w, wrapperLower, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // Check ResourceType match
        if (helper.ResourceTypes.Count > 0)
        {
            if (helper.ResourceTypes.Contains("*"))
                return true;

            var typeHex = $"0x{resourceType:X8}";
            if (helper.ResourceTypes.Contains(typeHex))
                return true;
        }

        // Check ResourceGroup match
        if (helper.ResourceGroups.Count > 0)
        {
            if (helper.ResourceGroups.Contains("*"))
                return true;

            var groupHex = $"0x{resourceGroup:X8}";
            if (helper.ResourceGroups.Contains(groupHex))
                return true;
        }

        // Check Instance match
        if (helper.Instances.Count > 0)
        {
            if (helper.Instances.Contains("*"))
                return true;

            var instanceHex = $"0x{instance:X16}";
            if (helper.Instances.Contains(instanceHex))
                return true;
        }

        return false;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        lock (Lock)
        {
            if (_loaded) return;

            LoadHelpers();
            _loaded = true;
        }
    }

    /// <summary>
    /// Loads all *.helper files from the Helpers folder.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 21-84
    /// </remarks>
    private void LoadHelpers()
    {
        _allHelpers.Clear();

        if (!Directory.Exists(_helpersFolder))
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Helpers folder not found: {_helpersFolder}");
            return;
        }

        foreach (var file in Directory.GetFiles(_helpersFolder, "*.helper"))
        {
            try
            {
                var helper = ParseHelperFile(file);
                if (helper != null && !string.IsNullOrEmpty(helper.Command))
                {
                    _allHelpers.Add(helper);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to parse helper file {file}: {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"[TS4Tools] Loaded {_allHelpers.Count} helper definitions");
    }

    /// <summary>
    /// Parses a single .helper file.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 31-83
    /// </remarks>
    private static HelperDefinition? ParseHelperFile(string filePath)
    {
        var id = Path.GetFileNameWithoutExtension(filePath);
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["file"] = id
        };

        var inCommentBlock = false;

        using var reader = new StreamReader(filePath);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();

            // Handle block comments
            if (inCommentBlock)
            {
                var endIdx = line.IndexOf("*/", StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    line = line[(endIdx + 2)..].Trim();
                    inCommentBlock = false;
                }
                else
                {
                    continue;
                }
            }

            // Handle line comments
            foreach (var marker in CommentMarkers)
            {
                var idx = line.IndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    line = line[..idx].Trim();
                }
            }

            if (string.IsNullOrEmpty(line)) continue;

            // Handle start of block comment
            var blockStart = line.IndexOf("/*", StringComparison.Ordinal);
            if (blockStart >= 0)
            {
                line = line[..blockStart].Trim();
                inCommentBlock = true;
            }

            if (string.IsNullOrEmpty(line)) continue;

            // Parse key: value or key=value
            var separatorIdx = line.IndexOfAny([':', '=']);
            if (separatorIdx < 0) continue;

            var key = line[..separatorIdx].Trim();
            var value = line[(separatorIdx + 1)..].Trim();

            // Normalize reserved keywords to lowercase
            if (ReservedKeywords.Contains(key.ToLowerInvariant()))
            {
                key = key.ToLowerInvariant();
            }

            // Don't overwrite existing keys
            config.TryAdd(key, value);
        }

        // Must have a command to be valid
        if (!config.ContainsKey("command"))
        {
            return null;
        }

        return new HelperDefinition
        {
            Id = id,
            Label = config.GetValueOrDefault("label", ""),
            Description = config.GetValueOrDefault("desc", ""),
            Command = config.GetValueOrDefault("command", ""),
            Arguments = config.GetValueOrDefault("arguments", ""),
            IsReadOnly = config.ContainsKey("readonly"),
            IgnoreWriteTimestamp = config.ContainsKey("ignorewritetimestamp"),
            Wrappers = ParseSpaceSeparatedList(config.GetValueOrDefault("wrapper", "")),
            ResourceTypes = ParseSpaceSeparatedList(config.GetValueOrDefault("ResourceType", "")),
            ResourceGroups = ParseSpaceSeparatedList(config.GetValueOrDefault("ResourceGroup", "")),
            Instances = ParseSpaceSeparatedList(config.GetValueOrDefault("Instance", ""))
        };
    }

    private static List<string> ParseSpaceSeparatedList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return [.. value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}

/// <summary>
/// Represents a helper definition loaded from a .helper file.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 104-130
/// </remarks>
public sealed class HelperDefinition
{
    /// <summary>
    /// Unique identifier (filename without extension).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display label for the helper button.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Description/tooltip for the helper.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Command to execute (executable path or name).
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Arguments to pass to the command. {} is replaced with the temp filename.
    /// </summary>
    public string Arguments { get; init; } = "";

    /// <summary>
    /// If true, changes from the helper won't be imported back.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// If true, always assume file was modified (don't check timestamp).
    /// </summary>
    public bool IgnoreWriteTimestamp { get; init; }

    /// <summary>
    /// Wrapper class names this helper applies to.
    /// </summary>
    public List<string> Wrappers { get; init; } = [];

    /// <summary>
    /// Resource types (as hex strings like 0x12345678) this helper applies to.
    /// </summary>
    public List<string> ResourceTypes { get; init; } = [];

    /// <summary>
    /// Resource groups (as hex strings) this helper applies to.
    /// </summary>
    public List<string> ResourceGroups { get; init; } = [];

    /// <summary>
    /// Resource instances (as hex strings) this helper applies to.
    /// </summary>
    public List<string> Instances { get; init; } = [];

    /// <summary>
    /// Gets whether this helper uses file export (has {} in command or arguments).
    /// </summary>
    public bool UsesFileExport => Command.Contains("{}") || Arguments.Contains("{}");
}

/// <summary>
/// Result of executing a helper program.
/// </summary>
public sealed class HelperExecutionResult
{
    /// <summary>
    /// Whether the helper executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Modified resource data if the helper modified the file.
    /// Null if readonly or no changes detected.
    /// </summary>
    public byte[]? ModifiedData { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// A helper instance bound to a specific resource for execution.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 116-128
/// </remarks>
public sealed class HelperInstance
{
    private readonly HelperDefinition _definition;
    private readonly uint _resourceType;
    private readonly uint _resourceGroup;
    private readonly ulong _instance;

    public HelperInstance(HelperDefinition definition, uint resourceType, uint resourceGroup, ulong instance)
    {
        _definition = definition;
        _resourceType = resourceType;
        _resourceGroup = resourceGroup;
        _instance = instance;
    }

    public string Id => _definition.Id;
    public string Label => !string.IsNullOrEmpty(_definition.Label) ? _definition.Label : _definition.Id;
    public string Description => _definition.Description;
    public string Command => _definition.Command;
    public string Arguments => _definition.Arguments;
    public bool IsReadOnly => _definition.IsReadOnly;
    public bool IgnoreWriteTimestamp => _definition.IgnoreWriteTimestamp;
    public bool UsesFileExport => _definition.UsesFileExport;

    /// <summary>
    /// Gets the temp filename for this resource.
    /// </summary>
    /// <param name="resourceName">Optional resource name.</param>
    /// <returns>Full path to temp file.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 126-127
    /// Uses TGIN format: S4_Type_Group_Instance[_Name]
    /// </remarks>
    public string GetTempFilename(string? resourceName = null)
    {
        var name = $"S4_{_resourceType:X8}_{_resourceGroup:X8}_{_instance:X16}";
        if (!string.IsNullOrEmpty(resourceName))
        {
            // Sanitize the name for use in filename
            var safeName = string.Join("_", resourceName.Split(Path.GetInvalidFileNameChars()));
            name += $"_{safeName}";
        }
        return Path.Combine(Path.GetTempPath(), name);
    }

    /// <summary>
    /// Expands command and arguments with the actual filename and resource properties.
    /// </summary>
    /// <param name="filename">The temp filename to substitute for {}.</param>
    /// <returns>Tuple of (expanded command, expanded arguments).</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 254-257
    /// </remarks>
    public (string Command, string Arguments) ExpandCommandLine(string filename)
    {
        var cmd = Command.Replace("{}", filename);
        var args = Arguments.Replace("{}", filename);

        // Additional property substitution would happen here if we had access to the resource
        // For now, just handle the filename substitution

        return (cmd, args);
    }

    /// <summary>
    /// Executes the helper with the given resource data.
    /// </summary>
    /// <param name="resourceData">The resource data to edit.</param>
    /// <param name="resourceName">Optional resource name for the temp filename.</param>
    /// <returns>Result containing modified data if changed.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 176-205, 252-279
    /// </remarks>
    public async Task<HelperExecutionResult> ExecuteAsync(ReadOnlyMemory<byte> resourceData, string? resourceName = null)
    {
        if (!UsesFileExport)
        {
            return new HelperExecutionResult
            {
                Success = false,
                ErrorMessage = "Helper does not use file export (no {} in command/arguments)"
            };
        }

        var tempFile = GetTempFilename(resourceName);

        try
        {
            // Write resource data to temp file
            await File.WriteAllBytesAsync(tempFile, resourceData.ToArray());
            var lastWriteTime = File.GetLastWriteTimeUtc(tempFile);

            // Expand command line
            var (cmd, args) = ExpandCommandLine(tempFile);

            // Execute the helper
            var success = await ExecuteProcessAsync(cmd, args);

            if (!success)
            {
                return new HelperExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Helper process exited with non-zero code"
                };
            }

            // Check for modifications
            if (!IsReadOnly)
            {
                var newWriteTime = File.GetLastWriteTimeUtc(tempFile);

                if (IgnoreWriteTimestamp || newWriteTime != lastWriteTime)
                {
                    var modifiedData = await File.ReadAllBytesAsync(tempFile);
                    return new HelperExecutionResult
                    {
                        Success = true,
                        ModifiedData = modifiedData
                    };
                }
            }

            return new HelperExecutionResult { Success = true };
        }
        catch (Exception ex)
        {
            return new HelperExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            // Clean up temp file
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Executes a process and waits for it to exit.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 259-278
    /// </remarks>
    private static async Task<bool> ExecuteProcessAsync(string command, string arguments)
    {
        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = false;

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to start helper: {ex.Message}");
            throw new InvalidOperationException($"Failed to start '{command}': {ex.Message}", ex);
        }

        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }
}
