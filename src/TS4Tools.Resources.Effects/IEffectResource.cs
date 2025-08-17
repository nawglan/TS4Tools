using System.ComponentModel.DataAnnotations;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Interface for effect resources in The Sims 4.
/// </summary>
public interface IEffectResource : IResource
{
    /// <summary>
    /// Gets the effect type.
    /// </summary>
    EffectType EffectType { get; }

    /// <summary>
    /// Gets the effect name or identifier.
    /// </summary>
    string EffectName { get; }

    /// <summary>
    /// Gets the blend mode for the effect.
    /// </summary>
    BlendMode BlendMode { get; }

    /// <summary>
    /// Gets the effect parameters.
    /// </summary>
    IReadOnlyList<EffectParameter> Parameters { get; }

    /// <summary>
    /// Gets the texture references used by the effect.
    /// </summary>
    IReadOnlyList<EffectTexture> Textures { get; }

    /// <summary>
    /// Gets a value indicating whether the effect is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the effect duration in seconds (0 for continuous effects).
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the effect priority for rendering order.
    /// </summary>
    int Priority { get; }
}
