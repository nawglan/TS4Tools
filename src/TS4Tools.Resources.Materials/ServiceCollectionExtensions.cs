using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Materials;

/// <summary>
/// Extension methods for registering material resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds material resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMaterialResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register concrete factory types first
        services.AddSingleton<MaterialResourceFactory>();
        services.AddSingleton<MTBLResourceFactory>();

        // Register as interface implementations
        services.AddTransient<IResourceFactory, MaterialResourceFactory>(provider =>
            provider.GetRequiredService<MaterialResourceFactory>());
        services.AddTransient<IResourceFactory, MTBLResourceFactory>(provider =>
            provider.GetRequiredService<MTBLResourceFactory>());

        return services;
    }
}
