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

using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Represents a simplified mesh resource for basic 3D geometry data.
/// This is a lightweight wrapper for mesh data that doesn't require the full GEOM format.
/// </summary>
public sealed class MeshResource : IResource, IDisposable
{
    private readonly MemoryStream _stream;
    private bool _disposed;
    private bool _modified;

    #region Mesh Data Properties

    /// <summary>
    /// The vertices of the mesh as a flat array of coordinates.
    /// Each vertex is represented as 3 consecutive floats (X, Y, Z).
    /// </summary>
    public IReadOnlyList<float> Vertices { get; private set; } = Array.Empty<float>();

    /// <summary>
    /// The normals of the mesh as a flat array of normal vectors.
    /// Each normal is represented as 3 consecutive floats (X, Y, Z).
    /// </summary>
    public IReadOnlyList<float> Normals { get; private set; } = Array.Empty<float>();

    /// <summary>
    /// The UV coordinates of the mesh as a flat array.
    /// Each UV coordinate is represented as 2 consecutive floats (U, V).
    /// </summary>
    public IReadOnlyList<float> UVCoordinates { get; private set; } = Array.Empty<float>();

    /// <summary>
    /// The triangle indices that define the mesh faces.
    /// Each triangle is represented as 3 consecutive indices.
    /// </summary>
    public IReadOnlyList<ushort> Indices { get; private set; } = Array.Empty<ushort>();

    /// <summary>
    /// The number of vertices in the mesh.
    /// </summary>
    public int VertexCount => Vertices.Count / 3;

    /// <summary>
    /// The number of triangles in the mesh.
    /// </summary>
    public int TriangleCount => Indices.Count / 3;

    /// <summary>
    /// Gets whether the mesh has normal data.
    /// </summary>
    public bool HasNormals => Normals.Count > 0;

    /// <summary>
    /// Gets whether the mesh has UV coordinate data.
    /// </summary>
    public bool HasUVs => UVCoordinates.Count > 0;

    /// <summary>
    /// Optional mesh name or identifier.
    /// </summary>
    public string? Name { get; set; }

    #endregion

    #region IResource Implementation

