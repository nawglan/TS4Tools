namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Represents a triangle face with three vertex indices.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 931-995
/// </summary>
public readonly struct GeomFace : IEquatable<GeomFace>
{
    /// <summary>Size in bytes when serialized (3 ushorts = 6 bytes).</summary>
    public const int Size = 6;

    /// <summary>Index of the first vertex.</summary>
    public ushort VertexIndex0 { get; }

    /// <summary>Index of the second vertex.</summary>
    public ushort VertexIndex1 { get; }

    /// <summary>Index of the third vertex.</summary>
    public ushort VertexIndex2 { get; }

    /// <summary>
    /// Creates a new face with the specified vertex indices.
    /// </summary>
    public GeomFace(ushort v0, ushort v1, ushort v2)
    {
        VertexIndex0 = v0;
        VertexIndex1 = v1;
        VertexIndex2 = v2;
    }

    /// <summary>
    /// Reads a face from the span.
    /// Source: GEOM.cs Face.Parse lines 953-959
    /// </summary>
    public static GeomFace Read(ReadOnlySpan<byte> data, ref int position)
    {
        var face = new GeomFace(
            BinaryPrimitives.ReadUInt16LittleEndian(data[position..]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 2)..]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 4)..])
        );
        position += Size;
        return face;
    }

    /// <summary>
    /// Writes the face to a binary writer.
    /// Source: GEOM.cs Face.UnParse lines 960-966
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(VertexIndex0);
        writer.Write(VertexIndex1);
        writer.Write(VertexIndex2);
    }

    /// <summary>
    /// Compares this face with another for equality.
    /// </summary>
    public bool Equals(GeomFace other)
    {
        return VertexIndex0 == other.VertexIndex0
            && VertexIndex1 == other.VertexIndex1
            && VertexIndex2 == other.VertexIndex2;
    }

    /// <summary>
    /// Compares this face with an object for equality.
    /// </summary>
    public override bool Equals(object? obj) => obj is GeomFace other && Equals(other);

    /// <summary>
    /// Gets a hash code for this face.
    /// </summary>
    public override int GetHashCode() =>
        HashCode.Combine(VertexIndex0, VertexIndex1, VertexIndex2);

    /// <summary>
    /// Compares two faces for equality.
    /// </summary>
    public static bool operator ==(GeomFace left, GeomFace right) => left.Equals(right);

    /// <summary>
    /// Compares two faces for inequality.
    /// </summary>
    public static bool operator !=(GeomFace left, GeomFace right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of this face.
    /// </summary>
    public override string ToString() => $"Face({VertexIndex0}, {VertexIndex1}, {VertexIndex2})";
}

/// <summary>
/// A list of faces in a GEOM block.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 996-1010
/// </summary>
public sealed class GeomFaceList
{
    private readonly List<GeomFace> _faces = [];

    /// <summary>The faces in this list.</summary>
    public IReadOnlyList<GeomFace> Faces => _faces;

    /// <summary>Number of faces.</summary>
    public int Count => _faces.Count;

    /// <summary>
    /// Creates an empty face list.
    /// </summary>
    public GeomFaceList()
    {
    }

    /// <summary>
    /// Creates a face list from existing faces.
    /// </summary>
    public GeomFaceList(IEnumerable<GeomFace> faces)
    {
        _faces.AddRange(faces);
    }

    /// <summary>
    /// Reads the face list from the span.
    /// Note: The count in the file is the number of indices (faces * 3), not the number of faces.
    /// Source: GEOM.cs FaceList.ReadCount line 1004
    /// </summary>
    public static GeomFaceList Read(ReadOnlySpan<byte> data, ref int position)
    {
        // Read the index count (number of face indices, which is faces * 3)
        int indexCount = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        int faceCount = indexCount / 3;

        var list = new GeomFaceList();
        for (int i = 0; i < faceCount; i++)
        {
            list._faces.Add(GeomFace.Read(data, ref position));
        }

        return list;
    }

    /// <summary>
    /// Writes the face list to a binary writer.
    /// Note: The count written is the number of indices (faces * 3), not the number of faces.
    /// Source: GEOM.cs FaceList.WriteCount line 1006
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        // Write the index count (number of face indices, which is faces * 3)
        writer.Write(_faces.Count * 3);

        foreach (var face in _faces)
        {
            face.Write(writer);
        }
    }

    /// <summary>
    /// Gets a face by index.
    /// </summary>
    public GeomFace this[int index] => _faces[index];

    /// <summary>
    /// Adds a face to the list.
    /// </summary>
    public void Add(GeomFace face) => _faces.Add(face);
}
