using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Materials;

/// <summary>
/// Represents an MTBL (Material Table) resource that contains material definitions and model references.
/// This resource type (0x81CA1A10) handles material tables with entries for 3D models and their properties.
/// </summary>
public sealed class MTBLResource : IResource, IDisposable
{
    private bool _disposed;
    private readonly ResourceKey _key;
    private byte[] _data;
    private MemoryStream? _stream;
    private readonly List<MTBLEntry> _entries = new();

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the raw data of the MTBL resource.
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// Gets the version of the MTBL format.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Gets the material table entries.
    /// </summary>
    public IReadOnlyList<MTBLEntry> Entries => _entries.AsReadOnly();

    #region IResource Implementation

    /// <summary>
    /// Gets the resource data as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            return _stream ??= new MemoryStream(_data, writable: false);
        }
    }

    /// <summary>
    /// Gets the resource data as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return (byte[])_data.Clone();
        }
    }

    /// <summary>
    /// Occurs when the resource has been changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version for this resource.
    /// </summary>
    public int RequestedApiVersion { get; init; } = 1;

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        "Version", "Entries", "Data"
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field value by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => TypedValue.Create(Version),
            1 => TypedValue.Create(_entries.Count),
            2 => TypedValue.Create(_data),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 2: // Data field
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
                        ParseEntries();
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// Gets or sets a content field value by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[string name]
    {
        get => name switch
        {
            "Version" => TypedValue.Create(Version),
            "Entries" => TypedValue.Create(_entries.Count),
            "Data" => TypedValue.Create(_data),
            _ => throw new ArgumentException($"Unknown field name: {name}", nameof(name))
        };
        set
        {
            switch (name)
            {
                case "Data":
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
                        ParseEntries();
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown field name: {name}", nameof(name));
            }
        }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MTBLResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The raw MTBL data.</param>
    public MTBLResource(ResourceKey key, byte[] data)
    {
        _key = key;
        _data = data ?? throw new ArgumentNullException(nameof(data));
        ParseData();
    }

    /// <summary>
    /// Parses the MTBL data to extract version and entries.
    /// </summary>
    private void ParseData()
    {
        if (_data.Length < 8)
        {
            Version = 1;
            return;
        }

        try
        {
            using var reader = new BinaryReader(new MemoryStream(_data));
            
            // Read version (simplified parsing)
            Version = reader.ReadUInt32();
            var entryCount = reader.ReadUInt32();
            
            ParseEntries();
        }
        catch
        {
            // If parsing fails, use defaults
            Version = 1;
        }
    }

    /// <summary>
    /// Parses material table entries from the data.
    /// </summary>
    private void ParseEntries()
    {
        _entries.Clear();
        
        if (_data.Length < 12)
            return;

        try
        {
            using var reader = new BinaryReader(new MemoryStream(_data));
            reader.ReadUInt32(); // Skip version
            var entryCount = reader.ReadUInt32();
            
            for (int i = 0; i < entryCount && reader.BaseStream.Position < reader.BaseStream.Length - 64; i++)
            {
                var entry = ParseEntry(reader);
                if (entry != null)
                    _entries.Add(entry);
            }
        }
        catch
        {
            // If parsing fails, leave entries empty
        }
    }

    /// <summary>
    /// Parses a single MTBL entry from the binary reader.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The parsed entry or null if parsing failed.</returns>
    private static MTBLEntry? ParseEntry(BinaryReader reader)
    {
        try
        {
            var modelIID = reader.ReadUInt64();
            var baseFileNameHash = reader.ReadUInt64();
            var widthAndMappingFlags = reader.ReadUInt32();
            var minimumWallHeight = reader.ReadByte();
            var numberOfLevels = reader.ReadByte();
            var unused = reader.ReadByte();
            reader.ReadByte(); // padding
            
            var thumbnailBoundsMinX = reader.ReadSingle();
            var thumbnailBoundsMinZ = reader.ReadSingle();
            var thumbnailBoundsMinY = reader.ReadSingle();
            var thumbnailBoundsMaxX = reader.ReadSingle();
            var thumbnailBoundsMaxZ = reader.ReadSingle();
            var thumbnailBoundsMaxY = reader.ReadSingle();
            
            var modelFlags = reader.ReadUInt32();
            var vfxHash = reader.ReadUInt64();

            return new MTBLEntry(
                modelIID, baseFileNameHash, widthAndMappingFlags,
                minimumWallHeight, numberOfLevels, unused,
                thumbnailBoundsMinX, thumbnailBoundsMinZ, thumbnailBoundsMinY,
                thumbnailBoundsMaxX, thumbnailBoundsMaxZ, thumbnailBoundsMaxY,
                modelFlags, vfxHash);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes the MTBL resource to a byte array.
    /// </summary>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize()
    {
        ThrowIfDisposed();
        return (byte[])_data.Clone();
    }

    /// <summary>
    /// Serializes the MTBL resource to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await stream.WriteAsync(_data, cancellationToken);
    }

    /// <summary>
    /// Gets the size of the resource data.
    /// </summary>
    /// <returns>The size in bytes.</returns>
    public long GetSize()
    {
        ThrowIfDisposed();
        return _data.Length;
    }

    /// <summary>
    /// Creates a clone of this MTBL resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A cloned MTBL resource.</returns>
    public MTBLResource Clone(ResourceKey newKey)
    {
        ThrowIfDisposed();
        return new MTBLResource(newKey, (byte[])_data.Clone());
    }

    /// <summary>
    /// Throws if the resource has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MTBLResource));
        }
    }

    /// <summary>
    /// Releases all resources used by the MTBLResource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Returns a string representation of this MTBL resource.
    /// </summary>
    /// <returns>A descriptive string.</returns>
    public override string ToString()
    {
        return $"MTBLResource [Key={_key}, Version={Version}, Entries={_entries.Count}, Size={_data.Length} bytes]";
    }
}

