using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Service for automatically discovering and loading plugins from standard locations.
/// PHASE 4.20.4: Implements auto-discovery capabilities for legacy and modern plugins.
/// </summary>
public sealed class PluginDiscoveryService : IDisposable
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly PluginRegistrationManager _pluginManager;
    private readonly List<AssemblyLoadContext> _loadContexts = new();
    private readonly List<string> _standardLocations = new();
    private bool _disposed;

    /// <summary>
    /// Default plugin subdirectory names to search.
    /// </summary>
    public static readonly string[] DefaultPluginDirectories = {
        "Plugins",
        "Extensions", 
        "Helpers",
        "Wrappers"
    };

    /// <summary>
    /// Standard file patterns for plugin assemblies.
    /// </summary>
    public static readonly string[] PluginFilePatterns = {
        "*.dll",
        "*Helper.dll",
        "*Plugin.dll",
        "*Wrapper.dll",
        "*Extension.dll"
    };

    /// <summary>
    /// Initializes a new instance of the PluginDiscoveryService.
    /// </summary>
    /// <param name="logger">Logger for discovery operations.</param>
    /// <param name="pluginManager">Plugin registration manager for discovered plugins.</param>
    public PluginDiscoveryService(
        ILogger<PluginDiscoveryService> logger, 
        PluginRegistrationManager pluginManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        
        InitializeStandardLocations();
    }

    /// <summary>
    /// Gets the list of standard plugin discovery locations.
    /// </summary>
    public IReadOnlyList<string> StandardLocations => _standardLocations.AsReadOnly();

    /// <summary>
    /// Discovers and loads plugins from all standard locations.
    /// </summary>
    /// <returns>Number of plugins successfully discovered and registered.</returns>
    public int DiscoverPlugins()
    {
        _logger.LogInformation("Starting plugin discovery from {LocationCount} standard locations", _standardLocations.Count);
        
        var discoveredCount = 0;
        
        foreach (var location in _standardLocations)
        {
            try
            {
                discoveredCount += DiscoverPluginsFromDirectory(location);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover plugins from location: {Location}", location);
            }
        }

        _logger.LogInformation("Plugin discovery completed. Discovered {Count} plugins", discoveredCount);
        return discoveredCount;
    }

    /// <summary>
    /// Discovers plugins from a specific directory.
    /// </summary>
    /// <param name="directory">Directory to scan for plugins.</param>
    /// <returns>Number of plugins discovered from this directory.</returns>
    public int DiscoverPluginsFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogDebug("Plugin directory does not exist: {Directory}", directory);
            return 0;
        }

        _logger.LogDebug("Scanning directory for plugins: {Directory}", directory);
        
        var discoveredCount = 0;
        
        foreach (var pattern in PluginFilePatterns)
        {
            try
            {
                var files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    if (TryLoadPluginAssembly(file))
                    {
                        discoveredCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan pattern {Pattern} in directory {Directory}", pattern, directory);
            }
        }

        return discoveredCount;
    }

    /// <summary>
    /// Discovers plugins with enhanced metadata and dependency resolution.
    /// PHASE 4.20.5: Includes dependency resolution and ordered loading.
    /// </summary>
    /// <param name="dependencyResolver">Optional dependency resolver for ordering plugins.</param>
    /// <returns>Result containing discovered plugin metadata and dependency resolution information.</returns>
    public PluginDiscoveryResult DiscoverPluginsWithDependencies(PluginDependencyResolver? dependencyResolver = null)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Starting enhanced plugin discovery with dependency resolution");

        var discoveredMetadata = new List<PluginMetadata>();
        var discoveryIssues = new List<string>();

        // First pass: discover all plugin assemblies and extract metadata
        foreach (var location in _standardLocations)
        {
            try
            {
                var metadata = DiscoverPluginsInDirectoryWithMetadata(location);
                discoveredMetadata.AddRange(metadata);
                _logger.LogDebug("Discovered {Count} plugins in {Location}", metadata.Count, location);
            }
            catch (Exception ex)
            {
                var issue = $"Failed to discover plugins in {location}: {ex.Message}";
                discoveryIssues.Add(issue);
                _logger.LogWarning(ex, "Plugin discovery failed for location: {Location}", location);
            }
        }

        _logger.LogInformation("Initial discovery found {Count} plugins", discoveredMetadata.Count);

        // Second pass: resolve dependencies and determine loading order
        DependencyResolutionResult? resolutionResult = null;
        if (dependencyResolver != null && discoveredMetadata.Count > 0)
        {
            resolutionResult = dependencyResolver.ResolveDependencies(discoveredMetadata);
            _logger.LogInformation("Dependency resolution: {LoadableCount} loadable, {IssueCount} dependency issues", 
                resolutionResult.OrderedPlugins.Count, resolutionResult.Issues.Count);
        }

        // Third pass: register plugins in dependency order
        var registeredCount = 0;
        var registrationIssues = new List<string>();

        var pluginsToRegister = resolutionResult?.OrderedPlugins ?? discoveredMetadata;
        foreach (var plugin in pluginsToRegister)
        {
            try
            {
                if (_pluginManager.RegisterPlugin(plugin))
                {
                    registeredCount++;
                    _logger.LogDebug("Registered plugin: {PluginName}", plugin.PluginInfo?.Name ?? plugin.AssemblyName);
                }
                else
                {
                    var issue = $"Failed to register plugin: {plugin.PluginInfo?.Name ?? plugin.AssemblyName}";
                    registrationIssues.Add(issue);
                }
            }
            catch (Exception ex)
            {
                var issue = $"Exception registering plugin {plugin.PluginInfo?.Name ?? plugin.AssemblyName}: {ex.Message}";
                registrationIssues.Add(issue);
                _logger.LogWarning(ex, "Failed to register plugin: {PluginName}", plugin.PluginInfo?.Name ?? plugin.AssemblyName);
            }
        }

        _logger.LogInformation("Enhanced plugin discovery complete: {RegisteredCount} registered, {TotalIssues} total issues",
            registeredCount, discoveryIssues.Count + registrationIssues.Count + (resolutionResult?.Issues.Count ?? 0));

        return new PluginDiscoveryResult(
            discoveredMetadata,
            resolutionResult,
            registeredCount,
            discoveryIssues,
            registrationIssues);
    }

    /// <summary>
    /// Discovers plugin assemblies in a directory and extracts their metadata.
    /// </summary>
    /// <param name="directory">Directory to scan for plugins.</param>
    /// <returns>Collection of plugin metadata for discovered assemblies.</returns>
    private List<PluginMetadata> DiscoverPluginsInDirectoryWithMetadata(string directory)
    {
        var metadata = new List<PluginMetadata>();

        if (!Directory.Exists(directory))
        {
            return metadata;
        }

        foreach (var pattern in PluginFilePatterns)
        {
            try
            {
                var files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var pluginMetadata = TryExtractPluginMetadata(file);
                    if (pluginMetadata != null)
                    {
                        metadata.Add(pluginMetadata);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan pattern {Pattern} in directory {Directory}", pattern, directory);
            }
        }

        return metadata;
    }

    /// <summary>
    /// Attempts to extract metadata from a plugin assembly without registering it.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>Plugin metadata if extraction succeeds, null otherwise.</returns>
    private PluginMetadata? TryExtractPluginMetadata(string assemblyPath)
    {
        if (!CanAccessFile(assemblyPath))
        {
            _logger.LogDebug("Cannot access assembly file: {AssemblyPath}", assemblyPath);
            return null;
        }

        try
        {
            // Load assembly in isolated context for metadata extraction
            var loadContext = new AssemblyLoadContext($"MetadataExtraction_{Path.GetFileName(assemblyPath)}", true);
            _loadContexts.Add(loadContext);

            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var metadata = PluginMetadata.FromAssembly(assembly, assemblyPath);

            _logger.LogDebug("Extracted metadata for plugin: {PluginName} v{Version}",
                metadata.PluginInfo?.Name ?? metadata.AssemblyName, metadata.Version);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract metadata from assembly: {AssemblyPath}", assemblyPath);
            return null;
        }
    }

    /// <summary>
    /// Attempts to load a plugin assembly and register its resource handlers.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>True if the assembly contained valid plugins and was registered.</returns>
    private bool TryLoadPluginAssembly(string assemblyPath)
    {
        try
        {
            // Skip if file is locked or inaccessible
            if (!CanAccessFile(assemblyPath))
            {
                _logger.LogDebug("Cannot access file, skipping: {AssemblyPath}", assemblyPath);
                return false;
            }

            // Create isolated load context for the plugin
            var loadContext = new AssemblyLoadContext($"Plugin_{Path.GetFileNameWithoutExtension(assemblyPath)}", true);
            _loadContexts.Add(loadContext);

            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            
            _logger.LogDebug("Loaded assembly for plugin discovery: {AssemblyPath}", assemblyPath);
            
            return ScanAssemblyForPlugins(assembly, assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load assembly as plugin: {AssemblyPath}", assemblyPath);
            return false;
        }
    }

    /// <summary>
    /// Scans an assembly for resource handler types and registers them.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    /// <param name="assemblyPath">Original path of the assembly for logging.</param>
    /// <returns>True if any plugins were found and registered.</returns>
    private bool ScanAssemblyForPlugins(Assembly assembly, string assemblyPath)
    {
        var pluginsFound = false;
        
        try
        {
            var types = assembly.GetExportedTypes();
            var resourceHandlers = new Dictionary<string, Type>();
            
            foreach (var type in types)
            {
                if (IsValidResourceHandlerType(type))
                {
                    // Try to determine resource type from type or attributes
                    var resourceTypeKey = GetResourceTypeKey(type);
                    if (resourceTypeKey != null)
                    {
                        resourceHandlers[resourceTypeKey] = type;
                        _logger.LogDebug("Found resource handler {Type} for resource type {ResourceType}", type.FullName, resourceTypeKey);
                    }
                }
            }

            if (resourceHandlers.Count > 0)
            {
                // Create plugin metadata for the assembly
                var pluginMetadata = PluginMetadata.FromAssembly(assembly, assemblyPath);
                var pluginName = $"AutoDiscovered_{Path.GetFileNameWithoutExtension(assemblyPath)}";
                
                try
                {
                    _pluginManager.RegisterPluginDirect(pluginMetadata, resourceHandlers);
                    _logger.LogInformation("Auto-discovered and registered plugin: {PluginName} with {HandlerCount} resource handlers", 
                        pluginName, resourceHandlers.Count);
                    pluginsFound = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register auto-discovered plugin {PluginName} from {AssemblyPath}", pluginName, assemblyPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan assembly for plugins: {AssemblyPath}", assemblyPath);
        }

        return pluginsFound;
    }

    /// <summary>
    /// Determines if a type is a valid resource handler that can be auto-registered.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type can be registered as a resource handler.</returns>
    private static bool IsValidResourceHandlerType(Type type)
    {
        // Must be a concrete class
        if (!type.IsClass || type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            return false;

        // Must implement IResource
        if (!typeof(IResource).IsAssignableFrom(type))
            return false;

        // Must have a constructor that can be used for resource creation
        var constructors = type.GetConstructors();
        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            
            // Look for constructors that match resource creation patterns:
            // - (int APIversion)
            // - (int APIversion, Stream s)
            // - Empty constructor
            if (parameters.Length == 0 ||
                (parameters.Length == 1 && parameters[0].ParameterType == typeof(int)) ||
                (parameters.Length == 2 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(Stream)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to determine the resource type key for a resource handler type.
    /// </summary>
    /// <param name="type">Type to analyze.</param>
    /// <returns>Resource type key if determinable, null otherwise.</returns>
    private static string? GetResourceTypeKey(Type type)
    {
        try
        {
            // Try to get ResourceType property if it exists
            var resourceTypeProperty = type.GetProperty("ResourceType", BindingFlags.Public | BindingFlags.Static);
            if (resourceTypeProperty != null && resourceTypeProperty.PropertyType == typeof(uint))
            {
                var resourceTypeValue = (uint?)resourceTypeProperty.GetValue(null);
                if (resourceTypeValue.HasValue)
                {
                    return $"0x{resourceTypeValue.Value:X8}";
                }
            }

            // Try to instantiate the type and get ResourceType from instance
            var constructors = type.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                
                // Try empty constructor first
                if (parameters.Length == 0)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance != null)
                        {
                            var instanceResourceTypeProperty = type.GetProperty("ResourceType", BindingFlags.Public | BindingFlags.Instance);
                            if (instanceResourceTypeProperty?.PropertyType == typeof(uint))
                            {
                                var resourceTypeValue = (uint?)instanceResourceTypeProperty.GetValue(instance);
                                if (resourceTypeValue.HasValue && resourceTypeValue.Value != 0)
                                {
                                    return $"0x{resourceTypeValue.Value:X8}";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore instantiation errors
                    }
                    break;
                }
                
                // Try constructor with int parameter (APIversion)
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type, 1);
                        if (instance != null)
                        {
                            var instanceResourceTypeProperty = type.GetProperty("ResourceType", BindingFlags.Public | BindingFlags.Instance);
                            if (instanceResourceTypeProperty?.PropertyType == typeof(uint))
                            {
                                var resourceTypeValue = (uint?)instanceResourceTypeProperty.GetValue(instance);
                                if (resourceTypeValue.HasValue && resourceTypeValue.Value != 0)
                                {
                                    return $"0x{resourceTypeValue.Value:X8}";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore instantiation errors
                    }
                    break;
                }
            }

            // Fallback: use type name as resource type key
            // This allows registration even if we can't determine the exact resource type
            return $"Type_{type.Name}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a file can be accessed for reading.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>True if the file can be read.</returns>
    private static bool CanAccessFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes the list of standard plugin discovery locations.
    /// </summary>
    private void InitializeStandardLocations()
    {
        // Get the directory where the main assembly is located
        var mainAssemblyLocation = Assembly.GetEntryAssembly()?.Location 
            ?? Assembly.GetExecutingAssembly().Location;
        var baseDirectory = Path.GetDirectoryName(mainAssemblyLocation) ?? Environment.CurrentDirectory;

        // Add the main assembly directory (legacy s4pi pattern)
        _standardLocations.Add(baseDirectory);

        // Add standard plugin subdirectories
        foreach (var subdirectory in DefaultPluginDirectories)
        {
            var pluginDir = Path.Combine(baseDirectory, subdirectory);
            _standardLocations.Add(pluginDir);
        }

        // Add user-specific plugin directory
        var userPluginsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TS4Tools",
            "Plugins");
        _standardLocations.Add(userPluginsDir);

        // Add system-wide plugin directory
        var systemPluginsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "TS4Tools",
            "Plugins");
        _standardLocations.Add(systemPluginsDir);

        _logger.LogDebug("Initialized {Count} standard plugin discovery locations", _standardLocations.Count);
    }

    /// <summary>
    /// Disposes the plugin discovery service and unloads plugin contexts.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var context in _loadContexts)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unload plugin context");
            }
        }

        _loadContexts.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginDiscoveryService));
    }
}

