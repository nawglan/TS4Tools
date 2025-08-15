using System.ComponentModel;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a world building resource in The Sims 4.
/// </summary>
public sealed class WorldResource : IResource, IDisposable
{
    private readonly List<ObjectManager> _objectManagers = new();
    private readonly List<SceneObject> _sceneObjects = new();
    private bool _isDisposed;
    private MemoryStream? _stream;

    /// <summary>
    /// Initializes a new instance of the WorldResource class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    public WorldResource(ResourceKey key, uint version)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        IsDirty = true;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public ResourceKey Key { get; }

    /// <summary>
    /// Gets the resource version.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource is editable.
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the resource has been modified.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Gets the list of object managers.
    /// </summary>
    public IReadOnlyList<ObjectManager> ObjectManagers => _objectManagers;

    /// <summary>
    /// Gets the list of scene objects.
    /// </summary>
    public IReadOnlyList<SceneObject> SceneObjects => _sceneObjects;

    /// <summary>
    /// Gets the number of object managers.
    /// </summary>
    public int ObjectManagerCount => _objectManagers.Count;

    /// <summary>
    /// Gets the number of scene objects.
    /// </summary>
    public int SceneObjectCount => _sceneObjects.Count;

    /// <summary>
    /// Gets a value indicating whether the world has any object managers.
    /// </summary>
    public bool HasObjectManagers => _objectManagers.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the world has any scene objects.
    /// </summary>
    public bool HasSceneObjects => _sceneObjects.Count > 0;

    /// <summary>
    /// Gets the total number of objects in the world.
    /// </summary>
    public int TotalObjectCount => _objectManagers.Count + _sceneObjects.Count;

    /// <summary>
    /// Gets a value indicating whether the world is empty.
    /// </summary>
    public bool IsEmpty => _objectManagers.Count == 0 && _sceneObjects.Count == 0;

