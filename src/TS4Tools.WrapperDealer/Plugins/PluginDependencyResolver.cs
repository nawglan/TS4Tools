using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Resolves plugin dependencies and determines loading order.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
public sealed class PluginDependencyResolver
{
    private readonly ILogger<PluginDependencyResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the PluginDependencyResolver.
    /// </summary>
    /// <param name="logger">Logger for dependency resolution operations.</param>
    public PluginDependencyResolver(ILogger<PluginDependencyResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves dependencies and returns plugins in dependency-ordered loading sequence.
    /// </summary>
    /// <param name="plugins">Collection of plugin metadata to resolve.</param>
    /// <returns>
    /// A result containing the ordered loading sequence and any dependency issues.
    /// </returns>
    public DependencyResolutionResult ResolveDependencies(IReadOnlyList<PluginMetadata> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var issues = new List<DependencyIssue>();
        var loadablePlugins = new List<PluginMetadata>();

        // Create a map of plugin name to metadata for quick lookup
        var pluginMap = BuildPluginMap(plugins);

        // First pass: identify plugins with unresolvable dependencies
        var filteredPlugins = FilterUnresolvablePlugins(plugins, pluginMap, issues);

        // Second pass: topological sort of resolvable plugins
        var orderedPlugins = TopologicalSort(filteredPlugins, pluginMap, issues);

        _logger.LogInformation("Dependency resolution complete: {LoadableCount} loadable, {IssueCount} issues",
            orderedPlugins.Count, issues.Count);

        return new DependencyResolutionResult(orderedPlugins, issues);
    }

    /// <summary>
    /// Validates that all dependencies for a plugin can be satisfied.
    /// </summary>
    /// <param name="plugin">The plugin to validate dependencies for.</param>
    /// <param name="availablePlugins">Collection of available plugins that could satisfy dependencies.</param>
    /// <returns>Collection of unresolved dependency issues.</returns>
    public IReadOnlyList<DependencyIssue> ValidatePluginDependencies(
        PluginMetadata plugin, 
        IReadOnlyList<PluginMetadata> availablePlugins)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(availablePlugins);

        var issues = new List<DependencyIssue>();
        var pluginMap = BuildPluginMap(availablePlugins);

        foreach (var dependency in plugin.Dependencies)
        {
            var dependencyIssue = ValidateDependency(plugin, dependency, pluginMap);
            if (dependencyIssue != null)
            {
                issues.Add(dependencyIssue);
            }
        }

        return issues;
    }

    private Dictionary<string, List<PluginMetadata>> BuildPluginMap(IReadOnlyList<PluginMetadata> plugins)
    {
        var map = new Dictionary<string, List<PluginMetadata>>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            // Use plugin info name if available, otherwise assembly name
            var pluginName = plugin.PluginInfo?.Name ?? plugin.AssemblyName;

            if (!map.TryGetValue(pluginName, out var pluginList))
            {
                pluginList = new List<PluginMetadata>();
                map[pluginName] = pluginList;
            }

            pluginList.Add(plugin);
        }

        // Sort each plugin list by version (highest first)
        foreach (var pluginList in map.Values)
        {
            pluginList.Sort((a, b) => b.Version.CompareTo(a.Version));
        }

