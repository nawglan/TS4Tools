using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Text.DependencyInjection;

/// <summary>
/// Dependency injection extensions for text resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds text resource services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTextResourceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the text resource factory as both specific and generic resource factory
        services.AddSingleton<TextResourceFactory>();
        services.AddSingleton<IResourceFactory<ITextResource>>(provider => provider.GetRequiredService<TextResourceFactory>());

        return services;
    }
}
