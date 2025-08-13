using System.Diagnostics.CodeAnalysis;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for RLE (Run-Length Encoded) compressed image resources.
/// Supports RLE2, RLES, and other RLE format variations used in The Sims 4.
/// </summary>
[SuppressMessage("Naming", "S101:Rename interface 'IRLEResource' to match pascal case naming rules", Justification = "RLE is a well-known acronym in the domain")]
public interface IRLEResource : IResource
{
    /// <summary>
    /// Gets the width of the RLE image in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the RLE image in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the number of mipmap levels in the RLE image.
    /// </summary>
    uint MipCount { get; }

    /// <summary>
    /// Gets the RLE format version (RLE2, RLES, etc.).
    /// </summary>
    RLEVersion Version { get; }

    /// <summary>
    /// Gets the pixel format identifier.
    /// </summary>
    uint PixelFormat { get; }

    /// <summary>
    /// Converts the RLE resource to DDS format.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A stream containing the DDS data.</returns>
    Task<Stream> ToDDSAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts the RLE resource to DDS format synchronously.
    /// </summary>
    /// <returns>A stream containing the DDS data.</returns>
    Stream ToDDS();

    /// <summary>
    /// Gets the raw RLE data.
    /// </summary>
    ReadOnlySpan<byte> GetRawData();

    /// <summary>
    /// Sets the RLE data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing RLE data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetDataAsync(Stream stream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enumeration of RLE format versions.
/// </summary>
public enum RLEVersion
{
    /// <summary>
    /// Unknown or invalid version.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Standard RLE2 format.
    /// </summary>
    RLE2 = 0x32454C52,

    /// <summary>
    /// RLE with specular channel (RLES).
    /// </summary>
    RLES = 0x53454C52
}

/// <summary>
/// Enumeration of pixel format identifiers used in RLE resources.
/// </summary>
public enum RLEPixelFormat
{
    /// <summary>
    /// None/Unknown format.
    /// </summary>
    None = 0,

    /// <summary>
    /// DXT1 compression format.
    /// </summary>
    DXT1 = 0x31545844,

    /// <summary>
    /// DXT5 compression format.
    /// </summary>
    DXT5 = 0x35545844,

    /// <summary>
    /// 8-bit luminance format.
    /// </summary>
    L8 = 0x384C,

    /// <summary>
    /// ATI2 format (3Dc).
    /// </summary>
    ATI2 = 0x32495441
}
