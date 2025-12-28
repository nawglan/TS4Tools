
namespace TS4Tools.Resources;

/// <summary>
/// Registry for resource handlers that maps resource types to factories.
/// </summary>
public sealed class ResourceHandlerRegistry : IResourceHandlerRegistry
{
    private readonly ConcurrentDictionary<uint, IResourceFactory> _factories = new();
    private IResourceFactory _defaultFactory;

    /// <summary>
    /// Creates a new resource handler registry with a default factory.
    /// </summary>
    public ResourceHandlerRegistry()
    {
        _defaultFactory = new DefaultResourceFactory();
    }

    /// <inheritdoc/>
    public IResourceFactory DefaultFactory
    {
        get => _defaultFactory;
        set => _defaultFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<uint> RegisteredTypes => _factories.Keys.ToList().AsReadOnly();

    /// <inheritdoc/>
    public IResourceFactory? GetFactory(uint resourceType)
    {
        return _factories.TryGetValue(resourceType, out var factory) ? factory : null;
    }

    /// <summary>
    /// Gets the factory for a resource type, or the default factory if not found.
    /// </summary>
    public IResourceFactory GetFactoryOrDefault(uint resourceType)
    {
        return GetFactory(resourceType) ?? _defaultFactory;
    }

    /// <summary>
    /// Registers a factory for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <param name="factory">The factory to register.</param>
    /// <returns>True if registered successfully, false if a factory was already registered.</returns>
    public bool Register(uint resourceType, IResourceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return _factories.TryAdd(resourceType, factory);
    }

    /// <summary>
    /// Registers a factory for a specific resource type, replacing any existing registration.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <param name="factory">The factory to register.</param>
    public void RegisterOrReplace(uint resourceType, IResourceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[resourceType] = factory;
    }

    /// <summary>
    /// Unregisters a factory for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <returns>True if unregistered, false if not found.</returns>
    public bool Unregister(uint resourceType)
    {
        return _factories.TryRemove(resourceType, out _);
    }

    /// <summary>
    /// Discovers and registers all resource handlers from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public void DiscoverHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(IResourceFactory).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                continue;

            var attributes = type.GetCustomAttributes<ResourceHandlerAttribute>();
            if (!attributes.Any())
                continue;

            // Create factory instance
            IResourceFactory? factory = null;
            try
            {
                factory = (IResourceFactory?)Activator.CreateInstance(type);
            }
            catch
            {
                // Skip types that can't be instantiated
                continue;
            }

            if (factory == null)
                continue;

            // Register for all declared resource types
            foreach (var attr in attributes)
            {
                if (attr.ResourceType == 0)
                {
                    // Resource type 0 means default handler
                    _defaultFactory = factory;
                }
                else
                {
                    Register(attr.ResourceType, factory);
                }
            }
        }
    }

    /// <summary>
    /// Discovers and registers all resource handlers from all loaded assemblies.
    /// </summary>
    public void DiscoverAllHandlers()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                DiscoverHandlers(assembly);
            }
            catch
            {
                // Skip assemblies that can't be scanned
            }
        }
    }

    /// <summary>
    /// Creates a shared registry instance with default handlers.
    /// </summary>
    public static ResourceHandlerRegistry CreateDefault()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverAllHandlers();
        return registry;
    }
}
