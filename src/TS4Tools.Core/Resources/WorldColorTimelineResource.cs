using System.Globalization;
using TS4Tools.Core.Common;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Implementation of World Color Timeline resource for managing day/night lighting cycles.
/// </summary>
/// <remarks>
/// WorldColorTimelineResource handles environmental lighting data including:
/// - 29 distinct color timeline properties for various lighting effects
/// - Day/night cycle transitions with interpolation
/// - Weather-based lighting modifications
/// - Point-of-interest specific lighting settings
/// - Version 13/14 format compatibility
/// </remarks>
internal class WorldColorTimelineResource : ResourceBase, IWorldColorTimelineResource
{
    private const uint SupportedVersion13 = 13;
    private const uint SupportedVersion14 = 14;

    private readonly List<IColorTimeline> _colorTimelines;

    public uint Version { get; set; } = SupportedVersion14;

    public IReadOnlyList<IColorTimeline> ColorTimelines => _colorTimelines.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the WorldColorTimelineResource class.
    /// </summary>
    internal WorldColorTimelineResource()
    {
        _colorTimelines = new List<IColorTimeline>();
    }

    /// <summary>
    /// Initializes a new instance with stream data.
    /// </summary>
    /// <param name="stream">The stream containing the resource data.</param>
    internal WorldColorTimelineResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        Load(stream);
    }

    public void AddColorTimeline(IColorTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        _colorTimelines.Add(timeline);
        OnResourceChanged();
    }

    public bool RemoveColorTimeline(IColorTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var removed = _colorTimelines.Remove(timeline);
        if (removed)
        {
            OnResourceChanged();
        }
        return removed;
    }

    public void ClearColorTimelines()
    {
        if (_colorTimelines.Count > 0)
        {
            _colorTimelines.Clear();
            OnResourceChanged();
        }
    }

    protected override void Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        Version = reader.ReadUInt32();

        if (Version != SupportedVersion13 && Version != SupportedVersion14)
        {
            throw new InvalidOperationException(
                $"Unsupported WorldColorTimeline version: {Version}. " +
                $"Supported versions are {SupportedVersion13} and {SupportedVersion14}.");
        }

        var timelineCount = reader.ReadUInt32();
        _colorTimelines.Clear();

        for (int i = 0; i < timelineCount; i++)
        {
            var timeline = LoadColorTimeline(reader);
            _colorTimelines.Add(timeline);
        }
    }

    private IColorTimeline LoadColorTimeline(BinaryReader reader)
    {
        var timeline = new ColorTimeline
        {
            Version = reader.ReadUInt32()
        };

        // Load all 29 color timeline properties in the legacy order
        timeline.AmbientColors = LoadColorTimelineData(reader);
        timeline.DirectionalColors = LoadColorTimelineData(reader);
        timeline.ShadowColors = LoadColorTimelineData(reader);
        timeline.SkyHorizonColors = LoadColorTimelineData(reader);
        timeline.FogStartRange = LoadColorTimelineData(reader);
        timeline.FogEndRange = LoadColorTimelineData(reader);
        timeline.SkyHorizonDarkColors = LoadColorTimelineData(reader);
        timeline.SkyLightColors = LoadColorTimelineData(reader);
        timeline.SkyDarkColors = LoadColorTimelineData(reader);
        timeline.SunColors = LoadColorTimelineData(reader);
        timeline.HaloColors = LoadColorTimelineData(reader);
        timeline.SunDarkCloudColors = LoadColorTimelineData(reader);
        timeline.SunLightCloudColors = LoadColorTimelineData(reader);
        timeline.HorizonDarkCloudColors = LoadColorTimelineData(reader);
        timeline.HorizonLightCloudColors = LoadColorTimelineData(reader);
        timeline.CloudShadowCloudColors = LoadColorTimelineData(reader);

        timeline.PointOfInterestId = reader.ReadUInt32();

        timeline.BloomThresholds = LoadColorTimelineData(reader);
        timeline.BloomIntensities = LoadColorTimelineData(reader);

        timeline.SunriseTime = reader.ReadSingle();
        timeline.SunsetTime = reader.ReadSingle();

        timeline.DenseFogColors = LoadColorTimelineData(reader);
        timeline.DenseFogStartRange = LoadColorTimelineData(reader);
        timeline.DenseFogEndRange = LoadColorTimelineData(reader);
        timeline.MoonRadiusMultipliers = LoadColorTimelineData(reader);
        timeline.SunRadiusMultipliers = LoadColorTimelineData(reader);

        timeline.StarsAppearTime = reader.ReadSingle();
        timeline.StarsDisappearTime = reader.ReadSingle();

        // Version 14+ features
        if (Version >= SupportedVersion14 && timeline.Version >= SupportedVersion14)
        {
            timeline.RemapTimeline = reader.ReadBoolean();
        }

        return timeline;
    }

    private static IColorTimelineData LoadColorTimelineData(BinaryReader reader)
    {
        var colorData = new ColorTimelineData();
        var pointCount = reader.ReadUInt32();

        for (int i = 0; i < pointCount; i++)
        {
            var colorPoint = new ColorData
            {
                Red = reader.ReadSingle(),
                Green = reader.ReadSingle(),
                Blue = reader.ReadSingle(),
                Alpha = reader.ReadSingle(),
                Time = reader.ReadSingle()
            };
            colorData.AddColorPoint(colorPoint);
        }

        return colorData;
    }

    protected override void Save(Stream stream)
    {
        using var writer = new BinaryWriter(stream);

        writer.Write(Version);
        writer.Write((uint)_colorTimelines.Count);

        foreach (var timeline in _colorTimelines)
        {
            SaveColorTimeline(writer, timeline);
        }
    }

    private void SaveColorTimeline(BinaryWriter writer, IColorTimeline timeline)
    {
        writer.Write(timeline.Version);

        // Save all color timeline properties in legacy order
        SaveColorTimelineData(writer, timeline.AmbientColors);
        SaveColorTimelineData(writer, timeline.DirectionalColors);
        SaveColorTimelineData(writer, timeline.ShadowColors);
        SaveColorTimelineData(writer, timeline.SkyHorizonColors);
        SaveColorTimelineData(writer, timeline.FogStartRange);
        SaveColorTimelineData(writer, timeline.FogEndRange);
        SaveColorTimelineData(writer, timeline.SkyHorizonDarkColors);
        SaveColorTimelineData(writer, timeline.SkyLightColors);
        SaveColorTimelineData(writer, timeline.SkyDarkColors);
        SaveColorTimelineData(writer, timeline.SunColors);
        SaveColorTimelineData(writer, timeline.HaloColors);
        SaveColorTimelineData(writer, timeline.SunDarkCloudColors);
        SaveColorTimelineData(writer, timeline.SunLightCloudColors);
        SaveColorTimelineData(writer, timeline.HorizonDarkCloudColors);
        SaveColorTimelineData(writer, timeline.HorizonLightCloudColors);
        SaveColorTimelineData(writer, timeline.CloudShadowCloudColors);

        writer.Write(timeline.PointOfInterestId);

        SaveColorTimelineData(writer, timeline.BloomThresholds);
        SaveColorTimelineData(writer, timeline.BloomIntensities);

        writer.Write(timeline.SunriseTime);
        writer.Write(timeline.SunsetTime);

        SaveColorTimelineData(writer, timeline.DenseFogColors);
        SaveColorTimelineData(writer, timeline.DenseFogStartRange);
        SaveColorTimelineData(writer, timeline.DenseFogEndRange);
        SaveColorTimelineData(writer, timeline.MoonRadiusMultipliers);
        SaveColorTimelineData(writer, timeline.SunRadiusMultipliers);

        writer.Write(timeline.StarsAppearTime);
        writer.Write(timeline.StarsDisappearTime);

        // Version 14+ features
        if (Version >= SupportedVersion14 && timeline.Version >= SupportedVersion14)
        {
            writer.Write(timeline.RemapTimeline);
        }
    }

    private static void SaveColorTimelineData(BinaryWriter writer, IColorTimelineData colorData)
    {
        writer.Write((uint)colorData.ColorPoints.Count);

        foreach (var point in colorData.ColorPoints)
        {
            writer.Write(point.Red);
            writer.Write(point.Green);
            writer.Write(point.Blue);
            writer.Write(point.Alpha);
            writer.Write(point.Time);
        }
    }

    public override string ToString()
    {
        return $"WorldColorTimelineResource (v{Version}) - {_colorTimelines.Count} timelines";
    }
}

