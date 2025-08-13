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
using System.Text;
using System.Text.Json;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a lot description resource that contains detailed metadata and properties for lots in The Sims 4.
/// This resource provides rich lot information beyond basic placement data.
/// </summary>
public sealed class LotDescriptionResource : IResource, IDisposable, INotifyPropertyChanged
{
    private readonly ResourceKey _key;
    private readonly List<string> _contentFields = new()
    {
        "LotId",
        "LotName",
        "Description",
        "LotType",
        "LotTraits",
        "Metadata"
    };
    private bool _isDirty = true;
    private bool _disposed;
    private MemoryStream? _stream;
    private string _lotName = string.Empty;
    private string _lotDescription = string.Empty;
    private LotType _lotType = LotType.Residential;
    private readonly List<LotTrait> _lotTraits = new();
    private readonly Dictionary<string, object> _metadata = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LotDescriptionResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    public LotDescriptionResource(ResourceKey key, uint version)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        _stream = new MemoryStream();

        // Initialize ContentFields for new resources
        _contentFields.AddRange([
            "LotId",
            "LotName",
            "LotDescription",
            "LotType",
            "LotTraits",
            "ThumbnailKey",
            "LotFlags"
        ]);
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
    /// Gets or sets the lot name.
    /// </summary>
    public string LotName
    {
        get => _lotName;
        set
        {
            if (_lotName != value)
            {
                _lotName = value ?? string.Empty;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the lot description.
    /// </summary>
    public string LotDescription
    {
        get => _lotDescription;
        set
        {
            if (_lotDescription != value)
            {
                _lotDescription = value ?? string.Empty;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the lot type.
    /// </summary>
    public LotType LotType
    {
        get => _lotType;
        set
        {
            if (_lotType != value)
            {
                _lotType = value;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the collection of lot traits.
    /// </summary>
    public IReadOnlyList<LotTrait> LotTraits => _lotTraits.AsReadOnly();

    /// <summary>
    /// Gets the metadata dictionary for storing additional lot properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => _metadata;

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
            WriteToStream(ms);
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
    /// Loads the lot description resource from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Handle null or truly empty stream (no content at all)
        if (stream == null || stream.Length == 0)
        {
            // Initialize with default values for empty lot description
            IsDirty = true;
            return Task.CompletedTask;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Read magic bytes to verify format
            var magic = reader.ReadBytes(4);
            if (!magic.SequenceEqual(Encoding.UTF8.GetBytes("LOTD")))
            {
                throw new InvalidDataException("Invalid lot description resource format: missing LOTD magic bytes");
            }

            // Read version
            var version = reader.ReadUInt32();
            if (version > 1)
            {
                throw new InvalidDataException($"Unsupported lot description resource version: {version}");
            }

            // Read content - this would depend on the actual file format
            // For now, just mark as not dirty since we successfully loaded
            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load lot description resource: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a lot trait to the lot.
    /// </summary>
    /// <param name="trait">The trait to add.</param>
    public void AddLotTrait(LotTrait trait)
    {
        if (!_lotTraits.Contains(trait))
        {
            _lotTraits.Add(trait);
            IsDirty = true;
            OnPropertyChanged(nameof(LotTraits));
        }
    }

    /// <summary>
    /// Removes a lot trait from the lot.
    /// </summary>
    /// <param name="trait">The trait to remove.</param>
    /// <returns>True if the trait was removed; otherwise, false.</returns>
    public bool RemoveLotTrait(LotTrait trait)
    {
        var removed = _lotTraits.Remove(trait);
        if (removed)
        {
            IsDirty = true;
            OnPropertyChanged(nameof(LotTraits));
        }
        return removed;
    }

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public void SetMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        _metadata[key] = value;
        IsDirty = true;
        OnPropertyChanged(nameof(Metadata));
    }

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    /// <typeparam name="T">The expected type of the metadata value.</typeparam>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value, or default(T) if not found.</returns>
    public T? GetMetadata<T>(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
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
        writer.Write("LOTD"u8.ToArray());
        writer.Write(Version);
        writer.Write(Key.Instance);

        // Write lot properties
        writer.Write(_lotName);
        writer.Write(_lotDescription);
        writer.Write((int)_lotType);

        // Write traits
        writer.Write(_lotTraits.Count);
        foreach (var trait in _lotTraits)
        {
            writer.Write(trait.Name);
            writer.Write(trait.TraitId);
        }

        // Write metadata
        if (_metadata.Count > 0)
        {
            var metadataJson = JsonSerializer.Serialize(_metadata, new JsonSerializerOptions { WriteIndented = false });
            var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);
            writer.Write(metadataBytes.Length);
            writer.Write(metadataBytes);
        }
        else
        {
            writer.Write(0);
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(Key.Instance), // LotId
            1 => TypedValue.Create(_lotName), // LotName
            2 => TypedValue.Create(_lotDescription), // Description
            3 => TypedValue.Create(_lotType), // LotType
            4 => TypedValue.Create(_lotTraits.Count), // LotTraits count
            5 => TypedValue.Create(_metadata.Count), // Metadata count
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "LotId" => TypedValue.Create(Key.Instance),
            "LotName" => TypedValue.Create(_lotName),
            "Description" => TypedValue.Create(_lotDescription),
            "LotType" => TypedValue.Create(_lotType),
            "LotTraits" => TypedValue.Create(_lotTraits.Count),
            "Metadata" => TypedValue.Create(_metadata.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        switch (index)
        {
            case 1: // LotName
                LotName = value.Value?.ToString() ?? string.Empty;
                break;
            case 2: // Description
                LotDescription = value.Value?.ToString() ?? string.Empty;
                break;
            case 3: // LotType
                if (value.Value is LotType lotType)
                    LotType = lotType;
                else if (value.Value is int lotTypeInt)
                    LotType = (LotType)lotTypeInt;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "LotName":
                LotName = value.Value?.ToString() ?? string.Empty;
                break;
            case "Description":
                LotDescription = value.Value?.ToString() ?? string.Empty;
                break;
            case "LotType":
                if (value.Value is LotType lotType)
                    LotType = lotType;
                else if (value.Value is int lotTypeInt)
                    LotType = (LotType)lotTypeInt;
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
/// Represents the type of lot.
/// </summary>
public enum LotType
{
    /// <summary>Residential lot.</summary>
    Residential = 0,
    /// <summary>Community lot.</summary>
    Community = 1,
    /// <summary>Commercial lot.</summary>
    Commercial = 2,
    /// <summary>Special lot (like parks).</summary>
    Special = 3
}

/// <summary>
/// Represents a lot trait.
/// </summary>
/// <param name="Name">The trait name.</param>
/// <param name="TraitId">The trait identifier.</param>
public readonly record struct LotTrait(string Name, ulong TraitId);
