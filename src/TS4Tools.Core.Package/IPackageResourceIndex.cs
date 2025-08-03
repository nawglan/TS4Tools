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

namespace TS4Tools.Core.Package;

/// <summary>
/// Interface for package resource index management
/// </summary>
public interface IPackageResourceIndex : IReadOnlyCollection<IResourceIndexEntry>
{
    /// <summary>
    /// Index type information
    /// </summary>
    uint IndexType { get; }
    
    /// <summary>
    /// Get resource index entry by key
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <returns>Index entry or null if not found</returns>
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers - ResourceKey is appropriate here
    IResourceIndexEntry? this[IResourceKey key] { get; }
#pragma warning restore CA1043
    
    /// <summary>
    /// Check if the index contains a resource with the specified key
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <returns>True if resource exists</returns>
    bool Contains(IResourceKey key);
    
    /// <summary>
    /// Get all resource keys in the index
    /// </summary>
    /// <returns>Collection of resource keys</returns>
    IEnumerable<IResourceKey> GetResourceKeys();
    
    /// <summary>
    /// Get resource index entries by type
    /// </summary>
    /// <param name="resourceType">Resource type to filter by</param>
    /// <returns>Collection of matching index entries</returns>
    IEnumerable<IResourceIndexEntry> GetByResourceType(uint resourceType);
    
    /// <summary>
    /// Get resource index entries by group
    /// </summary>
    /// <param name="resourceGroup">Resource group to filter by</param>
    /// <returns>Collection of matching index entries</returns>
    IEnumerable<IResourceIndexEntry> GetByResourceGroup(uint resourceGroup);
    
    /// <summary>
    /// Try to get a resource index entry by key
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <param name="entry">Output index entry</param>
    /// <returns>True if found, false otherwise</returns>
    bool TryGetValue(IResourceKey key, [NotNullWhen(true)] out IResourceIndexEntry? entry);
}
