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
- Binary property editor control

## Resource Wrappers Status (~95% Complete)
All major wrappers are ported. Minor gaps (handled by UnknownRcolBlock fallback):
- TREE (0x021D7E8C) - rarely used
- TkMk (0x033260E3) - rarely used
- ANIM (0x63A33EA7) - rarely used

## Feature Priority Queue (implement in order)

### Phase 1 - CRITICAL: Package Save Safety (MUST FIX FIRST)

1. File locking during package save
   - Source: s4pi/Package/Package.cs lines 61-79
   - Add FileStream.Lock()/Unlock() around save operations
   - Prevents concurrent modification corruption
   - Implementation: src/TS4Tools.Core/Package/DbpfPackage.cs SaveAsync method

2. Atomic save with temp file pattern
   - Source: s4pi/Package/Package.cs lines 64-75
   - Save to Path.GetTempFileName() first
   - Only copy to original after successful save
   - If save fails, original package untouched
   - Implementation: src/TS4Tools.Core/Package/DbpfPackage.cs SaveAsync method

3. Stream truncation after save
   - Source: s4pi/Package/Package.cs line 75
   - Call output.SetLength(output.Position) after final write
   - Prevents trailing garbage when re-saved package is smaller
   - Implementation: src/TS4Tools.Core/Package/PackageWriter.cs WriteAsync method

### Phase 2 - HIGH: Advanced Preview Controls

4. DDS preview with channel toggles
   - Source: s4pe/BuiltInValueControl.cs DDS_Control class
   - Create DdsPreviewControl.axaml in Views/Controls/
   - Add R/G/B/A channel toggle checkboxes
   - Support 0x00B2D882 (DST) and 0x8FFB80F6 type IDs
   - Integrate with ImageViewerViewModel

5. RLE texture preview
   - Source: s4pe/BuiltInValueControl.cs RLE_Control class
   - Create RlePreviewControl.axaml in Views/Controls/
   - Support 0x3453CF95 and 0xBA856C78 type IDs
   - Decode RLE format to displayable image

6. DeformerMap visualization
   - Source: s4pe/BuiltInValueControl.cs DMAP_Control class
   - Create DeformerMapPreviewControl.axaml in Views/Controls/
   - Visualize deformer map data as image/grid
   - Support 0xDB43E069 type ID

### Phase 3 - HIGH: Batch Operations

7. Batch export to folder
   - Source: s4pe/MainForm.cs resourceExportToFolder_Click
   - Add Resource → Export → Export to Folder menu item
   - Select destination folder
   - Export all selected resources with proper filenames
   - Progress dialog with cancellation
   - Use TGIN format: S4_Type_Group_Instance[_Name].ext

8. Export to Package dialog
   - Source: s4pe/MainForm.cs resourceExportPackage_Click
   - Browse for destination package (or create new)
   - Export selected resources to another package
   - Handle duplicates (Replace/Skip/Allow)

### Phase 4 - MEDIUM: Helper Tools (Avalonia Rewrites)

9. DDS Import/Export Helper
   - Source: s4pe Helpers/DDSHelper/
   - Create DdsHelperWindow.axaml in Views/Dialogs/
   - Import DDS files with format conversion options
   - Export resources as DDS with format selection
   - Replace legacy DDSHelper.exe functionality

10. Thumbnail Import Helper
    - Source: s4pe Helpers/ThumbnailHelper/
    - Create ThumbnailHelperWindow.axaml in Views/Dialogs/
    - Import PNG files as thumbnails
    - Auto-generate JFIF+ALFA format
    - Replace legacy ThumbnailHelper.exe functionality

11. External programs manager
    - Source: s4pe/Settings (external program registration)
    - Create ManageExternalProgramsWindow.axaml
    - Add/Edit/Remove external program associations
    - Configure which resource types use which programs
    - Store in settings

### Phase 5 - MEDIUM: Core Missing Features

12. Undo/Redo system
    - Source: s4pe/MainForm.cs editUndo_Click, editRedo_Click
    - Create UndoRedoService to track resource operations
    - Track: Add, Delete, Modify, Replace operations
    - Wire to Edit menu Undo (Ctrl+Z) and Redo (Ctrl+Y)

13. Select All command (Ctrl+A)
    - Source: s4pe/MainForm.cs resourceSelectAll_Click
    - Add to Edit menu with keyboard shortcut
    - Select all visible resources in filtered list

14. Advanced hex content search
    - Source: s4pe/Tools/SearchForm.cs
    - Add search modes: Hex bytes, Unicode string, ASCII string
    - Add type filter during search
    - Multi-threaded search with progress bar
    - Search cancellation support

15. Open ReadOnly mode
    - Source: s4pe/MainForm.cs fileOpenReadOnly_Click
    - Add File → Open ReadOnly menu item
    - Open package without write permissions
    - Disable Save, mark window title as [Read Only]

16. Import as DBC format
    - Source: s4pe/Import/ExperimentalDBCWarning.cs
    - Support DBC (Database Cache) format import
    - Show experimental warning dialog
    - Parse DBC structure into resources
    - Add "Prompt for DBC autosaving" setting toggle

### Phase 6 - MEDIUM: Keyboard Shortcuts

