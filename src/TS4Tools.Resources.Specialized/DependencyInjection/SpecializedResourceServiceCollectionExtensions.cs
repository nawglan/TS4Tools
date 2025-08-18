using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Interfaces.Resources.Specialized;
using TS4Tools.Resources.Specialized;
using TS4Tools.Resources.Specialized.Templates;
using TS4Tools.Resources.Specialized.Configuration;
using TS4Tools.Resources.Specialized.Geometry;
using TS4Tools.Resources.Specialized.Geometry.Factories;

namespace TS4Tools.Resources.Specialized.DependencyInjection;

/// <summary>
/// Phase 4.19 Specialized Resource Services DI Extensions.
/// Provides dependency injection configuration for specialized/legacy resource types.
/// Implements P1 CRITICAL requirement for specialized resource factory registration.
/// </summary>
public static class SpecializedResourceServiceCollectionExtensions
{
    /// <summary>
    /// Adds specialized resource services to the service collection.
    /// This enables support for advanced modding scenarios requiring specialized resource types.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpecializedResources(this IServiceCollection services)
    {
        // Register NGMPHashMapResource factory (0xF3A38370) - P0 CRITICAL
        services.AddTransient<NGMPHashMapResourceFactory>();
        services.AddTransient<INGMPHashMapResource>(provider =>
        {
            var factory = provider.GetRequiredService<NGMPHashMapResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register ObjKeyResource factory (Phase 4.19 P1 CRITICAL)
        services.AddTransient<ObjKeyResourceFactory>();
        services.AddTransient<IObjKeyResource>(provider =>
        {
            var factory = provider.GetRequiredService<ObjKeyResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register HashMapResource factory (Phase 4.19 P1 CRITICAL)
        services.AddTransient<HashMapResourceFactory>();
        services.AddTransient<IHashMapResource>(provider =>
        {
            var factory = provider.GetRequiredService<HashMapResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register UserCAStPresetResource factory (Phase 4.19 P2 HIGH)
        services.AddTransient<UserCAStPresetResourceFactory>();
        services.AddTransient<IUserCAStPresetResource>(provider =>
        {
            var factory = provider.GetRequiredService<UserCAStPresetResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register PresetResource factory (Phase 4.19 P2 HIGH)
        services.AddTransient<PresetResourceFactory>();
        services.AddTransient<IPresetResource>(provider =>
        {
            var factory = provider.GetRequiredService<PresetResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register SwatchResource factory (Phase 4.19 P2 HIGH)
        services.AddTransient<SwatchResourceFactory>();
        services.AddTransient<ISwatchResource>(provider =>
        {
            var factory = provider.GetRequiredService<SwatchResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register ComplateResource factory (Phase 4.19 P3 MEDIUM)
        services.AddTransient<ComplateResourceFactory>();
        services.AddTransient<IComplateResource>(provider =>
        {
            var factory = provider.GetRequiredService<ComplateResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register TuningResource factory (Phase 4.19 P3 MEDIUM)
        services.AddTransient<TuningResourceFactory>();
        services.AddTransient<ITuningResource>(provider =>
        {
            var factory = provider.GetRequiredService<TuningResourceFactory>();
            return Task.Run(async () => await factory.CreateTuningAsync("default", "configuration", 0, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register NameMapResource factory (Phase 4.19 P3 MEDIUM)
        services.AddTransient<NameMapResourceFactory>();
        services.AddTransient<INameMapResource>(provider =>
        {
            var factory = provider.GetRequiredService<NameMapResourceFactory>();
            return Task.Run(async () => await factory.CreateNameMapAsync("default", "1.0", "general", false, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register BlendGeometryResource factory (Phase 4.19 P4 LOW)
        services.AddTransient<BlendGeometryResourceFactory>();
        services.AddTransient<IBlendGeometryResource>(provider =>
        {
            var factory = provider.GetRequiredService<BlendGeometryResourceFactory>();
            return Task.Run(async () => await factory.CreateAsync("default_mesh", "1.0", default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register TerrainGeometryResource factory (Phase 4.19 P4 LOW)
        services.AddTransient<TerrainGeometryResourceFactory>();
        services.AddTransient<ITerrainGeometryResource>(provider =>
        {
            var factory = provider.GetRequiredService<TerrainGeometryResourceFactory>();
            return Task.Run(async () => await factory.CreateAsync(256, 256, 1.0f, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        // Register ConfigurationResource factory (Phase 4.19 P3 MEDIUM)
        services.AddTransient<ConfigurationResourceFactory>();
        services.AddTransient<IConfigurationResource>(provider =>
        {
            var factory = provider.GetRequiredService<ConfigurationResourceFactory>();
            return Task.Run(async () => await factory.CreateResourceAsync(1, null, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        });

        return services;
    }
}