/// <summary>
/// Represents a single entry in a material table.
/// </summary>
public sealed class MTBLEntry
{
    /// <summary>
    /// Gets the model instance ID.
    /// </summary>
    public ulong ModelIID { get; }

    /// <summary>
    /// Gets the base filename hash.
    /// </summary>
    public ulong BaseFileNameHash { get; }

    /// <summary>
    /// Gets the width and mapping flags.
    /// </summary>
    public uint WidthAndMappingFlags { get; }

    /// <summary>
    /// Gets the minimum wall height.
    /// </summary>
    public byte MinimumWallHeight { get; }

    /// <summary>
    /// Gets the number of levels.
    /// </summary>
    public byte NumberOfLevels { get; }

    /// <summary>
    /// Gets the unused field.
    /// </summary>
    public byte Unused { get; }

    /// <summary>
    /// Gets the thumbnail bounds minimum X coordinate.
    /// </summary>
    public float ThumbnailBoundsMinX { get; }

    /// <summary>
    /// Gets the thumbnail bounds minimum Z coordinate.
    /// </summary>
    public float ThumbnailBoundsMinZ { get; }

    /// <summary>
    /// Gets the thumbnail bounds minimum Y coordinate.
    /// </summary>
    public float ThumbnailBoundsMinY { get; }

    /// <summary>
    /// Gets the thumbnail bounds maximum X coordinate.
    /// </summary>
    public float ThumbnailBoundsMaxX { get; }

    /// <summary>
    /// Gets the thumbnail bounds maximum Z coordinate.
    /// </summary>
    public float ThumbnailBoundsMaxZ { get; }

    /// <summary>
    /// Gets the thumbnail bounds maximum Y coordinate.
    /// </summary>
    public float ThumbnailBoundsMaxY { get; }

    /// <summary>
    /// Gets the model flags.
    /// </summary>
    public uint ModelFlags { get; }

    /// <summary>
    /// Gets the VFX hash.
    /// </summary>
    public ulong VfxHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MTBLEntry"/> class.
    /// </summary>
    public MTBLEntry(
        ulong modelIID, ulong baseFileNameHash, uint widthAndMappingFlags,
        byte minimumWallHeight, byte numberOfLevels, byte unused,
        float thumbnailBoundsMinX, float thumbnailBoundsMinZ, float thumbnailBoundsMinY,
        float thumbnailBoundsMaxX, float thumbnailBoundsMaxZ, float thumbnailBoundsMaxY,
        uint modelFlags, ulong vfxHash)
    {
        ModelIID = modelIID;
        BaseFileNameHash = baseFileNameHash;
        WidthAndMappingFlags = widthAndMappingFlags;
        MinimumWallHeight = minimumWallHeight;
        NumberOfLevels = numberOfLevels;
        Unused = unused;
        ThumbnailBoundsMinX = thumbnailBoundsMinX;
        ThumbnailBoundsMinZ = thumbnailBoundsMinZ;
        ThumbnailBoundsMinY = thumbnailBoundsMinY;
        ThumbnailBoundsMaxX = thumbnailBoundsMaxX;
        ThumbnailBoundsMaxZ = thumbnailBoundsMaxZ;
        ThumbnailBoundsMaxY = thumbnailBoundsMaxY;
        ModelFlags = modelFlags;
        VfxHash = vfxHash;
    }

    /// <summary>
    /// Returns a string representation of this MTBL entry.
    /// </summary>
    /// <returns>A descriptive string.</returns>
    public override string ToString()
    {
        return $"MTBLEntry [ModelIID=0x{ModelIID:X16}, BaseFileNameHash=0x{BaseFileNameHash:X16}, Levels={NumberOfLevels}]";
    }
}
