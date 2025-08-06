using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Extension methods for configuring animation and character resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds animation and character resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAnimationResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register all animation and character resource factories
        services.AddTransient<IResourceFactory, AnimationResourceFactory>();
        services.AddTransient<IResourceFactory, CharacterResourceFactory>();
        services.AddTransient<IResourceFactory, RigResourceFactory>();
        
        return services;
    }
}
