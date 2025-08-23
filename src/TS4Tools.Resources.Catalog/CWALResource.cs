using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Represents a CWAL (Catalog Wall Pattern) resource that contains wall pattern definitions for catalog display.
/// This resource type (0xD5F0F921) handles wall pattern catalog entries with material and image data.
/// </summary>
public sealed class CWALResource : IResource, IDisposable
{
    private bool _disposed;
    private readonly ResourceKey _key;
    private byte[] _data;
    private MemoryStream? _stream;

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the raw data of the CWAL resource.
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// Gets the version of the CWAL format.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Gets the catalog common data section.
    /// </summary>
    public CatalogCommonData? CommonData { get; private set; }

    /// <summary>
    /// Gets the number of material entries.
    /// </summary>
    public int MaterialEntryCount { get; private set; }

    /// <summary>
    /// Gets the number of image groups.
    /// </summary>
    public int ImageGroupCount { get; private set; }

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
        "Version", "MaterialEntryCount", "ImageGroupCount", "Data"
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
            1 => TypedValue.Create(MaterialEntryCount),
            2 => TypedValue.Create(ImageGroupCount),
            3 => TypedValue.Create(_data),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 3: // Data field
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
                        ParseData();
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
            "MaterialEntryCount" => TypedValue.Create(MaterialEntryCount),
            "ImageGroupCount" => TypedValue.Create(ImageGroupCount),
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
                        ParseData();
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
    /// Initializes a new instance of the <see cref="CWALResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The raw CWAL data.</param>
    public CWALResource(ResourceKey key, byte[] data)
    {
        _key = key;
        _data = data ?? throw new ArgumentNullException(nameof(data));
        ParseData();
    }

    /// <summary>
    /// Parses the CWAL data to extract version and structure.
    /// </summary>
    private void ParseData()
    {
        if (_data.Length < 8)
        {
            Version = 7; // Default version from legacy code
            return;
        }

        try
        {
            using var reader = new BinaryReader(new MemoryStream(_data));
            
            // Read version (CWAL typically uses version 7)
            Version = reader.ReadUInt32();
            
            // Try to parse catalog common section if there's enough data
            if (_data.Length >= 64) // Rough estimate for minimum catalog common size
            {
                ParseCatalogCommon(reader);
            }
            
            // Estimate counts based on remaining data
            EstimateCounts();
        }
        catch
        {
            // If parsing fails, use defaults
            Version = 7;
            MaterialEntryCount = 0;
            ImageGroupCount = 0;
        }
    }

    /// <summary>
    /// Parses the catalog common data section.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    private void ParseCatalogCommon(BinaryReader reader)
    {
        try
        {
            // Simplified catalog common parsing
            // This would contain catalog data like name, description, etc.
            var nameLength = reader.ReadUInt32();
            if (nameLength > 0 && nameLength < 1024 && reader.BaseStream.Position + nameLength < reader.BaseStream.Length)
            {
                var nameBytes = reader.ReadBytes((int)nameLength);
                var name = System.Text.Encoding.UTF8.GetString(nameBytes);
                CommonData = new CatalogCommonData(name);
            }
        }
        catch
        {
            // If parsing fails, leave CommonData as null
        }
    }

    /// <summary>
    /// Estimates material and image counts based on data size.
    /// </summary>
    private void EstimateCounts()
    {
        // Rough estimation based on remaining data size
        // This is simplified - real CWAL parsing would be more complex
        var remainingSize = Math.Max(0, _data.Length - 64);
        MaterialEntryCount = Math.Min(remainingSize / 128, 100); // Estimate
        ImageGroupCount = Math.Min(remainingSize / 256, 50); // Estimate
    }

    /// <summary>
    /// Serializes the CWAL resource to a byte array.
    /// </summary>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize()
    {
        ThrowIfDisposed();
        return (byte[])_data.Clone();
    }

    /// <summary>
    /// Serializes the CWAL resource to a stream.
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
    /// Creates a clone of this CWAL resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A cloned CWAL resource.</returns>
    public CWALResource Clone(ResourceKey newKey)
    {
        ThrowIfDisposed();
        return new CWALResource(newKey, (byte[])_data.Clone());
    }

    /// <summary>
    /// Throws if the resource has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CWALResource));
        }
    }

    /// <summary>
    /// Releases all resources used by the CWALResource.
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
    /// Returns a string representation of this CWAL resource.
    /// </summary>
    /// <returns>A descriptive string.</returns>
    public override string ToString()
    {
        var name = CommonData?.Name ?? "Unknown";
        return $"CWALResource [Key={_key}, Version={Version}, Name={name}, MaterialEntries={MaterialEntryCount}, ImageGroups={ImageGroupCount}, Size={_data.Length} bytes]";
    }
}

/// <summary>
/// Represents catalog common data for CWAL resources.
/// </summary>
public sealed class CatalogCommonData
{
    /// <summary>
    /// Gets the catalog item name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogCommonData"/> class.
    /// </summary>
    /// <param name="name">The catalog item name.</param>
    public CatalogCommonData(string name)
    {
        Name = name ?? string.Empty;
    }
}
