using TS4Tools.Compression;
using TS4Tools.Resources;

namespace TS4Tools.Package;

/// <summary>
/// Implementation of a DBPF package file.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs
///
/// Header format (96 bytes):
/// - Offset 0: Magic "DBPF" (lines 593)
/// - Offset 4: Major version = 2 (lines 594)
/// - Offset 8: Minor version = 1 (lines 595)
/// - Offset 36: Index count (line 601)
/// - Offset 40: Index position low (line 604)
/// - Offset 44: Index size (line 602)
/// - Offset 60: Unused4 = 3 (lines 596, 603)
/// - Offset 64: Index position (line 604)
///
/// Reading flow: constructor → CheckHeader() → Index property (lines 547-636)
/// Saving flow: SavePackage() → SaveAs() (lines 54-174)
/// </remarks>
public sealed class DbpfPackage : IMutablePackage
{
    private readonly Stream? _stream;
    private readonly bool _leaveOpen;
    private readonly List<ResourceIndexEntry> _resources = [];
    private bool _disposed;

    // Header data
    private byte[] _header = new byte[PackageLimits.HeaderSize];

    public string? FilePath { get; }
    public bool IsDirty { get; private set; }
    public bool IsReadOnly { get; }

    /// <summary>Package major version (usually 2).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 268</remarks>
    public int MajorVersion => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(4));

    /// <summary>Package minor version (usually 1).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 273</remarks>
    public int MinorVersion => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(8));

    /// <summary>User version major (typically 0).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 278</remarks>
    public int UserVersionMajor => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x0C));

    /// <summary>User version minor (typically 0).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 283</remarks>
    public int UserVersionMinor => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x10));

    /// <summary>Creation timestamp (typically not set).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 293</remarks>
    public int CreationTime => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x18));

    /// <summary>Last update timestamp (typically not set).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 298</remarks>
    public int UpdatedTime => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x1C));

    /// <summary>Number of entries in the package index.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 308</remarks>
    public int HeaderIndexCount => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(36));

    /// <summary>Index size on disk in bytes.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 318</remarks>
    public int HeaderIndexSize => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(44));

    /// <summary>Index position in file.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 333</remarks>
    public int HeaderIndexPosition
    {
        get
        {
            int pos = BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(64));
            return pos != 0 ? pos : BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(40));
        }
    }

    private int IndexCount => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(36));
    private int IndexSize => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(44));
    private int IndexPosition
    {
        get
        {
            int pos = BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(64));
            return pos != 0 ? pos : BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(40));
        }
    }

    public int ResourceCount => _resources.Count;
    public IReadOnlyList<IResourceIndexEntry> Resources => _resources;

    public event EventHandler? ResourceIndexInvalidated;

    /// <summary>
    /// Raises the ResourceIndexInvalidated event.
    /// </summary>
    private void OnResourceIndexInvalidated()
    {
        ResourceIndexInvalidated?.Invoke(this, EventArgs.Empty);
    }

    private DbpfPackage(Stream? stream, string? filePath, bool isReadOnly, bool leaveOpen)
    {
        _stream = stream;
        FilePath = filePath;
        IsReadOnly = isReadOnly;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Creates a new empty package.
    /// </summary>
    public static DbpfPackage CreateNew()
    {
        var package = new DbpfPackage(null, null, false, false);
        package.InitializeHeader(2, 1);
        return package;
    }

    /// <summary>
    /// Opens a package from a file.
    /// </summary>
    public static async Task<DbpfPackage> OpenAsync(
        string path,
        bool readWrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        var fileAccess = readWrite ? FileAccess.ReadWrite : FileAccess.Read;
        var stream = new FileStream(path, FileMode.Open, fileAccess, FileShare.Read, 4096, FileOptions.Asynchronous);

        try
        {
            var package = new DbpfPackage(stream, path, !readWrite, leaveOpen: false);
            await package.LoadAsync(cancellationToken).ConfigureAwait(false);
            return package;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Opens a package from a stream.
    /// </summary>
    public static async Task<DbpfPackage> OpenAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var package = new DbpfPackage(stream, null, !stream.CanWrite, leaveOpen);
        await package.LoadAsync(cancellationToken).ConfigureAwait(false);
        return package;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_stream == null) return;

        _stream.Position = 0;

        // Read header
        int bytesRead = await _stream.ReadAsync(_header, cancellationToken).ConfigureAwait(false);
        if (bytesRead != PackageLimits.HeaderSize)
            throw new PackageFormatException("Unexpected end of file reading header.");

        ValidateHeader();

        // Read index
        await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        OnResourceIndexInvalidated();
    }

    private void ValidateHeader()
    {
        // Check magic
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(_header);
        if (magic != PackageLimits.Magic)
            throw new PackageFormatException(
                $"Invalid magic: expected 0x{PackageLimits.Magic:X8} (DBPF), got 0x{magic:X8}");

        // Check version
        if (MajorVersion != 2)
            throw new PackageFormatException($"Unsupported major version: {MajorVersion}. Expected 2.");

        if (MinorVersion != 1)
            throw new PackageFormatException($"Unsupported minor version: {MinorVersion}. Expected 1.");

        // Validate index count
        int indexCount = IndexCount;
        if (indexCount < 0 || indexCount > PackageLimits.MaxResourceCount)
            throw new PackageFormatException(
                $"Invalid index count: {indexCount}. Max allowed: {PackageLimits.MaxResourceCount}");
    }

    private async Task ReadIndexAsync(CancellationToken cancellationToken)
    {
        if (_stream == null) return;

        int indexPosition = IndexPosition;
        int indexSize = IndexSize;
        int indexCount = IndexCount;

        if (indexPosition == 0 || indexCount == 0)
            return;

        // Validate index position
        if (indexPosition < PackageLimits.HeaderSize || indexPosition > _stream.Length)
            throw new PackageFormatException($"Invalid index position: {indexPosition}");

        // Validate index size to prevent OOM from malformed files
        if (indexSize < 0 || indexSize > PackageLimits.MaxResourceSize)
            throw new PackageFormatException(
                $"Invalid index size: {indexSize}. Must be between 0 and {PackageLimits.MaxResourceSize:N0} bytes.");

        // Verify index doesn't extend beyond file
        long maxPossibleIndexSize = _stream.Length - indexPosition;
        if (indexSize > maxPossibleIndexSize)
            throw new PackageFormatException(
                $"Index size ({indexSize:N0}) exceeds remaining file length ({maxPossibleIndexSize:N0}).");

        // Read entire index into memory
        _stream.Position = indexPosition;
        var indexData = new byte[indexSize];
        int bytesRead = await _stream.ReadAsync(indexData, cancellationToken).ConfigureAwait(false);
        if (bytesRead != indexSize)
            throw new PackageFormatException("Unexpected end of file reading index.");

        // Parse index
        var span = indexData.AsSpan();
        int offset = 0;

        // Read index type
        uint indexType = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
        offset += 4;

        // Read header fields based on index type
        uint headerType = 0, headerGroup = 0, headerInstanceHigh = 0;

        if ((indexType & 0x01) != 0)
        {
            headerType = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
            offset += 4;
        }
        if ((indexType & 0x02) != 0)
        {
            headerGroup = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
            offset += 4;
        }
        if ((indexType & 0x04) != 0)
        {
            headerInstanceHigh = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
            offset += 4;
        }

        // Calculate entry size
        int entrySize = ResourceIndexEntrySerializer.GetEntrySize(indexType);

        // Read entries
        _resources.Clear();
        _resources.Capacity = indexCount;

        for (int i = 0; i < indexCount; i++)
        {
            if (offset + entrySize > span.Length)
                throw new PackageFormatException($"Unexpected end of index at entry {i}");

            var entry = ResourceIndexEntrySerializer.Read(
                span[offset..],
                indexType,
                headerType,
                headerGroup,
                headerInstanceHigh);

            _resources.Add(entry);
            offset += entrySize;
        }
    }

    private void InitializeHeader(int major, int minor)
    {
        Array.Clear(_header);

        // Magic: "DBPF"
        BinaryPrimitives.WriteUInt32LittleEndian(_header.AsSpan(0), PackageLimits.Magic);

        // Version
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(4), major);
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(8), minor);

        // Index position (starts right after header for new packages)
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(64), PackageLimits.HeaderSize);

        // Unused4 = 3 (historical)
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(60), 3);
    }

    public IResourceIndexEntry? Find(ResourceKey key)
    {
        return _resources.Find(e => !e.IsDeleted && e.Key == key);
    }

    public IResourceIndexEntry? Find(Func<IResourceIndexEntry, bool> predicate)
    {
        return _resources.Find(e => !e.IsDeleted && predicate(e));
    }

    public IEnumerable<IResourceIndexEntry> FindAll(Func<IResourceIndexEntry, bool> predicate)
    {
        return _resources.Where(e => !e.IsDeleted && predicate(e));
    }

    public async ValueTask<ReadOnlyMemory<byte>> GetResourceDataAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);

        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));

        // Return cached data if available
        if (rie.ResourceData.HasValue)
            return rie.ResourceData.Value;

        // Check for invalid offset (new resource not yet written)
        if (rie.ChunkOffset == 0xFFFFFFFF)
            return ReadOnlyMemory<byte>.Empty;

        // Special case: deleted marker
        if (rie.FileSize == 1 && rie.MemorySize == 0xFFFFFFFF)
            return ReadOnlyMemory<byte>.Empty;

        if (_stream == null)
            throw new InvalidOperationException("Package has no backing stream.");

        // Validate resource size to prevent OOM from malformed files
        if (rie.FileSize > PackageLimits.MaxResourceSize)
            throw new PackageFormatException(
                $"Resource file size ({rie.FileSize:N0}) exceeds maximum ({PackageLimits.MaxResourceSize:N0}).");

        if (rie.IsCompressed && rie.MemorySize > PackageLimits.MaxResourceSize)
            throw new PackageFormatException(
                $"Resource uncompressed size ({rie.MemorySize:N0}) exceeds maximum ({PackageLimits.MaxResourceSize:N0}).");

        // Verify resource doesn't extend beyond file
        if (rie.ChunkOffset + rie.FileSize > (ulong)_stream.Length)
            throw new PackageFormatException(
                $"Resource at 0x{rie.ChunkOffset:X8} extends beyond end of file.");

        // Read from stream
        _stream.Position = rie.ChunkOffset;

        byte[] data;
        if (!rie.IsCompressed)
        {
            // Uncompressed
            data = new byte[rie.FileSize];
            int bytesRead = await _stream.ReadAsync(data, cancellationToken).ConfigureAwait(false);
            if (bytesRead != rie.FileSize)
                throw new PackageFormatException($"Unexpected end of file reading resource at offset 0x{rie.ChunkOffset:X8}");
        }
        else
        {
            // Compressed - read and decompress
            var compressedData = new byte[rie.FileSize];
            int bytesRead = await _stream.ReadAsync(compressedData, cancellationToken).ConfigureAwait(false);
            if (bytesRead != rie.FileSize)
                throw new PackageFormatException($"Unexpected end of file reading compressed resource at offset 0x{rie.ChunkOffset:X8}");

            data = Decompressor.Decompress(compressedData, (int)rie.MemorySize);
        }

        // Cache and return
        rie.ResourceData = data;
        return data;
    }

    public async ValueTask<IResource> GetResourceAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default)
    {
        var data = await GetResourceDataAsync(entry, cancellationToken).ConfigureAwait(false);
        return new DefaultResource(entry.Key, data);
    }

    public IResourceIndexEntry? AddResource(ResourceKey key, ReadOnlyMemory<byte> data, bool rejectDuplicates = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");

        if (rejectDuplicates && Find(key) != null)
            return null;

        var entry = new ResourceIndexEntry(key)
        {
            MemorySize = (uint)data.Length,
            ResourceData = data.ToArray(),
            IsDirty = true
        };

        _resources.Add(entry);
        IsDirty = true;
        return entry;
    }

    public void ReplaceResource(IResourceIndexEntry entry, ReadOnlyMemory<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");

        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));

        rie.MemorySize = (uint)data.Length;
        rie.ResourceData = data.ToArray();
        rie.IsDirty = true;
        IsDirty = true;
    }

    public void DeleteResource(IResourceIndexEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");

        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));

        if (!rie.IsDeleted)
        {
            rie.IsDeleted = true;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Saves the package to its original file using atomic temp file pattern.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 54-88
    ///
    /// Safety measures:
    /// - Save to temp file first (line 64, 67)
    /// - Lock header bytes on original stream (line 69)
    /// - Copy temp contents to original (lines 71-74)
    /// - Truncate stream to new length (line 75)
    /// - Delete temp file and unlock (line 79)
    /// </remarks>
    public async ValueTask SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("Package was not opened from a file. Use SaveAsAsync instead.");

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream == null)
            throw new InvalidOperationException("Package has no stream to save to.");

        if (!_stream.CanWrite)
            throw new InvalidOperationException("Package is read-only.");

        // Use atomic save pattern: write to temp file first, then copy back
        // Source: s4pi/Package/Package.cs lines 64-79
        string tmpFile = Path.GetTempFileName();
        FileStream? fs = _stream as FileStream;
        bool locked = false;

        try
        {
            // Write to temp file first
            await using (var tempStream = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await SaveToStreamAsync(tempStream, cancellationToken).ConfigureAwait(false);
            }

            // Lock the header on original stream to prevent concurrent modification
            // Source: s4pi/Package/Package.cs line 69
            // Note: File locking is not supported on macOS, so we only lock on Windows/Linux
            if (fs != null && OperatingSystem.IsWindows())
            {
                fs.Lock(0, PackageLimits.HeaderSize);
                locked = true;
            }

            // Copy temp file contents back to original stream
            // Source: s4pi/Package/Package.cs lines 71-74
            _stream.Position = 0;
            await using (var tempReadStream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await tempReadStream.CopyToAsync(_stream, cancellationToken).ConfigureAwait(false);
            }

            // Truncate to new length to prevent trailing garbage
            // Source: s4pi/Package/Package.cs line 75
            _stream.SetLength(_stream.Position);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Re-read header to sync internal state
            // Source: s4pi/Package/Package.cs lines 81-82
            _stream.Position = 0;
            int bytesRead = await _stream.ReadAsync(_header, cancellationToken).ConfigureAwait(false);
            if (bytesRead != PackageLimits.HeaderSize)
                throw new PackageFormatException("Failed to re-read header after save.");

            IsDirty = false;
            OnResourceIndexInvalidated();
        }
        finally
        {
            // Always unlock and delete temp file
            // Source: s4pi/Package/Package.cs line 79
            if (locked && fs != null && OperatingSystem.IsWindows())
            {
                fs.Unlock(0, PackageLimits.HeaderSize);
            }

            try { File.Delete(tmpFile); } catch { /* Ignore cleanup errors */ }
        }
    }

    /// <summary>
    /// Saves the package to a new file path.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 155-160
    /// </remarks>
    public async ValueTask SaveAsAsync(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(path);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await SaveToStreamAsync(fs, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new PackageWriter(this, _resources, _stream);
        await writer.WriteAsync(stream, cancellationToken).ConfigureAwait(false);
        IsDirty = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_stream != null && !_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_stream != null && !_leaveOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal byte[] Header => _header;
}
