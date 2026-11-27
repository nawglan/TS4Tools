// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/CatalogTags/CatalogTagRegistry.cs lines 29-164
// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/CatalogTags/Tag.cs lines 33-110 (Tag)
// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/CatalogTags/CompoundTag.cs lines 33-100 (CompoundTag)
// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/CatalogTags/TagListing.cs and TagDocument.cs (XML shape of the data file)
// Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/Extensions/TagEnumerableExtensions.cs lines 22-28 (Order)

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A catalog tag or category: a numeric id with its human readable name.
/// </summary>
/// <remarks>
/// Source: Tag.cs lines 33-110. Legacy name: <c>Tag</c> (renamed to avoid clashing with other tag types). Equality is by
/// <see cref="Index"/> only, as in legacy. The WinForms TypeConverter/UITypeEditor parts of the legacy class are UI concerns and are not ported.
/// </remarks>
public sealed class CatalogTag : IEquatable<CatalogTag>
{
    /// <summary>Creates an unknown tag (value "unknown", index 0).</summary>
    public CatalogTag()
    {
    }

    /// <summary>Creates a tag.</summary>
    /// <param name="index">The tag id.</param>
    /// <param name="value">The tag name.</param>
    public CatalogTag(uint index, string value)
    {
        Index = index;
        Value = value;
    }

    /// <summary>The id of this tag (the XML <c>ev</c> attribute).</summary>
    public uint Index { get; set; }

    /// <summary>The human readable name of this tag.</summary>
    public string Value { get; set; } = "unknown";

    /// <summary>The id as a uint.</summary>
    public uint ToUInt32() => Index;

    /// <summary>The id truncated to a ushort (pre-version-11 catalog tags are 16-bit).</summary>
    public ushort ToUInt16() => (ushort)Index;

    /// <inheritdoc/>
    public override string ToString() => $"0x{Index:X4} {Value}";

    /// <inheritdoc/>
    public bool Equals(CatalogTag? other) => other is not null && Index == other.Index;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as CatalogTag);

    /// <inheritdoc/>
    public override int GetHashCode() => Index.GetHashCode();

    /// <summary>Equality by <see cref="Index"/>.</summary>
    public static bool operator ==(CatalogTag? left, CatalogTag? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Inequality by <see cref="Index"/>.</summary>
    public static bool operator !=(CatalogTag? left, CatalogTag? right) => !(left == right);
}

/// <summary>
/// A compound tag consisting of a category tag and a value tag.
/// </summary>
/// <remarks>Source: CompoundTag.cs lines 33-100. Legacy name: <c>CompoundTag</c>.</remarks>
public sealed class CatalogCompoundTag : IEquatable<CatalogCompoundTag>
{
    private CatalogTag? _category;
    private CatalogTag? _value;

    /// <summary>The category this compound tag belongs to (an unknown tag when unset).</summary>
    public CatalogTag Category
    {
        get => _category ?? new CatalogTag();
        set => _category = value;
    }

    /// <summary>The value of this compound tag (an unknown tag when unset).</summary>
    public CatalogTag Value
    {
        get => _value ?? new CatalogTag();
        set => _value = value;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Category} - {Value}";

    /// <inheritdoc/>
    public bool Equals(CatalogCompoundTag? other) => other is not null && Category == other.Category && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as CatalogCompoundTag);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Category, Value);

    /// <summary>Equality by category and value.</summary>
    public static bool operator ==(CatalogCompoundTag? left, CatalogCompoundTag? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Inequality by category and value.</summary>
    public static bool operator !=(CatalogCompoundTag? left, CatalogCompoundTag? right) => !(left == right);
}

/// <summary>
/// A named list of tags (an element of the legacy tuning file: <c>TagCategory</c> or <c>Tag</c>).
/// </summary>
/// <remarks>Source: TagListing.cs. The legacy XmlSerializer plumbing (TagDocument) is replaced by embedded tables.</remarks>
/// <param name="Name">The listing name: "TagCategory" or "Tag".</param>
/// <param name="Elements">The tags in file order.</param>
public sealed record CatalogTagListing(string Name, IReadOnlyList<CatalogTag> Elements);

/// <summary>
/// Registry of the known catalog categories and tags: maps tag ids to names.
/// </summary>
/// <remarks>
/// Source: CatalogTagRegistry.cs. Legacy loads <c>S4_03B33DDF_00000000_D89CB9186B79ACB7.xml</c> from beside the executable
/// at run time; here its content is embedded as compile-time tables (<c>CatalogTagData</c>) so lookups work on every platform without
/// extra files. Unknown ids yield a tag with the given index and the default value "unknown" (legacy FetchTag behaviour).
/// </remarks>
public static class CatalogTagRegistry
{
    private static readonly Lazy<Registry> Data = new(() => new Registry());

    /// <summary>The two listings of the tuning file ("TagCategory" and "Tag").</summary>
    public static IReadOnlyList<CatalogTagListing> Listings => Data.Value.Listings;

