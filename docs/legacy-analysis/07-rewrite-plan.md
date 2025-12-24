# TS4Tools Rewrite Plan

This document outlines the plan for rewriting s4pe/s4pi as TS4Tools using modern .NET and Avalonia UI.

## Technology Stack

- **.NET 9** - Latest LTS framework
- **C# 12+** - Modern language features
- **Avalonia UI 11.x** - Cross-platform XAML UI
- **CommunityToolkit.Mvvm** - MVVM framework
- **Microsoft.Extensions.DependencyInjection** - DI container
- **xUnit + FluentAssertions** - Testing
- **BenchmarkDotNet** - Performance testing

## Project Structure

```
TS4Tools/
├── src/
│   ├── TS4Tools.Core/           # Core library (package parsing, resources)
│   │   ├── Package/             # DBPF package handling
│   │   ├── Resources/           # Resource wrapper system
│   │   ├── Compression/         # ZLIB/RefPack compression
│   │   └── Hashing/             # FNV hash utilities
│   │
│   ├── TS4Tools.Interfaces/     # Public API contracts
│   │   ├── IPackage.cs
│   │   ├── IResource.cs
│   │   ├── IResourceKey.cs
│   │   └── ...
│   │
│   ├── TS4Tools.Wrappers/       # Resource type wrappers
│   │   ├── StblResource/        # String tables
│   │   ├── ImageResource/       # Images/textures
│   │   ├── CatalogResource/     # Build/Buy catalog
│   │   └── ...
│   │
│   └── TS4Tools.UI/             # Avalonia application
│       ├── ViewModels/
│       ├── Views/
│       ├── Services/
│       └── Controls/
│
├── tests/
│   ├── TS4Tools.Core.Tests/
│   ├── TS4Tools.Wrappers.Tests/
│   └── TS4Tools.Benchmarks/
│
└── docs/
    └── legacy-analysis/         # This documentation
```

## Phase 1: Core Infrastructure

### 1.1 Interfaces
Define clean, modern interfaces:

```csharp
public readonly record struct ResourceKey(
    uint ResourceType,
    uint ResourceGroup,
    ulong Instance) : IEquatable<ResourceKey>;

public interface IResource : IDisposable
{
    ReadOnlyMemory<byte> Data { get; }
    bool IsDirty { get; }
    event EventHandler? Changed;
}

public interface IPackage : IAsyncDisposable
{
    IReadOnlyList<IResourceIndexEntry> Resources { get; }
    ValueTask<IResource> GetResourceAsync(ResourceKey key, CancellationToken ct = default);
    ValueTask SaveAsync(CancellationToken ct = default);
}
```

### 1.2 DBPF Package Reader/Writer

Implement DBPF format handling:
- Header parsing (96 bytes)
- Index reading with type flags optimization
- Resource data access
- ZLIB compression (use System.IO.Compression)
- RefPack decompression (read-only, for legacy files)

```csharp
public sealed class Package : IPackage
{
    public static async Task<Package> OpenAsync(
        string path,
        bool readWrite = false,
        CancellationToken ct = default);

    public static Package CreateNew();
}
```

### 1.3 Resource Wrapper System

Attribute-based registration:

```csharp
[ResourceType(0x220557DA)]
public sealed class StblResource : ResourceBase
{
    public IReadOnlyList<StringEntry> Entries { get; }
}
```

Factory pattern:

```csharp
public interface IResourceFactory
{
    bool CanCreate(uint resourceType);
    IResource Create(ReadOnlyMemory<byte> data);
}
```

## Phase 2: Essential Wrappers

Priority order based on usage frequency:

### 2.1 DefaultResource
Fallback wrapper for raw byte access.

### 2.2 StblResource (String Table)
- FNV hash keys
- UTF-8 strings
- Version 5 format

### 2.3 NameMapResource
- Hash → Name mapping
- Essential for debugging

### 2.4 ImageResource
- DDS textures
- RLE compression
- Thumbnail variants

### 2.5 TextResource
- Plain text/XML
- Configuration files

## Phase 3: Avalonia UI

### 3.1 Main Window

```
┌─────────────────────────────────────────────────────────────┐
│ Menu Bar                                                     │
├─────────────────────────────────────────────────────────────┤
│ ┌───────────────────────┬───────────────────────────────────┤
│ │ Resource Tree/List    │ Property Grid / Preview           │
│ │                       │                                   │
│ │ - Filter              │ Tabs:                             │
│ │ - Search              │ - Properties                      │
│ │ - Sorting             │ - Hex View                        │
│ │                       │ - Preview                         │
│ └───────────────────────┴───────────────────────────────────┤
├─────────────────────────────────────────────────────────────┤
│ Status Bar                                                   │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 ViewModels

```csharp
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private PackageViewModel? _currentPackage;

    [ObservableProperty]
    private ResourceViewModel? _selectedResource;

    [RelayCommand]
    private async Task OpenPackageAsync()
    {
        var file = await _fileService.OpenFileAsync("Package Files|*.package");
        if (file != null)
        {
            CurrentPackage = await _packageService.OpenAsync(file);
        }
    }
}
```

### 3.3 Key Controls

- **ResourceListControl** - Virtualized list with filtering
- **PropertyGridControl** - Resource property editing
- **HexViewControl** - Binary data viewer
- **PreviewControl** - Type-specific previews

## Phase 4: Advanced Wrappers

### 4.1 CASPartResource
Character customization data.

### 4.2 CatalogResource
Build/Buy mode items (24 subtypes).

### 4.3 MeshChunks
3D geometry data.

### 4.4 RigResource
Skeleton/bone hierarchies.

### 4.5 Remaining Wrappers
Implement based on community need.

## Phase 5: Helper Integration

### 5.1 External Tool Support
- Process launching
- File exchange (temp files or pipes)
- Cross-platform considerations

### 5.2 Built-in Editors
- Text editor (AvaloniaEdit)
- Hex editor
- Image viewer

## Testing Strategy

### Unit Tests
- Resource parsing/serialization round-trips
- Package read/write
- Compression algorithms

### Integration Tests
- Real .package files
- Cross-platform validation
- Large file handling

### Benchmarks
- Package loading time
- Resource access patterns
- Memory usage

## Migration Path

### Compatibility Layer
Optional `TS4Tools.Compatibility` project:
- `IPackage` adapter for legacy code
- `AResource` compatibility shim
- Plugin migration support

### Breaking Changes
- Remove reflection-based ContentFields
- Require async for I/O
- Immutable ResourceKey

## Security Considerations

All parsers must validate:
- Magic bytes and version numbers
- Array sizes before allocation
- String lengths before reading
- Offset bounds checking
- Reasonable limits on counts

```csharp
public static class Limits
{
    public const int MaxResourceCount = 100_000;
    public const int MaxResourceSize = 100 * 1024 * 1024; // 100 MB
    public const int MaxStringLength = 1024 * 1024; // 1 MB
}
```

## Performance Goals

- Package open: < 100ms for typical mods
- Resource access: < 10ms cached
- UI responsiveness: 60 FPS maintained
- Memory: < 500 MB for large packages

## Development Milestones

1. **Core library** - Package R/W, basic wrappers
2. **Minimal UI** - Open, browse, view resources
3. **Essential wrappers** - STBL, NameMap, Image, Text
4. **Full UI** - Edit, save, filter, search
5. **Advanced wrappers** - CAS, Catalog, Mesh
6. **Polish** - Performance, UX, documentation
