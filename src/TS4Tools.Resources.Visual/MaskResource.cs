using System.Runtime.InteropServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Represents a mask resource for alpha masks and overlays in The Sims 4 package files.
/// Masks are used for transparency effects, selective rendering, and visual layering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public sealed class MaskResource : IResource, IDisposable
{
    private readonly ResourceKey _key;
    private byte[] _data;
    private bool _isDirty;
    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Gets the resource key that uniquely identifies this mask resource.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets or sets the mask data as raw bytes.
    /// </summary>
    public ReadOnlySpan<byte> Data => _data.AsSpan();

    /// <summary>
    /// Gets the mask width in pixels.
    /// </summary>
    public uint Width { get; private set; }

    /// <summary>
    /// Gets the mask height in pixels.
    /// </summary>
    public uint Height { get; private set; }

    /// <summary>
    /// Gets the number of channels in the mask (typically 1 for alpha, 4 for RGBA).
    /// </summary>
    public byte Channels { get; private set; }

    /// <summary>
    /// Gets the bits per channel (typically 8 for standard masks).
    /// </summary>
    public byte BitsPerChannel { get; private set; }

    /// <summary>
    /// Gets or sets the mask format type.
    /// </summary>
    public MaskFormat Format { get; private set; }

    /// <summary>
    /// Indicates whether the resource has been modified and needs to be saved.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The mask data.</param>
    /// <param name="width">The mask width in pixels.</param>
    /// <param name="height">The mask height in pixels.</param>
    /// <param name="channels">The number of channels.</param>
    /// <param name="bitsPerChannel">The bits per channel.</param>
    /// <param name="format">The mask format.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public MaskResource(
        ResourceKey key,
        ReadOnlySpan<byte> data,
        uint width,
        uint height,
        byte channels = 1,
        byte bitsPerChannel = 8,
        MaskFormat format = MaskFormat.Alpha)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (width == 0 || height == 0)
            throw new ArgumentException("Mask dimensions must be greater than zero", nameof(width));

        if (channels == 0)
            throw new ArgumentException("Number of channels must be greater than zero", nameof(channels));

        if (bitsPerChannel == 0)
            throw new ArgumentException("Bits per channel must be greater than zero", nameof(bitsPerChannel));

        _key = key;
        _data = data.ToArray();
        Width = width;
        Height = height;
        Channels = channels;
        BitsPerChannel = bitsPerChannel;
        Format = format;
        _isDirty = false;
    }

    /// <summary>
    /// Updates the mask data.
    /// </summary>
    /// <param name="newData">The new mask data.</param>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public void UpdateData(ReadOnlySpan<byte> newData, uint width, uint height)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException("Mask dimensions must be greater than zero");

        var expectedSize = width * height * Channels * (BitsPerChannel / 8);
        if (newData.Length != expectedSize)
            throw new ArgumentException($"Data size {newData.Length} does not match expected size {expectedSize}");

        _data = newData.ToArray();
        Width = width;
        Height = height;
        _isDirty = true;
    }

    /// <summary>
    /// Gets a pixel's alpha value at the specified coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The alpha value (0-255).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
    public byte GetPixelAlpha(uint x, uint y)
    {
        if (x >= Width || y >= Height)
            throw new ArgumentOutOfRangeException(x >= Width ? nameof(x) : nameof(y), "Coordinates are out of bounds");

        if (Channels == 0 || BitsPerChannel != 8)
            throw new InvalidOperationException("Cannot get pixel alpha for non-8-bit or zero-channel masks");

        var bytesPerPixel = Channels * (BitsPerChannel / 8);
        var index = (int)((y * Width + x) * bytesPerPixel);

        return Format switch
        {
            MaskFormat.Alpha => _data[index],
            MaskFormat.RGBA => _data[index + 3], // Alpha is the 4th channel
            _ => _data[index]
        };
    }

    /// <summary>
    /// Sets a pixel's alpha value at the specified coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="alpha">The alpha value (0-255).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
    public void SetPixelAlpha(uint x, uint y, byte alpha)
    {
        if (x >= Width || y >= Height)
            throw new ArgumentOutOfRangeException(x >= Width ? nameof(x) : nameof(y), "Coordinates are out of bounds");

        if (Channels == 0 || BitsPerChannel != 8)
            throw new InvalidOperationException("Cannot set pixel alpha for non-8-bit or zero-channel masks");

        var bytesPerPixel = Channels * (BitsPerChannel / 8);
        var index = (int)((y * Width + x) * bytesPerPixel);

        switch (Format)
        {
            case MaskFormat.Alpha:
                _data[index] = alpha;
                break;
            case MaskFormat.RGBA:
                _data[index + 3] = alpha; // Alpha is the 4th channel
                break;
            default:
                _data[index] = alpha;
                break;
        }

        _isDirty = true;
    }

    /// <summary>
    /// Creates a copy of this mask resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A copy of this mask resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when newKey is null.</exception>
    public MaskResource Clone(ResourceKey newKey)
    {
        ArgumentNullException.ThrowIfNull(newKey);

        return new MaskResource(
            newKey,
            _data.AsSpan(),
            Width,
            Height,
            Channels,
            BitsPerChannel,
            Format);
    }

    #region IResource Implementation

    /// <summary>
    /// Gets the resource data as a stream.
    /// </summary>
    public Stream Stream => _stream ??= new MemoryStream(_data, writable: false);

    /// <summary>
    /// Gets the resource data as a byte array.
    /// </summary>
    public byte[] AsBytes => _data.ToArray();

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
        "Width", "Height", "Channels", "BitsPerChannel", "Format", "Data"
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
            2 => TypedValue.Create(Channels),
            3 => TypedValue.Create(BitsPerChannel),
            4 => TypedValue.Create(Format.ToString()),
            5 => TypedValue.Create(_data),
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
                    byte? channelsValue = value.GetValue<byte>();
                    if (channelsValue.HasValue)
                    {
                        Channels = channelsValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 3:
                    byte? bitsPerChannelValue = value.GetValue<byte>();
                    if (bitsPerChannelValue.HasValue)
                    {
                        BitsPerChannel = bitsPerChannelValue.Value;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 4:
                    var formatString = value.GetValue<string>();
                    if (formatString != null && Enum.TryParse<MaskFormat>(formatString, out var parsedFormat))
                    {
                        Format = parsedFormat;
                        _isDirty = true;
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 5:
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
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
    /// Releases all resources used by the MaskResource.
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
    /// Returns a string representation of this mask resource.
    /// </summary>
    /// <returns>A string containing the mask's key and dimensions.</returns>
    public override string ToString()
    {
        return $"MaskResource [Key={_key}, Size={Width}x{Height}, Channels={Channels}, Format={Format}]";
    }
}

/// <summary>
/// Defines the supported mask formats.
/// </summary>
public enum MaskFormat
{
    /// <summary>
    /// Single-channel alpha mask.
    /// </summary>
    Alpha = 0,

    /// <summary>
    /// RGBA format with alpha channel.
    /// </summary>
    RGBA = 1,

    /// <summary>
    /// Grayscale mask.
    /// </summary>
    Grayscale = 2,

    /// <summary>
    /// Custom or unknown format.
    /// </summary>
    Custom = 255
}