    /// <summary>
    /// Fetches the tag for <paramref name="index"/>.
    /// </summary>
    /// <returns>The known tag, or a new tag with that index and value "unknown".</returns>
    /// <remarks>Source: CatalogTagRegistry.cs lines 72-80 (FetchTag).</remarks>
    public static CatalogTag FetchTag(uint index) => GetOrDefault(Data.Value.Tags, index);

    /// <summary>
    /// Fetches the category for <paramref name="index"/>.
    /// </summary>
    /// <returns>The known category, or a new tag with that index and value "unknown".</returns>
    /// <remarks>Source: CatalogTagRegistry.cs lines 82-90 (FetchCategory).</remarks>
    public static CatalogTag FetchCategory(uint index) => GetOrDefault(Data.Value.Categories, index);

    /// <summary>Tries to get a tag name by id.</summary>
    /// <returns>True when the id is a known tag.</returns>
    public static bool TryGetTagName(uint index, [NotNullWhen(true)] out string? name)
    {
        if (Data.Value.Tags.TryGetValue(index, out var tag))
        {
            name = tag.Value;
            return true;
        }

        name = null;
        return false;
    }

    /// <summary>Gets a tag name by id, or null when unknown.</summary>
    public static string? GetTagName(uint index) => TryGetTagName(index, out var name) ? name : null;

    /// <summary>Gets a category name by id, or null when unknown.</summary>
    public static string? GetCategoryName(uint index) =>
        Data.Value.Categories.TryGetValue(index, out var tag) ? tag.Value : null;

    /// <summary>All known tags, ordered by index then name.</summary>
    /// <remarks>Source: CatalogTagRegistry.cs lines 92-98 (AllTags), ordered with TagEnumerableExtensions.Order.</remarks>
    public static IEnumerable<CatalogTag> AllTags() => Data.Value.Tags.Values.Order();

    /// <summary>All known category tags, ordered by index then name.</summary>
    /// <remarks>Source: CatalogTagRegistry.cs lines 100-105 (AllCategories).</remarks>
    public static IEnumerable<CatalogTag> AllCategories() => Data.Value.Categories.Values.Order();

    /// <summary>
    /// All known categories plus dummy categories (index uint.MaxValue) for tag-name prefixes that have no matching category.
    /// </summary>
    /// <remarks>Source: CatalogTagRegistry.cs lines 107-130 (AllCategoriesWithDummiesForUnpairedTags).</remarks>
    public static IEnumerable<CatalogTag> AllCategoriesWithDummiesForUnpairedTags()
    {
        var known = AllCategories().ToList();

        foreach (var tag in AllTags())
        {
            int index = tag.Value.IndexOf('_', StringComparison.Ordinal);
            if (index == -1)
                continue;

            string prefix = tag.Value[..index];
            if (!known.Any(c => c.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase)))
                known.Add(new CatalogTag(uint.MaxValue, prefix));
        }

        return known;
    }

    /// <summary>
    /// Gets the category for a tag from its name prefix (text before the first underscore). Returns a dummy when none matches.
    /// </summary>
    /// <remarks>Source: CatalogTagRegistry.cs lines 132-149 (GetCategoryFor).</remarks>
    public static CatalogTag GetCategoryFor(CatalogTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        int index = tag.Value.IndexOf('_', StringComparison.Ordinal);
        string prefix = index != -1 ? tag.Value[..index] : string.Empty;

        var category = Data.Value.Categories.Values.FirstOrDefault(t => t.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase));
        return category ?? new CatalogTag(uint.MaxValue, prefix);
    }

    /// <summary>
    /// All tags whose name starts with <c>category_</c> (case-insensitive).
    /// </summary>
    /// <remarks>Source: CatalogTagRegistry.cs lines 151-163 (FetchTagsForCategory).</remarks>
    public static IEnumerable<CatalogTag> FetchTagsForCategory(string category)
    {
        ArgumentNullException.ThrowIfNull(category);

        string prefix = category + "_";
        return Data.Value.Tags.Values.Where(t => t.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Orders by index then value (TagEnumerableExtensions.Order).</summary>
    private static IOrderedEnumerable<CatalogTag> Order(this IEnumerable<CatalogTag> tags) =>
        tags.OrderBy(t => t.Index).ThenBy(t => t.Value, StringComparer.Ordinal);

    private static CatalogTag GetOrDefault(Dictionary<uint, CatalogTag> dictionary, uint index) =>
        dictionary.TryGetValue(index, out var tag) ? tag : new CatalogTag { Index = index };

    private sealed class Registry
    {
        public Registry()
        {
            Categories = CatalogTagData.Categories.ToDictionary(t => t.Index, t => new CatalogTag(t.Index, t.Value));
            Tags = CatalogTagData.Tags.ToDictionary(t => t.Index, t => new CatalogTag(t.Index, t.Value));
            Listings =
            [
                new CatalogTagListing("TagCategory", [.. Categories.Values]),
                new CatalogTagListing("Tag", [.. Tags.Values]),
            ];
        }

        public Dictionary<uint, CatalogTag> Categories { get; }

        public Dictionary<uint, CatalogTag> Tags { get; }

        public IReadOnlyList<CatalogTagListing> Listings { get; }
    }
}
