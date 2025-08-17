/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a World Color Timeline resource that defines day/night lighting cycles,
/// weather patterns, and environmental color transitions in The Sims 4.
/// Modern async implementation of the legacy WorldColorTimeLineResource (0x19301120).
/// </summary>
public sealed class WorldColorTimelineResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private readonly List<ColorTimeline> _colorTimelines;
    private uint _version = 1;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this world color timeline.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets or sets the resource version. Latest version supports timeline remapping.
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                _isDirty = true;
                OnResourceChanged();
            }
        }
    }

    /// <summary>
    /// Gets the collection of color timelines for different environmental elements.
    /// </summary>
    public IReadOnlyList<ColorTimeline> ColorTimelines => _colorTimelines.AsReadOnly();

    /// <summary>
    /// Gets or sets whether the resource has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => _isDirty = value;
    }

    /// <summary>
    /// Initializes a new instance of the WorldColorTimelineResource class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public WorldColorTimelineResource(ResourceKey key, uint version = 1)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _version = version;
        _colorTimelines = new List<ColorTimeline>();
    }

    /// <summary>
    /// Loads world color timeline data from the specified stream asynchronously.
    /// Implements the legacy binary format for full compatibility.
    /// </summary>
    /// <param name="stream">The stream containing color timeline data.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid data.</exception>
    public async Task LoadFromStreamAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Handle empty stream - create default timeline
        if (stream.Length == 0)
        {
            _colorTimelines.Clear();
            _colorTimelines.Add(CreateDefaultColorTimeline());
            _isDirty = true;
            OnResourceChanged();
            return;
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        try
        {
            // Read header
            _version = reader.ReadUInt32();

            // Read color timeline count
            var timelineCount = reader.ReadUInt32();
            _colorTimelines.Clear();

            // Read each color timeline
            for (uint i = 0; i < timelineCount; i++)
            {
                var timeline = await ReadColorTimelineAsync(reader);
                _colorTimelines.Add(timeline);
            }

            _isDirty = false;
            OnResourceChanged();
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"Failed to parse WorldColorTimelineResource: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves the world color timeline data to the specified stream asynchronously.
    /// Maintains byte-perfect compatibility with legacy format.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public async Task SaveToStreamAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(_version);

        // Write color timeline count
        writer.Write((uint)_colorTimelines.Count);

        // Write each color timeline
        foreach (var timeline in _colorTimelines)
        {
            await WriteColorTimelineAsync(writer, timeline);
        }

        _isDirty = false;
        OnResourceChanged();
    }

    /// <summary>
    /// Adds a color timeline to the resource.
    /// </summary>
    /// <param name="timeline">The color timeline to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when timeline is null.</exception>
    public void AddColorTimeline(ColorTimeline timeline)
    {
        if (timeline == null)
            throw new ArgumentNullException(nameof(timeline));

        _colorTimelines.Add(timeline);
        _isDirty = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a color timeline from the resource.
    /// </summary>
    /// <param name="timeline">The color timeline to remove.</param>
    /// <returns>True if the timeline was removed; otherwise, false.</returns>
    public bool RemoveColorTimeline(ColorTimeline timeline)
    {
        var removed = _colorTimelines.Remove(timeline);
        if (removed)
        {
            _isDirty = true;
            OnResourceChanged();
        }
        return removed;
    }

    /// <summary>
    /// Creates a default color timeline with standard day/night cycle values.
    /// </summary>
    /// <returns>A default color timeline.</returns>
    public static ColorTimeline CreateDefaultColorTimeline()
    {
        var timeline = new ColorTimeline
        {
            Version = 14,
            SunriseTime = 6.0f,
            SunsetTime = 18.0f,
            StarsAppearTime = 19.0f,
            StarsDisappearTime = 5.0f,
            PointOfInterestId = 0,
            RemapTimeline = true
        };

        // Populate collections with default data
        var defaultData = CreateDefaultColorTimelineData();
        foreach (var item in defaultData) timeline.AmbientColors.Add(item);
        foreach (var item in defaultData) timeline.DirectionalColors.Add(item);
        foreach (var item in defaultData) timeline.ShadowColors.Add(item);
        foreach (var item in defaultData) timeline.SkyHorizonColors.Add(item);
        foreach (var item in defaultData) timeline.FogStartRange.Add(item);
        foreach (var item in defaultData) timeline.FogEndRange.Add(item);

        return timeline;
    }

    /// <summary>
    /// Creates default color timeline data with standard values.
    /// </summary>
    /// <returns>Default color timeline data.</returns>
    private static List<ColorDataPoint> CreateDefaultColorTimelineData()
    {
        return new List<ColorDataPoint>
        {
            new ColorDataPoint { R = 1.0f, G = 1.0f, B = 1.0f, A = 1.0f, Time = 0.0f },
            new ColorDataPoint { R = 1.0f, G = 1.0f, B = 1.0f, A = 1.0f, Time = 1.0f }
        };
    }

    /// <summary>
    /// Reads a color timeline from the binary reader asynchronously.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>A task that returns the color timeline.</returns>
    private async Task<ColorTimeline> ReadColorTimelineAsync(BinaryReader reader)
    {
        var timeline = new ColorTimeline
        {
            Version = reader.ReadUInt32()
        };

        // Read color data collections (order matches legacy format)
        var ambientColors = await ReadColorTimelineDataAsync(reader);
        timeline.AmbientColors.Clear();
        foreach (var item in ambientColors) timeline.AmbientColors.Add(item);

        var directionalColors = await ReadColorTimelineDataAsync(reader);
        timeline.DirectionalColors.Clear();
        foreach (var item in directionalColors) timeline.DirectionalColors.Add(item);

        var shadowColors = await ReadColorTimelineDataAsync(reader);
        timeline.ShadowColors.Clear();
        foreach (var item in shadowColors) timeline.ShadowColors.Add(item);

        var skyHorizonColors = await ReadColorTimelineDataAsync(reader);
        timeline.SkyHorizonColors.Clear();
        foreach (var item in skyHorizonColors) timeline.SkyHorizonColors.Add(item);

        var fogStartRange = await ReadColorTimelineDataAsync(reader);
        timeline.FogStartRange.Clear();
        foreach (var item in fogStartRange) timeline.FogStartRange.Add(item);

        var fogEndRange = await ReadColorTimelineDataAsync(reader);
        timeline.FogEndRange.Clear();
        foreach (var item in fogEndRange) timeline.FogEndRange.Add(item);

        var skyHorizonDarkColors = await ReadColorTimelineDataAsync(reader);
        timeline.SkyHorizonDarkColors.Clear();
        foreach (var item in skyHorizonDarkColors) timeline.SkyHorizonDarkColors.Add(item);

        var skyLightColors = await ReadColorTimelineDataAsync(reader);
        timeline.SkyLightColors.Clear();
        foreach (var item in skyLightColors) timeline.SkyLightColors.Add(item);

        var skyDarkColors = await ReadColorTimelineDataAsync(reader);
        timeline.SkyDarkColors.Clear();
        foreach (var item in skyDarkColors) timeline.SkyDarkColors.Add(item);

        var sunColors = await ReadColorTimelineDataAsync(reader);
        timeline.SunColors.Clear();
        foreach (var item in sunColors) timeline.SunColors.Add(item);

        var haloColors = await ReadColorTimelineDataAsync(reader);
        timeline.HaloColors.Clear();
        foreach (var item in haloColors) timeline.HaloColors.Add(item);

        var sunDarkCloudColors = await ReadColorTimelineDataAsync(reader);
        timeline.SunDarkCloudColors.Clear();
        foreach (var item in sunDarkCloudColors) timeline.SunDarkCloudColors.Add(item);

        var sunLightCloudColors = await ReadColorTimelineDataAsync(reader);
        timeline.SunLightCloudColors.Clear();
        foreach (var item in sunLightCloudColors) timeline.SunLightCloudColors.Add(item);

        var horizonDarkCloudColors = await ReadColorTimelineDataAsync(reader);
        timeline.HorizonDarkCloudColors.Clear();
        foreach (var item in horizonDarkCloudColors) timeline.HorizonDarkCloudColors.Add(item);

        var horizonLightCloudColors = await ReadColorTimelineDataAsync(reader);
        timeline.HorizonLightCloudColors.Clear();
        foreach (var item in horizonLightCloudColors) timeline.HorizonLightCloudColors.Add(item);

        var cloudShadowCloudColors = await ReadColorTimelineDataAsync(reader);
        timeline.CloudShadowCloudColors.Clear();
        foreach (var item in cloudShadowCloudColors) timeline.CloudShadowCloudColors.Add(item);

        // Point of interest ID
        timeline.PointOfInterestId = reader.ReadUInt32();

        // Bloom settings
        var bloomThresholds = await ReadColorTimelineDataAsync(reader);
        timeline.BloomThresholds.Clear();
        foreach (var item in bloomThresholds) timeline.BloomThresholds.Add(item);

        var bloomIntensities = await ReadColorTimelineDataAsync(reader);
        timeline.BloomIntensities.Clear();
        foreach (var item in bloomIntensities) timeline.BloomIntensities.Add(item);

        // Sun/moon timing
        timeline.SunriseTime = reader.ReadSingle();
        timeline.SunsetTime = reader.ReadSingle();

        // Fog settings
        var denseFogColors = await ReadColorTimelineDataAsync(reader);
        timeline.DenseFogColors.Clear();
        foreach (var item in denseFogColors) timeline.DenseFogColors.Add(item);

        var denseFogStartRange = await ReadColorTimelineDataAsync(reader);
        timeline.DenseFogStartRange.Clear();
        foreach (var item in denseFogStartRange) timeline.DenseFogStartRange.Add(item);

        var denseFogEndRange = await ReadColorTimelineDataAsync(reader);
        timeline.DenseFogEndRange.Clear();
        foreach (var item in denseFogEndRange) timeline.DenseFogEndRange.Add(item);

        // Celestial body settings
        var moonRadiusMultipliers = await ReadColorTimelineDataAsync(reader);
        timeline.MoonRadiusMultipliers.Clear();
        foreach (var item in moonRadiusMultipliers) timeline.MoonRadiusMultipliers.Add(item);

        var sunRadiusMultipliers = await ReadColorTimelineDataAsync(reader);
        timeline.SunRadiusMultipliers.Clear();
        foreach (var item in sunRadiusMultipliers) timeline.SunRadiusMultipliers.Add(item);

        // Stars timing
        timeline.StarsAppearTime = reader.ReadSingle();
        timeline.StarsDisappearTime = reader.ReadSingle();

        // Version 14+ features
        if (timeline.Version >= 14)
        {
            timeline.RemapTimeline = reader.ReadBoolean();
        }

        return timeline;
    }

    /// <summary>
    /// Reads color timeline data (collection of color data points) from the binary reader.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>A task that returns the color timeline data.</returns>
    private async Task<List<ColorDataPoint>> ReadColorTimelineDataAsync(BinaryReader reader)
    {
        var count = reader.ReadUInt32();
        var dataPoints = new List<ColorDataPoint>();

        for (uint i = 0; i < count; i++)
        {
            var colorData = new ColorDataPoint
            {
                R = reader.ReadSingle(),
                G = reader.ReadSingle(),
                B = reader.ReadSingle(),
                A = reader.ReadSingle(),
                Time = reader.ReadSingle()
            };
            dataPoints.Add(colorData);
        }

        return await Task.FromResult(dataPoints);
    }

    /// <summary>
    /// Writes a color timeline to the binary writer asynchronously.
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    /// <param name="timeline">The color timeline to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private async Task WriteColorTimelineAsync(BinaryWriter writer, ColorTimeline timeline)
    {
        writer.Write(timeline.Version);

        // Write color data collections in exact legacy order
        await WriteColorTimelineDataAsync(writer, timeline.AmbientColors);
        await WriteColorTimelineDataAsync(writer, timeline.DirectionalColors);
        await WriteColorTimelineDataAsync(writer, timeline.ShadowColors);
        await WriteColorTimelineDataAsync(writer, timeline.SkyHorizonColors);
        await WriteColorTimelineDataAsync(writer, timeline.FogStartRange);
        await WriteColorTimelineDataAsync(writer, timeline.FogEndRange);
        await WriteColorTimelineDataAsync(writer, timeline.SkyHorizonDarkColors);
        await WriteColorTimelineDataAsync(writer, timeline.SkyLightColors);
        await WriteColorTimelineDataAsync(writer, timeline.SkyDarkColors);
        await WriteColorTimelineDataAsync(writer, timeline.SunColors);
        await WriteColorTimelineDataAsync(writer, timeline.HaloColors);
        await WriteColorTimelineDataAsync(writer, timeline.SunDarkCloudColors);
        await WriteColorTimelineDataAsync(writer, timeline.SunLightCloudColors);
        await WriteColorTimelineDataAsync(writer, timeline.HorizonDarkCloudColors);
        await WriteColorTimelineDataAsync(writer, timeline.HorizonLightCloudColors);
        await WriteColorTimelineDataAsync(writer, timeline.CloudShadowCloudColors);

        writer.Write(timeline.PointOfInterestId);

        await WriteColorTimelineDataAsync(writer, timeline.BloomThresholds);
        await WriteColorTimelineDataAsync(writer, timeline.BloomIntensities);

        writer.Write(timeline.SunriseTime);
        writer.Write(timeline.SunsetTime);

        await WriteColorTimelineDataAsync(writer, timeline.DenseFogColors);
        await WriteColorTimelineDataAsync(writer, timeline.DenseFogStartRange);
        await WriteColorTimelineDataAsync(writer, timeline.DenseFogEndRange);
        await WriteColorTimelineDataAsync(writer, timeline.MoonRadiusMultipliers);
        await WriteColorTimelineDataAsync(writer, timeline.SunRadiusMultipliers);

        writer.Write(timeline.StarsAppearTime);
        writer.Write(timeline.StarsDisappearTime);

        if (timeline.Version >= 14)
        {
            writer.Write(timeline.RemapTimeline);
        }
    }

    /// <summary>
    /// Writes color timeline data to the binary writer asynchronously.
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    /// <param name="dataPoints">The color data points to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private async Task WriteColorTimelineDataAsync(BinaryWriter writer, IList<ColorDataPoint> dataPoints)
    {
        writer.Write((uint)dataPoints.Count);

        foreach (var dataPoint in dataPoints)
        {
            writer.Write(dataPoint.R);
            writer.Write(dataPoint.G);
            writer.Write(dataPoint.B);
            writer.Write(dataPoint.A);
            writer.Write(dataPoint.Time);
        }

        await Task.CompletedTask;
    }

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            var stream = new MemoryStream();
            SaveToStreamAsync(stream).GetAwaiter().GetResult();
            stream.Position = 0;
            return stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            using var stream = new MemoryStream();
            SaveToStreamAsync(stream).GetAwaiter().GetResult();
            return stream.ToArray();
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public int RequestedApiVersion => 1;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public bool IsEditable { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Version),
        nameof(ColorTimelines),
        nameof(IsEditable)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(ColorTimelines) => new TypedValue(typeof(IReadOnlyList<ColorTimeline>), ColorTimelines),
            nameof(IsEditable) => new TypedValue(typeof(bool), IsEditable),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("WorldColorTimelineResource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(ColorTimelines)],
            2 => this[nameof(IsEditable)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-2, got {index}")
        };
        set => throw new NotSupportedException("WorldColorTimelineResource fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Returns a string representation of the resource.
    /// </summary>
    /// <returns>A string representation of the resource.</returns>
    public override string ToString()
    {
        return $"WorldColorTimelineResource v{Version} ({_colorTimelines.Count} timelines)";
    }

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Releases all resources used by the WorldColorTimelineResource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a color timeline that defines lighting and environmental color changes over time.
/// Contains multiple color data collections for different environmental elements.
/// </summary>
public class ColorTimeline
{
    /// <summary>
    /// Gets or sets the timeline version. Version 14+ supports timeline remapping.
    /// </summary>
    public uint Version { get; set; } = 14;

    /// <summary>
    /// Gets the ambient lighting color timeline.
    /// </summary>
    public IList<ColorDataPoint> AmbientColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the directional lighting color timeline.
    /// </summary>
    public IList<ColorDataPoint> DirectionalColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the shadow color timeline.
    /// </summary>
    public IList<ColorDataPoint> ShadowColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sky horizon color timeline.
    /// </summary>
    public IList<ColorDataPoint> SkyHorizonColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the fog start range timeline.
    /// </summary>
    public IList<ColorDataPoint> FogStartRange { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the fog end range timeline.
    /// </summary>
    public IList<ColorDataPoint> FogEndRange { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sky horizon dark colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SkyHorizonDarkColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sky light colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SkyLightColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sky dark colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SkyDarkColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sun colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SunColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the halo colors timeline.
    /// </summary>
    public IList<ColorDataPoint> HaloColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sun dark cloud colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SunDarkCloudColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sun light cloud colors timeline.
    /// </summary>
    public IList<ColorDataPoint> SunLightCloudColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the horizon dark cloud colors timeline.
    /// </summary>
    public IList<ColorDataPoint> HorizonDarkCloudColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the horizon light cloud colors timeline.
    /// </summary>
    public IList<ColorDataPoint> HorizonLightCloudColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the cloud shadow cloud colors timeline.
    /// </summary>
    public IList<ColorDataPoint> CloudShadowCloudColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets or sets the point of interest ID associated with this timeline.
    /// </summary>
    public uint PointOfInterestId { get; set; }

    /// <summary>
    /// Gets the bloom threshold timeline.
    /// </summary>
    public IList<ColorDataPoint> BloomThresholds { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the bloom intensity timeline.
    /// </summary>
    public IList<ColorDataPoint> BloomIntensities { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets or sets the sunrise time (in hours, 0-24).
    /// </summary>
    public float SunriseTime { get; set; } = 6.0f;

    /// <summary>
    /// Gets or sets the sunset time (in hours, 0-24).
    /// </summary>
    public float SunsetTime { get; set; } = 18.0f;

    /// <summary>
    /// Gets the dense fog colors timeline.
    /// </summary>
    public IList<ColorDataPoint> DenseFogColors { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the dense fog start range timeline.
    /// </summary>
    public IList<ColorDataPoint> DenseFogStartRange { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the dense fog end range timeline.
    /// </summary>
    public IList<ColorDataPoint> DenseFogEndRange { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the moon radius multipliers timeline.
    /// </summary>
    public IList<ColorDataPoint> MoonRadiusMultipliers { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets the sun radius multipliers timeline.
    /// </summary>
    public IList<ColorDataPoint> SunRadiusMultipliers { get; } = new List<ColorDataPoint>();

    /// <summary>
    /// Gets or sets the time when stars appear (in hours, 0-24).
    /// </summary>
    public float StarsAppearTime { get; set; } = 19.0f;

    /// <summary>
    /// Gets or sets the time when stars disappear (in hours, 0-24).
    /// </summary>
    public float StarsDisappearTime { get; set; } = 5.0f;

    /// <summary>
    /// Gets or sets whether to remap the timeline. Available in version 14+.
    /// </summary>
    public bool RemapTimeline { get; set; } = true;
}

/// <summary>
/// Represents a single color data point in a timeline with RGBA values and time.
/// </summary>
public class ColorDataPoint
{
    /// <summary>
    /// Gets or sets the red color component (0.0-1.0).
    /// </summary>
    public float R { get; set; }

    /// <summary>
    /// Gets or sets the green color component (0.0-1.0).
    /// </summary>
    public float G { get; set; }

    /// <summary>
    /// Gets or sets the blue color component (0.0-1.0).
    /// </summary>
    public float B { get; set; }

    /// <summary>
    /// Gets or sets the alpha component (0.0-1.0).
    /// </summary>
    public float A { get; set; }

    /// <summary>
    /// Gets or sets the time value (0.0-1.0, representing 0-24 hours).
    /// </summary>
    public float Time { get; set; }

    /// <summary>
    /// Returns a string representation of the color data point.
    /// </summary>
    /// <returns>A string representation of the color data point.</returns>
    public override string ToString()
    {
        return $"R: {R:F3}, G: {G:F3}, B: {B:F3}, A: {A:F3}, Time: {Time:F3}";
    }
}
