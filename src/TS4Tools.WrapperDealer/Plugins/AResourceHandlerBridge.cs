using System.Reflection;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Provides legacy AResourceHandler.Add() pattern compatibility for community plugins.
/// This bridge enables existing plugins to register resource handlers using legacy patterns
/// while internally using the modern TS4Tools plugin system.
/// </summary>
public static class AResourceHandlerBridge
{
    private static PluginRegistrationManager? _registrationManager;
    private static readonly ILogger _logger = new NullLogger<PluginRegistrationManager>();
    private static readonly object _lock = new();
    
    // Cache to persist registrations across re-initializations
    private static readonly Dictionary<string, Type> _persistedRegistrations = new();

    /// <summary>
    /// Initializes the bridge with a plugin registration manager.
    /// </summary>
    /// <param name="registrationManager">The plugin registration manager to use.</param>
    public static void Initialize(PluginRegistrationManager registrationManager)
    {
        lock (_lock)
        {
            _registrationManager = registrationManager ?? throw new ArgumentNullException(nameof(registrationManager));
            
            // Re-register any previously cached registrations with the new manager
            foreach (var (resourceType, handlerType) in _persistedRegistrations)
            {
                try
                {
                    RegisterWithManager(resourceType, handlerType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to re-register cached handler {HandlerType} for resource type {ResourceType}",
                        handlerType.FullName, resourceType);
                }
            }
        }
    }

