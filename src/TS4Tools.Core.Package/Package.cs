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

using TS4Tools.Core.Package.Compression;

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
    private readonly ICompressionService _compressionService;
    private readonly Dictionary<IResourceKey, byte[]> _resourceData = new();

    /// <summary>
    /// Gets whether this package is in read-only mode.
    /// In read-only mode, modification operations will throw exceptions.
    /// </summary>
    public bool IsReadOnly { get; private init; }

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
    /// <param name="compressionService">The compression service to use for resource compression/decompression</param>
    /// <param name="readOnly">Whether the package should be opened in read-only mode</param>
    public Package(ICompressionService compressionService, bool readOnly = false)
    {
        _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        IsReadOnly = readOnly;
        _header = PackageHeader.CreateDefault();
        _index = new PackageResourceIndex();
        _isDirty = !readOnly; // Don't mark as dirty if read-only
    }

    /// <summary>
    /// Creates a package from a stream
    /// </summary>
    /// <param name="stream">Stream containing package data</param>
    /// <param name="compressionService">The compression service to use for resource compression/decompression</param>
    /// <param name="readOnly">Whether the package should be opened in read-only mode</param>
    /// <param name="fileName">Optional filename for the package</param>
    public Package(Stream stream, ICompressionService compressionService, bool readOnly = false, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        IsReadOnly = readOnly;

        _packageStream = stream;
        FileName = fileName;
        _index = new PackageResourceIndex(); // Initialize before calling LoadFromStream

        LoadFromStream(stream);
    }

    /// <inheritdoc />
    public async Task SavePackageAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot save a read-only package");
        }

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

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot save a read-only package");
        }

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

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot save a read-only package");
        }

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
    public async Task<Stream?> GetResourceStreamAsync(IResourceIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);

        // Read raw resource data from the package file
        var resourceKey = entry as IResourceKey;
        if (resourceKey == null)
        {
            return null;
        }

        var indexEntry = entry as ResourceIndexEntry;
        if (indexEntry == null)
        {
            return null;
        }

        // Read the raw data from the file at the specified position
        var buffer = new byte[indexEntry.FileSize];

        _packageStream!.Seek(indexEntry.ChunkOffset, SeekOrigin.Begin);
        await _packageStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

        // If the resource is compressed, decompress it
        if (indexEntry.IsCompressed)
        {
            var decompressedData = await _compressionService.DecompressAsync(buffer, (int)indexEntry.MemorySize, cancellationToken).ConfigureAwait(false);
            return new MemoryStream(decompressedData);
        }

        return new MemoryStream(buffer);
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

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot add resources to a read-only package");
        }

        // Remove existing resource if it exists
        RemoveResource(key);

        // Compress the resource if requested
        byte[] resourceData;
        uint fileSize;
        uint memorySize = (uint)resource.Length;

        if (compressed && resource.Length > 0)
        {
            resourceData = _compressionService.Compress(resource);
            fileSize = (uint)resourceData.Length;
        }
        else
        {
            resourceData = resource.ToArray();
            fileSize = (uint)resourceData.Length;
        }

        var compressionFlag = compressed && fileSize != memorySize ? (ushort)0xFFFF : (ushort)0x0000;

        var entry = new ResourceIndexEntry(key, fileSize, memorySize, 0, compressionFlag);

        // Store the resource data for later writing
        _resourceData[key] = resourceData;

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

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot remove resources from a read-only package");
        }

        if (!_index.TryGetValue(key, out var entry))
        {
            return false;
        }

        var removed = _index.Remove(key);
        if (removed)
        {
            // Also remove from resource data storage
            _resourceData.Remove(key);
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
    /// <param name="compressionService">The compression service to use</param>
    /// <param name="readOnly">Whether to open the package in read-only mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New package instance</returns>
    public static async Task<Package> LoadFromFileAsync(string filePath, ICompressionService compressionService, bool readOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(compressionService);

        var fileAccess = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
        var fileStream = new FileStream(filePath, FileMode.Open, fileAccess, FileShare.Read, 4096, FileOptions.Asynchronous);

        Package? package = null;
        try
        {
            package = new Package(fileStream, compressionService, readOnly, filePath);
            await Task.CompletedTask.ConfigureAwait(false); // Placeholder for any async initialization

            // Transfer ownership of fileStream to package - don't dispose here
            return package;
        }
        catch
        {
            // If package creation failed, dispose the stream
            await fileStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Load package data from a stream
    /// </summary>
    /// <param name="stream">Stream containing package data</param>
    /// <param name="compressionService">The compression service to use</param>
    /// <param name="readOnly">Whether to open the package in read-only mode</param>
    /// <param name="fileName">Optional filename</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New package instance</returns>
    public static async Task<Package> LoadFromStreamAsync(Stream stream, ICompressionService compressionService, bool readOnly = false, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(compressionService);

        var package = new Package(stream, compressionService, readOnly, fileName);

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
        if (header.IndexSize == 0 || header.ResourceCount == 0)
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

        // Write resource data
        await WriteResourceDataAsync(writer, cancellationToken).ConfigureAwait(false);

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

    private async Task WriteResourceDataAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        // Calculate the starting position for resource data (after header and index)
        var resourceDataStart = PackageHeader.HeaderSize + CalculateIndexSize();
        var currentPosition = resourceDataStart;

        // Update chunk offsets in the index entries and write resource data
        foreach (var entry in _index)
        {
            if (entry is ResourceIndexEntry resourceEntry)
            {
                // Update the chunk offset for this resource
                resourceEntry.ChunkOffset = (uint)currentPosition;

                // Write the resource data if we have it
                if (_resourceData.TryGetValue(entry, out var data))
                {
                    await writer.BaseStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                    currentPosition += data.Length;
                }
                else if (_packageStream != null)
                {
                    // If we don't have new data, copy from the original package stream
                    var originalData = new byte[resourceEntry.FileSize];
                    var originalPosition = _packageStream.Position;

                    _packageStream.Seek(resourceEntry.ChunkOffset, SeekOrigin.Begin);
                    await _packageStream.ReadExactlyAsync(originalData, cancellationToken).ConfigureAwait(false);

                    await writer.BaseStream.WriteAsync(originalData, cancellationToken).ConfigureAwait(false);
                    currentPosition += originalData.Length;

                    // Restore original position
                    _packageStream.Position = originalPosition;
                }
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
        // Package.GetResource is deprecated in favor of ResourceManager.LoadResourceAsync
        // This maintains interface compatibility while guiding users to the correct API
        throw new NotSupportedException(
            "Direct resource loading from Package is not supported. " +
            "Use IResourceManager.LoadResourceAsync(package, entry, apiVersion) instead. " +
            "This approach provides proper dependency injection, caching, and factory patterns.");
    }

    private async Task<IResource?> LoadResourceAsync(IResourceIndexEntry entry, CancellationToken cancellationToken)
    {
        // Package.GetResourceAsync is deprecated in favor of ResourceManager.LoadResourceAsync
        // This maintains interface compatibility while guiding users to the correct API
        await Task.CompletedTask;
        throw new NotSupportedException(
            "Direct resource loading from Package is not supported. " +
            "Use IResourceManager.LoadResourceAsync(package, entry, apiVersion) instead. " +
            "This approach provides proper dependency injection, caching, and factory patterns.");
    }
}
