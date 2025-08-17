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

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Represents a modular resource that defines modular building components and their assembly rules.
/// This resource enables the building system's modular construction capabilities in The Sims 4.
/// </summary>
public sealed class ModularResource : IResource, IDisposable, INotifyPropertyChanged
{
    private readonly ResourceKey _key;
    private readonly List<string> _contentFields = new()
    {
        "ComponentCount",
        "ConnectionCount",
        "ConstraintCount"
    };
    private bool _isDirty = true;
    private bool _disposed;
    private MemoryStream? _stream;
    private readonly List<ModularComponent> _components = new();
    private readonly List<ModularConnection> _connections = new();
    private readonly Dictionary<string, ModularConstraint> _constraints = new();
    private string _resourceName = string.Empty;
    private uint _version = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModularResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The resource version.</param>
    public ModularResource(ResourceKey key, uint version)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _version = version;
        _stream = new MemoryStream();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public ResourceKey Key => _key;

    /// <inheritdoc/>
    public uint Version => _version;

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
    /// Gets or sets the resource name.
    /// </summary>
    public string ResourceName
    {
        get => _resourceName;
        set
        {
            if (_resourceName != value)
            {
                _resourceName = value ?? string.Empty;
                IsDirty = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the collection of modular components.
    /// </summary>
    public IReadOnlyList<ModularComponent> Components => _components;

    /// <summary>
    /// Gets the collection of component connections.
    /// </summary>
    public IReadOnlyList<ModularConnection> Connections => _connections;

    /// <summary>
    /// Gets the constraints dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, ModularConstraint> Constraints => _constraints;

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
    /// Adds a modular component to the resource.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddComponent(ModularComponent component)
    {
        if (!_components.Any(c => c.ComponentId == component.ComponentId))
        {
            _components.Add(component);
            IsDirty = true;
            OnPropertyChanged(nameof(Components));
        }
    }

    /// <summary>
    /// Removes a modular component from the resource.
    /// </summary>
    /// <param name="componentId">The ID of the component to remove.</param>
    /// <returns>True if the component was removed; otherwise, false.</returns>
    public bool RemoveComponent(ulong componentId)
    {
        var component = _components.FirstOrDefault(c => c.ComponentId == componentId);
        if (component.ComponentId != 0)
        {
            var removed = _components.Remove(component);
            if (removed)
            {
                // Remove any connections involving this component
                _connections.RemoveAll(c => c.ComponentAId == componentId || c.ComponentBId == componentId);
                IsDirty = true;
                OnPropertyChanged(nameof(Components));
                OnPropertyChanged(nameof(Connections));
            }
            return removed;
        }
        return false;
    }

    /// <summary>
    /// Adds a connection between two components.
    /// </summary>
    /// <param name="connection">The connection to add.</param>
    public void AddConnection(ModularConnection connection)
    {
        // Validate that both components exist
        var componentA = _components.FirstOrDefault(c => c.ComponentId == connection.ComponentAId);
        var componentB = _components.FirstOrDefault(c => c.ComponentId == connection.ComponentBId);

        if (componentA.ComponentId == 0 || componentB.ComponentId == 0)
        {
            throw new InvalidOperationException("Cannot create connection between non-existent components.");
        }

        if (!_connections.Any(c => c.ConnectionId == connection.ConnectionId))
        {
            _connections.Add(connection);
            IsDirty = true;
            OnPropertyChanged(nameof(Connections));
        }
    }

    /// <summary>
    /// Removes a connection.
    /// </summary>
    /// <param name="connectionId">The ID of the connection to remove.</param>
    /// <returns>True if the connection was removed; otherwise, false.</returns>
    public bool RemoveConnection(ulong connectionId)
    {
        var connection = _connections.FirstOrDefault(c => c.ConnectionId == connectionId);
        if (connection.ConnectionId != 0)
        {
            var removed = _connections.Remove(connection);
            if (removed)
            {
                IsDirty = true;
                OnPropertyChanged(nameof(Connections));
            }
            return removed;
        }
        return false;
    }

    /// <summary>
    /// Sets a constraint for the modular system.
    /// </summary>
    /// <param name="name">The constraint name.</param>
    /// <param name="constraint">The constraint definition.</param>
    public void SetConstraint(string name, ModularConstraint constraint)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _constraints[name] = constraint;
        IsDirty = true;
        OnPropertyChanged(nameof(Constraints));
    }

    /// <summary>
    /// Removes a constraint.
    /// </summary>
    /// <param name="name">The name of the constraint to remove.</param>
    /// <returns>True if the constraint was removed; otherwise, false.</returns>
    public bool RemoveConstraint(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var removed = _constraints.Remove(name);
        if (removed)
        {
            IsDirty = true;
            OnPropertyChanged(nameof(Constraints));
        }
        return removed;
    }

    /// <summary>
    /// Validates the modular assembly for consistency.
    /// </summary>
    /// <returns>A validation result containing any errors or warnings.</returns>
    public ModularValidationResult ValidateAssembly()
    {
        var result = new ModularValidationResult();

        // Check for orphaned components
        var connectedComponents = new HashSet<ulong>();
        foreach (var connection in _connections)
        {
            connectedComponents.Add(connection.ComponentAId);
            connectedComponents.Add(connection.ComponentBId);
        }

        foreach (var component in _components)
        {
            if (!connectedComponents.Contains(component.ComponentId))
            {
                result.Warnings.Add($"Component {component.ComponentId} ({component.ComponentName}) is not connected to any other component.");
            }
        }

        // Check for invalid connections
        foreach (var connection in _connections)
        {
            var componentA = _components.FirstOrDefault(c => c.ComponentId == connection.ComponentAId);
            var componentB = _components.FirstOrDefault(c => c.ComponentId == connection.ComponentBId);

            if (componentA.ComponentId == 0)
            {
                result.Errors.Add($"Connection {connection.ConnectionId} references non-existent component A: {connection.ComponentAId}");
            }

            if (componentB.ComponentId == 0)
            {
                result.Errors.Add($"Connection {connection.ConnectionId} references non-existent component B: {connection.ComponentBId}");
            }
        }

        return result;
    }

    /// <summary>
    /// Loads the modular resource from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Read resource header
            _version = reader.ReadUInt32();
            var nameLength = reader.ReadInt32();
            if (nameLength > 0)
            {
                var nameBytes = reader.ReadBytes(nameLength);
                _resourceName = System.Text.Encoding.UTF8.GetString(nameBytes);
            }

            // Read components
            var componentCount = reader.ReadInt32();
            _components.Clear();
            for (int i = 0; i < componentCount; i++)
            {
                var componentId = reader.ReadUInt64();
                var componentNameLength = reader.ReadInt32();
                var componentNameBytes = reader.ReadBytes(componentNameLength);
                var componentName = System.Text.Encoding.UTF8.GetString(componentNameBytes);

                var componentType = (ComponentType)reader.ReadInt32();
                var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                var rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                var scale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                _components.Add(new ModularComponent(componentId, componentName, componentType, position, rotation, scale));
            }

            // Read connections
            var connectionCount = reader.ReadInt32();
            _connections.Clear();
            for (int i = 0; i < connectionCount; i++)
            {
                var connectionId = reader.ReadUInt64();
                var componentAId = reader.ReadUInt64();
                var componentBId = reader.ReadUInt64();
                var connectionType = (ConnectionType)reader.ReadInt32();
                var strength = reader.ReadSingle();

                _connections.Add(new ModularConnection(connectionId, componentAId, componentBId, connectionType, strength));
            }

            // Read constraints
            var constraintCount = reader.ReadInt32();
            _constraints.Clear();
            for (int i = 0; i < constraintCount; i++)
            {
                var nameLength2 = reader.ReadInt32();
                var nameBytes2 = reader.ReadBytes(nameLength2);
                var constraintName = System.Text.Encoding.UTF8.GetString(nameBytes2);

                var constraintType = (ConstraintType)reader.ReadInt32();
                var minValue = reader.ReadSingle();
                var maxValue = reader.ReadSingle();

                _constraints[constraintName] = new ModularConstraint(constraintType, minValue, maxValue);
            }

            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load modular resource: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the modular resource to a stream.
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

            // Write resource header
            writer.Write(_version);
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(_resourceName);
            writer.Write(nameBytes.Length);
            if (nameBytes.Length > 0)
            {
                writer.Write(nameBytes);
            }

            // Write components
            writer.Write(_components.Count);
            foreach (var component in _components)
            {
                writer.Write(component.ComponentId);
                var componentNameBytes = System.Text.Encoding.UTF8.GetBytes(component.ComponentName);
                writer.Write(componentNameBytes.Length);
                writer.Write(componentNameBytes);

                writer.Write((int)component.ComponentType);
                writer.Write(component.Position.X);
                writer.Write(component.Position.Y);
                writer.Write(component.Position.Z);
                writer.Write(component.Rotation.X);
                writer.Write(component.Rotation.Y);
                writer.Write(component.Rotation.Z);
                writer.Write(component.Scale.X);
                writer.Write(component.Scale.Y);
                writer.Write(component.Scale.Z);
            }

            // Write connections
            writer.Write(_connections.Count);
            foreach (var connection in _connections)
            {
                writer.Write(connection.ConnectionId);
                writer.Write(connection.ComponentAId);
                writer.Write(connection.ComponentBId);
                writer.Write((int)connection.ConnectionType);
                writer.Write(connection.Strength);
            }

            // Write constraints
            writer.Write(_constraints.Count);
            foreach (var kvp in _constraints)
            {
                var constraintNameBytes = System.Text.Encoding.UTF8.GetBytes(kvp.Key);
                writer.Write(constraintNameBytes.Length);
                writer.Write(constraintNameBytes);

                writer.Write((int)kvp.Value.ConstraintType);
                writer.Write(kvp.Value.MinValue);
                writer.Write(kvp.Value.MaxValue);
            }

            await writer.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to save modular resource: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the resource as a stream.
    /// </summary>
    /// <returns>A stream containing the resource data.</returns>
    public async Task<Stream> AsStreamAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var memoryStream = new MemoryStream();
        await SaveToStreamAsync(memoryStream).ConfigureAwait(false);
        memoryStream.Position = 0;
        return memoryStream;
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
        writer.Write("MODR"u8.ToArray());
        writer.Write(_version);
        writer.Write(Key.Instance);
        writer.Write(_resourceName);

        // Write components
        writer.Write(_components.Count);
        foreach (var component in _components)
        {
            writer.Write(component.ComponentId);
            writer.Write(component.ComponentName);
            writer.Write(component.Position.X);
            writer.Write(component.Position.Y);
            writer.Write(component.Position.Z);
            writer.Write(component.Rotation.X);
            writer.Write(component.Rotation.Y);
            writer.Write(component.Rotation.Z);
            writer.Write(component.Scale.X);
            writer.Write(component.Scale.Y);
            writer.Write(component.Scale.Z);
        }

        // Write connections
        writer.Write(_connections.Count);
        foreach (var connection in _connections)
        {
            writer.Write(connection.ConnectionId);
            writer.Write(connection.ComponentAId);
            writer.Write(connection.ComponentBId);
            writer.Write((int)connection.ConnectionType);
        }

        // Write constraints
        writer.Write(_constraints.Count);
        foreach (var kvp in _constraints)
        {
            writer.Write(kvp.Key);
            writer.Write((int)kvp.Value.ConstraintType);
            writer.Write(kvp.Value.MinValue);
            writer.Write(kvp.Value.MaxValue);
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(_components.Count), // ComponentCount
            1 => TypedValue.Create(_connections.Count), // ConnectionCount
            2 => TypedValue.Create(_constraints.Count), // ConstraintCount
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "ComponentCount" => TypedValue.Create(_components.Count),
            "ConnectionCount" => TypedValue.Create(_connections.Count),
            "ConstraintCount" => TypedValue.Create(_constraints.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        switch (index)
        {
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown.", nameof(name));
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
/// Represents a modular component.
/// </summary>
/// <param name="ComponentId">The unique component identifier.</param>
/// <param name="ComponentName">The component name.</param>
/// <param name="ComponentType">The type of component.</param>
/// <param name="Position">The component position in 3D space.</param>
/// <param name="Rotation">The component rotation.</param>
/// <param name="Scale">The component scale.</param>
public readonly record struct ModularComponent(
    ulong ComponentId,
    string ComponentName,
    ComponentType ComponentType,
    Vector3 Position,
    Vector3 Rotation,
    Vector3 Scale);

/// <summary>
/// Represents a connection between modular components.
/// </summary>
/// <param name="ConnectionId">The unique connection identifier.</param>
/// <param name="ComponentAId">The first component ID.</param>
/// <param name="ComponentBId">The second component ID.</param>
/// <param name="ConnectionType">The type of connection.</param>
/// <param name="Strength">The connection strength.</param>
public readonly record struct ModularConnection(
    ulong ConnectionId,
    ulong ComponentAId,
    ulong ComponentBId,
    ConnectionType ConnectionType,
    float Strength);

/// <summary>
/// Represents a modular constraint.
/// </summary>
/// <param name="ConstraintType">The type of constraint.</param>
/// <param name="MinValue">The minimum constraint value.</param>
/// <param name="MaxValue">The maximum constraint value.</param>
public readonly record struct ModularConstraint(
    ConstraintType ConstraintType,
    float MinValue,
    float MaxValue);

/// <summary>
/// Represents validation results for modular assembly.
/// </summary>
public sealed class ModularValidationResult
{
    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IList<string> Errors { get; } = new List<string>();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public IList<string> Warnings { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether the validation was successful (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Defines the types of modular components.
/// </summary>
public enum ComponentType
{
    /// <summary>Foundation component.</summary>
    Foundation = 0,
    /// <summary>Wall component.</summary>
    Wall = 1,
    /// <summary>Floor component.</summary>
    Floor = 2,
    /// <summary>Roof component.</summary>
    Roof = 3,
    /// <summary>Door component.</summary>
    Door = 4,
    /// <summary>Window component.</summary>
    Window = 5,
    /// <summary>Decorative component.</summary>
    Decorative = 6
}

/// <summary>
/// Defines the types of connections between components.
/// </summary>
public enum ConnectionType
{
    /// <summary>Rigid connection.</summary>
    Rigid = 0,
    /// <summary>Flexible connection.</summary>
    Flexible = 1,
    /// <summary>Hinge connection.</summary>
    Hinge = 2,
    /// <summary>Sliding connection.</summary>
    Sliding = 3
}

/// <summary>
/// Defines the types of constraints.
/// </summary>
public enum ConstraintType
{
    /// <summary>Distance constraint.</summary>
    Distance = 0,
    /// <summary>Angle constraint.</summary>
    Angle = 1,
    /// <summary>Scale constraint.</summary>
    Scale = 2,
    /// <summary>Position constraint.</summary>
    Position = 3
}
