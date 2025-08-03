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
/// Modern implementation of a Sims 4 package with async operations and performance optimizations
/// </summary>
public sealed class Package : IPackage
{
    private const int ApiVersion = 1;
    
    private PackageHeader _header;
    private PackageResourceIndex _index;
    private Stream? _packageStream;
    private bool _isDirty;
    private bool _disposed;
    
    /// <inheritdoc />
    public int RequestedApiVersion => ApiVersion;
    
    /// <inheritdoc />
    public int RecommendedApiVersion => ApiVersion;
    
    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields { get; } = new[]
    {
        "Magic", "Major", "Minor", "UserVersionMajor", "UserVersionMinor", 
        "CreatedDate", "ModifiedDate", "ResourceCount"
    };
    
    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => TypedValue.Create(Encoding.ASCII.GetString(_header.Magic)),
            1 => TypedValue.Create(Major),
            2 => TypedValue.Create(Minor),
            3 => TypedValue.Create(UserVersionMajor),
            4 => TypedValue.Create(UserVersionMinor),
            5 => TypedValue.Create(CreatedDate),
            6 => TypedValue.Create(ModifiedDate),
            7 => TypedValue.Create(ResourceCount),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set => throw new NotSupportedException("Package fields are read-only");
    }
    
    /// <inheritdoc />
    public ReadOnlySpan<byte> Magic => _header.Magic;
    
    /// <inheritdoc />
    public int Major => _header.Major;
    
    /// <inheritdoc />
    public int Minor => _header.Minor;
    
    /// <inheritdoc />
    public int UserVersionMajor => _header.UserVersionMajor;
    
    /// <inheritdoc />
    public int UserVersionMinor => _header.UserVersionMinor;
    
    /// <inheritdoc />
    public DateTime CreatedDate => _header.CreatedDate;
    
    /// <inheritdoc />
    public DateTime ModifiedDate => _header.ModifiedDate;
    
    /// <inheritdoc />
    public int ResourceCount => _index.Count;
    
    /// <inheritdoc />
    public IPackageResourceIndex ResourceIndex => _index;
    
    /// <inheritdoc />
    public bool IsDirty => _isDirty;
    
    /// <inheritdoc />
    public string? FileName { get; private set; }
    
    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            "Magic" => TypedValue.Create(Encoding.ASCII.GetString(_header.Magic)),
            "Major" => TypedValue.Create(Major),
            "Minor" => TypedValue.Create(Minor),
            "UserVersionMajor" => TypedValue.Create(UserVersionMajor),
            "UserVersionMinor" => TypedValue.Create(UserVersionMinor),
            "CreatedDate" => TypedValue.Create(CreatedDate),
            "ModifiedDate" => TypedValue.Create(ModifiedDate),
            "ResourceCount" => TypedValue.Create(ResourceCount),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Package fields are read-only");
    }
    
    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used
    public event EventHandler? ResourceIndexInvalidated;
#pragma warning restore CS0067
    
    /// <inheritdoc />
    public event EventHandler<ResourceEventArgs>? ResourceAdded;
    
    /// <inheritdoc />
    public event EventHandler<ResourceEventArgs>? ResourceRemoved;
    
    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<ResourceEventArgs>? ResourceModified;
