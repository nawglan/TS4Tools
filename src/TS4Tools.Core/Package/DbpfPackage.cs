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
/// - Offset 68: Unused5 (28 bytes, line 338)
///
/// Wrapper resolution: <see cref="GetResourceAsync(IResourceIndexEntry, bool, CancellationToken)"/> routes through
/// <see cref="ResourceHandlers"/> (default <see cref="ResourceHandlerRegistry.Shared"/>), mirroring
/// legacy WrapperDealer.GetResource (WrapperDealer.cs lines 53-67).
///
/// Thread safety: all reads of the shared backing stream are serialised with a semaphore, so concurrent
/// <see cref="GetResourceDataAsync"/> calls do not race on <see cref="Stream.Position"/>. Index mutation
/// (add/replace/delete/sort) is not thread-safe, as in legacy.
///
/// Reading flow: constructor → CheckHeader() → Index property (lines 547-636)
/// Saving flow: SavePackage() → SaveAs() (lines 54-174)
/// </remarks>
public sealed class DbpfPackage : IMutablePackage
{
    private readonly Stream? _stream;
    private readonly bool _leaveOpen;
    private readonly List<ResourceIndexEntry> _resources = [];
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private IResourceHandlerRegistry? _resourceHandlers;
    private uint _indexType;
    private bool _disposed;

    // Header data
    private byte[] _header = new byte[PackageLimits.HeaderSize];

    /// <summary>The file path this package was loaded from, if any.</summary>
    public string? FilePath { get; }
    /// <summary>Whether the package has unsaved changes.</summary>
    public bool IsDirty { get; private set; }
    /// <summary>Whether the package is read-only.</summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Gets or sets the registry used to resolve resource wrappers in <see cref="GetResourceAsync(IResourceIndexEntry, CancellationToken)"/>.
    /// Defaults to <see cref="ResourceHandlerRegistry.Shared"/>.
    /// </summary>
    [AllowNull]
    public IResourceHandlerRegistry ResourceHandlers
    {
        get => _resourceHandlers ?? ResourceHandlerRegistry.Shared;
        set => _resourceHandlers = value;
    }

    /// <summary>Package header: "DBPF" magic bytes (offset 0).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 263</remarks>
    public ReadOnlyMemory<byte> Magic => _header.AsSpan(0, 4).ToArray();

