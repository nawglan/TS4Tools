namespace TS4Tools.Resources.World.Models;

/// <summary>
/// Represents color data with RGBA values and timing information for world lighting systems
/// </summary>
public class ColorData : IEquatable<ColorData>
{
    /// <summary>
    /// Red component (0.0 to 1.0)
    /// </summary>
    public float R { get; set; }

    /// <summary>
    /// Green component (0.0 to 1.0)
    /// </summary>
    public float G { get; set; }

    /// <summary>
    /// Blue component (0.0 to 1.0)
    /// </summary>
    public float B { get; set; }

    /// <summary>
    /// Alpha component (0.0 to 1.0)
    /// </summary>
    public float A { get; set; }

    /// <summary>
    /// Time component for timeline positioning
    /// </summary>
    public float Time { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorData"/> class with default values.
    /// </summary>
    public ColorData()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorData"/> class with specified values.
    /// </summary>
    /// <param name="r">Red component (0.0 to 1.0)</param>
    /// <param name="g">Green component (0.0 to 1.0)</param>
    /// <param name="b">Blue component (0.0 to 1.0)</param>
    /// <param name="a">Alpha component (0.0 to 1.0)</param>
    /// <param name="time">Time component for timeline positioning</param>
    public ColorData(float r, float g, float b, float a, float time)
    {
        R = r;
        G = g;
        B = b;
        A = a;
        Time = time;
    }

    /// <summary>
    /// Determines whether the specified <see cref="ColorData"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="ColorData"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public bool Equals(ColorData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return R.Equals(other.R) &&
               G.Equals(other.G) &&
               B.Equals(other.B) &&
               A.Equals(other.A) &&
               Time.Equals(other.Time);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as ColorData);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode() => HashCode.Combine(R, G, B, A, Time);

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => $"RGBA({R:F3}, {G:F3}, {B:F3}, {A:F3}) @ {Time:F3}";
}
