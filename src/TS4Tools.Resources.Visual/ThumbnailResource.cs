using System.Runtime.InteropServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Represents a thumbnail resource for preview and thumbnail generation in The Sims 4 package files.
/// Thumbnails are used for item previews, catalog displays, and quick visual identification.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class ThumbnailResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private byte[] _imageData;
    private byte[] _metadataData;
    private bool _isDirty;
    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this thumbnail resource.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the thumbnail image data as raw bytes.
    /// </summary>
    public ReadOnlySpan<byte> ImageData => _imageData.AsSpan();

    /// <summary>
    /// Gets the thumbnail metadata as raw bytes.
    /// </summary>
    public ReadOnlySpan<byte> MetadataData => _metadataData.AsSpan();

    /// <summary>
    /// Gets the thumbnail width in pixels.
    /// </summary>
    public uint Width { get; private set; }

    /// <summary>
    /// Gets the thumbnail height in pixels.
    /// </summary>
    public uint Height { get; private set; }

    /// <summary>
    /// Gets or sets the thumbnail format.
    /// </summary>
    public ThumbnailFormat Format { get; private set; }

    /// <summary>
    /// Gets or sets the compression quality (0-100, where 100 is highest quality).
    /// </summary>
    public byte Quality { get; private set; }

    /// <summary>
    /// Gets or sets whether the thumbnail has an alpha channel.
    /// </summary>
    public bool HasAlpha { get; private set; }

    /// <summary>
    /// Gets the thumbnail's aspect ratio (width / height).
    /// </summary>
    public float AspectRatio => Width > 0 && Height > 0 ? (float)Width / Height : 1.0f;

    /// <summary>
    /// Gets the thumbnail's total pixel count.
    /// </summary>
    public uint PixelCount => Width * Height;

    /// <summary>
    /// Indicates whether the resource has been modified and needs to be saved.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="imageData">The thumbnail image data.</param>
    /// <param name="width">The thumbnail width in pixels.</param>
    /// <param name="height">The thumbnail height in pixels.</param>
    /// <param name="format">The thumbnail format.</param>
    /// <param name="quality">The compression quality (0-100).</param>
    /// <param name="hasAlpha">Whether the thumbnail has an alpha channel.</param>
    /// <param name="metadataData">Optional metadata associated with the thumbnail.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public ThumbnailResource(
        ResourceKey key,
        ReadOnlySpan<byte> imageData,
        uint width,
        uint height,
        ThumbnailFormat format = ThumbnailFormat.JPEG,
        byte quality = 85,
        bool hasAlpha = false,
        ReadOnlySpan<byte> metadataData = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (width == 0 || height == 0)
            throw new ArgumentException("Thumbnail dimensions must be greater than zero", nameof(width));

        if (quality > 100)
            throw new ArgumentException("Quality must be between 0 and 100", nameof(quality));

        _key = key;
        _imageData = imageData.ToArray();
        _metadataData = metadataData.ToArray();
        Width = width;
        Height = height;
        Format = format;
        Quality = quality;
        HasAlpha = hasAlpha;
        _isDirty = false;
    }

    /// <summary>
    /// Updates the thumbnail image data.
    /// </summary>
    /// <param name="newImageData">The new image data.</param>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    /// <param name="format">The new format (optional).</param>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public void UpdateImage(ReadOnlySpan<byte> newImageData, uint width, uint height, ThumbnailFormat? format = null)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException("Thumbnail dimensions must be greater than zero");

        if (newImageData.IsEmpty)
            throw new ArgumentException("Image data cannot be empty", nameof(newImageData));

        _imageData = newImageData.ToArray();
        Width = width;
        Height = height;

        if (format.HasValue)
            Format = format.Value;

        _isDirty = true;
    }

    /// <summary>
    /// Updates the thumbnail metadata.
    /// </summary>
    /// <param name="newMetadata">The new metadata data.</param>
    public void UpdateMetadata(ReadOnlySpan<byte> newMetadata)
    {
        _metadataData = newMetadata.ToArray();
        _isDirty = true;
    }

    /// <summary>
    /// Resizes the thumbnail to new dimensions (conceptually - actual resizing would need image processing).
    /// This method updates the dimensions but does not modify the actual image data.
    /// </summary>
    /// <param name="newWidth">The new width in pixels.</param>
    /// <param name="newHeight">The new height in pixels.</param>
    /// <param name="maintainAspectRatio">Whether to maintain the original aspect ratio.</param>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public void Resize(uint newWidth, uint newHeight, bool maintainAspectRatio = true)
    {
        if (newWidth == 0 || newHeight == 0)
            throw new ArgumentException("New dimensions must be greater than zero");

        if (maintainAspectRatio)
        {
            var currentRatio = AspectRatio;
            var newRatio = (float)newWidth / newHeight;

            // Adjust dimensions to maintain aspect ratio
            if (newRatio > currentRatio)
            {
                newWidth = (uint)(newHeight * currentRatio);
            }
            else if (newRatio < currentRatio)
            {
                newHeight = (uint)(newWidth / currentRatio);
            }
        }

        Width = newWidth;
        Height = newHeight;
        _isDirty = true;
    }

    /// <summary>
    /// Gets the expected file size for the thumbnail based on its format and dimensions.
    /// </summary>
    /// <returns>The estimated file size in bytes.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate",
        Justification = "Method performs calculation and estimation, not simple property access")]
    public long GetEstimatedFileSize()
    {
        var pixelCount = PixelCount;

        return Format switch
        {
            ThumbnailFormat.PNG => HasAlpha ? pixelCount * 4 : pixelCount * 3, // Rough estimate
            ThumbnailFormat.JPEG => (long)(pixelCount * 3 * (Quality / 100.0)), // Compressed estimate
            ThumbnailFormat.DDS => HasAlpha ? pixelCount * 4 : pixelCount * 3, // Uncompressed DDS
            ThumbnailFormat.BMP => HasAlpha ? pixelCount * 4 : pixelCount * 3, // Uncompressed BMP
            _ => pixelCount * 3 // Default to 24-bit estimate
        };
    }

    /// <summary>
    /// Determines if the thumbnail is considered high resolution.
    /// </summary>
    /// <param name="threshold">The pixel count threshold (default: 256x256 = 65536).</param>
    /// <returns>True if the thumbnail exceeds the resolution threshold.</returns>
    public bool IsHighResolution(uint threshold = 65536)
    {
        return PixelCount > threshold;
    }

    /// <summary>
    /// Creates a copy of this thumbnail resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A copy of this thumbnail resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when newKey is null.</exception>
    public ThumbnailResource Clone(ResourceKey newKey)
    {
        ArgumentNullException.ThrowIfNull(newKey);

        return new ThumbnailResource(
            newKey,
            _imageData.AsSpan(),
            Width,
            Height,
            Format,
            Quality,
            HasAlpha,
            _metadataData.AsSpan());
    }

    #region IResource Implementation

    /// <summary>
    /// Gets the resource data as a stream.
    /// </summary>
    public Stream Stream => _stream ??= new MemoryStream(_imageData, writable: false);

    /// <summary>
    /// Gets the resource data as a byte array.
    /// </summary>
    public byte[] AsBytes => _imageData.ToArray();

    /// <summary>
    /// Occurs when the resource has been changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version for this resource.
    /// </summary>
    public int RequestedApiVersion { get; init; } = 1;

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        "Width", "Height", "Format", "Quality", "HasAlpha", "ImageData", "MetadataData"
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field value by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => TypedValue.Create(Width),
            1 => TypedValue.Create(Height),
            2 => TypedValue.Create(Format.ToString()),
            3 => TypedValue.Create(Quality),
            4 => TypedValue.Create(HasAlpha),
            5 => TypedValue.Create(_imageData),
            6 => TypedValue.Create(_metadataData),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 0:
                    uint? widthValue = value.GetValue<uint>();
                    if (widthValue.HasValue)
                    {
                        Width = widthValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 1:
                    uint? heightValue = value.GetValue<uint>();
                    if (heightValue.HasValue)
                    {
                        Height = heightValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 2:
                    var formatString = value.GetValue<string>();
                    if (formatString != null && Enum.TryParse<ThumbnailFormat>(formatString, out var parsedFormat))
                    {
                        Format = parsedFormat;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 3:
                    byte? qualityValue = value.GetValue<byte>();
                    if (qualityValue.HasValue)
                    {
                        Quality = qualityValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 4:
                    bool? hasAlphaValue = value.GetValue<bool>();
                    if (hasAlphaValue.HasValue)
                    {
                        HasAlpha = hasAlphaValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 5:
                    var newImageData = value.GetValue<byte[]>();
                    if (newImageData != null)
                    {
                        _imageData = newImageData;
                        _stream?.Dispose();
                        _stream = null;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 6:
                    var newMetadataData = value.GetValue<byte[]>();
                    if (newMetadataData != null)
                    {
                        _metadataData = newMetadataData;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// Gets or sets a content field value by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[string name]
    {
        get
        {
            var index = ContentFields.ToList().IndexOf(name);
            if (index == -1)
                throw new ArgumentException($"Field '{name}' not found");
            return this[index];
        }
        set
        {
            var index = ContentFields.ToList().IndexOf(name);
            if (index == -1)
                throw new ArgumentException($"Field '{name}' not found");
            this[index] = value;
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases all resources used by the ThumbnailResource.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Marks the resource as clean (not modified).
    /// </summary>
    public void MarkClean()
    {
        _isDirty = false;
    }

    /// <summary>
    /// Returns a string representation of this thumbnail resource.
    /// </summary>
    /// <returns>A string containing the thumbnail's key and properties.</returns>
    public override string ToString()
    {
        return $"ThumbnailResource [Key={_key}, Size={Width}x{Height}, Format={Format}, Quality={Quality}%, Alpha={HasAlpha}]";
    }
}

/// <summary>
/// Defines the supported thumbnail formats.
/// </summary>
public enum ThumbnailFormat
{
    /// <summary>
    /// JPEG format (good compression, no transparency).
    /// </summary>
    JPEG = 0,

    /// <summary>
    /// PNG format (lossless, supports transparency).
    /// </summary>
    PNG = 1,

    /// <summary>
    /// DirectDraw Surface format.
    /// </summary>
    DDS = 2,

    /// <summary>
    /// Bitmap format (uncompressed).
    /// </summary>
    BMP = 3,

    /// <summary>
    /// Custom or unknown format.
    /// </summary>
    Custom = 255
}