#pragma warning restore CS0067
    
    /// <summary>
    /// Creates a new empty package
    /// </summary>
    public Package()
    {
        _header = PackageHeader.CreateDefault();
        _index = new PackageResourceIndex();
        _isDirty = true;
    }
    
    /// <summary>
    /// Creates a package from a stream
    /// </summary>
    /// <param name="stream">Stream containing package data</param>
    /// <param name="fileName">Optional filename for the package</param>
    public Package(Stream stream, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        _packageStream = stream;
        FileName = fileName;
        _index = new PackageResourceIndex(); // Initialize before calling LoadFromStream
        
        LoadFromStream(stream);
    }
    
    /// <inheritdoc />
    public async Task SavePackageAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (_packageStream == null)
        {
            throw new InvalidOperationException("Package has no stream to save to");
        }
        
        if (!_packageStream.CanWrite)
        {
            throw new InvalidOperationException("Package stream is read-only");
        }
        
        await SaveToStreamAsync(_packageStream, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task SaveAsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);
        
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream is not writable", nameof(stream));
        }
        
        await SaveToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
        await SaveToStreamAsync(fileStream, cancellationToken).ConfigureAwait(false);
        
        FileName = filePath;
    }
    
    /// <inheritdoc />
    public IResource? GetResource(IResourceKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        
        if (!_index.TryGetValue(key, out var entry))
        {
            return null;
        }
        
        return LoadResource(entry);
    }
    
    /// <inheritdoc />
    public async Task<IResource?> GetResourceAsync(IResourceKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        
        if (!_index.TryGetValue(key, out var entry))
        {
            return null;
        }
        
        return await LoadResourceAsync(entry, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public IResourceIndexEntry AddResource(IResourceKey key, ReadOnlySpan<byte> resource, bool compressed = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        
        // Remove existing resource if it exists
        RemoveResource(key);
        
        // Create new index entry
        var fileSize = (uint)resource.Length;
        var memorySize = fileSize;
        var compressionFlag = compressed ? (ushort)0xFFFF : (ushort)0x0000;
        
        var entry = new ResourceIndexEntry(key, fileSize, memorySize, 0, compressionFlag);
        
        // Add to index
        _index.Add(entry);
        _isDirty = true;
        
        // Raise event
        ResourceAdded?.Invoke(this, new ResourceEventArgs(key, entry));
        
        return entry;
    }
    
    /// <inheritdoc />
    public bool RemoveResource(IResourceKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        
        if (!_index.TryGetValue(key, out var entry))
        {
            return false;
        }
        
        var removed = _index.Remove(key);
        if (removed)
        {
            _isDirty = true;
            ResourceRemoved?.Invoke(this, new ResourceEventArgs(key, entry));
        }
        
        return removed;
    }
    
    /// <inheritdoc />
    public async Task CompactAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // For now, compacting just updates the header and index
        // In a full implementation, this would reorganize the physical layout
        _isDirty = true;
        
        await Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _packageStream?.Dispose();
            _disposed = true;
        }
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_packageStream != null)
            {
                await _packageStream.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Load package data from a file
    /// </summary>
    /// <param name="filePath">Path to the package file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New package instance</returns>
    public static async Task<Package> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        try
        {
            var package = new Package(fileStream, filePath);
            await Task.CompletedTask.ConfigureAwait(false); // Placeholder for any async initialization
            return package;
        }
        catch
        {
            await fileStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
    
    /// <summary>
    /// Load package data from a stream
    /// </summary>
    /// <param name="stream">Stream containing package data</param>
    /// <param name="fileName">Optional filename</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New package instance</returns>
    public static async Task<Package> LoadFromStreamAsync(Stream stream, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        var package = new Package(stream, fileName);
        
        await Task.CompletedTask; // Placeholder for any async initialization
        return package;
    }
    
    private void LoadFromStream(Stream stream)
    {
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        
        // Read header
        _header = PackageHeader.Read(reader);
        
        if (!_header.IsValid)
        {
            throw new InvalidDataException("Invalid package header - not a DBPF file");
        }
        
        // Read index
        LoadIndex(stream, _header);
        
        _isDirty = false;
    }
    
    private void LoadIndex(Stream stream, PackageHeader header)
    {
        if (header.IndexPosition == 0 || header.IndexSize == 0)
        {
            _index = new PackageResourceIndex();
            return;
        }
        
        stream.Position = header.IndexPosition;
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        
        var indexType = reader.ReadUInt32();
        var entries = new List<IResourceIndexEntry>();
        
        // Read index entries based on type
        for (int i = 0; i < header.ResourceCount; i++)
        {
            var entry = ResourceIndexEntry.Read(reader, indexType);
            entries.Add(entry);
        }
        
        _index = new PackageResourceIndex(indexType, entries);
    }
    
    private async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        
        // Update header with current counts and positions
        var indexPosition = PackageHeader.HeaderSize;
        var indexSize = CalculateIndexSize();
        
        var updatedHeader = new PackageHeader(
            _header.Magic,
            _header.Major,
            _header.Minor,
            _header.UserVersionMajor,
            _header.UserVersionMinor,
            _header.Unused1,
            _header.CreatedDateRaw,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // Update modified date
            _header.IndexMajor,
            _index.Count,
            indexPosition,
            indexSize,
            _header.Unused2,
            _header.Unused3,
            _header.IndexMinor,
            _header.HoleIndexPosition,
            _header.HoleIndexSize,
            _header.HoleCount,
            _header.Unused4,
            _header.Unused5,
            _header.Unused6);
        
        // Write header
        updatedHeader.Write(writer);
        
        // Write index
        await WriteIndexAsync(writer, cancellationToken).ConfigureAwait(false);
        
        // TODO: Write resource data
        
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        
        _header = updatedHeader;
        _isDirty = false;
    }
    
    private async Task WriteIndexAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        writer.Write(_index.IndexType);
        
        foreach (var entry in _index)
        {
            if (entry is ResourceIndexEntry resourceEntry)
            {
                resourceEntry.Write(writer);
            }
            
            // Check for cancellation periodically
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        await Task.CompletedTask;
    }
    
    private int CalculateIndexSize()
    {
        // 4 bytes for index type + entry size * count
        return 4 + (_index.Count * ResourceIndexEntry.EntrySize);
    }
    
    private IResource? LoadResource(IResourceIndexEntry entry)
    {
        // TODO: Implement resource loading
        throw new NotImplementedException("Resource loading not yet implemented");
    }
    
    private async Task<IResource?> LoadResourceAsync(IResourceIndexEntry entry, CancellationToken cancellationToken)
    {
        // TODO: Implement async resource loading
        await Task.CompletedTask;
        return LoadResource(entry);
    }
}
