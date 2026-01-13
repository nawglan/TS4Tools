namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Abstract base class for vertex elements in a GEOM block.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 439-459
/// </summary>
public abstract class GeomVertexElement : IEquatable<GeomVertexElement>
{
    /// <summary>The usage type of this element.</summary>
    public abstract GeomUsageType Usage { get; }

    /// <summary>Size in bytes when serialized.</summary>
    public abstract int Size { get; }

    /// <summary>Writes the element to a binary writer.</summary>
    public abstract void Write(BinaryWriter writer);

    /// <summary>Compares two vertex elements for equality.</summary>
    public abstract bool Equals(GeomVertexElement? other);

    /// <summary>Compares this element with an object for equality.</summary>
    public override bool Equals(object? obj) => obj is GeomVertexElement other && Equals(other);

    /// <summary>Gets a hash code for this element.</summary>
    public abstract override int GetHashCode();
}

/// <summary>
/// Position vertex element (3 floats: X, Y, Z).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 460-483
/// </summary>
public sealed class PositionElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.Position;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 12;

    /// <summary>X coordinate.</summary>
    public float X { get; set; }

    /// <summary>Y coordinate.</summary>
    public float Y { get; set; }

    /// <summary>Z coordinate.</summary>
    public float Z { get; set; }

    /// <summary>
    /// Creates an empty position element.
    /// </summary>
    public PositionElement() { }

    /// <summary>
    /// Creates a position element with the specified coordinates.
    /// </summary>
    public PositionElement(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Reads a position element from the span.
    /// </summary>
    public static PositionElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new PositionElement
        {
            X = BinaryPrimitives.ReadSingleLittleEndian(data[position..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..])
        };
        position += 12;
        return element;
    }

    /// <summary>
    /// Writes the position element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }

    /// <summary>
    /// Compares this position element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is PositionElement p && X == p.X && Y == p.Y && Z == p.Z;
    }

    /// <summary>
    /// Gets a hash code for this position element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Returns a string representation of this position element.
    /// </summary>
    public override string ToString() => $"Position({X:F3}, {Y:F3}, {Z:F3})";
}

/// <summary>
/// Normal vertex element (3 floats: X, Y, Z).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 484-495
/// </summary>
public sealed class NormalElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.Normal;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 12;

    /// <summary>X component of the normal vector.</summary>
    public float X { get; set; }

    /// <summary>Y component of the normal vector.</summary>
    public float Y { get; set; }

    /// <summary>Z component of the normal vector.</summary>
    public float Z { get; set; }

    /// <summary>
    /// Creates an empty normal element.
    /// </summary>
    public NormalElement() { }

    /// <summary>
    /// Creates a normal element with the specified vector components.
    /// </summary>
    public NormalElement(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Reads a normal element from the span.
    /// </summary>
    public static NormalElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new NormalElement
        {
            X = BinaryPrimitives.ReadSingleLittleEndian(data[position..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..])
        };
        position += 12;
        return element;
    }

    /// <summary>
    /// Writes the normal element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }

    /// <summary>
    /// Compares this normal element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is NormalElement n && X == n.X && Y == n.Y && Z == n.Z;
    }

    /// <summary>
    /// Gets a hash code for this normal element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Returns a string representation of this normal element.
    /// </summary>
    public override string ToString() => $"Normal({X:F3}, {Y:F3}, {Z:F3})";
}

