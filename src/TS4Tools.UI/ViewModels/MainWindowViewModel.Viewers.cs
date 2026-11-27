// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2644-2670 (resource wrapped by WrapperDealer on selection)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2406-2409 (Settings > Manage Wrappers)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2867-2890 (controlPanel1_ValueClick floats the Value text)
// Source: legacy_references/Sims4Tools/s4pe Helpers/DDSHelper, RLEDDSHelper, RLESDDSHelper, DMAPImageHelper, ModelViewer

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Resources;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Editors;
using TS4Tools.UI.Views.Viewers;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Viewer selection through the wrapper registry, wrapper management and the image/model helper tools.
/// </summary>
public partial class MainWindowViewModel
{
    private EventHandler? _wrappersChangedHandler;
    private WrapperRegistryService? _wrapperService;

    /// <summary>What kind of view the current resource got.</summary>
    internal ResourceViewKind CurrentViewKind { get; private set; } = ResourceViewKind.Hex;

    /// <summary>The wrapper of the current resource (for tests).</summary>
    internal object? CurrentResourceWrapper => _currentResourceWrapper;

    private void InitializeViewers()
    {
        _wrapperService = WrapperRegistryService.Instance;
        _wrappersChangedHandler = (_, _) => _ = RunGuardedAsync(OnWrappersChangedAsync, "Error reloading resource");
        _wrapperService.WrappersChanged += _wrappersChangedHandler;
    }

    private void DisposeViewers()
    {
        if (_wrapperService != null && _wrappersChangedHandler != null)
        {
            _wrapperService.WrappersChanged -= _wrappersChangedHandler;
        }
        _wrappersChangedHandler = null;
        _wrapperService = null;
    }

    /// <summary>
    /// Re-resolves the shown resource after wrappers were enabled or disabled, unless it has uncommitted edits.
    /// </summary>
    /// <remarks>Legacy applies a changed disabled list to the next GetResource; reloading the current view shows it at once.</remarks>
    private async Task OnWrappersChangedAsync()
    {
        if (IsResourceDirty || SelectedResource == null) return;
        await UpdateSelectedResourceDetailsAsync();
    }

    /// <summary>
    /// Wraps the selected resource's data through the package's registry and chooses its view.
    /// </summary>
    private ResourceViewerSelection CreateViewerSelection(ResourceKey key, ReadOnlyMemory<byte> data, bool forceHex)
    {
        IResourceHandlerRegistry registry = _package?.ResourceHandlers ?? ResourceHandlerRegistry.Shared;
        var selection = ResourceViewerFactory.Create(key, data, registry, ResourceViewerOptions.From(ShellSettings, forceHex));
        CurrentViewKind = selection.Kind;
        return selection;
    }

    /// <summary>Views for the viewer view models added here.</summary>
    private static ValueViewerView? CreateViewerView(object viewModel) => viewModel switch
    {
        ValueViewerViewModel => new ValueViewerView { DataContext = viewModel },
        _ => null
    };

    /// <summary>
    /// Shows plugin load problems in the status bar without blocking start-up.
    /// </summary>
    public void ReportWrapperLoadErrors(IReadOnlyList<WrapperLoadError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (WrapperRegistryService.DescribeLoadErrors(errors) is { } message)
        {
            StatusMessage = message;
        }
    }

    #region Tool availability

    /// <summary>Whether the DDS export applies to the current resource.</summary>
    public bool CanExportDds => ResourceImageTools.CanExport(_currentResourceWrapper, ResourceExportKind.Dds);

    /// <summary>Whether a DDS can be imported into the current resource.</summary>
    public bool CanImportDds => ResourceImageTools.CanImport(_currentResourceWrapper, ResourceImportKind.Dds);

    /// <summary>Whether a DDS can be imported into the current resource as DST.</summary>
    public bool CanImportDdsToDst => ResourceImageTools.CanImport(_currentResourceWrapper, ResourceImportKind.DdsToDst);

    /// <summary>Whether the current resource is a DDS/DST/RLE image that can be edited (image import, HSV, alpha).</summary>
    public bool CanEditDdsImage => ResourceImageTools.CanEditImage(_currentResourceWrapper);

