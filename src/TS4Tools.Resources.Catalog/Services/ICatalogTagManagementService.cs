using TS4Tools.Resources.Common.CatalogTags;

namespace TS4Tools.Resources.Catalog.Services;

/// <summary>
/// Interface for catalog tag management services providing business logic for tag operations.
/// </summary>
public interface ICatalogTagManagementService
{
    /// <summary>
    /// Searches for catalog tags based on the specified criteria.
    /// </summary>
    /// <param name="searchTerm">The search term to match against tag names and descriptions.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="requiredFlags">Optional flags that must be present.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of matching catalog tags.</returns>
    Task<IEnumerable<ICatalogTagResource>> SearchTagsAsync(
        string searchTerm,
        CatalogTagCategory? category = null,
        CatalogTagFlags? requiredFlags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete hierarchy of tags starting from the specified root tag.
    /// </summary>
    /// <param name="rootTagId">The root tag ID to start from.</param>
    /// <param name="includeRoot">Whether to include the root tag in the results.</param>
    /// <param name="maxDepth">Maximum depth to traverse (default is unlimited).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of tags in the hierarchy.</returns>
    Task<IEnumerable<ICatalogTagResource>> GetTagHierarchyAsync(
        uint rootTagId,
        bool includeRoot = true,
        int maxDepth = int.MaxValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ancestor tags for the specified tag.
    /// </summary>
    /// <param name="tagId">The tag ID to find ancestors for.</param>
    /// <param name="includeTag">Whether to include the tag itself in the results.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of ancestor tags ordered from root to immediate parent.</returns>
    Task<IEnumerable<ICatalogTagResource>> GetTagAncestorsAsync(
        uint tagId,
        bool includeTag = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters resources based on tag criteria.
    /// </summary>
    /// <param name="resources">The resources to filter.</param>
    /// <param name="requiredTagIds">Tags that must be present.</param>
    /// <param name="excludedTagIds">Tags that must not be present.</param>
    /// <param name="mode">Whether to require all tags or any of the tags.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of resources matching the tag criteria.</returns>
    Task<IEnumerable<ICatalogTagResource>> FilterByTagsAsync(
        IEnumerable<ICatalogTagResource> resources,
        IEnumerable<uint> requiredTagIds,
        IEnumerable<uint>? excludedTagIds = null,
        TagFilterMode mode = TagFilterMode.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a tag's hierarchy is consistent and has no circular references.
    /// </summary>
    /// <param name="tag">The tag to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the hierarchy is valid, false otherwise.</returns>
    Task<bool> ValidateTagHierarchyAsync(
        ICatalogTagResource tag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports tag definitions for backup or sharing.
    /// </summary>
    /// <param name="tagIds">Specific tags to export, or null for all tags.</param>
    /// <param name="includeHierarchy">Whether to include hierarchy information.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Export data containing tag definitions.</returns>
    Task<TagExportData> ExportTagDefinitionsAsync(
        IEnumerable<uint>? tagIds = null,
        bool includeHierarchy = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports tag definitions from export data.
    /// </summary>
    /// <param name="importData">The export data to import.</param>
    /// <param name="replaceExisting">Whether to replace existing tags with the same ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result information about the import operation.</returns>
    Task<TagImportResult> ImportTagDefinitionsAsync(
        TagExportData importData,
        bool replaceExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a tag in the management cache for efficient access.
    /// </summary>
    /// <param name="tag">The tag to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RegisterTagAsync(ICatalogTagResource tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a tag from the management cache.
    /// </summary>
    /// <param name="tagId">The tag ID to unregister.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UnregisterTagAsync(uint tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tag by ID from the cache.
    /// </summary>
    /// <param name="tagId">The tag ID to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The tag if found, null otherwise.</returns>
    Task<ICatalogTagResource?> GetTagAsync(uint tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the internal tag cache.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Mode for tag filtering operations.
/// </summary>
public enum TagFilterMode
{
    /// <summary>All specified tags must be present.</summary>
    All,

    /// <summary>Any of the specified tags must be present.</summary>
    Any
}

/// <summary>
/// Data structure for exporting tag definitions.
/// </summary>
public class TagExportData
{
    /// <summary>Gets or sets the export date.</summary>
    public DateTime ExportDate { get; set; }

    /// <summary>Gets or sets whether hierarchy information is included.</summary>
    public bool IncludesHierarchy { get; set; }

    /// <summary>Gets the tag definitions.</summary>
    public ICollection<TagDefinition> Tags { get; } = new List<TagDefinition>();
}

/// <summary>
/// Definition of a tag for export/import operations.
/// </summary>
public class TagDefinition
{
    /// <summary>Gets or sets the tag ID.</summary>
    public uint TagId { get; set; }

    /// <summary>Gets or sets the tag name.</summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent tag ID.</summary>
    public uint ParentTagId { get; set; }

    /// <summary>Gets or sets the sort order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the tag category.</summary>
    public CatalogTagCategory Category { get; set; }

    /// <summary>Gets or sets the tag flags.</summary>
    public CatalogTagFlags Flags { get; set; }

    /// <summary>Gets the child tag IDs.</summary>
    public ICollection<uint> ChildTagIds { get; } = new List<uint>();

    /// <summary>Gets the filter criteria.</summary>
    public ICollection<TagFilterDefinition> FilterCriteria { get; } = new List<TagFilterDefinition>();
}

/// <summary>
/// Definition of a filter criterion for export/import operations.
/// </summary>
public class TagFilterDefinition
{
    /// <summary>Gets or sets the property name.</summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the filter operator.</summary>
    public FilterOperator Operator { get; set; }

    /// <summary>Gets or sets the filter value as a string.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets whether this criterion is required.</summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// Result of a tag import operation.
/// </summary>
public class TagImportResult
{
    /// <summary>Gets or sets the total number of tags processed.</summary>
    public int TotalProcessed { get; set; }

    /// <summary>Gets or sets the number of successful imports.</summary>
    public int SuccessfulImports { get; set; }

    /// <summary>Gets or sets the number of failed imports.</summary>
    public int FailedImports { get; set; }

    /// <summary>Gets any errors that occurred during import.</summary>
    public ICollection<string> Errors { get; } = new List<string>();
}
