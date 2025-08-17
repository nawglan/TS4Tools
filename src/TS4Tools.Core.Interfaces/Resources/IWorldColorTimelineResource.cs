using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Represents a World Color Timeline resource that defines day/night cycles,
/// lighting transitions, and environmental color settings for Sims 4 worlds.
/// </summary>
/// <remarks>
/// WorldColorTimelineResource manages complex timeline data for environmental lighting effects including:
/// - Day/night color transitions for sky, sun, and ambient lighting
/// - Weather-related color effects (fog, clouds, shadows)
/// - Seasonal and time-based lighting variations
/// - Point-of-interest specific lighting settings
/// </remarks>
public interface IWorldColorTimelineResource : IResource
{
    /// <summary>
    /// Gets or sets the version of the color timeline format.
    /// </summary>
    /// <remarks>
    /// Version 13+ supports basic timeline features.
    /// Version 14+ adds additional timeline remapping capabilities.
    /// </remarks>
    uint Version { get; set; }

    /// <summary>
    /// Gets the collection of color timelines for this world.
    /// </summary>
    /// <remarks>
    /// Each timeline defines color transitions for different environmental aspects
    /// such as ambient lighting, directional lighting, sky colors, etc.
    /// </remarks>
    IReadOnlyList<IColorTimeline> ColorTimelines { get; }

    /// <summary>
    /// Adds a new color timeline to the resource.
    /// </summary>
    /// <param name="timeline">The timeline to add.</param>
    void AddColorTimeline(IColorTimeline timeline);

    /// <summary>
    /// Removes a color timeline from the resource.
    /// </summary>
    /// <param name="timeline">The timeline to remove.</param>
    /// <returns>True if the timeline was removed, false if it was not found.</returns>
    bool RemoveColorTimeline(IColorTimeline timeline);

    /// <summary>
    /// Clears all color timelines.
    /// </summary>
    void ClearColorTimelines();

    /// <summary>
    /// Saves the resource data to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    void Save(Stream stream);
}

/// <summary>
/// Represents a single color timeline with all environmental lighting components.
/// </summary>
public interface IColorTimeline
{
    /// <summary>
    /// Gets or sets the timeline version.
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Gets or sets the ambient light color data over time.
    /// </summary>
    IColorTimelineData AmbientColors { get; set; }

    /// <summary>
    /// Gets or sets the directional light color data over time.
    /// </summary>
    IColorTimelineData DirectionalColors { get; set; }

    /// <summary>
    /// Gets or sets the shadow color data over time.
    /// </summary>
    IColorTimelineData ShadowColors { get; set; }

    /// <summary>
    /// Gets or sets the sky horizon color data over time.
    /// </summary>
    IColorTimelineData SkyHorizonColors { get; set; }

    /// <summary>
    /// Gets or sets the fog start range color data over time.
    /// </summary>
    IColorTimelineData FogStartRange { get; set; }

    /// <summary>
    /// Gets or sets the fog end range color data over time.
    /// </summary>
    IColorTimelineData FogEndRange { get; set; }

    /// <summary>
    /// Gets or sets the sky horizon dark color data over time.
    /// </summary>
    IColorTimelineData SkyHorizonDarkColors { get; set; }

    /// <summary>
    /// Gets or sets the sky light color data over time.
    /// </summary>
    IColorTimelineData SkyLightColors { get; set; }

    /// <summary>
    /// Gets or sets the sky dark color data over time.
    /// </summary>
    IColorTimelineData SkyDarkColors { get; set; }

    /// <summary>
    /// Gets or sets the sun color data over time.
    /// </summary>
    IColorTimelineData SunColors { get; set; }

    /// <summary>
    /// Gets or sets the sun halo color data over time.
    /// </summary>
    IColorTimelineData HaloColors { get; set; }

    /// <summary>
    /// Gets or sets the sun dark cloud color data over time.
    /// </summary>
    IColorTimelineData SunDarkCloudColors { get; set; }

    /// <summary>
    /// Gets or sets the sun light cloud color data over time.
    /// </summary>
    IColorTimelineData SunLightCloudColors { get; set; }

    /// <summary>
    /// Gets or sets the horizon dark cloud color data over time.
    /// </summary>
    IColorTimelineData HorizonDarkCloudColors { get; set; }

