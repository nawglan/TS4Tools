# TS4Tools 100% Legacy Parity Loop

You are implementing remaining features to achieve 100% parity with legacy s4pe.

## Critical Paths
- Guidelines: /mnt/ai/code/TS4Tools/CLAUDE.md
- Legacy s4pe: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pe/
- Modern UI: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/
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
- Helper file parser and execution system
- Control panel toolbar (Hex/Value/Grid, Auto mode, UseNames/UseTags)
- Manage Wrappers dialog
- Bookmarks menu and Organise Bookmarks dialog
- Custom Places dialog
- Floating preview window
- Save Preview command
- IFileSystemService abstraction

## Feature Priority Queue (implement in order)

### Phase 6 - Core Missing Features (High Priority)
1. Undo/Redo system
   - Source: s4pe/MainForm.cs editUndo_Click, editRedo_Click
   - Create UndoRedoService to track resource operations
   - Track: Add, Delete, Modify, Replace operations
   - Wire to Edit menu Undo (Ctrl+Z) and Redo (Ctrl+Y)

2. Select All command (Ctrl+A)
   - Source: s4pe/MainForm.cs resourceSelectAll_Click
   - Add to Edit menu with keyboard shortcut
   - Select all visible resources in filtered list

3. Advanced hex content search
   - Source: s4pe/Tools/SearchForm.cs
   - Add search modes: Hex bytes, Unicode string, ASCII string
   - Add type filter during search
   - Multi-threaded search with progress bar
   - Search cancellation support

### Phase 7 - Workflow Improvements (Medium Priority)
4. Open ReadOnly mode
   - Source: s4pe/MainForm.cs fileOpenReadOnly_Click
   - Add File → Open ReadOnly menu item
   - Open package without write permissions
   - Disable Save, mark window title as [Read Only]

5. Import from Package dialog
   - Source: s4pe/MainForm.cs resourceImportPackage_Click
   - Browse for source package file
   - Select resources to import
   - Handle duplicates (Replace/Skip/Allow)

6. Export to Package dialog
   - Source: s4pe/MainForm.cs resourceExportPackage_Click
   - Browse for destination package (or create new)
   - Export selected resources to another package

7. Import as DBC format
   - Source: s4pe/Import/ExperimentalDBCWarning.cs
   - Support DBC (Database Cache) format import
   - Show experimental warning dialog
   - Parse DBC structure into resources
   - Add "Prompt for DBC autosaving" setting toggle

8. External editor file tracking
   - Source: s4pe/MainForm.cs helper code
   - Track temp file modification time after external edit
   - Prompt to reimport when file changes
   - Auto-commit changes back to package

### Phase 8 - Keyboard Shortcuts (Medium Priority)
9. Quick recent files (Ctrl+1 through Ctrl+9)
   - Source: s4pe/MainForm.cs recent file handling
   - Map Ctrl+1-9 to first 9 recent files

10. Quick bookmarks (Ctrl+Shift+1 through Ctrl+Shift+9)
    - Source: s4pe/MainForm.cs bookmark handling
    - Map Ctrl+Shift+1-9 to first 9 bookmarks

11. Additional shortcuts
    - Insert key → Add resource
    - Ctrl+Shift+B → Bookmark current package
    - Ctrl+Shift+M → Import from file
    - Ctrl+Shift+X → Export to file
    - Ctrl+Shift+H → Open in hex editor
    - Ctrl+Shift+T → Open in text editor
    - F1 → Help

12. Paste ResourceKey to Filter
    - Source: s4pe/MainForm.cs filter context menu
    - Add context menu item to filter panel
    - Parse clipboard TGI and populate filter fields

### Phase 9 - Property Grid Enhancements (Low Priority)
13. TGIBlockSelection control
    - Source: s4pe/s4pePropertyGrid/TGIBlockSelection.cs
    - Dropdown selector for TGI blocks in DependentLists
    - Integrate with property grid editor

14. ReaderEditorPanel control
    - Source: s4pe/s4pePropertyGrid/ReaderEditorPanel.cs
    - Import/Export buttons for BinaryReader/TextReader fields
    - Allow importing binary data into property grid fields

### Phase 10 - Polish (Low Priority)
15. Set Max Recent/Bookmarks UI
    - Source: s4pe/MainForm.cs fileRecentSetMax_Click, fileBookmarkSetMax_Click
    - Add "Set Max..." option to Recent and Bookmarks submenus
    - Use simple number input dialog

16. PackageInfo panel improvements
    - Source: s4pe/PackageInfo/PackageInfoWidget.cs
    - Add package header info tab to PackageStatsWindow
    - Show: Version, flags, index info, timestamps

17. Save Copy As command
    - Source: s4pe/MainForm.cs fileSaveCopyAs_Click
    - File → Save Copy As
    - Save copy without changing current file path

18. Check for Updates
    - Source: s4pe/Settings/UpdateChecker.cs, s4pe/MainForm.cs helpUpdate_Click
    - Add Help → Check for Updates menu item
    - Add Settings → Automatic Update Checks toggle
    - Check GitHub releases for newer versions
    - Show update available notification

## Each Iteration

1. **CHECK PROGRESS**: List implemented vs unimplemented features from the queue above.
   Read MainWindowViewModel.cs and Services/ to see what exists.
   Check for: UndoRedoService, SelectAll, AdvancedSearch, ReadOnly mode, etc.
   If ALL Phase 6-10 features are implemented, skip to COMPLETION CHECK.

2. **SELECT NEXT**: Pick the FIRST unimplemented feature from the priority queue.

3. **ANALYZE LEGACY**: Read the corresponding legacy s4pe code.
   Add Source comments referencing legacy file + line numbers.

4. **IMPLEMENT**: Follow Avalonia MVVM patterns:
   - Create View (.axaml) in Views/ or Views/Dialogs/ or Views/Controls/
   - Create ViewModel in ViewModels/ if needed
   - Wire up in MainWindowViewModel.cs
   - Use CommunityToolkit.Mvvm [RelayCommand] attributes
   - Follow existing code patterns in the project
   - Add services to Services/ folder if needed

5. **VERIFY**: Run 'dotnet build'. Fix any errors before proceeding.

6. **COMMIT**: Stage and commit with conventional format:
   'feat(ui): add [feature name]'

## Completion Check

After each iteration, check if ALL Phase 6-10 features are implemented:
- Undo/Redo system
- Select All command
- Advanced hex content search
- Open ReadOnly mode
- Import from Package dialog
- Export to Package dialog
- Import as DBC format (with autosave setting)
- External editor file tracking
- Quick recent files shortcuts
- Quick bookmark shortcuts
- Additional keyboard shortcuts
- Paste ResourceKey to Filter
- TGIBlockSelection control
- ReaderEditorPanel control
- Set Max Recent/Bookmarks UI
- PackageInfo panel improvements
- Save Copy As command
- Check for Updates (with auto-check setting)

If ALL are implemented and 'dotnet build' passes, output: <promise>FULL_PARITY_COMPLETE</promise>

## Important
- ALWAYS read existing code before implementing (follow established patterns)
- One feature per iteration maximum
- Reference legacy s4pe code with Source comments
- Use Avalonia + CommunityToolkit.Mvvm patterns consistently
- Skip feature if blocked; note reason and move to next
- For external process launching, use System.Diagnostics.Process
- For temp files, use Path.GetTempPath() and proper cleanup
