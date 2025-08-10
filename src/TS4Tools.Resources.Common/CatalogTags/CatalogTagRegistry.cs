using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Resources.Common.CatalogTags;

/// <summary>
/// Registry class that manages known catalog categories and tags.
/// Modernized version with dependency injection support, async operations, and better error handling.
/// </summary>
public sealed partial class CatalogTagRegistry
{
    private const string CatalogTuningFileName = "S4_03B33DDF_00000000_D89CB9186B79ACB7.xml";

    private readonly ILogger<CatalogTagRegistry> _logger;
    private readonly Lazy<FrozenDictionary<uint, Tag>> _tags;
    private readonly Lazy<FrozenDictionary<uint, Tag>> _categories;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTagRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public CatalogTagRegistry(ILogger<CatalogTagRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tags = new Lazy<FrozenDictionary<uint, Tag>>(() => LoadTags("Tag"));
        _categories = new Lazy<FrozenDictionary<uint, Tag>>(() => LoadTags("TagCategory"));
    }

    /// <summary>
    /// Gets all known tags as a read-only collection.
    /// </summary>
    public ReadOnlyDictionary<uint, Tag> Tags => new(_tags.Value);

    /// <summary>
    /// Gets all known categories as a read-only collection.
    /// </summary>
    public ReadOnlyDictionary<uint, Tag> Categories => new(_categories.Value);

    /// <summary>
    /// Fetches the matching tag for the specified index.
    /// </summary>
    /// <param name="index">The tag index to look up.</param>
    /// <returns>A <see cref="Tag"/> instance containing the matching value, or a default if no match was found.</returns>
    public Tag FetchTag(uint index)
    {
        return _tags.Value.GetValueOrDefault(index, new Tag(index, "unknown"));
    }

    /// <summary>
    /// Fetches the matching category for the specified index.
    /// </summary>
    /// <param name="index">The category index to look up.</param>
    /// <returns>A <see cref="Tag"/> instance containing the matching value, or a default if no match was found.</returns>
    public Tag FetchCategory(uint index)
    {
        return _categories.Value.GetValueOrDefault(index, new Tag(index, "unknown"));
    }

    /// <summary>
    /// Tries to fetch a tag by index.
    /// </summary>
    /// <param name="index">The tag index.</param>
    /// <param name="tag">The found tag, or null if not found.</param>
    /// <returns>True if the tag was found; otherwise, false.</returns>
    public bool TryFetchTag(uint index, out Tag? tag)
    {
        return _tags.Value.TryGetValue(index, out tag);
    }

    /// <summary>
    /// Tries to fetch a category by index.
    /// </summary>
    /// <param name="index">The category index.</param>
    /// <param name="category">The found category, or null if not found.</param>
    /// <returns>True if the category was found; otherwise, false.</returns>
    public bool TryFetchCategory(uint index, out Tag? category)
    {
        return _categories.Value.TryGetValue(index, out category);
    }

    /// <summary>
    /// Returns all known tags.
    /// </summary>
    /// <returns>An enumerable of all tags.</returns>
    public IEnumerable<Tag> AllTags()
    {
        return _tags.Value.Values;
    }

    /// <summary>
    /// Returns all known categories.
    /// </summary>
    /// <returns>An enumerable of all categories.</returns>
    public IEnumerable<Tag> AllCategories()
    {
        return _categories.Value.Values;
    }

    [LoggerMessage(LogLevel.Warning, "Embedded catalog tuning resource '{ResourceName}' not found")]
    private partial void LogResourceNotFound(string resourceName);

    [LoggerMessage(LogLevel.Debug, "Loading catalog tags from embedded resource '{ResourceName}', listing: '{ListingName}'")]
    private partial void LogLoadingCatalogTags(string resourceName, string listingName);

    [LoggerMessage(LogLevel.Error, "Failed to deserialize tag document from embedded resource '{ResourceName}'")]
    private partial void LogDeserializationFailed(string resourceName);

    [LoggerMessage(LogLevel.Warning, "Listing '{ListingName}' not found in tag document")]
    private partial void LogListingNotFound(string listingName);

    [LoggerMessage(LogLevel.Information, "Loaded {TagCount} {ListingName} entries from catalog tuning")]
    private partial void LogTagsLoaded(int tagCount, string listingName);

    [LoggerMessage(LogLevel.Error, "Failed to load catalog tags for listing '{ListingName}'")]
    private partial void LogLoadFailed(Exception ex, string listingName);

    private FrozenDictionary<uint, Tag> LoadTags(string listingName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"TS4Tools.Resources.Common.CatalogTags.{CatalogTuningFileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogResourceNotFound(resourceName);
                return FrozenDictionary<uint, Tag>.Empty;
            }

            LogLoadingCatalogTags(resourceName, listingName);

            // Create secure XML reader settings
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var xmlReader = XmlReader.Create(stream, settings);
            var serializer = new XmlSerializer(typeof(TagDocument));

            if (serializer.Deserialize(xmlReader) is not TagDocument document)
            {
                LogDeserializationFailed(resourceName);
                return FrozenDictionary<uint, Tag>.Empty;
            }

            var listing = document.ListingsReadOnly.FirstOrDefault(t => t.Name == listingName);
            if (listing == null)
            {
                LogListingNotFound(listingName);
                return FrozenDictionary<uint, Tag>.Empty;
            }

            var tags = listing.ElementsReadOnly.ToFrozenDictionary(t => t.Index, t => t);
            LogTagsLoaded(tags.Count, listingName);

            return tags;
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex, listingName);
            return FrozenDictionary<uint, Tag>.Empty;
        }
    }
}
