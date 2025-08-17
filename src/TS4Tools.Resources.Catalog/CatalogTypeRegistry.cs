using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Registry for catalog resource types and their associated factories.
/// Provides automatic discovery and registration of catalog resource implementations.
/// </summary>
public sealed class CatalogTypeRegistry
{
    private readonly ILogger<CatalogTypeRegistry> _logger;
    private readonly Dictionary<uint, CatalogTypeInfo> _typeRegistry;
    private readonly Dictionary<Type, CatalogTypeInfo> _typeByClass;
    private readonly List<IResourceFactory> _factories;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTypeRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    public CatalogTypeRegistry(ILogger<CatalogTypeRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _typeRegistry = new Dictionary<uint, CatalogTypeInfo>();
        _typeByClass = new Dictionary<Type, CatalogTypeInfo>();
        _factories = new List<IResourceFactory>();
    }

    /// <summary>
    /// Gets the collection of registered catalog types.
    /// </summary>
    public IReadOnlyCollection<CatalogTypeInfo> RegisteredTypes => _typeRegistry.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets the collection of registered factories.
    /// </summary>
    public IReadOnlyCollection<IResourceFactory> RegisteredFactories => _factories.AsReadOnly();

    /// <summary>
    /// Discovers and registers all catalog resource types from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for catalog types.</param>
    /// <param name="cancellationToken">Token to cancel the discovery operation.</param>
    /// <returns>A task representing the discovery operation.</returns>
    public async Task DiscoverTypesAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Starting catalog type discovery");

        var discoveredCount = 0;

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var types = await DiscoverTypesFromAssemblyAsync(assembly, cancellationToken).ConfigureAwait(false);
                discoveredCount += types.Count();

                foreach (var typeInfo in types)
                {
                    RegisterType(typeInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover types from assembly {AssemblyName}", assembly.FullName);
            }
        }

