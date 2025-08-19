using System.Reflection;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Provides automatic plugin discovery for WrapperDealer compatibility layer.
/// Scans plugin directories, validates plugin assemblies, and returns discovered plugin metadata.
/// </summary>
public static class PluginDiscovery
{
    /// <summary>
    /// Scans the specified directory for plugin assemblies (*.dll).
    /// </summary>
    /// <param name="pluginDirectory">The directory to scan for plugins.</param>
    /// <param name="logger">Logger for diagnostics and error reporting.</param>
    /// <returns>List of discovered plugin file paths.</returns>
    public static IReadOnlyList<string> DiscoverPlugins(string pluginDirectory, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
            throw new ArgumentException("Plugin directory cannot be null or empty.", nameof(pluginDirectory));
        if (!Directory.Exists(pluginDirectory))
            throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDirectory}");

        var pluginFiles = Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            .Where(f => !IsSystemAssembly(f))
            .ToList();

        logger.LogInformation("Discovered {Count} plugin assemblies in {Directory}", pluginFiles.Count, pluginDirectory);
        return pluginFiles;
    }

    /// <summary>
    /// Discovers plugins with full metadata validation and compatibility checking.
    /// </summary>
    /// <param name="pluginDirectory">The directory to scan for plugins.</param>
    /// <param name="logger">Logger for diagnostics and error reporting.</param>
    /// <returns>List of discovered plugin metadata.</returns>
    public static IReadOnlyList<PluginMetadata> DiscoverPluginsWithMetadata(
        string pluginDirectory,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var pluginFiles = DiscoverPlugins(pluginDirectory, logger);
        var pluginMetadata = new List<PluginMetadata>();

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                logger.LogDebug("Analyzing plugin: {PluginFile}", pluginFile);

                // Load the assembly to extract metadata
                var assembly = LegacyAssemblyLoader.LoadFile(pluginFile);
                var metadata = PluginMetadata.FromAssembly(assembly, pluginFile);

                pluginMetadata.Add(metadata);

                // Log compatibility warnings
                if (metadata.CompatibilityWarnings.Count > 0)
                {
                    foreach (var warning in metadata.CompatibilityWarnings)
                    {
                        logger.LogWarning("Plugin {PluginName}: {Warning}", metadata.AssemblyName, warning);
                    }
                }

                if (!metadata.IsCompatible)
                {
                    logger.LogWarning("Plugin {PluginName} is incompatible: {Reason}", 
                        metadata.AssemblyName, metadata.IncompatibilityReason);
                }
                else
                {
                    logger.LogInformation("Plugin {PluginName} v{Version} is compatible ({HandlerCount} potential handlers)",
                        metadata.AssemblyName, metadata.Version, metadata.PotentialResourceHandlers.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze plugin: {PluginFile}", pluginFile);

                // Create a metadata entry for the failed plugin
                var failedMetadata = CreateFailedPluginMetadata(pluginFile, ex);
                pluginMetadata.Add(failedMetadata);
            }
        }

        // Sort plugins by compatibility and priority
        var sortedPlugins = pluginMetadata
            .OrderByDescending(p => p.IsCompatible)
            .ThenBy(p => p.AssemblyName)
            .ToList();

        logger.LogInformation("Plugin discovery completed: {Total} plugins found, {Compatible} compatible, {Incompatible} incompatible",
            sortedPlugins.Count,
            sortedPlugins.Count(p => p.IsCompatible),
            sortedPlugins.Count(p => !p.IsCompatible));

        return sortedPlugins.AsReadOnly();
    }

    /// <summary>
    /// Checks if a file is likely a system assembly that should be excluded from plugin discovery.
    /// </summary>
    /// <param name="filePath">The path to the assembly file.</param>
    /// <returns>True if the file appears to be a system assembly.</returns>
    private static bool IsSystemAssembly(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        // Exclude common system assemblies
        var systemPrefixes = new[]
        {
            "System.",
            "Microsoft.",
            "mscorlib",
            "netstandard",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework"
        };

        return systemPrefixes.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a PluginMetadata instance for a plugin that failed to load.
    /// </summary>
    /// <param name="filePath">The path to the failed plugin.</param>
    /// <param name="exception">The exception that occurred during loading.</param>
    /// <returns>A PluginMetadata instance representing the failed plugin.</returns>
    private static PluginMetadata CreateFailedPluginMetadata(string filePath, Exception exception)
    {
        var fileInfo = new FileInfo(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        return new PluginMetadata
        {
            FilePath = filePath,
            AssemblyName = fileName,
            AssemblyFullName = fileName,
            Version = new Version(0, 0, 0, 0),
            TargetFramework = "Unknown",
            IsSigned = false,
            FileSizeBytes = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime,
            IsCompatible = false,
            CompatibilityWarnings = Array.Empty<string>(),
            IncompatibilityReason = $"Failed to load: {exception.Message}",
            PotentialResourceHandlers = Array.Empty<string>()
        };
    }
}