/// <summary>
/// UV (texture coordinate) vertex element (2 floats: U, V).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 496-517
/// </summary>
public sealed class UVElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.UV;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 8;

    /// <summary>U texture coordinate.</summary>
    public float U { get; set; }

    /// <summary>V texture coordinate.</summary>
    public float V { get; set; }

    /// <summary>
    /// Creates an empty UV element.
    /// </summary>
    public UVElement() { }

    /// <summary>
    /// Creates a UV element with the specified texture coordinates.
    /// </summary>
    public UVElement(float u, float v)
    {
        U = u;
        V = v;
    }

    /// <summary>
    /// Reads a UV element from the span.
    /// </summary>
    public static UVElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new UVElement
        {
            U = BinaryPrimitives.ReadSingleLittleEndian(data[position..]),
            V = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..])
        };
        position += 8;
        return element;
    }

    /// <summary>
    /// Writes the UV element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(U);
        writer.Write(V);
    }

    /// <summary>
    /// Compares this UV element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is UVElement uv && U == uv.U && V == uv.V;
    }

    /// <summary>
    /// Gets a hash code for this UV element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(U, V);

    /// <summary>
    /// Returns a string representation of this UV element.
    /// </summary>
    public override string ToString() => $"UV({U:F3}, {V:F3})";
}

/// <summary>
/// Bone assignment vertex element (uint ID).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 518-537
/// </summary>
public sealed class BoneAssignmentElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.BoneAssignment;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 4;

    /// <summary>Bone assignment identifier.</summary>
    public uint Id { get; set; }

    /// <summary>
    /// Creates an empty bone assignment element.
    /// </summary>
    public BoneAssignmentElement() { }

    /// <summary>
    /// Creates a bone assignment element with the specified ID.
    /// </summary>
    public BoneAssignmentElement(uint id)
    {
        Id = id;
    }

    /// <summary>
    /// Reads a bone assignment element from the span.
    /// </summary>
    public static BoneAssignmentElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new BoneAssignmentElement
        {
            Id = BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        };
        position += 4;
        return element;
    }

    /// <summary>
    /// Writes the bone assignment element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(Id);
    }

    /// <summary>
    /// Compares this bone assignment element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is BoneAssignmentElement b && Id == b.Id;
    }

    /// <summary>
    /// Gets a hash code for this bone assignment element.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns a string representation of this bone assignment element.
    /// </summary>
    public override string ToString() => $"BoneAssignment({Id})";
}

/// <summary>
/// Weights vertex element using floats (4 floats: W1, W2, W3, W4).
/// Used in GEOM version 0x05.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 538-563
/// </summary>
public sealed class WeightsElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.Weights;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 16;

    /// <summary>First bone weight value.</summary>
    public float W1 { get; set; }

    /// <summary>Second bone weight value.</summary>
    public float W2 { get; set; }

    /// <summary>Third bone weight value.</summary>
    public float W3 { get; set; }

    /// <summary>Fourth bone weight value.</summary>
    public float W4 { get; set; }

    /// <summary>
    /// Creates an empty weights element.
    /// </summary>
    public WeightsElement() { }

    /// <summary>
    /// Creates a weights element with the specified weight values.
    /// </summary>
    public WeightsElement(float w1, float w2, float w3, float w4)
    {
        W1 = w1;
        W2 = w2;
        W3 = w3;
        W4 = w4;
    }

    /// <summary>
    /// Reads a weights element from the span.
    /// </summary>
    public static WeightsElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new WeightsElement
        {
            W1 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]),
            W2 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]),
            W3 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..]),
            W4 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 12)..])
        };
        position += 16;
        return element;
    }

    /// <summary>
    /// Writes the weights element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(W1);
        writer.Write(W2);
        writer.Write(W3);
        writer.Write(W4);
    }

    /// <summary>
    /// Compares this weights element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is WeightsElement w && W1 == w.W1 && W2 == w.W2 && W3 == w.W3 && W4 == w.W4;
    }

    /// <summary>
    /// Gets a hash code for this weights element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(W1, W2, W3, W4);

    /// <summary>
    /// Returns a string representation of this weights element.
    /// </summary>
    public override string ToString() => $"Weights({W1:F3}, {W2:F3}, {W3:F3}, {W4:F3})";
}

