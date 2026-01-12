# TS4Tools Rewrite Loop

Start an autonomous Ralph Wiggum loop to implement UI features for s4pe parity.

---

## Usage

```
/rewrite-loop
```

No arguments required. The loop will automatically discover remaining work.

---

## Execute

Execute this slash command:

/ralph-wiggum:ralph-loop "## TS4Tools UI Parity Loop

You are implementing UI features to achieve parity with legacy s4pe.

### Critical Paths
- Guidelines: /mnt/ai/code/TS4Tools/CLAUDE.md
- Legacy s4pe: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pe/
- Modern UI: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/
- Views: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/Views/
- ViewModels: /mnt/ai/code/TS4Tools/src/TS4Tools.UI/ViewModels/

### Feature Priority Queue (implement in order)

**Phase 1 - Essential Operations:**
1. Copy/Paste/Duplicate resource operations (Edit menu + context menu)
2. Search dialog (Tools → Search, regex support, multi-field)
3. Resource Details dialog (right-click → Details, show all metadata)
4. New Package command (File → New)

**Phase 2 - Settings & Configuration:**
5. Settings dialog framework (Settings menu)
6. External Programs settings (configure external editors per type)
7. Display options (Use Names, Use Tags, Auto-preview toggle)
8. Bookmarks system (File → Bookmarks, custom folder shortcuts)

**Phase 3 - Advanced Features:**
9. Advanced filter widget (multi-field, regex, column selection)
10. Batch import dialog (import multiple files with settings)
11. Property grid editor (field-level resource editing)
12. Package merge utility (combine packages)

**Phase 4 - Polish:**
13. Floating preview windows (undock resource preview)
14. Control panel UI (persistent toggles for display options)
15. Resource compression toggle (mark compressed/uncompressed)

### Each Iteration

1. **CHECK PROGRESS**: List implemented vs unimplemented features from the queue above.
   Read MainWindowViewModel.cs and Views/ to see what exists.
   If ALL Phase 1-3 features are implemented, skip to COMPLETION CHECK.

2. **SELECT NEXT**: Pick the FIRST unimplemented feature from the priority queue.

3. **ANALYZE LEGACY**: Read the corresponding legacy s4pe code:
   - MainForm.cs for menu operations
   - Tools/SearchForm.cs for search
   - Settings/*.cs for settings dialogs
   - Import/*.cs for batch import
   - Filter/ResourceFilterWidget.cs for filtering
   Add Source comments referencing legacy file + line numbers.

4. **IMPLEMENT**: Follow Avalonia MVVM patterns:
   - Create View (.axaml) in Views/ or Views/Dialogs/
   - Create ViewModel in ViewModels/
   - Wire up in MainWindowViewModel.cs
   - Use CommunityToolkit.Mvvm [RelayCommand] attributes
   - Follow existing code patterns in the project

5. **VERIFY**: Run 'dotnet build'. Fix any errors before proceeding.

6. **COMMIT**: Stage and commit with conventional format:
   'feat(ui): add [feature name]'

### Completion Check

After each iteration, check if ALL Phase 1-3 features are implemented:
- Copy/Paste/Duplicate operations
- Search dialog
- Resource Details dialog
- New Package command
- Settings dialog framework
- External Programs settings
- Display options
- Bookmarks system
- Advanced filter widget
- Batch import dialog
- Property grid editor
- Package merge utility

If ALL are implemented and 'dotnet build' passes, output: <promise>UI_PARITY_COMPLETE</promise>

### Important
- ALWAYS read existing code before implementing (follow established patterns)
- One feature per iteration maximum
- Reference legacy s4pe code with Source comments
- Use Avalonia + CommunityToolkit.Mvvm patterns consistently
- Skip feature if blocked; note reason and move to next
" --completion-promise "UI_PARITY_COMPLETE" --max-iterations 30

---

## Control Commands

- **Cancel**: `/ralph-wiggum:cancel-ralph`
- **Monitor**: `git log --oneline -20`
- **Check progress**: `ls src/TS4Tools.UI/Views/`
