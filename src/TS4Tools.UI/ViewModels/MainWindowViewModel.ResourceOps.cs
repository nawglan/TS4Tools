using CommunityToolkit.Mvvm.Input;
using TS4Tools;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Resource menu operations: add, copy/paste, duplicate, replace, compressed/deleted toggles,
/// details, import (file, batch, package, paste, drag-and-drop, replace from) and export.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1280-2110 (Resource menu) and
/// legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 104-826.
/// </remarks>
public partial class MainWindowViewModel
{
    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 66-69 (nmapList)
    private const uint NameMapResourceType = 0x0166038C;

    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs line 1562 (target = 0x5A42)
    private const ushort ZlibCompression = 0x5A42;

    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 45-51 (packageExtensions)
    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 53-86 (xmlList, stblList, nmapList,
    // deleteList): resource types the DBC import allows duplicates of, or removes afterwards.
    private static readonly uint[] DbcXmlTypes = [0x025C95B6, 0x025ED6F4, 0x03B33DDF, 0x044AE110, 0x0604ABDA];
    private const uint StblResourceType = 0x220557DA;
    private const uint Sims3PackManifestType = 0x73E93EEB;
    private const uint DdsAsDstTypeId = 0x00B2D882;
    private const uint ThumbnailTypeId = 0x3C2A8647;

    private static readonly string[] ImportPackageExtensions = ["package", "world", "dbc", "nhd"];

    /// <summary>Whether to save the package after each imported package.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 88-93 (AutoSaveState)</remarks>
    internal enum AutoSaveState
    {
        Never,
        Ask,
        Always
    }

    private IResourceOpsDialogService? _resourceOpsDialogs;
    private List<CopiedResource> _copiedResources = [];
    private NotifyCollectionChangedEventHandler? _selectionCollectionHandler;
    private PropertyChangedEventHandler? _selectionPropertyHandler;

    /// <summary>
    /// Dialogs, pickers and clipboard used by the resource operations. Settable so tests can script them.
    /// </summary>
    internal IResourceOpsDialogService ResourceOpsDialogs
    {
        get => _resourceOpsDialogs ?? ResourceOpsDialogService.Instance;
        set => _resourceOpsDialogs = value;
    }

    /// <summary>Whether resources have been copied in this session.</summary>
    public bool HasClipboardResource => _copiedResources.Count > 0;

    /// <summary>
    /// Compressed state of the selection: true if all selected are compressed, false if none
    /// (or nothing is selected), null if mixed.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1338-1363 (CompressedCheckState)</remarks>
    public bool? SelectionCompressedState => GetCheckState(i => i.IsCompressed);

    /// <summary>
    /// Deleted state of the selection: true if all selected are marked deleted, false if none, null if mixed.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1365-1390 (IsDeletedCheckState)</remarks>
    public bool? SelectionDeletedState => GetCheckState(i => i.IsDeleted);

    /// <summary>Check mark of the Compressed menu item.</summary>
    public bool IsSelectionCompressed => SelectionCompressedState == true;

    /// <summary>Check mark of the Deleted menu item.</summary>
    public bool IsSelectionDeleted => SelectionDeletedState == true;

    /// <summary>Whether more than one resource is selected.</summary>
    public bool HasMultipleSelection => SelectedResources.Count > 1;

    #region Selection and list helpers

    private void InitializeResourceOps()
    {
        _selectionCollectionHandler = (_, _) => OnSelectionStateChanged();
        SelectedResources.CollectionChanged += _selectionCollectionHandler;
        _selectionPropertyHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedResource))
            {
                OnSelectionStateChanged();
            }
        };
        PropertyChanged += _selectionPropertyHandler;
    }

    private void DisposeResourceOps()
    {
        if (_selectionCollectionHandler != null)
        {
            SelectedResources.CollectionChanged -= _selectionCollectionHandler;
            _selectionCollectionHandler = null;
        }

        if (_selectionPropertyHandler != null)
        {
            PropertyChanged -= _selectionPropertyHandler;
            _selectionPropertyHandler = null;
        }
    }

    private void OnSelectionStateChanged()
    {
        OnPropertyChanged(nameof(SelectionCompressedState));
        OnPropertyChanged(nameof(SelectionDeletedState));
        OnPropertyChanged(nameof(IsSelectionCompressed));
        OnPropertyChanged(nameof(IsSelectionDeleted));
        OnPropertyChanged(nameof(HasMultipleSelection));
    }

    /// <summary>
    /// The selected rows: the multi-selection, or the single selected resource.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs (SelectedResources)</remarks>
    internal IReadOnlyList<ResourceItemViewModel> GetSelectedItems()
    {
        if (SelectedResources.Count > 0) return [.. SelectedResources];
        return SelectedResource != null ? [SelectedResource] : [];
    }

    private bool? GetCheckState(Func<ResourceItemViewModel, bool> predicate)
    {
        var items = GetSelectedItems();
        if (items.Count == 0) return false;
        var count = items.Count(predicate);
        if (count == 0) return false;
        if (count == items.Count) return true;
        return null;
    }

    /// <summary>
    /// The index entry of a row. Rows normally carry their entry; rows created without one fall back
    /// to a key lookup.
    /// </summary>
    private IResourceIndexEntry? ResolveEntry(ResourceItemViewModel item) =>
        item.Entry ?? _package?.Find(item.Key);

    private bool EnsureWritable()
    {
        if (_package is { IsReadOnly: false }) return true;
        StatusMessage = "Package is read-only";
        return false;
    }

    /// <summary>
    /// Adds a row for a new index entry.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs Add(rie, select)</remarks>
    private ResourceItemViewModel AddResourceItem(IResourceIndexEntry entry, bool select, bool refreshList = true)
    {
        var item = new ResourceItemViewModel(entry, _hashNameService.TryGetName(entry.Key.Instance));
        Resources.Add(item);
        if (refreshList || select)
        {
            ApplyFilter();
        }
        OnPropertyChanged(nameof(ResourceCount));
        if (select)
        {
            SelectedResource = item;
        }
        return item;
    }

    private void RefreshItemsFor(IResourceIndexEntry entry)
    {
        foreach (var item in Resources)
        {
            if (ReferenceEquals(item.Entry, entry))
            {
                item.Refresh();
            }
        }
        OnSelectionStateChanged();
    }

    /// <summary>
    /// After a save the package re-reads its index: deleted entries are gone and every entry object is
    /// new. Rows are re-pointed at the new entries (keeping selection and the loaded editor), and rows
    /// for deleted entries are removed.
    /// </summary>
    private void ResyncResourceListAfterSave()
    {
        if (_package == null) return;

        var live = Resources.Where(r => !r.IsDeleted).ToList();
        var entries = _package.Resources;
        var matches = live.Count == entries.Count;
        for (var i = 0; matches && i < live.Count; i++)
        {
            matches = live[i].Key == entries[i].Key;
        }

        if (!matches)
        {
            // Unexpected ordering: rebuild the list from the package.
            ClearResourceEditState();
            SetSelectedResourceSilently(null);
            SelectedResources.Clear();
            PopulateResourceList();
            SelectedResourceTypeName = string.Empty;
            SelectedResourceKey = string.Empty;
            SelectedResourceInfo = string.Empty;
            EditorContent = null;
            OnPropertyChanged(nameof(ResourceCount));
            OnSelectionStateChanged();
            return;
        }

        var removed = Resources.Where(r => r.IsDeleted).ToList();
        foreach (var item in removed)
        {
            if (ReferenceEquals(_loadedResourceItem, item))
            {
                ClearResourceEditState();
            }
            if (ReferenceEquals(SelectedResource, item))
            {
                SetSelectedResourceSilently(null);
            }
            SelectedResources.Remove(item);
            Resources.Remove(item);
            FilteredResources.Remove(item);
        }

        for (var i = 0; i < live.Count; i++)
        {
            live[i].Rebind(entries[i]);
        }

        OnPropertyChanged(nameof(ResourceCount));
        OnSelectionStateChanged();
    }

    /// <summary>
    /// Adds a resource, honouring the duplicate handling and compression choice.
    /// </summary>
    /// <returns>The new entry, or null if a duplicate was rejected.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1710-1735 (NewResource). A replaced
    /// duplicate is only marked deleted (Package.DeleteResource), so it stays listed until saved.
    /// </remarks>
    internal IResourceIndexEntry? NewResource(ResourceKey key, ReadOnlyMemory<byte> data, ImportDuplicateHandling dups, bool compress)
    {
        if (_package == null) return null;

        var existing = _package.Find(key);
        if (existing != null)
        {
            if (dups == ImportDuplicateHandling.Reject)
            {
                return null;
            }

            if (dups == ImportDuplicateHandling.Replace)
            {
                _package.DeleteResource(existing);
                RefreshItemsFor(existing);
            }
        }

        var entry = _package.AddResource(key, data, rejectDuplicates: false);
        if (entry == null) return null;

        // Source: MainForm.cs line 1730 (rie.Compressed = compress ? 0x5A42 : 0)
        _package.SetCompression(entry, compress ? ZlibCompression : (ushort)0);
        SetPackageDirty(true);
        return entry;
    }

    /// <summary>
    /// Adds names to the package's name map (creating one if needed) and refreshes displayed names.
    /// </summary>
    /// <param name="names">Instance/name pairs.</param>
    /// <param name="create">Create a name map if the package has none.</param>
    /// <param name="replace">Replace existing names for the same instance.</param>
    /// <returns>False if the names could not be written.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 176-225
    /// (MergeNamemap / ResourceName: the first name map is updated; a new one is 0x0166038C-0-0).
    /// </remarks>
    internal async Task<bool> MergeNameMapAsync(IEnumerable<KeyValuePair<ulong, string>> names, bool create, bool replace)
    {
        if (_package == null || _package.IsReadOnly) return false;

        var entry = _package.Find(e => e.Key.ResourceType == NameMapResourceType);
        if (entry == null)
        {
            if (!create) return false;
            entry = _package.AddResource(new ResourceKey(NameMapResourceType, 0, 0), rejectDuplicates: false);
            if (entry == null) return false;
            AddResourceItem(entry, select: false);
        }

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var nameMap = new NameMapResource(entry.Key, data);
            foreach (var (instance, name) in names)
            {
                if (nameMap.ContainsKey(instance))
                {
                    if (replace) nameMap[instance] = name;
                }
                else
                {
                    nameMap.Add(instance, name);
                }
            }

            _package.ReplaceResource(entry, nameMap.Data);
        }
        catch (Exception ex) when (ex is InvalidDataException or ResourceFormatException or ArgumentException or InvalidOperationException)
        {
            // Source: BrowserWidget.cs lines 212-219 ("Resource names cannot be added...")
            StatusMessage = $"Resource names cannot be added: {ex.Message}";
            return false;
        }

        SetPackageDirty(true);
        RefreshItemsFor(entry);
        await ReloadResourceNamesAsync();

        // Source: BrowserWidget.cs lines 209-210 (reload the name map if it is the selected resource)
        if (_loadedResourceItem != null && ReferenceEquals(_loadedResourceItem.Entry, entry))
        {
            await UpdateSelectedResourceDetailsAsync();
        }

        return true;
    }

    private async Task ReloadResourceNamesAsync()
    {
        if (_package == null) return;
        _hashNameService.Clear();
        await _hashNameService.LoadFromPackageAsync(_package);
        foreach (var item in Resources)
        {
            item.InstanceName = _hashNameService.TryGetName(item.Key.Instance);
        }
    }

    private static bool IsPackageFile(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.');
        return ImportPackageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Add, duplicate, replace, toggles, details

    /// <summary>
    /// Adds a new, empty resource whose key, name and compression are chosen in the Resource Details dialog.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1454-1482 (ResourceAdd)</remarks>
    [RelayCommand]
    private async Task AddResourceAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        var details = new ResourceDetailsViewModel(useName: true, displayFilename: false);
        try
        {
            if (!await ResourceOpsDialogs.ShowResourceDetailsAsync(details) || details.Key is not { } key) return;

            var entry = NewResource(key, ReadOnlyMemory<byte>.Empty, details.Settings.DuplicateHandling, details.Settings.Compress);
            if (entry == null)
            {
                StatusMessage = "A resource with that key already exists";
                return;
            }

            // Source: MainForm.cs line 1475 ("Ensure there's an actual resource in the package": the new
            // entry gets the data of a newly created wrapper for its type)
            try
            {
                var resource = _package.ResourceHandlers.CreateNewResource(key);
                _package.ReplaceResource(entry, resource);
            }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ResourceFormatException)
            {
                // Keep the entry empty if the wrapper cannot create a new resource.
                Debug.WriteLine($"CreateNewResource failed for {key}: {ex.Message}");
            }

            var item = AddResourceItem(entry, select: true);

            if (details.Settings.UseName && !string.IsNullOrEmpty(details.ResourceName))
            {
                await MergeNameMapAsync([new(key.Instance, details.ResourceName)], create: true, replace: details.Settings.AllowRename);
            }

            StatusMessage = $"Added resource: {item.TypeName}";
        }
        finally
        {
            details.Detach();
        }
    }

    /// <summary>
    /// Duplicates the selected resources (same key, same data and compression).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1540-1558 (ResourceDuplicate). Legacy
    /// duplicates the single selected resource; each selected resource is duplicated here.
    /// </remarks>
    [RelayCommand]
    private async Task DuplicateResourceAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;

        ResourceItemViewModel? last = null;
        foreach (var source in items)
        {
            var entry = ResolveEntry(source);
            if (entry == null) continue;

            var data = await _package.GetResourceDataAsync(entry);
            var newEntry = _package.AddResource(entry.Key, data, rejectDuplicates: false);
            if (newEntry == null) continue;

            // Source: MainForm.cs line 1550 (rie.Compressed = SelectedResource.Compressed)
            _package.SetCompression(newEntry, entry.CompressionType);
            last = AddResourceItem(newEntry, select: false, refreshList: false);
        }

        if (last == null) return;

        SetPackageDirty(true);
        ApplyFilter();
        if (items.Count == 1)
        {
            SelectedResource = last;
        }
        StatusMessage = items.Count == 1 ? $"Duplicated resource: {last.TypeName}" : $"Duplicated {items.Count} resources";
    }

    /// <summary>
    /// Replaces the selected resource's data with the contents of a file.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1634-1684 (ResourceReplace: uncommitted
    /// edits are dropped and the resource is reloaded).
    /// </remarks>
    [RelayCommand]
    private async Task ReplaceResourceAsync()
    {
        if (_package == null || SelectedResource == null || !EnsureWritable()) return;

        var item = SelectedResource;
        var entry = ResolveEntry(item);
        if (entry == null) return;

        var path = await ResourceOpsDialogs.PickReplaceFileAsync("Replace Resource", entry.Key.ResourceType);
        if (string.IsNullOrEmpty(path)) return;

        byte[] data;
        try
        {
            data = await _fileSystem.ReadAllBytesAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ResourceOpsDialogs.ShowMessageAsync($"Could not open file:\n{path}.\nNo changes made.\n\n{ex.Message}", "Replace Resource");
            return;
        }

        _package.ReplaceResource(entry, data);
        item.FileSize = entry.MemorySize;
        IsResourceDirty = false;
        SetPackageDirty(true);

        if (ReferenceEquals(_loadedResourceItem, item) || ReferenceEquals(SelectedResource, item))
        {
            await UpdateSelectedResourceDetailsAsync();
        }

        StatusMessage = $"Replaced with {data.Length:N0} bytes";
    }

    /// <summary>
    /// Toggles the Compressed flag of the selection: all compressed become uncompressed, otherwise all
    /// become compressed (0x5A42).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1560-1572 (ResourceCompressed). Legacy
    /// computes IsPackageDirty = !isPackageDirty || changed, which can clear an already-dirty flag; the
    /// package is simply marked dirty here so unsaved changes are never forgotten.
    /// </remarks>
    [RelayCommand]
    private void ToggleCompressed()
    {
        if (_package == null || !EnsureWritable()) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;

        var target = SelectionCompressedState == true ? (ushort)0 : ZlibCompression;
        foreach (var item in items)
        {
            var entry = ResolveEntry(item);
            if (entry == null) continue;
            _package.SetCompression(entry, target);
            item.Refresh();
        }

        SetPackageDirty(true);
        OnSelectionStateChanged();
        StatusMessage = target == 0 ? "Marked uncompressed" : "Marked compressed";
    }

    /// <summary>
    /// Toggles the Deleted flag of the selection. Deleted resources stay listed (and can be undeleted)
    /// until the package is saved, when they are dropped.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1574-1582 (ResourceIsDeleted), line 2745
    /// (the Delete key calls it) and legacy_references/Sims4Tools/s4pi/Package/Package.cs line 117 (deleted
    /// entries are skipped on save).
    /// </remarks>
    [RelayCommand]
    private void ToggleDeleted()
    {
        if (_package == null || !EnsureWritable()) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;

        var target = SelectionDeletedState != true;
        foreach (var item in items)
        {
            if (ResolveEntry(item) is not IMutableResourceIndexEntry entry) continue;
            entry.IsDeleted = target;
            item.Refresh();
        }

        SetPackageDirty(true);
        OnSelectionStateChanged();
        StatusMessage = target
            ? $"Marked {items.Count} resource(s) deleted"
            : $"Restored {items.Count} resource(s)";
    }

    /// <summary>
    /// Shows the Resource Details dialog for the selected resource and applies key, compression and name.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1584-1617 (ResourceDetails)</remarks>
    [RelayCommand]
    private async Task ResourceDetailsAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var item = SelectedResource;
        if (ResolveEntry(item) is not { } entry) return;

        var name = item.InstanceName;
        var details = new ResourceDetailsViewModel(!string.IsNullOrEmpty(name), displayFilename: false, entry.Key);
        details.Settings.Compress = entry.CompressionType != 0;
        if (details.Settings.UseName)
        {
            details.ResourceName = name ?? string.Empty;
        }

        try
        {
            if (!await ResourceOpsDialogs.ShowResourceDetailsAsync(details) || details.Key is not { } key) return;
            if (!EnsureWritable()) return;

            // Source: MainForm.cs lines 1604-1605 (browserWidget1.ResourceKey = ir; Compressed = ...)
            if (entry is IMutableResourceIndexEntry mutable && mutable.Key != key)
            {
                mutable.Key = key;
            }
            _package.SetCompression(entry, details.Settings.Compress ? ZlibCompression : (ushort)0);

            item.InstanceName = _hashNameService.TryGetName(key.Instance);
            item.Refresh();

            if (details.Settings.UseName && !string.IsNullOrEmpty(details.ResourceName))
            {
                await MergeNameMapAsync([new(key.Instance, details.ResourceName)], create: true, replace: details.Settings.AllowRename);
            }

            SetPackageDirty(true);
            OnSelectionStateChanged();

            if (ReferenceEquals(SelectedResource, item))
            {
                SelectedResourceTypeName = item.TypeName;
                SelectedResourceKey = item.DisplayKey;
            }

            StatusMessage = "Resource details updated";
        }
        finally
        {
            details.Detach();
        }
    }

    #endregion

    #region Copy and paste

    /// <summary>
    /// Copies the selected resource's key to the clipboard ("0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII").
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1624-1632 (ResourceCopyRk)</remarks>
    [RelayCommand]
    private async Task CopyResourceKeyAsync()
    {
        var items = GetSelectedItems();
        if (items.Count != 1) return;

        var keyString = ResourceDetailsViewModel.FormatKey(items[0].Key);
        await ResourceOpsDialogs.SetClipboardTextAsync(keyString);
        StatusMessage = $"Copied: {keyString}";
    }

    /// <summary>
    /// Copies the selected resources (key, name and data) to the clipboard.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1484-1538 (ResourceCopy). The resources
    /// are kept in-process and put on the system clipboard in a TS4Tools format, with their keys as text.
    /// </remarks>
    [RelayCommand]
    private async Task CopyResourceAsync()
    {
        if (_package == null) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;

        try
        {
            var copied = new List<CopiedResource>(items.Count);
            foreach (var item in items)
            {
                var entry = ResolveEntry(item);
                if (entry == null) continue;
                var data = await _package.GetResourceDataAsync(entry);
                copied.Add(new CopiedResource(Tgin.From(entry.Key, item.InstanceName), data.ToArray()));
            }

            if (copied.Count == 0) return;

            _copiedResources = copied;
            OnPropertyChanged(nameof(HasClipboardResource));

            var keys = string.Join("\r\n", copied.Select(c => ResourceDetailsViewModel.FormatKey(c.Tgin.Key)));
            await ResourceOpsDialogs.SetClipboardResourcesAsync(ResourceClipboardFormat.Serialize(copied), keys);

            StatusMessage = copied.Count == 1 ? $"Copied resource: {items[0].TypeName}" : $"Copied {copied.Count} resources";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or PackageFormatException)
        {
            StatusMessage = $"Copy error: {ex.Message}";
        }
    }

    /// <summary>
    /// Pastes copied resources (via Resource Details for one, Import Batch for several), or files on the
    /// clipboard.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 512-559 (ResourcePaste)</remarks>
    [RelayCommand]
    private async Task PasteResourceAsync()
    {
        if (_package == null) return;

        IReadOnlyList<CopiedResource>? resources =
            ResourceClipboardFormat.TryDeserialize(await ResourceOpsDialogs.GetClipboardResourcesAsync());

        if (resources == null || resources.Count == 0)
        {
            var files = await ResourceOpsDialogs.GetClipboardFilesAsync();
            if (files.Count == 1)
            {
                await ImportSingleFileAsync(files[0], "Resource->Paste");
                return;
            }

            if (files.Count > 1)
            {
                await ImportBatchFilesAsync(files, "Resource->Paste");
                return;
            }

            // The system clipboard may be unavailable (or hold no resources): use this session's copy.
            resources = _copiedResources;
        }

        if (resources.Count == 1)
        {
            await ImportSingleAsync(resources[0]);
        }
        else if (resources.Count > 1)
        {
            await ImportBatchAsync(resources);
        }
        else
        {
            StatusMessage = "Nothing to paste";
        }
    }

    /// <summary>
    /// Imports one copied resource through the Resource Details dialog.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 621-637 (ImportSingle(MyDataFormat))</remarks>
    private async Task ImportSingleAsync(CopiedResource data)
    {
        if (!EnsureWritable()) return;

        var details = new ResourceDetailsViewModel(useName: true, displayFilename: true) { Filename = data.Tgin.ToFileName() };
        try
        {
            if (!await ResourceOpsDialogs.ShowResourceDetailsAsync(details) || !details.IsValid) return;

            var s = details.Settings;
            await ImportStreamAsync(data.Data, details.Tgin, s.UseName, s.AllowRename, s.Compress, s.DuplicateHandling, select: true);
        }
        finally
        {
            details.Detach();
        }
    }

    /// <summary>
    /// Imports several copied resources through the Import Batch dialog.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 711-761 (ImportBatch(IList&lt;MyDataFormat&gt;))</remarks>
    private async Task ImportBatchAsync(IReadOnlyList<CopiedResource> ldata)
    {
        if (!EnsureWritable()) return;

        var batch = new ImportBatchViewModel(ldata.Select(d => d.Tgin.ToFileName()), ImportBatchMode.Paste, "Resource->Paste");
        if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

        var output = new List<CopiedResource>();
        foreach (var name in batch.Items)
        {
            var match = ldata.FirstOrDefault(d => d.Tgin.ToFileName() == name);
            if (match != null)
            {
                output.Add(match);
            }
        }

        if (output.Count == 0) return;
        if (output.Count == 1)
        {
            await ImportSingleAsync(output[0]);
            return;
        }

        var s = batch.Settings;
        var added = 0;
        foreach (var data in output)
        {
            if (await ImportStreamAsync(data.Data, data.Tgin, s.UseName, s.AllowRename, s.Compress, s.DuplicateHandling, select: false))
            {
                added++;
            }
        }

        ApplyFilter();
        StatusMessage = $"Pasted {added} resource(s)";
    }

    /// <summary>
    /// Adds one resource from data (and its name, if requested).
    /// </summary>
    /// <returns>True if the resource was added.</returns>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 805-825 (ImportStream)</remarks>
    private async Task<bool> ImportStreamAsync(byte[] data, Tgin tgin, bool useName, bool rename, bool compress,
        ImportDuplicateHandling dups, bool select)
    {
        if (useName && !string.IsNullOrEmpty(tgin.ResourceName))
        {
            await MergeNameMapAsync([new(tgin.Instance, tgin.ResourceName)], create: true, replace: rename);
        }

        var entry = NewResource(tgin.Key, data, dups, compress);
        if (entry == null) return false;
        AddResourceItem(entry, select, refreshList: select);
        return true;
    }

    #endregion

    #region Import

    /// <summary>
    /// Imports resource files chosen by the user (several files go through Import Batch).
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 104-131 (ResourceImport)</remarks>
    [RelayCommand]
    private async Task ImportResourceAsync()
    {
        const string title = "Import Resources";
        var files = await ResourceOpsDialogs.PickImportFilesAsync(title, allowMultiple: true);
        if (files.Count == 0) return;

        if (files.Count > 1)
        {
            await ImportBatchFilesAsync(files, title);
        }
        else
        {
            await ImportSingleFileAsync(files[0], title);
        }
    }

    /// <summary>
    /// Opens the Import Batch dialog with no files, so files and folders can be added to it.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 654-709 (ImportBatch(string[]))</remarks>
    [RelayCommand]
    private Task BatchImportAsync() => ImportBatchFilesAsync([], "Batch Import");

    /// <summary>
    /// Imports all resources of packages chosen by the user.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 133-162 (ResourceImportPackages)</remarks>
    [RelayCommand]
    private Task ImportFromPackageAsync() => ImportFromPackagesAsync("Import Packages");

    /// <summary>
    /// Merges the resources of other packages into the current one. As in legacy, this is the package
    /// import: the Import Batch dialog supplies the duplicate handling, compression and name options.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 133-162 (ResourceImportPackages)</remarks>
    [RelayCommand]
    private Task MergePackagesAsync() => ImportFromPackagesAsync("Merge Packages");

    internal async Task ImportFromPackagesAsync(string title)
    {
        if (_package == null || !EnsureWritable()) return;

        var files = await ResourceOpsDialogs.PickPackagesAsync(title);
        if (files.Count == 0) return;

        var batch = new ImportBatchViewModel(files, ImportBatchMode.Package, title);
        if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

        var s = batch.Settings;
        await ImportPackagesCommonAsync([.. batch.Items], title, s.DuplicateHandling, s.Compress, s.UseName, s.AllowRename);
    }

    /// <summary>
    /// Imports DBC (or package) files as a database cache: duplicate XML, STBL and name map resources are
    /// kept and then merged, resources of the Sims3Pack manifest type are dropped.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 197-290 (ResourceImportAsDbc) and
    /// Import/ExperimentalDBCWarning.cs (the warning that must be confirmed first).
    /// </remarks>
    [RelayCommand]
    private async Task ImportDbcAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        if (!await ResourceOpsDialogs.ConfirmDbcWarningAsync()) return;

        const string title = "Import DBC";
        var files = await ResourceOpsDialogs.PickPackagesAsync(title);
        if (files.Count == 0) return;

        // Source: Import.cs lines 214-217 (AskDBCAutoSave: Ask, otherwise always save)
        var autoSave = SettingsService.Instance.Settings.PromptDbcAutosave ? AutoSaveState.Ask : AutoSaveState.Always;
        await ImportPackagesCommonAsync(files, title, ImportDuplicateHandling.Allow, compress: true, useNames: true,
            dupsList: DbcAllowedDuplicateTypes, autoSaveState: autoSave);

        await CleanUpDbcImportAsync();
    }

    private static readonly HashSet<uint> DbcAllowedDuplicateTypes = [.. DbcXmlTypes, StblResourceType, NameMapResourceType];

    /// <summary>
    /// Merges duplicate string tables (the first value of a string key wins), drops Sims3Pack manifests and
    /// reports duplicate XML resources that need a manual merge.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 228-285 (DBC clean up)</remarks>
    internal async Task CleanUpDbcImportAsync()
    {
        if (_package == null) return;

        StatusMessage = "Doing DBC clean up...";
        try
        {
            foreach (var dupKey in DuplicateKeys(_package.FindAll(e => e.Key.ResourceType == StblResourceType)))
            {
                var olds = _package.FindAll(e => e.Key == dupKey).ToList();
                var merged = new StblResource(dupKey, ReadOnlyMemory<byte>.Empty);
                foreach (var old in olds)
                {
                    var oldStbl = new StblResource(dupKey, await _package.GetResourceDataAsync(old));
                    foreach (var entry in oldStbl.Entries)
                    {
                        if (!merged.ContainsKey(entry.KeyHash))
                        {
                            merged.Add(entry.KeyHash, entry.Value, entry.Flags);
                        }
                    }
                }

                var newEntry = NewResource(dupKey, merged.Data, ImportDuplicateHandling.Allow, compress: true);
                foreach (var old in olds)
                {
                    _package.DeleteResource(old);
                    RefreshItemsFor(old);
                }
                if (newEntry != null)
                {
                    AddResourceItem(newEntry, select: false, refreshList: false);
                }
            }

            // Source: Import.cs lines 262-277 (get rid of Sims3Pack resources that sneak in)
            foreach (var manifest in _package.FindAll(e => e.Key.ResourceType == Sims3PackManifestType).ToList())
            {
                _package.DeleteResource(manifest);
                RefreshItemsFor(manifest);
            }
            SetPackageDirty(true);
            ApplyFilter();

            // Source: Import.cs lines 279-283 (remaining duplicate XMLs are too messy to fix automatically)
            if (DuplicateKeys(_package.FindAll(e => DbcXmlTypes.Contains(e.Key.ResourceType))).Count > 0)
            {
                await ResourceOpsDialogs.ShowMessageAsync("Manual merge of XML files required.", "Import DBC");
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or ResourceFormatException or IOException or ArgumentException)
        {
            await ResourceOpsDialogs.ShowMessageAsync($"Could not clean up the DBC import.\n{ex.Message}", "Import DBC");
        }
        finally
        {
            StatusMessage = "DBC import finished";
        }
    }

    // Source: Import.cs lines 288-303 (DupsOnly: one key for each key that occurs more than once)
    private static List<ResourceKey> DuplicateKeys(IEnumerable<IResourceIndexEntry> entries)
    {
        var seen = new HashSet<ResourceKey>();
        var result = new List<ResourceKey>();
        foreach (var entry in entries)
        {
            if (!seen.Add(entry.Key) && !result.Contains(entry.Key))
            {
                result.Add(entry.Key);
            }
        }
        return result;
    }

    /// <summary>
    /// Imports a DDS file as a DST image: the key (type, group, instance, name) and import settings
    /// come from the Resource Details dialog.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe Helpers/DDSHelper/Import.cs; the key dialog is
    /// Import.cs lines 592-619 (ImportSingle(string, string)).</remarks>
    [RelayCommand]
    private async Task ImportDdsAsDstAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        var files = await ResourceOpsDialogs.PickImportFilesAsync("Import DDS as DST", allowMultiple: false);
        if (files.Count != 1) return;

        await ImportDdsAsDstFileAsync(files[0]);
    }

    internal async Task ImportDdsAsDstFileAsync(string filePath)
    {
        if (_package == null || !EnsureWritable()) return;

        byte[] ddsData;
        try
        {
            ddsData = await _fileSystem.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Import error: {ex.Message}";
            return;
        }

        // Source: DSTResource.cs lines 82-134 (ImportToDST): DXT1/DXT5 are shuffled, DST1/3/5 pass through
        byte[] dstData;
        try
        {
            dstData = DstDecoder.ImportToDst(ddsData);
        }
        catch (NotSupportedException)
        {
            StatusMessage = "DDS format not supported (only DXT1/DXT5)";
            return;
        }
        catch (InvalidDataException)
        {
            StatusMessage = "Invalid DDS file";
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var details = new ResourceDetailsViewModel(useName: true, displayFilename: false);
        try
        {
            details.SetKey(new ResourceKey(DdsAsDstTypeId, 0, Wrappers.Hashing.FnvHash.Fnv64(fileName)));
            details.ResourceName = fileName;
            if (!await ResourceOpsDialogs.ShowResourceDetailsAsync(details) || !details.IsValid) return;

            var s = details.Settings;
            if (await ImportStreamAsync(dstData, details.Tgin, s.UseName, s.AllowRename, s.Compress, s.DuplicateHandling, select: true))
            {
                StatusMessage = $"Imported DDS as DST: {fileName}";
            }
            else
            {
                StatusMessage = "Duplicate resource rejected";
            }
        }
        finally
        {
            details.Detach();
        }
    }

    /// <summary>
    /// Converts images to JFIF+ALFA thumbnails and imports them with the import settings.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe Helpers/ThumbnailHelper/ImportThumb.cs;
    /// the settings are the Import Batch dialog (Import.cs lines 654-709).</remarks>
    [RelayCommand]
    private async Task ImportThumbnailAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        var converted = await ResourceOpsDialogs.ConvertThumbnailImagesAsync();
        if (converted.Count == 0) return;

        await ImportThumbnailsAsync(converted);
    }

    internal async Task ImportThumbnailsAsync(IReadOnlyList<(string FileName, byte[] Data)> converted)
    {
        if (_package == null || !EnsureWritable()) return;

        const string title = "Import Thumbnails";
        var batch = new ImportBatchViewModel(converted.Select(c => c.FileName), ImportBatchMode.File, title);
        if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

        var s = batch.Settings;
        var imported = 0;
        var namesOk = true;
        foreach (var name in batch.Items)
        {
            var match = converted.FirstOrDefault(c => c.FileName == name);
            if (match.Data == null) continue;

            var baseName = Path.GetFileNameWithoutExtension(match.FileName);
            var tgin = new Tgin(ThumbnailTypeId, 0, Wrappers.Hashing.FnvHash.Fnv64(baseName), baseName);
            if (await ImportStreamAsync(match.Data, tgin, namesOk && s.UseName, s.AllowRename, s.Compress, s.DuplicateHandling, select: false))
            {
                imported++;
            }
        }

        ApplyFilter();
        StatusMessage = $"Imported {imported} thumbnail{(imported != 1 ? "s" : "")}";
    }

    /// <summary>
    /// Replaces the selected resources with the resources of the same key from packages chosen by the user.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 164-195 (ResourceReplaceFrom)</remarks>
    [RelayCommand]
    private async Task ReplaceFromPackagesAsync()
    {
        if (_package == null || !EnsureWritable()) return;

        var selection = GetSelectedItems().Select(i => i.Key).ToList();
        if (selection.Count == 0) return;

        const string title = "Replace Selected Resources from Package(s)";
        var files = await ResourceOpsDialogs.PickPackagesAsync(title);
        if (files.Count == 0) return;

        var batch = new ImportBatchViewModel(files, ImportBatchMode.ReplaceFrom, title);
        if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

        await ImportPackagesCommonAsync([.. batch.Items], title, ImportDuplicateHandling.Replace, batch.Settings.Compress,
            selection: selection);
    }

    /// <summary>
    /// Imports files dropped on the window: several files or a folder go through Import Batch,
    /// a single file through Resource Details (or Import Batch for a package).
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 561-590 (browserWidget1_DragDrop)</remarks>
    public async Task ImportDroppedFilesAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;

        const string title = "File(s)->Drop";
        if (paths.Count > 1 || Directory.Exists(paths[0]))
        {
            await ImportBatchFilesAsync(paths, title);
        }
        else
        {
            await ImportSingleFileAsync(paths[0], title);
        }
    }

    /// <summary>
    /// Imports one file: a package through Import Batch, anything else through Resource Details.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 592-619 (ImportSingle(string, string))</remarks>
    internal async Task ImportSingleFileAsync(string filename, string title)
    {
        if (_package == null)
        {
            await NewPackageAsync();
        }
        if (!EnsureWritable()) return;

        if (IsPackageFile(filename))
        {
            var batch = new ImportBatchViewModel([filename], ImportBatchMode.Package, title);
            if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

            var bs = batch.Settings;
            await ImportPackagesCommonAsync([filename], title, bs.DuplicateHandling, bs.Compress, bs.UseName);
            return;
        }

        var details = new ResourceDetailsViewModel(useName: true, displayFilename: true) { Filename = filename };
        try
        {
            if (!await ResourceOpsDialogs.ShowResourceDetailsAsync(details) || !details.IsValid) return;

            var s = details.Settings;
            await ImportFileAsync(details.Filename, details.Tgin, s.UseName, s.AllowRename, s.Compress, s.DuplicateHandling, select: true);
        }
        finally
        {
            details.Detach();
        }
    }

    /// <summary>
    /// Imports a list of files and folders (folders recursively) through the Import Batch dialog.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 654-709 (ImportBatch(string[], string)).
    /// The files imported are those left in the dialog's list (which can be edited).
    /// </remarks>
    internal async Task ImportBatchFilesAsync(IReadOnlyList<string> batchPaths, string title)
    {
        if (_package == null)
        {
            await NewPackageAsync();
        }
        if (!EnsureWritable()) return;

        var allFiles = new List<string>();
        foreach (var path in batchPaths)
        {
            if (Directory.Exists(path))
            {
                // Walk the tree off the UI thread so dropping a large folder does not freeze the window.
                var folder = path;
                allFiles.AddRange(await Task.Run(() => GetFilesRecursive(folder)));
            }
            else if (_fileSystem.FileExists(path))
            {
                allFiles.Add(path);
            }
        }

        var batch = new ImportBatchViewModel(allFiles, ImportBatchMode.File, title);
        if (!await ResourceOpsDialogs.ShowImportBatchAsync(batch)) return;

        var items = batch.Items.ToList();
        var resources = items.Where(p => !IsPackageFile(p)).ToList();
        var packages = items.Where(IsPackageFile).ToList();
        var s = batch.Settings;

        try
        {
            if (packages.Count > 0)
            {
                await ImportPackagesCommonAsync(packages, title, s.DuplicateHandling, s.Compress, s.UseName);
            }

            var nmOk = true;
            var imported = 0;
            foreach (var file in resources)
            {
                var (added, namesOk) = await ImportFileAsync(file, Tgin.Parse(file), nmOk && s.UseName, s.AllowRename, s.Compress,
                    s.DuplicateHandling, select: false);
                nmOk = namesOk;
                if (added) imported++;
            }

            ApplyFilter();
            if (resources.Count > 0)
            {
                StatusMessage = $"Imported {imported} of {resources.Count} file(s)";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ApplyFilter();
            await ResourceOpsDialogs.ShowMessageAsync($"Could not import all resources - aborting.\n{ex.Message}", title);
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 639-652 (GetFiles: subfolders first)
    private static List<string> GetFilesRecursive(string folder)
    {
        var files = new List<string>();
        foreach (var dir in Directory.EnumerateDirectories(folder))
        {
            files.AddRange(GetFilesRecursive(dir));
        }
        files.AddRange(Directory.EnumerateFiles(folder));
        return files;
    }

    /// <summary>
    /// Imports one resource file under the given key (and its name, if requested).
    /// </summary>
    /// <returns>Whether the resource was added, and whether its name could be written.</returns>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 763-803 (ImportFile)</remarks>
    private async Task<(bool Added, bool NamesOk)> ImportFileAsync(string filename, Tgin tgin, bool useName, bool rename,
        bool compress, ImportDuplicateHandling dups, bool select)
    {
        var data = await _fileSystem.ReadAllBytesAsync(filename);

        var namesOk = true;
        if (useName && !string.IsNullOrEmpty(tgin.ResourceName))
        {
            namesOk = await MergeNameMapAsync([new(tgin.Instance, tgin.ResourceName)], create: true, replace: rename);
        }

        var entry = NewResource(tgin.Key, data, dups, compress);
        if (entry != null)
        {
            AddResourceItem(entry, select, refreshList: select);
            if (select)
            {
                StatusMessage = $"Imported {data.Length:N0} bytes";
            }
        }
        return (entry != null, namesOk);
    }

    /// <summary>
    /// Imports resources from packages: name maps are merged (if names are used), everything else is added
    /// with the given duplicate handling and compression. With <paramref name="selection"/>, only resources
    /// with those keys are imported (Replace selected from packages). With <paramref name="dupsList"/> and
    /// Allow, only resource types in the list may be duplicated.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 314-503 (ImportPackagesCommon)</remarks>
    internal async Task ImportPackagesCommonAsync(IReadOnlyList<string> packageList, string title, ImportDuplicateHandling dups,
        bool compress, bool useNames = false, bool rename = false, IReadOnlyCollection<ResourceKey>? selection = null,
        IReadOnlySet<uint>? dupsList = null, AutoSaveState autoSaveState = AutoSaveState.Ask)
    {
        if (_package == null) return;

        // Source: Import.cs lines 328-345 (Ask: Yes = auto-save, No = don't, Cancel = abort)
        bool autoSave;
        if (autoSaveState == AutoSaveState.Ask)
        {
            var answer = await ResourceOpsDialogs.AskYesNoCancelAsync("Auto-save current package after each package imported?", title);
            if (answer == YesNoCancelResult.Cancel) return;
            autoSave = answer == YesNoCancelResult.Yes;
        }
        else
        {
            autoSave = autoSaveState == AutoSaveState.Always;
        }

        var keys = selection == null ? null : new HashSet<ResourceKey>(selection);
        var skipAll = false;
        var total = 0;

        foreach (var filename in packageList)
        {
            if (!string.IsNullOrEmpty(PackagePath) &&
                string.Equals(Path.GetFullPath(PackagePath), Path.GetFullPath(filename), StringComparison.Ordinal))
            {
                await ResourceOpsDialogs.ShowMessageAsync("Skipping current package.", title);
                continue;
            }

            StatusMessage = $"Importing {Path.GetFileNameWithoutExtension(filename)}...";

            DbpfPackage imported;
            try
            {
                imported = await DbpfPackage.OpenAsync(filename, readWrite: false);
            }
            catch (Exception ex) when (ex is PackageFormatException or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                if (skipAll) continue;
                // Source: Import.cs lines 370-389 ("Skip this", "Skip all", "Abort")
                var choice = await ResourceOpsDialogs.AskChoiceAsync(
                    $"Could not open package {Path.GetFileName(filename)}.\n{ex.Message}", title,
                    ["Skip this", "Skip all", "Abort"], defaultIndex: 0, cancelIndex: 2);
                if (choice == 0) continue;
                if (choice == 1)
                {
                    skipAll = true;
                    continue;
                }
                break;
            }

            var abort = false;
            await using (imported)
            {
                var toAdd = new List<(ResourceKey Key, ReadOnlyMemory<byte> Data)>();
                IResourceIndexEntry? current = null;
                try
                {
                    var entries = keys == null
                        ? imported.Resources
                        : imported.Resources.Where(e => !e.IsDeleted && keys.Contains(e.Key)).ToList();

                    foreach (var rie in entries)
                    {
                        current = rie;
                        if (rie.Key.ResourceType == NameMapResourceType)
                        {
                            if (useNames)
                            {
                                var nameMap = new NameMapResource(rie.Key, await imported.GetResourceDataAsync(rie));
                                await MergeNameMapAsync(nameMap, create: true, replace: rename);
                            }
                        }
                        else
                        {
                            toAdd.Add((rie.Key, await imported.GetResourceDataAsync(rie)));
                        }
                    }
                }
                catch (Exception ex) when (ex is PackageFormatException or InvalidDataException or IOException or ResourceFormatException)
                {
                    var rk = current != null ? $"(RK: {ResourceDetailsViewModel.FormatKey(current.Key)})\n" : "(RK is null)\n";
                    await ResourceOpsDialogs.ShowMessageAsync($"Could not import all resources - aborting.\n{rk}{ex.Message}", title);
                    abort = true;
                }

                if (!abort)
                {
                    foreach (var (key, data) in toAdd)
                    {
                        // Source: Import.cs lines 441-447 (with Allow, only types in dupsList may be duplicated;
                        // other types replace)
                        var dupThis = dups == ImportDuplicateHandling.Allow && dupsList != null && !dupsList.Contains(key.ResourceType)
                            ? ImportDuplicateHandling.Replace
                            : dups;
                        var entry = NewResource(key, data, dupThis, compress);
                        if (entry != null)
                        {
                            AddResourceItem(entry, select: false, refreshList: false);
                            total++;
                        }
                    }
                }
            }

            ApplyFilter();
            if (abort) break;

            // Source: Import.cs lines 485-488 (auto-save after each package; stop if the save fails)
            if (autoSave && !await SaveCurrentPackageAsync()) break;
        }

        ApplyFilter();
        StatusMessage = $"Imported {total} resource(s)";
    }

    /// <summary>
    /// Saves the current package to its file, or asks for one (legacy FileSave).
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 841-865 (FileSave)</remarks>
    private Task<bool> SaveCurrentPackageAsync() =>
        !IsReadOnly && !string.IsNullOrEmpty(PackagePath) ? SavePackageAsync() : SavePackageAsAsync();

    #endregion

    #region Export

    /// <summary>
    /// Exports the selected resource to a file (TGIN file name), or several to a folder.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1758-1791 (ResourceExport)</remarks>
    [RelayCommand]
    private async Task ExportResourceAsync()
    {
        if (_package == null) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;
        if (items.Count > 1)
        {
            await ExportBatchAsync(items);
            return;
        }

        var item = items[0];
        var entry = ResolveEntry(item);
        if (entry == null) return;

        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1773-1781 (the dialog starts in
        // LastExportPath, which is then set to the chosen file's folder)
        var settings = SettingsService.Instance.Settings;
        var path = await ResourceOpsDialogs.PickExportFileAsync(
            Tgin.From(entry.Key, item.InstanceName).ToFileName(), settings.LastExportPath);
        if (string.IsNullOrEmpty(path)) return;
        settings.LastExportPath = Path.GetDirectoryName(path);

        try
        {
            var size = await ExportFileAsync(entry, path);
            StatusMessage = $"Exported {size:N0} bytes";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PackageFormatException)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    /// <summary>
    /// Exports the selected resources to a folder, one TGIN-named file each.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1793-1864 (ExportBatch)</remarks>
    [RelayCommand]
    private async Task ExportToFolderAsync()
    {
        if (_package == null) return;
        var items = GetSelectedItems();
        if (items.Count == 0) return;
        await ExportBatchAsync(items);
    }

    private async Task ExportBatchAsync(IReadOnlyList<ResourceItemViewModel> items)
    {
        if (_package == null) return;

        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1798-1804 (the folder browser
        // starts in LastExportPath, which is then set to the chosen folder)
        var settings = SettingsService.Instance.Settings;
        var folder = await ResourceOpsDialogs.PickExportFolderAsync(settings.LastExportPath);
        if (string.IsNullOrEmpty(folder)) return;
        settings.LastExportPath = folder;

        var overwriteAll = false;
        var skipAll = false;
        var exported = 0;
        var errors = 0;

        foreach (var item in items)
        {
            var entry = ResolveEntry(item);
            if (entry == null) continue;

            var file = Path.Combine(folder, Tgin.From(entry.Key, item.InstanceName).ToFileName());
            if (_fileSystem.FileExists(file))
            {
                if (skipAll) continue;
                if (!overwriteAll)
                {
                    var choice = await ResourceOpsDialogs.AskOverwriteAsync(file);
                    if (choice == OverwritePromptResult.No) continue;
                    if (choice == OverwritePromptResult.NoToAll)
                    {
                        skipAll = true;
                        continue;
                    }
                    if (choice == OverwritePromptResult.YesToAll) overwriteAll = true;
                    if (choice == OverwritePromptResult.Abandon) break;
                }
            }

            try
            {
                await ExportFileAsync(entry, file);
                exported++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PackageFormatException)
            {
                Debug.WriteLine($"Export of {file} failed: {ex.Message}");
                errors++;
            }
        }

        StatusMessage = errors > 0
            ? $"Exported {exported} resources ({errors} errors)"
            : $"Exported {exported} resources to folder";
    }

    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1866-1880 (ExportFile writes the uncompressed data)
    private async Task<int> ExportFileAsync(IResourceIndexEntry entry, string path)
    {
        var data = await _package!.GetResourceDataAsync(entry);
        await _fileSystem.WriteAllBytesAsync(path, data.ToArray());
        return data.Length;
    }

    /// <summary>
    /// Exports the selected resources (and their names) into another package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1882-2091 (ResourceExportToPackage,
    /// ExportResourceToPackage). Legacy maps the first button ("Reject") to replace = true; here Reject
    /// keeps the target's resource and Replace replaces it, as the buttons say.
    /// </remarks>
    [RelayCommand]
    private async Task ExportToPackageAsync()
    {
        if (_package == null) return;

        var items = GetSelectedItems();
        if (items.Count == 0) return;

        var targetPath = await ResourceOpsDialogs.PickExportPackageAsync();
        if (string.IsNullOrEmpty(targetPath)) return;

        if (!string.IsNullOrEmpty(PackagePath) &&
            Path.GetFullPath(PackagePath).Equals(Path.GetFullPath(targetPath), StringComparison.Ordinal))
        {
            await ResourceOpsDialogs.ShowMessageAsync("Target must not be same as source.", "Export to package");
            return;
        }

        var isNew = !_fileSystem.FileExists(targetPath);
        DbpfPackage target;
        try
        {
            target = isNew ? DbpfPackage.CreateNew() : await DbpfPackage.OpenAsync(targetPath, readWrite: true);
        }
        catch (Exception ex) when (ex is PackageFormatException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            await ResourceOpsDialogs.ShowMessageAsync($"Export cannot begin.  Could not open target package:\n{targetPath}\n\n{ex.Message}", "Export to package");
            return;
        }

        await using (target)
        {
            var replace = false;
            if (!isNew)
            {
                var answer = await ResourceOpsDialogs.AskExportDuplicatesAsync();
                if (answer == ExportDuplicatesPromptResult.Abandon) return;
                replace = answer == ExportDuplicatesPromptResult.Replace;
            }

            try
            {
                StatusMessage = $"Exporting to {Path.GetFileNameWithoutExtension(targetPath)}...";
                var sourceNames = new Dictionary<ulong, string>();
                var exported = 0;

                foreach (var item in items)
                {
                    var entry = ResolveEntry(item);
                    if (entry == null) continue;

                    if (!string.IsNullOrEmpty(item.InstanceName))
                    {
                        sourceNames.TryAdd(entry.Key.Instance, item.InstanceName);
                    }

                    if (await ExportResourceToPackageAsync(target, entry, replace))
                    {
                        exported++;
                    }
                }

                if (sourceNames.Count > 0)
                {
                    var nmEntry = target.Find(e => e.Key.ResourceType == NameMapResourceType)
                        ?? target.AddResource(new ResourceKey(NameMapResourceType, 0,
                            FNV64.GetHash(targetPath + DateTime.Now.ToString(CultureInfo.CurrentCulture))), rejectDuplicates: true);
                    if (nmEntry != null)
                    {
                        var targetNames = new NameMapResource(nmEntry.Key, await target.GetResourceDataAsync(nmEntry));
                        foreach (var (instance, name) in sourceNames)
                        {
                            targetNames.TryAdd(instance, name);
                        }
                        target.ReplaceResource(nmEntry, targetNames.Data);
                    }
                }

                StatusMessage = "Saving...";
                if (isNew)
                {
                    await target.SaveAsAsync(targetPath);
                }
                else
                {
                    await target.SaveAsync();
                }

                StatusMessage = $"Exported {exported} resources";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PackageFormatException or ResourceFormatException)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2073-2091 (ExportResourceToPackage)
    private async Task<bool> ExportResourceToPackageAsync(DbpfPackage target, IResourceIndexEntry source, bool replace)
    {
        var existing = target.Find(source.Key);
        if (existing != null)
        {
            if (!replace) return false;
            target.DeleteResource(existing);
        }

        var data = await _package!.GetResourceDataAsync(source);
        var entry = target.AddResource(source.Key, data, rejectDuplicates: true);
        if (entry == null) return false;
        target.SetCompression(entry, source.CompressionType);
        return true;
    }

    #endregion
}
