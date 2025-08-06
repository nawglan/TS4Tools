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
using TS4Tools.Resources.Catalog;
using TS4Tools.Resources.Images;
using TS4Tools.Resources.Strings;
using TS4Tools.Resources.Text;

namespace TS4Tools.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering all resource wrapper factories.
/// </summary>
public static class ResourceFactoryExtensions
{
    /// <summary>
    /// Registers all available resource wrapper factories with the dependency injection container.
    /// This ensures they are available for automatic discovery by the ResourceWrapperRegistry.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <returns>The service collection for fluent configuration</returns>
    public static IServiceCollection AddAllResourceFactories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register String Table Resource Factory
        services.AddResourceFactory<StringTableResource, StringTableResourceFactory>();

        // Register Image Resource Factory
        services.AddResourceFactory<ImageResource, ImageResourceFactory>();

        // Register Catalog Resource Factory
        services.AddResourceFactory<CatalogResource, CatalogResourceFactory>();

        // Register Text Resource Factory
        services.AddResourceFactory<ITextResource, TextResourceFactory>();

        return services;
    }
}