/// <summary>
/// Weights vertex element using bytes (4 bytes: W1, W2, W3, W4).
/// Used in GEOM version 0x0C.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 564-589
/// </summary>
public sealed class WeightBytesElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.Weights;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 4;

    /// <summary>First bone weight value (as byte).</summary>
    public byte W1 { get; set; }

    /// <summary>Second bone weight value (as byte).</summary>
    public byte W2 { get; set; }

    /// <summary>Third bone weight value (as byte).</summary>
    public byte W3 { get; set; }

    /// <summary>Fourth bone weight value (as byte).</summary>
    public byte W4 { get; set; }

    /// <summary>
    /// Creates an empty weight bytes element.
    /// </summary>
    public WeightBytesElement() { }

    /// <summary>
    /// Creates a weight bytes element with the specified weight values.
    /// </summary>
    public WeightBytesElement(byte w1, byte w2, byte w3, byte w4)
    {
        W1 = w1;
        W2 = w2;
        W3 = w3;
        W4 = w4;
    }

    /// <summary>
    /// Reads a weight bytes element from the span.
    /// </summary>
    public static WeightBytesElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new WeightBytesElement
        {
            W1 = data[position],
            W2 = data[position + 1],
            W3 = data[position + 2],
            W4 = data[position + 3]
        };
        position += 4;
        return element;
    }

    /// <summary>
    /// Writes the weight bytes element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(W1);
        writer.Write(W2);
        writer.Write(W3);
        writer.Write(W4);
    }

    /// <summary>
    /// Compares this weight bytes element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is WeightBytesElement w && W1 == w.W1 && W2 == w.W2 && W3 == w.W3 && W4 == w.W4;
    }

    /// <summary>
    /// Gets a hash code for this weight bytes element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(W1, W2, W3, W4);

    /// <summary>
    /// Returns a string representation of this weight bytes element.
    /// </summary>
    public override string ToString() => $"WeightBytes({W1}, {W2}, {W3}, {W4})";
}

/// <summary>
/// Tangent normal vertex element (3 floats: X, Y, Z).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 590-601
/// </summary>
public sealed class TangentNormalElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.TangentNormal;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 12;

    /// <summary>X component of the tangent normal vector.</summary>
    public float X { get; set; }

    /// <summary>Y component of the tangent normal vector.</summary>
    public float Y { get; set; }

    /// <summary>Z component of the tangent normal vector.</summary>
    public float Z { get; set; }

    /// <summary>
    /// Creates an empty tangent normal element.
    /// </summary>
    public TangentNormalElement() { }

    /// <summary>
    /// Creates a tangent normal element with the specified vector components.
    /// </summary>
    public TangentNormalElement(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Reads a tangent normal element from the span.
    /// </summary>
    public static TangentNormalElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new TangentNormalElement
        {
            X = BinaryPrimitives.ReadSingleLittleEndian(data[position..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..])
        };
        position += 12;
        return element;
    }

    /// <summary>
    /// Writes the tangent normal element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }

    /// <summary>
    /// Compares this tangent normal element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is TangentNormalElement t && X == t.X && Y == t.Y && Z == t.Z;
    }

    /// <summary>
    /// Gets a hash code for this tangent normal element.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Returns a string representation of this tangent normal element.
    /// </summary>
    public override string ToString() => $"TangentNormal({X:F3}, {Y:F3}, {Z:F3})";
}

