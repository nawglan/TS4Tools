using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Represents an MLOD (Mesh Level of Detail) resource that contains geometry LOD data.
/// This resource type (0x01D10F34) handles multiple levels of detail for 3D objects.
/// </summary>
public sealed class MLODResource : IResource, IDisposable
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
    /// Gets the raw data of the MLOD resource.
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// Gets the version of the MLOD format.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets the number of mesh levels of detail in this resource.
    /// </summary>
    public int MeshCount { get; }

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
        "Version", "MeshCount", "Data"
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
            1 => TypedValue.Create(MeshCount),
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
            "MeshCount" => TypedValue.Create(MeshCount),
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
    /// Initializes a new instance of the <see cref="MLODResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The raw MLOD data.</param>
    /// <param name="version">The MLOD format version.</param>
    /// <param name="meshCount">The number of mesh LODs.</param>
    public MLODResource(ResourceKey key, byte[] data, uint version = 1, int meshCount = 0)
    {
        _key = key;
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Version = version;
        MeshCount = meshCount;
    }

    /// <summary>
    /// Serializes the MLOD resource to a byte array.
    /// </summary>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize()
    {
        ThrowIfDisposed();
        return (byte[])_data.Clone();
    }

    /// <summary>
    /// Serializes the MLOD resource to a stream.
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
    /// Creates a clone of this MLOD resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A cloned MLOD resource.</returns>
    public MLODResource Clone(ResourceKey newKey)
    {
        ThrowIfDisposed();
        return new MLODResource(newKey, (byte[])_data.Clone(), Version, MeshCount);
    }

    /// <summary>
    /// Throws if the resource has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MLODResource));
        }
    }

    /// <summary>
    /// Releases all resources used by the MLODResource.
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
    /// Returns a string representation of this MLOD resource.
    /// </summary>
    /// <returns>A descriptive string.</returns>
    public override string ToString()
    {
        return $"MLODResource [Key={_key}, Version={Version}, MeshCount={MeshCount}, Size={_data.Length} bytes]";
    }
}