    /// <summary>
    /// Gets the approximate resource size in bytes.
    /// </summary>
    public long ResourceSize
    {
        get
        {
            try
            {
                using var stream = new MemoryStream();
                SaveToStreamAsync(stream).GetAwaiter().GetResult();
                return stream.Length;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Gets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the approximate memory usage in bytes.
    /// </summary>
    public long MemoryUsage => 
        (_objectManagers.Count * 24) + // Rough estimate for ObjectManager
        (_sceneObjects.Count * 32) +   // Rough estimate for SceneObject
        64; // Base object overhead

    /// <summary>
    /// Gets the complexity level of the resource based on object count.
    /// </summary>
    public string ResourceComplexity => TotalObjectCount switch
    {
        0 => "Empty",
        < 10 => "Simple",
        < 100 => "Moderate",
        < 500 => "Complex",
        _ => "Very Complex"
    };

    /// <summary>
    /// Adds an object manager to the world.
    /// </summary>
    /// <param name="manager">The object manager to add.</param>
    public void AddObjectManager(ObjectManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _objectManagers.Add(manager);
        IsDirty = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes an object manager from the world.
    /// </summary>
    /// <param name="manager">The object manager to remove.</param>
    /// <returns>True if the manager was removed; otherwise, false.</returns>
    public bool RemoveObjectManager(ObjectManager manager)
    {
        var removed = _objectManagers.Remove(manager);
        if (removed)
        {
            IsDirty = true;
            OnResourceChanged();
        }
        return removed;
    }

    /// <summary>
    /// Adds a scene object to the world.
    /// </summary>
    /// <param name="sceneObject">The scene object to add.</param>
    public void AddSceneObject(SceneObject sceneObject)
    {
        ArgumentNullException.ThrowIfNull(sceneObject);
        _sceneObjects.Add(sceneObject);
        IsDirty = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a scene object from the world.
    /// </summary>
    /// <param name="sceneObject">The scene object to remove.</param>
    /// <returns>True if the object was removed; otherwise, false.</returns>
    public bool RemoveSceneObject(SceneObject sceneObject)
    {
        var removed = _sceneObjects.Remove(sceneObject);
        if (removed)
        {
            IsDirty = true;
            OnResourceChanged();
        }
        return removed;
    }

    /// <summary>
    /// Loads the resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadFromStreamAsync(Stream stream)
    {
        // Handle null or truly empty stream (no content at all)
        if (stream == null || stream.Length == 0)
        {
            // Initialize with default values for empty resource
            _objectManagers.Clear();
            _sceneObjects.Clear();
            IsDirty = true;
            OnResourceChanged();
            return;
        }

        using var reader = new BinaryReader(stream);

        // Read header
        var version = reader.ReadUInt32();

        // Read object managers
        var objectManagerCount = reader.ReadUInt32();
        _objectManagers.Clear();
        for (uint i = 0; i < objectManagerCount; i++)
        {
            var managerId = reader.ReadUInt64();
            var managerName = reader.ReadString();
            var manager = new ObjectManager(managerId, managerName);
            _objectManagers.Add(manager);
        }

        // Read scene objects
        var sceneObjectCount = reader.ReadUInt32();
        _sceneObjects.Clear();
        for (uint i = 0; i < sceneObjectCount; i++)
        {
            var objectId = reader.ReadUInt64();
            var objectName = reader.ReadString();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var rotation = reader.ReadSingle();

            var sceneObject = new SceneObject(objectId, objectName)
            {
                Position = new Vector3(x, y, z),
                Rotation = rotation
            };
            _sceneObjects.Add(sceneObject);
        }

        IsDirty = false;
        OnResourceChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Saves the resource to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveToStreamAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(Version);

        // Write object managers
        writer.Write((uint)_objectManagers.Count);
        foreach (var manager in _objectManagers)
        {
            writer.Write(manager.Id);
            writer.Write(manager.Name);
        }

        // Write scene objects
        writer.Write((uint)_sceneObjects.Count);
        foreach (var obj in _sceneObjects)
        {
            writer.Write(obj.Id);
            writer.Write(obj.Name);
            writer.Write(obj.Position.X);
            writer.Write(obj.Position.Y);
            writer.Write(obj.Position.Z);
            writer.Write(obj.Rotation);
        }

        IsDirty = false;
        OnResourceChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Returns a string representation of the resource.
    /// </summary>
    /// <returns>A string representation of the resource.</returns>
    public override string ToString()
    {
        return $"WorldResource v{Version} ({_objectManagers.Count} managers, {_sceneObjects.Count} objects)";
    }

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_stream == null)
            {
                _stream = new MemoryStream();
                SaveToStreamAsync(_stream).GetAwaiter().GetResult();
                _stream.Position = 0;
            }
            return _stream;
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
        nameof(Version),
        nameof(IsEditable),
        nameof(ObjectManagers),
        nameof(SceneObjects),
        nameof(IsDirty),
        nameof(ObjectManagerCount),
        nameof(SceneObjectCount),
        nameof(HasObjectManagers),
        nameof(HasSceneObjects),
        nameof(TotalObjectCount),
        nameof(IsEmpty),
        nameof(ResourceSize),
        nameof(LastModified),
        nameof(MemoryUsage),
        nameof(ResourceComplexity)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(IsEditable) => new TypedValue(typeof(bool), IsEditable),
            nameof(ObjectManagers) => new TypedValue(typeof(IReadOnlyList<ObjectManager>), ObjectManagers),
            nameof(SceneObjects) => new TypedValue(typeof(IReadOnlyList<SceneObject>), SceneObjects),
            nameof(IsDirty) => new TypedValue(typeof(bool), IsDirty),
            nameof(ObjectManagerCount) => new TypedValue(typeof(int), ObjectManagerCount),
            nameof(SceneObjectCount) => new TypedValue(typeof(int), SceneObjectCount),
            nameof(HasObjectManagers) => new TypedValue(typeof(bool), HasObjectManagers),
            nameof(HasSceneObjects) => new TypedValue(typeof(bool), HasSceneObjects),
            nameof(TotalObjectCount) => new TypedValue(typeof(int), TotalObjectCount),
            nameof(IsEmpty) => new TypedValue(typeof(bool), IsEmpty),
            nameof(ResourceSize) => new TypedValue(typeof(long), ResourceSize),
            nameof(LastModified) => new TypedValue(typeof(DateTime), LastModified),
            nameof(MemoryUsage) => new TypedValue(typeof(long), MemoryUsage),
            nameof(ResourceComplexity) => new TypedValue(typeof(string), ResourceComplexity),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("World resource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(IsEditable)],
            2 => this[nameof(ObjectManagers)],
            3 => this[nameof(SceneObjects)],
            4 => this[nameof(IsDirty)],
            5 => this[nameof(ObjectManagerCount)],
            6 => this[nameof(SceneObjectCount)],
            7 => this[nameof(HasObjectManagers)],
            8 => this[nameof(HasSceneObjects)],
            9 => this[nameof(TotalObjectCount)],
            10 => this[nameof(IsEmpty)],
            11 => this[nameof(ResourceSize)],
            12 => this[nameof(LastModified)],
            13 => this[nameof(MemoryUsage)],
            14 => this[nameof(ResourceComplexity)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-14, got {index}")
        };
        set => throw new NotSupportedException("World resource fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        LastModified = DateTime.UtcNow;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Releases all resources used by the WorldResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _stream?.Dispose();
            _isDisposed = true;
        }
    }
}

/// <summary>
/// Represents an object manager in a world resource.
/// </summary>
/// <param name="Id">The unique identifier for the object manager.</param>
/// <param name="Name">The name of the object manager.</param>
public record class ObjectManager(ulong Id, string Name);

/// <summary>
/// Represents a scene object in a world resource.
/// </summary>
/// <param name="Id">The unique identifier for the scene object.</param>
/// <param name="Name">The name of the scene object.</param>
public record class SceneObject(ulong Id, string Name)
{
    /// <summary>
    /// Gets or sets the position of the scene object.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the rotation of the scene object.
    /// </summary>
    public float Rotation { get; set; } = 0f;
}

/// <summary>
/// Represents a 3D vector.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public record struct Vector3(float X, float Y, float Z)
{
    /// <summary>
    /// Gets a vector with all components set to zero.
    /// </summary>
    public static Vector3 Zero => new(0, 0, 0);

    /// <summary>
    /// Gets a vector with all components set to one.
    /// </summary>
    public static Vector3 One => new(1, 1, 1);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    /// <returns>A string representation of the vector.</returns>
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