/// <summary>
/// Color vertex element (ARGB int).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 602-620
/// </summary>
public sealed class ColorElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.Color;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 4;

    /// <summary>Color value in ARGB format.</summary>
    public int Argb { get; set; }

    /// <summary>
    /// Creates an empty color element.
    /// </summary>
    public ColorElement() { }

    /// <summary>
    /// Creates a color element with the specified ARGB value.
    /// </summary>
    public ColorElement(int argb)
    {
        Argb = argb;
    }

    /// <summary>
    /// Reads a color element from the span.
    /// </summary>
    public static ColorElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new ColorElement
        {
            Argb = BinaryPrimitives.ReadInt32LittleEndian(data[position..])
        };
        position += 4;
        return element;
    }

    /// <summary>
    /// Writes the color element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(Argb);
    }

    /// <summary>Gets the alpha channel value (0-255).</summary>
    public byte A => (byte)((Argb >> 24) & 0xFF);

    /// <summary>Gets the red channel value (0-255).</summary>
    public byte R => (byte)((Argb >> 16) & 0xFF);

    /// <summary>Gets the green channel value (0-255).</summary>
    public byte G => (byte)((Argb >> 8) & 0xFF);

    /// <summary>Gets the blue channel value (0-255).</summary>
    public byte B => (byte)(Argb & 0xFF);

    /// <summary>
    /// Compares this color element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is ColorElement c && Argb == c.Argb;
    }

    /// <summary>
    /// Gets a hash code for this color element.
    /// </summary>
    public override int GetHashCode() => Argb.GetHashCode();

    /// <summary>
    /// Returns a string representation of this color element.
    /// </summary>
    public override string ToString() => $"Color(A={A}, R={R}, G={G}, B={B})";
}

/// <summary>
/// Vertex ID element (uint).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 621-632
/// </summary>
public sealed class VertexIDElement : GeomVertexElement
{
    /// <summary>The usage type for this element.</summary>
    public override GeomUsageType Usage => GeomUsageType.VertexID;

    /// <summary>Size in bytes when serialized.</summary>
    public override int Size => 4;

    /// <summary>Vertex identifier value.</summary>
    public uint Id { get; set; }

    /// <summary>
    /// Creates an empty vertex ID element.
    /// </summary>
    public VertexIDElement() { }

    /// <summary>
    /// Creates a vertex ID element with the specified ID.
    /// </summary>
    public VertexIDElement(uint id)
    {
        Id = id;
    }

    /// <summary>
    /// Reads a vertex ID element from the span.
    /// </summary>
    public static VertexIDElement Read(ReadOnlySpan<byte> data, ref int position)
    {
        var element = new VertexIDElement
        {
            Id = BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        };
        position += 4;
        return element;
    }

    /// <summary>
    /// Writes the vertex ID element to a binary writer.
    /// </summary>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(Id);
    }

    /// <summary>
    /// Compares this vertex ID element with another vertex element for equality.
    /// </summary>
    public override bool Equals(GeomVertexElement? other)
    {
        return other is VertexIDElement v && Id == v.Id;
    }

    /// <summary>
    /// Gets a hash code for this vertex ID element.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns a string representation of this vertex ID element.
    /// </summary>
    public override string ToString() => $"VertexID({Id})";
}

/// <summary>
/// Reads a vertex element based on the usage type and version.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 647-666
/// </summary>
public static class GeomVertexElementReader
{
    /// <summary>
    /// Reads a vertex element from the span based on the usage type and GEOM version.
    /// </summary>
    public static GeomVertexElement Read(
        ReadOnlySpan<byte> data,
        ref int position,
        GeomUsageType usage,
        uint version)
    {
        return usage switch
        {
            GeomUsageType.Position => PositionElement.Read(data, ref position),
            GeomUsageType.Normal => NormalElement.Read(data, ref position),
            GeomUsageType.UV => UVElement.Read(data, ref position),
            GeomUsageType.BoneAssignment => BoneAssignmentElement.Read(data, ref position),
            GeomUsageType.Weights => version == 0x00000005
                ? WeightsElement.Read(data, ref position)
                : WeightBytesElement.Read(data, ref position),
            GeomUsageType.TangentNormal => TangentNormalElement.Read(data, ref position),
            GeomUsageType.Color => ColorElement.Read(data, ref position),
            GeomUsageType.VertexID => VertexIDElement.Read(data, ref position),
            _ => throw new InvalidDataException($"Unknown GEOM usage type: {usage}")
        };
    }
}

