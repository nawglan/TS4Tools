using System.Reflection;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Manages plugin registration and provides a bridge between legacy AResourceHandler patterns
/// and modern TS4Tools factory system. Enables seamless migration of community plugins.
/// </summary>
public sealed class PluginRegistrationManager : IDisposable
{
    private readonly ILogger<PluginRegistrationManager> _logger;
    private readonly Dictionary<string, IRegisteredPlugin> _registeredPlugins = new();
    private readonly Dictionary<string, Type> _resourceHandlers = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the PluginRegistrationManager.
    /// </summary>
    /// <param name="logger">Logger for plugin registration operations.</param>
    public PluginRegistrationManager(ILogger<PluginRegistrationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the number of currently registered plugins.
    /// </summary>
    public int RegisteredPluginCount
    {
        get
        {
            lock (_lock)
            {
                return _registeredPlugins.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of registered resource handlers.
    /// </summary>
    public int RegisteredHandlerCount
    {
        get
        {
            lock (_lock)
            {
                return _resourceHandlers.Count;
            }
        }
    }

    /// <summary>
    /// Registers a plugin from discovered plugin metadata.
    /// Analyzes the plugin assembly and registers all compatible resource handlers.
    /// </summary>
    /// <param name="pluginMetadata">The metadata of the plugin to register.</param>
    /// <returns>True if the plugin was successfully registered; false otherwise.</returns>
    public bool RegisterPlugin(PluginMetadata pluginMetadata)
    {
        ArgumentNullException.ThrowIfNull(pluginMetadata);
        ThrowIfDisposed();

        if (!pluginMetadata.IsCompatible)
        {
            _logger.LogWarning("Cannot register incompatible plugin: {PluginName} - {Reason}",
                pluginMetadata.AssemblyName, pluginMetadata.IncompatibilityReason);
            return false;
        }

        lock (_lock)
        {
            var pluginKey = pluginMetadata.AssemblyName;
            if (_registeredPlugins.ContainsKey(pluginKey))
            {
                _logger.LogDebug("Plugin {PluginName} is already registered", pluginKey);
                return true;
            }

            try
            {
                // Load the assembly and analyze for resource handlers
                var assembly = LegacyAssemblyLoader.LoadFile(pluginMetadata.FilePath);
                var plugin = AnalyzeAndRegisterPlugin(assembly, pluginMetadata);

                if (plugin != null)
                {
                    _registeredPlugins[pluginKey] = plugin;
                    _logger.LogInformation("Successfully registered plugin: {PluginName} with {HandlerCount} handlers",
                        pluginKey, plugin.ResourceHandlers.Count);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Plugin {PluginName} contains no compatible resource handlers", pluginKey);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register plugin: {PluginName}", pluginKey);
                return false;
            }
        }
    }

    /// <summary>
    /// Registers a plugin with explicitly provided handlers (used for synthetic plugins).
    /// </summary>
    /// <param name="pluginMetadata">The plugin metadata.</param>
    /// <param name="resourceHandlers">Dictionary of resource handlers.</param>
    /// <returns>True if registration succeeded; false otherwise.</returns>
    internal bool RegisterPluginDirect(PluginMetadata pluginMetadata, Dictionary<string, Type> resourceHandlers)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PluginRegistrationManager));
        }

        ArgumentNullException.ThrowIfNull(pluginMetadata);
        ArgumentNullException.ThrowIfNull(resourceHandlers);

        if (!pluginMetadata.IsCompatible)
        {
            _logger.LogWarning("Cannot register incompatible plugin: {PluginName}. Reason: {Reason}",
                pluginMetadata.AssemblyName, pluginMetadata.IncompatibilityReason);
            return false;
        }

        lock (_lock)
        {
            var pluginKey = pluginMetadata.AssemblyName;
            if (_registeredPlugins.ContainsKey(pluginKey))
            {
                _logger.LogDebug("Plugin {PluginName} is already registered", pluginKey);
                return true;
            }

            try
            {
                // Register all handlers
                foreach (var (resourceType, handlerType) in resourceHandlers)
                {
                    if (_resourceHandlers.ContainsKey(resourceType))
                    {
                        throw new InvalidOperationException(
                            $"Resource type {resourceType} is already handled by {_resourceHandlers[resourceType].FullName}");
                    }
                    _resourceHandlers[resourceType] = handlerType;
                }

                // Create and register the plugin
                var plugin = new RegisteredPlugin(pluginMetadata, resourceHandlers);
                _registeredPlugins[pluginKey] = plugin;

                _logger.LogInformation("Successfully registered plugin: {PluginName} with {HandlerCount} handlers",
                    pluginKey, resourceHandlers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register plugin: {PluginName}", pluginKey);
                throw;
            }
        }
    }

    /// <summary>
    /// Unregisters a plugin and all its resource handlers.
    /// </summary>
    /// <param name="pluginName">The name of the plugin to unregister.</param>
    /// <returns>True if the plugin was successfully unregistered; false if not found.</returns>
    public bool UnregisterPlugin(string pluginName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginName);
        ThrowIfDisposed();

        lock (_lock)
        {
            if (!_registeredPlugins.TryGetValue(pluginName, out var plugin))
            {
                _logger.LogDebug("Plugin {PluginName} is not registered", pluginName);
                return false;
            }

            try
            {
                // Remove all resource handlers registered by this plugin
                var handlersToRemove = _resourceHandlers
                    .Where(kvp => plugin.ResourceHandlers.ContainsKey(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var handlerKey in handlersToRemove)
                {
                    _resourceHandlers.Remove(handlerKey);
                }

                // Remove the plugin
                _registeredPlugins.Remove(pluginName);
                plugin.Dispose();

                _logger.LogInformation("Successfully unregistered plugin: {PluginName} and {HandlerCount} handlers",
                    pluginName, handlersToRemove.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering plugin: {PluginName}", pluginName);
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a resource handler type for the specified resource type ID.
    /// Provides compatibility with legacy WrapperDealer.GetWrapperType() calls.
    /// </summary>
    /// <param name="resourceTypeId">The resource type ID (hex string like "0x12345678").</param>
    /// <returns>The Type of the resource handler, or null if not found.</returns>
    public Type? GetResourceHandler(string resourceTypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceTypeId);
        ThrowIfDisposed();

        lock (_lock)
        {
            return _resourceHandlers.GetValueOrDefault(resourceTypeId);
        }
    }

    /// <summary>
    /// Gets all registered resource type IDs.
    /// Provides compatibility with legacy WrapperDealer.GetSupportedResourceTypes() calls.
    /// </summary>
    /// <returns>Array of supported resource type IDs.</returns>
    public string[] GetSupportedResourceTypes()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _resourceHandlers.Keys.ToArray();
        }
    }

    /// <summary>
    /// Gets information about all registered plugins.
    /// </summary>
    /// <returns>Collection of registered plugin information.</returns>
    public IReadOnlyCollection<IRegisteredPlugin> GetRegisteredPlugins()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _registeredPlugins.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Registers multiple plugins from a collection of plugin metadata.
    /// </summary>
    /// <param name="plugins">Collection of plugin metadata to register.</param>
    /// <returns>The number of successfully registered plugins.</returns>
    public int RegisterPlugins(IEnumerable<PluginMetadata> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        ThrowIfDisposed();

        var successCount = 0;
        foreach (var plugin in plugins)
        {
            if (RegisterPlugin(plugin))
            {
                successCount++;
            }
        }

        _logger.LogInformation("Registered {SuccessCount} plugins successfully", successCount);
        return successCount;
    }

    /// <summary>
    /// Analyzes an assembly and registers compatible resource handlers.
    /// </summary>
    /// <param name="assembly">The assembly to analyze.</param>
    /// <param name="metadata">The plugin metadata.</param>
    /// <returns>A registered plugin instance, or null if no handlers found.</returns>
    private IRegisteredPlugin? AnalyzeAndRegisterPlugin(Assembly assembly, PluginMetadata metadata)
    {
        var resourceHandlers = new Dictionary<string, Type>();

        try
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                // Look for types that implement IResource or inherit from legacy patterns
                if (IsResourceHandlerType(type))
                {
                    var supportedTypes = ExtractSupportedResourceTypes(type);
                    foreach (var resourceType in supportedTypes)
                    {
                        if (!string.IsNullOrEmpty(resourceType))
                        {
                            resourceHandlers[resourceType] = type;
                            _resourceHandlers[resourceType] = type;
                            _logger.LogDebug("Registered handler {TypeName} for resource type {ResourceType}",
                                type.FullName, resourceType);
                        }
                    }
                }
            }

            if (resourceHandlers.Count > 0)
            {
                return new RegisteredPlugin(metadata, resourceHandlers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing plugin assembly: {AssemblyName}", metadata.AssemblyName);
        }

        return null;
    }

    /// <summary>
    /// Determines if a type is a resource handler that should be registered.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a resource handler.</returns>
    private static bool IsResourceHandlerType(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.IsInterface)
            return false;

        // Check if implements IResource
        if (typeof(IResource).IsAssignableFrom(type))
            return true;

        // Check for legacy AResource pattern
        if (type.Name.EndsWith("Resource", StringComparison.OrdinalIgnoreCase) ||
            type.BaseType?.Name.Contains("AResource") == true)
            return true;

        return false;
    }

    /// <summary>
    /// Extracts supported resource type IDs from a resource handler type.
    /// </summary>
    /// <param name="handlerType">The resource handler type.</param>
    /// <returns>Collection of supported resource type IDs.</returns>
    private static IEnumerable<string> ExtractSupportedResourceTypes(Type handlerType)
    {
        var resourceTypes = new List<string>();

        try
        {
            // Look for resource type attributes or constants
            var attributes = handlerType.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                var attrType = attr.GetType();
                if (attrType.Name.Contains("ResourceType") || attrType.Name.Contains("ResourceHandler"))
                {
                    // Try to extract resource type value from attribute
                    var valueProperty = attrType.GetProperty("Value") ?? attrType.GetProperty("ResourceType");
                    if (valueProperty?.GetValue(attr) is string resourceType)
                    {
                        resourceTypes.Add(resourceType);
                    }
                }
            }

            // Look for static fields or properties that might contain resource type IDs
            var fields = handlerType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if ((field.Name.Contains("ResourceType", StringComparison.OrdinalIgnoreCase) ||
                     field.Name.Contains("TypeId", StringComparison.OrdinalIgnoreCase)) &&
                    field.FieldType == typeof(string))
                {
                    if (field.GetValue(null) is string resourceType)
                    {
                        resourceTypes.Add(resourceType);
                    }
                }
            }

            // If no explicit resource types found, try to infer from class name
            if (resourceTypes.Count == 0)
            {
                var inferredType = InferResourceTypeFromClassName(handlerType.Name);
                if (!string.IsNullOrEmpty(inferredType))
                {
                    resourceTypes.Add(inferredType);
                }
            }
        }
        catch (Exception)
        {
            // Ignore reflection errors
        }

