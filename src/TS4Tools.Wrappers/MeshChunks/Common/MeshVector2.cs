
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 2D vector with float components.
/// Source: s4pi Wrappers/MeshChunks/Common/Vector2.cs
/// </summary>
public readonly struct MeshVector2 : IEquatable<MeshVector2>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 8;

    public float X { get; }
    public float Y { get; }

    public MeshVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Reads a Vector2 from the span at the specified position.
    /// </summary>
    public static MeshVector2 Read(ReadOnlySpan<byte> data, ref int position)
    {
        float x = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        float y = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]);
        position += Size;
        return new MeshVector2(x, y);
    }

    /// <summary>
    /// Writes the vector to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        BinaryPrimitives.WriteSingleLittleEndian(data[position..], X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 4)..], Y);
        position += Size;
    }

    public static MeshVector2 Zero => new(0, 0);
    public static MeshVector2 One => new(1, 1);

    public bool Equals(MeshVector2 other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is MeshVector2 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(MeshVector2 left, MeshVector2 right) => left.Equals(right);
    public static bool operator !=(MeshVector2 left, MeshVector2 right) => !left.Equals(right);

    public override string ToString() => $"[{X,8:0.00000},{Y,8:0.00000}]";
}
