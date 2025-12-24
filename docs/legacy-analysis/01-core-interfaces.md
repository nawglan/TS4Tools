# s4pi Core Interfaces Analysis

This document analyzes the core interfaces and abstract classes in the s4pi library that form the foundation of the package editing system.

## Interface Hierarchy

```
IApiVersion
├── IContentFields
│   └── IResource ──────────────────→ AResource (abstract)
│   └── IPackage ───────────────────→ APackage (abstract) → Package
│   └── IResourceIndexEntry ────────→ AResourceIndexEntry
│
├── IResourceKey ───────────────────→ AResourceKey
│
└── IResourceHandler ───────────────→ AResourceHandler (abstract)

AApiVersionedFields (abstract) ─────→ implements IApiVersion, IContentFields
└── AHandlerElement (abstract) ─────→ adds change notification
    └── HandlerElement<T> ──────────→ wrapper for simple value types
    └── TGIBlockListIndex<T> ───────→ index into TGIBlock lists
```

## Core Interfaces

### IApiVersion
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IApiVersion.cs`

Provides API versioning support (not fully implemented in legacy code).

```csharp
public interface IApiVersion
{
    Int32 RequestedApiVersion { get; }   // Version requested on creation
    Int32 RecommendedApiVersion { get; } // Best available version
}
```

**Rewrite considerations:**
- Versioning was never fully implemented - consider if we need this complexity
- Could simplify to a single version or remove entirely

### IContentFields
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IContentFields.cs`

Provides reflection-based property access for UI binding.

```csharp
public interface IContentFields
{
    List<string> ContentFields { get; }           // Available field names
    TypedValue this[string index] { get; set; }   // Get/set by name
}
```

**Purpose:** Enables generic property grid binding without compile-time type knowledge.

**Rewrite considerations:**
- Modern alternatives: `System.Text.Json`, source generators, or explicit interfaces
- Avalonia has its own binding system that may not need this abstraction
- Consider if reflection overhead is acceptable

### IResource
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IResource.cs`

Minimal interface for resource content access.

```csharp
public interface IResource : IApiVersion, IContentFields
{
    Stream Stream { get; }           // Resource as stream
    byte[] AsBytes { get; }          // Resource as byte array
    event EventHandler ResourceChanged;
}
```

**Key patterns:**
- Resources are fundamentally byte streams
- Change notification for dirty tracking
- Inherits content fields for UI binding

### IResourceKey (TGI - Type/Group/Instance)
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IResourceKey.cs`

Unique identifier for resources within a package.

```csharp
public interface IResourceKey : IEqualityComparer<IResourceKey>,
                                 IEquatable<IResourceKey>,
                                 IComparable<IResourceKey>
{
    UInt32 ResourceType { get; set; }   // Resource type identifier
    UInt32 ResourceGroup { get; set; }  // Group identifier
    UInt64 Instance { get; set; }       // Unique instance ID
}
```

**The TGI System:**
- **T**ype: Identifies the resource format (e.g., 0x034AEECB = CAS Part)
- **G**roup: Logical grouping (often 0x00000000)
- **I**nstance: Unique 64-bit identifier (often FNV hash of name)

**Rewrite considerations:**
- This is fundamental - keep the same structure
- Consider making it a `readonly record struct` for immutability and value semantics

### IResourceIndexEntry
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IResourceIndexEntry.cs`

Extends IResourceKey with storage location metadata.

```csharp
public interface IResourceIndexEntry : IApiVersion, IContentFields,
                                        IResourceKey, IEquatable<IResourceIndexEntry>
{
    UInt32 Chunkoffset { get; set; }  // Position in package file
    UInt32 Filesize { get; set; }     // Compressed size on disk
    UInt32 Memsize { get; set; }      // Uncompressed size in memory
    UInt16 Compressed { get; set; }   // 0xFFFF if compressed, 0x0000 if not
    UInt16 Unknown2 { get; set; }     // Always 0x0001
    Stream Stream { get; }            // Index entry as bytes
    bool IsDeleted { get; set; }      // Soft-delete flag
}
```

**Compression detection:**
- `Compressed == 0xFFFF` means `Filesize != Memsize` (data is compressed)
- `Compressed == 0x0000` means `Filesize == Memsize` (data is uncompressed)

### IPackage
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/IPackage.cs`

Complete package management interface.

