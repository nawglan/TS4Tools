/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *    /// <summary>
    /// Event raised when the resource content changes.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version for this resource.
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
        nameof(Shader),
        nameof(VertexCount),
        nameof(FaceCount),
        nameof(BoneCount),
        "MaterialCount"
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

    #endregion

    #region Constructors                                                                    *
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

using System.Buffers.Binary;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Represents a geometry resource containing 3D mesh data for The Sims 4.
/// Supports vertex data, faces, materials, and skeletal animation information.
/// </summary>
public sealed class GeometryResource : IResource, IDisposable
{
    private readonly MemoryStream _stream;
    private bool _disposed;
    private bool _modified;

    #region Geometry Data Properties

    /// <summary>
    /// The GEOM tag identifier.
    /// </summary>
    public uint Tag { get; private set; } = 0x47454F4D; // "GEOM"

    /// <summary>
    /// The geometry format version.
    /// </summary>
    public uint Version { get; private set; } = 0x0000000E;

    /// <summary>
    /// The shader type used for rendering this geometry.
    /// </summary>
    public ShaderType Shader { get; set; }

    /// <summary>
    /// The merge group identifier for batching optimization.
    /// </summary>
    public uint MergeGroup { get; set; }

    /// <summary>
    /// The sort order for rendering priority.
    /// </summary>
    public uint SortOrder { get; set; }

    /// <summary>
    /// The vertex format definitions describing the vertex data layout.
    /// </summary>
    public IReadOnlyList<VertexFormat> VertexFormats { get; private set; } = Array.Empty<VertexFormat>();

    /// <summary>
    /// The raw vertex data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> VertexData { get; private set; }

    /// <summary>
    /// The number of vertices in this geometry.
    /// </summary>
    public int VertexCount { get; private set; }

    /// <summary>
    /// The triangle faces that define the mesh topology.
    /// </summary>
    public IReadOnlyList<Face> Faces { get; private set; } = Array.Empty<Face>();

    /// <summary>
    /// Gets the number of faces in the geometry.
    /// </summary>
    public int FaceCount => Faces.Count;

    /// <summary>
    /// Gets the number of bones in the geometry.
    /// </summary>
    public int BoneCount => BoneHashes?.Count ?? 0;

    /// <summary>
    /// The material hashes used by this geometry.
    /// </summary>
    public IReadOnlyList<uint> Materials { get; private set; } = Array.Empty<uint>();

    /// <summary>
    /// The skin index for skeletal animation (-1 if no skinning).
    /// </summary>
    public int SkinIndex { get; set; } = -1;

    /// <summary>
    /// UV stitching information for seamless texture mapping.
    /// </summary>
    public IReadOnlyList<UVStitch> UVStitches { get; private set; } = Array.Empty<UVStitch>();

    /// <summary>
    /// Seam stitching information for mesh optimization.
    /// </summary>
    public IReadOnlyList<SeamStitch> SeamStitches { get; private set; } = Array.Empty<SeamStitch>();

    /// <summary>
    /// Slot ray intersections for object placement.
    /// </summary>
    public IReadOnlyList<SlotrayIntersection> SlotrayIntersections { get; private set; } = Array.Empty<SlotrayIntersection>();

    /// <summary>
    /// Bone hash values for skeletal animation.
    /// </summary>
    public IReadOnlyList<uint> BoneHashes { get; private set; } = Array.Empty<uint>();

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
        nameof(Shader),
        nameof(VertexCount),
        nameof(FaceCount),
        nameof(BoneCount),
        "MaterialCount"
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

    #region Constructors

    /// <summary>
    /// Initializes a new empty geometry resource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    public GeometryResource(int requestedApiVersion = 1)
    {
        RequestedApiVersion = requestedApiVersion;
        _stream = new MemoryStream();
        _modified = true;
    }

    /// <summary>
    /// Initializes a new geometry resource from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing geometry data.</param>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid geometry data.</exception>
    public GeometryResource(Stream stream, int requestedApiVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(stream);

        RequestedApiVersion = requestedApiVersion;
        _stream = new MemoryStream();
        stream.CopyTo(_stream);
        _stream.Position = 0;

        ParseGeometryData();
    }

