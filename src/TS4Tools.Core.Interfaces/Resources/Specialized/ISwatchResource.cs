using System.Drawing;

namespace TS4Tools.Core.Interfaces.Resources.Specialized;

/// <summary>
/// Interface for color swatch resources used in character customization.
/// Manages collections of color swatches for recoloring CAS parts and materials.
/// </summary>
public interface ISwatchResource : IResource
{
    /// <summary>
    /// Gets or sets the display name of this swatch collection.
    /// </summary>
    string SwatchName { get; set; }

    /// <summary>
    /// Gets the category this swatch collection belongs to.
    /// </summary>
    SwatchCategory Category { get; }

    /// <summary>
    /// Gets the list of color swatches in this collection.
    /// </summary>
    IList<ColorSwatch> Swatches { get; }

    /// <summary>
    /// Adds a color swatch to the collection asynchronously.
    /// </summary>
    /// <param name="swatch">The color swatch to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task AddSwatchAsync(ColorSwatch swatch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a color swatch from the collection by index asynchronously.
    /// </summary>
    /// <param name="index">The index of the swatch to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task RemoveSwatchAsync(int index, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary color of a swatch at the specified index.
    /// </summary>
    /// <param name="swatchIndex">The index of the swatch</param>
    /// <returns>The primary color</returns>
    Color GetPrimaryColor(int swatchIndex);

    /// <summary>
    /// Gets the secondary color of a swatch at the specified index.
    /// </summary>
    /// <param name="swatchIndex">The index of the swatch</param>
    /// <returns>The secondary color, or null if not available</returns>
    Color? GetSecondaryColor(int swatchIndex);

    /// <summary>
    /// Sets the colors for a swatch at the specified index asynchronously.
    /// </summary>
    /// <param name="swatchIndex">The index of the swatch</param>
    /// <param name="primary">The primary color</param>
    /// <param name="secondary">The optional secondary color</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task SetColorsAsync(int swatchIndex, Color primary, Color? secondary = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets swatches filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>An enumerable of matching swatches</returns>
    IEnumerable<ColorSwatch> GetSwatchesByCategory(SwatchCategory category);

    /// <summary>
    /// Finds the swatch that most closely matches the target color asynchronously.
    /// </summary>
    /// <param name="targetColor">The color to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the closest matching swatch, or null if none found</returns>
    Task<ColorSwatch?> FindClosestMatchAsync(Color targetColor, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a category for organizing color swatches.
/// </summary>
public enum SwatchCategory
{
    /// <summary>Unknown or general category</summary>
    General = 0,
    /// <summary>Hair colors</summary>
    Hair = 1,
    /// <summary>Skin tones</summary>
    Skin = 2,
    /// <summary>Eye colors</summary>
    Eyes = 3,
    /// <summary>Clothing colors</summary>
    Clothing = 4,
    /// <summary>Makeup colors</summary>
    Makeup = 5,
    /// <summary>Accessory colors</summary>
    Accessories = 6,
    /// <summary>Object and furniture colors</summary>
    Objects = 7,
    /// <summary>Material swatches</summary>
    Materials = 8,
    /// <summary>Custom user-defined swatches</summary>
    Custom = 9
}

/// <summary>
/// Represents a single color swatch with primary and optional secondary colors.
/// </summary>
/// <param name="PrimaryColor">The primary color</param>
/// <param name="SecondaryColor">Optional secondary color for gradients or patterns</param>
/// <param name="Name">Display name for this swatch</param>
/// <param name="Category">Category this swatch belongs to</param>
public readonly record struct ColorSwatch(
    Color PrimaryColor,
    Color? SecondaryColor = null,
    string Name = "",
    SwatchCategory Category = SwatchCategory.General)
{
    /// <summary>
    /// Calculates the Euclidean distance between this swatch's primary color and a target color.
    /// </summary>
    /// <param name="targetColor">The color to compare against</param>
    /// <returns>The color distance value (lower values indicate closer matches)</returns>
    public double GetColorDistance(Color targetColor)
    {
        var deltaR = PrimaryColor.R - targetColor.R;
        var deltaG = PrimaryColor.G - targetColor.G;
        var deltaB = PrimaryColor.B - targetColor.B;
        return Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
    }
}
