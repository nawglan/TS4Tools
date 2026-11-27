// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs (handler → type list map)
// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 40-129 (registry semantics)

namespace TS4Tools;

/// <summary>
/// Attribute to mark a class as a resource handler and specify which resource types it handles.
/// </summary>
/// <remarks>
/// When several discovered factories claim the same resource type, the registry picks the claim with
/// the highest <see cref="Priority"/>; at equal priority an attribute claim beats a
/// <see cref="IResourceTypeProvider"/> list claim, and remaining ties are broken by the factory's full
/// type name (ordinal). The result never depends on assembly or type scan order.
/// </remarks>
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
    /// Precedence when several discovered factories claim the same type (higher wins). Default 0.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// For <c>[ResourceHandler(0)]</c> only: when true, discovery installs this factory as the registry's
    /// default (fallback) factory. Without it, a discovered type-0 handler is ignored (and reported in
    /// <c>ResourceHandlerRegistry.IgnoredDefaultHandlers</c>) so a plugin cannot silently replace the default.
    /// </summary>
    public bool ReplacesDefault { get; init; }

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
/// Implemented by an <see cref="IResourceFactory"/> whose handled resource types come from a list
/// (typically an embedded config file) rather than individual <see cref="ResourceHandlerAttribute"/>s.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 129-154 and
/// ImageResource/ImageResource.cs lines 71-96 (handlers built from TextResources.txt / ImageResources.txt).
/// Registry discovery treats implementing this interface as an opt-in equivalent to carrying a
/// <see cref="ResourceHandlerAttribute"/>. List claims rank below attribute claims of equal priority.
/// </remarks>
public interface IResourceTypeProvider
{
    /// <summary>
    /// Gets the resource types this factory handles.
    /// </summary>
    IReadOnlyCollection<uint> ResourceTypes { get; }

    /// <summary>
    /// Gets the precedence of the list claims (see <see cref="ResourceHandlerAttribute.Priority"/>). Default 0.
    /// </summary>
    int ResourceTypePriority => 0;
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

    /// <summary>
    /// Gets the factory for a resource type, or <see cref="DefaultFactory"/> when none is registered.
    /// </summary>
    IResourceFactory GetFactoryOrDefault(uint resourceType) => GetFactory(resourceType) ?? DefaultFactory;

    /// <summary>
    /// Creates a wrapper for existing resource data.
    /// </summary>
    /// <param name="key">The resource key; its type selects the factory.</param>
    /// <param name="data">The decompressed resource data.</param>
    /// <param name="alwaysDefault">When true, always use <see cref="DefaultFactory"/>.</param>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 64-67</remarks>
    IResource CreateResource(ResourceKey key, ReadOnlyMemory<byte> data, bool alwaysDefault = false) =>
        (alwaysDefault ? DefaultFactory : GetFactoryOrDefault(key.ResourceType)).Create(key, data);

    /// <summary>
    /// Creates a new, empty resource of the key's type.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 40-43</remarks>
    IResource CreateNewResource(ResourceKey key) => GetFactoryOrDefault(key.ResourceType).CreateEmpty(key);
}
