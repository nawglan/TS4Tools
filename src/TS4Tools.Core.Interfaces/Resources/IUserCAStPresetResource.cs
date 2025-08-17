using System.Diagnostics.CodeAnalysis;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for User CAS (Create-A-Sim) Preset resources.
/// Manages user-created character customization presets.
/// </summary>
public interface IUserCAStPresetResource : IResource
{
    /// <summary>
    /// Gets the version of the UserCAStPreset format.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the collection of presets stored in this resource.
    /// </summary>
    IReadOnlyList<ICASPreset> Presets { get; }

    /// <summary>
    /// Gets additional metadata for the preset collection.
    /// </summary>
    uint Unknown1 { get; set; }

    /// <summary>
    /// Gets additional metadata for the preset collection.
    /// </summary>
    uint Unknown2 { get; set; }

    /// <summary>
    /// Gets additional metadata for the preset collection.
    /// </summary>
    uint Unknown3 { get; set; }

    /// <summary>
    /// Adds a new preset to the collection.
    /// </summary>
    /// <param name="preset">The preset to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddPresetAsync(ICASPreset preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a preset from the collection.
    /// </summary>
    /// <param name="index">The index of the preset to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the preset was removed; otherwise, false.</returns>
    Task<bool> RemovePresetAsync(int index, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all presets from the collection.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ClearPresetsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for individual CAS presets.
/// </summary>
[SuppressMessage("Naming", "S101:Types should be named in PascalCase", Justification = "CAS is a well-known acronym in the domain")]
public interface ICASPreset
{
    /// <summary>
    /// Gets the XML data for the preset.
    /// </summary>
    string Xml { get; set; }

    /// <summary>
    /// Gets unknown data field 1.
    /// </summary>
    byte Unknown1 { get; set; }

    /// <summary>
    /// Gets unknown data field 2.
    /// </summary>
    byte Unknown2 { get; set; }

    /// <summary>
    /// Gets unknown data field 3.
    /// </summary>
    uint Unknown3 { get; set; }

    /// <summary>
    /// Gets unknown data field 4.
    /// </summary>
    byte Unknown4 { get; set; }

    /// <summary>
    /// Gets unknown data field 5.
    /// </summary>
    byte Unknown5 { get; set; }

    /// <summary>
    /// Gets unknown data field 6.
    /// </summary>
    byte Unknown6 { get; set; }

    /// <summary>
    /// Validates the preset data.
    /// </summary>
    /// <returns>True if the preset is valid; otherwise, false.</returns>
    bool IsValid();
}
