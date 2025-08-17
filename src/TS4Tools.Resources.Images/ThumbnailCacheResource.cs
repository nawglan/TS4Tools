/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Publi        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load thumbnail cache from stream: {ex.Message}", ex);
        }

        return Task.CompletedTask;ense as published by   *
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

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Represents a thumbnail cache resource that manages thumbnail storage and retrieval for improved UI performance.
/// This resource caches pre-generated thumbnails to avoid regenerating them on each access.
/// </summary>
public sealed class ThumbnailCacheResource : IResource, IDisposable, INotifyPropertyChanged
{
    private readonly ResourceKey _key;
    private readonly List<string> _contentFields = new()
    {
        "ResourceId",
        "CacheVersion",
        "MaxCacheSize",
        "ThumbnailCount",
        "TotalCacheSize"
    };
    private readonly ConcurrentDictionary<ulong, ThumbnailEntry> _thumbnailCache = new();
    private bool _isDirty = true;
    private bool _disposed;
    private MemoryStream? _stream;
    private uint _cacheVersion = 1;
    private int _maxCacheSize = 10000; // Maximum number of thumbnails to cache
    private long _totalCacheSize = 0; // Total size in bytes
    private long _cacheHitCount = 0;
    private long _cacheMissCount = 0;
    private long _totalRequests = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailCacheResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    public ThumbnailCacheResource(ResourceKey key, uint version)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        _stream = new MemoryStream();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public ResourceKey Key => _key;

