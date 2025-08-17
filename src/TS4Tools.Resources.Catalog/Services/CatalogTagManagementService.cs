using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Common.CatalogTags;

namespace TS4Tools.Resources.Catalog.Services;

/// <summary>
/// Service for managing catalog tag operations including search, hierarchy navigation, and import/export.
/// Provides business logic for advanced tag-based catalog filtering and organization.
/// </summary>
public sealed class CatalogTagManagementService : ICatalogTagManagementService
{
    private readonly ILogger<CatalogTagManagementService> _logger;
    private readonly Dictionary<uint, ICatalogTagResource> _tagCache;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTagManagementService"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public CatalogTagManagementService(ILogger<CatalogTagManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tagCache = new Dictionary<uint, ICatalogTagResource>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ICatalogTagResource>> SearchTagsAsync(
        string searchTerm,
        CatalogTagCategory? category = null,
        CatalogTagFlags? requiredFlags = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Searching tags with term '{SearchTerm}', category: {Category}, flags: {Flags}",
            searchTerm, category, requiredFlags);

        var results = new List<ICatalogTagResource>();

        lock (_cacheLock)
        {
            foreach (var tag in _tagCache.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Text search
                var matchesText = string.IsNullOrWhiteSpace(searchTerm) ||
                                 tag.TagName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                 tag.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

                // Category filter
                var matchesCategory = category == null || tag.Category == category.Value;

                // Flags filter
                var matchesFlags = requiredFlags == null || tag.Flags.HasFlag(requiredFlags.Value);

                if (matchesText && matchesCategory && matchesFlags)
                {
                    results.Add(tag);
                }
            }
        }

        _logger.LogDebug("Found {Count} tags matching search criteria", results.Count);
        return await Task.FromResult(results.OrderBy(t => t.SortOrder).ThenBy(t => t.TagName)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ICatalogTagResource>> GetTagHierarchyAsync(
        uint rootTagId,
        bool includeRoot = true,
        int maxDepth = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Getting tag hierarchy for root {RootTagId}, includeRoot: {IncludeRoot}, maxDepth: {MaxDepth}",
            rootTagId, includeRoot, maxDepth);

        var results = new List<ICatalogTagResource>();

        ICatalogTagResource? rootTag = null;
        lock (_cacheLock)
        {
            _tagCache.TryGetValue(rootTagId, out rootTag);
        }

        if (rootTag != null)
        {
            if (includeRoot)
            {
                results.Add(rootTag);
            }

            await CollectChildTagsRecursively(rootTag, results, maxDepth, 0, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogDebug("Collected {Count} tags in hierarchy for root {RootTagId}", results.Count, rootTagId);
        return await Task.FromResult(results).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ICatalogTagResource>> GetTagAncestorsAsync(
        uint tagId,
        bool includeTag = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Getting ancestors for tag {TagId}, includeTag: {IncludeTag}", tagId, includeTag);

        var ancestors = new List<ICatalogTagResource>();

        lock (_cacheLock)
        {
            if (_tagCache.TryGetValue(tagId, out var currentTag))
            {
                if (includeTag)
                {
                    ancestors.Add(currentTag);
                }

                // Walk up the parent chain
                var visited = new HashSet<uint> { tagId }; // Prevent circular references
                while (currentTag.ParentTagId != 0 && !visited.Contains(currentTag.ParentTagId))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (_tagCache.TryGetValue(currentTag.ParentTagId, out var parentTag))
                    {
                        ancestors.Insert(0, parentTag); // Insert at beginning for root-to-leaf order
                        visited.Add(parentTag.TagId);
                        currentTag = parentTag;
                    }
                    else
                    {
                        _logger.LogWarning("Parent tag {ParentTagId} not found in cache for tag {TagId}",
                            currentTag.ParentTagId, currentTag.TagId);
                        break;
                    }
                }
            }
        }

        _logger.LogDebug("Found {Count} ancestors for tag {TagId}", ancestors.Count, tagId);
        return await Task.FromResult(ancestors).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ICatalogTagResource>> FilterByTagsAsync(
        IEnumerable<ICatalogTagResource> resources,
        IEnumerable<uint> requiredTagIds,
        IEnumerable<uint>? excludedTagIds = null,
        TagFilterMode mode = TagFilterMode.All,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requiredTagIdSet = requiredTagIds.ToHashSet();
        var excludedTagIdSet = excludedTagIds?.ToHashSet() ?? new HashSet<uint>();

        _logger.LogDebug("Filtering resources by tags: {RequiredCount} required, {ExcludedCount} excluded, mode: {Mode}",
            requiredTagIdSet.Count, excludedTagIdSet.Count, mode);

        var results = new List<ICatalogTagResource>();

        foreach (var resource in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check excluded tags first
            if (excludedTagIdSet.Count > 0)
            {
                var hasExcludedTag = await HasAnyTagAsync(resource, excludedTagIdSet, cancellationToken).ConfigureAwait(false);
                if (hasExcludedTag)
                {
                    continue; // Skip this resource
                }
            }

            // Check required tags
            if (requiredTagIdSet.Count > 0)
            {
                var matchesRequired = mode switch
                {
                    TagFilterMode.All => await HasAllTagsAsync(resource, requiredTagIdSet, cancellationToken).ConfigureAwait(false),
                    TagFilterMode.Any => await HasAnyTagAsync(resource, requiredTagIdSet, cancellationToken).ConfigureAwait(false),
                    _ => false
                };

                if (!matchesRequired)
                {
                    continue; // Skip this resource
                }
            }

            results.Add(resource);
        }

        _logger.LogDebug("Filtered to {Count} resources matching tag criteria", results.Count);
        return await Task.FromResult(results).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTagHierarchyAsync(
        ICatalogTagResource tag,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check for circular references
        var visited = new HashSet<uint> { tag.TagId };
        var currentParentId = tag.ParentTagId;

        while (currentParentId != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (visited.Contains(currentParentId))
            {
                _logger.LogWarning("Circular reference detected in tag hierarchy for tag {TagId}", tag.TagId);
                return false;
            }

            visited.Add(currentParentId);

            lock (_cacheLock)
            {
                if (_tagCache.TryGetValue(currentParentId, out var parentTag))
                {
                    currentParentId = parentTag.ParentTagId;
                }
                else
                {
                    _logger.LogWarning("Parent tag {ParentTagId} not found for tag {TagId}", currentParentId, tag.TagId);
                    return false;
                }
            }
        }

        return await Task.FromResult(true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TagExportData> ExportTagDefinitionsAsync(
        IEnumerable<uint>? tagIds = null,
        bool includeHierarchy = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exportData = new TagExportData
        {
            ExportDate = DateTime.UtcNow,
            IncludesHierarchy = includeHierarchy
        };

        var tagsToExport = tagIds?.ToHashSet();

        lock (_cacheLock)
        {
            var tags = tagsToExport == null
                ? _tagCache.Values
                : _tagCache.Values.Where(t => tagsToExport.Contains(t.TagId));

            foreach (var tag in tags.OrderBy(t => t.SortOrder).ThenBy(t => t.TagName))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tagDef = new TagDefinition
                {
                    TagId = tag.TagId,
                    TagName = tag.TagName,
                    Description = tag.Description,
                    ParentTagId = tag.ParentTagId,
                    SortOrder = tag.SortOrder,
                    Category = tag.Category,
                    Flags = tag.Flags
                };

                // Add child tag IDs
                foreach (var childId in tag.ChildTagIds)
                {
                    tagDef.ChildTagIds.Add(childId);
                }

                // Add filter criteria
                foreach (var fc in tag.FilterCriteria)
                {
                    tagDef.FilterCriteria.Add(new TagFilterDefinition
                    {
                        PropertyName = fc.PropertyName,
                        Operator = fc.Operator,
                        Value = fc.Value?.ToString(),
                        IsRequired = fc.IsRequired
                    });
                }

                exportData.Tags.Add(tagDef);
            }
        }

        _logger.LogInformation("Exported {Count} tag definitions", exportData.Tags.Count);
        return await Task.FromResult(exportData).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TagImportResult> ImportTagDefinitionsAsync(
        TagExportData importData,
        bool replaceExisting = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new TagImportResult
        {
            TotalProcessed = importData.Tags.Count,
            SuccessfulImports = 0,
            FailedImports = 0
        };

        lock (_cacheLock)
        {
            foreach (var tagDef in importData.Tags)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var existingTag = _tagCache.ContainsKey(tagDef.TagId);

                    if (existingTag && !replaceExisting)
                    {
                        result.Errors.Add($"Tag {tagDef.TagId} already exists and replaceExisting is false");
                        result.FailedImports++;
                        continue;
                    }

                    // Create or update tag
                    var tag = new CatalogTagResource(tagDef.TagId, tagDef.TagName, tagDef.Category)
                    {
                        Description = tagDef.Description,
                        ParentTagId = tagDef.ParentTagId,
                        SortOrder = tagDef.SortOrder,
                        Flags = tagDef.Flags
                    };

                    // Add child tags
                    foreach (var childId in tagDef.ChildTagIds)
                    {
                        tag.AddChildTag(childId);
                    }

                    // Add filter criteria
                    foreach (var fcDef in tagDef.FilterCriteria)
                    {
                        var filterCriterion = new TagFilterCriterion
                        {
                            PropertyName = fcDef.PropertyName,
                            Operator = fcDef.Operator,
                            Value = fcDef.Value,
                            IsRequired = fcDef.IsRequired
                        };
                        tag.AddFilterCriterion(filterCriterion);
                    }

                    _tagCache[tagDef.TagId] = tag;
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    var error = $"Failed to import tag {tagDef.TagId}: {ex.Message}";
                    result.Errors.Add(error);
                    result.FailedImports++;
                    _logger.LogError(ex, "Failed to import tag {TagId}", tagDef.TagId);
                }
            }
        }

        _logger.LogInformation("Import completed: {Successful} successful, {Failed} failed out of {Total} total",
            result.SuccessfulImports, result.FailedImports, result.TotalProcessed);

        return await Task.FromResult(result).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RegisterTagAsync(ICatalogTagResource tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cacheLock)
        {
            _tagCache[tag.TagId] = tag;
        }

        _logger.LogDebug("Registered tag {TagId} '{TagName}' in cache", tag.TagId, tag.TagName);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterTagAsync(uint tagId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cacheLock)
        {
            _tagCache.Remove(tagId);
        }

        _logger.LogDebug("Unregistered tag {TagId} from cache", tagId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ICatalogTagResource?> GetTagAsync(uint tagId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cacheLock)
        {
            _tagCache.TryGetValue(tagId, out var tag);
            return Task.FromResult(tag);
        }
    }

    /// <inheritdoc />
    public Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cacheLock)
        {
            var count = _tagCache.Count;
            _tagCache.Clear();
            _logger.LogInformation("Cleared {Count} tags from cache", count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Recursively collects child tags up to the specified depth.
    /// </summary>
    private async Task CollectChildTagsRecursively(
        ICatalogTagResource parentTag,
        List<ICatalogTagResource> results,
        int maxDepth,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if (currentDepth >= maxDepth)
        {
            return;
        }

        foreach (var childId in parentTag.ChildTagIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_tagCache.TryGetValue(childId, out var childTag))
            {
                results.Add(childTag);
                await CollectChildTagsRecursively(childTag, results, maxDepth, currentDepth + 1, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Child tag {ChildTagId} not found in cache for parent {ParentTagId}",
                    childId, parentTag.TagId);
            }
        }
    }

    /// <summary>
    /// Checks if a resource has all the specified tags.
    /// </summary>
    private async Task<bool> HasAllTagsAsync(
        ICatalogTagResource resource,
        HashSet<uint> tagIds,
        CancellationToken cancellationToken)
    {
        // For this implementation, we'll check if the resource's tag ID or any of its ancestors match
        var resourceTags = await GetResourceTagsAsync(resource, cancellationToken).ConfigureAwait(false);
        return tagIds.All(tagId => resourceTags.Contains(tagId));
    }

    /// <summary>
    /// Checks if a resource has any of the specified tags.
    /// </summary>
    private async Task<bool> HasAnyTagAsync(
        ICatalogTagResource resource,
        HashSet<uint> tagIds,
        CancellationToken cancellationToken)
    {
        var resourceTags = await GetResourceTagsAsync(resource, cancellationToken).ConfigureAwait(false);
        return tagIds.Any(tagId => resourceTags.Contains(tagId));
    }

    /// <summary>
    /// Gets all tags associated with a resource (including hierarchy).
    /// </summary>
    private async Task<HashSet<uint>> GetResourceTagsAsync(
        ICatalogTagResource resource,
        CancellationToken cancellationToken)
    {
        var tags = new HashSet<uint> { resource.TagId };

        // Add child tags
        foreach (var childId in resource.ChildTagIds)
        {
            tags.Add(childId);
        }

        // Add ancestor tags
        var ancestors = await GetTagAncestorsAsync(resource.TagId, false, cancellationToken).ConfigureAwait(false);
        foreach (var ancestor in ancestors)
        {
            tags.Add(ancestor.TagId);
        }

        return tags;
    }
}