/// <summary>
/// Result of enhanced plugin discovery with dependency resolution.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
/// <param name="DiscoveredPlugins">All plugins discovered during scanning.</param>
/// <param name="DependencyResolution">Result of dependency resolution if performed.</param>
/// <param name="RegisteredCount">Number of plugins successfully registered.</param>
/// <param name="DiscoveryIssues">Issues encountered during plugin discovery.</param>
/// <param name="RegistrationIssues">Issues encountered during plugin registration.</param>
public sealed record PluginDiscoveryResult(
    IReadOnlyList<PluginMetadata> DiscoveredPlugins,
    DependencyResolutionResult? DependencyResolution,
    int RegisteredCount,
    IReadOnlyList<string> DiscoveryIssues,
    IReadOnlyList<string> RegistrationIssues)
{
    /// <summary>
    /// Gets all issues encountered during the discovery and registration process.
    /// </summary>
    public IReadOnlyList<string> AllIssues
    {
        get
        {
            var allIssues = new List<string>();
            allIssues.AddRange(DiscoveryIssues);
            allIssues.AddRange(RegistrationIssues);
            if (DependencyResolution != null)
            {
                allIssues.AddRange(DependencyResolution.Issues.Select(i => i.Description));
            }
            return allIssues;
        }
    }

    /// <summary>
    /// Whether the discovery process completed without any critical issues.
    /// </summary>
    public bool IsSuccessful => DiscoveryIssues.Count == 0 && RegistrationIssues.Count == 0 &&
                               (DependencyResolution?.Issues.Count(i => !IsOptionalDependencyIssue(i)) ?? 0) == 0;

    private static bool IsOptionalDependencyIssue(DependencyIssue issue)
    {
        // This is a simplified check - in practice, we'd need to correlate with actual dependency attributes
        return issue.Description.Contains("optional", StringComparison.OrdinalIgnoreCase);
    }
}
