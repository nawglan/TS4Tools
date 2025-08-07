using System.Runtime.InteropServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a neighborhood resource that contains neighborhood and world metadata in The Sims 4 package files.
/// Neighborhood resources define regional information, world descriptions, and neighborhood-specific configurations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class NeighborhoodResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private readonly List<NeighborhoodLot> _lots;
    private readonly Dictionary<uint, string> _localizedNames;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this neighborhood.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the neighborhood resource version.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets or sets the world name key for localization.
    /// </summary>
    public uint WorldNameKey { get; set; }

    /// <summary>
    /// Gets or sets the world description key for localization.
    /// </summary>
    public uint WorldDescriptionKey { get; set; }

    /// <summary>
    /// Gets or sets the price in Simoleons for this world.
    /// </summary>
    public uint SimoleonPrice { get; set; }

    /// <summary>
    /// Gets or sets the region description instance ID this neighborhood belongs to.
    /// </summary>
    public ulong RegionDescriptionInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the world name.
    /// </summary>
    public string WorldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ambience file instance ID.
    /// </summary>
    public ulong AmbienceFileInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the public space aural material identifier.
    /// </summary>
    public uint PublicSpaceAuralMaterial { get; set; }

    /// <summary>
    /// Gets or sets whether time override is enabled.
    /// </summary>
    public bool EnableTimeOverride { get; set; }

    /// <summary>
    /// Gets or sets the hour for time override (0-23).
    /// </summary>
    public byte Hour { get; set; }

    /// <summary>
    /// Gets or sets the minute for time override (0-59).
    /// </summary>
    public byte Minute { get; set; }

    /// <summary>
    /// Gets or sets the HSV tweaker file instance ID.
    /// </summary>
    public ulong HSVTweakerFileInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the climate type.
    /// </summary>
    public ClimateType Climate { get; set; }

    /// <summary>
    /// Gets or sets the terrain type.
    /// </summary>
    public TerrainType Terrain { get; set; }

    /// <summary>
    /// Gets the collection of lots in this neighborhood.
    /// </summary>
    public IReadOnlyList<NeighborhoodLot> Lots => _lots.AsReadOnly();

    /// <summary>
    /// Gets the collection of localized names.
    /// </summary>
    public IReadOnlyDictionary<uint, string> LocalizedNames => _localizedNames.AsReadOnly();

    /// <summary>
    /// Gets or sets whether the resource has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => _isDirty = value;
    }

    /// <summary>
    /// Initializes a new instance of the NeighborhoodResource class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The neighborhood version.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public NeighborhoodResource(ResourceKey key, uint version = 1)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        _lots = new List<NeighborhoodLot>();
        _localizedNames = new Dictionary<uint, string>();
    }

    /// <summary>
    /// Loads neighborhood data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing neighborhood data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid neighborhood data.</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream);

            await Task.Run(() =>
            {
                var version = reader.ReadUInt32();
                if (version == 0)
                    throw new InvalidDataException("Invalid neighborhood version");

                WorldNameKey = reader.ReadUInt32();
                WorldDescriptionKey = reader.ReadUInt32();
                SimoleonPrice = reader.ReadUInt32();
                RegionDescriptionInstanceId = reader.ReadUInt64();

                // Read world name
                var nameLength = reader.ReadInt32();
                if (nameLength > 0)
                {
                    var nameBytes = reader.ReadBytes(nameLength);
                    WorldName = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                }

                AmbienceFileInstanceId = reader.ReadUInt64();
                PublicSpaceAuralMaterial = reader.ReadUInt32();

                EnableTimeOverride = reader.ReadByte() != 0;
                Hour = reader.ReadByte();
                Minute = reader.ReadByte();

                HSVTweakerFileInstanceId = reader.ReadUInt64();

                Climate = (ClimateType)reader.ReadByte();
                Terrain = (TerrainType)reader.ReadByte();

                // Read lots
                var lotCount = reader.ReadUInt32();
                for (uint i = 0; i < lotCount && !cancellationToken.IsCancellationRequested; i++)
                {
                    var lot = new NeighborhoodLot
                    {
                        LotId = reader.ReadUInt32(),
                        InstanceId = reader.ReadUInt64(),
                        X = reader.ReadSingle(),
                        Y = reader.ReadSingle(),
                        Z = reader.ReadSingle(),
                        Rotation = reader.ReadSingle()
                    };
                    _lots.Add(lot);
                }

                // Read localized names
                var nameCount = reader.ReadUInt32();
                for (uint i = 0; i < nameCount && !cancellationToken.IsCancellationRequested; i++)
                {
                    var key = reader.ReadUInt32();
                    var length = reader.ReadInt32();
                    var value = string.Empty;

                    if (length > 0)
                    {
                        var bytes = reader.ReadBytes(length);
                        value = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    }

                    _localizedNames[key] = value;
                }

            }, cancellationToken);

            _isDirty = false;
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("Failed to load neighborhood data from stream", ex);
        }
    }

    /// <summary>
    /// Saves neighborhood data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write neighborhood data to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new BinaryWriter(stream);

        await Task.Run(() =>
        {
            writer.Write(Version);
            writer.Write(WorldNameKey);
            writer.Write(WorldDescriptionKey);
            writer.Write(SimoleonPrice);
            writer.Write(RegionDescriptionInstanceId);

            // Write world name
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(WorldName ?? string.Empty);
            writer.Write(nameBytes.Length);
            if (nameBytes.Length > 0)
            {
                writer.Write(nameBytes);
            }

            writer.Write(AmbienceFileInstanceId);
            writer.Write(PublicSpaceAuralMaterial);

            writer.Write(EnableTimeOverride ? (byte)1 : (byte)0);
            writer.Write(Hour);
            writer.Write(Minute);

            writer.Write(HSVTweakerFileInstanceId);

            writer.Write((byte)Climate);
            writer.Write((byte)Terrain);

            // Write lots
            writer.Write((uint)_lots.Count);
            foreach (var lot in _lots)
            {
                writer.Write(lot.LotId);
                writer.Write(lot.InstanceId);
                writer.Write(lot.X);
                writer.Write(lot.Y);
                writer.Write(lot.Z);
                writer.Write(lot.Rotation);
            }

            // Write localized names
            writer.Write((uint)_localizedNames.Count);
            foreach (var kvp in _localizedNames)
            {
                writer.Write(kvp.Key);
                var bytes = System.Text.Encoding.UTF8.GetBytes(kvp.Value ?? string.Empty);
                writer.Write(bytes.Length);
                if (bytes.Length > 0)
                {
                    writer.Write(bytes);
                }
            }

        }, cancellationToken);

        _isDirty = false;
    }

    /// <summary>
    /// Adds a lot to the neighborhood.
    /// </summary>
    /// <param name="lot">The lot to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when lot is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a lot with the same ID already exists.</exception>
    public void AddLot(NeighborhoodLot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);

        if (_lots.Any(l => l.LotId == lot.LotId))
            throw new ArgumentException($"Lot with ID {lot.LotId} already exists", nameof(lot));

        _lots.Add(lot);
        _isDirty = true;
    }

    /// <summary>
    /// Removes a lot from the neighborhood.
    /// </summary>
    /// <param name="lotId">The ID of the lot to remove.</param>
    /// <returns>true if the lot was removed; otherwise, false.</returns>
    public bool RemoveLot(uint lotId)
    {
        var lot = _lots.FirstOrDefault(l => l.LotId == lotId);
        if (lot == null)
            return false;

        var removed = _lots.Remove(lot);
        if (removed)
            _isDirty = true;

        return removed;
    }

    /// <summary>
    /// Sets a localized name for the specified key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="value">The localized value.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public void SetLocalizedName(uint key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _localizedNames[key] = value;
        _isDirty = true;
    }

    /// <summary>
    /// Removes a localized name.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <returns>true if the name was removed; otherwise, false.</returns>
    public bool RemoveLocalizedName(uint key)
    {
        var removed = _localizedNames.Remove(key);
        if (removed)
            _isDirty = true;

        return removed;
    }

    /// <summary>
    /// Validates the time override settings.
    /// </summary>
    /// <returns>true if the time settings are valid; otherwise, false.</returns>
    public bool IsValidTimeOverride()
    {
        return Hour < 24 && Minute < 60;
    }

    /// <summary>
    /// Disposes of resources used by the NeighborhoodResource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _lots.Clear();
        _localizedNames.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a string representation of the neighborhood resource.
    /// </summary>
    /// <returns>A string representation of the neighborhood resource.</returns>
    public override string ToString()
    {
        return $"NeighborhoodResource: {WorldName} (Lots: {_lots.Count}, Price: §{SimoleonPrice:N0})";
    }

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            var stream = new MemoryStream();
            SaveToStreamAsync(stream).GetAwaiter().GetResult();
            stream.Position = 0;
            return stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            using var stream = new MemoryStream();
            SaveToStreamAsync(stream).GetAwaiter().GetResult();
            return stream.ToArray();
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public int RequestedApiVersion => 1;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(WorldName),
        nameof(WorldNameKey),
        nameof(SimoleonPrice),
        nameof(Climate),
        nameof(Terrain),
        nameof(RegionDescriptionInstanceId),
        nameof(Lots)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(WorldName) => new TypedValue(typeof(string), WorldName),
            nameof(WorldNameKey) => new TypedValue(typeof(uint), WorldNameKey),
            nameof(SimoleonPrice) => new TypedValue(typeof(uint), SimoleonPrice),
            nameof(Climate) => new TypedValue(typeof(ClimateType), Climate),
            nameof(Terrain) => new TypedValue(typeof(TerrainType), Terrain),
            nameof(RegionDescriptionInstanceId) => new TypedValue(typeof(ulong), RegionDescriptionInstanceId),
            nameof(Lots) => new TypedValue(typeof(IReadOnlyList<NeighborhoodLot>), Lots),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Neighborhood resource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(WorldName)],
            1 => this[nameof(WorldNameKey)],
            2 => this[nameof(SimoleonPrice)],
            3 => this[nameof(Climate)],
            4 => this[nameof(Terrain)],
            5 => this[nameof(RegionDescriptionInstanceId)],
            6 => this[nameof(Lots)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-6, got {index}")
        };
        set => throw new NotSupportedException("Neighborhood resource fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents a lot within a neighborhood.
/// </summary>
public sealed class NeighborhoodLot
{
    /// <summary>
    /// Gets or sets the lot identifier.
    /// </summary>
    public uint LotId { get; set; }

    /// <summary>
    /// Gets or sets the lot instance identifier.
    /// </summary>
    public ulong InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Gets or sets the rotation in degrees.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets the position as a Vector3.
    /// </summary>
    public Vector3 Position => new(X, Y, Z);

    /// <summary>
    /// Returns a string representation of the neighborhood lot.
    /// </summary>
    /// <returns>A string representation of the neighborhood lot.</returns>
    public override string ToString()
    {
        return $"NeighborhoodLot {LotId} at ({X:F2}, {Y:F2}, {Z:F2})";
    }
}

/// <summary>
/// Represents climate types for neighborhoods.
/// </summary>
public enum ClimateType : int
{
    /// <summary>
    /// Temperate climate.
    /// </summary>
    Temperate = 0,

    /// <summary>
    /// Desert climate.
    /// </summary>
    Desert = 1,

    /// <summary>
    /// Tropical climate.
    /// </summary>
    Tropical = 2,

    /// <summary>
    /// Arctic climate.
    /// </summary>
    Arctic = 3,

    /// <summary>
    /// Mediterranean climate.
    /// </summary>
    Mediterranean = 4
}

/// <summary>
/// Represents terrain types for neighborhoods.
/// </summary>
public enum TerrainType : int
{
    /// <summary>
    /// Flat terrain.
    /// </summary>
    Flat = 0,

    /// <summary>
    /// Hilly terrain.
    /// </summary>
    Hilly = 1,

    /// <summary>
    /// Mountainous terrain.
    /// </summary>
    Mountainous = 2,

    /// <summary>
    /// Mountain terrain.
    /// </summary>
    Mountain = 2,

    /// <summary>
    /// Coastal terrain.
    /// </summary>
    Coastal = 3,

    /// <summary>
    /// Island terrain.
    /// </summary>
    Island = 4,

    /// <summary>
    /// Valley terrain.
    /// </summary>
    Valley = 5,

    /// <summary>
    /// Desert terrain.
    /// </summary>
    Desert = 6,

    /// <summary>
    /// Grassland terrain.
    /// </summary>
    Grassland = 7,

    /// <summary>
    /// Forest terrain.
    /// </summary>
    Forest = 8,

    /// <summary>
    /// Beach terrain.
    /// </summary>
    Beach = 9,

    /// <summary>
    /// Urban terrain.
    /// </summary>
    Urban = 10
}
