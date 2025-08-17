namespace TS4Tools.Core.Interfaces.Resources.Specialized;

/// <summary>
/// Interface for a generic preset resource that can store various types of preset data.
/// Supports inheritance, templating, versioning, and validation.
/// </summary>
public interface IPresetResource : IResource
{
    /// <summary>
    /// Gets the type identifier for this preset.
    /// </summary>
    string PresetType { get; }

    /// <summary>
    /// Gets or sets the display name of this preset.
    /// </summary>
    string PresetName { get; set; }

    /// <summary>
    /// Gets the version of this preset format.
    /// </summary>
    Version PresetVersion { get; }

    /// <summary>
    /// Gets the dictionary containing all preset data.
    /// </summary>
    IDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets a typed value from the preset data.
    /// </summary>
    /// <typeparam name="TValue">The type of value to retrieve</typeparam>
    /// <param name="key">The key to look up</param>
    /// <returns>The value if found, or default if not found</returns>
    TValue? GetValue<TValue>(string key);

    /// <summary>
    /// Sets a typed value in the preset data asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The type of value to store</typeparam>
    /// <param name="key">The key to store the value under</param>
    /// <param name="value">The value to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task SetValueAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the parent preset for inheritance scenarios.
    /// </summary>
    IPresetResource? ParentPreset { get; set; }

    /// <summary>
    /// Creates a child preset that inherits from this preset asynchronously.
    /// </summary>
    /// <param name="name">The name for the child preset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created child preset</returns>
    Task<IPresetResource> CreateChildPresetAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of this preset asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing true if valid, false otherwise</returns>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates this preset to a target version asynchronously.
    /// </summary>
    /// <param name="targetVersion">The version to migrate to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the migrated preset</returns>
    Task<IPresetResource> MigrateToVersionAsync(Version targetVersion, CancellationToken cancellationToken = default);
}