```csharp
public interface IPackage : IApiVersion, IContentFields, IDisposable
{
    // Save operations
    void SavePackage();
    void SaveAs(Stream s);
    void SaveAs(string path);

    // DBPF Header fields (96 bytes total)
    byte[] Magic { get; }              // "DBPF" (4 bytes)
    Int32 Major { get; }               // Version major: 2
    Int32 Minor { get; }               // Version minor: 0
    Int32 Indexcount { get; }          // Number of resources
    Int32 Indexposition { get; }       // Byte offset to index
    Int32 Indexsize { get; }           // Index size in bytes
    UInt32 Indextype { get; }          // Index format flags
    // ... other header fields

    // Index access
    event EventHandler ResourceIndexInvalidated;
    List<IResourceIndexEntry> GetResourceList { get; }

    // Find operations
    IResourceIndexEntry Find(Predicate<IResourceIndexEntry> Match);
    List<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match);

    // Content operations
    IResourceIndexEntry AddResource(IResourceKey rk, Stream stream, bool rejectDups);
    void ReplaceResource(IResourceIndexEntry rc, IResource res);
    void DeleteResource(IResourceIndexEntry rc);
}
```

## Abstract Base Classes

### AApiVersionedFields
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs` (1087 lines)

Large class providing:
- Reflection-based property enumeration
- Version-filtered field listing
- Priority-based field sorting (via `ElementPriorityAttribute`)
- TypedValue indexer implementation
- ValueBuilder for string representation
- FOURCC string/int conversion utilities

**Key static methods:**
```csharp
static List<string> GetContentFields(int APIversion, Type t)
static Dictionary<string, Type> GetContentFieldTypes(int APIversion, Type t)
static DependentList<TGIBlock> GetTGIBlocks(AApiVersionedFields o, string f)
```

**Nested classes:**
- `Comparer<T>` - Sorts API objects by field name
- `AHandlerElement` - Adds change handler support
- `HandlerElement<T>` - Wraps simple value types with change notification
- `TGIBlockListIndex<T>` - Index into TGIBlock list with parent reference

### AResource
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs`

Base class for all resource wrappers.

```csharp
public abstract class AResource : AApiVersionedFields, IResource
{
    protected Stream stream = null;
    protected bool dirty = false;

    protected AResource(int APIversion, Stream s)
    {
        requestedApiVersion = APIversion;
        stream = s;
    }

    public virtual Stream Stream
    {
        get
        {
            if (dirty || Settings.AsBytesWorkaround)
            {
                stream = UnParse();  // Re-serialize if dirty
                dirty = false;
            }
            stream.Position = 0;
            return stream;
        }
    }

    // CRITICAL: Subclasses must implement this
    protected abstract Stream UnParse();

    protected virtual void OnResourceChanged(object sender, EventArgs e)
    {
        dirty = true;
        ResourceChanged?.Invoke(sender, e);
    }
}
```

**Parse/UnParse Pattern:**
- **Parse**: Called in constructor to deserialize stream into properties
- **UnParse**: Serializes properties back to stream (abstract, must implement)
- Lazy serialization: Only calls UnParse when Stream property accessed and dirty

### AResourceHandler
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs`

Simple dictionary subclass for registering wrapper types.

```csharp
public abstract class AResourceHandler : Dictionary<Type, List<string>>, IResourceHandler
{
    public AResourceHandler() { }
}
```

Usage pattern (in each wrapper assembly):
```csharp
public class MyResourceHandler : AResourceHandler
{
    public MyResourceHandler()
    {
        Add(typeof(MyResource), new List<string> { "0x12345678", "0xABCDEF00" });
    }
}
```

## Key Attributes

### ElementPriorityAttribute
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/ElementPriorityAttribute.cs`

Controls field display order in UI.

```csharp
[ElementPriority(1)]
public uint Version { get; set; }

[ElementPriority(2)]
public string Name { get; set; }
```

### MinimumVersionAttribute / MaximumVersionAttribute
**File:** `legacy_references/Sims4Tools/s4pi/Interfaces/VersionAttribute.cs`

Version-gates field availability (rarely used).

```csharp
[MinimumVersion(2)]
public string NewField { get; set; }  // Only in API version 2+
```

## Rewrite Recommendations

### Keep
1. **IResourceKey structure** - The TGI system is fundamental to Sims 4 packages
2. **Parse/UnParse pattern** - Clean serialization abstraction
3. **Dirty tracking** - Essential for knowing when to save
4. **Change notification** - Required for reactive UI

### Modernize
1. **Replace reflection with source generators** - Better performance, compile-time safety
2. **Use `Span<T>`/`Memory<T>`** - For efficient binary parsing
3. **Consider `readonly record struct`** for ResourceKey - Immutability, value semantics
4. **Replace Dictionary-based handler** with attribute-based discovery or DI registration
5. **Async Save operations** - `SavePackageAsync()`, `SaveAsAsync()`

### Simplify
1. **Remove API versioning** - Not actually used, adds complexity
2. **Replace TypedValue with modern binding** - Avalonia has its own binding system
3. **Flatten inheritance hierarchy** - Consider composition over inheritance
4. **Remove UNDEF sections** - Dead code (Methods, Invoke)

### New Additions
1. **IAsyncDisposable** - For async resource cleanup
2. **Result<T> pattern** - For expected failures instead of exceptions
3. **Cancellation support** - `CancellationToken` on long operations
4. **Progress reporting** - `IProgress<T>` for large package operations