/// <summary>
/// Implementation of a color timeline containing all environmental lighting components.
/// </summary>
internal class ColorTimeline : IColorTimeline
{
    public uint Version { get; set; } = 14;
    public IColorTimelineData AmbientColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData DirectionalColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData ShadowColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SkyHorizonColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData FogStartRange { get; set; } = new ColorTimelineData();
    public IColorTimelineData FogEndRange { get; set; } = new ColorTimelineData();
    public IColorTimelineData SkyHorizonDarkColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SkyLightColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SkyDarkColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SunColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData HaloColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SunDarkCloudColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData SunLightCloudColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData HorizonDarkCloudColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData HorizonLightCloudColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData CloudShadowCloudColors { get; set; } = new ColorTimelineData();
    public uint PointOfInterestId { get; set; }
    public IColorTimelineData BloomThresholds { get; set; } = new ColorTimelineData();
    public IColorTimelineData BloomIntensities { get; set; } = new ColorTimelineData();
    public float SunriseTime { get; set; }
    public float SunsetTime { get; set; }
    public IColorTimelineData DenseFogColors { get; set; } = new ColorTimelineData();
    public IColorTimelineData DenseFogStartRange { get; set; } = new ColorTimelineData();
    public IColorTimelineData DenseFogEndRange { get; set; } = new ColorTimelineData();
    public IColorTimelineData MoonRadiusMultipliers { get; set; } = new ColorTimelineData();
    public IColorTimelineData SunRadiusMultipliers { get; set; } = new ColorTimelineData();
    public float StarsAppearTime { get; set; }
    public float StarsDisappearTime { get; set; }
    public bool RemapTimeline { get; set; }
}

