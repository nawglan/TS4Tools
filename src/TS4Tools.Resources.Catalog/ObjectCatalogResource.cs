using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Implementation of object catalog resources for Buy/Build mode objects.
/// Handles object pricing, categorization, placement rules, and environmental impact.
/// </summary>
[CatalogResource([0x319E4F1D], CatalogType.Object, 300, "Object Catalog Resource for Buy/Build mode objects")]
public sealed class ObjectCatalogResource : IObjectCatalogResource
{
    private readonly ILogger<ObjectCatalogResource> _logger;
    private readonly ConcurrentDictionary<uint, object> _properties = new();
    private readonly Dictionary<EnvironmentType, float> _environmentScores = new();
    private readonly List<uint> _categories = new();
    private readonly List<TgiReference> _icons = new();
    private readonly List<ObjectSlotInfo> _slots = new();
    private readonly List<uint> _tags = new();
    private bool _disposed;

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream { get; private set; } = new MemoryStream();

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            if (Stream is MemoryStream ms)
            {
                return ms.ToArray();
            }

            // For other stream types, read all data
            var originalPosition = Stream.Position;
            try
            {
                Stream.Position = 0;
                using var memoryStream = new MemoryStream();
                Stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
            finally
            {
                Stream.Position = originalPosition;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public uint ResourceType { get; init; } = 0x319E4F1D; // Object catalog resource

    /// <inheritdoc />
    public uint GroupId { get; init; }

    /// <inheritdoc />
    public ulong InstanceId { get; init; }

    /// <inheritdoc />
    public bool IsDirty { get; private set; }

    /// <inheritdoc />
    public void MarkDirty()
    {
        IsDirty = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogDebug("Object catalog resource {InstanceId:X16} marked as dirty", InstanceId);
    }

    /// <inheritdoc />
    public void MarkClean()
    {
        IsDirty = false;
        _logger.LogDebug("Object catalog resource {InstanceId:X16} marked as clean", InstanceId);
    }

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public uint ApiVersion { get; private set; } = 1;

    /// <inheritdoc />
    public int RequestedApiVersion => (int)ApiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IContentFields Implementation

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields { get; private set; } = [];

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index < ContentFields.Count ? TypedValue.Create(ContentFields[index]) : TypedValue.Create(string.Empty);
        set => throw new NotSupportedException("Content fields are read-only in this implementation");
    }

    /// <inheritdoc />
    public TypedValue this[string fieldName]
    {
        get
        {
            var field = ContentFields.FirstOrDefault(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            return field is not null ? TypedValue.Create(field) : TypedValue.Create(string.Empty);
        }
        set => throw new NotSupportedException("Content fields are read-only in this implementation");
    }

    #endregion

    #region ICatalogResource Implementation

    /// <inheritdoc />
    public CatalogCommonBlock? CommonBlock { get; private set; }

    /// <inheritdoc />
    public uint Version { get; private set; }

    /// <inheritdoc />
    public CatalogType CatalogType { get; private set; } = CatalogType.Object;

    /// <inheritdoc />
    public bool IsModified => IsDirty;

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            _logger.LogDebug("Loading object catalog resource from stream (length: {Length})", stream.Length);

            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            await LoadFromReaderAsync(reader, cancellationToken);

            Stream = stream;
            MarkClean();

            _logger.LogInformation("Successfully loaded object catalog resource {InstanceId:X16}", InstanceId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load object catalog resource from stream");
            throw new InvalidDataException("Failed to parse object catalog resource data", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            _logger.LogDebug("Saving object catalog resource to stream");

            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            await SaveToWriterAsync(writer, cancellationToken);

            MarkClean();

            _logger.LogInformation("Successfully saved object catalog resource {InstanceId:X16}", InstanceId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save object catalog resource to stream");
            throw new InvalidDataException("Failed to save object catalog resource data", ex);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (CommonBlock == null)
        {
            errors.Add("Catalog common block is required");
        }

        if (Price == 0)
        {
            errors.Add("Object price must be greater than zero");
        }

        if (!_categories.Any())
        {
            errors.Add("At least one category is required");
        }

        return errors;
    }

    /// <inheritdoc />
    public async Task<ICatalogResource> CloneAsync()
    {
        var clone = new ObjectCatalogResource(_logger, GroupId, InstanceId)
        {
            Version = Version,
            CommonBlock = CommonBlock,
            Price = Price,
            PlacementRules = PlacementRules,
            RigInfo = RigInfo,
            IsAvailableForPurchase = IsAvailableForPurchase,
            DepreciationValue = DepreciationValue,
            DescriptionKey = DescriptionKey
        };

        // Copy collections
        foreach (var category in _categories)
        {
            clone._categories.Add(category);
        }

        foreach (var kvp in _environmentScores)
        {
            clone._environmentScores[kvp.Key] = kvp.Value;
        }

        foreach (var icon in _icons)
        {
            clone._icons.Add(icon);
        }

        foreach (var slot in _slots)
        {
            clone._slots.Add(slot);
        }

        foreach (var tag in _tags)
        {
            clone._tags.Add(tag);
        }

        foreach (var kvp in _properties)
        {
            clone._properties[kvp.Key] = kvp.Value;
        }

        return await Task.FromResult(clone);
    }

    #endregion

    #region IObjectCatalogResource Implementation

    /// <inheritdoc />
    public uint Price { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<uint> Categories => _categories.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyDictionary<EnvironmentType, float> EnvironmentScores => _environmentScores.AsReadOnly();

    /// <inheritdoc />
    public ObjectPlacementRules PlacementRules { get; private set; } = new();

    /// <inheritdoc />
    public IReadOnlyList<TgiReference> Icons => _icons.AsReadOnly();

    /// <inheritdoc />
    public ObjectRigInfo? RigInfo { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ObjectSlotInfo> Slots => _slots.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<uint> Tags => _tags.AsReadOnly();

    /// <inheritdoc />
    public bool IsAvailableForPurchase { get; private set; } = true;

    /// <inheritdoc />
    public float DepreciationValue { get; private set; } = 0.8f;

    /// <inheritdoc />
    public uint DescriptionKey { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<uint, object> Properties => _properties.AsReadOnly();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectCatalogResource"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public ObjectCatalogResource(ILogger<ObjectCatalogResource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("Created new ObjectCatalogResource instance");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectCatalogResource"/> class with specific identifiers.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="groupId">The group identifier for this resource.</param>
    /// <param name="instanceId">The instance identifier for this resource.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public ObjectCatalogResource(ILogger<ObjectCatalogResource> logger, uint groupId, ulong instanceId)
        : this(logger)
    {
        GroupId = groupId;
        InstanceId = instanceId;
    }

    #endregion

    #region Private Loading Methods

    private async Task LoadFromReaderAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Read version
        Version = reader.ReadUInt32();
        _logger.LogDebug("Object catalog version: {Version}", Version);

        // Read catalog common block
        CommonBlock = await ReadCatalogCommonBlockAsync(reader, cancellationToken);

        // Read object-specific data based on version
        await ReadObjectDataAsync(reader, Version, cancellationToken);
    }

    private async Task<CatalogCommonBlock> ReadCatalogCommonBlockAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var commonBlock = new CatalogCommonBlock
        {
            FormatVersion = reader.ReadUInt32(),
            CompatibilityFlags = (CatalogCompatibilityFlags)reader.ReadInt32(),
            CategoryId = reader.ReadUInt32(),
            MetadataFlags = reader.ReadUInt32()
        };

        _logger.LogDebug("Read catalog common block - Format: {FormatVersion}, Category: {CategoryId:X8}",
            commonBlock.FormatVersion, commonBlock.CategoryId);

        return await Task.FromResult(commonBlock);
    }

    private async Task ReadObjectDataAsync(BinaryReader reader, uint version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Read price
        Price = reader.ReadUInt32();

        // Read categories
        var categoryCount = reader.ReadUInt32();
        _categories.Clear();
        for (var i = 0; i < categoryCount; i++)
        {
            _categories.Add(reader.ReadUInt32());
        }

        // Read environment scores
        var envCount = reader.ReadUInt32();
        _environmentScores.Clear();
        for (var i = 0; i < envCount; i++)
        {
            var envType = (EnvironmentType)reader.ReadInt32();
            var envValue = reader.ReadSingle();
            _environmentScores[envType] = envValue;
        }

        // Read placement rules
        PlacementRules = await ReadPlacementRulesAsync(reader, cancellationToken);

        // Read icons
        var iconCount = reader.ReadUInt32();
        _icons.Clear();
        for (var i = 0; i < iconCount; i++)
        {
            var typeId = reader.ReadUInt32();
            var groupId = reader.ReadUInt32();
            var instanceId = reader.ReadUInt64();
            _icons.Add(new TgiReference(typeId, groupId, instanceId));
        }

        // Read rig info (optional)
        var hasRigInfo = reader.ReadBoolean();
        if (hasRigInfo)
        {
            RigInfo = await ReadRigInfoAsync(reader, cancellationToken);
        }

        // Read slots
        var slotCount = reader.ReadUInt32();
        _slots.Clear();
        for (var i = 0; i < slotCount; i++)
        {
            _slots.Add(await ReadSlotInfoAsync(reader, cancellationToken));
        }

        // Read tags
        var tagCount = reader.ReadUInt32();
        _tags.Clear();
        for (var i = 0; i < tagCount; i++)
        {
            _tags.Add(reader.ReadUInt32());
        }

        // Read additional properties
        IsAvailableForPurchase = reader.ReadBoolean();
        DepreciationValue = reader.ReadSingle();
        DescriptionKey = reader.ReadUInt32();

        // Read extended properties
        await ReadExtendedPropertiesAsync(reader, version, cancellationToken);

        _logger.LogDebug("Read object data - Price: {Price}, Categories: {CategoryCount}, Icons: {IconCount}",
            Price, _categories.Count, _icons.Count);
    }

    private async Task<ObjectPlacementRules> ReadPlacementRulesAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rules = new ObjectPlacementRules
        {
            CanPlaceOnFloor = reader.ReadBoolean(),
            CanPlaceOnWall = reader.ReadBoolean(),
            CanPlaceOnSurface = reader.ReadBoolean(),
            RequiredClearance = reader.ReadSingle(),
            BlocksMovement = reader.ReadBoolean(),
            CanPlaceOutside = reader.ReadBoolean(),
            RequiresSpecificRoom = reader.ReadBoolean(),
            RoomTypeFlags = reader.ReadUInt32()
        };

        return await Task.FromResult(rules);
    }

    private async Task<ObjectRigInfo> ReadRigInfoAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typeId = reader.ReadUInt32();
        var groupId = reader.ReadUInt32();
        var instanceId = reader.ReadUInt64();

        var rigInfo = new ObjectRigInfo
        {
            RigReference = new TgiReference(typeId, groupId, instanceId),
            InteractionPointCount = reader.ReadUInt32(),
            AnimationFlags = reader.ReadUInt32(),
            RigComplexity = reader.ReadUInt32()
        };

        return await Task.FromResult(rigInfo);
    }

    private async Task<ObjectSlotInfo> ReadSlotInfoAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var slotInfo = new ObjectSlotInfo
        {
            SlotId = reader.ReadUInt32(),
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            AllowedObjectTypes = reader.ReadUInt32(),
            MaxObjectSize = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
        };

        return await Task.FromResult(slotInfo);
    }

    private async Task ReadExtendedPropertiesAsync(BinaryReader reader, uint version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var propertyCount = reader.ReadUInt32();
        _properties.Clear();

        for (var i = 0; i < propertyCount; i++)
        {
            var propertyId = reader.ReadUInt32();
            var propertyType = reader.ReadUInt32();

            object value = propertyType switch
            {
                1 => reader.ReadUInt32(), // UInt32
                2 => reader.ReadSingle(), // Float
                3 => reader.ReadBoolean(), // Boolean
                4 => reader.ReadString(), // String
                _ => reader.ReadBytes((int)reader.ReadUInt32()) // Raw data
            };

            _properties[propertyId] = value;
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Private Saving Methods

    private async Task SaveToWriterAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Write version
        writer.Write(Version);

        // Write catalog common block
        if (CommonBlock is not null)
        {
            await WriteCatalogCommonBlockAsync(writer, CommonBlock, cancellationToken);
        }

        // Write object-specific data
        await WriteObjectDataAsync(writer, Version, cancellationToken);
    }

    private async Task WriteCatalogCommonBlockAsync(BinaryWriter writer, CatalogCommonBlock commonBlock, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write(commonBlock.FormatVersion);
        writer.Write((int)commonBlock.CompatibilityFlags);
        writer.Write(commonBlock.CategoryId);
        writer.Write(commonBlock.MetadataFlags);

        await Task.CompletedTask;
    }

    private async Task WriteObjectDataAsync(BinaryWriter writer, uint version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Write price
        writer.Write(Price);

        // Write categories
        writer.Write((uint)_categories.Count);
        foreach (var category in _categories)
        {
            writer.Write(category);
        }

        // Write environment scores
        writer.Write((uint)_environmentScores.Count);
        foreach (var kvp in _environmentScores)
        {
            writer.Write((int)kvp.Key);
            writer.Write(kvp.Value);
        }

        // Write placement rules
        await WritePlacementRulesAsync(writer, PlacementRules, cancellationToken);

        // Write icons
        writer.Write((uint)_icons.Count);
        foreach (var icon in _icons)
        {
            writer.Write(icon.TypeId);
            writer.Write(icon.GroupId);
            writer.Write(icon.InstanceId);
        }

        // Write rig info
        writer.Write(RigInfo is not null);
        if (RigInfo is not null)
        {
            await WriteRigInfoAsync(writer, RigInfo, cancellationToken);
        }

        // Write slots
        writer.Write((uint)_slots.Count);
        foreach (var slot in _slots)
        {
            await WriteSlotInfoAsync(writer, slot, cancellationToken);
        }

        // Write tags
        writer.Write((uint)_tags.Count);
        foreach (var tag in _tags)
        {
            writer.Write(tag);
        }

        // Write additional properties
        writer.Write(IsAvailableForPurchase);
        writer.Write(DepreciationValue);
        writer.Write(DescriptionKey);

        // Write extended properties
        await WriteExtendedPropertiesAsync(writer, version, cancellationToken);
    }

    private async Task WritePlacementRulesAsync(BinaryWriter writer, ObjectPlacementRules rules, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write(rules.CanPlaceOnFloor);
        writer.Write(rules.CanPlaceOnWall);
        writer.Write(rules.CanPlaceOnSurface);
        writer.Write(rules.RequiredClearance);
        writer.Write(rules.BlocksMovement);
        writer.Write(rules.CanPlaceOutside);
        writer.Write(rules.RequiresSpecificRoom);
        writer.Write(rules.RoomTypeFlags);

        await Task.CompletedTask;
    }

    private async Task WriteRigInfoAsync(BinaryWriter writer, ObjectRigInfo rigInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write(rigInfo.RigReference.TypeId);
        writer.Write(rigInfo.RigReference.GroupId);
        writer.Write(rigInfo.RigReference.InstanceId);
        writer.Write(rigInfo.InteractionPointCount);
        writer.Write(rigInfo.AnimationFlags);
        writer.Write(rigInfo.RigComplexity);

        await Task.CompletedTask;
    }

    private async Task WriteSlotInfoAsync(BinaryWriter writer, ObjectSlotInfo slotInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write(slotInfo.SlotId);
        writer.Write(slotInfo.Position.X);
        writer.Write(slotInfo.Position.Y);
        writer.Write(slotInfo.Position.Z);
        writer.Write(slotInfo.Rotation.X);
        writer.Write(slotInfo.Rotation.Y);
        writer.Write(slotInfo.Rotation.Z);
        writer.Write(slotInfo.AllowedObjectTypes);
        writer.Write(slotInfo.MaxObjectSize.X);
        writer.Write(slotInfo.MaxObjectSize.Y);
        writer.Write(slotInfo.MaxObjectSize.Z);

        await Task.CompletedTask;
    }

    private async Task WriteExtendedPropertiesAsync(BinaryWriter writer, uint version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.Write((uint)_properties.Count);

        foreach (var kvp in _properties)
        {
            writer.Write(kvp.Key);

            // Determine property type and write accordingly
            switch (kvp.Value)
            {
                case uint uintValue:
                    writer.Write(1u); // UInt32 type
                    writer.Write(uintValue);
                    break;
                case float floatValue:
                    writer.Write(2u); // Float type
                    writer.Write(floatValue);
                    break;
                case bool boolValue:
                    writer.Write(3u); // Boolean type
                    writer.Write(boolValue);
                    break;
                case string stringValue:
                    writer.Write(4u); // String type
                    writer.Write(stringValue);
                    break;
                case byte[] byteArray:
                    writer.Write(0u); // Raw data type
                    writer.Write((uint)byteArray.Length);
                    writer.Write(byteArray);
                    break;
                default:
                    // Serialize as string representation
                    writer.Write(4u); // String type
                    writer.Write(kvp.Value.ToString() ?? string.Empty);
                    break;
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            Stream?.Dispose();

            _properties.Clear();
            _environmentScores.Clear();
            _categories.Clear();
            _icons.Clear();
            _slots.Clear();
            _tags.Clear();

            _disposed = true;
            _logger.LogDebug("ObjectCatalogResource disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ObjectCatalogResource disposal");
        }
    }

    #endregion

    #region IAsyncDisposable Implementation

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (Stream is not null)
            {
                await Stream.DisposeAsync();
            }

            _properties.Clear();
            _environmentScores.Clear();
            _categories.Clear();
            _icons.Clear();
            _slots.Clear();
            _tags.Clear();

            _disposed = true;
            _logger.LogDebug("ObjectCatalogResource disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ObjectCatalogResource disposal");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a category to this object's category list.
    /// </summary>
    /// <param name="categoryId">The category identifier to add.</param>
    public void AddCategory(uint categoryId)
    {
        if (!_categories.Contains(categoryId))
        {
            _categories.Add(categoryId);
            MarkDirty();
            _logger.LogDebug("Added category {CategoryId:X8} to object catalog resource", categoryId);
        }
    }

    /// <summary>
    /// Removes a category from this object's category list.
    /// </summary>
    /// <param name="categoryId">The category identifier to remove.</param>
    /// <returns>True if the category was removed, false if it wasn't found.</returns>
    public bool RemoveCategory(uint categoryId)
    {
        if (_categories.Remove(categoryId))
        {
            MarkDirty();
            _logger.LogDebug("Removed category {CategoryId:X8} from object catalog resource", categoryId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets the environment impact score for a specific environment type.
    /// </summary>
    /// <param name="environmentType">The environment type to set.</param>
    /// <param name="score">The environment impact score (-10 to +10).</param>
    public void SetEnvironmentScore(EnvironmentType environmentType, float score)
    {
        _environmentScores[environmentType] = Math.Clamp(score, -10f, 10f);
        MarkDirty();
        _logger.LogDebug("Set environment score {Type} = {Score} for object catalog resource", environmentType, score);
    }

    /// <summary>
    /// Adds an icon reference to this object.
    /// </summary>
    /// <param name="iconReference">The TGI reference for the icon.</param>
    public void AddIcon(TgiReference iconReference)
    {
        if (!_icons.Contains(iconReference))
        {
            _icons.Add(iconReference);
            MarkDirty();
            _logger.LogDebug("Added icon reference {IconRef} to object catalog resource", iconReference);
        }
    }

    /// <summary>
    /// Adds a tag to this object.
    /// </summary>
    /// <param name="tagId">The tag identifier to add.</param>
    public void AddTag(uint tagId)
    {
        if (!_tags.Contains(tagId))
        {
            _tags.Add(tagId);
            MarkDirty();
            _logger.LogDebug("Added tag {TagId:X8} to object catalog resource", tagId);
        }
    }

    #endregion
}