    /// <summary>
    /// The API version.
    /// </summary>
    public int RequestedApiVersion { get; }

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        "VertexCount",
        "TriangleCount",
        "HasNormals",
        "HasUVs"
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field by index.
    /// </summary>
    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    /// <summary>
    /// Gets or sets a content field by name.
    /// </summary>
    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    /// <summary>
    /// The resource content as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_modified)
            {
                WriteToStream();
                _modified = false;
            }
            _stream.Position = 0;
            return _stream;
        }
    }

    /// <summary>
    /// The resource content as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_modified)
            {
                WriteToStream();
                _modified = false;
            }
            return _stream.ToArray();
        }
    }

    /// <summary>
    /// Raised when the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    #endregion

    #region Content Field Access

    private TypedValue GetFieldByIndex(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(VertexCount),
            1 => TypedValue.Create(TriangleCount),
            2 => TypedValue.Create(HasNormals),
            3 => TypedValue.Create(HasUVs),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        // All properties are read-only for mesh resources
        OnResourceChanged();
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            "VertexCount" => TypedValue.Create(VertexCount),
            "TriangleCount" => TypedValue.Create(TriangleCount),
            "HasNormals" => TypedValue.Create(HasNormals),
            "HasUVs" => TypedValue.Create(HasUVs),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        // All properties are read-only for mesh resources
        OnResourceChanged();
    }

    private void OnResourceChanged()
    {
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new empty mesh resource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    public MeshResource(int requestedApiVersion = 1)
    {
        RequestedApiVersion = requestedApiVersion;
        _stream = new MemoryStream();
        _modified = true;
    }

    /// <summary>
    /// Initializes a new mesh resource from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing mesh data.</param>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid mesh data.</exception>
    public MeshResource(Stream stream, int requestedApiVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(stream);

        RequestedApiVersion = requestedApiVersion;

        _stream = new MemoryStream();
        stream.CopyTo(_stream);
        _stream.Position = 0;

        ParseMeshData();
    }

    /// <summary>
    /// Initializes a new mesh resource with the specified data.
    /// </summary>
    /// <param name="vertices">The vertex coordinates (X, Y, Z per vertex).</param>
    /// <param name="indices">The triangle indices.</param>
    /// <param name="normals">Optional normal vectors (X, Y, Z per vertex).</param>
    /// <param name="uvCoordinates">Optional UV coordinates (U, V per vertex).</param>
    /// <param name="name">Optional mesh name.</param>
    public MeshResource(IEnumerable<float> vertices, IEnumerable<ushort> indices,
                       IEnumerable<float>? normals = null, IEnumerable<float>? uvCoordinates = null,
                       string? name = null)
    {
        _stream = new MemoryStream();

        Vertices = vertices?.ToList() ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices?.ToList() ?? throw new ArgumentNullException(nameof(indices));
        Normals = normals?.ToList() ?? new List<float>();
        UVCoordinates = uvCoordinates?.ToList() ?? new List<float>();
        Name = name;

        ValidateMeshData();
        _modified = true;
    }

    #endregion

    #region Data Parsing

    private void ParseMeshData()
    {
        if (_stream.Length < 16) // Minimum size for header
            throw new InvalidDataException("Mesh stream too small");

        using var reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Simple binary format:
        // [4 bytes] Vertex count
        // [4 bytes] Index count  
        // [4 bytes] Has normals flag
        // [4 bytes] Has UV coordinates flag
        // [Variable] Vertex data (3 floats per vertex)
        // [Variable] Normal data (3 floats per vertex, if present)
        // [Variable] UV data (2 floats per vertex, if present)
        // [Variable] Index data (ushort per index)
        // [Variable] Name (length-prefixed string, optional)

        var vertexCount = reader.ReadInt32();
        var indexCount = reader.ReadInt32();
        var hasNormals = reader.ReadInt32() != 0;
        var hasUVs = reader.ReadInt32() != 0;

        if (vertexCount < 0 || indexCount < 0)
            throw new InvalidDataException("Invalid vertex or index count");

        // Read vertices
        var vertices = new float[vertexCount * 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = reader.ReadSingle();
        }
        Vertices = vertices;

        // Read normals if present
        if (hasNormals)
        {
            var normals = new float[vertexCount * 3];
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = reader.ReadSingle();
            }
            Normals = normals;
        }

        // Read UV coordinates if present
        if (hasUVs)
        {
            var uvs = new float[vertexCount * 2];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = reader.ReadSingle();
            }
            UVCoordinates = uvs;
        }

        // Read indices
        var indices = new ushort[indexCount];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = reader.ReadUInt16();
        }
        Indices = indices;

        // Read name if present
        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var nameLength = reader.ReadInt32();
            if (nameLength > 0 && nameLength < 1024) // Sanity check
            {
                var nameBytes = reader.ReadBytes(nameLength);
                Name = System.Text.Encoding.UTF8.GetString(nameBytes);
            }
        }
    }

    #endregion

    #region Data Writing

    private void WriteToStream()
    {
        _stream.SetLength(0);
        _stream.Position = 0;

        using var writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Write header
        writer.Write(VertexCount);
        writer.Write(Indices.Count);
        writer.Write(Normals.Count > 0 ? 1 : 0);
        writer.Write(UVCoordinates.Count > 0 ? 1 : 0);

        // Write vertices
        foreach (var vertex in Vertices)
        {
            writer.Write(vertex);
        }

        // Write normals if present
        if (Normals.Count > 0)
        {
            foreach (var normal in Normals)
            {
                writer.Write(normal);
            }
        }

        // Write UV coordinates if present
        if (UVCoordinates.Count > 0)
        {
            foreach (var uv in UVCoordinates)
            {
                writer.Write(uv);
            }
        }

        // Write indices
        foreach (var index in Indices)
        {
            writer.Write(index);
        }

        // Write name if present
        if (!string.IsNullOrEmpty(Name))
        {
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(Name);
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);
        }
        else
        {
            writer.Write(0); // No name
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the mesh data.
    /// </summary>
    /// <param name="vertices">The new vertex coordinates.</param>
    /// <param name="indices">The new triangle indices.</param>
    /// <param name="normals">Optional new normal vectors.</param>
    /// <param name="uvCoordinates">Optional new UV coordinates.</param>
    public void UpdateMeshData(IEnumerable<float> vertices, IEnumerable<ushort> indices,
                              IEnumerable<float>? normals = null, IEnumerable<float>? uvCoordinates = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Vertices = vertices?.ToList() ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices?.ToList() ?? throw new ArgumentNullException(nameof(indices));
        Normals = normals?.ToList() ?? new List<float>();
        UVCoordinates = uvCoordinates?.ToList() ?? new List<float>();

        ValidateMeshData();
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Calculates and sets automatic normal vectors for the mesh.
    /// </summary>
    public void CalculateNormals()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (VertexCount == 0 || Indices.Count == 0)
            return;

        var normals = new float[Vertices.Count]; // Same size as vertices (3 components each)

        // Calculate face normals and accumulate
        for (int i = 0; i < Indices.Count; i += 3)
        {
            if (i + 2 >= Indices.Count) break;

            var i0 = Indices[i] * 3;
            var i1 = Indices[i + 1] * 3;
            var i2 = Indices[i + 2] * 3;

            if (i0 + 2 >= Vertices.Count || i1 + 2 >= Vertices.Count || i2 + 2 >= Vertices.Count)
                continue;

            // Get triangle vertices
            var v0 = new float[] { Vertices[i0], Vertices[i0 + 1], Vertices[i0 + 2] };
            var v1 = new float[] { Vertices[i1], Vertices[i1 + 1], Vertices[i1 + 2] };
            var v2 = new float[] { Vertices[i2], Vertices[i2 + 1], Vertices[i2 + 2] };

            // Calculate edges
            var edge1 = new float[] { v1[0] - v0[0], v1[1] - v0[1], v1[2] - v0[2] };
            var edge2 = new float[] { v2[0] - v0[0], v2[1] - v0[1], v2[2] - v0[2] };

            // Calculate cross product (face normal)
            var normal = new float[]
            {
                edge1[1] * edge2[2] - edge1[2] * edge2[1],
                edge1[2] * edge2[0] - edge1[0] * edge2[2],
                edge1[0] * edge2[1] - edge1[1] * edge2[0]
            };

            // Add to vertex normals
            for (int j = 0; j < 3; j++)
            {
                normals[i0 + j] += normal[j];
                normals[i1 + j] += normal[j];
                normals[i2 + j] += normal[j];
            }
        }

        // Normalize all vertex normals
        for (int i = 0; i < normals.Length; i += 3)
        {
            var length = MathF.Sqrt(normals[i] * normals[i] +
                                   normals[i + 1] * normals[i + 1] +
                                   normals[i + 2] * normals[i + 2]);

            if (length > 0.0001f) // Avoid division by zero
            {
                normals[i] /= length;
                normals[i + 1] /= length;
                normals[i + 2] /= length;
            }
        }

        Normals = normals;
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Private Methods

    private void ValidateMeshData()
    {
        if (Vertices.Count % 3 != 0)
            throw new ArgumentException("Vertex count must be divisible by 3");

        if (Indices.Count % 3 != 0)
            throw new ArgumentException("Index count must be divisible by 3");

        if (Normals.Count > 0 && Normals.Count != Vertices.Count)
            throw new ArgumentException("Normal count must match vertex count");

        if (UVCoordinates.Count > 0 && UVCoordinates.Count != (Vertices.Count / 3) * 2)
            throw new ArgumentException("UV coordinate count must be (vertex count * 2 / 3)");

        // Validate indices are within range
        var maxIndex = VertexCount - 1;
        foreach (var index in Indices)
        {
            if (index > maxIndex)
                throw new ArgumentException($"Index {index} is out of range (max: {maxIndex})");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the mesh resource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
