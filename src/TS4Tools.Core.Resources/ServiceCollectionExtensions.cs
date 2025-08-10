/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using Microsoft.Extensions.Configuration;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Extension methods for configuring resource management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds resource management services to the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddResourceManager(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure options
        services.Configure<ResourceManagerOptions>(options =>
        {
            configuration.GetSection(ResourceManagerOptions.SectionName).Bind(options);
        });

        // Register core services
        services.AddSingleton<IResourceManager, ResourceManager>();

        // Register resource wrapper registry
        services.AddSingleton<IResourceWrapperRegistry, ResourceWrapperRegistry>();

        // Register default factory
        services.AddSingleton<DefaultResourceFactory>();

        return services;
    }

    /// <summary>
    /// Adds resource management services with custom configuration.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Options configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddResourceManager(this IServiceCollection services, Action<ResourceManagerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options
        services.Configure(configureOptions);

        // Register core services
        services.AddSingleton<IResourceManager, ResourceManager>();

        // Register default factory
        services.AddSingleton<DefaultResourceFactory>();

        return services;
    }

    /// <summary>
    /// Registers a resource factory for a specific resource type.
    /// </summary>
    /// <typeparam name="TResource">Resource type</typeparam>
    /// <typeparam name="TFactory">Factory type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime (default: Singleton)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddResourceFactory<TResource, TFactory>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TResource : class, IResource
        where TFactory : class, IResourceFactory<TResource>
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the factory
        services.Add(new ServiceDescriptor(typeof(TFactory), typeof(TFactory), lifetime));

        return services;
    }

    /// <summary>
    /// Adds and configures resource wrapper registry with automatic factory discovery.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddResourceWrapperRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register resource wrapper registry (if not already registered)
        services.AddSingleton<IResourceWrapperRegistry, ResourceWrapperRegistry>();

        return services;
    }

    /// <summary>
    /// Initializes the resource wrapper registry by discovering and registering all available factories.
    /// Call this method after building the service provider to complete factory registration.
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization process</returns>
    public static async Task<ResourceWrapperRegistryResult> InitializeResourceWrapperRegistryAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var registry = serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        return await registry.DiscoverAndRegisterFactoriesAsync(cancellationToken);
    }
}
