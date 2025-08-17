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

        // Register concrete factory types first
        services.AddSingleton<AnimationResourceFactory>();
        services.AddSingleton<CharacterResourceFactory>();
        services.AddSingleton<RigResourceFactory>();
        services.AddSingleton<FacialAnimationResourceFactory>();

        // Register as interface implementations
        services.AddTransient<IResourceFactory, AnimationResourceFactory>(provider =>
            provider.GetRequiredService<AnimationResourceFactory>());
        services.AddTransient<IResourceFactory, CharacterResourceFactory>(provider =>
            provider.GetRequiredService<CharacterResourceFactory>());
        services.AddTransient<IResourceFactory, RigResourceFactory>(provider =>
            provider.GetRequiredService<RigResourceFactory>());
        services.AddTransient<IResourceFactory, FacialAnimationResourceFactory>(provider =>
            provider.GetRequiredService<FacialAnimationResourceFactory>());

        return services;
    }
}
