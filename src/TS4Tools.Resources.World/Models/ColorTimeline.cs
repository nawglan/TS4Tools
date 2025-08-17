namespace TS4Tools.Resources.World.Models;

/// <summary>
/// Represents a complete color timeline with all environmental lighting components
/// </summary>
public class ColorTimeline : IEquatable<ColorTimeline>
{
    /// <summary>
    /// Version of this color timeline (typically 13 or 14+)
    /// </summary>
    public uint Version { get; set; } = 13;

    /// <summary>
    /// Ambient lighting colors
    /// </summary>
    public ColorTimelineData AmbientColors { get; set; } = new();

    /// <summary>
    /// Directional lighting colors
    /// </summary>
    public ColorTimelineData DirectionalColors { get; set; } = new();

    /// <summary>
    /// Shadow colors
    /// </summary>
    public ColorTimelineData ShadowColors { get; set; } = new();

    /// <summary>
    /// Sky horizon colors
    /// </summary>
    public ColorTimelineData SkyHorizonColors { get; set; } = new();

    /// <summary>
    /// Fog start range values
    /// </summary>
    public ColorTimelineData FogStartRange { get; set; } = new();

    /// <summary>
    /// Fog end range values
    /// </summary>
    public ColorTimelineData FogEndRange { get; set; } = new();

    /// <summary>
    /// Sky horizon dark colors
    /// </summary>
    public ColorTimelineData SkyHorizonDarkColors { get; set; } = new();

    /// <summary>
    /// Sky light colors
    /// </summary>
    public ColorTimelineData SkyLightColors { get; set; } = new();

    /// <summary>
    /// Sky dark colors
    /// </summary>
    public ColorTimelineData SkyDarkColors { get; set; } = new();

    /// <summary>
    /// Sun colors
    /// </summary>
    public ColorTimelineData SunColors { get; set; } = new();

    /// <summary>
    /// Halo colors
    /// </summary>
    public ColorTimelineData HaloColors { get; set; } = new();

    /// <summary>
    /// Sun dark cloud colors
    /// </summary>
    public ColorTimelineData SunDarkCloudColors { get; set; } = new();

    /// <summary>
    /// Sun light cloud colors
    /// </summary>
    public ColorTimelineData SunLightCloudColors { get; set; } = new();

    /// <summary>
    /// Horizon dark cloud colors
    /// </summary>
    public ColorTimelineData HorizonDarkCloudColors { get; set; } = new();

    /// <summary>
    /// Horizon light cloud colors
    /// </summary>
    public ColorTimelineData HorizonLightCloudColors { get; set; } = new();

    /// <summary>
    /// Cloud shadow cloud colors
    /// </summary>
    public ColorTimelineData CloudShadowCloudColors { get; set; } = new();

    /// <summary>
    /// Point of interest ID
    /// </summary>
    public uint PointOfInterestId { get; set; }

    /// <summary>
    /// Bloom effect thresholds
    /// </summary>
    public ColorTimelineData BloomThresholds { get; set; } = new();

    /// <summary>
    /// Bloom effect intensities
    /// </summary>
    public ColorTimelineData BloomIntensities { get; set; } = new();

    /// <summary>
    /// Time when sunrise begins
    /// </summary>
    public float SunriseTime { get; set; }

    /// <summary>
    /// Time when sunset begins
    /// </summary>
    public float SunsetTime { get; set; }

    /// <summary>
    /// Dense fog colors
    /// </summary>
    public ColorTimelineData DenseFogColors { get; set; } = new();

    /// <summary>
    /// Dense fog start range
    /// </summary>
    public ColorTimelineData DenseFogStartRange { get; set; } = new();

    /// <summary>
    /// Dense fog end range
    /// </summary>
    public ColorTimelineData DenseFogEndRange { get; set; } = new();

    /// <summary>
    /// Moon radius multipliers
    /// </summary>
    public ColorTimelineData MoonRadiusMultipliers { get; set; } = new();

    /// <summary>
    /// Sun radius multipliers
    /// </summary>
    public ColorTimelineData SunRadiusMultipliers { get; set; } = new();

