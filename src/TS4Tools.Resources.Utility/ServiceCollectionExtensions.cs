using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Extension methods for registering utility resources with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds utility resource factories to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUtilityResources(this IServiceCollection services)
    {
        // Register resource factories
        services.AddTransient<IResourceFactory, DataResourceFactory>();
        services.AddTransient<DataResourceFactory>();
        services.AddTransient<IResourceFactory, ConfigResourceFactory>();
        services.AddTransient<ConfigResourceFactory>();
        services.AddTransient<IResourceFactory, MetadataResourceFactory>();
        services.AddTransient<MetadataResourceFactory>();

        return services;
    }
}
