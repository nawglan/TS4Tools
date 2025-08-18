using System.Collections.ObjectModel;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Implementation of IRigResource for handling rig (skeleton) resources.
/// </summary>
public class RigResource : IRigResource, IDisposable
{
    private static readonly byte[] MagicBytes = [0x52, 0x49, 0x47, 0x53]; // "RIGS"

    private readonly List<Bone> _bones = [];
    private readonly List<string> _contentFields =
    [
        "RigName",
        "BoneCount",
        "RigVersion",
        "SupportsIk",
        "Bones"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RigResource"/> class.
    /// </summary>
    public RigResource()
    {
        RigName = string.Empty;
        RigVersion = 1;
        SupportsIk = false;
        Bones = new ReadOnlyCollection<Bone>(_bones);
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RigResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public RigResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ReadFromStream(stream);
    }

    private string _rigName = string.Empty;

    /// <inheritdoc />
    public string RigName
    {
        get => _rigName;
        set
        {
            if (_rigName != value)
            {
                _rigName = value;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Bone> Bones { get; private set; }

    /// <inheritdoc />
    public int BoneCount => _bones.Count;

    /// <inheritdoc />
    public int RigVersion { get; set; }

    /// <inheritdoc />
    public bool SupportsIk { get; set; }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RigResource));

            _stream ??= new MemoryStream();
            return _stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RigResource));

            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields;

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Adds a bone to the rig.
    /// </summary>
    /// <param name="bone">The bone to add</param>
    public void AddBone(Bone bone)
    {
        _bones.Add(bone);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a bone from the rig.
    /// </summary>
    /// <param name="bone">The bone to remove</param>
    /// <returns>True if the bone was removed</returns>
    public bool RemoveBone(Bone bone)
    {
        var removed = _bones.Remove(bone);
        if (removed)
            OnResourceChanged();
        return removed;
    }

    /// <summary>
    /// Clears all bones.
    /// </summary>
    public void ClearBones()
    {
        if (_bones.Count > 0)
        {
            _bones.Clear();
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Gets the root bone of the rig (bone with no parent).
    /// </summary>
    public Bone? RootBone
    {
        get
        {
            var rootBones = _bones.Where(b => b.ParentName == null).ToList();
            return rootBones.Count > 0 ? rootBones[0] : null;
        }
    }

    /// <summary>
    /// Gets the bone hierarchy as a list of root bones.
    /// </summary>
    public IReadOnlyList<Bone> BoneHierarchy => _bones.Where(b => b.ParentName == null).ToList().AsReadOnly();

    /// <summary>
    /// Finds a bone by name.
    /// </summary>
    /// <param name="boneName">The name of the bone to find</param>
    /// <returns>The bone if found, null otherwise</returns>
    public Bone? FindBone(string boneName)
    {
        if (string.IsNullOrEmpty(boneName))
            return null;

        var matchingBones = _bones.Where(b => b.Name != null && b.Name.Equals(boneName, StringComparison.OrdinalIgnoreCase)).ToList();
        return matchingBones.Count > 0 ? matchingBones[0] : null;
    }

    /// <summary>
    /// Finds a bone by name (alias for FindBone for test compatibility).
    /// </summary>
    /// <param name="boneName">The name of the bone to find</param>
    /// <returns>The bone if found, null otherwise</returns>
    public Bone? FindBoneByName(string boneName)
    {
        return FindBone(boneName);
    }

    /// <summary>
    /// Gets the bone hierarchy starting from root bones.
    /// </summary>
    public IEnumerable<Bone> AllBones => BoneHierarchy;

    /// <summary>
    /// Gets all child bones of a parent bone.
    /// </summary>
    /// <param name="parentBoneName">The name of the parent bone</param>
    /// <returns>Collection of child bones</returns>
    public IEnumerable<Bone> GetChildBones(string parentBoneName)
    {
        return _bones.Where(b => b.ParentName?.Equals(parentBoneName, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Gets all root bones (bones without parents).
    /// </summary>
    /// <returns>Collection of root bones</returns>
    public IEnumerable<Bone> GetRootBones()
    {
        return _bones.Where(b => string.IsNullOrEmpty(b.ParentName));
    }

    private TypedValue GetFieldValue(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetFieldValue(_contentFields[index]);
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "RigName" => TypedValue.Create(RigName),
            "BoneCount" => TypedValue.Create(BoneCount),
            "RigVersion" => TypedValue.Create(RigVersion),
            "SupportsIk" => TypedValue.Create(SupportsIk),
            "Bones" => TypedValue.Create(Bones.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        SetFieldValue(_contentFields[index], value);
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "RigName":
                RigName = value.Value?.ToString() ?? string.Empty;
                break;
            case "RigVersion":
                if (value.Value is int rigVersion)
                    RigVersion = rigVersion;
                break;
            case "SupportsIk":
                if (value.Value is bool supportsIk)
                    SupportsIk = supportsIk;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }

        OnResourceChanged();
    }

    /// <summary>
    /// Loads the rig resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Copy stream to memory stream for processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        memoryStream.Position = 0;

        ReadFromStream(memoryStream);
    }

    private void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);

        // Handle empty stream gracefully
        if (stream.Length == 0)
        {
            // Initialize with default values for empty stream
            RigName = string.Empty;
            Bones = new List<Bone>();
            return;
        }

        // Read and validate magic bytes
        if (stream.Length < 4)
        {
            throw new InvalidDataException("Invalid rig resource format");
        }

        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException("Invalid rig resource format");
        }

        // Read rig data
        var nameLength = reader.ReadInt32();
        RigName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
        RigVersion = reader.ReadInt32();
        SupportsIk = reader.ReadBoolean();

        // Read bones
        var boneCount = reader.ReadInt32();
        _bones.Clear();

        for (int i = 0; i < boneCount; i++)
        {
            var boneNameLength = reader.ReadInt32();
            var boneName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(boneNameLength));

            string? parentName = null;
            var hasParent = reader.ReadBoolean();
            if (hasParent)
            {
                var parentNameLength = reader.ReadInt32();
                parentName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(parentNameLength));
            }

            var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var rotation = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var scale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            _bones.Add(new Bone(boneName, parentName, position, rotation, scale));
        }
    }

    private void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        // Write magic bytes
        writer.Write(MagicBytes);

        // Write rig data
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(RigName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(RigVersion);
        writer.Write(SupportsIk);

        // Write bones
        writer.Write(_bones.Count);

        foreach (var bone in _bones)
        {
            var boneNameBytes = System.Text.Encoding.UTF8.GetBytes(bone.Name);
            writer.Write(boneNameBytes.Length);
            writer.Write(boneNameBytes);

            writer.Write(!string.IsNullOrEmpty(bone.ParentName));
            if (!string.IsNullOrEmpty(bone.ParentName))
            {
                var parentNameBytes = System.Text.Encoding.UTF8.GetBytes(bone.ParentName);
                writer.Write(parentNameBytes.Length);
                writer.Write(parentNameBytes);
            }

            writer.Write(bone.Position.X);
            writer.Write(bone.Position.Y);
            writer.Write(bone.Position.Z);

            writer.Write(bone.Rotation.X);
            writer.Write(bone.Rotation.Y);
            writer.Write(bone.Rotation.Z);
            writer.Write(bone.Rotation.W);

            writer.Write(bone.Scale.X);
            writer.Write(bone.Scale.Y);
            writer.Write(bone.Scale.Z);
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _disposed = true;
        }
    }
}