    /// <summary>Whether the current resource is a deformer map.</summary>
    public bool CanEditDeformerMapImage => _currentResourceWrapper is DeformerMapResource;

    /// <summary>Whether the current resource holds an MLOD or GEOM mesh.</summary>
    public bool CanOpenModelViewer => _currentResourceWrapper is RcolResource rcol && Services.ModelViewer.MeshExtractor.HasModel(rcol);

    /// <summary>Whether the current resource has a wrapper whose Value can be shown.</summary>
    public bool CanShowValue => _currentResourceWrapper != null;

    /// <summary>Whether any viewer tool applies.</summary>
    public bool HasViewerTools => CanExportDds || CanImportDds || CanEditDdsImage || CanEditDeformerMapImage || CanOpenModelViewer;

    private void OnViewerStateChanged()
    {
        OnPropertyChanged(nameof(CanExportDds));
        OnPropertyChanged(nameof(CanImportDds));
        OnPropertyChanged(nameof(CanImportDdsToDst));
        OnPropertyChanged(nameof(CanEditDdsImage));
        OnPropertyChanged(nameof(CanEditDeformerMapImage));
        OnPropertyChanged(nameof(CanOpenModelViewer));
        OnPropertyChanged(nameof(CanShowValue));
        OnPropertyChanged(nameof(HasViewerTools));
    }

    #endregion

    #region Commands

    /// <summary>Opens the Manage Wrappers dialog.</summary>
    /// <remarks>Source: MainForm.cs lines 2406-2409 (SettingsManageWrappers).</remarks>
    [RelayCommand]
    private async Task ManageWrappersAsync()
    {
        if (GetTopLevel() is not Window window) return;
        var dialog = new Views.Dialogs.ManageWrappersWindow();
        var applied = await dialog.ShowDialog<bool>(window);
        if (applied) StatusMessage = "Wrapper settings applied";
    }

    /// <summary>Floats the current wrapper's Value text.</summary>
    /// <remarks>Source: MainForm.cs lines 2867-2890 (ValueClick shows the Value text in a floating form).</remarks>
    [RelayCommand]
    private void ShowValue()
    {
        if (_currentResourceWrapper == null || SelectedResource == null) return;
        var vm = new ValueViewerViewModel();
        vm.LoadWrapper(_currentResourceWrapper);
        var window = new Views.Dialogs.FloatingPreviewWindow();
        window.SetContent(new ValueViewerView { DataContext = vm }, SelectedResource.InstanceName ?? SelectedResource.DisplayKey);
        ShowOwned(window);
    }

    /// <summary>Opens the 3D model viewer for the current MLOD/GEOM resource.</summary>
    /// <remarks>Source: s4pe Helpers/ModelViewer (MLODViewer.helper, GEOMViewer.helper).</remarks>
    [RelayCommand]
    private void OpenModelViewer()
    {
        if (_currentResourceWrapper is not RcolResource rcol || SelectedResource == null) return;
        var vm = new ModelViewerViewModel();
        vm.Load(rcol, SelectedResource.InstanceName ?? SelectedResource.DisplayKey);
        ShowOwned(new ModelViewerWindow(vm));
        StatusMessage = vm.Statistics;
    }

    [RelayCommand]
    private Task ExportDdsAsync() => PickAndExportAsync(ResourceExportKind.Dds, "Export to DDS");

    [RelayCommand]
    private Task ImportDdsAsync() => PickAndImportAsync(ResourceImportKind.Dds, "Import From DDS", "*.dds");

    [RelayCommand]
    private Task ImportDdsToDstAsync() => PickAndImportAsync(ResourceImportKind.DdsToDst, "Import DDS as DST", "*.dds");

    /// <summary>Replaces the image with a PNG/JPEG file (converted to the resource's DDS/DST/RLE layout).</summary>
    /// <remarks>Source: DDSPanel.cs lines 417-462 (Import(string/Stream)), DDSHelper/Import.cs lines 55-72.</remarks>
    [RelayCommand]
    private async Task ImportImageAsync()
    {
        if (!CanEditDdsImage) return;
        if (await PickImageFileAsync("Import from Image") is { } path)
        {
            await ImportImageIntoCurrentAsync(path);
        }
    }

