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

using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Factory for creating modular resources that handle modular building components and assembly rules.
/// </summary>
public sealed class ModularResourceFactory : ResourceFactoryBase<ModularResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModularResourceFactory"/> class.
    /// </summary>
    public ModularResourceFactory() : base(new[] { "MODULAR", "0xCF9A4ACE" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<ModularResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        var key = new ResourceKey(0xCF9A4ACE, 0x00000000, 0x0000000000000000);
        var resource = new ModularResource(key, 1);

        if (stream != null)
        {
            await resource.LoadFromStreamAsync(stream, cancellationToken);
        }

        return resource;
    }
}