17. Quick recent files (Ctrl+1 through Ctrl+9)
    - Source: s4pe/MainForm.cs recent file handling
    - Map Ctrl+1-9 to first 9 recent files

18. Quick bookmarks (Ctrl+Shift+1 through Ctrl+Shift+9)
    - Source: s4pe/MainForm.cs bookmark handling
    - Map Ctrl+Shift+1-9 to first 9 bookmarks

19. Additional shortcuts
    - Insert key → Add resource
    - Ctrl+Shift+B → Bookmark current package
    - Ctrl+Shift+M → Import from file
    - Ctrl+Shift+X → Export to file
    - Ctrl+Shift+H → Open in hex editor
    - Ctrl+Shift+T → Open in text editor
    - F1 → Help

### Phase 7 - LOW: Property Grid Enhancements

20. ReaderEditorPanel control
    - Source: s4pe/s4pePropertyGrid/ReaderEditorPanel.cs
    - Import/Export buttons for BinaryReader/TextReader fields
    - Allow importing binary data into property grid fields

### Phase 8 - LOW: Polish

21. Set Max Recent/Bookmarks UI
    - Source: s4pe/MainForm.cs fileRecentSetMax_Click, fileBookmarkSetMax_Click
    - Add "Set Max..." option to Recent and Bookmarks submenus
    - Use simple number input dialog

22. Save Copy As command
    - Source: s4pe/MainForm.cs fileSaveCopyAs_Click
    - File → Save Copy As
    - Save copy without changing current file path

23. Sort toggle in toolbar
    - Source: s4pe/ControlPanel/ControlPanel.cs
    - Add sort toggle button to control panel
    - Sort by Type, Group, Instance, Name

24. Hex-only viewing mode setting
    - Source: s4pe Settings
    - Add setting to force hex view for all resources
    - Bypass automatic preview selection

## Each Iteration

1. **CHECK PROGRESS**: List implemented vs unimplemented features from the queue above.
   Read MainWindowViewModel.cs, Services/, and Core/Package/ to see what exists.
   Check for: File locking, temp file save, DDS preview, batch export, etc.
   If ALL Phase 1-8 features are implemented, skip to COMPLETION CHECK.

2. **SELECT NEXT**: Pick the FIRST unimplemented feature from the priority queue.
   CRITICAL: Phase 1 features MUST be done first before any others.

3. **ANALYZE LEGACY**: Read the corresponding legacy s4pe/s4pi code.
   Add Source comments referencing legacy file + line numbers.

4. **IMPLEMENT**: Follow patterns:
   - For Core features: Edit src/TS4Tools.Core/Package/
   - For UI features: Create View (.axaml) in Views/ or Views/Dialogs/ or Views/Controls/
   - Create ViewModel in ViewModels/ if needed
   - Wire up in MainWindowViewModel.cs
   - Use CommunityToolkit.Mvvm [RelayCommand] attributes
   - Follow existing code patterns in the project
   - Add services to Services/ folder if needed

5. **VERIFY**: Run 'dotnet build'. Fix any errors before proceeding.

6. **COMMIT**: Stage and commit with conventional format:
   - Core fixes: 'fix(core): add file locking during package save'
   - UI features: 'feat(ui): add DDS preview with channel toggles'

## Completion Check

After each iteration, check if ALL Phase 1-8 features are implemented:

### Phase 1 - Critical Package Safety
- [ ] File locking during save
- [ ] Atomic save with temp file pattern
- [ ] Stream truncation after save

### Phase 2 - Advanced Preview Controls
- [ ] DDS preview with channel toggles
- [ ] RLE texture preview
- [ ] DeformerMap visualization

### Phase 3 - Batch Operations
- [ ] Batch export to folder
- [ ] Export to Package dialog

### Phase 4 - Helper Tools
- [ ] DDS Import/Export Helper
- [ ] Thumbnail Import Helper
- [ ] External programs manager

### Phase 5 - Core Missing Features
- [ ] Undo/Redo system
- [ ] Select All command
- [ ] Advanced hex content search
- [ ] Open ReadOnly mode
- [ ] Import as DBC format

### Phase 6 - Keyboard Shortcuts
- [ ] Quick recent files (Ctrl+1-9)
- [ ] Quick bookmarks (Ctrl+Shift+1-9)
- [ ] Additional shortcuts

### Phase 7 - Property Grid
- [ ] ReaderEditorPanel control

### Phase 8 - Polish
- [ ] Set Max Recent/Bookmarks UI
- [ ] Save Copy As command
- [ ] Sort toggle in toolbar
- [ ] Hex-only viewing mode setting

If ALL are implemented and 'dotnet build' passes, output: <promise>FULL_PARITY_COMPLETE</promise>

## Important
- CRITICAL: Phase 1 (Package Save Safety) MUST be implemented first - these prevent data loss
- ALWAYS read existing code before implementing (follow established patterns)
- One feature per iteration maximum
- Reference legacy s4pe/s4pi code with Source comments
- Use Avalonia + CommunityToolkit.Mvvm patterns consistently
- Skip feature if blocked; note reason and move to next
- For external process launching, use System.Diagnostics.Process
- For temp files, use Path.GetTempPath() and proper cleanup
- For file locking, use FileStream.Lock()/Unlock() with proper try/finally
