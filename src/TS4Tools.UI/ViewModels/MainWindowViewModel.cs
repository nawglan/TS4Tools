using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Compatibility;
using TS4Tools;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Controls;
using TS4Tools.UI.Views.Dialogs;
using TS4Tools.UI.Views.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private bool _disposed;
    private readonly HashNameService _hashNameService = new();
    private readonly IFileSystemService _fileSystem;
    private readonly PropertyChangedEventHandler _propertyChangedHandler;
    // Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings DBPFFilesAndAll
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package", "*.dbc", "*.world", "*.nhd"] };
    private static readonly FilePickerFileType BinaryFileType = new("Binary File") { Patterns = ["*.bin", "*.dat"] };
    private static readonly FilePickerFileType AllFilesType = new("All Files") { Patterns = ["*"] };

    private string RecentFilesPath => _fileSystem.CombinePath(
        _fileSystem.GetApplicationDataPath(),
        "TS4Tools", "recent.json");

    private static int MaxRecentFiles => SettingsService.Instance.Settings.MaxRecentFiles;

    private DbpfPackage? _package;
    private readonly IDialogService _dialogs;
    private readonly Func<object, Control?> _viewFactory;

    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 611-626 (isPackageDirty)
    private bool _isPackageDirty;

    // Current resource edit state.
    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 695-705 (resource, resourceIsDirty)
    private ResourceItemViewModel? _loadedResourceItem;
    private object? _currentEditorViewModel;
    private IResourceEditorViewModel? _currentEditor;
    private Control? _currentEditorView;
    private PropertyGridViewModel? _propertyGridViewModel;
    private IResource? _subscribedWrapper;
    private readonly EventHandler _resourceEditedHandler;

    // Selection-change prompt support (legacy SelectedResourceChanging can cancel the change)
    private bool _suppressSelectionHandling;
    private Task _pendingSelectionTask = Task.CompletedTask;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOpenPackage))]
    [NotifyPropertyChangedFor(nameof(ResourceCount))]
    private string _packagePath = string.Empty;

    /// <summary>
    /// Whether the current package is opened in read-only mode.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 566-572 (ReadWrite property)
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsPackageDirty))]
    private bool _isReadOnly;

    /// <summary>
    /// Whether saving is allowed (package open, writable and has unsaved changes).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 616-625 (MBF_save enabled = ReadWrite &amp;&amp; dirty)
    /// </remarks>
    public bool CanSave => HasOpenPackage && IsPackageDirty;

    /// <summary>
    /// Whether the open package has changes that have not been saved to disk.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 616-625 (IsPackageDirty => ReadWrite &amp;&amp; isPackageDirty)
    /// </remarks>
    public bool IsPackageDirty => !IsReadOnly && _isPackageDirty;

    /// <summary>
    /// Whether the selected resource has edits that have not been committed to the package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 700-705 (resourceIsDirty / CommitEnabled)
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private bool _isResourceDirty;

    /// <summary>
    /// Whether there are uncommitted resource edits or unsaved package changes.
    /// Used by the window to decide whether closing needs to prompt.
    /// </summary>
    public bool HasUnsavedChanges => _package != null && (IsResourceDirty || _isPackageDirty);

    [ObservableProperty]
    private string _title = "TS4Tools";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedResource))]
    private ResourceItemViewModel? _selectedResource;

    public bool HasSelectedResource => SelectedResource != null;

    [ObservableProperty]
    private string _selectedResourceTypeName = string.Empty;

    [ObservableProperty]
    private string _selectedResourceKey = string.Empty;

    [ObservableProperty]
    private string _selectedResourceInfo = string.Empty;

    [ObservableProperty]
    private Control? _editorContent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PropertyGridButtonText))]
    private bool _showPropertyGrid;

    [ObservableProperty]
    private Control? _propertyGridContent;

    /// <summary>
    /// The current resource wrapper for property grid display.
    /// </summary>
    private object? _currentResourceWrapper;

    public string PropertyGridButtonText => ShowPropertyGrid ? "Hide Properties" : "Show Properties";

    public bool HasOpenPackage => _package != null;

    public int ResourceCount => _package?.ResourceCount ?? 0;

    /// <summary>
    /// Available helpers for the currently selected resource.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HelperMenuItemViewModel> _availableHelpers = [];

    public bool HasHelpers => AvailableHelpers.Count > 0;

    /// <summary>
    /// Bookmarked package files for quick access.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
    /// Format: "0:path" for read-only, "1:path" for read-write
    /// </remarks>
    public ObservableCollection<BookmarkItemViewModel> Bookmarks { get; } = [];

    public bool HasBookmarks => Bookmarks.Count > 0;

    public ObservableCollection<ResourceItemViewModel> Resources { get; } = [];

    public ObservableCollection<ResourceItemViewModel> FilteredResources { get; } = [];

    /// <summary>
    /// The currently selected resources (for multi-select operations).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs
    /// </remarks>
    public ObservableCollection<ResourceItemViewModel> SelectedResources { get; } = [];

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = [];

    /// <summary>
    /// Creates a new instance of the MainWindowViewModel.
    /// </summary>
    /// <param name="fileSystem">File system service for I/O operations. Uses default if null.</param>
    /// <param name="dialogs">User prompt service (commit/save prompts, save path). Uses Avalonia dialogs if null.</param>
    /// <param name="viewFactory">Creates the view for an editor view model. Uses the built-in views if null.</param>
    public MainWindowViewModel(
        IFileSystemService? fileSystem = null,
        IDialogService? dialogs = null,
        Func<object, Control?>? viewFactory = null)
    {
        _fileSystem = fileSystem ?? FileSystemService.Instance;
        _dialogs = dialogs ?? DialogService.Instance;
        _viewFactory = viewFactory ?? CreateDefaultView;
        _resourceEditedHandler = (_, _) => OnResourceEdited();

        _propertyChangedHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedResource) && !_suppressSelectionHandling)
            {
                _pendingSelectionTask = RunGuardedAsync(HandleSelectedResourceChangedAsync, "Error loading resource");
            }
            else if (e.PropertyName == nameof(IsResourceDirty))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        };
        PropertyChanged += _propertyChangedHandler;
        InitializeResourceOps();

        _ = LoadRecentFilesAsync();
        LoadBookmarks();
        InitializeShell();
        InitializeViewers();
    }

    /// <summary>
    /// Creates a new empty package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 787-792 (FileNew method)
    /// </remarks>
    [RelayCommand]
    private async Task NewPackageAsync()
    {
        if (!await ConfirmClosePackageAsync()) return;
        await CloseCurrentPackageAsync();

        _package = DbpfPackage.CreateNew();
        PackagePath = "";

        _hashNameService.Clear();
        PopulateResourceList();

        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs line 791 (FileNew sets IsPackageDirty = true)
        SetPackageDirty(true);

        StatusMessage = "New package created";
        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private async Task OpenPackageAsync()
    {
        var files = await Storage.PickOpenFilesAsync(new FilePickerOpenOptions
        {
            Title = "Open Package",
            AllowMultiple = false,
            FileTypeFilter = [PackageFileType, AllFilesType]
        });

        if (files.Count == 1)
        {
            await LoadPackageAsync(files[0], readOnly: false);
        }
    }

    /// <summary>
    /// Opens a package in read-only mode.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 794-797 (FileOpenReadOnly)
    /// </remarks>
    [RelayCommand]
    private async Task OpenPackageReadOnlyAsync()
    {
        var files = await Storage.PickOpenFilesAsync(new FilePickerOpenOptions
        {
            Title = "Open Package (Read Only)",
            AllowMultiple = false,
            FileTypeFilter = [PackageFileType, AllFilesType]
        });

        if (files.Count == 1)
        {
            await LoadPackageAsync(files[0], readOnly: true);
        }
    }

    /// <summary>
    /// Loads a package from disk.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 799-839 (FileOpen)
    /// </remarks>
    private async Task LoadPackageAsync(string path, bool readOnly = false)
    {
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 576-594 (Filename setter
        // closes the current package first, which prompts and can be cancelled)
        if (!await ConfirmClosePackageAsync()) return;

        try
        {
            StatusMessage = $"Loading {_fileSystem.GetFileName(path)}...";

            await CloseCurrentPackageAsync();

            // Read-write unless opened read-only: DbpfPackage defaults to read-only, which
            // would make every commit, import, delete and save fail.
            var package = await DbpfPackage.OpenAsync(path, readWrite: !readOnly);
            await AttachPackageAsync(package, path, readOnly);
            await AddToRecentFilesAsync(path);

            var nameCount = _hashNameService.Count;
            StatusMessage = nameCount > 0
                ? $"Loaded {ResourceCount} resources ({nameCount} named)" + (readOnly ? " [Read Only]" : "")
                : $"Loaded {ResourceCount} resources" + (readOnly ? " [Read Only]" : "");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Makes an already-opened package the current package. The previous package must be closed.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 249-264 (MainForm_PackageFilenameChanged)
    /// Internal so tests can drive the view model with an in-memory package.
    /// </remarks>
    internal async Task AttachPackageAsync(DbpfPackage package, string path, bool readOnly = false)
    {
        _package = package;
        _isPackageDirty = false;
        PackagePath = path;
        IsReadOnly = readOnly;

        // Load hash-to-name mappings from NameMap resources
        await _hashNameService.LoadFromPackageAsync(_package);

        PopulateResourceList();
        UpdateTitle();

        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(IsPackageDirty));
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    private void PopulateResourceList()
    {
        Resources.Clear();
        FilteredResources.Clear();

        if (_package == null) return;

        foreach (var entry in _package.Resources)
        {
            var instanceName = _hashNameService.TryGetName(entry.Key.Instance);
            var item = new ResourceItemViewModel(entry, instanceName);
            Resources.Add(item);
            FilteredResources.Add(item);
        }

        ApplyFilter();
    }

    /// <summary>
    /// Rebuilds <see cref="FilteredResources"/> from <see cref="Resources"/> using the resource
    /// filter and the browser sort order (see MainWindowViewModel.Shell.cs).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 969-975, 978-1010
    /// (resourceList = filter == null ? pkg.GetResourceList : FilteredList(); UpdateList)
    /// </remarks>
    private void ApplyFilter() => RebuildBrowserList();

    /// <summary>
    /// Handles a change of <see cref="SelectedResource"/>: prompts to commit pending edits
    /// to the previously selected resource, then loads the new one.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2609-2642
    /// (browserWidget1_SelectedResourceChanging - Yes commits, No discards, Cancel keeps the
    /// old selection) and lines 2644-2670 (browserWidget1_SelectedResourceChanged).
    /// </remarks>
    private async Task HandleSelectedResourceChangedAsync()
    {
        var target = SelectedResource;
        var previous = _loadedResourceItem;

        if (IsResourceDirty && previous != null && !ReferenceEquals(previous, target))
        {
            var choice = await _dialogs.AskCommitChangesAsync(GetResourceDisplayName(previous));
            if (choice == CommitPromptResult.Cancel)
            {
                // Restore the previous selection without re-triggering the prompt.
                SetSelectedResourceSilently(previous);
                return;
            }

            if (choice == CommitPromptResult.Commit)
            {
                CommitCurrentResource();
            }
        }

        // The selection may have moved on again while the prompt was open.
        if (!ReferenceEquals(SelectedResource, target)) return;

        await UpdateSelectedResourceDetailsAsync();
    }

    private void SetSelectedResourceSilently(ResourceItemViewModel? item)
    {
        _suppressSelectionHandling = true;
        try
        {
            SelectedResource = item;
        }
        finally
        {
            _suppressSelectionHandling = false;
        }
    }

    /// <summary>
    /// Completes when the most recent selection change (including any commit prompt) has been processed.
    /// </summary>
    internal Task PendingSelectionTask => _pendingSelectionTask;

    /// <summary>
    /// Gets the display name used in commit prompts: the resource name, else its TGI.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2620-2626
    /// </remarks>
    private static string GetResourceDisplayName(ResourceItemViewModel item)
        => !string.IsNullOrEmpty(item.InstanceName)
            ? item.InstanceName
            : $"TGI {item.Key.ResourceType:X8}-{item.Key.ResourceGroup:X8}-{item.Key.Instance:X16}";

    private async Task UpdateSelectedResourceDetailsAsync()
    {
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2615-2616, 2669
        // (unsubscribe from the old resource, reset resourceIsDirty for the new one)
        ClearResourceEditState();

        if (SelectedResource == null || _package == null)
        {
            SelectedResourceTypeName = string.Empty;
            SelectedResourceKey = string.Empty;
            SelectedResourceInfo = string.Empty;
            EditorContent = null;
            PropertyGridContent = null;
            return;
        }

        var item = SelectedResource;
        var key = item.Key;
        SelectedResourceTypeName = item.TypeName;
        SelectedResourceKey = item.DisplayKey;

        var entry = ResolveEntry(item);
        if (entry != null)
        {
            var data = await _package.GetResourceDataAsync(entry);

            // Selection changed again while reading - the newer change will load its own resource.
            if (!ReferenceEquals(SelectedResource, item)) return;

            var info = $"Type: 0x{key.ResourceType:X8}\n";
            info += $"Group: 0x{key.ResourceGroup:X8}\n";
            info += $"Instance: 0x{key.Instance:X16}\n";
            info += $"\nData Size: {data.Length:N0} bytes\n";
            info += $"Compressed: {entry.IsCompressed}";

            SelectedResourceInfo = info;

            // Wrapper resolved through the package's registry, view chosen by wrapper type.
            // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2651-2654 (GetResource(..., HexOnly))
            var selection = CreateViewerSelection(key, data, ForceHexForSelection);
            var editorViewModel = selection.EditorViewModel;
            object? wrapper = selection.Wrapper;

            SetResourceEditState(item, editorViewModel, wrapper);
            EditorContent = ApplyAutoChoice(_currentEditorView, data);

            // Update property grid if visible
            if (ShowPropertyGrid)
            {
                UpdatePropertyGrid(_currentResourceWrapper);
            }

            // Update available helpers
            UpdateAvailableHelpers(key);
        }
    }

    /// <summary>
    /// Records the loaded resource's editor and wrapper and subscribes to their change signals.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2663-2669
    /// (resource.ResourceChanged += resource_ResourceChanged; resourceIsDirty = false)
    /// </remarks>
    private void SetResourceEditState(ResourceItemViewModel item, object editorViewModel, object? wrapper)
    {
        _loadedResourceItem = item;
        _currentEditorViewModel = editorViewModel;
        _currentResourceWrapper = wrapper;
        _currentEditorView = _viewFactory(editorViewModel);

        _currentEditor = editorViewModel as IResourceEditorViewModel;
        if (_currentEditor != null)
        {
            _currentEditor.ContentChanged += _resourceEditedHandler;
        }

        _subscribedWrapper = wrapper as IResource;
        if (_subscribedWrapper != null)
        {
            _subscribedWrapper.Changed += _resourceEditedHandler;
        }

        IsResourceDirty = false;
        OnViewerStateChanged();
    }

    /// <summary>
    /// Unsubscribes from and disposes the current editor state, discarding uncommitted edits.
    /// </summary>
    private void ClearResourceEditState()
    {
        if (_currentEditor != null)
        {
            _currentEditor.ContentChanged -= _resourceEditedHandler;
            _currentEditor = null;
        }

        if (_subscribedWrapper != null)
        {
            _subscribedWrapper.Changed -= _resourceEditedHandler;
            _subscribedWrapper = null;
        }

        ClearPropertyGridViewModel();

        (_currentEditorViewModel as IDisposable)?.Dispose();
        _currentEditorViewModel = null;
        _currentEditorView = null;
        _currentResourceWrapper = null;
        _loadedResourceItem = null;
        IsResourceDirty = false;
        OnViewerStateChanged();
    }

    /// <summary>
    /// Called when the editor, wrapper or property grid reports an edit.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 702-705 (resource_ResourceChanged)
    /// </remarks>
    private void OnResourceEdited()
    {
        // A package opened read-only cannot accept ReplaceResource, so edits there are
        // view-only and never become committable.
        if (_loadedResourceItem != null && _package is { IsReadOnly: false })
        {
            IsResourceDirty = true;
        }
    }

    /// <summary>
    /// Writes the current resource's edits back into the package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3060-3073 (controlPanel1_CommitClick)
    /// </remarks>
    [RelayCommand(CanExecute = nameof(IsResourceDirty))]
    private void Commit()
    {
        if (CommitCurrentResource())
        {
            StatusMessage = "Changes committed";
        }
    }

    /// <summary>
    /// Commits the current resource. Returns false if there was nothing that could be committed.
    /// </summary>
    private bool CommitCurrentResource()
    {
        if (_package == null || _loadedResourceItem == null) return false;

        var entry = ResolveEntry(_loadedResourceItem);
        if (entry == null) return false;

        ReadOnlyMemory<byte> data;
        if (_currentEditor != null)
        {
            data = _currentEditor.GetData();
        }
        else if (_currentResourceWrapper is IResource resource)
        {
            // Property-grid-only edits on a viewer without its own editor state
            data = resource.Data;
        }
        else
        {
            return false;
        }

        try
        {
            _package.ReplaceResource(entry, data);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = $"Commit error: {ex.Message}";
            return false;
        }

        // Entry FileSize is only recalculated on save; show the new data size meanwhile.
        _loadedResourceItem.FileSize = entry.MemorySize;
        IsResourceDirty = false;
        SetPackageDirty(true);
        return true;
    }

    /// <summary>
    /// If the current resource has uncommitted edits, asks whether to commit them.
    /// </summary>
    /// <param name="reloadOnDiscard">Reload the editor from the package when edits are discarded.</param>
    /// <returns>False if the user cancelled.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2617-2641
    /// </remarks>
    private async Task<bool> ResolvePendingResourceEditsAsync(bool reloadOnDiscard)
    {
        if (!IsResourceDirty || _loadedResourceItem == null) return true;

        var choice = await _dialogs.AskCommitChangesAsync(GetResourceDisplayName(_loadedResourceItem));
        switch (choice)
        {
            case CommitPromptResult.Cancel:
                return false;
            case CommitPromptResult.Commit:
                CommitCurrentResource();
                return true;
            default:
                if (reloadOnDiscard)
                {
                    await UpdateSelectedResourceDetailsAsync();
                }
                else
                {
                    IsResourceDirty = false;
                }
                return true;
        }
    }

    /// <summary>
    /// Asks the user what to do with uncommitted resource edits and unsaved package changes
    /// before the current package is closed (close, new, open, exit).
    /// </summary>
    /// <returns>True if it is OK to close the package; false if the user cancelled.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 628-683 (CurrentPackage setter:
    /// deselect resource - which prompts to commit - then "Save now?" Yes/No/Cancel; read-only
    /// packages are saved with Save As) and lines 231-247 (MainForm_FormClosing).
    /// </remarks>
    public async Task<bool> ConfirmClosePackageAsync()
    {
        if (_package == null) return true;

        if (!await ResolvePendingResourceEditsAsync(reloadOnDiscard: false)) return false;

        if (_isPackageDirty)
        {
            var choice = await _dialogs.AskSaveChangesAsync();
            if (choice == SavePromptResult.Cancel) return false;

            if (choice == SavePromptResult.Save)
            {
                var saved = !IsReadOnly && !string.IsNullOrEmpty(PackagePath)
                    ? await SavePackageAsync()
                    : await SavePackageAsAsync();
                if (!saved) return false;
            }

            SetPackageDirty(false);
        }

        return true;
    }

    /// <summary>
    /// Persists the application settings. Replaceable so tests do not write the user's settings file.
    /// </summary>
    internal Func<Task> SaveSettingsAsync { get; set; } = () => SettingsService.Instance.SaveAsync();

    /// <summary>
    /// Runs the main window's closing flow: prompts to commit/save the current package, then stores
    /// and saves the application settings, all before the window actually closes.
    /// </summary>
    /// <param name="storeShellSettings">
    /// Copies the window's own state (placement, splitters, column widths) into the settings; run
    /// after the prompts and before the settings are saved.
    /// </param>
    /// <returns>True if the window may close; false if the user cancelled.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 231-247 (MainForm_FormClosing closes
    /// the package, cancelling if it stays open, then calls SaveAppSettingsAndRaiseSettingsEvent while
    /// the form is still open). Saving here, asynchronously, replaces blocking on the save in Closed.
    /// </remarks>
    public async Task<bool> PrepareWindowCloseAsync(Action? storeShellSettings = null)
    {
        if (HasUnsavedChanges && !await ConfirmClosePackageAsync()) return false;

        storeShellSettings?.Invoke();
        await SaveSettingsAsync();
        return true;
    }

    /// <summary>
    /// Sets the package dirty flag and refreshes dependent state (title, Save enablement).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 616-626 (IsPackageDirty setter)
    /// </remarks>
    private void MarkPackageDirty()
    {
        if (!_isPackageDirty)
        {
            SetPackageDirty(true);
        }
    }

    private void SetPackageDirty(bool dirty)
    {
        _isPackageDirty = dirty;
        OnPropertyChanged(nameof(IsPackageDirty));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        UpdateTitle();
    }

    /// <summary>
    /// Builds the window title from the package name, read-only state and dirty marker.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 255-264 (title shows name and RW/RO).
    /// A trailing "*" marks unsaved changes.
    /// </remarks>
    private void UpdateTitle()
    {
        if (_package == null)
        {
            Title = "TS4Tools";
            return;
        }

        var name = string.IsNullOrEmpty(PackagePath) ? "[New Package]" : _fileSystem.GetFileName(PackagePath);
        var dirtyMarker = _isPackageDirty ? "*" : string.Empty;
        var readOnlyMarker = IsReadOnly ? " [Read Only]" : string.Empty;
        Title = $"TS4Tools - {name}{dirtyMarker}{readOnlyMarker}";
    }

    /// <summary>
    /// Updates the list of available helpers for the given resource key.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3095-3134 (SetHelpers)
    /// </remarks>
    private void UpdateAvailableHelpers(ResourceKey key)
    {
        AvailableHelpers.Clear();

        var wrapperName = _currentResourceWrapper?.GetType().Name;
        var helpers = HelperManager.Instance.GetHelpersForResource(
            key.ResourceType,
            key.ResourceGroup,
            key.Instance,
            wrapperName);

        // Filter out disabled helpers
        var settings = SettingsService.Instance.Settings;
        var enabledHelpers = helpers.Where(h => !settings.DisabledHelpers.Contains(h.Id));

        foreach (var helper in enabledHelpers)
        {
            AvailableHelpers.Add(new HelperMenuItemViewModel(helper, ExecuteHelperAsync));
        }

        OnPropertyChanged(nameof(HasHelpers));
    }

    /// <summary>
    /// Executes a helper program for the currently selected resource.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3147-3200 (do_HelperClick)
    /// </remarks>
    internal async Task ExecuteHelperAsync(HelperInstance helper)
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = ResolveEntry(SelectedResource);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        StatusMessage = $"Running {helper.Label}...";

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var result = await helper.ExecuteAsync(
                data,
                SelectedResource.InstanceName,
                HelperProperties.From(_currentResourceWrapper),
                HelperClipboard);

            if (!result.Success)
            {
                StatusMessage = $"Helper error: {result.ErrorMessage}";
                return;
            }

            if (result.ModifiedData != null)
            {
                // Source: MainForm.cs lines 3157-3161 (AfterEdit unless the helper is read only)
                StatusMessage = await AfterEditAsync(SelectedResource, entry, result.ModifiedData)
                    ? $"Resource updated by {helper.Label}"
                    : $"{helper.Label} changes not committed";
            }
            else
            {
                StatusMessage = $"{helper.Label} completed (no changes)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Helper error: {ex.Message}";
        }
    }

    private static HexViewerViewModel CreateHexViewer(ReadOnlyMemory<byte> data)
    {
        var vm = new HexViewerViewModel();
        vm.LoadData(data);
        return vm;
    }

    /// <summary>
    /// Creates the built-in view for an editor/viewer view model.
    /// </summary>
    private static Control? CreateDefaultView(object viewModel) => viewModel switch
    {
        HexViewerViewModel => new HexViewerView { DataContext = viewModel },
        StblEditorViewModel => new StblEditorView { DataContext = viewModel },
        NameMapEditorViewModel => new NameMapEditorView { DataContext = viewModel },
        TextEditorViewModel => new TextEditorView { DataContext = viewModel },
        ImageViewerViewModel => new ImageViewerView { DataContext = viewModel },
        SimDataViewerViewModel => new SimDataViewerView { DataContext = viewModel },
        RcolViewerViewModel => new RcolViewerView { DataContext = viewModel },
        CatalogViewerViewModel => new CatalogViewerView { DataContext = viewModel },
        DeformerMapViewerViewModel => new DeformerMapViewerView { DataContext = viewModel },
        PropertyGridViewModel => new PropertyGridEditorView { DataContext = viewModel },
        _ => CreateViewerView(viewModel)
    };

    /// <summary>
    /// Saves the package, committing (or discarding) pending resource edits first.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 841-865 (FileSave)
    /// </remarks>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_package == null) return;
        if (!await ResolvePendingResourceEditsAsync(reloadOnDiscard: true)) return;

        if (IsReadOnly || string.IsNullOrEmpty(PackagePath))
        {
            await SavePackageAsAsync();
        }
        else
        {
            await SavePackageAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_package == null) return;
        if (!await ResolvePendingResourceEditsAsync(reloadOnDiscard: true)) return;

        await SavePackageAsAsync();
    }

    /// <summary>
    /// Saves the package to its current file.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 841-865 (FileSave)
    /// </remarks>
    private async Task<bool> SavePackageAsync()
    {
        if (_package == null) return false;
        if (string.IsNullOrEmpty(PackagePath)) return await SavePackageAsAsync();

        try
        {
            await _package.SaveAsync();
            SetPackageDirty(false);
            RefreshResourceSizes();
            StatusMessage = "Package saved";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Saves the package to a new file chosen by the user and makes that file the current package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 867-901 (FileSaveAs: same path
    /// falls back to FileSave; otherwise SaveAs, clear dirty and reopen the new file read-write)
    /// </remarks>
    private async Task<bool> SavePackageAsAsync()
    {
        if (_package == null) return false;

        var path = await _dialogs.PickSavePackagePathAsync("Save Package As");
        if (string.IsNullOrEmpty(path)) return false;

        if (!string.IsNullOrEmpty(PackagePath) && !IsReadOnly &&
            FileIdentity.IsSameFile(path, PackagePath))
        {
            return await SavePackageAsync();
        }

        try
        {
            await _package.SaveAsAsync(path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
            return false;
        }

        // Reopen from the new file so subsequent saves go there (legacy: Filename = "1:" + path).
        _isPackageDirty = false;
        await CloseCurrentPackageAsync();
        try
        {
            var package = await DbpfPackage.OpenAsync(path, readWrite: true);
            await AttachPackageAsync(package, path, readOnly: false);
            await AddToRecentFilesAsync(path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Saved, but failed to reopen: {ex.Message}";
            return true;
        }

        StatusMessage = "Package saved";
        return true;
    }

    /// <summary>
    /// Refreshes list item sizes from the package index (e.g. after a save re-reads it).
    /// </summary>
    private void RefreshResourceSizes() => ResyncResourceListAfterSave();

    /// <summary>
    /// Saves a copy of the package without changing the current file path.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 903-928 (FileSaveCopyAs).
    /// Like legacy, this does not commit pending resource edits or clear the dirty flag.
    /// </remarks>
    [RelayCommand]
    private async Task SaveCopyAsAsync()
    {
        if (_package == null) return;

        var copyPath = await _dialogs.PickSavePackagePathAsync(
            "Save Copy As",
            !string.IsNullOrEmpty(PackagePath)
                ? $"{Path.GetFileNameWithoutExtension(PackagePath)}_copy.package"
                : "copy.package");

        if (string.IsNullOrEmpty(copyPath)) return;

        // A copy cannot replace the open file: the package would save in place behind the resource list's back
        // (its entries are re-read on save), so the list would show stale entries and later edits could be lost.
        if (!string.IsNullOrEmpty(PackagePath) &&
            FileIdentity.IsSameFile(copyPath, PackagePath))
        {
            StatusMessage = "A copy cannot replace the open package; use Save to save it in place.";
            return;
        }

        try
        {
            await _package.SaveAsAsync(copyPath);
            // Don't update PackagePath, Title or the dirty flag - this is a copy
            StatusMessage = $"Copy saved to {_fileSystem.GetFileName(copyPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }

    /// <summary>
    /// Closes the current package, prompting for uncommitted/unsaved changes.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 930-933 (FileClose)
    /// </remarks>
    [RelayCommand]
    private async Task ClosePackageAsync()
    {
        if (!await ConfirmClosePackageAsync()) return;
        await CloseCurrentPackageAsync();
        StatusMessage = "Ready";
    }

    /// <summary>
    /// Closes the current package without prompting. Callers must prompt first
    /// (see <see cref="ConfirmClosePackageAsync"/>); any uncommitted edits are discarded.
    /// </summary>
    private async ValueTask CloseCurrentPackageAsync()
    {
        ClearResourceEditState();
        SetSelectedResourceSilently(null);

        if (_package != null)
        {
            await _package.DisposeAsync();
            _package = null;
        }
        _isPackageDirty = false;
        _hashNameService.Clear();
        PackagePath = string.Empty;
        IsReadOnly = false;
        Resources.Clear();
        FilteredResources.Clear();
        PackageInfo.Update(null);
        SelectedResourceTypeName = string.Empty;
        SelectedResourceKey = string.Empty;
        SelectedResourceInfo = string.Empty;
        EditorContent = null;
        PropertyGridContent = null;
        UpdateTitle();
        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(IsPackageDirty));
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(PackagePath))
        {
            var wasReadOnly = IsReadOnly;
            await LoadPackageAsync(PackagePath, wasReadOnly);
        }
    }

    /// <summary>
    /// Exits the application by closing the main window, which runs the close prompts.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 985-988 (FileExit calls Close(),
    /// so MainForm_FormClosing gets a chance to prompt and cancel)
    /// </remarks>
    [RelayCommand]
    private void Exit() => Storage.CloseMainWindow();

    [RelayCommand]
    private async Task AboutAsync()
    {
        var topLevel = GetTopLevel();

        if (topLevel is Window window)
        {
            var dialog = new Window
            {
                Title = "About TS4Tools",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(24),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "TS4Tools", FontSize = 24, FontWeight = Avalonia.Media.FontWeight.Bold },
                        new TextBlock { Text = "Cross-platform Sims 4 Package Editor" },
                        new TextBlock { Text = $"Version {UpdateCheckerService.CurrentVersion}", Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                        new TextBlock { Text = "A modern rewrite of s4pe/s4pi", Opacity = 0.7 },
                        new TextBlock { Text = "Licensed under GPLv3", Margin = new Avalonia.Thickness(0, 16, 0, 0), Opacity = 0.7 }
                    }
                }
            };

            await dialog.ShowDialog(window);
        }
    }

    /// <summary>
    /// Checks for application updates.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs lines 82-129
    /// </remarks>
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        var topLevel = GetTopLevel();

        if (topLevel is not Window window) return;

        var result = await UpdateCheckerService.Instance.CheckForUpdatesAsync();

        if (!result.Success)
        {
            var errorDialog = new Window
            {
                Title = "Update Check Failed",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(24),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Failed to check for updates", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                        new TextBlock { Text = result.ErrorMessage ?? "Unknown error", TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                    }
                }
            };
            await errorDialog.ShowDialog(window);
            return;
        }

        if (result.UpdateAvailable)
        {
            var updateDialog = new Window
            {
                Title = "Update Available",
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var yesButton = new Button { Content = "Visit Download Page", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
            var noButton = new Button { Content = "Later", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

            yesButton.Click += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(result.DownloadUrl) { UseShellExecute = true });
                }
                catch { /* Ignore */ }
                updateDialog.Close();
            };
            noButton.Click += (_, _) => updateDialog.Close();

            updateDialog.Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "A new version is available!", FontSize = 16, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                    new TextBlock { Text = $"Current version: {result.CurrentVersion}" },
                    new TextBlock { Text = $"Latest version: {result.LatestVersion}" },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0),
                        Children = { yesButton, noButton }
                    }
                }
            };

            await updateDialog.ShowDialog(window);
        }
        else
        {
            var upToDateDialog = new Window
            {
                Title = "No Updates",
                Width = 350,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(24),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "You're up to date!", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                        new TextBlock { Text = $"Version {result.CurrentVersion} is the latest version." }
                    }
                }
            };
            await upToDateDialog.ShowDialog(window);
        }
    }

    /// <summary>
    /// Opens the Settings dialog.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
    /// </remarks>
    [RelayCommand]
    private async Task SettingsAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new SettingsWindow();
        await dialog.ShowDialog(window);
        LoadShellSettings();
        StatusMessage = "Settings updated";
    }

    private IStorageService? _storage;

    /// <summary>
    /// Main window and file pickers. Replaceable so tests can supply paths without a desktop lifetime.
    /// </summary>
    internal IStorageService Storage
    {
        get => _storage ?? AvaloniaStorageService.Instance;
        set => _storage = value;
    }

    /// <summary>The main window that owns this view model's dialogs, or null when there is none.</summary>
    private Window? GetTopLevel() => Storage.MainWindow;

    [RelayCommand]
    private async Task HashCalculatorAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new HashCalculatorWindow();
        await dialog.ShowDialog(window);
        StatusMessage = "Hash calculator closed";
    }

    [RelayCommand]
    private async Task PackageStatsAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new PackageStatsWindow(_package);
        await dialog.ShowDialog(window);
    }

    /// <summary>
    /// Opens the Search dialog to find resources.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs
    /// </remarks>
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new SearchWindow(_package, FilteredResources);
        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.SelectedResult != null)
        {
            // Navigate to the selected resource
            SelectedResource = dialog.SelectedResult;
            StatusMessage = $"Found: {dialog.SelectedResult.TypeName}";
        }
    }

    /// <summary>
    /// Selects all resources in the filtered list.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 236-285 (SelectAll)
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1619-1622 (ResourceSelectAll)
    /// </remarks>
    [RelayCommand]
    private void SelectAll()
    {
        if (_package == null || FilteredResources.Count == 0) return;

        SelectedResources.Clear();
        foreach (var resource in FilteredResources)
        {
            SelectedResources.Add(resource);
        }

        // Set first as the "active" selection for preview purposes
        if (FilteredResources.Count > 0)
        {
            SelectedResource = FilteredResources[0];
        }

        StatusMessage = $"Selected {SelectedResources.Count} resources";
        OnPropertyChanged(nameof(SelectedResources));
    }

    #region Control Panel Support
    // Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs

    /// <summary>
    /// Shows the hex view for the selected resource.
    /// </summary>
    public void ShowHexView()
    {
        if (SelectedResource == null || _package == null) return;

        var key = SelectedResource.Key;
        var entry = ResolveEntry(SelectedResource);
        if (entry == null) return;

        try
        {
            var dataTask = _package.GetResourceDataAsync(entry);
            dataTask.AsTask().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        // Only swap the displayed control; the loaded editor/wrapper (and any
                        // uncommitted edits) stay current so Commit and Value view still work.
                        EditorContent = _viewFactory(CreateHexViewer(t.Result));
                        StatusMessage = "Hex view";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Shows the value/preview view for the selected resource.
    /// </summary>
    public async void ShowValueView()
    {
        if (SelectedResource == null) return;

        try
        {
            if (_currentEditorView != null && ReferenceEquals(_loadedResourceItem, SelectedResource))
            {
                // Re-show the existing editor so uncommitted edits are not thrown away.
                EditorContent = _currentEditorView;
            }
            else
            {
                await UpdateSelectedResourceDetailsAsync();
            }
            StatusMessage = "Preview view";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes the configured hex editor for the selected resource.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3176-3200 (EditOte hex editor)
    /// </remarks>
    public async Task ExecuteHexEditorAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var settings = SettingsService.Instance.Settings;
        if (string.IsNullOrEmpty(settings.HexEditorCommand))
        {
            StatusMessage = "No hex editor configured. Set it in Settings > External Programs.";
            return;
        }

        var key = SelectedResource.Key;
        var entry = ResolveEntry(SelectedResource);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        StatusMessage = "Opening hex editor...";

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var tempFile = _fileSystem.CombinePath(_fileSystem.GetTempPath(), $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}.bin");

            // Write to temp file
            await _fileSystem.WriteAllBytesAsync(tempFile, data.ToArray());
            var lastWriteTime = _fileSystem.GetLastWriteTimeUtc(tempFile);

            // Build command line
            var quote = settings.HexEditorWantsQuotes ? "\"" : "";
            var args = quote + tempFile + quote;

            // Execute editor
            using var process = new Process();
            process.StartInfo.FileName = settings.HexEditorCommand;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            await process.WaitForExitAsync();

            // Check for modifications
            var newWriteTime = _fileSystem.GetLastWriteTimeUtc(tempFile);
            if (settings.HexEditorIgnoreTimestamp || newWriteTime != lastWriteTime)
            {
                var modifiedData = await _fileSystem.ReadAllBytesAsync(tempFile);
                _package.ReplaceResource(entry, modifiedData);
                MarkPackageDirty();
                await UpdateSelectedResourceDetailsAsync();
                StatusMessage = "Resource updated from hex editor";
            }
            else
            {
                StatusMessage = "Hex editor closed (no changes)";
            }

            // Cleanup
            _fileSystem.DeleteFile(tempFile);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hex editor error: {ex.Message}";
        }
    }

    // OnSortOptionChanged / OnHexOnlyOptionChanged / OnUseNamesOptionChanged /
    // OnUseTagsOptionChanged / OnAutoModeChanged live in MainWindowViewModel.Shell.cs.

    #endregion

    #region Float and Save Preview
    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1148-1163, 2910-2936

    /// <summary>
    /// Indicates whether the current editor content can be floated.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1148-1151 (CanFloat)
    /// </remarks>
    public bool CanFloat => HasSelectedResource && EditorContent != null;

    /// <summary>
    /// Indicates whether the current preview can be saved.
    /// Save is available when the preview content is text-based.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1089-1105 (CanSavePreview)
    /// </remarks>
    public bool CanSavePreview => HasSelectedResource && (IsTextBasedPreview || HasImagePreview);

    /// <summary>
    /// Opens the current editor content in a floating window.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1153-1163 (EditFloat),
    ///         lines 2910-2936 (f_FloatControl)
    /// </remarks>
    [RelayCommand]
    private async Task FloatPreviewAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = ResolveEntry(SelectedResource);
        if (entry == null) return;

        try
        {
            var data = await _package.GetResourceDataAsync(entry);

            // Create a new editor control for the floating window
            var contentViewModel = CreateViewerSelection(key, data, ForceHexForSelection).EditorViewModel;

            var content = _viewFactory(contentViewModel);
            if (content == null) return;

            var window = new Views.Dialogs.FloatingPreviewWindow();
            window.SetContent(content, SelectedResource.InstanceName ?? SelectedResource.DisplayKey);

            // Get the main window to set as owner
            var mainWindow = GetTopLevel();

            if (mainWindow != null)
            {
                window.Show(mainWindow);
            }
            else
            {
                window.Show();
            }

            StatusMessage = "Preview opened in floating window";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening floating preview: {ex.Message}";
        }
    }

    /// <summary>
    /// Saves the current preview content to a file.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1107-1146 (EditSavePreview)
    /// </remarks>
    [RelayCommand]
    private async Task SavePreviewAsync()
    {
        if (SelectedResource == null || EditorContent == null) return;

        // Image previews are saved as PNG (see MainWindowViewModel.Shell.cs)
        if (HasImagePreview)
        {
            await SavePreviewImageAsync();
            return;
        }

        // Get preview text content
        var text = GetPreviewText();
        if (string.IsNullOrEmpty(text))
        {
            StatusMessage = "No text content to save";
            return;
        }

        // Build default filename from resource key
        var key = SelectedResource.Key;
        var baseName = $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}";
        if (!string.IsNullOrEmpty(SelectedResource.InstanceName))
        {
            baseName = SelectedResource.InstanceName;
        }

        var path = await Storage.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = "Save Preview Content",
            SuggestedFileName = baseName + ".txt",
            FileTypeChoices = [
                new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ],
            DefaultExtension = ".txt"
        });

        if (path != null)
        {
            await _fileSystem.WriteAllTextAsync(path, text);
            StatusMessage = $"Preview saved to {_fileSystem.GetFileName(path)}";
        }
    }

    /// <summary>
    /// Gets the text content from the current preview.
    /// </summary>
    private string? GetPreviewText() => GetPreviewTextFor(PreviewModel);

    #endregion

    /// <summary>
    /// Toggles the property grid panel visibility.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
    /// </remarks>
    [RelayCommand]
    private void TogglePropertyGrid()
    {
        ShowPropertyGrid = !ShowPropertyGrid;

        if (ShowPropertyGrid && _currentResourceWrapper != null)
        {
            UpdatePropertyGrid(_currentResourceWrapper);
        }
    }

    private void UpdatePropertyGrid(object? resource)
    {
        ClearPropertyGridViewModel();

        if (resource == null)
        {
            PropertyGridContent = null;
            return;
        }

        var vm = new PropertyGridViewModel();
        // A bridged legacy wrapper is edited through its own s4pi object, like the s4pe grid; its ResourceChanged
        // reaches the adapter's Changed, which the edit state is subscribed to, and Commit reads the adapter's Data.
        vm.LoadResource(resource is LegacyResourceAdapter legacy ? legacy.LegacyResource : resource);
        // Grid edits change the wrapper in place; they must be committed like editor edits.
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3008-3029 (controlPanel1_GridClick)
        vm.PropertyValueChanged += _resourceEditedHandler;
        _propertyGridViewModel = vm;
        PropertyGridContent = _viewFactory(vm);
    }

    private void ClearPropertyGridViewModel()
    {
        if (_propertyGridViewModel == null) return;
        _propertyGridViewModel.PropertyValueChanged -= _resourceEditedHandler;
        _propertyGridViewModel.Dispose();
        _propertyGridViewModel = null;
    }

    /// <summary>
    /// The property grid view model for the current resource, if the grid is shown.
    /// </summary>
    internal PropertyGridViewModel? CurrentPropertyGrid => _propertyGridViewModel;

    /// <summary>
    /// The editor/viewer view model for the current resource.
    /// </summary>
    internal object? CurrentEditorViewModel => _currentEditorViewModel;

    [RelayCommand]
    private async Task OpenRecentFileAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (_fileSystem.FileExists(path))
        {
            await LoadPackageAsync(path);
        }
        else
        {
            StatusMessage = $"File not found: {path}";
            // Remove from recent files
            var item = RecentFiles.FirstOrDefault(r => r.Path == path);
            if (item != null)
            {
                RecentFiles.Remove(item);
                await SaveRecentFilesAsync();
            }
        }
    }

    private async Task LoadRecentFilesAsync()
    {
        RecentFiles.Clear();

        try
        {
            if (_fileSystem.FileExists(RecentFilesPath))
            {
                var json = await _fileSystem.ReadAllTextAsync(RecentFilesPath);
                var paths = JsonSerializer.Deserialize<string[]>(json) ?? [];

                foreach (var path in paths.Take(MaxRecentFiles))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        RecentFiles.Add(new RecentFileViewModel(path));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to load recent files: {ex.Message}");
        }
    }

    private async Task SaveRecentFilesAsync()
    {
        try
        {
            var dir = _fileSystem.GetDirectoryName(RecentFilesPath);
            if (!string.IsNullOrEmpty(dir))
            {
                _fileSystem.CreateDirectory(dir);
            }

            var paths = RecentFiles.Select(r => r.Path).ToArray();
            var json = JsonSerializer.Serialize(paths);
            await _fileSystem.WriteAllTextAsync(RecentFilesPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to save recent files: {ex.Message}");
        }
    }

    private async Task AddToRecentFilesAsync(string path)
    {
        // Remove if already exists
        var existing = RecentFiles.FirstOrDefault(r => r.Path == path);
        if (existing != null)
        {
            RecentFiles.Remove(existing);
        }

        // Add to front
        RecentFiles.Insert(0, new RecentFileViewModel(path));

        // Trim to max
        while (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        await SaveRecentFilesAsync();
    }

    #region Bookmarks
    // Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs

    /// <summary>
    /// Loads bookmarks from settings.
    /// </summary>
    private void LoadBookmarks()
    {
        Bookmarks.Clear();
        var settings = SettingsService.Instance.Settings;

        foreach (var bookmark in settings.Bookmarks)
        {
            if (!string.IsNullOrEmpty(bookmark))
            {
                Bookmarks.Add(new BookmarkItemViewModel(bookmark));
            }
        }

        OnPropertyChanged(nameof(HasBookmarks));
    }

    /// <summary>
    /// Opens a bookmarked package.
    /// </summary>
    [RelayCommand]
    private async Task OpenBookmarkAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Parse bookmark format: "0:path" for read-only, "1:path" for read-write
        var isReadOnly = false;
        var actualPath = path;
        if (path.Length > 2 && path[1] == ':' && (path[0] == '0' || path[0] == '1'))
        {
            isReadOnly = path[0] == '0';
            actualPath = path[2..];
        }

        if (!_fileSystem.FileExists(actualPath))
        {
            StatusMessage = $"Bookmark not found: {actualPath}";
            return;
        }

        await LoadPackageAsync(actualPath, isReadOnly);
        if (isReadOnly)
        {
            StatusMessage = $"Opened (read-only): {_fileSystem.GetFileName(actualPath)}";
        }
    }

    /// <summary>
    /// Opens the Organise Bookmarks dialog.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
    /// </remarks>
    [RelayCommand]
    private async Task OrganiseBookmarksAsync()
    {
        if (GetTopLevel() is not { } window) return;

        var dialog = new OrganiseBookmarksWindow();
        await dialog.ShowDialog(window);

        // Reload bookmarks after dialog closes
        LoadBookmarks();
    }

    /// <summary>
    /// Opens the Custom Places dialog.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs
    /// </remarks>
    [RelayCommand]
    private async Task CustomPlacesAsync()
    {
        if (GetTopLevel() is not { } window) return;

        var dialog = new OrganiseCustomPlacesWindow();
        await dialog.ShowDialog(window);
        StatusMessage = "Custom places updated";
    }

    /// <summary>
    /// Adds the current package to bookmarks.
    /// </summary>
    [RelayCommand]
    private async Task AddCurrentToBookmarksAsync()
    {
        if (string.IsNullOrEmpty(PackagePath)) return;

        var settings = SettingsService.Instance.Settings;
        if (settings.Bookmarks.Count >= settings.MaxBookmarks)
        {
            StatusMessage = $"Cannot add bookmark: maximum of {settings.MaxBookmarks} bookmarks reached";
            return;
        }

        var bookmarkEntry = "1:" + PackagePath; // 1 = read-write
        if (settings.Bookmarks.Contains(bookmarkEntry) || settings.Bookmarks.Contains("0:" + PackagePath))
        {
            StatusMessage = "Package is already bookmarked";
            return;
        }

        settings.Bookmarks.Add(bookmarkEntry);
        await SettingsService.Instance.SaveAsync();
        LoadBookmarks();
        StatusMessage = $"Added bookmark: {_fileSystem.GetFileName(PackagePath)}";
    }

    /// <summary>
    /// Sets the maximum number of recent files to keep.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 940-952 (FileSetMaxRecent)
    /// </remarks>
    [RelayCommand]
    private async Task SetMaxRecentFilesAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var settings = SettingsService.Instance.Settings;
        var dialog = new GetNumberWindow(
            "Max number of recent files:",
            "Recent Files",
            1, 20, settings.MaxRecentFiles);

        var result = await dialog.ShowDialog<bool>(window);
        if (result)
        {
            settings.MaxRecentFiles = dialog.Value;
            await SettingsService.Instance.SaveAsync();

            // Trim recent files if needed
            while (RecentFiles.Count > settings.MaxRecentFiles)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
            await SaveRecentFilesAsync();

            StatusMessage = $"Max recent files set to {settings.MaxRecentFiles}";
        }
    }

    /// <summary>
    /// Sets the maximum number of bookmarks to keep.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 965-978 (FileSetMaxBookmarks)
    /// </remarks>
    [RelayCommand]
    private async Task SetMaxBookmarksAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var settings = SettingsService.Instance.Settings;
        var dialog = new GetNumberWindow(
            "Max number of bookmarks:",
            "Bookmarks",
            1, 20, settings.MaxBookmarks);

        var result = await dialog.ShowDialog<bool>(window);
        if (result)
        {
            settings.MaxBookmarks = dialog.Value;
            await SettingsService.Instance.SaveAsync();

            // Trim bookmarks if needed
            while (settings.Bookmarks.Count > settings.MaxBookmarks)
            {
                settings.Bookmarks.RemoveAt(settings.Bookmarks.Count - 1);
            }
            await SettingsService.Instance.SaveAsync();
            LoadBookmarks();

            StatusMessage = $"Max bookmarks set to {settings.MaxBookmarks}";
        }
    }

    #endregion

    /// <summary>
    /// Executes an async task with error handling, updating StatusMessage on failure.
    /// </summary>
    private async Task RunGuardedAsync(Func<Task> asyncAction, string errorPrefix = "Error")
    {
        try
        {
            await asyncAction().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{errorPrefix}: {ex}");
            StatusMessage = $"{errorPrefix}: {ex.Message}";
        }
    }

    /// <summary>
    /// Disposes the package when the window closes without explicit ClosePackage.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        PropertyChanged -= _propertyChangedHandler;
        DisposeResourceOps();
        DisposeShell();
        DisposeViewers();
        await CloseCurrentPackageAsync().ConfigureAwait(false);
        ClearResourceEditState();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// View model for a recent file entry.
