
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 4D vector with float components.
/// Source: s4pi Wrappers/MeshChunks/Common/Vector4.cs
/// </summary>
public readonly struct MeshVector4 : IEquatable<MeshVector4>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 16;

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }

    public MeshVector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Reads a Vector4 from the span at the specified position.
    /// </summary>
    public static MeshVector4 Read(ReadOnlySpan<byte> data, ref int position)
    {
        float x = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        float y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]);
        float z = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..]);
        float w = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 12)..]);
        position += Size;
        return new MeshVector4(x, y, z, w);
    }

    /// <summary>
    /// Writes the vector to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        BinaryPrimitives.WriteSingleLittleEndian(data[position..], X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 4)..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 8)..], Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 12)..], W);
        position += Size;
    }

    public static MeshVector4 Zero => new(0, 0, 0, 0);
    public static MeshVector4 One => new(1, 1, 1, 1);

    public bool Equals(MeshVector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    public override bool Equals(object? obj) => obj is MeshVector4 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public static bool operator ==(MeshVector4 left, MeshVector4 right) => left.Equals(right);
    public static bool operator !=(MeshVector4 left, MeshVector4 right) => !left.Equals(right);

    public override string ToString() => $"[{X,8:0.00000},{Y,8:0.00000},{Z,8:0.00000},{W,8:0.00000}]";
}
