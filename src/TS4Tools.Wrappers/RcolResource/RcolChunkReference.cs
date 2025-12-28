
namespace TS4Tools.Wrappers;

/// <summary>
/// Reference type indicator bits for RCOL chunk references.
/// Source: GenericRCOLResource.cs lines 362-380
/// </summary>
public enum RcolReferenceType : byte
{
    /// <summary>
    /// Reference to a public chunk (below PublicChunks count).
    /// </summary>
    Public = 0x0,

    /// <summary>
    /// Reference to a private chunk (at or above PublicChunks count).
    /// </summary>
    Private = 0x1,

    /// <summary>
    /// External reference (no known usage).
    /// </summary>
    External = 0x2,

    /// <summary>
    /// Delayed reference to another resource via Resources list.
    /// </summary>
    Delayed = 0x3
}

/// <summary>
/// RCOL chunk reference encoding.
/// Uses top 4 bits for reference type, lower 28 bits for 1-based index.
/// Source: GenericRCOLResource.cs lines 385-660
/// </summary>
public readonly struct RcolChunkReference : IEquatable<RcolChunkReference>
{
    /// <summary>
    /// Size of a chunk reference in bytes.
    /// </summary>
    public const int Size = 4;

    private readonly uint _value;

    /// <summary>
    /// Creates a chunk reference from a raw value.
    /// </summary>
    public RcolChunkReference(uint value)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a chunk reference from type and index.
    /// </summary>
    /// <param name="refType">The reference type.</param>
    /// <param name="index">The 0-based index.</param>
    public RcolChunkReference(RcolReferenceType refType, int index)
    {
        if (index < -1)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be >= -1");

        _value = index == -1 ? 0 : (((uint)refType << 28) | ((uint)(index + 1) & 0x0FFFFFFF));
    }

    /// <summary>
    /// The raw encoded value.
    /// </summary>
    public uint RawValue => _value;

    /// <summary>
    /// Whether this reference is null/unset (value == 0).
    /// </summary>
    public bool IsNull => _value == 0;

    /// <summary>
    /// The reference type (top 4 bits).
    /// Source: GenericRCOLResource.cs line 643
    /// </summary>
    public RcolReferenceType RefType => _value == 0
        ? (RcolReferenceType)0xFF // Invalid for null reference
        : (RcolReferenceType)(_value >> 28);

    /// <summary>
    /// The 0-based index into the appropriate list.
    /// Returns -1 if the reference is null.
    /// Source: GenericRCOLResource.cs line 638 - "(chunkReference & 0x0FFFFFFF) - 1"
    /// </summary>
    public int Index => _value == 0 ? -1 : (int)(_value & 0x0FFFFFFF) - 1;

    /// <summary>
    /// Reads a chunk reference from a span.
    /// </summary>
    public static RcolChunkReference Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new ArgumentException($"Data too short for chunk reference: expected {Size} bytes, got {data.Length}");

        return new RcolChunkReference(BinaryPrimitives.ReadUInt32LittleEndian(data));
    }

    /// <summary>
    /// Writes this chunk reference to a span.
    /// </summary>
    public void Write(Span<byte> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException($"Destination too short for chunk reference: need {Size} bytes, got {destination.Length}");

        BinaryPrimitives.WriteUInt32LittleEndian(destination, _value);
    }

    /// <inheritdoc/>
    public bool Equals(RcolChunkReference other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RcolChunkReference other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(RcolChunkReference left, RcolChunkReference right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(RcolChunkReference left, RcolChunkReference right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        IsNull ? "(null)" : $"{RefType}[{Index}]";
}