    /// <summary>
    /// Time when stars appear
    /// </summary>
    public float StarsAppearTime { get; set; }

    /// <summary>
    /// Time when stars disappear
    /// </summary>
    public float StarsDisappearTime { get; set; }

    /// <summary>
    /// Whether to remap timeline (version 14+ only)
    /// </summary>
    public bool RemapTimeline { get; set; } = true;

    /// <summary>
    /// Determines whether the specified <see cref="ColorTimeline"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="ColorTimeline"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public bool Equals(ColorTimeline? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Version == other.Version &&
               AmbientColors.Equals(other.AmbientColors) &&
               DirectionalColors.Equals(other.DirectionalColors) &&
               ShadowColors.Equals(other.ShadowColors) &&
               SkyHorizonColors.Equals(other.SkyHorizonColors) &&
               FogStartRange.Equals(other.FogStartRange) &&
               FogEndRange.Equals(other.FogEndRange) &&
               SkyHorizonDarkColors.Equals(other.SkyHorizonDarkColors) &&
               SkyLightColors.Equals(other.SkyLightColors) &&
               SkyDarkColors.Equals(other.SkyDarkColors) &&
               SunColors.Equals(other.SunColors) &&
               HaloColors.Equals(other.HaloColors) &&
               SunDarkCloudColors.Equals(other.SunDarkCloudColors) &&
               SunLightCloudColors.Equals(other.SunLightCloudColors) &&
               HorizonDarkCloudColors.Equals(other.HorizonDarkCloudColors) &&
               HorizonLightCloudColors.Equals(other.HorizonLightCloudColors) &&
               CloudShadowCloudColors.Equals(other.CloudShadowCloudColors) &&
               PointOfInterestId == other.PointOfInterestId &&
               BloomThresholds.Equals(other.BloomThresholds) &&
               BloomIntensities.Equals(other.BloomIntensities) &&
               SunriseTime.Equals(other.SunriseTime) &&
               SunsetTime.Equals(other.SunsetTime) &&
               DenseFogColors.Equals(other.DenseFogColors) &&
               DenseFogStartRange.Equals(other.DenseFogStartRange) &&
               DenseFogEndRange.Equals(other.DenseFogEndRange) &&
               MoonRadiusMultipliers.Equals(other.MoonRadiusMultipliers) &&
               SunRadiusMultipliers.Equals(other.SunRadiusMultipliers) &&
               StarsAppearTime.Equals(other.StarsAppearTime) &&
               StarsDisappearTime.Equals(other.StarsDisappearTime) &&
               RemapTimeline == other.RemapTimeline;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as ColorTimeline);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Version);
        hash.Add(AmbientColors);
        hash.Add(DirectionalColors);
        hash.Add(ShadowColors);
        hash.Add(SkyHorizonColors);
        hash.Add(FogStartRange);
        hash.Add(FogEndRange);
        hash.Add(SkyHorizonDarkColors);
        hash.Add(SkyLightColors);
        hash.Add(SkyDarkColors);
        hash.Add(SunColors);
        hash.Add(HaloColors);
        hash.Add(SunDarkCloudColors);
        hash.Add(SunLightCloudColors);
        hash.Add(HorizonDarkCloudColors);
        hash.Add(HorizonLightCloudColors);
        hash.Add(CloudShadowCloudColors);
        hash.Add(PointOfInterestId);
        hash.Add(BloomThresholds);
        hash.Add(BloomIntensities);
        hash.Add(SunriseTime);
        hash.Add(SunsetTime);
        hash.Add(DenseFogColors);
        hash.Add(DenseFogStartRange);
        hash.Add(DenseFogEndRange);
        hash.Add(MoonRadiusMultipliers);
        hash.Add(SunRadiusMultipliers);
        hash.Add(StarsAppearTime);
        hash.Add(StarsDisappearTime);
        hash.Add(RemapTimeline);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => $"ColorTimeline v{Version} (POI: {PointOfInterestId})";
}
