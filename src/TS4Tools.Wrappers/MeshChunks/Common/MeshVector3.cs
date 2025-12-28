using System.Buffers.Binary;

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 3D vector with float components.
/// Source: s4pi Wrappers/MeshChunks/Common/Vector3.cs
/// </summary>
public readonly struct MeshVector3 : IEquatable<MeshVector3>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 12;

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

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

    public static MeshVector3 Zero => new(0, 0, 0);
    public static MeshVector3 One => new(1, 1, 1);
    public static MeshVector3 UnitX => new(1, 0, 0);
    public static MeshVector3 UnitY => new(0, 1, 0);
    public static MeshVector3 UnitZ => new(0, 0, 1);

    public bool Equals(MeshVector3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is MeshVector3 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public static bool operator ==(MeshVector3 left, MeshVector3 right) => left.Equals(right);
    public static bool operator !=(MeshVector3 left, MeshVector3 right) => !left.Equals(right);

    public override string ToString() => $"[{X,8:0.00000},{Y,8:0.00000},{Z,8:0.00000}]";
}
