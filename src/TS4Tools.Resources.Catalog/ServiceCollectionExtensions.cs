using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Resources.Catalog.Services;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Extension methods for registering catalog resource services in dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds catalog resource services to the dependency injection container.
    /// Registers all catalog resource factories and related services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddCatalogResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register catalog type registry
        services.AddSingleton<CatalogTypeRegistry>();

        // Register catalog resource factories
        services.AddSingleton<CatalogResourceFactory>();
        services.AddSingleton<ObjectCatalogResourceFactory>();
        services.AddSingleton<CatalogTagResourceFactory>();

        // Register catalog management services
        services.AddSingleton<ICatalogTagManagementService, CatalogTagManagementService>();

        return services;
    }
}
