# s4pe GUI Application Architecture

This document analyzes the s4pe (Sims 4 Package Editor) WinForms GUI application.

## Source Location

`legacy_references/Sims4Tools/s4pe/`

## Overview

s4pe is a WinForms application with:
- Main form orchestrating all widgets (~3,255 lines)
- Modular widgets for specific functions
- Helper tool integration for external editors
- Event-driven architecture

## Application Entry Point

**File:** `s4pe/Program.cs`

```csharp
[STAThread]
static void Main(string[] args)
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    // Configuration upgrade check...
    Application.Run(new MainForm(args));
}
```

## Main Form Structure

**File:** `s4pe/MainForm.cs` (~3,255 lines)

### Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ Menu Bar (MenuBarWidget)                                         │
├─────────────────────────────────────────────────────────────────┤
│ SplitContainer (Vertical)                                        │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ SplitContainer (Horizontal)                                  │ │
│ │ ┌───────────────────────┬───────────────────────────────────┤ │
│ │ │ BrowserWidget         │ Tab Control                        │ │
│ │ │ (Resource List)       │ - PackageInfoWidget               │ │
│ │ │                       │ - s4piPropertyGrid                │ │
│ │ └───────────────────────┴───────────────────────────────────┤ │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │ Preview Panel (pnAuto)                                       │ │
│ │ - HexControl                                                 │ │
│ │ - TextControl                                                │ │
│ │ - DDSWidget (images)                                         │ │
│ │ - Built-in value controls                                    │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ Control Panel (ControlPanel)                                     │
│ [Sort] [Auto: Off/Hex/Preview] [HexOnly] [Helper1] [Helper2]    │
├─────────────────────────────────────────────────────────────────┤
│ Status Bar (Progress tracking)                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Key Widgets

### BrowserWidget
**File:** `s4pe/BrowserWidget/BrowserWidget.cs`

Displays package resources in a sortable, filterable ListView.

**Properties:**
- `Package` - Current IPackage
- `SelectedResource` - Selected IResourceIndexEntry
- `Fields` - Visible columns
- `Filter` - Active filter

**Events:**
- `ListUpdated`
- `ItemActivate`
- `SelectedResourceChanging` / `SelectedResourceChanged`
- `DeletePressed`

### ControlPanel
**File:** `s4pe/ControlPanel/ControlPanel.cs`

User interaction controls.

**Properties:**
- `Sort` - Enable sorting
- `AutoMode` - Off/Hex/Preview
- `HexOnly` - Force hex view
- `Helper1Enabled/Label`, `Helper2Enabled/Label`

**Events:**
- `SortChanged`, `AutoChanged`, `HexClick`
- `Helper1Click`, `Helper2Click`
- `CommitClick`

### MenuBarWidget
**File:** `s4pe/MenuBarWidget/MenuBarWidget.cs`

Main menu and context menus.

**Menus:**
- File: New, Open, Save, Close, MRU list
- Edit: Copy, Paste, Float controls
- Resource: Add, Delete, Replace, Import/Export
- Tools: FNV Hash, Search
- Settings: Wrappers, Bookmarks, Updates

### ResourceFilterWidget
**File:** `s4pe/Filter/ResourceFilterWidget.cs`

Dynamic filtering based on resource properties.

### PackageInfoWidget
**File:** `s4pe/PackageInfo/PackageInfoWidget.cs`

Displays IPackage metadata.

### s4piPropertyGrid
**File:** `s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs`

Custom PropertyGrid for AApiVersionedFields with nested object support.

## s4pi Integration

### Package Management

```csharp
// Opening
CurrentPackage = Package.OpenPackage(0, filename, readWrite);

// Saving
CurrentPackage.SavePackage();
CurrentPackage.SaveAs(filename);

// Resource access
IResourceIndexEntry rie = CurrentPackage.Find(predicate);
IResource resource = WrapperDealer.GetResource(0, CurrentPackage, rie);
```

### WrapperDealer Integration

```csharp
// Get all available wrappers
WrapperDealer.TypeMap

// Disable specific wrappers
WrapperDealer.Disabled.Add(new KeyValuePair<string, Type>(code, type));

// Get resource with appropriate wrapper
IResource res = WrapperDealer.GetResource(0, package, indexEntry);

// Force raw bytes (no wrapper)
IResource res = WrapperDealer.GetResource(0, package, indexEntry, AlwaysDefault: true);
```

## Helper Tool Integration

### Configuration Files

Located in `s4pe/Helpers/`:
- `Helpers.txt` - Documentation
- `*.helper` - Individual helper configs

### Helper File Format

