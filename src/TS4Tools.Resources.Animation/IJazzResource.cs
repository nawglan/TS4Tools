using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Interface for JAZZ animation state machine resources.
/// JAZZ resources contain XML data that defines animation state machines and transitions.
/// </summary>
public interface IJazzResource : IResource
{
    /// <summary>
    /// Gets or sets the XML content of the JAZZ resource.
    /// </summary>
    string XmlContent { get; set; }

    /// <summary>
    /// Gets or sets the name of the animation state machine.
    /// </summary>
    string StateMachineName { get; set; }

    /// <summary>
    /// Gets or sets the version of the JAZZ format.
    /// </summary>
    int FormatVersion { get; set; }

    /// <summary>
    /// Gets a value indicating whether the XML content is valid.
    /// </summary>
    bool IsValidXml { get; }

    /// <summary>
    /// Validates the XML content and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation errors, or empty if valid.</returns>
    Task<IReadOnlyList<string>> ValidateXmlAsync();

    /// <summary>
    /// Loads the JAZZ resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the JAZZ resource to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
