using TS4Tools.Resources;

namespace TS4Tools.Wrappers.MiscResource;

/// <summary>
/// World Color Timeline resource (0x19301120).
/// Contains color data for world lighting, fog, sky, sun, clouds, etc. over a day/night cycle.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/WorldColorTimelineResource.cs lines 30-558
/// </summary>
public sealed class WorldColorTimelineResource : TypedResource
{
    /// <summary>
    /// Type ID for World Color Timeline resources.
    /// </summary>
    public const uint TypeId = 0x19301120;

    #region Properties

    /// <summary>
    /// Resource version.
    /// Source: WorldColorTimelineResource.cs line 39
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// List of color timelines.
    /// Source: WorldColorTimelineResource.cs line 40
    /// </summary>
    public List<ColorTimeline> ColorTimelines { get; set; } = [];

    #endregion

    /// <summary>
    /// Creates a new World Color Timeline resource.
    /// </summary>
    public WorldColorTimelineResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            InitializeDefaults();
            return;
        }

        int offset = 0;

        // Source: WorldColorTimelineResource.cs lines 50-51
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Parse list count
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count < 0 || count > 10000)
            throw new InvalidDataException($"Invalid timeline count: {count}");

        ColorTimelines = new List<ColorTimeline>(count);

        for (int i = 0; i < count; i++)
        {
            var timeline = ColorTimeline.Parse(data, ref offset);
            ColorTimelines.Add(timeline);
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = GetSerializedSize();
        var buffer = new byte[size];
        int offset = 0;

        // Source: WorldColorTimelineResource.cs lines 57-59
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        // Write list count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), ColorTimelines.Count);
        offset += 4;

        foreach (var timeline in ColorTimelines)
        {
            timeline.WriteTo(buffer.AsSpan(), ref offset);
        }

        return buffer;
    }

    private int GetSerializedSize()
    {
        int size = 4 + 4; // version + count
        foreach (var timeline in ColorTimelines)
        {
            size += timeline.GetSerializedSize();
        }
        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 1;
        ColorTimelines = [];
    }
}

/// <summary>
/// A single color timeline entry with multiple color data series.
/// Source: WorldColorTimelineResource.cs lines 94-387
/// </summary>
public sealed class ColorTimeline
{
    /// <summary>
    /// Timeline version.
    /// </summary>
    public uint Version { get; set; } = 13;

    /// <summary>
    /// Ambient light colors over time.
    /// </summary>
    public ColorTimelineData AmbientColors { get; set; } = new();

    /// <summary>
    /// Directional light colors over time.
    /// </summary>
    public ColorTimelineData DirectionalColors { get; set; } = new();

    /// <summary>
    /// Shadow colors over time.
    /// </summary>
    public ColorTimelineData ShadowColors { get; set; } = new();

    /// <summary>
    /// Sky horizon colors over time.
    /// </summary>
    public ColorTimelineData SkyHorizonColors { get; set; } = new();

    /// <summary>
    /// Fog start range over time.
    /// </summary>
    public ColorTimelineData FogStartRange { get; set; } = new();

    /// <summary>
    /// Fog end range over time.
    /// </summary>
    public ColorTimelineData FogEndRange { get; set; } = new();

    /// <summary>
    /// Sky horizon dark colors over time.
    /// </summary>
    public ColorTimelineData SkyHorizonDarkColors { get; set; } = new();

    /// <summary>
    /// Sky light colors over time.
    /// </summary>
    public ColorTimelineData SkyLightColors { get; set; } = new();

    /// <summary>
    /// Sky dark colors over time.
    /// </summary>
    public ColorTimelineData SkyDarkColors { get; set; } = new();

    /// <summary>
    /// Sun colors over time.
    /// </summary>
    public ColorTimelineData SunColors { get; set; } = new();

    /// <summary>
    /// Halo colors over time.
    /// </summary>
    public ColorTimelineData HaloColors { get; set; } = new();

    /// <summary>
    /// Sun dark cloud colors over time.
    /// </summary>
    public ColorTimelineData SunDarkCloudColors { get; set; } = new();

    /// <summary>
    /// Sun light cloud colors over time.
    /// </summary>
    public ColorTimelineData SunLightCloudColors { get; set; } = new();

    /// <summary>
    /// Horizon dark cloud colors over time.
    /// </summary>
    public ColorTimelineData HorizonDarkCloudColors { get; set; } = new();

    /// <summary>
    /// Horizon light cloud colors over time.
    /// </summary>
    public ColorTimelineData HorizonLightCloudColors { get; set; } = new();

    /// <summary>
    /// Cloud shadow colors over time.
    /// </summary>
    public ColorTimelineData CloudShadowCloudColors { get; set; } = new();

    /// <summary>
    /// Point of interest ID.
    /// </summary>
    public uint PointOfInterestId { get; set; }

    /// <summary>
    /// Bloom thresholds over time.
    /// </summary>
    public ColorTimelineData BloomThresholds { get; set; } = new();

