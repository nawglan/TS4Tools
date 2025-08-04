using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Settings;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.Resources;
using TS4Tools.Core.System.Platform;
using TS4Tools.Extensions.ResourceTypes;
using TS4Tools.Extensions.Utilities;
using TS4Tools.Resources.Common.CatalogTags;

namespace TS4Tools.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering TS4Tools services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all TS4Tools core services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The configuration instance for settings binding.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddTS4ToolsCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register core settings system
        services.AddTS4ToolsSettings(configuration);

        // Register platform services (must be early in the chain)
        services.AddTS4ToolsPlatformServices();

        // Register package management services (from TS4Tools.Core.Package)
        services.AddTS4ToolsPackageServices();

        // Register resource management services
        services.AddResourceManager(configuration);

        // Register extension services
        services.AddTS4ToolsExtensions();

        // Register common resource utilities
        services.AddTS4ToolsResourceCommon();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }

    /// <summary>
    /// Registers TS4Tools platform services.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddTS4ToolsPlatformServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register platform service as singleton
        services.AddSingleton<IPlatformService, PlatformService>();

        return services;
    }

    /// <summary>
    /// Registers TS4Tools resource management services.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddTS4ToolsResourceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register resource services using the existing extension method
        // Note: This requires configuration but we'll handle it in the main method
        // services.AddResourceManager(configuration);

        return services;
    }

    /// <summary>
    /// Registers TS4Tools extension services.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddTS4ToolsExtensions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register extension services from TS4Tools.Extensions
        services.AddSingleton<IResourceTypeRegistry, ResourceTypeRegistry>();
        services.AddSingleton<IFileNameService, FileNameService>();

        return services;
    }

    /// <summary>
    /// Registers TS4Tools common resource utilities.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddTS4ToolsResourceCommon(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register common resource services from TS4Tools.Resources.Common
        services.AddSingleton<CatalogTagRegistry>();

        return services;
    }

    /// <summary>
    /// Configures a hosted service application with all TS4Tools services.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for fluent configuration.</returns>
    public static IHostApplicationBuilder AddTS4ToolsServices(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add all TS4Tools services
        builder.Services.AddTS4ToolsCore(builder.Configuration);

        return builder;
    }
}
