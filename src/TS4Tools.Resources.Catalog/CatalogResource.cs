using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common.Collections;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Represents a catalog resource containing object definitions and metadata for The Sims 4.
/// This is essential simulation object metadata that defines how objects appear and behave in the catalog.
/// </summary>
public sealed class CatalogResource : IResource, IApiVersion, IContentFields, IEquatable<CatalogResource>, INotifyPropertyChanged
{
    #region Constants

    /// <summary>
    /// The current version of the catalog resource format.
    /// </summary>
    public const uint CurrentVersion = 1;

    /// <summary>
    /// The standard catalog version identifier.
    /// </summary>
    public const uint StandardCatalogVersion = 0x00000009;

    #endregion

    #region Fields

    private readonly ILogger<CatalogResource> _logger;
    private uint _version = CurrentVersion;
    private uint _catalogVersion = StandardCatalogVersion;
    private uint _nameHash;
    private uint _descriptionHash;
    private uint _price;
    private uint _unknown1;
    private uint _unknown2;
    private uint _unknown3;
    private List<ResourceReference> _styleReferences = [];
    private ushort _unknown4;
    private List<ushort> _tags = [];
    private List<SellingPoint> _sellingPoints = [];
    private ulong _unknown5;
    private ushort _unknown6;
    private ulong _unknown7;
    private bool _disposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogResource"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public CatalogResource(ILogger<CatalogResource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogResource"/> class from stream data.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="stream">The stream containing catalog resource data.</param>
    /// <param name="cancellationToken">The cancellation token for async operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or stream is null.</exception>
    public CatalogResource(ILogger<CatalogResource> logger, Stream stream, CancellationToken cancellationToken = default)
        : this(logger)
    {
        ArgumentNullException.ThrowIfNull(stream);
        LoadFromStreamAsync(stream, cancellationToken).GetAwaiter().GetResult();
    }

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IContentFields Implementation

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        nameof(Version),
        nameof(CatalogVersion),
        nameof(NameHash),
        nameof(DescriptionHash),
        nameof(Price),
        nameof(Unknown1),
        nameof(Unknown2),
        nameof(Unknown3),
        nameof(StyleReferences),
        nameof(Unknown4),
        nameof(Tags),
        nameof(SellingPoints),
        nameof(Unknown5),
        nameof(Unknown6),
        nameof(Unknown7)
    }.AsReadOnly();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the version of the catalog resource format.
    /// </summary>
    [ElementPriority(0)]
    public uint Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    /// <summary>
    /// Gets or sets the catalog version identifier.
    /// </summary>
    [ElementPriority(1)]
    public uint CatalogVersion
    {
        get => _catalogVersion;
        set => SetProperty(ref _catalogVersion, value);
    }

    /// <summary>
    /// Gets or sets the FNV hash of the object name for localization.
    /// </summary>
    [ElementPriority(2)]
    public uint NameHash
    {
        get => _nameHash;
        set => SetProperty(ref _nameHash, value);
    }

    /// <summary>
    /// Gets or sets the FNV hash of the object description for localization.
    /// </summary>
    [ElementPriority(3)]
    public uint DescriptionHash
    {
        get => _descriptionHash;
        set => SetProperty(ref _descriptionHash, value);
    }

    /// <summary>
    /// Gets or sets the catalog price in simoleons.
    /// </summary>
    [ElementPriority(4)]
    public uint Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    /// <summary>
    /// Gets or sets an unknown field (possibly object flags or category).
    /// </summary>
    [ElementPriority(5)]
    public uint Unknown1
    {
        get => _unknown1;
        set => SetProperty(ref _unknown1, value);
    }

    /// <summary>
    /// Gets or sets an unknown field (possibly object properties).
    /// </summary>
    [ElementPriority(6)]
    public uint Unknown2
    {
        get => _unknown2;
        set => SetProperty(ref _unknown2, value);
    }

    /// <summary>
    /// Gets or sets an unknown field (possibly thumbnail or icon reference).
    /// </summary>
    [ElementPriority(7)]
    public uint Unknown3
    {
        get => _unknown3;
        set => SetProperty(ref _unknown3, value);
    }

    /// <summary>
    /// Gets the list of style-related resource references (meshes, textures, etc.).
    /// </summary>
    [ElementPriority(8)]
    public IList<ResourceReference> StyleReferences => _styleReferences;

    /// <summary>
    /// Gets or sets an unknown field (possibly style flags).
    /// </summary>
    [ElementPriority(9)]
    public ushort Unknown4
    {
        get => _unknown4;
        set => SetProperty(ref _unknown4, value);
    }

    /// <summary>
    /// Gets the list of catalog tags for filtering and categorization.
    /// </summary>
    [ElementPriority(10)]
    public IList<ushort> Tags => _tags;

    /// <summary>
    /// Gets the list of selling points (commodity effects).
    /// </summary>
    [ElementPriority(11)]
    public IList<SellingPoint> SellingPoints => _sellingPoints;

    /// <summary>
    /// Gets or sets an unknown field (possibly object behavior flags).
    /// </summary>
    [ElementPriority(12)]
    public ulong Unknown5
    {
        get => _unknown5;
        set => SetProperty(ref _unknown5, value);
    }

    /// <summary>
    /// Gets or sets an unknown field (possibly placement flags).
    /// </summary>
    [ElementPriority(13)]
    public ushort Unknown6
    {
        get => _unknown6;
        set => SetProperty(ref _unknown6, value);
    }

    /// <summary>
    /// Gets or sets an unknown field (possibly additional metadata).
    /// </summary>
    [ElementPriority(14)]
    public ulong Unknown7
    {
        get => _unknown7;
        set => SetProperty(ref _unknown7, value);
    }

    #endregion

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream { get; private set; } = new MemoryStream();

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            using var ms = new MemoryStream();
            SaveToStreamAsync(ms).GetAwaiter().GetResult();
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public int RequestedApiVersion { get; set; } = 1;

    /// <inheritdoc />
    public async Task<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        await SaveToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    #endregion

    #region IContentFields Implementation (Indexers)

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get
        {
            if (index < 0 || index >= ContentFields.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return this[ContentFields[index]];
        }
        set
        {
            if (index < 0 || index >= ContentFields.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            this[ContentFields[index]] = value;
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => name switch
        {
            nameof(Version) => TypedValue.Create(_version),
            nameof(CatalogVersion) => TypedValue.Create(_catalogVersion),
            nameof(NameHash) => TypedValue.Create(_nameHash),
            nameof(DescriptionHash) => TypedValue.Create(_descriptionHash),
            nameof(Price) => TypedValue.Create(_price),
            nameof(Unknown1) => TypedValue.Create(_unknown1),
            nameof(Unknown2) => TypedValue.Create(_unknown2),
            nameof(Unknown3) => TypedValue.Create(_unknown3),
            nameof(StyleReferences) => TypedValue.Create(_styleReferences),
            nameof(Unknown4) => TypedValue.Create(_unknown4),
            nameof(Tags) => TypedValue.Create(_tags),
            nameof(SellingPoints) => TypedValue.Create(_sellingPoints),
            nameof(Unknown5) => TypedValue.Create(_unknown5),
            nameof(Unknown6) => TypedValue.Create(_unknown6),
            nameof(Unknown7) => TypedValue.Create(_unknown7),
            _ => throw new ArgumentException($"Field '{name}' not found", nameof(name))
        };
        set
        {
            switch (name)
            {
                case nameof(Version):
                    {
                        uint? val = value.GetValue<uint>();
                        Version = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(CatalogVersion):
                    {
                        uint? val = value.GetValue<uint>();
                        CatalogVersion = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(NameHash):
                    {
                        uint? val = value.GetValue<uint>();
                        NameHash = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(DescriptionHash):
                    {
                        uint? val = value.GetValue<uint>();
                        DescriptionHash = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(Price):
                    {
                        uint? val = value.GetValue<uint>();
                        Price = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(Unknown1):
                    {
                        uint? val = value.GetValue<uint>();
                        Unknown1 = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(Unknown2):
                    {
                        uint? val = value.GetValue<uint>();
                        Unknown2 = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(Unknown3):
                    {
                        uint? val = value.GetValue<uint>();
                        Unknown3 = val.HasValue ? val.Value : 0u;
                        break;
                    }
                case nameof(Unknown4):
                    {
                        ushort? val = value.GetValue<ushort>();
                        Unknown4 = val.HasValue ? val.Value : (ushort)0;
                        break;
                    }
                case nameof(Unknown5):
                    {
                        ulong? val = value.GetValue<ulong>();
                        Unknown5 = val.HasValue ? val.Value : 0ul;
                        break;
                    }
                case nameof(Unknown6):
                    {
                        ushort? val = value.GetValue<ushort>();
                        Unknown6 = val.HasValue ? val.Value : (ushort)0;
                        break;
                    }
                case nameof(Unknown7):
                    {
                        ulong? val = value.GetValue<ulong>();
                        Unknown7 = val.HasValue ? val.Value : 0ul;
                        break;
                    }
                default:
                    throw new ArgumentException($"Field '{name}' not found or is read-only", nameof(name));
            }
        }
    }

    #endregion

    #region Data I/O Operations

    /// <summary>
    /// Loads catalog resource data from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing catalog data.</param>
    /// <param name="cancellationToken">The cancellation token for async operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream contains invalid catalog data.</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _logger.LogDebug("Loading catalog resource from stream (length: {Length} bytes)", stream.Length);

        // Ensure we have enough data for the minimum header
        const int minimumHeaderSize = 44; // Version through Unknown3 + byte count
        if (stream.Length < minimumHeaderSize)
        {
            throw new InvalidDataException($"Stream too short for catalog resource. Expected at least {minimumHeaderSize} bytes, got {stream.Length}");
        }

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read basic header
        _version = reader.ReadUInt32();
        _catalogVersion = reader.ReadUInt32();
        _nameHash = reader.ReadUInt32();
        _descriptionHash = reader.ReadUInt32();
        _price = reader.ReadUInt32();
        _unknown1 = reader.ReadUInt32();
        _unknown2 = reader.ReadUInt32();
        _unknown3 = reader.ReadUInt32();

        // Read style references
        var styleCount = reader.ReadByte();
        _styleReferences.Clear();
        for (int i = 0; i < styleCount; i++)
        {
            var instance = reader.ReadUInt64();
            var resourceType = reader.ReadUInt32();
            var resourceGroup = reader.ReadUInt32();

            _styleReferences.Add(new ResourceReference(resourceType, resourceGroup, instance));
        }

        // Read remaining fields
        _unknown4 = reader.ReadUInt16();

        // Read tags
        var tagCount = reader.ReadInt32();
        _tags.Clear();
        for (int i = 0; i < tagCount; i++)
        {
            _tags.Add(reader.ReadUInt16());
        }

        // Read selling points
        await ReadSellingPointsAsync(reader, cancellationToken).ConfigureAwait(false);

        _unknown5 = reader.ReadUInt64();
        _unknown6 = reader.ReadUInt16();
        _unknown7 = reader.ReadUInt64();

        Stream = stream;

        _logger.LogDebug("Successfully loaded catalog resource with {StyleCount} styles, {TagCount} tags, {SellingPointCount} selling points",
            _styleReferences.Count, _tags.Count, _sellingPoints.Count);

        OnPropertyChanged(nameof(Stream));
    }

    /// <summary>
    /// Saves catalog resource data to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The target stream for catalog data.</param>
    /// <param name="cancellationToken">The cancellation token for async operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _logger.LogDebug("Saving catalog resource to stream");

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write basic header
        writer.Write(_version);
        writer.Write(_catalogVersion);
        writer.Write(_nameHash);
        writer.Write(_descriptionHash);
        writer.Write(_price);
        writer.Write(_unknown1);
        writer.Write(_unknown2);
        writer.Write(_unknown3);

        // Write style references
        writer.Write((byte)_styleReferences.Count);
        foreach (var styleRef in _styleReferences)
        {
            writer.Write(styleRef.Instance);
            writer.Write(styleRef.ResourceType);
            writer.Write(styleRef.ResourceGroup);
        }

        // Write remaining fields
        writer.Write(_unknown4);

        // Write tags
        writer.Write(_tags.Count);
        foreach (var tag in _tags)
        {
            writer.Write(tag);
        }

        // Write selling points
        await WriteSellingPointsAsync(writer, cancellationToken).ConfigureAwait(false);

        writer.Write(_unknown5);
        writer.Write(_unknown6);
        writer.Write(_unknown7);

        _logger.LogDebug("Successfully saved catalog resource ({Length} bytes)", stream.Length);
    }

    private async Task ReadSellingPointsAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        var sellingPointCount = reader.ReadInt32();
        _sellingPoints.Clear();

        for (int i = 0; i < sellingPointCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var commodity = reader.ReadUInt16();
            var amount = reader.ReadUInt32();

            _sellingPoints.Add(new SellingPoint(commodity, amount));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task WriteSellingPointsAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        writer.Write(_sellingPoints.Count);

        foreach (var sellingPoint in _sellingPoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.Write(sellingPoint.Commodity);
            writer.Write(sellingPoint.Amount);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a formatted string representation of the catalog resource for debugging.
    /// </summary>
    /// <returns>A string representation containing key catalog information.</returns>
    public override string ToString()
    {
        return $"CatalogResource(Name={NameHash:X8}, Desc={DescriptionHash:X8}, Price={Price}, Tags={Tags.Count}, Styles={StyleReferences.Count})";
    }

    #endregion

    #region IEquatable<CatalogResource> Implementation

    /// <inheritdoc />
    public bool Equals(CatalogResource? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return _version == other._version &&
               _catalogVersion == other._catalogVersion &&
               _nameHash == other._nameHash &&
               _descriptionHash == other._descriptionHash &&
               _price == other._price &&
               _unknown1 == other._unknown1 &&
               _unknown2 == other._unknown2 &&
               _unknown3 == other._unknown3 &&
               _styleReferences.SequenceEqual(other._styleReferences) &&
               _unknown4 == other._unknown4 &&
               _tags.SequenceEqual(other._tags) &&
               _sellingPoints.SequenceEqual(other._sellingPoints) &&
               _unknown5 == other._unknown5 &&
               _unknown6 == other._unknown6 &&
               _unknown7 == other._unknown7;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CatalogResource other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_version);
        hash.Add(_catalogVersion);
        hash.Add(_nameHash);
        hash.Add(_descriptionHash);
        hash.Add(_price);
        hash.Add(_unknown1);
        hash.Add(_unknown2);
        hash.Add(_unknown3);
        foreach (var styleRef in _styleReferences)
            hash.Add(styleRef);
        hash.Add(_unknown4);
        foreach (var tag in _tags)
            hash.Add(tag);
        foreach (var sellingPoint in _sellingPoints)
            hash.Add(sellingPoint);
        hash.Add(_unknown5);
        hash.Add(_unknown6);
        hash.Add(_unknown7);
        return hash.ToHashCode();
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changes.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">The backing field for the property.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the value changed and PropertyChanged was raised.</returns>
    private bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        Stream?.Dispose();
        _disposed = true;
    }

    #endregion
}
