using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Services;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// ViewModel for image preview with channel toggle support.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 90-216 (DDSControl)
/// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 218-261 (RLEControl)
/// The legacy DDSControl and RLEControl provide R/G/B/A channel toggles and alpha inversion.
/// </remarks>
public partial class ImageViewerViewModel : ViewModelBase
{
    private ImageResource? _resource;
    private RleResource? _rleResource;
    private ThumbnailResource? _thumbnailResource;
    private byte[]? _decodedPixels;
    private bool _isRleFormat;

    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private string _formatName = string.Empty;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private int _dataSize;

    [ObservableProperty]
    private bool _isPreviewAvailable;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Channel toggles - Source: s4pe/BuiltInValueControl.cs line 97
    // static bool channel1 = true, channel2 = true, channel3 = true, channel4 = true, invertch4 = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChannelControls))]
    private bool _showRedChannel = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChannelControls))]
    private bool _showGreenChannel = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChannelControls))]
    private bool _showBlueChannel = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChannelControls))]
    private bool _showAlphaChannel = true;

    [ObservableProperty]
    private bool _invertAlpha;

    // HSV shift, applied to the preview and made permanent by "Apply HSV shift"
    // Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DDSPanel.cs lines 131-160 (HueShift,
    // SaturationShift, ValueShift: floats in the 0.0-1.0 domain, effective only when HSV is supported)

    /// <summary>Hue shift (-1 to 1, wraps).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HsvShift))]
    private float _hueShift;

    /// <summary>Saturation shift (-1 to 1).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HsvShift))]
    private float _saturationShift;

    /// <summary>Value shift (-1 to 1).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HsvShift))]
    private float _valueShift;

    /// <summary>The shift the HSV sliders describe.</summary>
    public HsvShift HsvShift => new(HueShift, SaturationShift, ValueShift);

    /// <summary>Whether the HSV sliders are at zero.</summary>
    public bool IsHsvNeutral => HsvShift.IsEmpty;

    partial void OnHueShiftChanged(float value) => OnHsvShiftChanged();
    partial void OnSaturationShiftChanged(float value) => OnHsvShiftChanged();
    partial void OnValueShiftChanged(float value) => OnHsvShiftChanged();

    private void OnHsvShiftChanged()
    {
        OnPropertyChanged(nameof(IsHsvNeutral));
        RefreshImage();
    }

    /// <summary>Sets the HSV sliders back to zero.</summary>
    [RelayCommand]
    public void ResetHsvShift()
    {
        HueShift = 0;
        SaturationShift = 0;
        ValueShift = 0;
    }

    /// <summary>
    /// The pixels as displayed: the decoded pixels with the HSV shift and channel settings applied
    /// (RGBA32), or null when no pixels are decoded.
    /// </summary>
    public byte[]? PreviewPixels => _decodedPixels == null ? null : ApplyChannelFilter(ApplyHsvPreview(_decodedPixels));

    /// <summary>
    /// Whether channel controls should be shown (for DDS/DST/RLE formats).
    /// </summary>
    public bool HasChannelControls => _isRleFormat || _resource?.Format is ImageFormat.Dds or ImageFormat.Dst;

    partial void OnShowRedChannelChanged(bool value) => RefreshImage();
    partial void OnShowGreenChannelChanged(bool value) => RefreshImage();
    partial void OnShowBlueChannelChanged(bool value) => RefreshImage();
    partial void OnShowAlphaChannelChanged(bool value) => RefreshImage();
    partial void OnInvertAlphaChanged(bool value) => RefreshImage();

    public void LoadResource(ImageResource resource)
    {
        _resource = resource;
        _rleResource = null;
        _thumbnailResource = null;
        _isRleFormat = false;
        FormatName = GetFormatDisplayName(resource);
        Width = resource.Width;
        Height = resource.Height;
        DataSize = resource.DataLength;

        LoadImage(resource);
        OnPropertyChanged(nameof(HasChannelControls));
    }

    /// <summary>
    /// Loads an RLE texture resource.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 218-261 (RLEControl)
    /// RLE textures are DXT5-compressed with additional run-length encoding.
    /// </remarks>
    public void LoadRleResource(RleResource resource)
    {
        _resource = null;
        _thumbnailResource = null;
        _rleResource = resource;
        _isRleFormat = true;
        FormatName = $"RLE ({resource.Version})";
        Width = resource.Width;
        Height = resource.Height;
        DataSize = resource.RawData.Length;

        LoadRleImage(resource);
        OnPropertyChanged(nameof(HasChannelControls));
    }

    /// <summary>
    /// Loads a JFIF thumbnail resource (JPEG, optionally with an ALFA alpha PNG).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 428-472 (ThumbnailControl shows
    /// ThumbnailResource.Image). The embedded ALFA alpha is composited onto the JPEG colour data (ThumbnailCompositor).
    /// </remarks>
    public void LoadThumbnailResource(ThumbnailResource resource)
    {
        _resource = null;
        _rleResource = null;
        _thumbnailResource = resource;
        _isRleFormat = false;
        _decodedPixels = null;
        FormatName = resource.HasEmbeddedAlpha ? "JFIF (with ALFA alpha)" : "JFIF";
        DataSize = resource.DataLength;
        Width = 0;
        Height = 0;

        // Source: ThumbnailResource.cs lines 119-160 (TransformToPNG): JPEG colour + ALFA alpha
        if (resource.HasEmbeddedAlpha && TryCompositeThumbnail(resource))
        {
            OnPropertyChanged(nameof(HasChannelControls));
            return;
        }

        try
        {
            using var stream = new MemoryStream(resource.GetJpegData().ToArray());
            Image = new Bitmap(stream);
            Width = Image.PixelSize.Width;
            Height = Image.PixelSize.Height;
            IsPreviewAvailable = true;
            StatusMessage = $"Thumbnail loaded ({Width}x{Height})";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or NotSupportedException or InvalidDataException)
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to load thumbnail: {ex.Message}";
        }

        OnPropertyChanged(nameof(HasChannelControls));
    }

    private bool TryCompositeThumbnail(ThumbnailResource resource)
    {
        try
        {
            var (rgba, width, height) = ThumbnailCompositor.Composite(resource.GetJpegData().Span, resource.GetAlphaPngData().Span);
            _decodedPixels = rgba;
            Width = width;
            Height = height;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or InvalidOperationException)
        {
            // Show the colour image alone, like a thumbnail without ALFA
            _decodedPixels = null;
            Debug.WriteLine($"[TS4Tools] Thumbnail alpha not applied: {ex.Message}");
            return false;
        }

        try
        {
            RefreshImage();
        }
        catch (InvalidOperationException ex)
        {
            // No rendering platform to create the bitmap (the decoded pixels are still available)
            Image = null;
            StatusMessage = $"Failed to create thumbnail bitmap: {ex.Message}";
            return true;
        }

        IsPreviewAvailable = Image != null;
        StatusMessage = $"Thumbnail loaded with alpha ({Width}x{Height})";
        return true;
    }

    /// <summary>
    /// Re-reads the image from the loaded wrapper (after its data was replaced, e.g. by an import).
    /// </summary>
    public void Reload()
    {
        if (_resource != null) LoadResource(_resource);
        else if (_rleResource != null) LoadRleResource(_rleResource);
        else if (_thumbnailResource != null) LoadThumbnailResource(_thumbnailResource);
    }

    /// <summary>The decoded RGBA32 pixels of a DDS/DST/RLE image, if any.</summary>
    public byte[]? DecodedPixels => _decodedPixels;

    private void LoadRleImage(RleResource resource)
    {
        try
        {
            // Convert RLE to DDS, then decode
            var ddsData = resource.ToDds();
            if (ddsData == null || ddsData.Length == 0)
            {
                Image = null;
                _decodedPixels = null;
                IsPreviewAvailable = false;
                StatusMessage = "Failed to decompress RLE texture";
                return;
            }

            // Decode the DDS data
            _decodedPixels = DxtDecoder.DecompressDds(ddsData);
            if (_decodedPixels == null)
            {
                Image = null;
                IsPreviewAvailable = false;
                StatusMessage = "Failed to decode RLE texture";
                return;
            }

            // Ensure we have valid dimensions
            if (Width <= 0 || Height <= 0)
            {
                Image = null;
                IsPreviewAvailable = false;
                StatusMessage = "Invalid image dimensions";
                return;
            }

            // Create bitmap with current channel settings
            RefreshImage();

            if (Image != null)
            {
                IsPreviewAvailable = true;
                StatusMessage = $"RLE image loaded ({Width}x{Height}, {resource.Version})";
            }
        }
        catch (Exception ex)
        {
            Image = null;
            _decodedPixels = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to decode RLE: {ex.Message}";
        }
    }

    private static string GetFormatDisplayName(ImageResource resource)
    {
        if (resource.Format == ImageFormat.Dds || resource.Format == ImageFormat.Dst)
        {
            var dxtFormat = resource.DxtFormat;
            return dxtFormat switch
            {
                DxtFormat.Dxt1 => $"{resource.Format} (DXT1/BC1)",
                DxtFormat.Dxt5 => $"{resource.Format} (DXT5/BC3)",
                _ => resource.Format.ToString()
            };
        }
        return resource.Format.ToString();
    }

    private void LoadImage(ImageResource resource)
    {
        // PNG can be directly displayed by Avalonia
        if (resource.Format == ImageFormat.Png && resource.DataLength > 0)
        {
            LoadPngImage(resource);
        }
        // DDS/DST textures need to be decoded first
        else if (resource.Format == ImageFormat.Dds || resource.Format == ImageFormat.Dst)
        {
            LoadDdsImage(resource);
        }
        else
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = "Unknown image format";
        }
    }

    private void LoadPngImage(ImageResource resource)
    {
        try
        {
            using var stream = new MemoryStream(resource.ImageData.ToArray());
            Image = new Bitmap(stream);
            IsPreviewAvailable = true;

            // Update dimensions from actual bitmap if not parsed
            if (Width == 0)
                Width = Image.PixelSize.Width;
            if (Height == 0)
                Height = Image.PixelSize.Height;

            StatusMessage = $"PNG image loaded ({Width}x{Height})";
        }
        catch (Exception ex)
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to load PNG: {ex.Message}";
        }
    }

    private void LoadDdsImage(ImageResource resource)
    {
        try
        {
            _decodedPixels = resource.GetDecodedPixels();
            if (_decodedPixels == null)
            {
                Image = null;
                IsPreviewAvailable = false;
                StatusMessage = $"{resource.Format} format not supported for preview";
                return;
            }

            // Ensure we have valid dimensions
            if (Width <= 0 || Height <= 0)
            {
                Image = null;
                IsPreviewAvailable = false;
                StatusMessage = "Invalid image dimensions";
                return;
            }

            // Create bitmap with current channel settings
            RefreshImage();

            if (Image != null)
            {
                IsPreviewAvailable = true;
                StatusMessage = $"{resource.Format} image loaded ({Width}x{Height}, {resource.DxtFormat})";
            }
        }
        catch (Exception ex)
        {
            Image = null;
            _decodedPixels = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to decode {resource.Format}: {ex.Message}";
        }
    }

    /// <summary>
    /// Refreshes the displayed image with current channel settings.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 119-134
    /// The DDSPanel in legacy code has Channel1-4 properties that filter the display.
    /// </remarks>
    private void RefreshImage()
    {
        if (_decodedPixels == null || Width <= 0 || Height <= 0)
            return;

        // Apply the HSV shift, then channel filtering
        var filteredPixels = ApplyChannelFilter(ApplyHsvPreview(_decodedPixels));

        // Create WriteableBitmap from filtered RGBA pixels (needs a rendering platform; the decoded pixels stay available)
        WriteableBitmap? bitmap;
        try
        {
            bitmap = CreateBitmapFromRgba(filteredPixels, Width, Height);
        }
        catch (InvalidOperationException)
        {
            bitmap = null;
        }

        if (bitmap == null)
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = "Failed to create bitmap from decoded pixels";
            return;
        }

        Image = bitmap;
    }

    /// <summary>
    /// Applies the HSV sliders to RGBA pixels (a copy; the decoded pixels stay unshifted).
    /// </summary>
    /// <remarks>Source: RGBHSV.cs lines 160-171 (HSVShift), via <see cref="ColorHsva.Shift"/>.</remarks>
    private byte[] ApplyHsvPreview(byte[] sourcePixels)
    {
        var shift = HsvShift;
        if (shift.IsEmpty) return sourcePixels;

        var result = new byte[sourcePixels.Length];
        for (var i = 0; i + 3 < sourcePixels.Length; i += 4)
        {
            var argb = ((uint)sourcePixels[i + 3] << 24) | ((uint)sourcePixels[i] << 16) | ((uint)sourcePixels[i + 1] << 8) | sourcePixels[i + 2];
            var shifted = ColorHsva.FromArgb(argb).Shift(shift).ToArgb();
            result[i] = (byte)(shifted >> 16);
            result[i + 1] = (byte)(shifted >> 8);
            result[i + 2] = (byte)shifted;
            result[i + 3] = (byte)(shifted >> 24);
        }
        return result;
    }

    /// <summary>
    /// Applies channel filtering to RGBA pixel data.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs
    /// When a channel is disabled, its value is set to 0 (for RGB) or 255 (for alpha).
    /// InvertCh4 inverts the alpha channel values.
    /// </remarks>
    private byte[] ApplyChannelFilter(byte[] sourcePixels)
    {
        // If all channels enabled and no inversion, return original
        if (ShowRedChannel && ShowGreenChannel && ShowBlueChannel && ShowAlphaChannel && !InvertAlpha)
            return sourcePixels;

        var result = new byte[sourcePixels.Length];

        for (var i = 0; i < sourcePixels.Length; i += 4)
        {
            // RGBA order
            result[i] = ShowRedChannel ? sourcePixels[i] : (byte)0;           // R
            result[i + 1] = ShowGreenChannel ? sourcePixels[i + 1] : (byte)0; // G
            result[i + 2] = ShowBlueChannel ? sourcePixels[i + 2] : (byte)0;  // B

            // Alpha channel with optional inversion
            byte alpha = sourcePixels[i + 3];
            if (InvertAlpha)
                alpha = (byte)(255 - alpha);
            result[i + 3] = ShowAlphaChannel ? alpha : (byte)255;             // A
        }

        return result;
    }

    /// <summary>
    /// Creates a WriteableBitmap from RGBA32 pixel data.
    /// </summary>
    private static WriteableBitmap? CreateBitmapFromRgba(byte[] pixels, int width, int height)
    {
        var expectedSize = width * height * 4;
        if (pixels.Length < expectedSize)
            return null;

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using var framebuffer = bitmap.Lock();

        // Copy pixel data to the bitmap
        // RGBA input matches Rgba8888 format directly
        var stride = framebuffer.RowBytes;
        var sourceStride = width * 4;

        if (stride == sourceStride)
        {
            // Fast path: direct copy when strides match
            Marshal.Copy(pixels, 0, framebuffer.Address, expectedSize);
        }
        else
        {
            // Slow path: copy row by row when strides differ
            for (var y = 0; y < height; y++)
            {
                var destPtr = framebuffer.Address + y * stride;
                Marshal.Copy(pixels, y * sourceStride, destPtr, sourceStride);
            }
        }

        return bitmap;
    }

    public ReadOnlyMemory<byte> GetData()
    {
        if (_resource != null)
            return _resource.Data;
        if (_rleResource != null)
            return _rleResource.Data;
        if (_thumbnailResource != null)
            return _thumbnailResource.Data;
        return ReadOnlyMemory<byte>.Empty;
    }
}
