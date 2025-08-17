using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Interface for icon resources that handle UI icons and visual elements.
/// Supports various icon formats and provides access to icon metadata and pixel data.
/// </summary>
public interface IIconResource : IResource, IApiVersion, IAsyncDisposable
{
    /// <summary>
    /// Gets the icon width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the icon height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the icon format (DDS, PNG, TGA, etc.).
    /// </summary>
    IconFormat Format { get; }

    /// <summary>
    /// Gets the icon category for UI organization.
    /// </summary>
    IconCategory Category { get; }

    /// <summary>
    /// Gets additional icon metadata and usage hints.
    /// </summary>
    IconMetadata Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this icon is part of a sprite atlas.
    /// </summary>
    bool IsAtlasIcon { get; }

    /// <summary>
    /// Gets the atlas coordinates if this is part of a sprite atlas.
    /// </summary>
    AtlasCoordinates? AtlasCoordinates { get; }

    /// <summary>
    /// Asynchronously gets the raw pixel data for this icon.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The raw pixel data as a memory span</returns>
    Task<ReadOnlyMemory<byte>> GetPixelDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously loads icon data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing icon data</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the loading operation</returns>
    Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously saves icon data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write icon data to</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the saving operation</returns>
    Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a scaled version of this icon.
    /// </summary>
    /// <param name="newWidth">The target width</param>
    /// <param name="newHeight">The target height</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A new icon resource with the specified dimensions</returns>
    Task<IIconResource> CreateScaledAsync(int newWidth, int newHeight, CancellationToken cancellationToken = default);
}

/// <summary>
/// Supported icon formats for The Sims 4 UI system.
/// </summary>
public enum IconFormat
{
    /// <summary>Unknown or unsupported format</summary>
    Unknown = 0,

    /// <summary>DirectDraw Surface format (primary)</summary>
    DDS = 1,

    /// <summary>Portable Network Graphics format</summary>
    PNG = 2,

    /// <summary>Truevision TGA format</summary>
    TGA = 3,

    /// <summary>JPEG format (rarely used)</summary>
    JPEG = 4,

    /// <summary>Raw bitmap data</summary>
    Raw = 5
}

/// <summary>
/// Icon categories for UI organization and usage hints.
/// </summary>
public enum IconCategory
{
    /// <summary>General UI icon</summary>
    General = 0,

    /// <summary>Object catalog thumbnails</summary>
    CatalogThumbnail = 1,

    /// <summary>Sim portraits and avatars</summary>
    SimPortrait = 2,

    /// <summary>Interface buttons and controls</summary>
    InterfaceButton = 3,

    /// <summary>Status and notification icons</summary>
    StatusIcon = 4,

    /// <summary>Inventory and item icons</summary>
    InventoryIcon = 5,

    /// <summary>World map and navigation icons</summary>
    MapIcon = 6,

    /// <summary>Custom content and mod icons</summary>
    CustomContent = 7
}

/// <summary>
/// Metadata for icon resources providing usage hints and optimization information.
/// </summary>
public class IconMetadata : IEquatable<IconMetadata>
{
    /// <summary>
    /// Gets or sets the icon usage hint for the UI system.
    /// </summary>
    public string UsageHint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DPI scaling factor for high-resolution displays.
    /// </summary>
    public float DpiScaling { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether this icon should be cached in memory.
    /// </summary>
    public bool ShouldCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the icon priority for loading and caching.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets additional metadata tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = new();

    /// <inheritdoc />
    public bool Equals(IconMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return UsageHint == other.UsageHint &&
               DpiScaling.Equals(other.DpiScaling) &&
               ShouldCache == other.ShouldCache &&
               Priority == other.Priority &&
               Tags.SequenceEqual(other.Tags);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as IconMetadata);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(UsageHint, DpiScaling, ShouldCache, Priority, Tags.Count);
}

/// <summary>
/// Represents coordinates within a sprite atlas for icon extraction.
/// </summary>
public class AtlasCoordinates : IEquatable<AtlasCoordinates>
{
    /// <summary>
    /// Gets or sets the X coordinate within the atlas.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate within the atlas.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the icon within the atlas.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the icon within the atlas.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the atlas texture identifier.
    /// </summary>
    public uint AtlasId { get; set; }

    /// <inheritdoc />
    public bool Equals(AtlasCoordinates? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height && AtlasId == other.AtlasId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as AtlasCoordinates);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height, AtlasId);
}
