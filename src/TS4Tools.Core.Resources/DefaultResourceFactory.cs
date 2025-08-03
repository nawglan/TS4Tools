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

namespace TS4Tools.Core.Resources;

/// <summary>
/// Factory for creating default resource instances.
/// This factory handles any resource type as a fallback.
/// </summary>
internal sealed class DefaultResourceFactory : ResourceFactoryBase<IResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResourceFactory"/> class.
    /// </summary>
    public DefaultResourceFactory() : base(new[] { "*" }, priority: -1000) // Lowest priority
    {
    }
    
    /// <inheritdoc />
    public override async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        
        // For async consistency, but no actual async work needed for default resource
        await Task.CompletedTask;
        
        return new DefaultResource(apiVersion, stream);
    }
}