        _logger.LogInformation("Catalog type discovery completed: {Count} types discovered", discoveredCount);
    }

    /// <summary>
    /// Discovers catalog types from the current application domain.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the discovery operation.</param>
    /// <returns>A task representing the discovery operation.</returns>
    public async Task DiscoverTypesFromCurrentDomainAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName?.Contains("TS4Tools") == true);

        await DiscoverTypesAsync(assemblies, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers a catalog type manually.
    /// </summary>
    /// <param name="typeInfo">The catalog type information.</param>
    public void RegisterType(CatalogTypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        foreach (var resourceTypeId in typeInfo.SupportedResourceTypes)
        {
            if (_typeRegistry.ContainsKey(resourceTypeId))
            {
                var existing = _typeRegistry[resourceTypeId];
                _logger.LogWarning("Resource type {ResourceType:X8} is already registered by {ExistingType}, overriding with {NewType}",
                    resourceTypeId, existing.ResourceType.Name, typeInfo.ResourceType.Name);
            }

            _typeRegistry[resourceTypeId] = typeInfo;
        }

        _typeByClass[typeInfo.ResourceType] = typeInfo;

        _logger.LogDebug("Registered catalog type {TypeName} for resource types: {ResourceTypes}",
            typeInfo.ResourceType.Name,
            string.Join(", ", typeInfo.SupportedResourceTypes.Select(rt => $"0x{rt:X8}")));
    }

    /// <summary>
    /// Registers a resource factory.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    public void RegisterFactory(IResourceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factories.Add(factory);
        _factories.Sort((f1, f2) => f2.Priority.CompareTo(f1.Priority));

        _logger.LogDebug("Registered factory {FactoryType} with priority {Priority}",
            factory.GetType().Name, factory.Priority);
    }

    /// <summary>
    /// Gets the catalog type information for the specified resource type.
    /// </summary>
    /// <param name="resourceTypeId">The resource type identifier.</param>
    /// <returns>The catalog type information, or null if not found.</returns>
    public CatalogTypeInfo? GetTypeInfo(uint resourceTypeId)
    {
        _typeRegistry.TryGetValue(resourceTypeId, out var typeInfo);
        return typeInfo;
    }

    /// <summary>
    /// Gets the catalog type information for the specified resource class.
    /// </summary>
    /// <param name="resourceType">The resource type class.</param>
    /// <returns>The catalog type information, or null if not found.</returns>
    public CatalogTypeInfo? GetTypeInfo(Type resourceType)
    {
        _typeByClass.TryGetValue(resourceType, out var typeInfo);
        return typeInfo;
    }

    /// <summary>
    /// Gets the appropriate factory for the specified resource type.
    /// </summary>
    /// <param name="resourceTypeId">The resource type identifier.</param>
    /// <returns>The factory, or null if none found.</returns>
    public IResourceFactory? GetFactory(uint resourceTypeId)
    {
        return _factories.FirstOrDefault(f => f.SupportedResourceTypes.Contains(resourceTypeId.ToString("X8")));
    }

    /// <summary>
    /// Checks if a resource type is supported.
    /// </summary>
    /// <param name="resourceTypeId">The resource type identifier.</param>
    /// <returns>True if the resource type is supported; otherwise, false.</returns>
    public bool IsSupported(uint resourceTypeId)
    {
        return _typeRegistry.ContainsKey(resourceTypeId);
    }

    /// <summary>
    /// Gets all factories that support the specified resource type, ordered by priority.
    /// </summary>
    /// <param name="resourceTypeId">The resource type identifier.</param>
    /// <returns>The collection of factories that support the resource type.</returns>
    public IEnumerable<IResourceFactory> GetFactoriesForType(uint resourceTypeId)
    {
        var resourceTypeString = resourceTypeId.ToString("X8");
        return _factories.Where(f => f.SupportedResourceTypes.Contains(resourceTypeString));
    }

    /// <summary>
    /// Discovers catalog types from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The discovered catalog type information.</returns>
    private async Task<IEnumerable<CatalogTypeInfo>> DiscoverTypesFromAssemblyAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var discoveredTypes = new List<CatalogTypeInfo>();

        try
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(ICatalogResource).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<CatalogResourceAttribute>() != null);

            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attribute = type.GetCustomAttribute<CatalogResourceAttribute>();
                if (attribute != null)
                {
                    var typeInfo = new CatalogTypeInfo(
                        type,
                        attribute.SupportedResourceTypes,
                        attribute.CatalogType,
                        attribute.Priority,
                        attribute.Description);

                    discoveredTypes.Add(typeInfo);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Failed to load some types from assembly {AssemblyName}: {LoaderExceptions}",
                assembly.FullName,
                string.Join(", ", ex.LoaderExceptions?.Select(le => le?.Message) ?? Array.Empty<string>()));

            // Continue with the types that could be loaded
            if (ex.Types != null)
            {
                foreach (var type in ex.Types.Where(t => t != null))
                {
                    // Process successfully loaded types
                }
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return discoveredTypes;
    }
}

/// <summary>
/// Information about a catalog resource type.
/// </summary>
/// <param name="ResourceType">The .NET type that implements the catalog resource.</param>
/// <param name="SupportedResourceTypes">The resource type IDs supported by this implementation.</param>
/// <param name="CatalogType">The catalog type classification.</param>
/// <param name="Priority">The priority for factory selection (higher values take precedence).</param>
/// <param name="Description">A description of the catalog resource type.</param>
public sealed record CatalogTypeInfo(
    Type ResourceType,
    IReadOnlyList<uint> SupportedResourceTypes,
    CatalogType CatalogType,
    int Priority,
    string Description);

/// <summary>
/// Attribute to mark catalog resource implementations for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CatalogResourceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogResourceAttribute"/> class.
    /// </summary>
    /// <param name="supportedResourceTypes">The resource type IDs supported by this implementation.</param>
    /// <param name="catalogType">The catalog type classification.</param>
    /// <param name="priority">The priority for factory selection.</param>
    /// <param name="description">A description of the catalog resource type.</param>
    public CatalogResourceAttribute(uint[] supportedResourceTypes, CatalogType catalogType = CatalogType.Unknown, int priority = 100, string description = "")
    {
        SupportedResourceTypes = supportedResourceTypes?.ToList().AsReadOnly() ?? new List<uint>().AsReadOnly();
        CatalogType = catalogType;
        Priority = priority;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the resource type IDs supported by this implementation.
    /// </summary>
    public IReadOnlyList<uint> SupportedResourceTypes { get; }

    /// <summary>
    /// Gets the catalog type classification.
    /// </summary>
    public CatalogType CatalogType { get; }

    /// <summary>
    /// Gets the priority for factory selection.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets the description of the catalog resource type.
    /// </summary>
    public string Description { get; }
}