/// <summary>
/// Implementation of color timeline data with interpolation support.
/// </summary>
internal class ColorTimelineData : IColorTimelineData
{
    private readonly List<IColorData> _colorPoints;

    public IReadOnlyList<IColorData> ColorPoints => _colorPoints.AsReadOnly();

    internal ColorTimelineData()
    {
        _colorPoints = new List<IColorData>();
    }

    public void AddColorPoint(IColorData colorData)
    {
        ArgumentNullException.ThrowIfNull(colorData);
        _colorPoints.Add(colorData);
        // Keep points sorted by time for interpolation
        _colorPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public bool RemoveColorPoint(IColorData colorData)
    {
        ArgumentNullException.ThrowIfNull(colorData);
        return _colorPoints.Remove(colorData);
    }

    public void ClearColorPoints()
    {
        _colorPoints.Clear();
    }

    public IColorData GetColorAtTime(float time)
    {
        if (_colorPoints.Count == 0)
        {
            return new ColorData { Time = time };
        }

        if (_colorPoints.Count == 1)
        {
            return new ColorData
            {
                Red = _colorPoints[0].Red,
                Green = _colorPoints[0].Green,
                Blue = _colorPoints[0].Blue,
                Alpha = _colorPoints[0].Alpha,
                Time = time
            };
        }

        // Find the two points to interpolate between
        for (int i = 0; i < _colorPoints.Count - 1; i++)
        {
            var current = _colorPoints[i];
            var next = _colorPoints[i + 1];

            if (time >= current.Time && time <= next.Time)
            {
                // Linear interpolation
                float t = (time - current.Time) / (next.Time - current.Time);

                return new ColorData
                {
                    Red = Lerp(current.Red, next.Red, t),
                    Green = Lerp(current.Green, next.Green, t),
                    Blue = Lerp(current.Blue, next.Blue, t),
                    Alpha = Lerp(current.Alpha, next.Alpha, t),
                    Time = time
                };
            }
        }

        // Return the closest endpoint
        return time < _colorPoints[0].Time ?
            CloneColorData(_colorPoints[0], time) :
            CloneColorData(_colorPoints[^1], time);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static IColorData CloneColorData(IColorData source, float newTime)
    {
        return new ColorData
        {
            Red = source.Red,
            Green = source.Green,
            Blue = source.Blue,
            Alpha = source.Alpha,
            Time = newTime
        };
    }
}

/// <summary>
/// Implementation of a single color data point.
/// </summary>
internal class ColorData : IColorData
{
    public float Red { get; set; }
    public float Green { get; set; }
    public float Blue { get; set; }
    public float Alpha { get; set; } = 1.0f;
    public float Time { get; set; }

    public override string ToString()
    {
        return $"RGBA({Red:F3}, {Green:F3}, {Blue:F3}, {Alpha:F3}) @ {Time:F3}";
    }
}
