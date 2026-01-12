# TS4Tools 100% Legacy Parity Loop

You are implementing remaining features to achieve 100% parity with legacy s4pe.

## Critical Paths
- Guidelines: /mnt/ai/code/TS4Tools/CLAUDE.md
- Legacy s4pe: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pe/
- Legacy s4pi Package: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pi/Package/
- Legacy Helpers: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pe Helpers/
- Modern UI: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/
- Modern Core: /mnt/ai/code/TS4Tools/src/TS4Tools.Core/
- Views: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/Views/
- ViewModels: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/ViewModels/

## Already Implemented (DO NOT REIMPLEMENT)
- Copy/Paste/Duplicate operations
- Search dialog (basic)
- Resource Details dialog
- New Package command
- Settings dialog framework
- Advanced filter widget
- Batch import dialog
- Property grid editor
- Package merge utility
- All resource editors (Hex, STBL, NameMap, Text, Image, Catalog, RCOL, SimData)
- Helper file parser and execution system (HelperManager.cs)
- Control panel toolbar (Hex/Value/Grid, Auto mode, UseNames/UseTags)
- Manage Wrappers dialog
- Bookmarks menu and Organise Bookmarks dialog
- Custom Places dialog
- Floating preview window
- Save Preview command
- IFileSystemService abstraction
- Check for Updates (UpdateCheckerService)
- Package Header tab in Package Statistics
- Paste ResourceKey to Filter
- TGI block selector control
- Binary property editor control (ReaderEditorPanel equivalent)
- File locking during package save
- Atomic save with temp file pattern
- Stream truncation after save
- DDS preview with channel toggles
- RLE texture preview
- Batch export to folder
- Export to Package dialog
- DDS Import as DST command
- Select All command (Ctrl+A)
- Open ReadOnly mode
- Quick recent files (Ctrl+1-9)
- Quick bookmarks (Ctrl+Shift+1-9)
- Keyboard shortcuts (Insert, Ctrl+Shift+M, Ctrl+Shift+V)
- Set Max Recent/Bookmarks UI
- Save Copy As command
- Sort toggle in toolbar
- Hex-only viewing mode setting (Force Hex View)

## Resource Wrappers Status (~95% Complete)
All major wrappers are ported. Minor gaps (handled by UnknownRcolBlock fallback):
- TREE (0x021D7E8C)
- TkMk (0x033260E3)
- ANIM (0x63A33EA7)

## Feature Priority Queue (implement in order)

### Phase 1 - DeformerMap Visualization

1. DeformerMap preview control
   - Source: s4pe/BuiltInValueControl.cs DMAP_Control class
   - Create DeformerMapPreviewControl.axaml in Views/Controls/
   - Create DeformerMapViewerViewModel in ViewModels/Editors/
   - Visualize deformer map data as image/grid showing bone weights
   - Support 0xDB43E069 type ID
   - Wire into MainWindowViewModel editor selection

### Phase 2 - Thumbnail Import Helper

2. Thumbnail Import Helper window
   - Source: s4pe Helpers/ThumbnailHelper/
   - Create ThumbnailHelperWindow.axaml in Views/Dialogs/
   - Create ThumbnailHelperViewModel in ViewModels/
   - Import PNG files as thumbnails
   - Auto-generate JFIF+ALFA format (thumbnail resources use this dual format)
   - Support batch import of multiple PNG files
   - Wire to Resource → Import → Import Thumbnail menu item
   - Replace legacy ThumbnailHelper.exe functionality

### Phase 3 - DBC Import

3. Import as DBC format
   - Source: s4pe/Import/ExperimentalDBCWarning.cs
   - Source: s4pi/Package/Package.cs DBC handling
   - Create DbcImportService in Services/
   - Support DBC (Database Cache) format import
   - Show experimental warning dialog before import
   - Parse DBC structure into individual resources
   - Add "Prompt for DBC autosaving" setting toggle in AppSettings
   - Wire to File → Import → Import DBC menu item

## Each Iteration

1. **CHECK PROGRESS**: List implemented vs unimplemented features from the queue above.
   Read MainWindowViewModel.cs, Services/, and ViewModels/Editors/ to see what exists.
   If ALL Phase 1-3 features are implemented, skip to COMPLETION CHECK.

2. **SELECT NEXT**: Pick the FIRST unimplemented feature from the priority queue.

3. **ANALYZE LEGACY**: Read the corresponding legacy s4pe/s4pi code thoroughly.
   - For DeformerMap: Read s4pe/BuiltInValueControl.cs DMAP_Control section
   - For Thumbnail: Read s4pe Helpers/ThumbnailHelper/ directory
   - For DBC: Read s4pe/Import/ and s4pi DBC-related code
   Add Source comments referencing legacy file + line numbers.

4. **IMPLEMENT**: Follow patterns:
   - For UI features: Create View (.axaml) in Views/ or Views/Dialogs/ or Views/Controls/
   - Create ViewModel in ViewModels/ or ViewModels/Editors/ if needed
   - Wire up in MainWindowViewModel.cs
   - Use CommunityToolkit.Mvvm [RelayCommand] attributes
   - Follow existing code patterns in the project
   - Add services to Services/ folder if needed

5. **VERIFY**: Run 'dotnet build'. Fix any errors before proceeding.

6. **COMMIT**: Stage and commit with conventional format:
   - 'feat(ui): add DeformerMap visualization preview'
   - 'feat(ui): add Thumbnail Import Helper dialog'
   - 'feat(core): add DBC format import support'

## Completion Check

After each iteration, check if ALL Phase 1-3 features are implemented:

### Phase 1 - DeformerMap Visualization
- [ ] DeformerMap preview control with bone weight visualization

### Phase 2 - Thumbnail Import Helper
- [ ] ThumbnailHelperWindow with PNG to JFIF+ALFA conversion

### Phase 3 - DBC Import
- [ ] DBC format import with experimental warning
- [ ] "Prompt for DBC autosaving" setting

If ALL are implemented and 'dotnet build' passes, output: <promise>FULL_PARITY_COMPLETE</promise>

## Important
- ALWAYS read existing code before implementing (follow established patterns)
- One feature per iteration maximum
- Reference legacy s4pe/s4pi code with Source comments
- Use Avalonia + CommunityToolkit.Mvvm patterns consistently
- Implement ALL features - do not skip any regardless of complexity
- For external process launching, use System.Diagnostics.Process
- For temp files, use Path.GetTempPath() and proper cleanup
- For image manipulation, use Avalonia.Media.Imaging or SkiaSharp if needed
