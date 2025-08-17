using System.Diagnostics.CodeAnalysis;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for LRLE (Lossless Run-Length Encoded) compressed image resources.
/// Supports color palette management and high-quality texture compression used in The Sims 4.
/// </summary>
[SuppressMessage("Naming", "S101:Rename interface 'ILRLEResource' to match pascal case naming rules", Justification = "LRLE is a well-known acronym in the domain")]
public interface ILRLEResource : IResource
{
    /// <summary>
    /// Gets the width of the LRLE image in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the LRLE image in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the number of mipmap levels in the LRLE image.
    /// </summary>
    uint MipCount { get; }

    /// <summary>
    /// Gets the LRLE format version.
    /// </summary>
    LRLEVersion Version { get; }

    /// <summary>
    /// Gets the magic number identifier for this LRLE resource.
    /// </summary>
    uint Magic { get; }

    /// <summary>
    /// Gets the number of colors in the palette.
    /// </summary>
    uint ColorCount { get; }

    /// <summary>
    /// Converts the LRLE resource to a bitmap image.
    /// </summary>
    /// <param name="mipLevel">The mip level to convert (0 = full resolution).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A stream containing the bitmap data.</returns>
    Task<Stream> ToBitmapAsync(int mipLevel = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts the LRLE resource to a bitmap image synchronously.
    /// </summary>
    /// <param name="mipLevel">The mip level to convert (0 = full resolution).</param>
    /// <returns>A stream containing the bitmap data.</returns>
    Stream ToBitmap(int mipLevel = 0);

    /// <summary>
    /// Gets the raw LRLE data for a specific mip level.
    /// </summary>
    /// <param name="mipLevel">The mip level to get data for.</param>
    /// <returns>Raw LRLE data as a read-only span.</returns>
    ReadOnlySpan<byte> GetRawData(int mipLevel = 0);

    /// <summary>
    /// Sets the LRLE data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing LRLE data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetDataAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates LRLE resource from a bitmap image.
    /// </summary>
    /// <param name="imageStream">Stream containing bitmap image data.</param>
    /// <param name="generateMipmaps">Whether to generate mipmap levels.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task CreateFromImageAsync(Stream imageStream, bool generateMipmaps = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enumeration of LRLE format versions.
/// </summary>
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "LRLE version values are defined as specific uint constants in the format specification")]
public enum LRLEVersion : uint
{
    /// <summary>
    /// Unknown or invalid version.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// LRLE Version 1 format (legacy).
    /// </summary>
    Version1 = 0x0,

    /// <summary>
    /// LRLE Version 2 format with enhanced color palette.
    /// </summary>
    Version2 = 0x32303056 // "V 02"
}

/// <summary>
/// Enumeration of LRLE encoding states used in the compression algorithm.
/// </summary>
public enum LRLEEncodingState
{
    /// <summary>
    /// Starting a new encoding block.
    /// </summary>
    StartNew = 0,

    /// <summary>
    /// State is not yet determined.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// Encoding a run of different colors.
    /// </summary>
    ColorRun = 2,

    /// <summary>
    /// Encoding a run of repeated colors.
    /// </summary>
    RepeatRun = 3
}