    /// <summary>Takes the alpha channel from the greyscale of an image file.</summary>
    /// <remarks>Source: DDSPanel.cs lines 625-680 (SetAlphaFromGreyscale), DdsFile.cs lines 1392-1412.</remarks>
    [RelayCommand]
    private async Task SetAlphaFromGreyscaleAsync()
    {
        if (!CanEditDdsImage) return;
        if (await PickImageFileAsync("Alpha from Greyscale Image") is { } path)
        {
            await SetAlphaFromGreyscaleFileAsync(path);
        }
    }

    /// <summary>Makes the image opaque and drops its alpha channel.</summary>
    /// <remarks>Source: DdsFile.cs lines 1454-1466 (DeleteAlphaChannel).</remarks>
    [RelayCommand]
    private void DeleteAlphaChannel()
    {
        EditCurrentImage("Alpha channel deleted", image => image.DeleteAlphaChannel());
    }

    /// <summary>Makes the HSV shift set in the image viewer permanent.</summary>
    /// <remarks>Source: DDSPanel.cs lines 625-635 (HSVShift), RGBHSV.cs lines 160-171, DdsFile.cs lines 1535-1560.</remarks>
    [RelayCommand]
    private void ApplyHsvShift()
    {
        if (_currentEditorViewModel is not ImageViewerViewModel viewer || viewer.IsHsvNeutral) return;

        var shift = viewer.HsvShift;
        if (EditCurrentImage("HSV shift applied", image => image.ApplyHsvShift(shift)))
        {
            viewer.ResetHsvShift();
        }
    }

    [RelayCommand]
    private Task ExportDeformerMapSkinAsync() => PickAndExportAsync(ResourceExportKind.DeformerMapSkin, "Export to Bitmap (Skintight data)");

    [RelayCommand]
    private Task ExportDeformerMapRobeAsync() => PickAndExportAsync(ResourceExportKind.DeformerMapRobe, "Export to Bitmap (Robe data)");

    [RelayCommand]
    private Task ImportDeformerMapBitmapAsync() => PickAndImportAsync(ResourceImportKind.DeformerMapBitmap, "Import Bitmap", "*.bmp");

    #endregion

    #region Import / export

