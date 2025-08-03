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
/// Representation of a Sims 4 Package (.package file)
/// </summary>
public interface IPackage : IApiVersion, IContentFields, IDisposable, IAsyncDisposable
{
    #region Package Properties
    
    /// <summary>
    /// Package header: "DBPF" bytes
    /// </summary>
    [ElementPriority(1)]
    ReadOnlySpan<byte> Magic { get; }
    
    /// <summary>
    /// Package header: Major version (typically 2)
    /// </summary>
    [ElementPriority(2)]
    int Major { get; }
    
    /// <summary>
    /// Package header: Minor version (typically 0)
    /// </summary>
    [ElementPriority(3)]
    int Minor { get; }
    
    /// <summary>
    /// Package header: User version major
    /// </summary>
    [ElementPriority(4)]
    int UserVersionMajor { get; }
    
    /// <summary>
    /// Package header: User version minor
    /// </summary>
    [ElementPriority(5)]
    int UserVersionMinor { get; }
    
    /// <summary>
    /// Package header: Creation time
    /// </summary>
    [ElementPriority(6)]
    DateTime CreatedDate { get; }
    
    /// <summary>
    /// Package header: Last modified time
    /// </summary>
    [ElementPriority(7)]
    DateTime ModifiedDate { get; }
    
    /// <summary>
    /// Number of resources in the package
    /// </summary>
    [ElementPriority(8)]
    int ResourceCount { get; }
    
    /// <summary>
    /// The package resource index
    /// </summary>
    IPackageResourceIndex ResourceIndex { get; }
    
    /// <summary>
    /// Indicates if the package has been modified since loading
    /// </summary>
    bool IsDirty { get; }
    
    /// <summary>
    /// The filename of the package if loaded from a file
    /// </summary>
    string? FileName { get; }
    
    #endregion
    
    #region Package Operations
    
    /// <summary>
    /// Save the package to its original location
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SavePackageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save the package to the specified stream
    /// </summary>
    /// <param name="stream">Target stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsAsync(Stream stream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save the package to the specified file path
    /// </summary>
    /// <param name="filePath">Target file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a resource by its key
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <returns>Resource or null if not found</returns>
    IResource? GetResource(IResourceKey key);
    
    /// <summary>
    /// Get a resource by its key asynchronously
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource or null if not found</returns>
    Task<IResource?> GetResourceAsync(IResourceKey key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add a resource to the package
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <param name="resource">Resource data</param>
    /// <param name="compressed">Whether to compress the resource</param>
    /// <returns>The added resource index entry</returns>
    IResourceIndexEntry AddResource(IResourceKey key, ReadOnlySpan<byte> resource, bool compressed = true);
    
    /// <summary>
    /// Remove a resource from the package
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <returns>True if resource was removed, false if not found</returns>
    bool RemoveResource(IResourceKey key);
    
    /// <summary>
    /// Compact the package by removing unused space
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task CompactAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Raised when the resource index is invalidated
    /// </summary>
    event EventHandler? ResourceIndexInvalidated;
    
    /// <summary>
    /// Raised when a resource is added
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceAdded;
    
    /// <summary>
    /// Raised when a resource is removed
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceRemoved;
    
    /// <summary>
    /// Raised when a resource is modified
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceModified;
    
    #endregion
}

/// <summary>
/// Event arguments for resource events
/// </summary>
public class ResourceEventArgs : EventArgs
{
    /// <summary>
    /// The resource key that was affected
    /// </summary>
    public IResourceKey ResourceKey { get; }
    
    /// <summary>
    /// The resource index entry (if available)
    /// </summary>
    public IResourceIndexEntry? IndexEntry { get; }
    
    /// <summary>
    /// Creates new resource event arguments
    /// </summary>
    /// <param name="resourceKey">The resource key</param>
    /// <param name="indexEntry">The index entry</param>
    public ResourceEventArgs(IResourceKey resourceKey, IResourceIndexEntry? indexEntry = null)
    {
        ResourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));
        IndexEntry = indexEntry;
    }
}
