namespace TS4Tools;

/// <summary>
/// Attribute to mark a class as a resource handler and specify which resource types it handles.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ResourceHandlerAttribute : Attribute
{
    /// <summary>
    /// The resource type this handler supports.
    /// Use 0 for a default/fallback handler.
    /// </summary>
    public uint ResourceType { get; }

    /// <summary>
    /// Optional content tag (e.g., "_IMG", "_RIG", "_STBL").
    /// </summary>
    public string? ContentTag { get; init; }

    /// <summary>
    /// Creates a new resource handler attribute.
    /// </summary>
    /// <param name="resourceType">The resource type code (e.g., 0x220557DA for STBL).</param>
    public ResourceHandlerAttribute(uint resourceType)
    {
        ResourceType = resourceType;
    }
}

/// <summary>
/// Factory interface for creating resource instances.
/// </summary>
public interface IResourceFactory
{
    /// <summary>
    /// Creates a resource from raw data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The raw resource data (decompressed).</param>
    /// <returns>The parsed resource.</returns>
    IResource Create(ResourceKey key, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Creates an empty resource with default values.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>A new empty resource.</returns>
    IResource CreateEmpty(ResourceKey key);
}

/// <summary>
/// Registry for resource handlers.
/// </summary>
public interface IResourceHandlerRegistry
{
    /// <summary>
    /// Gets the factory for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <returns>The factory, or null if not found.</returns>
    IResourceFactory? GetFactory(uint resourceType);

    /// <summary>
    /// Gets the default factory for unrecognized resource types.
    /// </summary>
    IResourceFactory DefaultFactory { get; }

    /// <summary>
    /// Gets all registered resource types.
    /// </summary>
    IReadOnlyCollection<uint> RegisteredTypes { get; }
}