    /// <summary>
    /// Writes an export of the current resource to <paramref name="path"/>.
    /// </summary>
    internal async Task<bool> ExportCurrentAsync(ResourceExportKind kind, string path)
    {
        if (_currentResourceWrapper == null) return false;
        try
        {
            var bytes = ResourceImageTools.Export(_currentResourceWrapper, kind);
            await _fileSystem.WriteAllBytesAsync(path, bytes);
            StatusMessage = $"Exported to {_fileSystem.GetFileName(path)}";
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            StatusMessage = $"Export error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Replaces the current resource's data from the file at <paramref name="path"/>. The resource becomes dirty and
    /// is written to the package by Commit, like any other edit.
    /// </summary>
    internal async Task<bool> ImportIntoCurrentAsync(ResourceImportKind kind, string path)
    {
        if (_currentResourceWrapper == null || _loadedResourceItem == null) return false;
        try
        {
            var bytes = await _fileSystem.ReadAllBytesAsync(path);
            ResourceImageTools.Import(_currentResourceWrapper, kind, bytes);
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            StatusMessage = $"Import error: {ex.Message}";
            return false;
        }

        // Show the new image and mark the resource edited (some wrappers do not raise Changed for every import).
        switch (_currentEditorViewModel)
        {
            case ImageViewerViewModel image:
                image.Reload();
                break;
            case DeformerMapViewerViewModel dmap when _currentResourceWrapper is DeformerMapResource map:
                dmap.LoadResource(map);
                break;
        }
        OnResourceEdited();
        OnViewerStateChanged();
        StatusMessage = $"Imported {_fileSystem.GetFileName(path)} - commit to keep the change";
        return true;
    }

    /// <summary>Decodes PNG/JPEG files (replaceable for tests; Avalonia's Bitmap needs a rendering platform).</summary>
    internal Func<byte[], DecodedImage?> ImageDecoder { get; set; } = ImageFileDecoder.Decode;

    /// <summary>
    /// Replaces the current image with the pixels of an image file. The resource becomes dirty and is
    /// written to the package by Commit.
    /// </summary>
    internal async Task<bool> ImportImageIntoCurrentAsync(string path)
    {
        if (_currentResourceWrapper == null || !CanEditDdsImage) return false;

        var decoded = await DecodeImageFileAsync(path);
        if (decoded is not { } image) return false;

        try
        {
            ResourceImageTools.ImportRgba(_currentResourceWrapper, image.Rgba, image.Width, image.Height);
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or NotSupportedException or ArgumentException)
        {
            StatusMessage = $"Import error: {ex.Message}";
            return false;
        }

        AfterWrapperImageChanged();
        StatusMessage = $"Imported {_fileSystem.GetFileName(path)} - commit to keep the change";
        return true;
    }

    /// <summary>Sets the current image's alpha from the greyscale of an image file.</summary>
    internal async Task<bool> SetAlphaFromGreyscaleFileAsync(string path)
    {
        if (_currentResourceWrapper == null || !CanEditDdsImage) return false;

        var decoded = await DecodeImageFileAsync(path);
        if (decoded is not { } source) return false;

        return EditCurrentImage("Alpha set from greyscale image",
            image => image.SetAlphaFromGreyscale(DdsImage.FromRgba32(source.Rgba, source.Width, source.Height)));
    }

    private async Task<DecodedImage?> DecodeImageFileAsync(string path)
    {
        byte[] bytes;
        try
        {
            bytes = await _fileSystem.ReadAllBytesAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Import error: {ex.Message}";
            return null;
        }

        var decoded = ImageDecoder(bytes);
        if (decoded == null)
        {
            StatusMessage = "Import error: not a PNG or JPEG image";
        }
        return decoded;
    }

    /// <summary>
    /// Loads the current image, applies an edit and stores it back in the wrapper's layout.
    /// </summary>
    private bool EditCurrentImage(string status, Action<DdsImage> edit)
    {
        if (_currentResourceWrapper == null || !CanEditDdsImage) return false;
        try
        {
            var image = ResourceImageTools.LoadForEdit(_currentResourceWrapper);
            edit(image);
            ResourceImageTools.StoreEdited(_currentResourceWrapper, image);
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or NotSupportedException or ArgumentException)
        {
            StatusMessage = $"Image edit error: {ex.Message}";
            return false;
        }

        AfterWrapperImageChanged();
        StatusMessage = $"{status} - commit to keep the change";
        return true;
    }

    /// <summary>Shows the changed image and marks the resource edited (some wrappers do not raise Changed).</summary>
    private void AfterWrapperImageChanged()
    {
        if (_currentEditorViewModel is ImageViewerViewModel viewer)
        {
            viewer.Reload();
        }
        OnResourceEdited();
        OnViewerStateChanged();
    }

    private async Task<string?> PickImageFileAsync(string title)
    {
        var files = await Storage.PickOpenFilesAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg"] },
                AllFilesType
            ],
        });
        return files.Count == 1 ? files[0] : null;
    }

    private async Task PickAndExportAsync(ResourceExportKind kind, string title)
    {
        if (SelectedResource == null) return;
        var key = SelectedResource.Key;
        var ext = ResourceImageTools.GetExtension(kind);
        var path = await Storage.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}{ext}",
            DefaultExtension = ext,
            FileTypeChoices = [new FilePickerFileType(ext.TrimStart('.').ToUpperInvariant()) { Patterns = [$"*{ext}"] }, AllFilesType],
        });
        if (path != null)
        {
            await ExportCurrentAsync(kind, path);
        }
    }

    private async Task PickAndImportAsync(ResourceImportKind kind, string title, string pattern)
    {
        if (SelectedResource == null) return;
        var files = await Storage.PickOpenFilesAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(pattern) { Patterns = [pattern] }, AllFilesType],
        });
        if (files.Count == 1 && files[0] is { } path)
        {
            await ImportIntoCurrentAsync(kind, path);
        }
    }

    private void ShowOwned(Window window)
    {
        if (GetTopLevel() is Window owner)
            window.Show(owner);
        else
            window.Show();
    }

    #endregion
}