    /// <summary>
    /// Initializes a new geometry resource with the specified parameters.
    /// </summary>
    /// <param name="shader">The shader type.</param>
    /// <param name="vertexFormats">The vertex format definitions.</param>
    /// <param name="vertexData">The raw vertex data.</param>
    /// <param name="faces">The triangle faces.</param>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    public GeometryResource(ShaderType shader, IEnumerable<VertexFormat> vertexFormats,
                           ReadOnlyMemory<byte> vertexData, IEnumerable<Face> faces, int requestedApiVersion = 1)
    {
        RequestedApiVersion = requestedApiVersion;
        _stream = new MemoryStream();
        Shader = shader;
        VertexFormats = vertexFormats?.ToList() ?? throw new ArgumentNullException(nameof(vertexFormats));
        VertexData = vertexData;
        Faces = faces?.ToList() ?? throw new ArgumentNullException(nameof(faces));

        // Calculate vertex count from vertex data size and format
        CalculateVertexCount();
        _modified = true;
    }

    #endregion

    #region Data Parsing

    private void ParseGeometryData()
    {
        if (_stream.Length < 24) // Minimum size for header
            throw new InvalidDataException("Geometry stream too small");

        using var reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Read header
        Tag = reader.ReadUInt32();
        if (Tag != 0x47454F4D) // "GEOM"
            throw new InvalidDataException($"Invalid GEOM tag: 0x{Tag:X8}");

        Version = reader.ReadUInt32();
        if (Version != 0x00000005 && Version != 0x0000000C &&
            Version != 0x0000000D && Version != 0x0000000E)
            throw new InvalidDataException($"Unsupported GEOM version: 0x{Version:X8}");

        // Skip TGI offset and size (we'll handle these later if needed)
        var tgiOffset = reader.ReadUInt32();
        var tgiSize = reader.ReadUInt32();

        // Read shader
        Shader = (ShaderType)reader.ReadUInt32();

        // Skip shader data if present
        if (Shader != ShaderType.None)
        {
            var shaderDataSize = reader.ReadUInt32();
            reader.BaseStream.Seek(shaderDataSize, SeekOrigin.Current);
        }

        // Read geometry properties
        MergeGroup = reader.ReadUInt32();
        SortOrder = reader.ReadUInt32();

        // Read vertex data
        VertexCount = reader.ReadInt32();
        ReadVertexFormats(reader);
        ReadVertexData(reader);

        // Read face data
        var numSubMeshes = reader.ReadUInt32();
        if (numSubMeshes != 1)
            throw new InvalidDataException($"Expected 1 submesh, found {numSubMeshes}");

        var facePointSize = reader.ReadByte();
        if (facePointSize != 2)
            throw new InvalidDataException($"Expected face point size 2, found {facePointSize}");

        ReadFaces(reader);

        // Read version-specific data
        if (Version == 0x00000005)
        {
            SkinIndex = reader.ReadInt32();
        }
        else if (Version >= 0x0000000C)
        {
            ReadUVStitches(reader);

            if (Version >= 0x0000000D)
            {
                ReadSeamStitches(reader);
            }

            ReadSlotrayIntersections(reader);
        }

        // Read bone hashes
        ReadBoneHashes(reader);
    }

    private void ReadVertexFormats(BinaryReader reader)
    {
        var formatCount = reader.ReadInt32();
        var formats = new VertexFormat[formatCount];

        for (int i = 0; i < formatCount; i++)
        {
            var usage = (UsageType)reader.ReadUInt32();
            var dataType = (DataType)reader.ReadUInt32();
            var subUsage = reader.ReadByte();
            var reserved = reader.ReadByte();

            // Skip padding
            reader.ReadUInt16();

            formats[i] = new VertexFormat(usage, dataType, subUsage, reserved);
        }

        VertexFormats = formats;
    }

    private void ReadVertexData(BinaryReader reader)
    {
        // Calculate vertex stride
        var stride = VertexFormats.Sum(f => f.GetElementSize());
        var totalSize = VertexCount * stride;

        if (totalSize > 0)
        {
            var data = reader.ReadBytes(totalSize);
            VertexData = data;
        }
    }

    private void ReadFaces(BinaryReader reader)
    {
        var faceCount = reader.ReadInt32();
        var faces = new Face[faceCount];

        for (int i = 0; i < faceCount; i++)
        {
            var a = reader.ReadUInt16();
            var b = reader.ReadUInt16();
            var c = reader.ReadUInt16();
            faces[i] = new Face(a, b, c);
        }

        Faces = faces;
    }

    private void ReadUVStitches(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var stitches = new UVStitch[count];

        for (int i = 0; i < count; i++)
        {
            var vertexA = reader.ReadUInt32();
            var vertexB = reader.ReadUInt32();
            stitches[i] = new UVStitch(vertexA, vertexB);
        }

        UVStitches = stitches;
    }

    private void ReadSeamStitches(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var stitches = new SeamStitch[count];

        for (int i = 0; i < count; i++)
        {
            var vertexA = reader.ReadUInt32();
            var vertexB = reader.ReadUInt32();
            stitches[i] = new SeamStitch(vertexA, vertexB);
        }

        SeamStitches = stitches;
    }

    private void ReadSlotrayIntersections(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var intersections = new SlotrayIntersection[count];

        for (int i = 0; i < count; i++)
        {
            var position = reader.ReadUInt32();
            var normal = reader.ReadUInt32();
            intersections[i] = new SlotrayIntersection(position, normal);
        }

        SlotrayIntersections = intersections;
    }

    private void ReadBoneHashes(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var hashes = new uint[count];

        for (int i = 0; i < count; i++)
        {
            hashes[i] = reader.ReadUInt32();
        }

        BoneHashes = hashes;
    }

    #endregion

    #region Data Writing

    private void WriteToStream()
    {
        _stream.SetLength(0);
        _stream.Position = 0;

        using var writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Write header
        writer.Write(Tag);
        writer.Write(Version);

        // Reserve space for TGI offset and size (we'll update these later)
        var tgiOffsetPosition = writer.BaseStream.Position;
        writer.Write(0U); // TGI offset
        writer.Write(0U); // TGI size

        // Write shader
        writer.Write((uint)Shader);

        // Skip shader data for now (not implemented)
        if (Shader != ShaderType.None)
        {
            writer.Write(0U); // Shader data size
        }

        // Write geometry properties
        writer.Write(MergeGroup);
        writer.Write(SortOrder);

        // Write vertex data
        writer.Write(VertexCount);
        WriteVertexFormats(writer);
        WriteVertexData(writer);

        // Write face data
        writer.Write(1U); // Number of submeshes
        writer.Write((byte)2); // Face point size
        WriteFaces(writer);

        // Write version-specific data
        if (Version == 0x00000005)
        {
            writer.Write(SkinIndex);
        }
        else if (Version >= 0x0000000C)
        {
            WriteUVStitches(writer);

            if (Version >= 0x0000000D)
            {
                WriteSeamStitches(writer);
            }

            WriteSlotrayIntersections(writer);
        }

        // Write bone hashes
        WriteBoneHashes(writer);

        // For now, we'll skip TGI blocks (can be implemented later if needed)
        var currentPosition = writer.BaseStream.Position;
        writer.BaseStream.Seek(tgiOffsetPosition, SeekOrigin.Begin);
        writer.Write((uint)currentPosition);
        writer.Write(0U); // Empty TGI size
        writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
    }

    private void WriteVertexFormats(BinaryWriter writer)
    {
        writer.Write(VertexFormats.Count);

        foreach (var format in VertexFormats)
        {
            writer.Write((uint)format.Usage);
            writer.Write((uint)format.DataType);
            writer.Write(format.SubUsage);
            writer.Write(format.Reserved);
            writer.Write((ushort)0); // Padding
        }
    }

    private void WriteVertexData(BinaryWriter writer)
    {
        if (!VertexData.IsEmpty)
        {
            writer.Write(VertexData.Span);
        }
    }

    private void WriteFaces(BinaryWriter writer)
    {
        writer.Write(Faces.Count);

        foreach (var face in Faces)
        {
            writer.Write(face.A);
            writer.Write(face.B);
            writer.Write(face.C);
        }
    }

    private void WriteUVStitches(BinaryWriter writer)
    {
        writer.Write(UVStitches.Count);

        foreach (var stitch in UVStitches)
        {
            writer.Write(stitch.VertexA);
            writer.Write(stitch.VertexB);
        }
    }

    private void WriteSeamStitches(BinaryWriter writer)
    {
        writer.Write(SeamStitches.Count);

        foreach (var stitch in SeamStitches)
        {
            writer.Write(stitch.VertexA);
            writer.Write(stitch.VertexB);
        }
    }

    private void WriteSlotrayIntersections(BinaryWriter writer)
    {
        writer.Write(SlotrayIntersections.Count);

        foreach (var intersection in SlotrayIntersections)
        {
            writer.Write(intersection.Position);
            writer.Write(intersection.Normal);
        }
    }

    private void WriteBoneHashes(BinaryWriter writer)
    {
        writer.Write(BoneHashes.Count);

        foreach (var hash in BoneHashes)
        {
            writer.Write(hash);
        }
    }

    private void CalculateVertexCount()
    {
        if (VertexFormats.Count == 0 || VertexData.IsEmpty)
        {
            VertexCount = 0;
            return;
        }

        var stride = VertexFormats.Sum(f => f.GetElementSize());
        VertexCount = stride > 0 ? VertexData.Length / stride : 0;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the vertex data with new formats and data.
    /// </summary>
    /// <param name="vertexFormats">The new vertex format definitions.</param>
    /// <param name="vertexData">The new vertex data.</param>
    public void UpdateVertexData(IEnumerable<VertexFormat> vertexFormats, ReadOnlyMemory<byte> vertexData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        VertexFormats = vertexFormats?.ToList() ?? throw new ArgumentNullException(nameof(vertexFormats));
        VertexData = vertexData;

        CalculateVertexCount();
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the face data.
    /// </summary>
    /// <param name="faces">The new face data.</param>
    public void UpdateFaces(IEnumerable<Face> faces)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Faces = faces?.ToList() ?? throw new ArgumentNullException(nameof(faces));
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the bone hash data.
    /// </summary>
    /// <param name="boneHashes">The new bone hash data.</param>
    public void UpdateBoneHashes(IEnumerable<uint> boneHashes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BoneHashes = boneHashes?.ToList() ?? throw new ArgumentNullException(nameof(boneHashes));
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Content Field Access

    private TypedValue GetFieldByIndex(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(Shader),
            1 => TypedValue.Create(VertexCount),
            2 => TypedValue.Create(FaceCount),
            3 => TypedValue.Create(BoneCount),
            4 => TypedValue.Create(Materials?.Count ?? 0),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        switch (index)
        {
            case 0:
                if (value.GetValue<ShaderType>() is ShaderType shader)
                    Shader = shader;
                break;
            case 1:
            case 2:
            case 3:
            case 4:
                // Read-only properties
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
        OnResourceChanged();
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(Shader) => TypedValue.Create(Shader),
            nameof(VertexCount) => TypedValue.Create(VertexCount),
            nameof(FaceCount) => TypedValue.Create(FaceCount),
            nameof(BoneCount) => TypedValue.Create(BoneCount),
            "MaterialCount" => TypedValue.Create(Materials?.Count ?? 0),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            case nameof(Shader):
                if (value.GetValue<ShaderType>() is ShaderType shader)
                    Shader = shader;
                break;
            case nameof(VertexCount):
            case nameof(FaceCount):
            case nameof(BoneCount):
            case "MaterialCount":
                // Read-only properties
                break;
            default:
                throw new ArgumentException($"Unknown field: {name}", nameof(name));
        }
        OnResourceChanged();
    }

    private void OnResourceChanged()
    {
        _modified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the geometry resource.
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
