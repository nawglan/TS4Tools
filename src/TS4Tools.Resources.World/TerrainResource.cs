using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.World;

/// <summary>
/// Represents a terrain resource that contains terrain data and heightmaps in The Sims 4 package files.
/// Terrain resources define the ground mesh, height data, and terrain layers for world environments.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class TerrainResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private readonly List<TerrainVertex> _vertices;
    private readonly List<TerrainPass> _passes;
    private bool _isDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this terrain.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the terrain version.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets the layer index count.
    /// </summary>
    public uint LayerIndexCount { get; }

    /// <summary>
    /// Gets the minimum bounds of the terrain.
    /// </summary>
    public TerrainBounds MinBounds { get; }

    /// <summary>
    /// Gets the maximum bounds of the terrain.
    /// </summary>
    public TerrainBounds MaxBounds { get; }

    /// <summary>
    /// Gets the collection of terrain vertices.
    /// </summary>
    public IReadOnlyList<TerrainVertex> Vertices => _vertices.AsReadOnly();

    /// <summary>
    /// Gets the collection of terrain passes.
    /// </summary>
    public IReadOnlyList<TerrainPass> Passes => _passes.AsReadOnly();

    /// <summary>
    /// Gets or sets whether the resource has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => _isDirty = value;
    }

    /// <summary>
    /// Gets the number of terrain vertices.
    /// </summary>
    public int VertexCount => _vertices.Count;

    /// <summary>
    /// Gets the number of terrain passes.
    /// </summary>
    public int PassCount => _passes.Count;

    /// <summary>
    /// Gets the width of the terrain bounds.
    /// </summary>
    public ushort TerrainBoundsWidth => (ushort)(MaxBounds.X - MinBounds.X);

    /// <summary>
    /// Gets the height of the terrain bounds.
    /// </summary>
    public ushort TerrainBoundsHeight => (ushort)(MaxBounds.Y - MinBounds.Y);

    /// <summary>
    /// Gets the depth of the terrain bounds.
    /// </summary>
    public ushort TerrainBoundsDepth => (ushort)(MaxBounds.Z - MinBounds.Z);

    /// <summary>
    /// Gets whether the terrain has vertex data.
    /// </summary>
    public bool HasVertexData => _vertices.Count > 0;

    /// <summary>
    /// Gets whether the terrain has pass data.
    /// </summary>
    public bool HasPassData => _passes.Count > 0;

    /// <summary>
    /// Gets the total number of indices across all passes.
    /// </summary>
    public int TotalIndicesCount => _passes.Sum(p => p.Indices.Length);

    /// <summary>
    /// Initializes a new instance of the TerrainResource class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="version">The terrain version.</param>
    /// <param name="layerIndexCount">The layer index count.</param>
    /// <param name="minBounds">The minimum bounds.</param>
    /// <param name="maxBounds">The maximum bounds.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public TerrainResource(ResourceKey key, uint version = 1, uint layerIndexCount = 0,
        TerrainBounds minBounds = default, TerrainBounds maxBounds = default)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        LayerIndexCount = layerIndexCount;
        MinBounds = minBounds;
        MaxBounds = maxBounds;
        _vertices = new List<TerrainVertex>();
        _passes = new List<TerrainPass>();
    }

    /// <summary>
    /// Loads terrain data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing terrain data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid terrain data.</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Handle null or truly empty stream (no content at all)
        if (stream == null || stream.Length == 0)
        {
            // Initialize with default values for empty terrain
            _vertices.Clear();
            _passes.Clear();
            _isDirty = true;
            OnResourceChanged();
            return;
        }

        try
        {
            using var reader = new BinaryReader(stream);

            // Check if we have enough bytes for a complete terrain resource
            // Minimum: 28 bytes (header) + 4 bytes (vertexCount) + 4 bytes (passCount) = 36 bytes
            if (stream.Length < 36)
            {
                // Handle incomplete/partial terrain data by initializing empty
                _vertices.Clear();
                _passes.Clear();
                _isDirty = true;
                OnResourceChanged();
                return;
            }

            // Read terrain header
            var header = await ReadTerrainHeaderAsync(reader, cancellationToken);

            // Read vertices - use safer ReadUInt32 with bounds checking
            var vertexCount = reader.ReadUInt32();
            if (vertexCount > 0 && stream.Position + (vertexCount * 20) > stream.Length) // Assume ~20 bytes per vertex
            {
                throw new InvalidDataException("Stream too short for specified vertex count");
            }

            _vertices.Clear();
            for (uint i = 0; i < vertexCount && !cancellationToken.IsCancellationRequested; i++)
            {
                var vertex = await ReadTerrainVertexAsync(reader, cancellationToken);
                _vertices.Add(vertex);
            }

            // Read passes - use safer ReadUInt32 with bounds checking
            var passCount = reader.ReadUInt32();
            if (passCount > 0 && stream.Position + (passCount * 16) > stream.Length) // Assume ~16 bytes per pass
            {
                throw new InvalidDataException("Stream too short for specified pass count");
            }

            _passes.Clear();
            for (uint i = 0; i < passCount && !cancellationToken.IsCancellationRequested; i++)
            {
                var pass = await ReadTerrainPassAsync(reader, cancellationToken);
                _passes.Add(pass);
            }

            _isDirty = false;
            OnResourceChanged();
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("Failed to load terrain data from stream", ex);
        }
    }

    /// <summary>
    /// Saves terrain data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write terrain data to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new BinaryWriter(stream);

        // Write terrain header
        await WriteTerrainHeaderAsync(writer, cancellationToken);

        // Write vertices
        writer.Write((uint)_vertices.Count);
        foreach (var vertex in _vertices)
        {
            await WriteTerrainVertexAsync(writer, vertex, cancellationToken);
        }

        // Write passes
        writer.Write((uint)_passes.Count);
        foreach (var pass in _passes)
        {
            await WriteTerrainPassAsync(writer, pass, cancellationToken);
        }

        _isDirty = false;
        OnResourceChanged();
    }

    /// <summary>
    /// Adds a terrain vertex.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when vertex is null.</exception>
    public void AddVertex(TerrainVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);

        _vertices.Add(vertex);
        _isDirty = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a terrain vertex.
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>true if the vertex was removed; otherwise, false.</returns>
    public bool RemoveVertex(TerrainVertex vertex)
    {
        if (vertex == null)
            return false;

        var removed = _vertices.Remove(vertex);
        if (removed)
        {
            _isDirty = true;
            OnResourceChanged();
        }

        return removed;
    }

    /// <summary>
    /// Adds a terrain pass.
    /// </summary>
    /// <param name="pass">The pass to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when pass is null.</exception>
    public void AddPass(TerrainPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);

        _passes.Add(pass);
        _isDirty = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a terrain pass.
    /// </summary>
    /// <param name="pass">The pass to remove.</param>
    /// <returns>true if the pass was removed; otherwise, false.</returns>
    public bool RemovePass(TerrainPass pass)
    {
        if (pass == null)
            return false;

        var removed = _passes.Remove(pass);
        if (removed)
        {
            _isDirty = true;
            OnResourceChanged();
        }

        return removed;
    }

    private async Task<TerrainHeader> ReadTerrainHeaderAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Read terrain header data in a single operation
            Span<byte> headerData = stackalloc byte[28]; // 7 * sizeof(uint) + 6 * sizeof(ushort)
            reader.Read(headerData);

            return ReadTerrainHeaderFromSpan(headerData);
        }, cancellationToken);
    }

    private static TerrainHeader ReadTerrainHeaderFromSpan(ReadOnlySpan<byte> data)
    {
        return new TerrainHeader
        {
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0, 4)),
            LayerIndexCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4, 4)),
            MinX = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2)),
            MinY = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10, 2)),
            MinZ = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12, 2)),
            MaxX = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(14, 2)),
            MaxY = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(16, 2)),
            MaxZ = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(18, 2))
        };
    }

    private static async Task<TerrainVertex> ReadTerrainVertexAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Read terrain vertex data in a single operation
            Span<byte> vertexData = stackalloc byte[24]; // 5 * sizeof(float) + sizeof(uint)
            reader.Read(vertexData);

            return ReadTerrainVertexFromSpan(vertexData);
        }, cancellationToken);
    }

    private static TerrainVertex ReadTerrainVertexFromSpan(ReadOnlySpan<byte> data)
    {
        return new TerrainVertex
        {
            X = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(0, 4)),
            Y = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(4, 4)),
            Z = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(8, 4)),
            U = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(12, 4)),
            V = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(16, 4)),
            LayerIndex = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(20, 4))
        };
    }

    private static async Task<TerrainPass> ReadTerrainPassAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var indicesCount = reader.ReadUInt32();
            var indices = new uint[indicesCount];

            for (uint i = 0; i < indicesCount; i++)
            {
                indices[i] = reader.ReadUInt32();
            }

            return new TerrainPass
            {
                Indices = indices,
                MaterialId = reader.ReadUInt32()
            };
        }, cancellationToken);
    }

    private async Task WriteTerrainHeaderAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            writer.Write(Version);
            writer.Write(LayerIndexCount);
            writer.Write(MinBounds.X);
            writer.Write(MinBounds.Y);
            writer.Write(MinBounds.Z);
            writer.Write(MaxBounds.X);
            writer.Write(MaxBounds.Y);
            writer.Write(MaxBounds.Z);
        }, cancellationToken);
    }

    private static async Task WriteTerrainVertexAsync(BinaryWriter writer, TerrainVertex vertex, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            writer.Write(vertex.X);
            writer.Write(vertex.Y);
            writer.Write(vertex.Z);
            writer.Write(vertex.U);
            writer.Write(vertex.V);
            writer.Write(vertex.LayerIndex);
        }, cancellationToken);
    }

    private static async Task WriteTerrainPassAsync(BinaryWriter writer, TerrainPass pass, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            writer.Write((uint)pass.Indices.Length);
            foreach (var index in pass.Indices)
            {
                writer.Write(index);
            }
            writer.Write(pass.MaterialId);
        }, cancellationToken);
    }

    /// <summary>
    /// Disposes of resources used by the TerrainResource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _vertices.Clear();
        _passes.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a string representation of the terrain resource.
    /// </summary>
    /// <returns>A string representation of the terrain resource.</returns>
    public override string ToString()
    {
        return $"TerrainResource (Version: {Version}, Vertices: {_vertices.Count}, Passes: {_passes.Count})";
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
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Version),
        nameof(LayerIndexCount),
        nameof(MinBounds),
        nameof(MaxBounds),
        nameof(Vertices),
        nameof(Passes),
        nameof(VertexCount),
        nameof(PassCount),
        nameof(TerrainBoundsWidth),
        nameof(TerrainBoundsHeight),
        nameof(TerrainBoundsDepth),
        nameof(HasVertexData),
        nameof(HasPassData),
        nameof(TotalIndicesCount),
        nameof(IsDirty)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(LayerIndexCount) => new TypedValue(typeof(uint), LayerIndexCount),
            nameof(MinBounds) => new TypedValue(typeof(TerrainBounds), MinBounds),
            nameof(MaxBounds) => new TypedValue(typeof(TerrainBounds), MaxBounds),
            nameof(Vertices) => new TypedValue(typeof(IReadOnlyList<TerrainVertex>), Vertices),
            nameof(Passes) => new TypedValue(typeof(IReadOnlyList<TerrainPass>), Passes),
            nameof(VertexCount) => new TypedValue(typeof(int), VertexCount),
            nameof(PassCount) => new TypedValue(typeof(int), PassCount),
            nameof(TerrainBoundsWidth) => new TypedValue(typeof(ushort), TerrainBoundsWidth),
            nameof(TerrainBoundsHeight) => new TypedValue(typeof(ushort), TerrainBoundsHeight),
            nameof(TerrainBoundsDepth) => new TypedValue(typeof(ushort), TerrainBoundsDepth),
            nameof(HasVertexData) => new TypedValue(typeof(bool), HasVertexData),
            nameof(HasPassData) => new TypedValue(typeof(bool), HasPassData),
            nameof(TotalIndicesCount) => new TypedValue(typeof(int), TotalIndicesCount),
            nameof(IsDirty) => new TypedValue(typeof(bool), IsDirty),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Terrain resource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(LayerIndexCount)],
            2 => this[nameof(MinBounds)],
            3 => this[nameof(MaxBounds)],
            4 => this[nameof(Vertices)],
            5 => this[nameof(Passes)],
            6 => this[nameof(VertexCount)],
            7 => this[nameof(PassCount)],
            8 => this[nameof(TerrainBoundsWidth)],
            9 => this[nameof(TerrainBoundsHeight)],
            10 => this[nameof(TerrainBoundsDepth)],
            11 => this[nameof(HasVertexData)],
            12 => this[nameof(HasPassData)],
            13 => this[nameof(TotalIndicesCount)],
            14 => this[nameof(IsDirty)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-14, got {index}")
        };
        set => throw new NotSupportedException("Terrain resource fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents terrain bounds.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public readonly record struct TerrainBounds(ushort X, ushort Y, ushort Z);

/// <summary>
/// Represents terrain header information.
/// </summary>
public sealed class TerrainHeader
{
    /// <summary>
    /// Gets or sets the terrain version.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Gets or sets the layer index count.
    /// </summary>
    public uint LayerIndexCount { get; set; }

    /// <summary>
    /// Gets or sets the minimum X bound.
    /// </summary>
    public ushort MinX { get; set; }

    /// <summary>
    /// Gets or sets the minimum Y bound.
    /// </summary>
    public ushort MinY { get; set; }

    /// <summary>
    /// Gets or sets the minimum Z bound.
    /// </summary>
    public ushort MinZ { get; set; }

    /// <summary>
    /// Gets or sets the maximum X bound.
    /// </summary>
    public ushort MaxX { get; set; }

    /// <summary>
    /// Gets or sets the maximum Y bound.
    /// </summary>
    public ushort MaxY { get; set; }

    /// <summary>
    /// Gets or sets the maximum Z bound.
    /// </summary>
    public ushort MaxZ { get; set; }
}

/// <summary>
/// Represents a terrain vertex.
/// </summary>
public sealed class TerrainVertex
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Gets or sets the U texture coordinate.
    /// </summary>
    public float U { get; set; }

    /// <summary>
    /// Gets or sets the V texture coordinate.
    /// </summary>
    public float V { get; set; }

    /// <summary>
    /// Gets or sets the layer index.
    /// </summary>
    public uint LayerIndex { get; set; }

    /// <summary>
    /// Returns a string representation of the terrain vertex.
    /// </summary>
    /// <returns>A string representation of the terrain vertex.</returns>
    public override string ToString()
    {
        return $"TerrainVertex ({X}, {Y}, {Z}) UV({U}, {V}) Layer: {LayerIndex}";
    }
}

/// <summary>
/// Represents a terrain rendering pass.
/// </summary>
public sealed class TerrainPass
{
    /// <summary>
    /// Gets or sets the vertex indices for this pass.
    /// </summary>
    public uint[] Indices { get; set; } = Array.Empty<uint>();

    /// <summary>
    /// Gets or sets the material identifier for this pass.
    /// </summary>
    public uint MaterialId { get; set; }

    /// <summary>
    /// Returns a string representation of the terrain pass.
    /// </summary>
    /// <returns>A string representation of the terrain pass.</returns>
    public override string ToString()
    {
        return $"TerrainPass (Indices: {Indices.Length}, Material: {MaterialId})";
    }
}
