namespace TS4Tools.Core.Plugins;

/// <summary>
/// Registry for resource wrapper types and their associated resource type mappings.
/// This provides the modern replacement for the legacy WrapperDealer TypeMap system.
/// </summary>
public interface IResourceWrapperRegistry
{
    /// <summary>
    /// Register a handler type for one or more resource types.
    /// </summary>
    /// <param name="handlerType">The resource handler type</param>
    /// <param name="resourceTypes">Collection of resource type identifiers this handler supports</param>
    void RegisterHandler(Type handlerType, IEnumerable<string> resourceTypes);

    /// <summary>
    /// Unregister a handler type from all resource types.
    /// </summary>
    /// <param name="handlerType">Handler type to remove</param>
    void UnregisterHandler(Type handlerType);

    /// <summary>
    /// Get the handler type for a specific resource type.
    /// </summary>
    /// <param name="resourceType">Resource type identifier</param>
    /// <returns>Handler type, or null if no handler found</returns>
    Type? GetHandlerType(string resourceType);

    /// <summary>
    /// Get all registered handler mappings.
    /// This maintains compatibility with legacy WrapperDealer.TypeMap access patterns.
    /// </summary>
    /// <returns>Collection of resource type to handler type mappings</returns>
    IEnumerable<KeyValuePair<string, Type>> GetAllHandlers();

    /// <summary>
    /// Disable a handler type (add to disabled list).
    /// This maintains compatibility with legacy WrapperDealer.Disabled functionality.
    /// </summary>
    /// <param name="handlerType">Handler type to disable</param>
    void DisableHandler(Type handlerType);

    /// <summary>
    /// Enable a previously disabled handler type.
    /// </summary>
    /// <param name="handlerType">Handler type to enable</param>
    void EnableHandler(Type handlerType);

    /// <summary>
    /// Check if a handler type is currently disabled.
    /// </summary>
    /// <param name="handlerType">Handler type to check</param>
    /// <returns>True if disabled, false otherwise</returns>
    bool IsHandlerDisabled(Type handlerType);

    /// <summary>
    /// Get all disabled handler mappings.
    /// This maintains compatibility with legacy WrapperDealer.Disabled collection access.
    /// </summary>
    /// <returns>Collection of disabled resource type to handler type mappings</returns>
    ICollection<KeyValuePair<string, Type>> GetDisabledHandlers();
}
