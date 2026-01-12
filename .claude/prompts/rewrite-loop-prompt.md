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
- Search dialog
- Resource Details dialog
- New Package command
- Settings dialog framework
- Advanced filter widget
- Batch import dialog
- Property grid editor
- Package merge utility
- All resource editors (Hex, STBL, NameMap, Text, Image, Catalog, RCOL, SimData)

## Feature Priority Queue (implement in order)

### Phase 1 - External Programs Integration
1. Helper file parser (parse .helper config files from Helpers/ folder)
   - Source: s4pe/Helpers/*.helper, MainForm.cs helper loading
   - Config format: command, arguments, resource type filters
2. External Programs settings dialog (Settings → External Programs)
   - Source: s4pe/Settings/ExternalProgramsDialog.cs
   - Configure helper programs per resource type
3. Helper execution system (launch external editors)
   - Source: s4pe/MainForm.cs EditOte(), helper execution code
   - Export resource to temp file, launch program, reimport on close

### Phase 2 - Control Panel Widget
4. Control panel toolbar (persistent toolbar below menu)
   - Source: s4pe/ControlPanel/ControlPanel.cs
   - Buttons: Hex, Value, Grid, Helper1, Helper2, HexEdit, Commit
   - Checkboxes: Sort, HexOnly, UseNames, UseTags
   - Radio group: Auto Off/Hex/Preview
5. Auto mode toggle (Off/Hex/Preview selection)
   - Source: s4pe/ControlPanel/ControlPanel.cs lines 99-127
   - Controls automatic preview behavior
6. UseNames/UseTags display toggles
   - Source: s4pe/ControlPanel/ControlPanel.cs lines 174-202
   - Toggle name resolution and tag display in resource list

### Phase 3 - Wrapper Management
7. Manage Wrappers dialog (Settings → Manage Wrappers)
   - Source: s4pe/Settings/ManageWrappersDialog.cs
   - Enable/disable specific resource wrappers
   - Show wrapper info: type ID, name, assembly
8. Wrapper enable/disable persistence
   - Source: s4pe/Settings/ManageWrappersDialog.cs
   - Save wrapper states to settings
   - Apply on startup

### Phase 4 - Bookmarks System
9. Bookmarks menu (File → Bookmarks)
   - Source: s4pe/MainForm.cs bookmark handling
   - Quick access to favorite folders
10. Organise Bookmarks dialog
    - Source: s4pe/Settings/OrganiseBookmarksDialog.cs
    - Add, remove, reorder bookmarks
11. Custom Places dialog (organize file dialog shortcuts)
    - Source: s4pe/Settings/OrganiseCustomPlacesDialog.cs
    - Configure custom places in Open/Save dialogs

### Phase 5 - Floating Windows
12. Floating preview window (Edit → Float)
    - Source: s4pe/MainForm.cs EditFloat()
    - Undock preview panel to separate window
13. Save Preview command (Edit → Save Preview)
    - Source: s4pe/MainForm.cs EditSavePreview()
    - Save current preview content to file

## Each Iteration

1. **CHECK PROGRESS**: List implemented vs unimplemented features from the queue above.
   Read MainWindowViewModel.cs and Views/ to see what exists.
   Check for: HelperManager, ControlPanel, ManageWrappers, Bookmarks, FloatingPreview
   If ALL Phase 1-5 features are implemented, skip to COMPLETION CHECK.

2. **SELECT NEXT**: Pick the FIRST unimplemented feature from the priority queue.

3. **ANALYZE LEGACY**: Read the corresponding legacy s4pe code:
   - Helpers/*.helper for helper file format
   - Settings/ExternalProgramsDialog.cs for external programs UI
   - Settings/ManageWrappersDialog.cs for wrapper management
   - Settings/OrganiseBookmarksDialog.cs for bookmarks
   - ControlPanel/ControlPanel.cs for toolbar
   - MainForm.cs for integration code
   Add Source comments referencing legacy file + line numbers.

4. **IMPLEMENT**: Follow Avalonia MVVM patterns:
   - Create View (.axaml) in Views/ or Views/Dialogs/ or Views/Controls/
   - Create ViewModel in ViewModels/
   - Wire up in MainWindowViewModel.cs
   - Use CommunityToolkit.Mvvm [RelayCommand] attributes
   - Follow existing code patterns in the project
   - Add services to Services/ folder if needed (e.g., HelperManager)

5. **VERIFY**: Run 'dotnet build'. Fix any errors before proceeding.

6. **COMMIT**: Stage and commit with conventional format:
   'feat(ui): add [feature name]'

## Completion Check

After each iteration, check if ALL Phase 1-5 features are implemented:
- Helper file parser
- External Programs settings dialog
- Helper execution system
- Control panel toolbar
- Auto mode toggle
- UseNames/UseTags display toggles
- Manage Wrappers dialog
- Wrapper enable/disable persistence
- Bookmarks menu
- Organise Bookmarks dialog
- Custom Places dialog
- Floating preview window
- Save Preview command

If ALL are implemented and 'dotnet build' passes, output: <promise>FULL_PARITY_COMPLETE</promise>

## Important
- ALWAYS read existing code before implementing (follow established patterns)
- One feature per iteration maximum
- Reference legacy s4pe code with Source comments
- Use Avalonia + CommunityToolkit.Mvvm patterns consistently
- Skip feature if blocked; note reason and move to next
- For external process launching, use System.Diagnostics.Process
- For temp files, use Path.GetTempPath() and proper cleanup