```
Label: Image Editor
Command: mspaint.exe
Arguments: "{}"
Desc: Open image in MS Paint
ReadOnly: yes
```

### Matching Criteria

```
Wrapper: ImageResource      # Match by wrapper class
ResourceType: 0x02DC343F    # Match by type code
ResourceType: *             # Wildcard match
```

### Execution Flow

```csharp
// Create manager for selected resource
HelperManager mgr = new HelperManager(selectedResource, ...);

// Get matching helpers
List<Helper> helpers = mgr.GetHelpers();

// Execute helper (returns modified data)
MemoryStream result = mgr.execHelper(index);
```

## Core Workflows

### Package Opening

```
User selects file
  → Filename property set
    → PackageFilenameChanged event
      → Package.OpenPackage()
        → CurrentPackage property set
          → PackageChanged event
            → BrowserWidget.Package = package
            → PackageInfoWidget.Package = package
            → Update menu states
```

### Resource Selection

```
User clicks resource
  → SelectedResource property set
    → SelectedResourceChanged event
      → WrapperDealer.GetResource()
        → Create HelperManager
          → Update ControlPanel
            → Trigger auto-preview
              → GetPreviewControl()
```

### Resource Modification

```
User modifies data
  → IResource.ResourceChanged event
    → resourceIsDirty = true
      → Enable Commit button

User clicks Commit
  → CurrentPackage.ReplaceResource()
    → IsPackageDirty = true
      → Enable Save button
```

## Preview System

Fallback chain:
1. Built-in ValueControl (by resource type)
2. String Value field (if present)
3. DDS preview (for images)
4. Hex view (if enabled)
5. Text view (if enabled)
6. No preview

## Settings Persistence

```csharp
// Save event pattern
MainForm.SaveSettings += (s, e) => {
    Properties.Settings.Default.WindowPosition = this.Location;
    Properties.Settings.Default.SplitterPosition = splitContainer.SplitterDistance;
};

// Widget-level persistence
BrowserWidget_SaveSettings += (s, e) => {
    Properties.Settings.Default.ColumnWidths = GetColumnWidths();
};
```

## Dirty State Management

```csharp
bool IsPackageDirty { get; set; }  // Package needs save
bool resourceIsDirty;               // Current resource needs commit

// Prompt before data loss
if (IsPackageDirty && !ConfirmClose()) return;
```

## Tool Dialogs

- `FNVHashDialog` - Calculate FNV32/FNV64 hashes
- `SearchForm` - Search package by criteria

## Import System

**Files:**
- `Import/Import.cs` - Single resource import
- `Import/ImportBatch.cs` - Batch import wizard
- `Import/ImportSettings.cs` - Import configuration
- `Import/ResourceDetails.cs` - Per-resource options

## Rewrite for Avalonia

### Architecture Mapping

| WinForms | Avalonia |
|----------|----------|
| Form | Window |
| UserControl | UserControl |
| SplitContainer | Grid with GridSplitter |
| ListView | DataGrid / ListBox with ItemTemplate |
| PropertyGrid | Custom PropertyGrid or TreeDataGrid |
| MenuStrip | Menu |
| StatusStrip | Panel at bottom |

### MVVM Conversion

```csharp
// Old: Code-behind event handling
void browserWidget1_SelectedResourceChanged(object sender, EventArgs e)
{
    var resource = WrapperDealer.GetResource(...);
    UpdatePreview(resource);
}

// New: ViewModel with reactive properties
public class MainViewModel : ReactiveObject
{
    [Reactive] public IResourceIndexEntry? SelectedResource { get; set; }

    public MainViewModel()
    {
        this.WhenAnyValue(x => x.SelectedResource)
            .Where(r => r != null)
            .SelectMany(r => LoadResourceAsync(r))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(res => CurrentResource = res);
    }
}
```

### Async Operations

```csharp
// Old: Synchronous
var package = Package.OpenPackage(0, filename, readWrite);

// New: Async with progress
var package = await PackageService.OpenAsync(filename, readWrite, progress);
```

### Command Pattern

```csharp
// Old: Menu event handlers
menuFileSave.Click += (s, e) => SavePackage();

// New: Commands
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

SaveCommand = ReactiveCommand.CreateFromTask(
    SavePackageAsync,
    this.WhenAnyValue(x => x.IsPackageDirty)
);
```

### Preview System

```csharp
// Dynamic preview control selection
public interface IResourcePreview
{
    bool CanPreview(IResource resource);
    Control CreatePreview(IResource resource);
}

// Register previews
services.AddSingleton<IResourcePreview, HexPreview>();
services.AddSingleton<IResourcePreview, ImagePreview>();
services.AddSingleton<IResourcePreview, TextPreview>();
```
