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
using TS4Tools.Core.Package.Compression;

namespace TS4Tools.Core.Package.DependencyInjection;

/// <summary>
/// Dependency injection extension methods for TS4Tools.Core.Package services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TS4Tools.Core.Package services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTS4ToolsPackageServices(this IServiceCollection services)
    {
        // Register compression services
        services.AddSingleton<ICompressionService, ZlibCompressionService>();
        
        // Register package services
        services.AddSingleton<IPackageFactory, PackageFactory>();
        services.AddScoped<IPackageService, PackageService>();

        return services;
    }
}
