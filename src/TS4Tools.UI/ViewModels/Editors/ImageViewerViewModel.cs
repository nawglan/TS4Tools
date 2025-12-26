using System.IO;
using Avalonia.Media.Imaging;
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
        FormatName = resource.Format.ToString();
        Width = resource.Width;
        Height = resource.Height;
        DataSize = resource.DataLength;

        LoadImage(resource);
    }

    private void LoadImage(ImageResource resource)
    {
        // Only PNG can be directly displayed by Avalonia
        if (resource.Format == ImageFormat.Png && resource.DataLength > 0)
        {
            try
            {
                // TODO: Verify Avalonia Bitmap copies data from stream. If not, stream must be kept alive
                // with the ViewModel lifetime to avoid potential issues with disposed stream access.
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
        else if (resource.Format == ImageFormat.Dds || resource.Format == ImageFormat.Dst)
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = $"{resource.Format} preview not supported - Export to view";
        }
        else
        {
            Image = null;
            IsPreviewAvailable = false;
            StatusMessage = "Unknown image format";
        }
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}
