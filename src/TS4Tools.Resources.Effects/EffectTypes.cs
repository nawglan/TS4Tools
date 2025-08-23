namespace TS4Tools.Resources.Effects;

/// <summary>
/// Types of visual effects supported in The Sims 4.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// No effect specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Particle system effect.
    /// </summary>
    Particle = 0x00000001,

    /// <summary>
    /// Lighting effect.
    /// </summary>
    Light = 0x00000002,

    /// <summary>
    /// Screen space effect.
    /// </summary>
    ScreenSpace = 0x00000003,

    /// <summary>
    /// Water effect.
    /// </summary>
    Water = 0x00000004,

    /// <summary>
    /// Fire effect.
    /// </summary>
    Fire = 0x00000005,

    /// <summary>
    /// Smoke effect.
    /// </summary>
    Smoke = 0x00000006,

    /// <summary>
    /// Magic/spell effect.
    /// </summary>
    Magic = 0x00000007,

    /// <summary>
    /// Weather effect.
    /// </summary>
    Weather = 0x00000008,

    /// <summary>
    /// Atmospheric effect.
    /// </summary>
    Atmospheric = 0x00000009,

    /// <summary>
    /// Post-processing effect.
    /// </summary>
    PostProcess = 0x0000000A
}

/// <summary>
/// Blend modes for visual effects.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Normal alpha blending.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Additive blending.
    /// </summary>
    Additive = 1,

    /// <summary>
    /// Multiply blending.
    /// </summary>
    Multiply = 2,

    /// <summary>
    /// Screen blending.
    /// </summary>
    Screen = 3,

    /// <summary>
    /// Overlay blending.
    /// </summary>
    Overlay = 4
}

/// <summary>
/// Represents a visual effect parameter.
/// </summary>
/// <param name="Name">Parameter name.</param>
/// <param name="Type">Parameter type.</param>
/// <param name="Value">Parameter value.</param>
public readonly record struct EffectParameter(string Name, string Type, object Value);

/// <summary>
/// Represents an effect texture reference.
/// </summary>
/// <param name="TextureName">Name of the texture.</param>
/// <param name="TextureIndex">Index in texture array.</param>
/// <param name="UvIndex">UV coordinate index.</param>
public readonly record struct EffectTexture(string TextureName, uint TextureIndex, uint UvIndex);

/// <summary>
/// Types of light sources supported in The Sims 4.
/// </summary>
public enum LightType
{
    /// <summary>
    /// Directional light (like sunlight).
    /// </summary>
    Directional = 0,

    /// <summary>
    /// Point light (omnidirectional).
    /// </summary>
    Point = 1,

    /// <summary>
    /// Spot light (cone-shaped).
    /// </summary>
    Spot = 2,

    /// <summary>
    /// Area light (rectangular or circular).
    /// </summary>
    Area = 3,

    /// <summary>
    /// Ambient light (environmental).
    /// </summary>
    Ambient = 4
}

/// <summary>
/// Light falloff/attenuation types.
/// </summary>
public enum LightFalloff
{
    /// <summary>
    /// No falloff (constant intensity).
    /// </summary>
    None = 0,

    /// <summary>
    /// Linear falloff.
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Quadratic falloff (physically accurate).
    /// </summary>
    Quadratic = 2,

    /// <summary>
    /// Exponential falloff.
    /// </summary>
    Exponential = 3
}

/// <summary>
/// Represents a light color with RGB components.
/// </summary>
/// <param name="Red">Red component (0.0 to 1.0).</param>
/// <param name="Green">Green component (0.0 to 1.0).</param>
/// <param name="Blue">Blue component (0.0 to 1.0).</param>
public readonly record struct LightColor(float Red, float Green, float Blue)
{
    /// <summary>
    /// White light.
    /// </summary>
    public static readonly LightColor White = new(1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Warm white light.
    /// </summary>
    public static readonly LightColor WarmWhite = new(1.0f, 0.9f, 0.7f);

    /// <summary>
    /// Cool white light.
    /// </summary>
    public static readonly LightColor CoolWhite = new(0.8f, 0.9f, 1.0f);

    /// <summary>
    /// Creates a light color from RGB values (0-255 range).
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>A new LightColor.</returns>
    public static LightColor FromRgb(byte r, byte g, byte b) =>
        new(r / 255.0f, g / 255.0f, b / 255.0f);
}

/// <summary>
/// Represents a 3D vector for positions and directions.
/// </summary>
/// <param name="X">X coordinate.</param>
/// <param name="Y">Y coordinate.</param>
/// <param name="Z">Z coordinate.</param>
public readonly record struct Vector3(float X, float Y, float Z)
{
    /// <summary>
    /// Zero vector.
    /// </summary>
    public static readonly Vector3 Zero = new(0.0f, 0.0f, 0.0f);

    /// <summary>
    /// Unit vector pointing up.
    /// </summary>
    public static readonly Vector3 Up = new(0.0f, 1.0f, 0.0f);

    /// <summary>
    /// Unit vector pointing forward.
    /// </summary>
    public static readonly Vector3 Forward = new(0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Unit vector pointing right.
    /// </summary>
    public static readonly Vector3 Right = new(1.0f, 0.0f, 0.0f);

    /// <summary>
    /// Calculates the length of the vector.
    /// </summary>
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a normalized version of the vector.
    /// </summary>
    public Vector3 Normalized
    {
        get
        {
            var length = Length;
            return length > 0 ? new Vector3(X / length, Y / length, Z / length) : Zero;
        }
    }
}

/// <summary>
/// Resource type constants for effect resources.
/// </summary>
public static class EffectResourceTypes
{
    /// <summary>
    /// Light Resource (0x03B4C61D).
    /// Defines lighting properties for scenes and objects.
    /// </summary>
    public const uint LightResourceType = 0x03B4C61D;

    /// <summary>
    /// Effect Resource (0x033C3C97).
    /// General visual effects and shader resources.
    /// </summary>
    public const uint EffectResourceType = 0x033C3C97;
}