        return map;
    }

    private List<PluginMetadata> FilterUnresolvablePlugins(
        IReadOnlyList<PluginMetadata> plugins,
        Dictionary<string, List<PluginMetadata>> pluginMap,
        List<DependencyIssue> issues)
    {
        var loadablePlugins = new List<PluginMetadata>();

        foreach (var plugin in plugins)
        {
            var canLoad = true;

            foreach (var dependency in plugin.Dependencies)
            {
                if (dependency.IsOptional)
                    continue;

                var dependencyIssue = ValidateDependency(plugin, dependency, pluginMap);
                if (dependencyIssue != null)
                {
                    issues.Add(dependencyIssue);
                    canLoad = false;
                }
            }

            if (canLoad)
            {
                loadablePlugins.Add(plugin);
            }
            else
            {
                _logger.LogWarning("Plugin {PluginName} has unresolvable dependencies and will not be loaded",
                    plugin.PluginInfo?.Name ?? plugin.AssemblyName);
            }
        }

        return loadablePlugins;
    }

    private DependencyIssue? ValidateDependency(
        PluginMetadata plugin,
        PluginDependency dependency,
        Dictionary<string, List<PluginMetadata>> pluginMap)
    {
        if (!pluginMap.TryGetValue(dependency.PluginName, out var candidatePlugins))
        {
            return new DependencyIssue(
                plugin.PluginInfo?.Name ?? plugin.AssemblyName,
                dependency.PluginName,
                DependencyIssueType.Missing,
                $"Required plugin '{dependency.PluginName}' is not available");
        }

        // Find the highest version that satisfies the dependency
        var satisfyingPlugin = candidatePlugins
            .Where(p => dependency.IsSatisfiedBy(p.Version))
            .OrderByDescending(p => p.Version)
            .FirstOrDefault();
        if (satisfyingPlugin == null)
        {
            var availableVersions = string.Join(", ", candidatePlugins.Select(p => p.Version.ToString()));
            return new DependencyIssue(
                plugin.PluginInfo?.Name ?? plugin.AssemblyName,
                dependency.PluginName,
                DependencyIssueType.VersionMismatch,
                $"No compatible version found. Required: {dependency.MinimumVersion}" +
                (dependency.MaximumVersion != null ? $"-{dependency.MaximumVersion}" : "+") +
                $", Available: {availableVersions}");
        }

        return null;
    }

    private List<PluginMetadata> TopologicalSort(
        List<PluginMetadata> plugins,
        Dictionary<string, List<PluginMetadata>> pluginMap,
        List<DependencyIssue> issues)
    {
        var ordered = new List<PluginMetadata>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        // Deduplicate plugins - select only the highest version of each plugin that can be loaded
        var selectedPlugins = new Dictionary<string, PluginMetadata>();
        foreach (var plugin in plugins)
        {
            var pluginName = plugin.PluginInfo?.Name ?? plugin.AssemblyName;
            if (!selectedPlugins.TryGetValue(pluginName, out var existingPlugin) ||
                plugin.Version > existingPlugin.Version)
            {
                selectedPlugins[pluginName] = plugin;
            }
        }

        foreach (var plugin in selectedPlugins.Values)
        {
            var pluginName = plugin.PluginInfo?.Name ?? plugin.AssemblyName;
            if (!visited.Contains(pluginName))
            {
                if (!VisitPlugin(plugin, pluginMap, visited, visiting, ordered, issues))
                {
                    // Circular dependency detected - skip this plugin
                    _logger.LogWarning("Circular dependency detected for plugin {PluginName}", pluginName);
                }
            }
        }

        return ordered;
    }

    private bool VisitPlugin(
        PluginMetadata plugin,
        Dictionary<string, List<PluginMetadata>> pluginMap,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<PluginMetadata> ordered,
        List<DependencyIssue> issues)
    {
        var pluginName = plugin.PluginInfo?.Name ?? plugin.AssemblyName;

        if (visiting.Contains(pluginName))
        {
            // Circular dependency
            issues.Add(new DependencyIssue(
                pluginName,
                string.Empty,
                DependencyIssueType.CircularDependency,
                $"Circular dependency detected involving plugin '{pluginName}'"));
            return false;
        }

        if (visited.Contains(pluginName))
        {
            return true;
        }

        visiting.Add(pluginName);

        // Visit dependencies first (only required ones for ordering)
        foreach (var dependency in plugin.Dependencies.Where(d => !d.IsOptional))
        {
            if (pluginMap.TryGetValue(dependency.PluginName, out var dependencyPlugins))
            {
                var dependencyPlugin = dependencyPlugins
                    .Where(p => dependency.IsSatisfiedBy(p.Version))
                    .OrderByDescending(p => p.Version)
                    .FirstOrDefault();
                if (dependencyPlugin != null)
                {
                    if (!VisitPlugin(dependencyPlugin, pluginMap, visited, visiting, ordered, issues))
                    {
                        visiting.Remove(pluginName);
                        return false;
                    }
                }
            }
        }

        visiting.Remove(pluginName);
        visited.Add(pluginName);
        ordered.Add(plugin);

        return true;
    }
}

/// <summary>
/// Result of dependency resolution operation.
/// </summary>
/// <param name="OrderedPlugins">Plugins ordered for loading based on dependencies.</param>
/// <param name="Issues">Any dependency issues found during resolution.</param>
public sealed record DependencyResolutionResult(
    IReadOnlyList<PluginMetadata> OrderedPlugins,
    IReadOnlyList<DependencyIssue> Issues);

/// <summary>
/// Represents an issue with plugin dependencies.
/// </summary>
/// <param name="PluginName">The name of the plugin with the dependency issue.</param>
/// <param name="DependencyName">The name of the dependency causing the issue.</param>
/// <param name="IssueType">The type of dependency issue.</param>
/// <param name="Description">A human-readable description of the issue.</param>
public sealed record DependencyIssue(
    string PluginName,
    string DependencyName,
    DependencyIssueType IssueType,
    string Description);

/// <summary>
/// Types of dependency issues that can occur during resolution.
/// </summary>
public enum DependencyIssueType
{
    /// <summary>
    /// A required dependency is not available.
    /// </summary>
    Missing,

    /// <summary>
    /// No available version of the dependency satisfies the requirement.
    /// </summary>
    VersionMismatch,

    /// <summary>
    /// A circular dependency was detected.
    /// </summary>
    CircularDependency,

    /// <summary>
    /// The dependency is incompatible with the current system.
    /// </summary>
    Incompatible
}
