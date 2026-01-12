using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers.CasPartResource;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// ViewModel for DeformerMap preview visualization.
/// Converts deformer map data to visual representation.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 343-425 (DeformerMapControl)
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 289-486 (ToBitMap)
/// </remarks>
public partial class DeformerMapViewerViewModel : ViewModelBase
{
    private DeformerMapResource? _resource;

    [ObservableProperty]
    private Bitmap? _skinImage;

    [ObservableProperty]
    private Bitmap? _robeImage;

    [ObservableProperty]
    private bool _showSkin = true;

    [ObservableProperty]
    private bool _showRobe;

    [ObservableProperty]
    private bool _isPreviewAvailable;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private uint _version;

    [ObservableProperty]
    private uint _width;

    [ObservableProperty]
    private uint _height;

    [ObservableProperty]
    private string _ageGender = string.Empty;

    [ObservableProperty]
    private string _physique = string.Empty;

    [ObservableProperty]
    private string _shapeOrNormals = string.Empty;

    [ObservableProperty]
    private string _robeChannel = string.Empty;

    [ObservableProperty]
    private int _scanLineCount;

    /// <summary>
    /// The currently displayed image (skin or robe based on toggle).
    /// </summary>
    public Bitmap? CurrentImage => ShowRobe ? RobeImage : SkinImage;

    partial void OnShowSkinChanged(bool value)
    {
        if (value)
        {
            ShowRobe = false;
            OnPropertyChanged(nameof(CurrentImage));
        }
    }

    partial void OnShowRobeChanged(bool value)
    {
        if (value)
        {
            ShowSkin = false;
            OnPropertyChanged(nameof(CurrentImage));
        }
    }

    /// <summary>
    /// Loads a DeformerMapResource for preview.
    /// </summary>
    public void LoadResource(DeformerMapResource resource)
    {
        _resource = resource;
        Version = resource.Version;
        Width = resource.MaxCol > 0 ? resource.MaxCol - resource.MinCol + 1 : 0;
        Height = resource.MaxRow > 0 ? resource.MaxRow - resource.MinRow + 1 : 0;
        AgeGender = resource.AgeGender.ToString();
        Physique = resource.Physique.ToString();
        ShapeOrNormals = resource.IsShapeOrNormals.ToString();
        RobeChannel = resource.HasRobeChannel.ToString();
        ScanLineCount = resource.ScanLines.Count;

        if (Width == 0 || Height == 0 || resource.ScanLines.Count == 0)
        {
            SkinImage = null;
            RobeImage = null;
            IsPreviewAvailable = false;
            StatusMessage = "No pixel data in deformer map";
            return;
        }

        try
        {
            // Generate skin bitmap
            var skinPixels = GenerateBitmapPixels(resource, OutputType.Skin);
            if (skinPixels != null)
            {
                SkinImage = CreateBitmapFromRgb(skinPixels, (int)Width, (int)Height);
            }

            // Generate robe bitmap
            var robePixels = GenerateBitmapPixels(resource, OutputType.Robe);
            if (robePixels != null)
            {
                RobeImage = CreateBitmapFromRgb(robePixels, (int)Width, (int)Height);
            }

            IsPreviewAvailable = SkinImage != null || RobeImage != null;
            OnPropertyChanged(nameof(CurrentImage));

            if (IsPreviewAvailable)
            {
                StatusMessage = $"DeformerMap loaded ({Width}x{Height}, {ScanLineCount} scanlines)";
            }
            else
            {
                StatusMessage = "Failed to decode deformer map";
            }
        }
        catch (Exception ex)
        {
            SkinImage = null;
            RobeImage = null;
            IsPreviewAvailable = false;
            StatusMessage = $"Failed to decode deformer map: {ex.Message}";
        }
    }

    /// <summary>
    /// Output type for bitmap generation.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 290-294
    /// </remarks>
    private enum OutputType
    {
        Skin,
        Robe
    }

