# s4pi Extras and Helper Tools

This document catalogs the s4pi extras libraries and s4pe helper applications.

## s4pi Extras

Located in `legacy_references/Sims4Tools/s4pi Extras/`

### Helpers Library
**Files:** `Helpers/Helpers.cs`, `Helpers/RunHelper.cs`

Helper system for integrating external tools:
- Helper configuration parsing (`.helper` files)
- Process execution with temp file exchange
- Matching helpers to resource types

### Extensions Library
**Files:** `Extensions/`

Utility extensions:
- `TGIN.cs` - Type/Group/Instance/Name tuple
- `FileNameConverter.cs` - TGI to filename conversion
- `ExtList.cs` - Extension list utilities

### DDSPanel Library
**Files:** `DDSPanel/`

DDS (DirectDraw Surface) texture handling:
- `DdsFile.cs` - DDS format parsing/writing
- `DdsSquish.cs` - DXT compression/decompression
- `DDSPanel.cs` - WinForms preview control
- `RGBHSV.cs` - Color space conversion

### Filetable Library
**Files:** `Filetable/`

Game file system access:
- `FileTable.cs` - Game package enumeration
- `GameFolders.cs` - Game installation detection
- `GameFoldersForm.cs` - Folder selection UI
- `RK.cs` - Resource key utilities
- `SpecificResource.cs` - Resource location
- `PathPackageTuple.cs` - Package path mapping

## s4pe Helper Applications

Located in `legacy_references/Sims4Tools/s4pe Helpers/`

### DDSHelper
Standard DDS texture import/export.

**Usage:** Edit DDS textures in external editors.

### RLEDDSHelper
RLE-compressed DDS texture handling.

**Usage:** Edit RLE-compressed textures.

### RLESDDSHelper
RLES (variant RLE) compressed DDS handling.

**Usage:** Edit RLES-compressed textures.

### DMAPImageHelper
DMAP (depth map) image processing.

**Usage:** Edit depth/displacement maps.

### ThumbnailHelper
Thumbnail generation and import.

**Usage:** Create/edit object thumbnails.

### ModelViewer
3D model visualization (WPF-based).

**Usage:** Preview GEOM/mesh resources.

## Helper Integration Pattern

Helpers communicate with s4pe via:
1. Temp file with exported resource data
2. Helper edits the file
3. s4pe reads back modified data
4. Optional: Clipboard fallback

### Helper Configuration Example

```
Label: Edit DDS
Command: DDSHelper.exe
Arguments: "{}"
Desc: Edit DDS texture
ResourceType: 0x00B2D882
```

## Rewrite Considerations

### DDSPanel
- Replace with cross-platform image library
- Consider ImageSharp or SkiaSharp for DDS support
- May need custom DXT decompression

### Filetable
- Platform-specific game folder detection
- Windows registry → Linux/macOS alternatives
- Consider Steam library parsing

### Helpers
- Reconsider temp file approach
- Could use memory-mapped files
- Platform-specific process launching

### ModelViewer
- WPF is Windows-only
- Consider Avalonia 3D or separate viewer
- Could use OpenGL/Vulkan bindings
