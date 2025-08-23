using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Extension methods for configuring effect resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds effect resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEffectResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register concrete factory types first
        services.AddSingleton<EffectResourceFactory>();
        services.AddSingleton<LightResourceFactory>();

        // Register as interface implementations
        services.AddTransient<IResourceFactory, EffectResourceFactory>(provider =>
            provider.GetRequiredService<EffectResourceFactory>());
        services.AddTransient<IResourceFactory, LightResourceFactory>(provider =>
            provider.GetRequiredService<LightResourceFactory>());

        return services;
    }
}