    /// <summary>
    /// Generates RGB pixel data from deformer map scan lines.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 297-486
    /// The legacy ToBitMap method decodes compressed and uncompressed scan lines into RGB pixels.
    /// </remarks>
    private static byte[]? GenerateBitmapPixels(DeformerMapResource resource, OutputType type)
    {
        if (resource.MaxCol == 0) return null;

        var height = resource.MaxRow - resource.MinRow + 1;
        var width = resource.MaxCol - resource.MinCol + 1;

        var pixelArraySkinTight = new byte[width * height * 3];
        var pixelArrayRobe = new byte[width * height * 3];

        int destIndexRobe = 0;
        int destSkinTight = 0;

        for (int i = 0; i < height; i++)
        {
            if (i >= resource.ScanLines.Count)
                break;

            var scan = resource.ScanLines[i];
            int pixelSize = scan.RobeChannel == Wrappers.CasPartResource.RobeChannel.Present ? 6 : 3;

            if (!scan.IsCompressed)
            {
                // Uncompressed scan line
                for (int j = 0; j < width; j++)
                {
                    if ((j * pixelSize) + 2 >= scan.UncompressedPixels.Length)
                        break;

                    pixelArraySkinTight[destSkinTight++] = scan.UncompressedPixels[(j * pixelSize) + 0];
                    pixelArraySkinTight[destSkinTight++] = scan.UncompressedPixels[(j * pixelSize) + 1];
                    pixelArraySkinTight[destSkinTight++] = scan.UncompressedPixels[(j * pixelSize) + 2];

                    switch (scan.RobeChannel)
                    {
                        case Wrappers.CasPartResource.RobeChannel.Present:
                            if ((j * pixelSize) + 5 < scan.UncompressedPixels.Length)
                            {
                                pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 3];
                                pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 4];
                                pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 5];
                            }
                            else
                            {
                                destIndexRobe += 3;
                            }
                            break;
                        case Wrappers.CasPartResource.RobeChannel.Dropped:
                            pixelArrayRobe[destIndexRobe++] = 0;
                            pixelArrayRobe[destIndexRobe++] = 0;
                            pixelArrayRobe[destIndexRobe++] = 0;
                            break;
                        case Wrappers.CasPartResource.RobeChannel.IsCopy:
                            pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 0];
                            pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 1];
                            pixelArrayRobe[destIndexRobe++] = scan.UncompressedPixels[(j * pixelSize) + 2];
                            break;
                    }
                }
            }
            else
            {
                // RLE compressed scan line - decode using index tables
                // Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 355-416
                for (int j = 0; j < width; j++)
                {
                    // Find proper RLE run using index tables
                    uint step = 1U + width / (uint)(scan.NumIndexes - 1U);
                    uint idx = (uint)(j / step);

                    if (idx >= scan.PixelPosIndexes.Length || idx >= scan.DataPosIndexes.Length)
                    {
                        destSkinTight += 3;
                        destIndexRobe += 3;
                        continue;
                    }

                    uint pixelPosX = scan.PixelPosIndexes[idx];
                    uint dataPos = (uint)(scan.DataPosIndexes[idx] * (pixelSize + 1));

                    if (dataPos >= scan.RleArrayOfPixels.Length)
                    {
                        destSkinTight += 3;
                        destIndexRobe += 3;
                        continue;
                    }

                    uint runLength = scan.RleArrayOfPixels[dataPos];

                    // Unwind RLE data to find the correct pixel
                    while (j >= pixelPosX + runLength && dataPos + pixelSize + 1 < scan.RleArrayOfPixels.Length)
                    {
                        pixelPosX += runLength;
                        dataPos += (uint)(1 + pixelSize);
                        runLength = scan.RleArrayOfPixels[dataPos];
                    }

                    uint pixelStart = dataPos + 1;
                    if (pixelStart + 2 >= scan.RleArrayOfPixels.Length)
                    {
                        destSkinTight += 3;
                        destIndexRobe += 3;
                        continue;
                    }

                    pixelArraySkinTight[destSkinTight++] = scan.RleArrayOfPixels[pixelStart + 0];
                    pixelArraySkinTight[destSkinTight++] = scan.RleArrayOfPixels[pixelStart + 1];
                    pixelArraySkinTight[destSkinTight++] = scan.RleArrayOfPixels[pixelStart + 2];

                    switch (scan.RobeChannel)
                    {
                        case Wrappers.CasPartResource.RobeChannel.Present:
                            if (pixelStart + 5 < scan.RleArrayOfPixels.Length)
                            {
                                pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 3];
                                pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 4];
                                pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 5];
                            }
                            else
                            {
                                destIndexRobe += 3;
                            }
                            break;
                        case Wrappers.CasPartResource.RobeChannel.Dropped:
                            pixelArrayRobe[destIndexRobe++] = 0;
                            pixelArrayRobe[destIndexRobe++] = 0;
                            pixelArrayRobe[destIndexRobe++] = 0;
                            break;
                        case Wrappers.CasPartResource.RobeChannel.IsCopy:
                            pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 0];
                            pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 1];
                            pixelArrayRobe[destIndexRobe++] = scan.RleArrayOfPixels[pixelStart + 2];
                            break;
                    }
                }
            }
        }

        return type == OutputType.Robe ? pixelArrayRobe : pixelArraySkinTight;
    }

    /// <summary>
    /// Creates a WriteableBitmap from RGB24 pixel data.
    /// </summary>
    /// <remarks>
    /// The legacy code writes BMP format but we create an Avalonia bitmap directly.
    /// Note: The deformer map data is bottom-up like BMP, so we flip vertically.
    /// </remarks>
    private static WriteableBitmap? CreateBitmapFromRgb(byte[] rgbPixels, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return null;

        var expectedSize = width * height * 3;
        if (rgbPixels.Length < expectedSize)
            return null;

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Rgba8888,
            AlphaFormat.Opaque);

        using var framebuffer = bitmap.Lock();

        // Convert RGB to RGBA and flip vertically (BMP is bottom-up)
        var stride = framebuffer.RowBytes;

        for (var y = 0; y < height; y++)
        {
            // Source row from bottom (flip vertically)
            int srcY = height - 1 - y;
            var destPtr = framebuffer.Address + y * stride;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = (srcY * width + x) * 3;
                var destOffset = x * 4;

                // RGB -> RGBA (add full alpha)
                Marshal.WriteByte(destPtr + destOffset, rgbPixels[srcIdx]);     // R
                Marshal.WriteByte(destPtr + destOffset + 1, rgbPixels[srcIdx + 1]); // G
                Marshal.WriteByte(destPtr + destOffset + 2, rgbPixels[srcIdx + 2]); // B
                Marshal.WriteByte(destPtr + destOffset + 3, 255);                   // A
            }
        }

        return bitmap;
    }
}