    /// <summary>
    /// Bloom intensities over time.
    /// </summary>
    public ColorTimelineData BloomIntensities { get; set; } = new();

    /// <summary>
    /// Sunrise time (in hours? 0-24).
    /// </summary>
    public float SunriseTime { get; set; }

    /// <summary>
    /// Sunset time (in hours? 0-24).
    /// </summary>
    public float SunsetTime { get; set; }

    /// <summary>
    /// Dense fog colors over time.
    /// </summary>
    public ColorTimelineData DenseFogColors { get; set; } = new();

    /// <summary>
    /// Dense fog start range over time.
    /// </summary>
    public ColorTimelineData DenseFogStartRange { get; set; } = new();

    /// <summary>
    /// Dense fog end range over time.
    /// </summary>
    public ColorTimelineData DenseFogEndRange { get; set; } = new();

    /// <summary>
    /// Moon radius multipliers over time.
    /// </summary>
    public ColorTimelineData MoonRadiusMultipliers { get; set; } = new();

    /// <summary>
    /// Sun radius multipliers over time.
    /// </summary>
    public ColorTimelineData SunRadiusMultipliers { get; set; } = new();

    /// <summary>
    /// Stars appear time.
    /// </summary>
    public float StarsAppearTime { get; set; }

    /// <summary>
    /// Stars disappear time.
    /// </summary>
    public float StarsDisappearTime { get; set; }

    /// <summary>
    /// Whether to remap the timeline (version 14+).
    /// </summary>
    public bool RemapTimeline { get; set; } = true;

    /// <summary>
    /// Parses a ColorTimeline from binary data.
    /// Source: WorldColorTimelineResource.cs lines 203-239
    /// </summary>
    public static ColorTimeline Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var timeline = new ColorTimeline();

        timeline.Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        timeline.AmbientColors = ColorTimelineData.Parse(data, ref offset);
        timeline.DirectionalColors = ColorTimelineData.Parse(data, ref offset);
        timeline.ShadowColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SkyHorizonColors = ColorTimelineData.Parse(data, ref offset);
        timeline.FogStartRange = ColorTimelineData.Parse(data, ref offset);
        timeline.FogEndRange = ColorTimelineData.Parse(data, ref offset);
        timeline.SkyHorizonDarkColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SkyLightColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SkyDarkColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SunColors = ColorTimelineData.Parse(data, ref offset);
        timeline.HaloColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SunDarkCloudColors = ColorTimelineData.Parse(data, ref offset);
        timeline.SunLightCloudColors = ColorTimelineData.Parse(data, ref offset);
        timeline.HorizonDarkCloudColors = ColorTimelineData.Parse(data, ref offset);
        timeline.HorizonLightCloudColors = ColorTimelineData.Parse(data, ref offset);
        timeline.CloudShadowCloudColors = ColorTimelineData.Parse(data, ref offset);

        timeline.PointOfInterestId = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        timeline.BloomThresholds = ColorTimelineData.Parse(data, ref offset);
        timeline.BloomIntensities = ColorTimelineData.Parse(data, ref offset);

        timeline.SunriseTime = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        timeline.SunsetTime = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        timeline.DenseFogColors = ColorTimelineData.Parse(data, ref offset);
        timeline.DenseFogStartRange = ColorTimelineData.Parse(data, ref offset);
        timeline.DenseFogEndRange = ColorTimelineData.Parse(data, ref offset);
        timeline.MoonRadiusMultipliers = ColorTimelineData.Parse(data, ref offset);
        timeline.SunRadiusMultipliers = ColorTimelineData.Parse(data, ref offset);

        timeline.StarsAppearTime = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        timeline.StarsDisappearTime = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        if (timeline.Version >= 14)
        {
            timeline.RemapTimeline = data[offset++] != 0;
        }

