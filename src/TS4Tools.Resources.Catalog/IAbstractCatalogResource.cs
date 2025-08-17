using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Abstract interface defining the common contract for all catalog resource types.
/// Provides core properties and methods shared across different catalog implementations.
/// </summary>
public interface IAbstractCatalogResource : ICatalogResource
{
    /// <summary>
    /// Gets or sets the catalog version number.
    /// Different versions support different features and data layouts.
    /// </summary>
    new uint Version { get; set; }

    /// <summary>
    /// Gets or sets the catalog common block containing shared metadata.
    /// This block is present in all catalog resource types.
    /// </summary>
    new CatalogCommonBlock? CommonBlock { get; set; }

    /// <summary>
    /// Validates the catalog resource data for consistency and correctness.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the validation operation</param>
    /// <returns>A task containing the validation result with any errors or warnings</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the minimum supported version for this catalog resource type.
    /// </summary>
    uint MinimumSupportedVersion { get; }

    /// <summary>
    /// Gets the maximum supported version for this catalog resource type.
    /// </summary>
    uint MaximumSupportedVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the current version is supported.
    /// </summary>
    bool IsVersionSupported { get; }

    /// <summary>
    /// Performs version-specific migrations when loading older catalog formats.
    /// </summary>
    /// <param name="fromVersion">The version being migrated from</param>
    /// <param name="cancellationToken">Token to cancel the migration operation</param>
    /// <returns>A task representing the migration operation</returns>
    Task MigrateVersionAsync(uint fromVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets additional validation rules specific to this catalog resource type.
    /// </summary>
    IEnumerable<IValidationRule> GetValidationRules();

    /// <summary>
    /// Creates a deep copy of this catalog resource.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the cloning operation</param>
    /// <returns>A task containing the cloned catalog resource</returns>
    Task<IAbstractCatalogResource> CloneAsync(CancellationToken cancellationToken = default);
}
