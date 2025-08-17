using System.Diagnostics.CodeAnalysis;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Common;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Factory for creating LRLE (Lossless Run-Length Encoded) resource instances.
/// Handles the creation and initialization of LRLE image resources with proper validation.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Factory methods need to handle various resource creation scenarios")]
public sealed class LRLEResourceFactory : ResourceFactoryBase<ILRLEResource>
{
    /// <summary>
    /// The resource type identifier for LRLE resources.
    /// </summary>
    public const uint ResourceType = 0x0166038C; // LRLE resource type from The Sims 4

    /// <summary>
    /// Initializes a new instance of the <see cref="LRLEResourceFactory"/> class.
    /// </summary>
    public LRLEResourceFactory() : base(new[] { "0x0166038C", "LRLE" }, priority: 100)
    {
    }

    /// <summary>
    /// Creates a new LRLE resource instance asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource.</param>
    /// <param name="stream">Optional stream containing LRLE data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the created resource instance.</returns>
    /// <exception cref="ArgumentException">Thrown when API version is invalid.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream data cannot be parsed as LRLE.</exception>
    public override async Task<ILRLEResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateApiVersion(apiVersion);

        var resource = new LRLEResource();

        if (stream != null)
        {
            try
            {
                using var memoryStream = await CreateMemoryStreamAsync(stream, cancellationToken);
                if (memoryStream != null)
                {
                    await resource.SetDataAsync(memoryStream, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not ArgumentNullException and not OperationCanceledException)
            {
                throw new InvalidDataException($"Failed to create LRLE resource from stream: {ex.Message}", ex);
            }
        }

        return resource;
    }

    /// <summary>
    /// Validates the API version for LRLE resources.
    /// </summary>
    /// <param name="apiVersion">API version to validate</param>
    /// <exception cref="ArgumentException">Thrown when API version is not supported</exception>
    protected override void ValidateApiVersion(int apiVersion)
    {
        if (apiVersion < 1 || apiVersion > 10) // LRLE supports versions 1-10
        {
            throw new ArgumentException($"Unsupported API version {apiVersion}. LRLE resources support API versions 1-10.", nameof(apiVersion));
        }
    }

    /// <summary>
    /// Validates whether the provided data appears to be valid LRLE format.
    /// </summary>
    /// <param name="data">The data to validate.</param>
    /// <returns>True if the data appears to be valid LRLE format; otherwise, false.</returns>
    public bool ValidateData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16) // Minimum header size
        {
            return false;
        }

        try
        {
            // Check LRLE magic number
            var magic = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]);
            if (magic != 0x454C524C) // 'LRLE'
            {
                return false;
            }

            // Check version
            var version = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
            if (version != 0x32303056 && version != 0x0) // Valid version numbers
            {
                return false;
            }

            // Check dimensions
            var width = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]);
            var height = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data[10..12]);

            if (width == 0 || height == 0 || width > 16384 || height > 16384) // Reasonable limits
            {
                return false;
            }

            // Check mip map count
            var mipMapCount = data[12];
            if (mipMapCount > 9) // Reasonable limit for mip maps
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets format information for LRLE resources.
    /// </summary>
    public LRLEFormatInfo FormatInfo { get; } = new()
    {
        Name = "LRLE (Lossless Run-Length Encoded Image)",
        Description = "Lossless Run-Length Encoded image format used in The Sims 4 for compressed textures with color palettes",
        MaxWidth = 16384,
        MaxHeight = 16384,
        MaxMipMapLevels = 9,
        SupportedVersions = [LRLEVersion.Version1, LRLEVersion.Version2],
        HasColorPalette = true,
        IsLossless = true,
        SupportsTransparency = true
    };
}

/// <summary>
/// Contains format information for LRLE resources.
/// </summary>
public sealed class LRLEFormatInfo
{
    /// <summary>
    /// Gets or sets the format name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the format description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the maximum supported width.
    /// </summary>
    public int MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum supported height.
    /// </summary>
    public int MaxHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of mip map levels.
    /// </summary>
    public int MaxMipMapLevels { get; set; }

    /// <summary>
    /// Gets or sets the supported LRLE versions.
    /// </summary>
    public LRLEVersion[]? SupportedVersions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the format has a color palette.
    /// </summary>
    public bool HasColorPalette { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the format is lossless.
    /// </summary>
    public bool IsLossless { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the format supports transparency.
    /// </summary>
    public bool SupportsTransparency { get; set; }
}
