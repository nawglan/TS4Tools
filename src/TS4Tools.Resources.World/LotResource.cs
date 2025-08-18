using System.Runtime.InteropServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a lot resource that defines lot placement and configuration in The Sims 4 package files.
/// Lot resources contain information about lot size, price, position, and associated world data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class LotResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this lot.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the lot resource version.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets or sets the world description instance ID this lot belongs to.
    /// </summary>
    public ulong WorldDescriptionInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the unique lot identifier.
    /// </summary>
    public uint LotId { get; set; }

    /// <summary>
    /// Gets or sets the price of the lot in Simoleons.
    /// </summary>
    public uint SimoleonPrice { get; set; }

    /// <summary>
    /// Gets or sets the lot size in the X direction.
    /// </summary>
    public sbyte LotSizeX { get; set; }

    /// <summary>
    /// Gets or sets the lot size in the Z direction.
    /// </summary>
    public sbyte LotSizeZ { get; set; }

    /// <summary>
    /// Gets or sets whether the lot is editable.
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Gets or sets the ambience file instance ID.
    /// </summary>
    public ulong AmbienceFileInstanceId { get; set; }

    /// <summary>
    /// Gets or sets whether the lot is enabled for auto testing.
    /// </summary>
    public bool EnabledForAutoTest { get; set; }

    /// <summary>
    /// Gets or sets whether the lot has override ambience.
    /// </summary>
    public bool HasOverrideAmbience { get; set; }

    /// <summary>
    /// Gets or sets the audio effect file instance ID.
    /// </summary>
    public ulong AudioEffectFileInstanceId { get; set; }

    /// <summary>
    /// Gets or sets whether build/buy mode is disabled for this lot.
    /// </summary>
    public bool DisableBuildBuy { get; set; }

    /// <summary>
    /// Gets or sets whether the lot is hidden from the lot picker.
    /// </summary>
    public bool HideFromLotPicker { get; set; }

    /// <summary>
    /// Gets or sets the building name key for localized lot names.
    /// </summary>
    public uint BuildingNameKey { get; set; }

    /// <summary>
    /// Gets or sets the camera position for lot view.
    /// </summary>
    public LotPosition CameraPosition { get; set; }

    /// <summary>
    /// Gets or sets the camera target position for lot view.
    /// </summary>
    public LotPosition CameraTarget { get; set; }

    /// <summary>
    /// Gets or sets the lot requirements venue identifier.
    /// </summary>
    public ulong LotRequirementsVenue { get; set; }

    /// <summary>
    /// Gets or sets the lot position in world coordinates.
    /// </summary>
    public LotPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the lot rotation in degrees.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets whether the resource has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => _isDirty = value;
    }

    /// <summary>
    /// Initializes a new instance of the LotResource class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The lot version.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public LotResource(ResourceKey key, uint version = 9)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;

        // Initialize new fields with default values
        BuildingNameKey = 0;
        CameraPosition = LotPosition.Origin;
        CameraTarget = LotPosition.Origin;
        LotRequirementsVenue = 0;
        Position = LotPosition.Origin;
        Rotation = 0f;
    }

    /// <summary>
    /// Loads lot data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing lot data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid lot data.</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Handle null or truly empty stream (no content at all)
        if (stream == null || stream.Length == 0)
        {
            // Initialize with default values for empty lot
            WorldDescriptionInstanceId = 0;
            LotId = 0;
            SimoleonPrice = 0;
            LotSizeX = 64;
            LotSizeZ = 64;
            IsEditable = true;
            AmbienceFileInstanceId = 0;
            EnabledForAutoTest = false;
            HasOverrideAmbience = false;
            AudioEffectFileInstanceId = 0;
            DisableBuildBuy = false;
            HideFromLotPicker = false;
            BuildingNameKey = 0;
            CameraPosition = LotPosition.Origin;
            CameraTarget = LotPosition.Origin;
            LotRequirementsVenue = 0;
            IsDirty = true;
            return;
        }

        try
        {
            using var reader = new BinaryReader(stream);

            await Task.Run(() =>
            {
                // Read lot data based on version
                var version = reader.ReadUInt32();
                if (version < 9)
                    throw new InvalidDataException($"Unsupported lot version: {version}");

                WorldDescriptionInstanceId = reader.ReadUInt64();
                LotId = reader.ReadUInt32();
                SimoleonPrice = reader.ReadUInt32();
                LotSizeX = reader.ReadSByte();
                LotSizeZ = reader.ReadSByte();
                IsEditable = reader.ReadSByte() != 0;

                AmbienceFileInstanceId = reader.ReadUInt64();
                EnabledForAutoTest = reader.ReadByte() != 0;

                HasOverrideAmbience = reader.ReadByte() != 0;
                AudioEffectFileInstanceId = reader.ReadUInt64();

                DisableBuildBuy = reader.ReadByte() != 0;
                HideFromLotPicker = reader.ReadByte() != 0;

                // Read building name key
                BuildingNameKey = reader.ReadUInt32();

                // Read camera position (3 floats: x, y, z)
                var camPosX = reader.ReadSingle();
                var camPosY = reader.ReadSingle();
                var camPosZ = reader.ReadSingle();
                CameraPosition = new LotPosition(camPosX, camPosY, camPosZ);

                // Read camera target (3 floats: x, y, z)
                var camTargetX = reader.ReadSingle();
                var camTargetY = reader.ReadSingle();
                var camTargetZ = reader.ReadSingle();
                CameraTarget = new LotPosition(camTargetX, camTargetY, camTargetZ);

                // Read lot requirements venue
                LotRequirementsVenue = reader.ReadUInt64();

            }, cancellationToken);

            _isDirty = false;
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("Failed to load lot data from stream", ex);
        }
    }

    /// <summary>
    /// Saves lot data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write lot data to.</param>
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
            writer.Write(WorldDescriptionInstanceId);
            writer.Write(LotId);
            writer.Write(SimoleonPrice);
            writer.Write(LotSizeX);
            writer.Write(LotSizeZ);
            writer.Write(IsEditable ? (sbyte)1 : (sbyte)0);

            writer.Write(AmbienceFileInstanceId);
            writer.Write(EnabledForAutoTest ? (byte)1 : (byte)0);

            writer.Write(HasOverrideAmbience ? (byte)1 : (byte)0);
            writer.Write(AudioEffectFileInstanceId);

            writer.Write(DisableBuildBuy ? (byte)1 : (byte)0);
            writer.Write(HideFromLotPicker ? (byte)1 : (byte)0);

            // Write building name key
            writer.Write(BuildingNameKey);

            // Write camera position (3 floats: x, y, z)
            writer.Write(CameraPosition.X);
            writer.Write(CameraPosition.Y);
            writer.Write(CameraPosition.Z);

            // Write camera target (3 floats: x, y, z)
            writer.Write(CameraTarget.X);
            writer.Write(CameraTarget.Y);
            writer.Write(CameraTarget.Z);

            // Write lot requirements venue
            writer.Write(LotRequirementsVenue);

        }, cancellationToken);

        _isDirty = false;
    }

    /// <summary>
    /// Calculates the lot area in square units.
    /// </summary>
    /// <returns>The lot area.</returns>
    public int CalculateArea()
    {
        return Math.Abs(LotSizeX) * Math.Abs(LotSizeZ);
    }

    /// <summary>
    /// Validates the lot configuration.
    /// </summary>
    /// <returns>A list of validation errors, empty if valid.</returns>
    public IReadOnlyCollection<string> Validate()
    {
        var errors = new List<string>();

        if (LotSizeX <= 0)
            errors.Add("Lot size X must be positive");

        if (LotSizeZ <= 0)
            errors.Add("Lot size Z must be positive");

        if (LotId == 0)
            errors.Add("Lot ID cannot be zero");

        if (WorldDescriptionInstanceId == 0)
            errors.Add("World description instance ID cannot be zero");

        return errors;
    }

    /// <summary>
    /// Disposes of resources used by the LotResource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a string representation of the lot resource.
    /// </summary>
    /// <returns>A string representation of the lot resource.</returns>
    public override string ToString()
    {
        return $"LotResource {LotId} ({LotSizeX}x{LotSizeZ}) - §{SimoleonPrice:N0}";
    }

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            var stream = new MemoryStream();
            Task.Run(async () => await SaveToStreamAsync(stream).ConfigureAwait(false)).GetAwaiter().GetResult();
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
            Task.Run(async () => await SaveToStreamAsync(stream).ConfigureAwait(false)).GetAwaiter().GetResult();
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
        nameof(LotId),
        nameof(SimoleonPrice),
        nameof(LotSizeX),
        nameof(LotSizeZ),
        nameof(IsEditable),
        nameof(AmbienceFileInstanceId),
        nameof(EnabledForAutoTest),
        nameof(HasOverrideAmbience),
        nameof(AudioEffectFileInstanceId),
        nameof(DisableBuildBuy),
        nameof(HideFromLotPicker),
        nameof(BuildingNameKey),
        nameof(CameraPosition),
        nameof(CameraTarget),
        nameof(LotRequirementsVenue)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(LotId) => new TypedValue(typeof(uint), LotId),
            nameof(SimoleonPrice) => new TypedValue(typeof(uint), SimoleonPrice),
            nameof(LotSizeX) => new TypedValue(typeof(sbyte), LotSizeX),
            nameof(LotSizeZ) => new TypedValue(typeof(sbyte), LotSizeZ),
            nameof(IsEditable) => new TypedValue(typeof(bool), IsEditable),
            nameof(AmbienceFileInstanceId) => new TypedValue(typeof(ulong), AmbienceFileInstanceId),
            nameof(EnabledForAutoTest) => new TypedValue(typeof(bool), EnabledForAutoTest),
            nameof(HasOverrideAmbience) => new TypedValue(typeof(bool), HasOverrideAmbience),
            nameof(AudioEffectFileInstanceId) => new TypedValue(typeof(ulong), AudioEffectFileInstanceId),
            nameof(DisableBuildBuy) => new TypedValue(typeof(bool), DisableBuildBuy),
            nameof(HideFromLotPicker) => new TypedValue(typeof(bool), HideFromLotPicker),
            nameof(BuildingNameKey) => new TypedValue(typeof(uint), BuildingNameKey),
            nameof(CameraPosition) => new TypedValue(typeof(LotPosition), CameraPosition),
            nameof(CameraTarget) => new TypedValue(typeof(LotPosition), CameraTarget),
            nameof(LotRequirementsVenue) => new TypedValue(typeof(ulong), LotRequirementsVenue),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Lot resource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(LotId)],
            1 => this[nameof(SimoleonPrice)],
            2 => this[nameof(LotSizeX)],
            3 => this[nameof(LotSizeZ)],
            4 => this[nameof(IsEditable)],
            5 => this[nameof(AmbienceFileInstanceId)],
            6 => this[nameof(EnabledForAutoTest)],
            7 => this[nameof(HasOverrideAmbience)],
            8 => this[nameof(AudioEffectFileInstanceId)],
            9 => this[nameof(DisableBuildBuy)],
            10 => this[nameof(HideFromLotPicker)],
            11 => this[nameof(BuildingNameKey)],
            12 => this[nameof(CameraPosition)],
            13 => this[nameof(CameraTarget)],
            14 => this[nameof(LotRequirementsVenue)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-14, got {index}")
        };
        set => throw new NotSupportedException("Lot resource fields are read-only via integer indexer");
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
/// Represents a lot position in world coordinates.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public readonly record struct LotPosition(float X, float Y, float Z)
{
    /// <summary>
    /// Gets the origin position (0, 0, 0).
    /// </summary>
    public static LotPosition Origin => new(0f, 0f, 0f);

    /// <summary>
    /// Calculates the distance from this position to another position.
    /// </summary>
    /// <param name="other">The other position.</param>
    /// <returns>The distance between the positions.</returns>
    public float DistanceTo(LotPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Returns a string representation of the lot position.
    /// </summary>
    /// <returns>A string representation of the lot position.</returns>
    public override string ToString()
    {
        return $"({X:F2}, {Y:F2}, {Z:F2})";
    }
}
