namespace TS4Tools.Resources.Common.CatalogTags;

/// <summary>
/// Represents a compound tag that can contain multiple child tags.
/// Modernized version with proper collection semantics and validation.
/// </summary>
public sealed class CompoundTag : IEquatable<CompoundTag>
{
    private readonly List<Tag> _tags = [];

    /// <summary>
    /// Gets the tags contained in this compound tag as a read-only collection.
    /// </summary>
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets or sets the name of this compound tag.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Adds a tag to this compound tag.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    public void AddTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        _tags.Add(tag);
    }

    /// <summary>
    /// Removes a tag from this compound tag.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>True if the tag was removed; otherwise, false.</returns>
    public bool RemoveTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return _tags.Remove(tag);
    }

    /// <summary>
    /// Removes a tag by index.
    /// </summary>
    /// <param name="index">The index of the tag to remove.</param>
    /// <returns>True if a tag with the specified index was removed; otherwise, false.</returns>
    public bool RemoveTagByIndex(uint index)
    {
        for (int i = _tags.Count - 1; i >= 0; i--)
        {
            if (_tags[i].Index == index)
            {
                _tags.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether this compound tag contains a tag with the specified index.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>True if a tag with the specified index exists; otherwise, false.</returns>
    public bool ContainsTag(uint index)
    {
        return _tags.Any(t => t.Index == index);
    }

    /// <summary>
    /// Gets a tag by its index.
    /// </summary>
    /// <param name="index">The index of the tag to retrieve.</param>
    /// <returns>The tag with the specified index, or null if not found.</returns>
    public Tag? GetTag(uint index)
    {
        return _tags.FirstOrDefault(t => t.Index == index);
    }

    /// <summary>
    /// Clears all tags from this compound tag.
    /// </summary>
    public void Clear()
    {
        _tags.Clear();
    }

    /// <summary>
    /// Gets the number of tags in this compound tag.
    /// </summary>
    public int Count => _tags.Count;

    /// <inheritdoc />
    public bool Equals(CompoundTag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Name == other.Name && 
               _tags.Count == other._tags.Count && 
               _tags.SequenceEqual(other._tags);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as CompoundTag);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        foreach (var tag in _tags)
        {
            hash.Add(tag);
        }
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name} ({Count} tags)";
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(CompoundTag? left, CompoundTag? right)
    {
        return EqualityComparer<CompoundTag>.Default.Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(CompoundTag? left, CompoundTag? right)
    {
        return !(left == right);
    }
}