    /// <summary>Package header: unused (offset 0x14).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 288</remarks>
    public int Unused1 => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x14));

    /// <summary>Package header: unused (offset 0x20).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 303</remarks>
    public int Unused2 => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(0x20));

    /// <summary>Package header: index record position low (offset 40; 0 when the position is at offset 64).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 313</remarks>
    public int IndexRecordPositionLow => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(40));

    /// <summary>Package header: unused (first int of the 12 bytes at offset 48).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 323</remarks>
    public int Unused3 => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(48));

    /// <summary>Package header: offset 60, always 3.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 328</remarks>
    public int Unused4 => BinaryPrimitives.ReadInt32LittleEndian(_header.AsSpan(60));

    /// <summary>Package header: unused 28 bytes at offset 68.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 338</remarks>
    public ReadOnlyMemory<byte> Unused5 => _header.AsSpan(68, 28).ToArray();

    /// <summary>Package index: the index type flags read from the index (0 for a new package).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 352; PackageIndex.cs lines 35-36, 57</remarks>
    public uint IndexType => _indexType;

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

    /// <summary>The number of resources in the package.</summary>
    public int ResourceCount => _resources.Count;
    /// <summary>The resource index entries in the package.</summary>
    public IReadOnlyList<IResourceIndexEntry> Resources => _resources;

    /// <summary>Raised when the resource index is invalidated.</summary>
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
    /// <param name="major">DBPF major version; legacy only supports 2.</param>
    /// <exception cref="PackageFormatException"><paramref name="major"/> is not a supported version.</exception>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 169-183, 527-545, 594</remarks>
    public static DbpfPackage CreateNew(int major = 2)
    {
        if (!SupportedMajors.Contains(major))
            throw new PackageFormatException(
                $"Expected major version(s) '{string.Join(", ", SupportedMajors)}'.  Found '{major}'.");

        var package = new DbpfPackage(null, null, false, false);
        package.InitializeHeader(major, 1);
        return package;
    }

    // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 594
    private static readonly int[] SupportedMajors = [2];

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
        // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 239 (FileShare.ReadWrite)
        var stream = new FileStream(path, FileMode.Open, fileAccess, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);

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
        await ReadExactlyAsync(_stream, _header, "header", cancellationToken).ConfigureAwait(false);

        ValidateHeader();

        // Read index
        await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        OnResourceIndexInvalidated();
    }

    /// <summary>
    /// Reads exactly <paramref name="buffer"/>.Length bytes, translating a short stream into a format error.
    /// </summary>
    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, string what, CancellationToken cancellationToken)
    {
        try
        {
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (EndOfStreamException ex)
        {
            throw new PackageFormatException($"Unexpected end of file reading {what}.", ex);
        }
    }

    private void ValidateHeader()
    {
        // Magic/version checks are legacy CheckHeader, which only runs when Settings.Checking is on.
        // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 83, 553, 607-620
        if (LibrarySettings.Checking)
        {
            uint magic = BinaryPrimitives.ReadUInt32LittleEndian(_header);
            if (magic != PackageLimits.Magic)
                throw new PackageFormatException(
                    $"Invalid magic: expected 0x{PackageLimits.Magic:X8} (DBPF), got 0x{magic:X8}");

            if (!SupportedMajors.Contains(MajorVersion))
                throw new PackageFormatException($"Unsupported major version: {MajorVersion}. Expected 2.");

            if (MinorVersion != 1)
                throw new PackageFormatException($"Unsupported minor version: {MinorVersion}. Expected 1.");
        }

        // Security bounds are always enforced.

        // Validate index count
        int indexCount = IndexCount;
        if (indexCount < 0 || indexCount > PackageLimits.MaxResourceCount)
            throw new PackageFormatException(
                $"Invalid index count: {indexCount}. Max allowed: {PackageLimits.MaxResourceCount}");
    }

    private async Task ReadIndexAsync(CancellationToken cancellationToken)
    {
        if (_stream == null) return;

        // Always start from an empty index: this is also used to re-sync after an
        // in-place save, where an empty package must not keep stale entries.
        _resources.Clear();
        _indexType = 0;

        int indexPosition = IndexPosition;
        int indexSize = IndexSize;
        int indexCount = IndexCount;

        // Source: legacy_references/Sims4Tools/s4pi/Package/PackageIndex.cs lines 52-53
        if (indexPosition == 0)
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

        // An index with no entries may legitimately be empty on disk.
        if (indexSize < 4)
        {
            if (indexCount == 0) return;
            throw new PackageFormatException($"Invalid index size: {indexSize} for {indexCount} entries.");
        }

        // Read entire index into memory
        _stream.Position = indexPosition;
        var indexData = new byte[indexSize];
        await ReadExactlyAsync(_stream, indexData, "index", cancellationToken).ConfigureAwait(false);

        // Parse index
        var span = indexData.AsSpan();
        int offset = 0;

        // Read index type
        // Source: legacy_references/Sims4Tools/s4pi/Package/PackageIndex.cs line 57
        uint indexType = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
        offset += 4;
        _indexType = indexType;

        int headerBytes = (ResourceIndexEntrySerializer.GetHeaderFieldCount(indexType) - 1) * 4;
        if (offset + headerBytes > span.Length)
            throw new PackageFormatException("Unexpected end of index reading index header.");

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

            entry.ElementChanged = OnEntryChanged;
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

        // Index size of an empty index: legacy setIndexsize(bw, new PackageIndex().Size).
        // PackageIndex.Size = (Count * (numFields - Hdrsize) + Hdrsize) * 4 = (0 + 1) * 4 = 4
        // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 539; PackageIndex.cs lines 38-46, 89
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(44), EmptyIndexSize);

        // Index version / Unused4 = 3 (Package.cs lines 540, 542, 603, 605)
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(60), 3);

        // Index position: 40 = 0, 64 = header size (Package.cs lines 541, 604)
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(40), 0);
        BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(64), PackageLimits.HeaderSize);
    }

    /// <summary>Size in bytes of an empty package index (index type only).</summary>
    internal const int EmptyIndexSize = 4;

    /// <summary>
    /// Marks the package dirty when an entry's Key/IsDeleted/CompressionType changes.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs (OnElementChanged in every setter)</remarks>
    private void OnEntryChanged(ResourceIndexEntry entry) => IsDirty = true;

    /// <summary>Finds a resource by its key.</summary>
    /// <param name="key">The resource key to find.</param>
    /// <returns>The resource entry, or null if not found.</returns>
    public IResourceIndexEntry? Find(ResourceKey key)
    {
        return _resources.Find(e => !e.IsDeleted && e.Key == key);
    }

    /// <summary>Finds a resource by type, group and instance.</summary>
    /// <remarks>
    /// Equivalent of the legacy TGI indexer (PackageIndex.cs lines 132-143); like <see cref="Find(ResourceKey)"/>,
    /// entries marked deleted are skipped (as legacy AddResource does when checking duplicates, Package.cs line 466).
    /// </remarks>
    public IResourceIndexEntry? Find(uint resourceType, uint resourceGroup, ulong instance) =>
        Find(new ResourceKey(resourceType, resourceGroup, instance));

    /// <summary>
    /// Sorts the index in place by a named field.
    /// </summary>
    /// <param name="field">
    /// A legacy field name (ResourceType, ResourceGroup, Instance, Chunkoffset, Filesize, Memsize, Compressed,
    /// Unknown2) or modern name (Key, ChunkOffset, FileSize, MemorySize, CompressionType, IsDeleted); case-insensitive.
    /// </param>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/PackageIndex.cs line 123 (Sort(string) using
    /// AApiVersionedFields.Comparer). The sort is stable. Sorting changes save order but not content, so it does not
    /// mark the package dirty (as in legacy).
    /// </remarks>
    /// <exception cref="ArgumentException">Unknown field name.</exception>
    public void SortResources(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Func<ResourceIndexEntry, IComparable> selector = field.ToUpperInvariant() switch
        {
            "RESOURCETYPE" => e => e.Key.ResourceType,
            "RESOURCEGROUP" => e => e.Key.ResourceGroup,
            "INSTANCE" => e => e.Key.Instance,
            "KEY" => e => (e.Key.ResourceType, e.Key.ResourceGroup, e.Key.Instance),
            "CHUNKOFFSET" => e => e.ChunkOffset,
            "FILESIZE" => e => e.FileSize,
            "MEMSIZE" or "MEMORYSIZE" => e => e.MemorySize,
            "COMPRESSED" or "COMPRESSIONTYPE" => e => e.CompressionType,
            "UNKNOWN2" => e => e.Unknown2,
            "ISDELETED" => e => e.IsDeleted,
            _ => throw new ArgumentException($"Unknown index field: {field}", nameof(field)),
        };
        var sorted = _resources.OrderBy(selector).ToList();
        _resources.Clear();
        _resources.AddRange(sorted);
        OnResourceIndexInvalidated();
    }

    /// <summary>Finds a resource matching the predicate.</summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The first matching resource entry, or null if not found.</returns>
    public IResourceIndexEntry? Find(Func<IResourceIndexEntry, bool> predicate)
    {
        return _resources.Find(e => !e.IsDeleted && predicate(e));
    }

    /// <summary>Finds all resources matching the predicate.</summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>All matching resource entries.</returns>
    public IEnumerable<IResourceIndexEntry> FindAll(Func<IResourceIndexEntry, bool> predicate)
    {
        return _resources.Where(e => !e.IsDeleted && predicate(e));
    }

    /// <summary>Gets the raw data for a resource.</summary>
    /// <param name="entry">The resource entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource data.</returns>
    public async ValueTask<ReadOnlyMemory<byte>> GetResourceDataAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);

        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));

        // Replaced/added data is held in memory; clean data is read on demand and never cached
        // (legacy only keeps ResourceStream for replaced data, ResourceIndexEntry.cs lines 252-256).
        if (rie.ResourceData.HasValue)
            return rie.ResourceData.Value;

        return await ReadResourceDataAsync(rie, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads (and decompresses) the stored data for an entry from the backing stream.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 649-673</remarks>
    internal async ValueTask<ReadOnlyMemory<byte>> ReadResourceDataAsync(ResourceIndexEntry rie, CancellationToken cancellationToken)
    {
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

        byte[] stored = new byte[rie.FileSize];
        await _streamLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Verify resource doesn't extend beyond file
            if ((ulong)rie.ChunkOffset + rie.FileSize > (ulong)_stream.Length)
                throw new PackageFormatException(
                    $"Resource at 0x{rie.ChunkOffset:X8} extends beyond end of file.");

            _stream.Position = rie.ChunkOffset;
            await ReadExactlyAsync(_stream, stored, $"resource at offset 0x{rie.ChunkOffset:X8}", cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _streamLock.Release();
        }

        if (!rie.IsCompressed)
            return stored;

        var data = Decompressor.Decompress(stored, (int)rie.MemorySize);

        // Source: legacy_references/Sims4Tools/s4pi/Package/Compression.cs lines 83-86
        if (LibrarySettings.Checking && data.Length != rie.MemorySize)
            throw new PackageFormatException(
                $"Resource data indicates size does not match index at 0x{rie.ChunkOffset:X8}. " +
                $"Read 0x{data.Length:X8}. Expected 0x{rie.MemorySize:X8}.");

        return data;
    }

    /// <summary>Gets a resource wrapper, resolved through <see cref="ResourceHandlers"/>.</summary>
    /// <param name="entry">The resource entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource, wrapped by the registered (non-disabled) factory for its type, else the default.</returns>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs line 53</remarks>
    public ValueTask<IResource> GetResourceAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default) =>
        GetResourceAsync(entry, alwaysDefault: false, cancellationToken);

    /// <summary>Gets a resource wrapper, optionally forcing the default (raw bytes) wrapper.</summary>
    /// <param name="entry">The resource entry.</param>
    /// <param name="alwaysDefault">When true, always use the registry's default factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 64-67</remarks>
    public async ValueTask<IResource> GetResourceAsync(
        IResourceIndexEntry entry,
        bool alwaysDefault,
        CancellationToken cancellationToken = default)
    {
        var data = await GetResourceDataAsync(entry, cancellationToken).ConfigureAwait(false);
        return ResourceHandlers.CreateResource(entry.Key, data, alwaysDefault);
    }

    /// <summary>Adds a resource to the package.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    /// <param name="rejectDuplicates">Whether to reject duplicate keys.</param>
    /// <returns>The new resource entry, or null if rejected as duplicate.</returns>
    public IResourceIndexEntry? AddResource(ResourceKey key, ReadOnlyMemory<byte> data, bool rejectDuplicates = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");

        if (rejectDuplicates && Find(key) != null)
            return null;

        // New entries start with Compressed = 0 (zeroed index entry, PackageIndex.cs lines 74-87), so they are
        // stored uncompressed unless the caller sets CompressionType (legacy packedChunk, Package.cs line 570).
        // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 464-471
        var entry = new ResourceIndexEntry(key)
        {
            MemorySize = (uint)data.Length,
            ResourceData = data.ToArray(),
            IsDirty = true,
            ElementChanged = OnEntryChanged,
        };

        _resources.Add(entry);
        IsDirty = true;
        return entry;
    }

    /// <summary>Adds an empty resource (legacy <c>AddResource(rk, null, rejectDups)</c>).</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="rejectDuplicates">Whether to reject duplicate keys.</param>
    /// <returns>The new resource entry, or null if rejected as duplicate.</returns>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 464-471</remarks>
    public IResourceIndexEntry? AddResource(ResourceKey key, bool rejectDuplicates = true) =>
        AddResource(key, ReadOnlyMemory<byte>.Empty, rejectDuplicates);

    /// <summary>Adds a resource using the data of a resource wrapper.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="resource">The resource whose <see cref="IResource.Data"/> is stored.</param>
    /// <param name="rejectDuplicates">Whether to reject duplicate keys.</param>
    /// <returns>The new resource entry, or null if rejected as duplicate.</returns>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 464-471 (stream = res.Stream)</remarks>
    public IResourceIndexEntry? AddResource(ResourceKey key, IResource resource, bool rejectDuplicates = true)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return AddResource(key, resource.Data, rejectDuplicates);
    }

    /// <summary>Replaces the data for an existing resource with a wrapper's data.</summary>
    /// <param name="entry">The resource entry to replace.</param>
    /// <param name="resource">The source resource.</param>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs line 478</remarks>
    public void ReplaceResource(IResourceIndexEntry entry, IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ReplaceResource(entry, resource.Data);
    }

    /// <summary>
    /// Sets the compression type of an entry; the resource is re-encoded on the next save.
    /// </summary>
    /// <param name="entry">The resource entry.</param>
    /// <param name="compressionType">0x0000 = store uncompressed; non-zero (e.g. 0x5A42) = compress if smaller.</param>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 116-120; Package.cs line 570</remarks>
    public void SetCompression(IResourceIndexEntry entry, ushort compressionType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");
        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));
        rie.CompressionType = compressionType;
    }

    /// <summary>Replaces the data for an existing resource.</summary>
    /// <param name="entry">The resource entry to replace.</param>
    /// <param name="data">The new resource data.</param>
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

    /// <summary>Deletes a resource from the package.</summary>
    /// <param name="entry">The resource entry to delete.</param>
    public void DeleteResource(IResourceIndexEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new InvalidOperationException("Package is read-only.");

        if (entry is not ResourceIndexEntry rie)
            throw new ArgumentException("Entry is not from this package.", nameof(entry));

        // Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 483-487
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
        bool streamLocked = false;

        try
        {
            // Write to temp file first
            await using (var tempStream = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await SaveToStreamAsync(tempStream, cancellationToken).ConfigureAwait(false);
            }

            // Hold the stream lock while the backing stream is rewritten so no concurrent read sees it half-written.
            await _streamLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            streamLocked = true;

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
            await ReadExactlyAsync(_stream, _header, "header after save", cancellationToken).ConfigureAwait(false);
            ValidateHeader();

            // Discard the in-memory index and re-read it from the saved file, so entries
            // carry their new offsets/sizes, deleted entries are dropped, and dirty flags
            // and cached data are cleared. Without this, later reads (and a second save)
            // would use stale offsets into the rewritten file.
            // Source: s4pi/Package/Package.cs lines 81-86 (index = null; re-read on demand)
            await ReadIndexAsync(cancellationToken).ConfigureAwait(false);

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

            if (streamLocked)
                _streamLock.Release();

            try { File.Delete(tmpFile); } catch { /* Ignore cleanup errors */ }
        }
    }

    /// <summary>
    /// Saves the package to a new file path. The package keeps its original backing file and stays dirty
    /// (legacy SaveAs does not reset any state).
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

    /// <summary>Saves the package to a stream. Does not change <see cref="IsDirty"/> (legacy SaveAs(Stream)).</summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 96-147</remarks>
    public async ValueTask SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);
        if (ReferenceEquals(stream, _stream))
            throw new ArgumentException("Cannot save a package onto its own backing stream; use SaveAsync.", nameof(stream));

        var writer = new PackageWriter(this, _resources, _stream, _streamLock);
        await writer.WriteAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_stream != null && !_leaveOpen)
        {
            _stream.Dispose();
        }
        _streamLock.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_stream != null && !_leaveOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        _streamLock.Dispose();
    }

    internal byte[] Header => _header;
}
