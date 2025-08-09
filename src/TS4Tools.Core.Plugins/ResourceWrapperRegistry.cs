using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Plugins;

/// <summary>
/// Registry implementation for resource wrapper types and their mappings.
/// This provides the modern replacement for the legacy WrapperDealer TypeMap system.
/// </summary>
public class ResourceWrapperRegistry : IResourceWrapperRegistry
{
    private readonly ConcurrentDictionary<string, Type> _typeMap = new();
    private readonly ConcurrentHashSet<Type> _disabledHandlers = new();
    private readonly ILogger<ResourceWrapperRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceWrapperRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ResourceWrapperRegistry(ILogger<ResourceWrapperRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a handler type for one or more resource types.
    /// </summary>
    public void RegisterHandler(Type handlerType, IEnumerable<string> resourceTypes)
    {
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        if (resourceTypes == null)
            throw new ArgumentNullException(nameof(resourceTypes));

        var resourceTypeList = resourceTypes.ToList();
        if (!resourceTypeList.Any())
            throw new ArgumentException("At least one resource type must be specified", nameof(resourceTypes));

        foreach (var resourceType in resourceTypeList)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                continue;

            _typeMap.AddOrUpdate(resourceType, handlerType, (key, existing) =>
            {
                _logger.LogWarning("Handler type {NewType} replaced {OldType} for resource type {ResourceType}",
                    handlerType.Name, existing.Name, resourceType);
                return handlerType;
            });
        }

        _logger.LogDebug("Registered handler {HandlerType} for {Count} resource types",
            handlerType.Name, resourceTypeList.Count);
    }

    /// <summary>
    /// Unregister a handler type from all resource types.
    /// </summary>
    public void UnregisterHandler(Type handlerType)
    {
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        var removedTypes = new List<string>();

        foreach (var kvp in _typeMap.ToList())
        {
            if (kvp.Value == handlerType)
            {
                _typeMap.TryRemove(kvp.Key, out _);
                removedTypes.Add(kvp.Key);
            }
        }

        if (removedTypes.Any())
        {
            _logger.LogInformation("Unregistered handler {HandlerType} from {Count} resource types",
                handlerType.Name, removedTypes.Count);
        }
    }

    /// <summary>
    /// Get the handler type for a specific resource type.
    /// </summary>
    public Type? GetHandlerType(string resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
            return null;

        if (_typeMap.TryGetValue(resourceType, out var handlerType) &&
            !_disabledHandlers.Contains(handlerType))
        {
            return handlerType;
        }

        // Fallback to default handler (wildcard pattern from legacy WrapperDealer)
        if (_typeMap.TryGetValue("*", out var defaultHandler) &&
            !_disabledHandlers.Contains(defaultHandler))
        {
            return defaultHandler;
        }

        return null;
    }

    /// <summary>
    /// Get all registered handler mappings.
    /// This maintains compatibility with legacy WrapperDealer.TypeMap access patterns.
    /// </summary>
    public IEnumerable<KeyValuePair<string, Type>> GetAllHandlers()
    {
        return _typeMap
            .Where(kvp => !_disabledHandlers.Contains(kvp.Value))
            .OrderBy(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Disable a handler type (add to disabled list).
    /// </summary>
    public void DisableHandler(Type handlerType)
    {
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        if (_disabledHandlers.Add(handlerType))
        {
            _logger.LogInformation("Disabled handler type: {HandlerType}", handlerType.Name);
        }
    }

    /// <summary>
    /// Enable a previously disabled handler type.
    /// </summary>
    public void EnableHandler(Type handlerType)
    {
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        if (_disabledHandlers.Remove(handlerType))
        {
            _logger.LogInformation("Enabled handler type: {HandlerType}", handlerType.Name);
        }
    }

    /// <summary>
    /// Check if a handler type is currently disabled.
    /// </summary>
    public bool IsHandlerDisabled(Type handlerType)
    {
        if (handlerType == null)
            return false;

        return _disabledHandlers.Contains(handlerType);
    }

    /// <summary>
    /// Get all disabled handler mappings.
    /// This maintains compatibility with legacy WrapperDealer.Disabled collection access.
    /// </summary>
    public ICollection<KeyValuePair<string, Type>> GetDisabledHandlers()
    {
        return _typeMap
            .Where(kvp => _disabledHandlers.Contains(kvp.Value))
            .ToList();
    }
}

/// <summary>
/// Thread-safe HashSet implementation for concurrent access.
/// </summary>
internal class ConcurrentHashSet<T> where T : notnull
{
    private readonly HashSet<T> _hashSet = new();
    private readonly object _lock = new();

    public bool Add(T item)
    {
        lock (_lock)
        {
            return _hashSet.Add(item);
        }
    }

    public bool Remove(T item)
    {
        lock (_lock)
        {
            return _hashSet.Remove(item);
        }
    }

    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _hashSet.Contains(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _hashSet.Clear();
        }
    }
}