/// </summary>
public sealed class RecentFileViewModel
{
    public string Path { get; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? "";

    public RecentFileViewModel(string path)
    {
        Path = path;
    }
}

/// <summary>
/// View model for a helper menu item.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3095-3134 (helper buttons)
/// </remarks>
public sealed class HelperMenuItemViewModel
{
    private readonly HelperInstance _helper;

    public HelperMenuItemViewModel(HelperInstance helper, Func<HelperInstance, Task> executeAction)
    {
        ArgumentNullException.ThrowIfNull(executeAction);
        _helper = helper;
        // Created once: a command built per property read would give the bound MenuItem a fresh
        // instance on every binding refresh, losing CanExecute/IsRunning state between reads.
        ExecuteCommand = new AsyncRelayCommand(() => executeAction(_helper));
    }

    public string Label => _helper.Label;
    public string Description => _helper.Description;
    public string Id => _helper.Id;

    public IAsyncRelayCommand ExecuteCommand { get; }
}

/// <summary>
/// View model for a bookmark menu item.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
/// Format: "0:path" for read-only, "1:path" for read-write
/// </remarks>
public sealed class BookmarkItemViewModel
{
    public BookmarkItemViewModel(string bookmarkEntry)
    {
        RawEntry = bookmarkEntry;

        // Parse format: "0:path" for read-only, "1:path" for read-write
        if (bookmarkEntry.Length > 2 && bookmarkEntry[1] == ':')
        {
            IsReadOnly = bookmarkEntry[0] == '0';
            FilePath = bookmarkEntry[2..];
        }
        else
        {
            IsReadOnly = false;
            FilePath = bookmarkEntry;
        }

        FileName = Path.GetFileName(FilePath);
    }

    public string RawEntry { get; }
    public string FilePath { get; }
    public string FileName { get; }
    public bool IsReadOnly { get; }

    public string DisplayName => IsReadOnly ? $"[RO] {FileName}" : FileName;
    public string ToolTip => FilePath;
}
