using TS4Tools.Package;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// SkyBox texture resource for world sky rendering.
/// Resource Type: 0x71A449C9
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SkyBoxTextureResource.cs
///
/// Format:
/// - Version: uint32
/// - TextureCount: int32
/// - Textures[]: SkyBoxTexture
///   - Type: int32 (enum SkyBoxTextureType)
///   - TGIKey: TGI (Type, Group, Instance - 16 bytes)
///   - TexturePathLength: int32 (Unicode char count)
///   - TexturePath: string (Unicode, length * 2 bytes)
/// </summary>
public sealed class SkyBoxTextureResource : TypedResource
{
    private const int MaxTextureCount = 1000; // Reasonable limit for validation

    private uint _version;
    private List<SkyBoxTexture> _textures = [];

    /// <summary>
    /// The format version.
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The list of sky box textures.
    /// </summary>
    public List<SkyBoxTexture> Textures
    {
        get => _textures;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _textures = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Creates a new SkyBoxTexture resource by parsing data.
    /// </summary>
    public SkyBoxTextureResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8) // Version + TextureCount
        {
            throw new InvalidDataException($"SkyBoxTexture data too short: {data.Length} bytes");
        }

        int offset = 0;

        // Read version
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read texture count
        int textureCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate texture count
        if (textureCount < 0 || textureCount > MaxTextureCount)
        {
            throw new InvalidDataException($"Invalid texture count: {textureCount}");
        }

        // Read textures
        _textures = new List<SkyBoxTexture>(textureCount);

        for (int i = 0; i < textureCount; i++)
        {
            // Need at least: Type (4) + TGI (16) + StringLength (4) = 24 bytes
            if (offset + 24 > data.Length)
            {
                throw new InvalidDataException($"Unexpected end of data reading texture {i}");
            }

            var texture = new SkyBoxTexture();

            // Read type
            texture.Type = (SkyBoxTextureType)BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            // Read TGI (Type, Group, Instance order)
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;
            texture.TgiKey = new ResourceKey(type, group, instance);

            // Read string length (Unicode char count)
            int stringLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (stringLength < 0 || offset + (stringLength * 2) > data.Length)
            {
                throw new InvalidDataException($"Invalid string length {stringLength} at offset {offset}");
            }

            // Read Unicode string
            texture.TexturePath = Encoding.Unicode.GetString(data.Slice(offset, stringLength * 2));
            offset += stringLength * 2;

            _textures.Add(texture);
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate total size
        int size = 8; // Version + TextureCount
        foreach (var texture in _textures)
        {
            size += 4; // Type
            size += 16; // TGI
            size += 4; // StringLength
            size += texture.TexturePath.Length * 2; // Unicode string
        }

        var buffer = new byte[size];
        int offset = 0;

        // Write version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _version);
        offset += 4;

        // Write texture count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _textures.Count);
        offset += 4;

        // Write textures
        foreach (var texture in _textures)
        {
            // Write type
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), (int)texture.Type);
            offset += 4;

            // Write TGI (Type, Group, Instance order)
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), texture.TgiKey.ResourceType);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), texture.TgiKey.ResourceGroup);
            offset += 4;
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), texture.TgiKey.Instance);
            offset += 8;

            // Write string length (Unicode char count)
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), texture.TexturePath.Length);
            offset += 4;

            // Write Unicode string
            Encoding.Unicode.GetBytes(texture.TexturePath, buffer.AsSpan(offset));
            offset += texture.TexturePath.Length * 2;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _version = 1;
        _textures = [];
    }
}

/// <summary>
/// Types of sky box textures.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SkyBoxTextureResource.cs lines 167-177
/// </summary>
public enum SkyBoxTextureType
{
    /// <summary>Cloud texture.</summary>
    Clouds = 0,

    /// <summary>Sun texture.</summary>
    Sun = 1,

    /// <summary>Sun halo effect texture.</summary>
    SunHalo = 2,

    /// <summary>Moon texture.</summary>
    Moon = 3,

    /// <summary>Stars texture.</summary>
    Stars = 4,

    /// <summary>Cube map texture.</summary>
    CubeMap = 5,

    /// <summary>Number of texture types (not a valid type).</summary>
    NumTextures = 6,

    /// <summary>No texture.</summary>
    None = -1
}

/// <summary>
/// A single sky box texture entry.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SkyBoxTextureResource.cs lines 97-178
/// </summary>
public sealed class SkyBoxTexture : IEquatable<SkyBoxTexture>
{
    /// <summary>
    /// The type of sky box texture.
    /// </summary>
    public SkyBoxTextureType Type { get; set; } = SkyBoxTextureType.None;

    /// <summary>
    /// The TGI key referencing the texture resource.
    /// </summary>
    public ResourceKey TgiKey { get; set; }

    /// <summary>
    /// The texture path string.
    /// </summary>
    public string TexturePath { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool Equals(SkyBoxTexture? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type &&
               TgiKey == other.TgiKey &&
               TexturePath == other.TexturePath;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SkyBoxTexture);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Type, TgiKey, TexturePath);
}
