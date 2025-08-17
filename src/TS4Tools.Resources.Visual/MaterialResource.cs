using System.Runtime.InteropServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Represents a material resource that defines material properties and shader parameters in The Sims 4 package files.
/// Materials control the visual appearance of 3D objects, including textures, lighting, and surface properties.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class MaterialResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private readonly Dictionary<string, MaterialProperty> _properties;
    private readonly List<MaterialTexture> _textures;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this material.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the name of this material.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the material type.
    /// </summary>
    public MaterialType Type { get; }

    /// <summary>
    /// Gets or sets the diffuse color of the material.
    /// </summary>
    public MaterialColor DiffuseColor { get; set; } = MaterialColor.White;

    /// <summary>
    /// Gets or sets the specular color of the material.
    /// </summary>
    public MaterialColor SpecularColor { get; set; } = MaterialColor.White;

    /// <summary>
    /// Gets or sets the emissive color of the material.
    /// </summary>
    public MaterialColor EmissiveColor { get; set; } = MaterialColor.Black;

    /// <summary>
    /// Gets or sets the shininess value (0.0 to 1.0).
    /// </summary>
    public float Shininess { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the opacity value (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the metallic value (0.0 to 1.0).
    /// </summary>
    public float Metallic { get; set; } = 0.0f;

    /// <summary>
    /// Gets or sets the roughness value (0.0 to 1.0).
    /// </summary>
    public float Roughness { get; set; } = 0.5f;

    /// <summary>
    /// Gets the collection of custom material properties.
    /// </summary>
    public IReadOnlyDictionary<string, MaterialProperty> Properties => _properties;

    /// <summary>
    /// Gets the collection of textures used by this material.
    /// </summary>
    public IReadOnlyList<MaterialTexture> Textures => _textures;

    // IResource implementation
    /// <summary>
    /// Gets the requested API version for this resource.
    /// </summary>
    public int RequestedApiVersion { get; }

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; }

    /// <summary>
    /// Event raised when the resource content changes.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the resource data as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            var bytes = AsBytes;
            return new MemoryStream(bytes);
        }
    }

    /// <summary>
    /// Gets the resource data as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            if (!_isDirty && _cachedBytes != null)
                return _cachedBytes;

            _cachedBytes = SerializeToBytes();
            _isDirty = false;
            return _cachedBytes;
        }
    }
    private byte[]? _cachedBytes;

    /// <summary>
    /// Gets or sets content field values by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[int index]
    {
        get
        {
            return index switch
            {
                0 => TypedValue.Create(Shininess),
                1 => TypedValue.Create(Opacity),
                2 => TypedValue.Create(Metallic),
                3 => TypedValue.Create(Roughness),
                4 => TypedValue.Create(DiffuseColor.ToString()),
                5 => TypedValue.Create(SpecularColor.ToString()),
                6 => TypedValue.Create(EmissiveColor.ToString()),
                7 => TypedValue.Create(Type.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    float? shininessValue = value.GetValue<float>();
                    if (shininessValue.HasValue)
                    {
                        Shininess = shininessValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 1:
                    float? opacityValue = value.GetValue<float>();
                    if (opacityValue.HasValue)
                    {
                        Opacity = opacityValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 2:
                    float? metallicValue = value.GetValue<float>();
                    if (metallicValue.HasValue)
                    {
                        Metallic = metallicValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 3:
                    float? roughnessValue = value.GetValue<float>();
                    if (roughnessValue.HasValue)
                    {
                        Roughness = roughnessValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 4:
                    var diffuseString = value.GetValue<string>();
                    if (diffuseString != null && MaterialColor.TryParse(diffuseString, out var diffuseColor))
                    {
                        DiffuseColor = diffuseColor;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 5:
                    var specularString = value.GetValue<string>();
                    if (specularString != null && MaterialColor.TryParse(specularString, out var specularColor))
                    {
                        SpecularColor = specularColor;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 6:
                    var emissiveString = value.GetValue<string>();
                    if (emissiveString != null && MaterialColor.TryParse(emissiveString, out var emissiveColor))
                    {
                        EmissiveColor = emissiveColor;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 7:
                    // Type is read-only after construction
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// Gets or sets content field values by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[string name]
    {
        get
        {
            var index = ContentFields.ToList().IndexOf(name);
            if (index == -1)
                throw new ArgumentException($"Field '{name}' not found", nameof(name));
            return this[index];
        }
        set
        {
            var index = ContentFields.ToList().IndexOf(name);
            if (index == -1)
                throw new ArgumentException($"Field '{name}' not found", nameof(name));
            this[index] = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialResource"/> class.
    /// </summary>
    /// <param name="key">The unique resource key.</param>
    /// <param name="name">The material name.</param>
    /// <param name="type">The material type.</param>
    /// <exception cref="ArgumentNullException">Thrown when key or name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
    public MaterialResource(ResourceKey key, string name, MaterialType type = MaterialType.Standard)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Material name cannot be empty", nameof(name));
        Type = type;

        _properties = new Dictionary<string, MaterialProperty>();
        _textures = new List<MaterialTexture>();

        RequestedApiVersion = 1;
        ContentFields = new List<string>
        {
            "Shininess", "Opacity", "Metallic", "Roughness",
            "DiffuseColor", "SpecularColor", "EmissiveColor", "Type"
        };
    }

    /// <summary>
    /// Adds or updates a custom material property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public void SetProperty(string name, object value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Property name cannot be empty", nameof(name));
        ArgumentNullException.ThrowIfNull(value);

        _properties[name] = new MaterialProperty(value);
        _isDirty = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets a custom material property by name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property value, or null if not found.</returns>
    public MaterialProperty? GetProperty(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : _properties.GetValueOrDefault(name);
    }

    /// <summary>
    /// Removes a custom material property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>True if the property was removed, false if it didn't exist.</returns>
    public bool RemoveProperty(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (_properties.Remove(name))
        {
            _isDirty = true;
            ResourceChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a texture to this material.
    /// </summary>
    /// <param name="texture">The texture to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when texture is null.</exception>
    public void AddTexture(MaterialTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        _textures.Add(texture);
        _isDirty = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a texture from this material.
    /// </summary>
    /// <param name="texture">The texture to remove.</param>
    /// <returns>True if the texture was removed, false if it wasn't found.</returns>
    public bool RemoveTexture(MaterialTexture texture)
    {
        if (texture == null)
            return false;

        if (_textures.Remove(texture))
        {
            _isDirty = true;
            ResourceChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all textures from this material.
    /// </summary>
    public void ClearTextures()
    {
        if (_textures.Count > 0)
        {
            _textures.Clear();
            _isDirty = true;
        }
    }

    /// <summary>
    /// Validates the material properties and constraints.
    /// </summary>
    /// <returns>A collection of validation issues, if any.</returns>
    public IEnumerable<string> Validate()
    {
        var issues = new List<string>();

        if (Opacity < 0.0f || Opacity > 1.0f)
            issues.Add("Opacity must be between 0.0 and 1.0");

        if (Shininess < 0.0f || Shininess > 1.0f)
            issues.Add("Shininess must be between 0.0 and 1.0");

        if (Metallic < 0.0f || Metallic > 1.0f)
            issues.Add("Metallic must be between 0.0 and 1.0");

        if (Roughness < 0.0f || Roughness > 1.0f)
            issues.Add("Roughness must be between 0.0 and 1.0");

        if (string.IsNullOrWhiteSpace(Name))
            issues.Add("Material name cannot be empty");

        return issues;
    }

    /// <summary>
    /// Creates a copy of this material resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A copy of this material resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when newKey is null.</exception>
    public MaterialResource Clone(ResourceKey newKey)
    {
        ArgumentNullException.ThrowIfNull(newKey);

        var cloned = new MaterialResource(newKey, Name, Type)
        {
            DiffuseColor = DiffuseColor,
            SpecularColor = SpecularColor,
            EmissiveColor = EmissiveColor,
            Shininess = Shininess,
            Opacity = Opacity,
            Metallic = Metallic,
            Roughness = Roughness
        };

        // Copy custom properties
        foreach (var kvp in _properties)
        {
            cloned.SetProperty(kvp.Key, kvp.Value.Value);
        }

        // Copy textures
        foreach (var texture in _textures)
        {
            cloned.AddTexture(texture);
        }

        return cloned;
    }

    /// <summary>
    /// Serializes the material data to a byte array.
    /// </summary>
    /// <returns>The serialized material data.</returns>
    private byte[] SerializeToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(Name);
        writer.Write((int)Type);

        // Write basic properties
        writer.Write(DiffuseColor.R);
        writer.Write(DiffuseColor.G);
        writer.Write(DiffuseColor.B);
        writer.Write(DiffuseColor.A);

        writer.Write(SpecularColor.R);
        writer.Write(SpecularColor.G);
        writer.Write(SpecularColor.B);
        writer.Write(SpecularColor.A);

        writer.Write(EmissiveColor.R);
        writer.Write(EmissiveColor.G);
        writer.Write(EmissiveColor.B);
        writer.Write(EmissiveColor.A);

        writer.Write(Shininess);
        writer.Write(Opacity);
        writer.Write(Metallic);
        writer.Write(Roughness);

        // Write custom properties
        writer.Write(_properties.Count);
        foreach (var prop in _properties)
        {
            writer.Write(prop.Key);
            writer.Write(prop.Value.Type.GetHashCode()); // Use hash code as type identifier
            writer.Write((float)prop.Value.Value);
        }

        // Write textures
        writer.Write(_textures.Count);
        foreach (var texture in _textures)
        {
            writer.Write((int)texture.Type);
            writer.Write(texture.TextureKey.ResourceType);
            writer.Write(texture.TextureKey.ResourceGroup);
            writer.Write(texture.TextureKey.Instance);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Releases all resources used by this material resource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cachedBytes = null;
            _disposed = true;
        }
    }

    /// <summary>
    /// Returns a string representation of this material resource.
    /// </summary>
    /// <returns>A string containing the material's key and name.</returns>
    public override string ToString()
    {
        return $"MaterialResource [Key={Key}, Name={Name}, Type={Type}]";
    }
}

/// <summary>
/// Represents a custom property value in a material.
/// </summary>
public sealed class MaterialProperty
{
    /// <summary>
    /// Gets the property value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public Type Type => Value.GetType();

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialProperty"/> class.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public MaterialProperty(object value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the property value as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The property value as the specified type.</returns>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to the specified type.</exception>
    public T GetValue<T>()
    {
        return (T)Value;
    }

    /// <summary>
    /// Tries to get the property value as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="value">The converted value, if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    public bool TryGetValue<T>(out T? value)
    {
        try
        {
            value = (T)Value;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Returns a string representation of this material property.
    /// </summary>
    /// <returns>A string containing the property value and type.</returns>
    public override string ToString()
    {
        return $"{Value} ({Type.Name})";
    }
}

/// <summary>
/// Represents a texture used in a material.
/// </summary>
public sealed class MaterialTexture
{
    /// <summary>
    /// Gets the texture resource key.
    /// </summary>
    public ResourceKey TextureKey { get; }

    /// <summary>
    /// Gets the texture type (e.g., Diffuse, Normal, Specular).
    /// </summary>
    public MaterialTextureType Type { get; }

    /// <summary>
    /// Gets the texture slot or index.
    /// </summary>
    public int Slot { get; }

    /// <summary>
    /// Gets the texture coordinate scaling factors.
    /// </summary>
    public (float U, float V) Scale { get; }

    /// <summary>
    /// Gets the texture coordinate offsets.
    /// </summary>
    public (float U, float V) Offset { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialTexture"/> class.
    /// </summary>
    /// <param name="textureKey">The texture resource key.</param>
    /// <param name="type">The texture type.</param>
    /// <param name="slot">The texture slot.</param>
    /// <param name="scale">The texture scale factors.</param>
    /// <param name="offset">The texture offsets.</param>
    /// <exception cref="ArgumentNullException">Thrown when textureKey is null.</exception>
    public MaterialTexture(
        ResourceKey textureKey,
        MaterialTextureType type,
        int slot = 0,
        (float U, float V) scale = default,
        (float U, float V) offset = default)
    {
        TextureKey = textureKey ?? throw new ArgumentNullException(nameof(textureKey));
        Type = type;
        Slot = slot;
        Scale = scale == default ? (1.0f, 1.0f) : scale;
        Offset = offset;
    }

    /// <summary>
    /// Returns a string representation of this material texture.
    /// </summary>
    /// <returns>A string containing the texture's key and properties.</returns>
    public override string ToString()
    {
        return $"MaterialTexture [Key={TextureKey}, Type={Type}, Slot={Slot}]";
    }
}

/// <summary>
/// Represents a color value used in materials.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialColor
{
    /// <summary>
    /// Gets the red component (0.0 to 1.0).
    /// </summary>
    public float R { get; }

    /// <summary>
    /// Gets the green component (0.0 to 1.0).
    /// </summary>
    public float G { get; }

    /// <summary>
    /// Gets the blue component (0.0 to 1.0).
    /// </summary>
    public float B { get; }

    /// <summary>
    /// Gets the alpha component (0.0 to 1.0).
    /// </summary>
    public float A { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialColor"/> struct.
    /// </summary>
    /// <param name="r">The red component (0.0 to 1.0).</param>
    /// <param name="g">The green component (0.0 to 1.0).</param>
    /// <param name="b">The blue component (0.0 to 1.0).</param>
    /// <param name="a">The alpha component (0.0 to 1.0).</param>
    public MaterialColor(float r, float g, float b, float a = 1.0f)
    {
        R = Math.Clamp(r, 0.0f, 1.0f);
        G = Math.Clamp(g, 0.0f, 1.0f);
        B = Math.Clamp(b, 0.0f, 1.0f);
        A = Math.Clamp(a, 0.0f, 1.0f);
    }

    /// <summary>
    /// Gets a white color (1.0, 1.0, 1.0, 1.0).
    /// </summary>
    public static MaterialColor White => new(1.0f, 1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Gets a black color (0.0, 0.0, 0.0, 1.0).
    /// </summary>
    public static MaterialColor Black => new(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Gets a transparent color (0.0, 0.0, 0.0, 0.0).
    /// </summary>
    public static MaterialColor Transparent => new(0.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    /// Parses a color from a string representation.
    /// </summary>
    /// <param name="value">The string value in format "R,G,B,A" or hex format.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static MaterialColor Parse(string value)
    {
        if (TryParse(value, out var color))
            return color;
        throw new FormatException($"Invalid color format: {value}");
    }

    /// <summary>
    /// Tries to parse a color from a string representation.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="color">The parsed color, if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? value, out MaterialColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Try hex format first (#RRGGBBAA or #RRGGBB)
        if (value.StartsWith('#') && value.Length >= 7)
        {
            try
            {
                var hex = value[1..];
                if (hex.Length == 6) hex += "FF"; // Add alpha if not present

                var r = int.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber) / 255.0f;
                var g = int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) / 255.0f;
                var b = int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) / 255.0f;
                var a = hex.Length >= 8 ? int.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber) / 255.0f : 1.0f;

                color = new MaterialColor(r, g, b, a);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Try comma-separated format (R,G,B,A or R,G,B)
        var parts = value.Split(',');
        if (parts.Length >= 3 && parts.Length <= 4)
        {
            try
            {
                var r = float.Parse(parts[0].Trim());
                var g = float.Parse(parts[1].Trim());
                var b = float.Parse(parts[2].Trim());
                var a = parts.Length == 4 ? float.Parse(parts[3].Trim()) : 1.0f;

                color = new MaterialColor(r, g, b, a);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns a string representation of this color.
    /// </summary>
    /// <returns>A string in the format "R,G,B,A".</returns>
    public override string ToString()
    {
        return $"{R:F3},{G:F3},{B:F3},{A:F3}";
    }

    /// <summary>
    /// Returns a hash code for this color.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <summary>
    /// Determines whether this instance is equal to another color.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is MaterialColor other && Equals(other);
    }

    /// <summary>
    /// Determines whether this instance is equal to another color.
    /// </summary>
    /// <param name="other">The other color to compare with.</param>
    /// <returns>True if the colors are equal, false otherwise.</returns>
    public bool Equals(MaterialColor other)
    {
        return Math.Abs(R - other.R) < 0.001f &&
               Math.Abs(G - other.G) < 0.001f &&
               Math.Abs(B - other.B) < 0.001f &&
               Math.Abs(A - other.A) < 0.001f;
    }

    /// <summary>
    /// Determines whether two colors are equal.
    /// </summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns>True if the colors are equal, false otherwise.</returns>
    public static bool operator ==(MaterialColor left, MaterialColor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two colors are not equal.
    /// </summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns>True if the colors are not equal, false otherwise.</returns>
    public static bool operator !=(MaterialColor left, MaterialColor right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Defines the available material types in The Sims 4.
/// </summary>
public enum MaterialType
{
    /// <summary>
    /// Standard material with basic lighting.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Glass material with transparency and refraction.
    /// </summary>
    Glass = 1,

    /// <summary>
    /// Metal material with high reflectivity.
    /// </summary>
    Metal = 2,

    /// <summary>
    /// Cloth or fabric material.
    /// </summary>
    Fabric = 3,

    /// <summary>
    /// Skin material with subsurface scattering.
    /// </summary>
    Skin = 4,

    /// <summary>
    /// Hair material with anisotropic properties.
    /// </summary>
    Hair = 5,

    /// <summary>
    /// Water material with special effects.
    /// </summary>
    Water = 6,

    /// <summary>
    /// Custom material type.
    /// </summary>
    Custom = 99
}

/// <summary>
/// Defines the texture types that can be used in materials.
/// </summary>
public enum MaterialTextureType
{
    /// <summary>
    /// Diffuse color texture (base color).
    /// </summary>
    Diffuse = 0,

    /// <summary>
    /// Normal map texture for surface details.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Specular reflection texture.
    /// </summary>
    Specular = 2,

    /// <summary>
    /// Emissive texture for glowing effects.
    /// </summary>
    Emissive = 3,

    /// <summary>
    /// Roughness texture for surface roughness.
    /// </summary>
    Roughness = 4,

    /// <summary>
    /// Metallic texture for metallic properties.
    /// </summary>
    Metallic = 5,

    /// <summary>
    /// Alpha/transparency mask texture.
    /// </summary>
    Alpha = 6,

    /// <summary>
    /// Ambient occlusion texture.
    /// </summary>
    AmbientOcclusion = 7,

    /// <summary>
    /// Height/displacement map texture.
    /// </summary>
    Height = 8,

    /// <summary>
    /// Custom texture type.
    /// </summary>
    Custom = 99
}
