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

using System.ComponentModel;
using System.Numerics;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a region description resource that defines geographical regions within a world.
/// Regions provide boundaries and properties for neighborhoods and districts.
/// </summary>
public sealed class RegionDescriptionResource : IResource, IDisposable, INotifyPropertyChanged
{
    private readonly ResourceKey _key;
    private readonly List<string> _contentFields = new()
    {
        "RegionId",
        "RegionName",
        "Description",
        "Climate",
        "Elevation",
        "BoundaryPoints",
        "AssociatedLots"
    };
    private bool _isDirty = true;
    private bool _disposed;
    private MemoryStream? _stream;
    private string _regionName = string.Empty;
    private string _regionDescription = string.Empty;
    private readonly List<Vector2> _boundaryPoints = new();
    private readonly List<ulong> _associatedLots = new();
    private RegionClimate _climate = RegionClimate.Temperate;
    private float _elevation = 0.0f;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionDescriptionResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    public RegionDescriptionResource(ResourceKey key, uint version)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        _stream = new MemoryStream();

        // Initialize ContentFields for new resources
        _contentFields.AddRange(["RegionName", "RegionDescription", "BoundaryPoints", "AssociatedLots", "Climate", "Elevation"]);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public ResourceKey Key => _key;

    /// <inheritdoc/>
    public uint Version { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource has been modified.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the region name.
    /// </summary>
    public string RegionName
    {
        get => _regionName;
        set
        {
            if (_regionName != value)
            {
                _regionName = value ?? string.Empty;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the region description.
    /// </summary>
    public string RegionDescription
    {
        get => _regionDescription;
        set
        {
            if (_regionDescription != value)
            {
                _regionDescription = value ?? string.Empty;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the boundary points that define the region's geographical boundaries.
    /// </summary>
    public IReadOnlyList<Vector2> BoundaryPoints => _boundaryPoints;

    /// <summary>
    /// Gets the collection of lot instance IDs associated with this region.
    /// </summary>
    public IReadOnlyList<ulong> AssociatedLots => _associatedLots;

    /// <summary>
    /// Gets or sets the climate type for this region.
    /// </summary>
    public RegionClimate Climate
    {
        get => _climate;
        set
        {
            if (_climate != value)
            {
                _climate = value;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the average elevation of the region.
    /// </summary>
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (Math.Abs(_elevation - value) > 0.001f)
            {
                _elevation = value;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    #region IResource Implementation

    /// <inheritdoc/>
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stream ??= new MemoryStream();
        }
    }

    /// <inheritdoc/>
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var ms = new MemoryStream();
            SaveToStreamAsync(ms).ConfigureAwait(false).GetAwaiter().GetResult();
            return ms.ToArray();
        }
    }

    /// <inheritdoc/>
    public event EventHandler? ResourceChanged;

    /// <inheritdoc/>
    public int RequestedApiVersion => 1;

    /// <inheritdoc/>
    public int RecommendedApiVersion => 1;

    /// <inheritdoc/>
    public IReadOnlyList<string> ContentFields => _contentFields;

    /// <inheritdoc/>
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc/>
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    #endregion

    /// <summary>
    /// Adds a boundary point to define the region's shape.
    /// </summary>
    /// <param name="point">The boundary point to add.</param>
    public void AddBoundaryPoint(Vector2 point)
    {
        _boundaryPoints.Add(point);
        IsDirty = true;
        OnPropertyChanged(nameof(BoundaryPoints));
    }

    /// <summary>
    /// Removes a boundary point.
    /// </summary>
    /// <param name="point">The boundary point to remove.</param>
    /// <returns>True if the point was removed; otherwise, false.</returns>
    public bool RemoveBoundaryPoint(Vector2 point)
    {
        var removed = _boundaryPoints.Remove(point);
        if (removed)
        {
            IsDirty = true;
            OnPropertyChanged(nameof(BoundaryPoints));
        }
        return removed;
    }

    /// <summary>
    /// Associates a lot with this region.
    /// </summary>
    /// <param name="lotInstanceId">The lot instance ID to associate.</param>
    public void AssociateLot(ulong lotInstanceId)
    {
        if (!_associatedLots.Contains(lotInstanceId))
        {
            _associatedLots.Add(lotInstanceId);
            IsDirty = true;
            OnPropertyChanged(nameof(AssociatedLots));
        }
    }

    /// <summary>
    /// Disassociates a lot from this region.
    /// </summary>
    /// <param name="lotInstanceId">The lot instance ID to disassociate.</param>
    /// <returns>True if the lot was disassociated; otherwise, false.</returns>
    public bool DisassociateLot(ulong lotInstanceId)
    {
        var removed = _associatedLots.Remove(lotInstanceId);
        if (removed)
        {
            IsDirty = true;
            OnPropertyChanged(nameof(AssociatedLots));
        }
        return removed;
    }

    /// <summary>
    /// Checks if a point is within the region boundaries.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is within the region; otherwise, false.</returns>
    public bool IsPointInRegion(Vector2 point)
    {
        if (_boundaryPoints.Count < 3) return false;

        // Ray casting algorithm for point-in-polygon test
        bool inside = false;
        int j = _boundaryPoints.Count - 1;

        for (int i = 0; i < _boundaryPoints.Count; j = i++)
        {
            var pi = _boundaryPoints[i];
            var pj = _boundaryPoints[j];

            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Loads the region description resource from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Handle null or truly empty stream (no content at all)
        if (stream == null || stream.Length == 0)
        {
            // Initialize with default values for empty region
            _regionName = string.Empty;
            _regionDescription = string.Empty;
            _boundaryPoints.Clear();
            _associatedLots.Clear();
            _climate = RegionClimate.Temperate;
            _elevation = 0.0f;
            _contentFields.Clear();
            _contentFields.AddRange(["RegionName", "RegionDescription", "BoundaryPoints", "AssociatedLots", "Climate", "Elevation"]);
            IsDirty = true;
            return Task.CompletedTask;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Read region name
            var nameLength = reader.ReadInt32();
            if (nameLength > 0)
            {
                var nameBytes = reader.ReadBytes(nameLength);
                _regionName = System.Text.Encoding.UTF8.GetString(nameBytes);
            }

            // Read region description
            var descriptionLength = reader.ReadInt32();
            if (descriptionLength > 0)
            {
                var descriptionBytes = reader.ReadBytes(descriptionLength);
                _regionDescription = System.Text.Encoding.UTF8.GetString(descriptionBytes);
            }

            // Read boundary points
            var boundaryCount = reader.ReadInt32();
            _boundaryPoints.Clear();
            for (int i = 0; i < boundaryCount; i++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                _boundaryPoints.Add(new Vector2(x, y));
            }

            // Read associated lots
            var lotCount = reader.ReadInt32();
            _associatedLots.Clear();
            for (int i = 0; i < lotCount; i++)
            {
                _associatedLots.Add(reader.ReadUInt64());
            }

            // Read climate and elevation
            _climate = (RegionClimate)reader.ReadInt32();
            _elevation = reader.ReadSingle();

            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load region description resource: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the region description resource to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Write region name
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(_regionName);
            writer.Write(nameBytes.Length);
            if (nameBytes.Length > 0)
            {
                writer.Write(nameBytes);
            }

            // Write region description
            var descriptionBytes = System.Text.Encoding.UTF8.GetBytes(_regionDescription);
            writer.Write(descriptionBytes.Length);
            if (descriptionBytes.Length > 0)
            {
                writer.Write(descriptionBytes);
            }

            // Write boundary points
            writer.Write(_boundaryPoints.Count);
            foreach (var point in _boundaryPoints)
            {
                writer.Write(point.X);
                writer.Write(point.Y);
            }

            // Write associated lots
            writer.Write(_associatedLots.Count);
            foreach (var lotId in _associatedLots)
            {
                writer.Write(lotId);
            }

            // Write climate and elevation
            writer.Write((int)_climate);
            writer.Write(_elevation);

            await writer.BaseStream.FlushAsync(cancellationToken);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to save region description resource: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the resource as a stream.
    /// </summary>
    /// <returns>A stream containing the resource data. The caller is responsible for disposing this stream.</returns>
    /// <remarks>
    /// This method creates a new <see cref="MemoryStream"/> containing the serialized resource data.
    /// The returned stream must be disposed by the caller to prevent memory leaks.
    /// Consider using a using statement or using declaration when calling this method.
    /// </remarks>
    public async Task<Stream> AsStreamAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var memoryStream = new MemoryStream();
        try
        {
            await SaveToStreamAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch
        {
            // If an exception occurs, dispose the stream to prevent memory leak
            memoryStream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        // Write magic bytes
        writer.Write("RGND"u8.ToArray());
        writer.Write(Version);
        writer.Write(Key.Instance);

        // Write region properties
        writer.Write(_regionName);
        writer.Write(_regionDescription);
        writer.Write((int)_climate);
        writer.Write(_elevation);

        // Write boundary points
        writer.Write(_boundaryPoints.Count);
        foreach (var point in _boundaryPoints)
        {
            writer.Write(point.X);
            writer.Write(point.Y);
        }

        // Write associated lots
        writer.Write(_associatedLots.Count);
        foreach (var lotId in _associatedLots)
        {
            writer.Write(lotId);
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(Key.Instance), // RegionId
            1 => TypedValue.Create(_regionName), // RegionName
            2 => TypedValue.Create(_regionDescription), // Description
            3 => TypedValue.Create(_climate), // Climate
            4 => TypedValue.Create(_elevation), // Elevation
            5 => TypedValue.Create(_boundaryPoints.Count), // BoundaryPoints count
            6 => TypedValue.Create(_associatedLots.Count), // AssociatedLots count
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "RegionId" => TypedValue.Create(Key.Instance),
            "RegionName" => TypedValue.Create(_regionName),
            "Description" => TypedValue.Create(_regionDescription),
            "Climate" => TypedValue.Create(_climate),
            "Elevation" => TypedValue.Create(_elevation),
            "BoundaryPoints" => TypedValue.Create(_boundaryPoints.Count),
            "AssociatedLots" => TypedValue.Create(_associatedLots.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        switch (index)
        {
            case 1: // RegionName
                RegionName = value.Value?.ToString() ?? string.Empty;
                break;
            case 2: // Description
                RegionDescription = value.Value?.ToString() ?? string.Empty;
                break;
            case 3: // Climate
                if (value.Value is RegionClimate climate)
                    Climate = climate;
                else if (value.Value is int climateInt)
                    Climate = (RegionClimate)climateInt;
                break;
            case 4: // Elevation
                if (value.Value is float elevation)
                    Elevation = elevation;
                else if (value.Value is double elevationDouble)
                    Elevation = (float)elevationDouble;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "RegionName":
                RegionName = value.Value?.ToString() ?? string.Empty;
                break;
            case "Description":
                RegionDescription = value.Value?.ToString() ?? string.Empty;
                break;
            case "Climate":
                if (value.Value is RegionClimate climate)
                    Climate = climate;
                else if (value.Value is int climateInt)
                    Climate = (RegionClimate)climateInt;
                break;
            case "Elevation":
                if (value.Value is float elevation)
                    Elevation = elevation;
                else if (value.Value is double elevationDouble)
                    Elevation = (float)elevationDouble;
                break;
            default:
                throw new ArgumentException($"Unknown field: {name}", nameof(name));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the climate type of a region.
/// </summary>
public enum RegionClimate
{
    /// <summary>Temperate climate.</summary>
    Temperate = 0,
    /// <summary>Desert climate.</summary>
    Desert = 1,
    /// <summary>Tropical climate.</summary>
    Tropical = 2,
    /// <summary>Arctic climate.</summary>
    Arctic = 3
}
