using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.CatalogTags;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Interface for catalog tag resources that represent tagging and categorization data for catalog items.
/// Provides hierarchical tagging support for organizing catalog content in Buy/Build mode.
/// </summary>
public interface ICatalogTagResource : ICatalogResource
{
    /// <summary>
    /// Gets or sets the unique tag identifier.
    /// </summary>
    uint TagId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the tag.
    /// </summary>
    string TagName { get; set; }

    /// <summary>
    /// Gets or sets the description of the tag for tooltips and documentation.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets the parent tag ID for hierarchical organization.
    /// Zero indicates this is a root-level tag.
    /// </summary>
    uint ParentTagId { get; set; }

    /// <summary>
    /// Gets or sets the sort order for this tag within its parent.
    /// Lower values appear first in UI lists.
    /// </summary>
    int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the tag category type (e.g., Function, Style, Room, etc.).
    /// </summary>
    CatalogTagCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the display icon reference for this tag in the UI.
    /// </summary>
    TgiReference? IconReference { get; set; }

    /// <summary>
    /// Gets or sets additional flags that control tag behavior and visibility.
    /// </summary>
    CatalogTagFlags Flags { get; set; }

    /// <summary>
    /// Gets the collection of child tag IDs that belong to this tag.
    /// </summary>
    IList<uint> ChildTagIds { get; }

    /// <summary>
    /// Gets the collection of filter criteria associated with this tag.
    /// Used for advanced filtering and search operations.
    /// </summary>
    IList<TagFilterCriterion> FilterCriteria { get; }

    /// <summary>
    /// Adds a child tag ID to this tag's children collection.
    /// </summary>
    /// <param name="childTagId">The child tag ID to add.</param>
    void AddChildTag(uint childTagId);

    /// <summary>
    /// Removes a child tag ID from this tag's children collection.
    /// </summary>
    /// <param name="childTagId">The child tag ID to remove.</param>
    /// <returns>True if the child was removed; otherwise, false.</returns>
    bool RemoveChildTag(uint childTagId);

    /// <summary>
    /// Clears all child tag IDs from this tag.
    /// </summary>
    void ClearChildTags();

    /// <summary>
    /// Adds a filter criterion to this tag.
    /// </summary>
    /// <param name="criterion">The filter criterion to add.</param>
    void AddFilterCriterion(TagFilterCriterion criterion);

    /// <summary>
    /// Removes a filter criterion from this tag.
    /// </summary>
    /// <param name="criterion">The filter criterion to remove.</param>
    /// <returns>True if the criterion was removed; otherwise, false.</returns>
    bool RemoveFilterCriterion(TagFilterCriterion criterion);

    /// <summary>
    /// Clears all filter criteria from this tag.
    /// </summary>
    void ClearFilterCriteria();

    /// <summary>
    /// Validates the tag hierarchy to ensure consistency.
    /// </summary>
    /// <returns>True if the tag hierarchy is valid; otherwise, false.</returns>
    bool ValidateHierarchy();

    /// <summary>
    /// Determines if this tag is an ancestor of the specified tag ID.
    /// </summary>
    /// <param name="tagId">The tag ID to check.</param>
    /// <returns>True if this tag is an ancestor; otherwise, false.</returns>
    bool IsAncestorOf(uint tagId);

    /// <summary>
    /// Determines if this tag is a descendant of the specified tag ID.
    /// </summary>
    /// <param name="tagId">The tag ID to check.</param>
    /// <returns>True if this tag is a descendant; otherwise, false.</returns>
    bool IsDescendantOf(uint tagId);

    /// <summary>
    /// Gets all ancestor tag IDs in order from immediate parent to root.
    /// </summary>
    /// <returns>Collection of ancestor tag IDs.</returns>
    IEnumerable<uint> GetAncestorIds();

    /// <summary>
    /// Gets all descendant tag IDs recursively.
    /// </summary>
    /// <returns>Collection of descendant tag IDs.</returns>
    IEnumerable<uint> GetDescendantIds();
}

/// <summary>
/// Enumeration of catalog tag categories.
/// </summary>
public enum CatalogTagCategory
{
    /// <summary>Function-based tags (seating, lighting, etc.)</summary>
    Function = 0,

    /// <summary>Style-based tags (modern, traditional, etc.)</summary>
    Style = 1,

    /// <summary>Room-based tags (bedroom, kitchen, etc.)</summary>
    Room = 2,

    /// <summary>Color-based tags (red, blue, etc.)</summary>
    Color = 3,

    /// <summary>Material-based tags (wood, metal, etc.)</summary>
    Material = 4,

    /// <summary>Price range tags (budget, luxury, etc.)</summary>
    Price = 5,

    /// <summary>Brand-based tags (specific manufacturer)</summary>
    Brand = 6,

    /// <summary>Custom user-defined tags</summary>
    Custom = 999
}

/// <summary>
/// Flags that control catalog tag behavior.
/// </summary>
[Flags]
public enum CatalogTagFlags
{
    /// <summary>No special flags</summary>
    None = 0,

    /// <summary>Tag is visible in Buy/Build mode UI</summary>
    Visible = 1 << 0,

    /// <summary>Tag can be selected by users</summary>
    Selectable = 1 << 1,

    /// <summary>Tag can be used for filtering</summary>
    Filterable = 1 << 2,

    /// <summary>Tag is searchable by name</summary>
    Searchable = 1 << 3,

    /// <summary>Tag is automatically assigned based on object properties</summary>
    AutoAssigned = 1 << 4,

    /// <summary>Tag is read-only and cannot be modified</summary>
    ReadOnly = 1 << 4,

    /// <summary>Tag is deprecated and should not be used for new objects</summary>
    Deprecated = 1 << 5,

    /// <summary>Tag requires expansion pack content</summary>
    RequiresExpansion = 1 << 6,

    /// <summary>Default flags for most tags</summary>
    Default = Visible | Filterable | Searchable
}

/// <summary>
/// Represents a filter criterion associated with a catalog tag.
/// </summary>
public sealed class TagFilterCriterion
{
    /// <summary>
    /// Gets or sets the property name to filter on.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison operator.
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets whether this criterion is required (AND) or optional (OR).
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Filter comparison operators.
/// </summary>
public enum FilterOperator
{
    /// <summary>Equal to</summary>
    Equal = 0,

    /// <summary>Not equal to</summary>
    NotEqual = 1,

    /// <summary>Less than</summary>
    LessThan = 2,

    /// <summary>Less than or equal to</summary>
    LessThanOrEqual = 3,

    /// <summary>Greater than</summary>
    GreaterThan = 4,

    /// <summary>Greater than or equal to</summary>
    GreaterThanOrEqual = 5,

    /// <summary>Contains (for string/collection properties)</summary>
    Contains = 6,

    /// <summary>Starts with (for string properties)</summary>
    StartsWith = 7,

    /// <summary>Ends with (for string properties)</summary>
    EndsWith = 8,

    /// <summary>Regular expression match</summary>
    Regex = 9
}
