namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A 4-byte packed value, typically used for bone indices or colors.
/// Source: s4pi Wrappers/MeshChunks/Common/UByte4.cs
/// </summary>
public readonly struct UByte4 : IEquatable<UByte4>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 4;

    /// <summary>The first byte component.</summary>
    public byte A { get; }
    /// <summary>The second byte component.</summary>
    public byte B { get; }
    /// <summary>The third byte component.</summary>
    public byte C { get; }
    /// <summary>The fourth byte component.</summary>
    public byte D { get; }

    /// <summary>
    /// Initializes a new UByte4 with the specified byte components.
    /// </summary>
    public UByte4(byte a, byte b, byte c, byte d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    /// <summary>
    /// Creates a UByte4 from a packed uint32.
    /// </summary>
    public UByte4(uint packed)
    {
        A = (byte)(packed & 0xFF);
        B = (byte)((packed >> 8) & 0xFF);
        C = (byte)((packed >> 16) & 0xFF);
        D = (byte)((packed >> 24) & 0xFF);
    }

    /// <summary>
    /// Reads a UByte4 from the span at the specified position.
    /// </summary>
    public static UByte4 Read(ReadOnlySpan<byte> data, ref int position)
    {
        var result = new UByte4(data[position], data[position + 1], data[position + 2], data[position + 3]);
        position += Size;
        return result;
    }

    /// <summary>
    /// Writes the value to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        data[position] = A;
        data[position + 1] = B;
        data[position + 2] = C;
        data[position + 3] = D;
        position += Size;
    }

    /// <summary>
    /// Converts to a packed uint32.
    /// </summary>
    public uint ToPacked() => A | ((uint)B << 8) | ((uint)C << 16) | ((uint)D << 24);

    /// <summary>Returns a zero value (0, 0, 0, 0).</summary>
    public static UByte4 Zero => new(0, 0, 0, 0);

    /// <inheritdoc/>
    public bool Equals(UByte4 other) => A == other.A && B == other.B && C == other.C && D == other.D;
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is UByte4 other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(A, B, C, D);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(UByte4 left, UByte4 right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(UByte4 left, UByte4 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"[{A:X2},{B:X2},{C:X2},{D:X2}]";
}
