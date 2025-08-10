using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TS4Tools.Resources.Common.CatalogTags;

/// <summary>
/// Represents the XML document structure for catalog tag definitions.
/// </summary>
[XmlRoot("Document")]
public sealed class TagDocument
{
    private TagListing[]? _listings;

    /// <summary>
    /// Gets or sets the list of tag listings in the document.
    /// </summary>
    [XmlElement("Listing")]
#pragma warning disable CA1819 // Properties should not return arrays - Required for XML serialization
    public TagListing[]? Listings
    {
        get => _listings;
        set => _listings = value;
    }
#pragma warning restore CA1819

    /// <summary>
    /// Gets the listings as a read-only collection.
    /// </summary>
    [XmlIgnore]
    public ReadOnlyCollection<TagListing> ListingsReadOnly =>
        _listings != null ? Array.AsReadOnly(_listings) : Array.AsReadOnly(Array.Empty<TagListing>());
}

/// <summary>
/// Represents a listing of tags within the document.
/// </summary>
public sealed class TagListing
{
    private Tag[]? _elements;

    /// <summary>
    /// Gets or sets the name of the listing (e.g., "Tag", "TagCategory").
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the elements within this listing.
    /// </summary>
    [XmlElement("Element")]
#pragma warning disable CA1819 // Properties should not return arrays - Required for XML serialization
    public Tag[]? Elements
    {
        get => _elements;
        set => _elements = value;
    }
#pragma warning restore CA1819

    /// <summary>
    /// Gets the elements as a read-only collection.
    /// </summary>
    [XmlIgnore]
    public ReadOnlyCollection<Tag> ElementsReadOnly =>
        _elements != null ? Array.AsReadOnly(_elements) : Array.AsReadOnly(Array.Empty<Tag>());
}
