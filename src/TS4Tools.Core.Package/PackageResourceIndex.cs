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
/// Implementation of package resource index with high-performance lookups
/// </summary>
internal sealed class PackageResourceIndex : IPackageResourceIndex
{
    private readonly Dictionary<IResourceKey, IResourceIndexEntry> _index;
    private readonly Dictionary<uint, List<IResourceIndexEntry>> _typeIndex;
    private readonly Dictionary<uint, List<IResourceIndexEntry>> _groupIndex;

    /// <inheritdoc />
    public uint IndexType { get; }

    /// <inheritdoc />
    public int Count => _index.Count;

    /// <inheritdoc />
    public IResourceIndexEntry? this[IResourceKey key]
    {
        get
        {
            _index.TryGetValue(key, out var entry);
            return entry;
        }
    }

    /// <summary>
    /// Creates a new empty package resource index
    /// </summary>
    /// <param name="indexType">Index type</param>
    public PackageResourceIndex(uint indexType = 0)
    {
        IndexType = indexType;
        _index = new Dictionary<IResourceKey, IResourceIndexEntry>();
        _typeIndex = new Dictionary<uint, List<IResourceIndexEntry>>();
        _groupIndex = new Dictionary<uint, List<IResourceIndexEntry>>();
    }

    /// <summary>
    /// Creates a package resource index from existing entries
    /// </summary>
    /// <param name="indexType">Index type</param>
    /// <param name="entries">Initial entries</param>
    public PackageResourceIndex(uint indexType, IEnumerable<IResourceIndexEntry> entries)
        : this(indexType)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            AddInternal(entry);
        }
    }

    /// <inheritdoc />
    public bool Contains(IResourceKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _index.ContainsKey(key);
    }

    /// <inheritdoc />
    public IEnumerable<IResourceKey> GetResourceKeys()
    {
        return _index.Keys;
    }

    /// <inheritdoc />
    public IEnumerable<IResourceIndexEntry> GetByResourceType(uint resourceType)
    {
        if (_typeIndex.TryGetValue(resourceType, out var entries))
        {
            return entries;
        }
        return Enumerable.Empty<IResourceIndexEntry>();
    }

    /// <inheritdoc />
    public IEnumerable<IResourceIndexEntry> GetByResourceGroup(uint resourceGroup)
    {
        if (_groupIndex.TryGetValue(resourceGroup, out var entries))
        {
            return entries;
        }
        return Enumerable.Empty<IResourceIndexEntry>();
    }

    /// <inheritdoc />
    public bool TryGetValue(IResourceKey key, [NotNullWhen(true)] out IResourceIndexEntry? entry)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _index.TryGetValue(key, out entry);
    }

    /// <inheritdoc />
    public IEnumerator<IResourceIndexEntry> GetEnumerator()
    {
        return _index.Values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Add a resource index entry to the index
    /// </summary>
    /// <param name="entry">Entry to add</param>
    /// <returns>True if added, false if key already exists</returns>
    internal bool Add(IResourceIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_index.ContainsKey(entry))
        {
            return false;
        }

        AddInternal(entry);
        return true;
    }

    /// <summary>
    /// Remove a resource index entry from the index
    /// </summary>
    /// <param name="key">Resource key to remove</param>
    /// <returns>True if removed, false if not found</returns>
    internal bool Remove(IResourceKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_index.TryGetValue(key, out var entry))
        {
            return false;
        }

        _index.Remove(key);

        // Remove from type index
        if (_typeIndex.TryGetValue(entry.ResourceType, out var typeEntries))
        {
            typeEntries.Remove(entry);
            if (typeEntries.Count == 0)
            {
                _typeIndex.Remove(entry.ResourceType);
            }
        }

        // Remove from group index
        if (_groupIndex.TryGetValue(entry.ResourceGroup, out var groupEntries))
        {
            groupEntries.Remove(entry);
            if (groupEntries.Count == 0)
            {
                _groupIndex.Remove(entry.ResourceGroup);
            }
        }

        return true;
    }

    /// <summary>
    /// Update an existing resource index entry
    /// </summary>
    /// <param name="entry">Updated entry</param>
    /// <returns>True if updated, false if not found</returns>
    internal bool Update(IResourceIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!_index.ContainsKey(entry))
        {
            return false;
        }

        // Remove the old entry and add the new one
        Remove(entry);
        AddInternal(entry);
        return true;
    }

    /// <summary>
    /// Clear all entries from the index
    /// </summary>
    internal void Clear()
    {
        _index.Clear();
        _typeIndex.Clear();
        _groupIndex.Clear();
    }

    private void AddInternal(IResourceIndexEntry entry)
    {
        _index[entry] = entry;

        // Add to type index
        if (!_typeIndex.TryGetValue(entry.ResourceType, out var typeEntries))
        {
            typeEntries = new List<IResourceIndexEntry>();
            _typeIndex[entry.ResourceType] = typeEntries;
        }
        typeEntries.Add(entry);

        // Add to group index
        if (!_groupIndex.TryGetValue(entry.ResourceGroup, out var groupEntries))
        {
            groupEntries = new List<IResourceIndexEntry>();
            _groupIndex[entry.ResourceGroup] = groupEntries;
        }
        groupEntries.Add(entry);
    }
}
