using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Interface for light resources in The Sims 4.
/// Light resources define lighting properties for scenes and objects.
/// </summary>
public interface ILightResource : IResource
{
    /// <summary>
    /// Gets or sets the type of light.
    /// </summary>
    LightType LightType { get; set; }

    /// <summary>
    /// Gets or sets the light color.
    /// </summary>
    LightColor Color { get; set; }

    /// <summary>
    /// Gets or sets the light intensity (0.0 to 1.0).
    /// </summary>
    float Intensity { get; set; }

    /// <summary>
    /// Gets or sets the light range/radius.
    /// </summary>
    float Range { get; set; }

    /// <summary>
    /// Gets or sets the light falloff type.
    /// </summary>
    LightFalloff Falloff { get; set; }

    /// <summary>
    /// Gets or sets whether the light casts shadows.
    /// </summary>
    bool CastsShadows { get; set; }

    /// <summary>
    /// Gets or sets the light position in 3D space.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the light direction (for directional/spot lights).
    /// </summary>
    Vector3 Direction { get; set; }

    /// <summary>
    /// Gets or sets the spot light cone angle (for spot lights).
    /// </summary>
    float ConeAngle { get; set; }

    /// <summary>
    /// Gets or sets whether the light is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the light priority for rendering.
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Loads the light resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the light resource to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
