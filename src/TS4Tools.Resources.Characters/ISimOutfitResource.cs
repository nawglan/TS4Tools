using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Interface for Sim outfit resources (SIMO).
/// Sim outfit resources define complete outfits for characters including clothing, accessories, and body modifications.
/// </summary>
public interface ISimOutfitResource : IResource
{
    /// <summary>
    /// Gets or sets the version of the SIMO format.
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Gets or sets the age flags for which this outfit is applicable.
    /// </summary>
    AgeGenderFlags Age { get; set; }

    /// <summary>
    /// Gets or sets the gender flags for which this outfit is applicable.
    /// </summary>
    AgeGenderFlags Gender { get; set; }

    /// <summary>
    /// Gets or sets the skin tone reference ID.
    /// </summary>
    ulong SkinToneReference { get; set; }

    /// <summary>
    /// Gets or sets the CAS part reference ID.
    /// </summary>
    ulong CasPartReference { get; set; }

    /// <summary>
    /// Gets the list of resource references used by this outfit.
    /// </summary>
    IReadOnlyList<ulong> DataReferences { get; }

    /// <summary>
    /// Gets the list of slider modifications for body shape.
    /// </summary>
    IReadOnlyList<SliderReference> BodySliders { get; }

    /// <summary>
    /// Gets the list of slider modifications for facial features.
    /// </summary>
    IReadOnlyList<SliderReference> FaceSliders { get; }

    /// <summary>
    /// Gets whether this outfit is valid and complete.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Adds a data reference to the outfit.
    /// </summary>
    /// <param name="reference">The resource reference to add.</param>
    void AddDataReference(ulong reference);

    /// <summary>
    /// Removes a data reference from the outfit.
    /// </summary>
    /// <param name="reference">The resource reference to remove.</param>
    /// <returns>True if the reference was removed.</returns>
    bool RemoveDataReference(ulong reference);

    /// <summary>
    /// Adds a slider reference to the outfit.
    /// </summary>
    /// <param name="slider">The slider reference to add.</param>
    /// <param name="isBodySlider">True if this is a body slider, false for face slider.</param>
    void AddSliderReference(SliderReference slider, bool isBodySlider = true);

    /// <summary>
    /// Loads the outfit resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the outfit resource to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