/// <summary>
/// A single vertex containing all its elements based on the format list.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 813-897
/// </summary>
public sealed class GeomVertex
{
    private readonly List<GeomVertexElement> _elements = [];

    /// <summary>The GEOM version.</summary>
    public uint Version { get; }

    /// <summary>The elements making up this vertex.</summary>
    public IReadOnlyList<GeomVertexElement> Elements => _elements;

    /// <summary>
    /// Creates an empty vertex.
    /// </summary>
    public GeomVertex(uint version)
    {
        Version = version;
    }

    /// <summary>
    /// Creates a vertex with the specified elements.
    /// </summary>
    public GeomVertex(uint version, IEnumerable<GeomVertexElement> elements)
    {
        Version = version;
        _elements.AddRange(elements);
    }

    /// <summary>
    /// Reads a vertex from the span based on the format list.
    /// Source: GEOM.cs ElementList constructor lines 641-668
    /// </summary>
    public static GeomVertex Read(
        ReadOnlySpan<byte> data,
        ref int position,
        GeomVertexFormatList formatList)
    {
        var vertex = new GeomVertex(formatList.Version);

        foreach (var format in formatList.Formats)
        {
            var element = GeomVertexElementReader.Read(data, ref position, format.Usage, formatList.Version);
            vertex._elements.Add(element);
        }

        return vertex;
    }

    /// <summary>
    /// Writes the vertex to a binary writer.
    /// Source: GEOM.cs ElementList.UnParse lines 718-742
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        foreach (var element in _elements)
        {
            element.Write(writer);
        }
    }

    /// <summary>
    /// Gets an element by usage type.
    /// </summary>
    public GeomVertexElement? GetElement(GeomUsageType usage)
    {
        return _elements.FirstOrDefault(e => e.Usage == usage);
    }

    /// <summary>
    /// Gets the position element if present.
    /// </summary>
    public PositionElement? Position => GetElement(GeomUsageType.Position) as PositionElement;

    /// <summary>
    /// Gets the normal element if present.
    /// </summary>
    public NormalElement? Normal => GetElement(GeomUsageType.Normal) as NormalElement;

    /// <summary>
    /// Gets the UV element if present.
    /// </summary>
    public UVElement? UV => GetElement(GeomUsageType.UV) as UVElement;
}

/// <summary>
/// A list of vertices in a GEOM block.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 899-928
/// </summary>
public sealed class GeomVertexList
{
    private readonly List<GeomVertex> _vertices = [];

    /// <summary>The GEOM version.</summary>
    public uint Version { get; }

    /// <summary>The format list defining vertex structure.</summary>
    public GeomVertexFormatList FormatList { get; }

    /// <summary>The vertices in this list.</summary>
    public IReadOnlyList<GeomVertex> Vertices => _vertices;

    /// <summary>Number of vertices.</summary>
    public int Count => _vertices.Count;

    /// <summary>
    /// Creates an empty vertex list.
    /// </summary>
    public GeomVertexList(uint version, GeomVertexFormatList formatList)
    {
        Version = version;
        FormatList = formatList;
    }

    /// <summary>
    /// Reads the vertex list from the span.
    /// Source: GEOM.cs VertexDataList constructor lines 907-908
    /// </summary>
    public static GeomVertexList Read(
        ReadOnlySpan<byte> data,
        ref int position,
        int vertexCount,
        GeomVertexFormatList formatList)
    {
        var list = new GeomVertexList(formatList.Version, formatList);

        for (int i = 0; i < vertexCount; i++)
        {
            list._vertices.Add(GeomVertex.Read(data, ref position, formatList));
        }

        return list;
    }

    /// <summary>
    /// Writes the vertex list to a binary writer.
    /// Note: Count is NOT written here (it's written separately in GEOM.Parse).
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        foreach (var vertex in _vertices)
        {
            vertex.Write(writer);
        }
    }

    /// <summary>
    /// Gets a vertex by index.
    /// </summary>
    public GeomVertex this[int index] => _vertices[index];
}
