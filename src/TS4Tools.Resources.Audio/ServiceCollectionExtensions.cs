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

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Extension methods for configuring audio and video resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds audio and video resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAudioVideoResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register sound resource factory
        services.AddSingleton<IResourceFactory, SoundResourceFactory>();
        
        // Register video resource factory
        services.AddSingleton<IResourceFactory, VideoResourceFactory>();

        return services;
    }

    /// <summary>
    /// Adds only sound resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSoundResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IResourceFactory, SoundResourceFactory>();

        return services;
    }

    /// <summary>
    /// Adds only video resource services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddVideoResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IResourceFactory, VideoResourceFactory>();

        return services;
    }
}
