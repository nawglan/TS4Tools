
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 4x3 transformation matrix used for bone inverse bind poses.
/// Stores Right, Up, Back, and Translate column vectors.
///
/// Binary layout: 12 floats stored in row-major order, but interpreted as column vectors.
/// Source: s4pi Wrappers/MeshChunks/Common/Matrix43.cs lines 68-93
/// </summary>
public readonly struct Matrix43 : IEquatable<Matrix43>
{
    /// <summary>Size in bytes when serialized (12 floats = 48 bytes).</summary>
    public const int Size = 48;

    /// <summary>The right (X-axis) column vector.</summary>
    public MeshVector3 Right { get; }

    /// <summary>The up (Y-axis) column vector.</summary>
    public MeshVector3 Up { get; }

    /// <summary>The back (Z-axis) column vector.</summary>
    public MeshVector3 Back { get; }

    /// <summary>The translation column vector.</summary>
    public MeshVector3 Translate { get; }

    public Matrix43(MeshVector3 right, MeshVector3 up, MeshVector3 back, MeshVector3 translate)
    {
        Right = right;
        Up = up;
        Back = back;
        Translate = translate;
    }

    /// <summary>
    /// Returns the identity matrix (scale 1, no rotation, no translation).
    /// </summary>
    public static Matrix43 Identity => new(
        MeshVector3.UnitX,
        MeshVector3.UnitY,
        MeshVector3.UnitZ,
        MeshVector3.Zero
    );

    /// <summary>
    /// Reads a Matrix43 from the span at the specified position.
    /// The legacy format stores data in row-major order:
    /// Row 0: m00, m01, m02, m03 (X components of each column)
    /// Row 1: m10, m11, m12, m13 (Y components of each column)
    /// Row 2: m20, m21, m22, m23 (Z components of each column)
    /// Source: s4pi Matrix43.cs Parse() lines 68-93
    /// </summary>
    public static Matrix43 Read(ReadOnlySpan<byte> data, ref int position)
    {
        // Read row 0 (X components of Right, Up, Back, Translate)
        float m00 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        float m01 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 4)..]);
        float m02 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 8)..]);
        float m03 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 12)..]);

        // Read row 1 (Y components)
        float m10 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 16)..]);
        float m11 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 20)..]);
        float m12 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 24)..]);
        float m13 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 28)..]);

        // Read row 2 (Z components)
        float m20 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 32)..]);
        float m21 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 36)..]);
        float m22 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 40)..]);
        float m23 = BinaryPrimitives.ReadSingleLittleEndian(data[(position + 44)..]);

        position += Size;

        // Reconstruct column vectors from row components
        return new Matrix43(
            new MeshVector3(m00, m10, m20), // Right column
            new MeshVector3(m01, m11, m21), // Up column
            new MeshVector3(m02, m12, m22), // Back column
            new MeshVector3(m03, m13, m23)  // Translate column
        );
    }

    /// <summary>
    /// Writes the matrix to the span at the specified position in row-major order.
    /// Source: s4pi Matrix43.cs UnParse() lines 94-111
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        // Write row 0 (X components)
        BinaryPrimitives.WriteSingleLittleEndian(data[position..], Right.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 4)..], Up.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 8)..], Back.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 12)..], Translate.X);

        // Write row 1 (Y components)
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 16)..], Right.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 20)..], Up.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 24)..], Back.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 28)..], Translate.Y);

        // Write row 2 (Z components)
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 32)..], Right.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 36)..], Up.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 40)..], Back.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[(position + 44)..], Translate.Z);

        position += Size;
    }

    public bool Equals(Matrix43 other) =>
        Right == other.Right && Up == other.Up && Back == other.Back && Translate == other.Translate;

    public override bool Equals(object? obj) => obj is Matrix43 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Right, Up, Back, Translate);
    public static bool operator ==(Matrix43 left, Matrix43 right) => left.Equals(right);
    public static bool operator !=(Matrix43 left, Matrix43 right) => !left.Equals(right);

    public override string ToString() => $"{Right},{Up},{Back},{Translate}";
}