    /// <summary>
    /// Gets or sets the horizon light cloud color data over time.
    /// </summary>
    IColorTimelineData HorizonLightCloudColors { get; set; }

    /// <summary>
    /// Gets or sets the cloud shadow color data over time.
    /// </summary>
    IColorTimelineData CloudShadowCloudColors { get; set; }

    /// <summary>
    /// Gets or sets the point of interest ID associated with this timeline.
    /// </summary>
    uint PointOfInterestId { get; set; }

    /// <summary>
    /// Gets or sets the bloom threshold values over time.
    /// </summary>
    IColorTimelineData BloomThresholds { get; set; }

    /// <summary>
    /// Gets or sets the bloom intensity values over time.
    /// </summary>
    IColorTimelineData BloomIntensities { get; set; }

    /// <summary>
    /// Gets or sets the sunrise time (0.0 to 1.0 representing 24-hour cycle).
    /// </summary>
    float SunriseTime { get; set; }

    /// <summary>
    /// Gets or sets the sunset time (0.0 to 1.0 representing 24-hour cycle).
    /// </summary>
    float SunsetTime { get; set; }

    /// <summary>
    /// Gets or sets the dense fog color data over time.
    /// </summary>
    IColorTimelineData DenseFogColors { get; set; }

    /// <summary>
    /// Gets or sets the dense fog start range values over time.
    /// </summary>
    IColorTimelineData DenseFogStartRange { get; set; }

    /// <summary>
    /// Gets or sets the dense fog end range values over time.
    /// </summary>
    IColorTimelineData DenseFogEndRange { get; set; }

    /// <summary>
    /// Gets or sets the moon radius multiplier values over time.
    /// </summary>
    IColorTimelineData MoonRadiusMultipliers { get; set; }

    /// <summary>
    /// Gets or sets the sun radius multiplier values over time.
    /// </summary>
    IColorTimelineData SunRadiusMultipliers { get; set; }

    /// <summary>
    /// Gets or sets the time when stars appear (0.0 to 1.0 representing 24-hour cycle).
    /// </summary>
    float StarsAppearTime { get; set; }

    /// <summary>
    /// Gets or sets the time when stars disappear (0.0 to 1.0 representing 24-hour cycle).
    /// </summary>
    float StarsDisappearTime { get; set; }

    /// <summary>
    /// Gets or sets whether timeline remapping is enabled (version 14+ feature).
    /// </summary>
    bool RemapTimeline { get; set; }
}

/// <summary>
/// Represents color data points over time for timeline interpolation.
/// </summary>
public interface IColorTimelineData
{
    /// <summary>
    /// Gets the collection of color data points in this timeline.
    /// </summary>
    IReadOnlyList<IColorData> ColorPoints { get; }

    /// <summary>
    /// Adds a color data point to the timeline.
    /// </summary>
    /// <param name="colorData">The color data to add.</param>
    void AddColorPoint(IColorData colorData);

    /// <summary>
    /// Removes a color data point from the timeline.
    /// </summary>
    /// <param name="colorData">The color data to remove.</param>
    /// <returns>True if the color data was removed, false if not found.</returns>
    bool RemoveColorPoint(IColorData colorData);

    /// <summary>
    /// Clears all color data points.
    /// </summary>
    void ClearColorPoints();

    /// <summary>
    /// Gets the interpolated color at the specified time.
    /// </summary>
    /// <param name="time">The time value (0.0 to 1.0).</param>
    /// <returns>The interpolated color data.</returns>
    IColorData GetColorAtTime(float time);
}

/// <summary>
/// Represents a single color data point with RGBA values and time.
/// </summary>
public interface IColorData
{
    /// <summary>
    /// Gets or sets the red component (0.0 to 1.0).
    /// </summary>
    float Red { get; set; }

    /// <summary>
    /// Gets or sets the green component (0.0 to 1.0).
    /// </summary>
    float Green { get; set; }

    /// <summary>
    /// Gets or sets the blue component (0.0 to 1.0).
    /// </summary>
    float Blue { get; set; }

    /// <summary>
    /// Gets or sets the alpha component (0.0 to 1.0).
    /// </summary>
    float Alpha { get; set; }

    /// <summary>
    /// Gets or sets the time value for this color point (0.0 to 1.0).
    /// </summary>
    float Time { get; set; }
}