        return timeline;
    }

    /// <summary>
    /// Writes the timeline to a buffer.
    /// Source: WorldColorTimelineResource.cs lines 241-277
    /// </summary>
    public void WriteTo(Span<byte> buffer, ref int offset)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Version);
        offset += 4;

        AmbientColors.WriteTo(buffer, ref offset);
        DirectionalColors.WriteTo(buffer, ref offset);
        ShadowColors.WriteTo(buffer, ref offset);
        SkyHorizonColors.WriteTo(buffer, ref offset);
        FogStartRange.WriteTo(buffer, ref offset);
        FogEndRange.WriteTo(buffer, ref offset);
        SkyHorizonDarkColors.WriteTo(buffer, ref offset);
        SkyLightColors.WriteTo(buffer, ref offset);
        SkyDarkColors.WriteTo(buffer, ref offset);
        SunColors.WriteTo(buffer, ref offset);
        HaloColors.WriteTo(buffer, ref offset);
        SunDarkCloudColors.WriteTo(buffer, ref offset);
        SunLightCloudColors.WriteTo(buffer, ref offset);
        HorizonDarkCloudColors.WriteTo(buffer, ref offset);
        HorizonLightCloudColors.WriteTo(buffer, ref offset);
        CloudShadowCloudColors.WriteTo(buffer, ref offset);

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], PointOfInterestId);
        offset += 4;

        BloomThresholds.WriteTo(buffer, ref offset);
        BloomIntensities.WriteTo(buffer, ref offset);

        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], SunriseTime);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], SunsetTime);
        offset += 4;

        DenseFogColors.WriteTo(buffer, ref offset);
        DenseFogStartRange.WriteTo(buffer, ref offset);
        DenseFogEndRange.WriteTo(buffer, ref offset);
        MoonRadiusMultipliers.WriteTo(buffer, ref offset);
        SunRadiusMultipliers.WriteTo(buffer, ref offset);

        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], StarsAppearTime);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], StarsDisappearTime);
        offset += 4;

        if (Version >= 14)
        {
            buffer[offset++] = RemapTimeline ? (byte)1 : (byte)0;
        }
    }

    /// <summary>
    /// Gets the serialized size.
    /// </summary>
    public int GetSerializedSize()
    {
        int size = 4; // version

        // 23 ColorTimelineData fields
        size += AmbientColors.GetSerializedSize();
        size += DirectionalColors.GetSerializedSize();
        size += ShadowColors.GetSerializedSize();
        size += SkyHorizonColors.GetSerializedSize();
        size += FogStartRange.GetSerializedSize();
        size += FogEndRange.GetSerializedSize();
        size += SkyHorizonDarkColors.GetSerializedSize();
        size += SkyLightColors.GetSerializedSize();
        size += SkyDarkColors.GetSerializedSize();
        size += SunColors.GetSerializedSize();
        size += HaloColors.GetSerializedSize();
        size += SunDarkCloudColors.GetSerializedSize();
        size += SunLightCloudColors.GetSerializedSize();
        size += HorizonDarkCloudColors.GetSerializedSize();
        size += HorizonLightCloudColors.GetSerializedSize();
        size += CloudShadowCloudColors.GetSerializedSize();

        size += 4; // PointOfInterestId

        size += BloomThresholds.GetSerializedSize();
        size += BloomIntensities.GetSerializedSize();

        size += 4 + 4; // SunriseTime + SunsetTime

        size += DenseFogColors.GetSerializedSize();
        size += DenseFogStartRange.GetSerializedSize();
        size += DenseFogEndRange.GetSerializedSize();
        size += MoonRadiusMultipliers.GetSerializedSize();
        size += SunRadiusMultipliers.GetSerializedSize();

        size += 4 + 4; // StarsAppearTime + StarsDisappearTime

        if (Version >= 14)
        {
            size += 1; // RemapTimeline
        }

        return size;
    }
}

/// <summary>
/// A list of color data points for a timeline.
/// Source: WorldColorTimelineResource.cs lines 389-437
/// </summary>
public sealed class ColorTimelineData
{
    /// <summary>
    /// The color data points.
    /// </summary>
    public List<TimelineColorData> ColorData { get; set; } = [];

    /// <summary>
    /// Parses ColorTimelineData from binary data.
    /// </summary>
    public static ColorTimelineData Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var result = new ColorTimelineData();

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count < 0 || count > 10000)
            throw new InvalidDataException($"Invalid color data count: {count}");

        result.ColorData = new List<TimelineColorData>(count);
        for (int i = 0; i < count; i++)
        {
            var entry = TimelineColorData.Parse(data, ref offset);
            result.ColorData.Add(entry);
        }

        return result;
    }

    /// <summary>
    /// Writes to a buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer, ref int offset)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], ColorData.Count);
        offset += 4;

        foreach (var entry in ColorData)
        {
            entry.WriteTo(buffer, ref offset);
        }
    }

    /// <summary>
    /// Gets the serialized size.
    /// </summary>
    public int GetSerializedSize()
    {
        return 4 + (ColorData.Count * TimelineColorData.SerializedSize);
    }
}

/// <summary>
/// A single color data point with RGBA and time.
/// Source: WorldColorTimelineResource.cs lines 468-542
/// </summary>
public readonly record struct TimelineColorData(float R, float G, float B, float A, float Time)
{
    /// <summary>
    /// Serialized size: 5 floats = 20 bytes.
    /// </summary>
    public const int SerializedSize = 20;

    /// <summary>
    /// Parses a TimelineColorData from binary data.
    /// Source: WorldColorTimelineResource.cs lines 501-509
    /// </summary>
    public static TimelineColorData Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        float r = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        float g = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        float b = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        float a = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        float time = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        return new TimelineColorData(r, g, b, a, time);
    }

    /// <summary>
    /// Writes to a buffer.
    /// Source: WorldColorTimelineResource.cs lines 511-519
    /// </summary>
    public void WriteTo(Span<byte> buffer, ref int offset)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], R);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], G);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], B);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], A);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], Time);
        offset += 4;
    }
}
