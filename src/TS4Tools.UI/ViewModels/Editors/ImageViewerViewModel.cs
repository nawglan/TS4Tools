using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class ImageViewerViewModel : ViewModelBase
{
    private ImageResource? _resource;

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

    public void LoadResource(ImageResource resource)
    {
        _resource = resource;
        FormatName = GetFormatDisplayName(resource);
        Width = resource.Width;
        Height = resource.Height;
        DataSize = resource.DataLength;

        LoadImage(resource);
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
            var pixels = resource.GetDecodedPixels();
            if (pixels == null)
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

            // Create WriteableBitmap from RGBA pixels
            var bitmap = CreateBitmapFromRgba(pixels, Width, Height);
            if (bitmap == null)
            {
                Image = null;
                IsPreviewAvailable = false;
                StatusMessage = "Failed to create bitmap from decoded pixels";
                return;
            }

            Image = bitmap;
            IsPreviewAvailable = true;
            StatusMessage = $"{resource.Format} image loaded ({Width}x{Height}, {resource.DxtFormat})";
        }
        catch (Exception ex)
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to decode {resource.Format}: {ex.Message}";
        }
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

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}