        return resourceTypes;
    }

    /// <summary>
    /// Attempts to infer a resource type ID from a class name.
    /// This is a fallback for plugins that don't explicitly declare resource types.
    /// </summary>
    /// <param name="className">The class name to analyze.</param>
    /// <returns>An inferred resource type ID, or null if none can be inferred.</returns>
    private static string? InferResourceTypeFromClassName(string className)
    {
        // This is a simplified inference - in practice, you might have a mapping table
        // or more sophisticated pattern matching based on known community plugins
        
        if (className.EndsWith("Resource", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = className[..^8]; // Remove "Resource" suffix
            
            // Generate a placeholder resource type - real implementation would need
            // proper mapping or community plugin database
            return $"0x{baseName.GetHashCode():X8}";
        }

        return null;
    }

    /// <summary>
    /// Throws ObjectDisposedException if the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Disposes the plugin registration manager and all registered plugins.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            _logger.LogDebug("Disposing PluginRegistrationManager");

            try
            {
                // Dispose all registered plugins
                foreach (var plugin in _registeredPlugins.Values)
                {
                    try
                    {
                        plugin.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing plugin: {PluginName}", plugin.Metadata.AssemblyName);
                    }
                }

                _registeredPlugins.Clear();
                _resourceHandlers.Clear();
            }
            finally
            {
                _disposed = true;
            }
        }

        _logger.LogDebug("PluginRegistrationManager disposed successfully");
    }
}

/// <summary>
/// Represents a registered plugin with its metadata and resource handlers.
/// </summary>
public interface IRegisteredPlugin : IDisposable
{
    /// <summary>
    /// The metadata of the registered plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Resource handlers provided by this plugin.
    /// Key is the resource type ID, value is the handler type.
    /// </summary>
    IReadOnlyDictionary<string, Type> ResourceHandlers { get; }

    /// <summary>
    /// The date and time when the plugin was registered.
    /// </summary>
    DateTime RegistrationTime { get; }

    /// <summary>
    /// Whether the plugin is currently active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Default implementation of IRegisteredPlugin.
/// </summary>
internal sealed class RegisteredPlugin : IRegisteredPlugin
{
    private bool _disposed;

    public PluginMetadata Metadata { get; }
    public IReadOnlyDictionary<string, Type> ResourceHandlers { get; }
    public DateTime RegistrationTime { get; }
    public bool IsActive => !_disposed;

    public RegisteredPlugin(PluginMetadata metadata, Dictionary<string, Type> resourceHandlers)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        ResourceHandlers = resourceHandlers?.AsReadOnly() ?? throw new ArgumentNullException(nameof(resourceHandlers));
        RegistrationTime = DateTime.UtcNow;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
