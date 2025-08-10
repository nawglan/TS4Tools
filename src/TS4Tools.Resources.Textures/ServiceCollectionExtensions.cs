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

using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Textures;

/// <summary>
/// Extension methods for configuring texture resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds texture resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddTextureResources(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register factories
        services.AddTransient<IResourceFactory, TxtcResourceFactory>();

        // Register resource types for dependency injection
        services.AddTransient<TxtcResource>();

        return services;
    }

    /// <summary>
    /// Adds texture resource services with custom configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Configuration action for texture services.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddTextureResources(this IServiceCollection services, Action<TextureResourceOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Configure options
        var options = new TextureResourceOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register services based on options
        return AddTextureResources(services);
    }
}

/// <summary>
/// Configuration options for texture resource services.
/// </summary>
public class TextureResourceOptions
{
    /// <summary>Gets or sets whether to enable texture compression support.</summary>
    public bool EnableCompressionSupport { get; set; } = true;

    /// <summary>Gets or sets whether to enable mipmap generation.</summary>
    public bool EnableMipmapGeneration { get; set; } = true;

    /// <summary>Gets or sets the maximum texture size for embedded data.</summary>
    public uint MaxEmbeddedTextureSize { get; set; } = 4096;

    /// <summary>Gets or sets whether to validate texture formats strictly.</summary>
    public bool StrictFormatValidation { get; set; } = true;

    /// <summary>Gets or sets the default texture format for new resources.</summary>
    public TextureFormat DefaultTextureFormat { get; set; } = TextureFormat.ARGB;

    /// <summary>Gets or sets whether to enable texture caching.</summary>
    public bool EnableTextureCaching { get; set; } = true;

    /// <summary>Gets or sets the maximum number of cached textures.</summary>
    public int MaxCachedTextures { get; set; } = 100;
}
