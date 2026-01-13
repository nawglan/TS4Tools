
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Defines a bounding box by its minimum and maximum vertices.
/// Source: s4pi/Interfaces/BoundingBox.cs
/// </summary>
public readonly struct BoundingBox : IEquatable<BoundingBox>
{
    /// <summary>Size in bytes when serialized (2 * MeshVector3.Size = 24 bytes).</summary>
    public const int Size = 24;

    /// <summary>Minimum corner of the bounding box.</summary>
    public MeshVector3 Min { get; }

    /// <summary>Maximum corner of the bounding box.</summary>
    public MeshVector3 Max { get; }

    /// <summary>
    /// Initializes a new bounding box with the specified minimum and maximum corners.
    /// </summary>
    public BoundingBox(MeshVector3 min, MeshVector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a bounding box from individual coordinates.
    /// </summary>
    public BoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        : this(new MeshVector3(minX, minY, minZ), new MeshVector3(maxX, maxY, maxZ))
    {
    }

    /// <summary>
    /// Reads a BoundingBox from the span at the specified position.
    /// </summary>
    public static BoundingBox Read(ReadOnlySpan<byte> data, ref int position)
    {
        var min = MeshVector3.Read(data, ref position);
        var max = MeshVector3.Read(data, ref position);
        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Writes the bounding box to the span at the specified position.
    /// </summary>
    public void Write(Span<byte> data, ref int position)
    {
        Min.Write(data, ref position);
        Max.Write(data, ref position);
    }

    /// <summary>
    /// Returns an empty bounding box at the origin.
    /// </summary>
    public static BoundingBox Empty => new(MeshVector3.Zero, MeshVector3.Zero);

    /// <inheritdoc/>
    public bool Equals(BoundingBox other) => Min == other.Min && Max == other.Max;
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BoundingBox other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Min, Max);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(BoundingBox left, BoundingBox right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(BoundingBox left, BoundingBox right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"[ Min: {Min} | Max: {Max} ]";
}
