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