    /// <inheritdoc/>
    public uint Version { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource has been modified.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the current cache version.
    /// </summary>
    public uint CacheVersion => _cacheVersion;

    /// <summary>
    /// Gets the number of thumbnails currently in the cache.
    /// </summary>
    public int CacheCount => _thumbnailCache.Count;

    /// <summary>
    /// Gets the total cache size in bytes.
    /// </summary>
    public long TotalCacheSize => _totalCacheSize;

    /// <summary>
    /// Gets or sets the maximum number of thumbnails to cache.
    /// </summary>
    public int MaxCacheSize
    {
        get => _maxCacheSize;
        set
        {
            if (_maxCacheSize != value && value > 0)
            {
                _maxCacheSize = value;
                IsDirty = true;
                OnPropertyChanged();

                // Enforce the new limit
                EnforceMaxCacheSize();
            }
        }
    }

    /// <summary>
    /// Gets the total number of cache hits.
    /// </summary>
    public long CacheHitCount => _cacheHitCount;

    /// <summary>
    /// Gets the total number of cache misses.
    /// </summary>
    public long CacheMissCount => _cacheMissCount;

    /// <summary>
    /// Gets the total number of requests.
    /// </summary>
    public long TotalRequests => _totalRequests;

    /// <summary>
    /// Gets the total memory usage in bytes (same as TotalCacheSize).
    /// </summary>
    public long TotalMemoryUsage => _totalCacheSize;

    /// <summary>
    /// Gets statistics about the thumbnail cache.
    /// </summary>
    public ThumbnailCacheStatistics Statistics
    {
        get
        {
            var thumbnails = _thumbnailCache.Values.ToArray();
            var totalHits = thumbnails.Sum(t => t.AccessCount);
            var avgSize = thumbnails.Length > 0 ? thumbnails.Average(t => t.DataSize) : 0;

            return new ThumbnailCacheStatistics(
                CacheCount,
                TotalCacheSize,
                totalHits,
                avgSize,
                DateTime.UtcNow);
        }
    }

    #region IResource Implementation

    /// <inheritdoc/>
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stream ??= new MemoryStream();
        }
    }

    /// <inheritdoc/>
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc/>
    public event EventHandler? ResourceChanged;

    /// <inheritdoc/>
    public int RequestedApiVersion => 1;

    /// <inheritdoc/>
    public int RecommendedApiVersion => 1;

    /// <inheritdoc/>
    public IReadOnlyList<string> ContentFields => _contentFields;

    /// <inheritdoc/>
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc/>
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Writes the resource content to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    private void WriteToStream(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        // Write magic bytes for thumbnail cache
        writer.Write(Encoding.UTF8.GetBytes("THMC"));

        // Write version
        writer.Write(1); // version

        // Write cache statistics
        writer.Write(CacheHitCount);
        writer.Write(CacheMissCount);
        writer.Write(TotalRequests);
        writer.Write(TotalMemoryUsage);

        // Write cache entries count
        writer.Write(_thumbnailCache.Count);

        // Write cache entries
        foreach (var kvp in _thumbnailCache)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value.Data.Length);
            writer.Write(kvp.Value.Data);
            writer.Write(kvp.Value.Width);
            writer.Write(kvp.Value.Height);
            writer.Write((int)kvp.Value.Format);
            writer.Write(kvp.Value.AccessCount);
            writer.Write(kvp.Value.LastAccessTime.ToBinary());
            writer.Write(kvp.Value.CreateTime.ToBinary());
        }
    }

    /// <summary>
    /// Gets the value of a field by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    private TypedValue GetFieldValue(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return index switch
        {
            0 => new TypedValue(typeof(ulong), Key.Instance), // ResourceId
            1 => new TypedValue(typeof(uint), CacheVersion), // CacheVersion
            2 => new TypedValue(typeof(int), MaxCacheSize), // MaxCacheSize
            3 => new TypedValue(typeof(int), CacheCount), // ThumbnailCount
            4 => new TypedValue(typeof(long), TotalCacheSize), // TotalCacheSize
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    /// <summary>
    /// Sets the value of a field by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <param name="value">The new value.</param>
    private void SetFieldValue(int index, TypedValue value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        switch (index)
        {
            case 0:
                // ResourceId is read-only
                break;
            case 1:
                // CacheVersion is read-only
                break;
            case 2:
                MaxCacheSize = (int)(value.Value ?? 0);
                break;
            case 3:
                // ThumbnailCount is read-only
                break;
            case 4:
                // TotalCacheSize is read-only
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the value of a field by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    private TypedValue GetFieldValue(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _contentFields.IndexOf(name);
        if (index == -1)
            throw new ArgumentException($"Unknown field: {name}", nameof(name));

        return GetFieldValue(index);
    }

    /// <summary>
    /// Sets the value of a field by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The new value.</param>
    private void SetFieldValue(string name, TypedValue value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _contentFields.IndexOf(name);
        if (index == -1)
            throw new ArgumentException($"Unknown field: {name}", nameof(name));

        SetFieldValue(index, value);
    }

    #endregion

    /// <summary>
    /// Adds or updates a thumbnail in the cache.
    /// </summary>
    /// <param name="resourceId">The resource ID for the thumbnail.</param>
    /// <param name="thumbnailData">The thumbnail image data.</param>
    /// <param name="width">The thumbnail width.</param>
    /// <param name="height">The thumbnail height.</param>
    /// <param name="format">The image format.</param>
    public void AddThumbnail(ulong resourceId, byte[] thumbnailData, int width, int height, ThumbnailFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(thumbnailData);

        if (thumbnailData.Length == 0)
        {
            throw new ArgumentException("Thumbnail data cannot be empty.", nameof(thumbnailData));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        var entry = new ThumbnailEntry(
            resourceId,
            thumbnailData,
            width,
            height,
            format,
            DateTime.UtcNow,
            DateTime.UtcNow,
            0);

        // Remove existing entry if present
        if (_thumbnailCache.TryRemove(resourceId, out var existingEntry))
        {
            Interlocked.Add(ref _totalCacheSize, -existingEntry.DataSize);
        }

        // Add new entry
        _thumbnailCache[resourceId] = entry;
        Interlocked.Add(ref _totalCacheSize, entry.DataSize);

        // Enforce cache size limits
        EnforceMaxCacheSize();

        IsDirty = true;
        OnPropertyChanged(nameof(CacheCount));
        OnPropertyChanged(nameof(TotalCacheSize));
    }

    /// <summary>
    /// Retrieves a thumbnail from the cache.
    /// </summary>
    /// <param name="resourceId">The resource ID to retrieve.</param>
    /// <returns>The thumbnail entry, or null if not found.</returns>
    public ThumbnailEntry? GetThumbnail(ulong resourceId)
    {
        Interlocked.Increment(ref _totalRequests);

        if (_thumbnailCache.TryGetValue(resourceId, out var entry))
        {
            Interlocked.Increment(ref _cacheHitCount);

            // Update access statistics
            var updatedEntry = entry with
            {
                LastAccessTime = DateTime.UtcNow,
                AccessCount = entry.AccessCount + 1
            };

            _thumbnailCache[resourceId] = updatedEntry;
            return updatedEntry;
        }

        Interlocked.Increment(ref _cacheMissCount);
        return null;
    }

    /// <summary>
    /// Removes a thumbnail from the cache.
    /// </summary>
    /// <param name="resourceId">The resource ID to remove.</param>
    /// <returns>True if the thumbnail was removed; otherwise, false.</returns>
    public bool RemoveThumbnail(ulong resourceId)
    {
        if (_thumbnailCache.TryRemove(resourceId, out var entry))
        {
            Interlocked.Add(ref _totalCacheSize, -entry.DataSize);
            IsDirty = true;
            OnPropertyChanged(nameof(CacheCount));
            OnPropertyChanged(nameof(TotalCacheSize));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a thumbnail exists in the cache.
    /// </summary>
    /// <param name="resourceId">The resource ID to check.</param>
    /// <returns>True if the thumbnail exists; otherwise, false.</returns>
    public bool ContainsThumbnail(ulong resourceId)
    {
        return _thumbnailCache.ContainsKey(resourceId);
    }

    /// <summary>
    /// Clears all thumbnails from the cache.
    /// </summary>
    public void ClearCache()
    {
        _thumbnailCache.Clear();
        Interlocked.Exchange(ref _totalCacheSize, 0);
        Interlocked.Exchange(ref _cacheHitCount, 0);
        Interlocked.Exchange(ref _cacheMissCount, 0);
        Interlocked.Exchange(ref _totalRequests, 0);
        IsDirty = true;
        OnPropertyChanged(nameof(CacheCount));
        OnPropertyChanged(nameof(TotalCacheSize));
    }

    /// <summary>
    /// Removes old or least recently used thumbnails to enforce cache size limits.
    /// </summary>
    private void EnforceMaxCacheSize()
    {
        if (_thumbnailCache.Count <= _maxCacheSize) return;

        var entriesToRemove = _thumbnailCache.Values
            .OrderBy(e => e.LastAccessTime)
            .Take(_thumbnailCache.Count - _maxCacheSize)
            .ToArray();

        foreach (var entry in entriesToRemove)
        {
            RemoveThumbnail(entry.ResourceId);
        }
    }

    /// <summary>
    /// Loads the thumbnail cache resource from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Read cache header
            _cacheVersion = reader.ReadUInt32();
            _maxCacheSize = reader.ReadInt32();

            // Read thumbnail entries
            var entryCount = reader.ReadInt32();
            _thumbnailCache.Clear();
            Interlocked.Exchange(ref _totalCacheSize, 0);

            for (int i = 0; i < entryCount; i++)
            {
                var resourceId = reader.ReadUInt64();
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                var format = (ThumbnailFormat)reader.ReadInt32();
                var createTime = DateTime.FromBinary(reader.ReadInt64());
                var accessTime = DateTime.FromBinary(reader.ReadInt64());
                var accessCount = reader.ReadUInt32();

                var dataLength = reader.ReadInt32();
                var data = reader.ReadBytes(dataLength);

                var entry = new ThumbnailEntry(resourceId, data, width, height, format, createTime, accessTime, accessCount);
                _thumbnailCache[resourceId] = entry;
                Interlocked.Add(ref _totalCacheSize, entry.DataSize);
            }

            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load thumbnail cache resource: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the thumbnail cache resource to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Write cache header
            writer.Write(_cacheVersion);
            writer.Write(_maxCacheSize);

            // Write thumbnail entries
            var entries = _thumbnailCache.Values.ToArray();
            writer.Write(entries.Length);

            foreach (var entry in entries)
            {
                writer.Write(entry.ResourceId);
                writer.Write(entry.Width);
                writer.Write(entry.Height);
                writer.Write((int)entry.Format);
                writer.Write(entry.CreateTime.ToBinary());
                writer.Write(entry.LastAccessTime.ToBinary());
                writer.Write(entry.AccessCount);
                writer.Write(entry.Data.Length);
                writer.Write(entry.Data);
            }

            writer.BaseStream.Flush();
            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to save thumbnail cache resource: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the resource as a stream.
    /// </summary>
    /// <returns>A stream containing the resource data.</returns>
    public async Task<Stream> AsStreamAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var memoryStream = new MemoryStream();
        await SaveToStreamAsync(memoryStream).ConfigureAwait(false);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a thumbnail entry in the cache.
/// </summary>
/// <param name="ResourceId">The resource ID this thumbnail belongs to.</param>
/// <param name="Data">The thumbnail image data.</param>
/// <param name="Width">The thumbnail width in pixels.</param>
/// <param name="Height">The thumbnail height in pixels.</param>
/// <param name="Format">The image format.</param>
/// <param name="CreateTime">When the thumbnail was created.</param>
/// <param name="LastAccessTime">When the thumbnail was last accessed.</param>
/// <param name="AccessCount">Number of times the thumbnail was accessed.</param>
public readonly record struct ThumbnailEntry(
    ulong ResourceId,
    byte[] Data,
    int Width,
    int Height,
    ThumbnailFormat Format,
    DateTime CreateTime,
    DateTime LastAccessTime,
    uint AccessCount)
{
    /// <summary>
    /// Gets the size of the thumbnail data in bytes.
    /// </summary>
    public int DataSize => Data.Length;
}

/// <summary>
/// Represents thumbnail cache statistics.
/// </summary>
/// <param name="TotalThumbnails">The total number of thumbnails in cache.</param>
/// <param name="TotalSize">The total cache size in bytes.</param>
/// <param name="TotalHits">The total number of cache hits.</param>
/// <param name="AverageThumbnailSize">The average thumbnail size.</param>
/// <param name="LastUpdated">When the statistics were last updated.</param>
public readonly record struct ThumbnailCacheStatistics(
    int TotalThumbnails,
    long TotalSize,
    long TotalHits,
    double AverageThumbnailSize,
    DateTime LastUpdated);

/// <summary>
/// Defines the supported thumbnail formats.
/// </summary>
public enum ThumbnailFormat
{
    /// <summary>JPEG format.</summary>
    Jpeg = 0,
    /// <summary>PNG format.</summary>
    Png = 1,
    /// <summary>DDS format.</summary>
    Dds = 2,
    /// <summary>TGA format.</summary>
    Tga = 3
}
