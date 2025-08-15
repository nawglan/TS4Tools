using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Implementation of icon resources that handle UI icons and visual elements for The Sims 4.
/// Supports DDS, PNG, TGA and other formats with sprite atlas capabilities.
/// Resource type: 0x73E93EEC
/// </summary>
public sealed class IconResource : IIconResource, IResource, INotifyPropertyChanged, IEquatable<IconResource>, IDisposable
{
    #region Constants

    /// <summary>
    /// The resource type identifier for icon resources.
    /// </summary>
    public const string ResourceType = "0x73E93EEC";

    /// <summary>
    /// The current version of the icon resource format.
    /// </summary>
    public const uint CurrentVersion = 1;

    /// <summary>
    /// Magic number for icon resource identification.
    /// </summary>
    private const uint IconMagic = 0x49434F4E; // "ICON"

    #endregion

    #region Fields

    private readonly ILogger<IconResource> _logger;
    private readonly ResourceKey _resourceKey;
    private bool _disposed;
    private bool _isModified;

    private int _width;
    private int _height;
    private IconFormat _format = IconFormat.Unknown;
    private IconCategory _category = IconCategory.General;
    private IconMetadata _metadata = new();
    private AtlasCoordinates? _atlasCoordinates;
    private ReadOnlyMemory<byte> _pixelData = ReadOnlyMemory<byte>.Empty;

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IconResource"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="resourceKey">The resource key for this icon.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or resourceKey is null.</exception>
    public IconResource(ILogger<IconResource> logger, ResourceKey resourceKey)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));

        _logger.LogDebug("Created IconResource for key {ResourceKey}", resourceKey);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IconResource"/> class from stream data.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="resourceKey">The resource key for this icon.</param>
    /// <param name="stream">The stream containing icon data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public IconResource(ILogger<IconResource> logger, ResourceKey resourceKey, Stream stream, CancellationToken cancellationToken = default)
        : this(logger, resourceKey)
    {
        ArgumentNullException.ThrowIfNull(stream);
        LoadFromStreamAsync(stream, cancellationToken).GetAwaiter().GetResult();
    }

    #endregion

    #region IResource Implementation

    /// <inheritdoc />
    public ResourceKey ResourceKey => _resourceKey;

    /// <inheritdoc />
    public Stream Stream => new MemoryStream(_pixelData.ToArray(), false);

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IIconResource Implementation

    /// <inheritdoc />
    public int Width
    {
        get => _width;
        private set
        {
            if (_width != value)
            {
                _width = value;
                _isModified = true;
                OnPropertyChanged(nameof(Width));
            }
        }
    }

    /// <inheritdoc />
    public int Height
    {
        get => _height;
        private set
        {
            if (_height != value)
            {
                _height = value;
                _isModified = true;
                OnPropertyChanged(nameof(Height));
            }
        }
    }

    /// <inheritdoc />
    public IconFormat Format
    {
        get => _format;
        private set
        {
            if (_format != value)
            {
                _format = value;
                _isModified = true;
                OnPropertyChanged(nameof(Format));
            }
        }
    }

    /// <inheritdoc />
    public IconCategory Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                _isModified = true;
                OnPropertyChanged(nameof(Category));
            }
        }
    }

    /// <inheritdoc />
    public IconMetadata Metadata
    {
        get => _metadata;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!_metadata.Equals(value))
            {
                _metadata = value;
                _isModified = true;
                OnPropertyChanged(nameof(Metadata));
            }
        }
    }

    /// <inheritdoc />
    public bool IsAtlasIcon => _atlasCoordinates != null;

    /// <inheritdoc />
    public AtlasCoordinates? AtlasCoordinates
    {
        get => _atlasCoordinates;
        set
        {
            if (!Equals(_atlasCoordinates, value))
            {
                _atlasCoordinates = value;
                _isModified = true;
                OnPropertyChanged(nameof(AtlasCoordinates));
                OnPropertyChanged(nameof(IsAtlasIcon));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this icon resource has been modified.
    /// </summary>
    public bool IsModified => _isModified;

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetPixelDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_pixelData.IsEmpty)
        {
            _logger.LogWarning("Pixel data is empty for icon {ResourceKey}", _resourceKey);
        }

        await Task.CompletedTask; // For async compliance
        return _pixelData;
    }

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Loading icon resource from stream (length: {Length})", stream.Length);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read and validate magic number
            var magic = reader.ReadUInt32();
            if (magic != IconMagic)
            {
                _logger.LogWarning("Invalid magic number for icon resource: 0x{Magic:X8}", magic);
            }

            // Read version
            var version = reader.ReadUInt32();
            if (version > CurrentVersion)
            {
                _logger.LogWarning("Unsupported icon version: {Version}", version);
            }

            // Read dimensions
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();

            // Read format
            Format = (IconFormat)reader.ReadUInt32();

            // Read category
            Category = (IconCategory)reader.ReadUInt32();

            // Read metadata
            await ReadMetadataAsync(reader, cancellationToken);

            // Read atlas coordinates if present
            var hasAtlas = reader.ReadBoolean();
            if (hasAtlas)
            {
                AtlasCoordinates = new AtlasCoordinates
                {
                    X = reader.ReadInt32(),
                    Y = reader.ReadInt32(),
                    Width = reader.ReadInt32(),
                    Height = reader.ReadInt32(),
                    AtlasId = reader.ReadUInt32()
                };
            }

            // Read pixel data
            var pixelDataLength = reader.ReadInt32();
            if (pixelDataLength > 0)
            {
                var pixelData = reader.ReadBytes(pixelDataLength);
                _pixelData = new ReadOnlyMemory<byte>(pixelData);
            }

            _isModified = false;
            _logger.LogDebug("Successfully loaded icon resource: {Width}x{Height}, format: {Format}", Width, Height, Format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load icon resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Saving icon resource to stream: {Width}x{Height}", Width, Height);

        try
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            // Write magic number and version
            writer.Write(IconMagic);
            writer.Write(CurrentVersion);

            // Write dimensions
            writer.Write(Width);
            writer.Write(Height);

            // Write format and category
            writer.Write((uint)Format);
            writer.Write((uint)Category);

            // Write metadata
            await WriteMetadataAsync(writer, cancellationToken);

            // Write atlas coordinates
            writer.Write(IsAtlasIcon);
            if (IsAtlasIcon && AtlasCoordinates != null)
            {
                writer.Write(AtlasCoordinates.X);
                writer.Write(AtlasCoordinates.Y);
                writer.Write(AtlasCoordinates.Width);
                writer.Write(AtlasCoordinates.Height);
                writer.Write(AtlasCoordinates.AtlasId);
            }

            // Write pixel data
            var pixelDataSpan = _pixelData.Span;
            writer.Write(pixelDataSpan.Length);
            if (pixelDataSpan.Length > 0)
            {
                writer.Write(pixelDataSpan);
            }

            _isModified = false;
            _logger.LogDebug("Successfully saved icon resource");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save icon resource to stream");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IIconResource> CreateScaledAsync(int newWidth, int newHeight, CancellationToken cancellationToken = default)
    {
        if (newWidth <= 0 || newHeight <= 0)
            throw new ArgumentException("Width and height must be positive");

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Creating scaled icon: {OldWidth}x{OldHeight} -> {NewWidth}x{NewHeight}",
            Width, Height, newWidth, newHeight);

        // For now, create a simple scaled version - real implementation would use image processing
        var scaledIcon = new IconResource(_logger, _resourceKey)
        {
            Width = newWidth,
            Height = newHeight,
            Format = this.Format,
            Category = this.Category,
            Metadata = new IconMetadata
            {
                UsageHint = _metadata.UsageHint,
                DpiScaling = _metadata.DpiScaling,
                ShouldCache = _metadata.ShouldCache,
                Priority = _metadata.Priority
            }
        };

        // Copy metadata tags
        foreach (var tag in _metadata.Tags)
        {
            scaledIcon.Metadata.Tags[tag.Key] = tag.Value;
        }

        // In a real implementation, this would perform actual image scaling
        // For now, just return the template
        await Task.CompletedTask;
        return scaledIcon;
    }

    #endregion

    #region Private Methods

    private async Task ReadMetadataAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var usageHintLength = reader.ReadInt32();
        var usageHint = usageHintLength > 0 ? reader.ReadString() : string.Empty;

        var dpiScaling = reader.ReadSingle();
        var shouldCache = reader.ReadBoolean();
        var priority = reader.ReadInt32();

        var tagCount = reader.ReadInt32();
        var tags = new Dictionary<string, string>();

        for (int i = 0; i < tagCount; i++)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            tags[key] = value;
        }

        _metadata = new IconMetadata
        {
            UsageHint = usageHint,
            DpiScaling = dpiScaling,
            ShouldCache = shouldCache,
            Priority = priority
        };

        foreach (var tag in tags)
        {
            _metadata.Tags[tag.Key] = tag.Value;
        }

        await Task.CompletedTask;
    }

    private async Task WriteMetadataAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write(_metadata.UsageHint.Length);
        if (_metadata.UsageHint.Length > 0)
        {
            writer.Write(_metadata.UsageHint);
        }

        writer.Write(_metadata.DpiScaling);
        writer.Write(_metadata.ShouldCache);
        writer.Write(_metadata.Priority);

        writer.Write(_metadata.Tags.Count);
        foreach (var tag in _metadata.Tags)
        {
            writer.Write(tag.Key);
            writer.Write(tag.Value);
        }

        await Task.CompletedTask;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IEquatable Implementation

    /// <inheritdoc />
    public bool Equals(IconResource? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return _resourceKey.Equals(other._resourceKey) &&
               Width == other.Width &&
               Height == other.Height &&
               Format == other.Format &&
               Category == other.Category &&
               _metadata.Equals(other._metadata) &&
               Equals(_atlasCoordinates, other._atlasCoordinates) &&
               _pixelData.Span.SequenceEqual(other._pixelData.Span);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as IconResource);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(_resourceKey, Width, Height, Format, Category, _metadata, _atlasCoordinates);

    #endregion

    #region IResource Implementation

    /// <summary>
    /// Gets the raw bytes of the resource.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            using var memoryStream = new MemoryStream();
            SaveToStreamAsync(memoryStream).GetAwaiter().GetResult();
            return memoryStream.ToArray();
        }
    }

    /// <summary>
    /// Occurs when the resource has been modified.
    /// </summary>
    public event EventHandler? ResourceChanged;

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    #endregion

    #region IContentFields Implementation

    private static readonly ReadOnlyCollection<string> ContentFieldNames = new List<string>
    {
        nameof(Width),
        nameof(Height),
        nameof(Format),
        nameof(Category),
        nameof(Metadata),
        nameof(AtlasCoordinates),
        nameof(IsAtlasIcon)
    }.AsReadOnly();

    /// <summary>
    /// Gets the content field names for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields => ContentFieldNames;

    /// <summary>
    /// Gets or sets a field value by index.
    /// </summary>
    /// <param name="index">The zero-based index of the field.</param>
    /// <returns>The field value at the specified index.</returns>
    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    /// <summary>
    /// Gets or sets a field value by name.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>The field value with the specified name.</returns>
    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    private TypedValue GetFieldByIndex(int index)
    {
        if (index < 0 || index >= ContentFieldNames.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0-{ContentFieldNames.Count - 1}.");

        return GetFieldByName(ContentFieldNames[index]);
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        if (index < 0 || index >= ContentFieldNames.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0-{ContentFieldNames.Count - 1}.");

        SetFieldByName(ContentFieldNames[index], value);
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(Width) => new TypedValue(typeof(int), Width),
            nameof(Height) => new TypedValue(typeof(int), Height),
            nameof(Format) => new TypedValue(typeof(IconFormat), Format),
            nameof(Category) => new TypedValue(typeof(IconCategory), Category),
            nameof(Metadata) => new TypedValue(typeof(IconMetadata), Metadata),
            nameof(AtlasCoordinates) => new TypedValue(typeof(AtlasCoordinates), AtlasCoordinates),
            nameof(IsAtlasIcon) => new TypedValue(typeof(bool), IsAtlasIcon),
            _ => throw new ArgumentOutOfRangeException(nameof(name), $"Unknown field name: {name}")
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            case nameof(Width):
                if (value.Type == typeof(int) && value.Value is int widthValue)
                {
                    _width = widthValue;
                    OnPropertyChanged(nameof(Width));
                }
                else
                {
                    throw new ArgumentException($"Expected int for {nameof(Width)}, got {value.Type}");
                }
                break;

            case nameof(Height):
                if (value.Type == typeof(int) && value.Value is int heightValue)
                {
                    _height = heightValue;
                    OnPropertyChanged(nameof(Height));
                }
                else
                {
                    throw new ArgumentException($"Expected int for {nameof(Height)}, got {value.Type}");
                }
                break;

            case nameof(Format):
                if (value.Type == typeof(IconFormat) && value.Value is IconFormat formatValue)
                {
                    _format = formatValue;
                    OnPropertyChanged(nameof(Format));
                }
                else
                {
                    throw new ArgumentException($"Expected IconFormat for {nameof(Format)}, got {value.Type}");
                }
                break;

            case nameof(Category):
                if (value.Type == typeof(IconCategory) && value.Value is IconCategory categoryValue)
                {
                    _category = categoryValue;
                    OnPropertyChanged(nameof(Category));
                }
                else
                {
                    throw new ArgumentException($"Expected IconCategory for {nameof(Category)}, got {value.Type}");
                }
                break;

            case nameof(Metadata):
                if (value.Type == typeof(IconMetadata) && value.Value is IconMetadata metadataValue)
                {
                    _metadata = metadataValue;
                    OnPropertyChanged(nameof(Metadata));
                }
                else
                {
                    throw new ArgumentException($"Expected IconMetadata for {nameof(Metadata)}, got {value.Type}");
                }
                break;

            case nameof(AtlasCoordinates):
                if (value.Type == typeof(AtlasCoordinates) && value.Value is AtlasCoordinates atlasValue)
                {
                    _atlasCoordinates = atlasValue;
                    OnPropertyChanged(nameof(AtlasCoordinates));
                    OnPropertyChanged(nameof(IsAtlasIcon));
                }
                else if (value.Value is null)
                {
                    _atlasCoordinates = null;
                    OnPropertyChanged(nameof(AtlasCoordinates));
                    OnPropertyChanged(nameof(IsAtlasIcon));
                }
                else
                {
                    throw new ArgumentException($"Expected AtlasCoordinates for {nameof(AtlasCoordinates)}, got {value.Type}");
                }
                break;

            case nameof(IsAtlasIcon):
                // IsAtlasIcon is read-only, computed from AtlasCoordinates
                throw new InvalidOperationException($"{nameof(IsAtlasIcon)} is read-only. Set {nameof(AtlasCoordinates)} instead.");

            default:
                throw new ArgumentOutOfRangeException(nameof(name), $"Unknown field name: {name}");
        }

        OnResourceChanged();
    }

    #endregion

    #region IApiVersion Implementation

    /// <summary>
    /// Gets the requested API version for this resource.
    /// </summary>
    public int RequestedApiVersion => 1;

    #endregion

    #region IDisposable Implementation

    private bool _syncDisposed = false;

    private void Dispose(bool disposing)
    {
        if (!_syncDisposed && !_disposed)
        {
            if (disposing)
            {
                _logger.LogDebug("Disposing IconResource for key {ResourceKey} (sync)", _resourceKey);
                _pixelData = ReadOnlyMemory<byte>.Empty;
            }

            _syncDisposed = true;
            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the IconResource.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region IAsyncDisposable Implementation

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing IconResource for key {ResourceKey}", _resourceKey);

            // Clear pixel data
            _pixelData = ReadOnlyMemory<byte>.Empty;

            _disposed = true;
            await Task.CompletedTask;
        }
    }

    #endregion
}
