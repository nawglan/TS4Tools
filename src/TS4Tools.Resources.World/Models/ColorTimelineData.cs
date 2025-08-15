namespace TS4Tools.Resources.World.Models;

/// <summary>
/// Represents a collection of color data points for a specific timeline element
/// </summary>
public class ColorTimelineData : IEquatable<ColorTimelineData>
{
    /// <summary>
    /// Gets the collection of color data points
    /// </summary>
    public IList<ColorData> ColorData { get; } = new List<ColorData>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorTimelineData"/> class.
    /// </summary>
    public ColorTimelineData()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorTimelineData"/> class with the specified color data.
    /// </summary>
    /// <param name="colorData">The color data to initialize with.</param>
    public ColorTimelineData(IEnumerable<ColorData> colorData)
    {
        foreach (var data in colorData)
            ColorData.Add(data);
    }

    /// <summary>
    /// Determines whether the specified <see cref="ColorTimelineData"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="ColorTimelineData"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public bool Equals(ColorTimelineData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ColorData.SequenceEqual(other.ColorData);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>true if the specified object is equal to this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as ColorTimelineData);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var data in ColorData)
        {
            hash.Add(data);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => $"ColorTimelineData({ColorData.Count} points)";
}
