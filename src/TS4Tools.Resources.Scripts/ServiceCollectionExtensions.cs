using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Scripts;

/// <summary>
/// Extension methods for registering script resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds script resource services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddScriptResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the factory as both the interface and implementation
        services.TryAddSingleton<ScriptResourceFactory>();
        services.TryAddSingleton<IResourceFactory<IScriptResource>>(provider => 
            provider.GetRequiredService<ScriptResourceFactory>());

        // Register the base factory interface for polymorphic access
        services.TryAddSingleton<IResourceFactory>(provider => 
            provider.GetRequiredService<ScriptResourceFactory>());

        return services;
    }
}
