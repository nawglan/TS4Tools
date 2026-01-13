
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 3D vector with float components.
/// Source: s4pi Wrappers/MeshChunks/Common/Vector3.cs
/// </summary>
public readonly struct MeshVector3 : IEquatable<MeshVector3>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 12;

    /// <summary>The X component.</summary>
    public float X { get; }
    /// <summary>The Y component.</summary>
    public float Y { get; }
    /// <summary>The Z component.</summary>
    public float Z { get; }

    /// <summary>
    /// Initializes a new 3D vector with the specified components.
    /// </summary>
    public MeshVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Reads a Vector3 from the span at the specified position.
    /// </summary>
    public static MeshVector3 Read(ReadOnlySpan<byte> data, ref int position)
    {
        float x = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        float y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]);
        float z = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..]);
        position += Size;
        return new MeshVector3(x, y, z);
    }

    /// <summary>
    /// Writes the vector to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        BinaryPrimitives.WriteSingleLittleEndian(data[position..], X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 4)..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 8)..], Z);
        position += Size;
    }

    /// <summary>Returns a zero vector (0, 0, 0).</summary>
    public static MeshVector3 Zero => new(0, 0, 0);
    /// <summary>Returns a vector with all components set to one (1, 1, 1).</summary>
    public static MeshVector3 One => new(1, 1, 1);
    /// <summary>Returns the X-axis unit vector (1, 0, 0).</summary>
    public static MeshVector3 UnitX => new(1, 0, 0);
    /// <summary>Returns the Y-axis unit vector (0, 1, 0).</summary>
    public static MeshVector3 UnitY => new(0, 1, 0);
    /// <summary>Returns the Z-axis unit vector (0, 0, 1).</summary>
    public static MeshVector3 UnitZ => new(0, 0, 1);

    /// <inheritdoc/>
    public bool Equals(MeshVector3 other) => X == other.X && Y == other.Y && Z == other.Z;
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MeshVector3 other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(MeshVector3 left, MeshVector3 right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(MeshVector3 left, MeshVector3 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"[{X,8:0.00000},{Y,8:0.00000},{Z,8:0.00000}]";
}