    /// <summary>
    /// Resets the bridge to uninitialized state.
    /// Used primarily for testing scenarios.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _registrationManager = null;
            _persistedRegistrations.Clear();
        }
    }

    /// <summary>
    /// Adds a resource handler for the specified type
    /// </summary>
    /// <param name="resourceType">The resource type identifier</param>
    /// <param name="handlerType">The handler type</param>
    public static void Add(string resourceType, Type handlerType)
    {
        if (resourceType == null)
        {
            throw new ArgumentNullException(nameof(resourceType));
        }

        if (string.IsNullOrEmpty(resourceType))
        {
            throw new ArgumentException("Resource type cannot be null or empty", nameof(resourceType));
        }

        if (handlerType == null)
        {
            throw new ArgumentNullException(nameof(handlerType));
        }

        // Check if we're initialized - throw if not
        if (_registrationManager == null)
        {
            throw new InvalidOperationException("AResourceHandlerBridge must be initialized before adding handlers");
        }

        // Register with the current manager and cache for future re-initializations
        RegisterWithManager(resourceType, handlerType);
    }

    /// <summary>
    /// Internal helper method to register a handler with the current manager.
    /// </summary>
    /// <param name="resourceType">The resource type ID.</param>
    /// <param name="handlerType">The handler type.</param>
    private static void RegisterWithManager(string resourceType, Type handlerType)
    {
        if (_registrationManager == null)
        {
            throw new InvalidOperationException("AResourceHandlerBridge must be initialized before use");
        }

        // Check if the manager has been disposed (common in test scenarios)
        try
        {
            // This will throw ObjectDisposedException if the manager is disposed
            var testCount = _registrationManager.RegisteredPluginCount;
        }
        catch (ObjectDisposedException)
        {
            throw new InvalidOperationException("AResourceHandlerBridge's PluginRegistrationManager has been disposed. Call Initialize() with a new manager.");
        }

        try
        {
            // Create a synthetic plugin metadata for the manually registered handler
            var syntheticMetadata = CreateSyntheticPluginMetadata(handlerType, resourceType);

            // Create the handler dictionary for this specific registration
            var handlerDict = new Dictionary<string, Type> { [resourceType] = handlerType };

            // Register directly using the internal method
            var success = _registrationManager.RegisterPluginDirect(syntheticMetadata, handlerDict);
            
            if (success)
            {
                _logger.LogInformation("Legacy handler registered: {HandlerType} for resource type {ResourceType}",
                    handlerType.FullName, resourceType);
            }
            else
            {
                _logger.LogWarning("Failed to register legacy handler: {HandlerType} for resource type {ResourceType}",
                    handlerType.FullName, resourceType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering legacy handler: {HandlerType} for resource type {ResourceType}",
                handlerType.FullName, resourceType);
            throw;
        }
    }

    /// <summary>
    /// Legacy-compatible resource handler registration method with multiple resource types.
    /// </summary>
    /// <param name="handlerType">The type of the resource handler.</param>
    /// <param name="resourceTypes">Array of resource type IDs supported by the handler.</param>
    public static void Add(Type handlerType, params string[] resourceTypes)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(resourceTypes);

        foreach (var resourceType in resourceTypes)
        {
            Add(resourceType, handlerType);
        }
    }

    /// <summary>
    /// Removes a resource handler registration.
    /// Provides compatibility with legacy unregistration patterns.
    /// </summary>
    /// <param name="resourceType">The resource type ID to unregister.</param>
    /// <returns>True if the handler was found and removed; false otherwise.</returns>
    public static bool Remove(string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        lock (_lock)
        {
            // Always remove from persisted registrations
            var wasInCache = _persistedRegistrations.Remove(resourceType);
            
            if (_registrationManager == null)
            {
                return wasInCache;
            }

            try
            {
                // Find the plugin that handles this resource type
                var registeredPlugins = _registrationManager.GetRegisteredPlugins();
                var targetPlugin = registeredPlugins.FirstOrDefault(p => 
                    p.ResourceHandlers.ContainsKey(resourceType));

                if (targetPlugin != null)
                {
                    var success = _registrationManager.UnregisterPlugin(targetPlugin.Metadata.AssemblyName);
                    
                    if (success)
                    {
                        _logger.LogInformation("Legacy handler unregistered for resource type: {ResourceType}", resourceType);
                    }
                    
                    return success;
                }

                _logger.LogDebug("No handler found for resource type: {ResourceType}", resourceType);
                return wasInCache; // Return true if it was in cache, even if not in manager
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering legacy handler for resource type: {ResourceType}", resourceType);
                return wasInCache;
            }
        }
    }

    /// <summary>
    /// Gets the handler type for a specific resource type.
    /// Provides compatibility with legacy handler lookup patterns.
    /// </summary>
    /// <param name="resourceType">The resource type ID to look up.</param>
    /// <returns>The handler type, or null if not found.</returns>
    public static Type? GetHandler(string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        lock (_lock)
        {
            return _registrationManager?.GetResourceHandler(resourceType);
        }
    }

    /// <summary>
    /// Gets all supported resource types.
    /// Provides compatibility with legacy enumeration patterns.
    /// </summary>
    /// <returns>Array of supported resource type IDs.</returns>
    public static string[] GetSupportedTypes()
    {
        lock (_lock)
        {
            return _registrationManager?.GetSupportedResourceTypes() ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Checks if a resource type is supported.
    /// </summary>
    /// <param name="resourceType">The resource type ID to check.</param>
    /// <returns>True if the resource type is supported; false otherwise.</returns>
    public static bool IsSupported(string resourceType)
    {
        return GetHandler(resourceType) != null;
    }

    /// <summary>
    /// Creates a synthetic plugin metadata for manually registered handlers.
    /// This allows legacy Add() calls to work with the modern plugin system.
    /// </summary>
    /// <param name="handlerType">The handler type being registered.</param>
    /// <param name="resourceType">The resource type it handles.</param>
    /// <returns>Synthetic plugin metadata.</returns>
    private static PluginMetadata CreateSyntheticPluginMetadata(Type handlerType, string resourceType)
    {
        var assembly = handlerType.Assembly;
        var assemblyName = assembly.GetName();
        var location = assembly.Location;

        // If no location (e.g., dynamic assembly), use a synthetic path
        if (string.IsNullOrEmpty(location))
        {
            location = $"synthetic://{assemblyName.Name ?? "Unknown"}";
        }

        // Create a unique plugin name that includes the resource type and handler type
        // This ensures each registration gets its own plugin entry
        var uniquePluginName = $"LegacyBridge-{handlerType.FullName}-{resourceType}";

        return new PluginMetadata
        {
            FilePath = location,
            AssemblyName = uniquePluginName, // Make it unique
            AssemblyFullName = $"{uniquePluginName}, Version=1.0.0.0",
            Version = assemblyName.Version ?? new Version(1, 0, 0, 0),
            TargetFramework = GetTargetFramework(assembly),
            IsSigned = IsAssemblySigned(assembly),
            FileSizeBytes = GetFileSize(location),
            LastModified = GetLastModified(location),
            IsCompatible = true, // Assume legacy handlers are compatible
            CompatibilityWarnings = Array.Empty<string>(),
            IncompatibilityReason = null,
            PotentialResourceHandlers = new[] { handlerType.FullName ?? handlerType.Name }
        };
    }

    private static string? GetTargetFramework(Assembly assembly)
    {
        try
        {
            var targetFrameworkAttribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            return targetFrameworkAttribute?.FrameworkName;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAssemblySigned(Assembly assembly)
    {
        try
        {
            var publicKey = assembly.GetName().GetPublicKey();
            return publicKey != null && publicKey.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static long GetFileSize(string filePath)
    {
        try
        {
            if (filePath.StartsWith("synthetic://"))
                return 0;
            
            if (File.Exists(filePath))
            {
                return new FileInfo(filePath).Length;
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    private static DateTime GetLastModified(string filePath)
    {
        try
        {
            if (filePath.StartsWith("synthetic://"))
                return DateTime.UtcNow;
            
            if (File.Exists(filePath))
            {
                return new FileInfo(filePath).LastWriteTimeUtc;
            }
        }
        catch
        {
            // Ignore errors
        }
        return DateTime.UtcNow;
    }
}
