using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Modern interface for catalog resources with proper async contract and disposal patterns.
/// Provides the foundation for all Buy/Build catalog functionality in The Sims 4.
/// </summary>
public interface ICatalogResource : IResource, IApiVersion, IContentFields, IAsyncDisposable
{
    /// <summary>
    /// Gets the catalog common block containing shared catalog metadata.
    /// This block contains critical information for catalog organization and compatibility.
    /// </summary>
    CatalogCommonBlock? CommonBlock { get; }

    /// <summary>
    /// Gets the catalog version identifier.
    /// Different versions support different features and data structures.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the collection of content fields containing catalog-specific data.
    /// This includes object properties, pricing, categories, and placement rules.
    /// </summary>
    new IReadOnlyList<string> ContentFields { get; }

    /// <summary>
    /// Gets the catalog type identifier indicating the specific catalog format.
    /// Different catalog types handle different object categories (furniture, decor, etc.).
    /// </summary>
    CatalogType CatalogType { get; }

    /// <summary>
    /// Gets a value indicating whether this catalog resource has been modified.
    /// Used for tracking changes and determining when saving is required.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    /// Asynchronously loads catalog data from the specified stream with proper cancellation support.
    /// </summary>
    /// <param name="stream">The stream containing catalog resource data</param>
    /// <param name="cancellationToken">Token to cancel the loading operation</param>
    /// <returns>A task representing the asynchronous loading operation</returns>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid catalog data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled</exception>
    Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously saves catalog data to the specified stream with proper cancellation support.
    /// </summary>
    /// <param name="stream">The stream to write catalog resource data to</param>
    /// <param name="cancellationToken">Token to cancel the saving operation</param>
    /// <returns>A task representing the asynchronous saving operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when the catalog resource is in an invalid state for saving</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled</exception>
    Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the catalog resource data for consistency and completeness.
    /// </summary>
    /// <returns>A collection of validation errors, empty if valid</returns>
    IEnumerable<string> Validate();

    /// <summary>
    /// Creates a deep copy of this catalog resource.
    /// Useful for creating modified versions without affecting the original.
    /// </summary>
    /// <returns>A deep copy of the catalog resource</returns>
    Task<ICatalogResource> CloneAsync();
}

/// <summary>
/// Represents the catalog type for different object categories.
/// Different types have different data structures and validation requirements.
/// </summary>
public enum CatalogType
{
    /// <summary>Unknown or unsupported catalog type</summary>
    Unknown = 0,

    /// <summary>Standard furniture and object catalog (0x049CA4CD)</summary>
    Standard = 1,

    /// <summary>Object catalog with placement rules (0x319E4F1D)</summary>
    Object = 2,

    /// <summary>Lot catalog for lot templates (0x9D1FFBCD)</summary>
    Lot = 3,

    /// <summary>Room catalog for room templates (0x1CC03E4C)</summary>
    Room = 4,

    /// <summary>Alternative catalog format (0xA8F7B517)</summary>
    Alternative = 5,

    /// <summary>Custom catalog type (0x48C28979)</summary>
    Custom = 6
}

/// <summary>
/// Represents the common catalog block containing shared metadata.
/// This block is present in all catalog resource types and contains critical information.
/// </summary>
public class CatalogCommonBlock : IEquatable<CatalogCommonBlock>
{
    /// <summary>
    /// Gets or sets the catalog format version.
    /// </summary>
    public uint FormatVersion { get; set; }

    /// <summary>
    /// Gets or sets the compatibility flags for this catalog entry.
    /// </summary>
    public CatalogCompatibilityFlags CompatibilityFlags { get; set; }

    /// <summary>
    /// Gets or sets the catalog category identifier.
    /// </summary>
    public uint CategoryId { get; set; }

    /// <summary>
    /// Gets or sets additional catalog metadata flags.
    /// </summary>
    public uint MetadataFlags { get; set; }

    /// <inheritdoc />
    public bool Equals(CatalogCommonBlock? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return FormatVersion == other.FormatVersion &&
               CompatibilityFlags == other.CompatibilityFlags &&
               CategoryId == other.CategoryId &&
               MetadataFlags == other.MetadataFlags;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CatalogCommonBlock);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(FormatVersion, CompatibilityFlags, CategoryId, MetadataFlags);
}

/// <summary>
/// Flags indicating catalog compatibility and feature support.
/// </summary>
[Flags]
public enum CatalogCompatibilityFlags : int
{
    /// <summary>No special compatibility requirements</summary>
    None = 0x00000000,

    /// <summary>Requires base game compatibility</summary>
    BaseGame = 0x00000001,

    /// <summary>Requires expansion pack features</summary>
    ExpansionPack = 0x00000002,

    /// <summary>Requires stuff pack features</summary>
    StuffPack = 0x00000004,

    /// <summary>Requires game pack features</summary>
    GamePack = 0x00000008,

    /// <summary>Custom content compatibility</summary>
    CustomContent = 0x00000010,

    /// <summary>Development/debug content</summary>
    Development = 0x00000020
}
