# TS4Tools agent guide

How to read and edit Sims 4 `.package` (DBPF) files with the TS4Tools libraries. Written for AI coding agents and humans.
Every C# block marked "Sample N" is copied from `tests/TS4Tools.Core.Tests/DocSamplesTests.cs`, which compiles and runs
against in-memory packages, so these samples match the current API. If you change a sample here, change the test too.
Samples omit the `using` lines. Add the namespaces listed in "Namespaces" below.

Legacy behavior is the source of truth: the code under `legacy_references/Sims4Tools/` (s4pi/s4pe) defines what is correct.

## 1. Project layout and what to reference

| Assembly | Contains | Reference it when |
|---|---|---|
| `TS4Tools.Interfaces` | `IPackage`, `IMutablePackage`, `IResource`, `IResourceIndexEntry`, `ResourceKey`, `IResourceFactory`, `[ResourceHandler]`, exceptions, `PackageLimits` | writing plugins; pulled in transitively by Core |
| `TS4Tools.Core` | `DbpfPackage`, `ResourceHandlerRegistry`, `TypedResource`, `ExtList`, `LibrarySettings`, compression, `FileTable/` | always (this is the package engine) |
| `TS4Tools.Wrappers` | typed wrappers: STBL, NameMap, Text, SimData, CASP, catalog (COBJ, ...), RCOL/GEOM, images (DDS/DST/RLE), `Hashing/FNV*` | when you need parsed resources or hashes |
| `TS4Tools.Compatibility` | legacy s4pi API shim (`WrapperDealer`, `s4pi.*` namespaces) | only to run old plugins |
| `TS4Tools.UI` | Avalonia app | never from library code |

Minimum for scripts and tools: reference `TS4Tools.Core` and `TS4Tools.Wrappers` (project references; no NuGet package is published).
`TS4Tools.Core` cannot reference `TS4Tools.Wrappers`; the registry finds wrappers at run time (section 5).

### Namespaces

```csharp
using TS4Tools.Extensions;                    // ExtList
using TS4Tools.FileTable;                     // GameFileTable, GameFolders, RK, SpecificResource
using TS4Tools.Package;                       // DbpfPackage
using TS4Tools.Resources;                     // ResourceHandlerRegistry, TypedResource, DefaultResource
using TS4Tools.Wrappers;                      // StblResource, NameMapResource, TextResource, SimDataResource, ImageResource, DdsImage, RcolResource, UnsupportedResourceVersionException
using TS4Tools.Wrappers.CasPartResource;      // CasPartResource and friends (namespace has the same name as the class)
using TS4Tools.Wrappers.CatalogResource;      // CobjResource, CatalogTagRegistry, ...
using TS4Tools.Wrappers.Hashing;              // FnvHash, FNV32, FNV64, FNV24, FNV64CLIP
// TS4Tools (root namespace, no using needed inside it): ResourceKey, IPackage, IResource, exceptions
```

Repo rule: do not add `using System.*` to `.cs` files. Add a `<Using Include="System.Text" />` item to the `.csproj` instead.
(The samples use `System.Text.Encoding` fully qualified in a test project that does not have that global using.)

## 2. Opening packages

`DbpfPackage` is the only `IMutablePackage` implementation. Always dispose it (`await using`). It owns the backing stream unless `leaveOpen: true`.

**Sample 1: open from a stream, header, find, read raw bytes**

```csharp
// Sample 1: open (stream), header, find, read raw bytes
await using var package = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
Console.WriteLine($"v{package.MajorVersion}.{package.MinorVersion} resources={package.ResourceCount}");

var key = new ResourceKey(StblType, 0, 0x1);
IResourceIndexEntry? entry = package.Find(key);                 // by key
entry = package.Find(StblType, 0, 0x1);                          // by t/g/i
var allStbl = package.FindAll(e => e.Key.ResourceType == StblType).ToList();
ReadOnlyMemory<byte> bytes = await package.GetResourceDataAsync(entry!); // decompressed
```

**Sample 2: open from disk, read-only vs read-write**

```csharp
// Sample 2: open from disk
await using (var ro = await DbpfPackage.OpenAsync(path))                 // read-only (default)
{
    // ro.AddResource(...) would throw InvalidOperationException
    ro.IsReadOnly.Should().BeTrue();
}
await using (var rw = await DbpfPackage.OpenAsync(path, readWrite: true))
{
    rw.IsReadOnly.Should().BeFalse();
}
```

- `OpenAsync(path)` opens read-only; mutating calls throw `InvalidOperationException("Package is read-only.")`. Use `readWrite: true` to edit or save in place. Files are opened with `FileShare.ReadWrite`, like legacy.
- `OpenAsync(Stream, leaveOpen)` is read-only only when `stream.CanWrite` is false.
- `DbpfPackage.CreateNew()` makes an empty package (major version 2, the only supported major). It has no `FilePath`, so use `SaveAsAsync` or `SaveToStreamAsync`.
- Header properties (all read-only): `MajorVersion`, `MinorVersion`, `UserVersionMajor/Minor`, `CreationTime`, `UpdatedTime`, `Unused1..Unused5`, `HeaderIndexCount/Size/Position`, `IndexType`, `ResourceCount`, `FilePath`, `IsDirty`, `IsReadOnly`.
- `package.Resources` is the index (`IReadOnlyList<IResourceIndexEntry>`) in file order. It includes entries marked deleted, and `ResourceCount` counts them too; all `Find`/`FindAll` overloads skip deleted entries.
- A bad file throws `PackageFormatException` (bad magic, version, counts or offsets). Counts and sizes are bounded by `PackageLimits` (500,000 resources, 100 MB per resource).

## 3. Finding resources

- `package.Find(ResourceKey)` and `package.Find(type, group, instance)`: first non-deleted exact match, or `null`.
- `package.Find(Func<IResourceIndexEntry,bool>)`: first non-deleted entry matching a predicate. `FindAll(predicate)` returns all of them (lazily).
- To see deleted entries, iterate `package.Resources` instead.
- `ResourceKey` is `readonly record struct (uint ResourceType, uint ResourceGroup, ulong Instance)`: value equality, `IComparable`, `with` expressions work.
- Group and instance are NOT interchangeable with game ids: `RK.AreEquivalent` (in `TS4Tools.FileTable`) ignores the high 5 group bits and applies the 0x736884F1 instance mask the way legacy FileTable does. Plain `==` is exact.

**Sample 3: ResourceKey formats**

```csharp
// Sample 3: ResourceKey formatting and parsing
var key = new ResourceKey(0x220557DA, 0x00000000, 0x00000000DEADBEEF);
string modern = key.ToString();                // "T:0x220557DA G:0x00000000 I:0x00000000DEADBEEF"
ResourceKey a = ResourceKey.Parse(modern);
ResourceKey b = ResourceKey.Parse("0x220557DA-0x00000000-0x00000000DEADBEEF");  // legacy s4pe form
ResourceKey c = ResourceKey.Parse("220557DA:00000000:DEADBEEF");                // legacy, no 0x
ResourceKey d = ResourceKey.Parse("key:220557DA:00000000:DEADBEEF");
bool ok = ResourceKey.TryParse("garbage", out _);                               // false, never throws
```

`ResourceKey.ToString()` gives `T:0x........ G:0x........ I:0x................`. `Parse`/`TryParse` accept that form plus the legacy forms `0xT-0xG-0xI`, `T:G:I` and `key:T:G:I` (hex, case-insensitive). `RK.Format(key)` produces the `0xT-0xG-0xI` form that s4pe shows.

## 4. Reading raw data

`GetResourceDataAsync(entry)` returns the decompressed bytes (verified against the index size when `LibrarySettings.Checking` is true). Resources you replaced or added return the in-memory bytes. Clean data is read from the stream on demand and not cached, so call it once and keep the result. Entries must come from the same package (anything else throws `ArgumentException`).

## 5. Typed wrappers

`GetResourceAsync(entry)` runs the bytes through the registry (`package.ResourceHandlers`, default `ResourceHandlerRegistry.Shared`) and returns the registered wrapper, or a `DefaultResource` (raw bytes) for unknown types.

- `ResourceHandlerRegistry.Shared` is created lazily by `CreateDefault()`: it loads `TS4Tools.Wrappers` by name (it must be deployed next to your app and referenced) and scans loaded assemblies. If your tool never touches a Wrappers type before the first `GetResourceAsync`, the assembly may not be loaded; the by-name load handles that, but a trimmed or single-file publish may not.
- To be explicit, build a registry yourself: `new ResourceHandlerRegistry()` then `DiscoverHandlers(typeof(StblResourceFactory).Assembly)`. Assign it to `package.ResourceHandlers` (per package) or `ResourceHandlerRegistry.Shared` (process-wide).
- `GetResourceAsync(entry, alwaysDefault: true)` skips wrappers and returns the raw `DefaultResource`.
- `registry.CreateResource(key, data, alwaysDefault)`, `CreateNewResource(key)` (empty resource of that type), `GetWrapperType(type)`, `GetFactory(type)`, `RegisteredTypes`, `TypeMap`.
- Precedence when several factories claim a type: explicit `Register` calls, then higher `ResourceHandlerAttribute.Priority`, then attribute over type-list claims, then type name. `DisableWrapper`/`SetDisabledWrappers` turn off a wrapper and fall back to the next claim, then the default.
- Plugins: `registry.LoadWrappersFromFolder(folder)` returns a list of `WrapperLoadError`; factories are classes implementing `IResourceFactory` and tagged `[ResourceHandler(0xTYPE)]`.

**Sample 4: typed read and edit of a STBL**

```csharp
// Sample 4: typed access with an explicit registry, edit STBL, write back
package.ResourceHandlers = WrapperRegistry();   // or leave unset to use ResourceHandlerRegistry.Shared
var entry = package.Find(new ResourceKey(StblType, 0, 0x1))!;

var stbl = (StblResource)await package.GetResourceAsync(entry);
stbl[FnvHash.Fnv32("Hello")] = "Hello, edited";             // modify (indexer adds when missing)
stbl.Add(FnvHash.Fnv32("Goodbye"), "Goodbye");              // add (does NOT check duplicates)
stbl.Remove(FnvHash.Fnv32("Nope"));                         // false when absent
package.ReplaceResource(entry, stbl);                       // marks the package dirty
```

**Sample 5: bypass wrappers, ask the registry**

```csharp
// Sample 5: bypass wrappers
IResource raw = await package.GetResourceAsync(entry, alwaysDefault: true);   // DefaultResource, bytes only
var registry = ResourceHandlerRegistry.CreateDefault();                         // Wrappers found automatically
Type wrapper = registry.GetWrapperType(StblType);                               // StblResourceFactory
IResource fresh = registry.CreateNewResource(new ResourceKey(StblType, 0, 0x42)); // empty STBL
```

### Editing rules for typed resources

1. A wrapper is a snapshot. Mutating it does nothing to the package until you call `package.ReplaceResource(entry, resource)` (or `AddResource(key, resource)` for a new entry). Both copy `resource.Data` at that moment.
2. `resource.Data` re-serializes. Wrappers keep unknown bytes where they can, but a wrapper that does not round-trip a file byte-for-byte will rewrite it. Use `scan --roundtrip` (section 14) to find such types.
3. Dirty tracking: `package.IsDirty` becomes true on any add, replace, delete, re-key or `SetCompression`.

Resource-specific notes:

- **STBL** (`0x220557DA`, `StblResource`): `Entries`, `Count`, indexer `stbl[hash]` (get throws `KeyNotFoundException`; set replaces the value or adds), `TryGetValue`, `ContainsKey`, `Remove`, `Clear`, `Add(hash, value, flags)`. `Add` does not check for duplicate hashes. Key hash is `FnvHash.Fnv32(keyString)`. Only STBL version 5 parses. Anything else throws `ResourceFormatException`.
- **NameMap** (`0x0166038C`, `NameMapResource`): an `IDictionary<ulong,string>` (instance hash to name). `nmap.Add(FnvHash.Fnv64(name), name)`.
- **Text** (`TextResource`, many types including tuning XML `0x03B33DDF`, and `.xml`/`.txt`-like types listed in `TextResources.txt`): `Text` (string), `HasBom`, `IsXml`, `LineCount`. Original bytes are kept verbatim until you assign `Text`, so untouched binary-ish text types are safe.
- **SimData** (`SimDataResource`): parsed `Schemas` and `Tables`; read and write fields through `SimDataTable.GetUInt32/GetFloat/...` and `SetUInt32/SetFloat/...` with a `SimDataField` from `table.Schema.Fields`.
- **CASP** (`CasPartResource`, `0x034AEECB`): plain properties (`Name`, `BodyType`, `AgeGender`, `SwatchColors`, `LodBlocks`, ...). Other CAS types (`SkinToneResource`, `SimOutfitResource`, `StyleLookResource`, `GeomListResource`, ...) live in the same namespace.
- **Catalog** (`CobjResource` and ~20 siblings, all deriving `AbstractCatalogResource`): `CommonBlock` has `Price`, `NameHash`, `DescriptionHash`, `Tags`, `PackId`, ... `CatalogTagRegistry.FetchTag(id)`/`GetTagName(id)`/`AllTags()` map tag ids to names.
- **RCOL / GEOM / model chunks** (`RcolResource`): chunk list with `IRcolBlock`s; unknown block types are kept as `UnknownRcolBlock` bytes.
- **Images**: `ImageResource` covers DDS, DST (shuffled DDS, type `0x00B2D882`) and PNG; `ToDds()` gives a standard DDS, `ImportToDst(dds)` converts a DDS in, `GetDecodedPixels()` returns RGBA32. `DdsImage` (`Load`, `CreateSolid`, `FromRgba32`, `ToRgba32`, `SaveToDds`, `Resize`) edits pixels. `RleResource` (`0x3453CF95`, `0xBA856C78`): `ToDds()`, `ImportToRle(dds)`. There is no PNG encoder in the libraries; hand `ToRgba32()` to one of your own.

**Sample 6: create NameMap and Text resources, then edit**

```csharp
// Sample 6: create NameMap and text resources from scratch
var nmapKey = new ResourceKey(NameMapType, 0, 0x1);
var nmap = new NameMapResource(nmapKey, ReadOnlyMemory<byte>.Empty);
nmap.Add(FnvHash.Fnv64("my_object"), "my_object");          // instance hash -> readable name
package.AddResource(nmapKey, nmap);

var textKey = new ResourceKey(TuningType, 0, FnvHash.Fnv64("my_object"));
var text = new TextResource(textKey, ReadOnlyMemory<byte>.Empty) { Text = "<I c=\"x\" i=\"y\" m=\"z\" n=\"my_object\" s=\"1\"/>" };
var entry = package.AddResource(textKey, text)!;
text.Text = text.Text.Replace("\"1\"", "\"2\"", StringComparison.Ordinal);
package.ReplaceResource(entry, text);
```

**Sample 18: catalog object and tags**

```csharp
// Sample 18: catalog objects and tag names
var cobj = new CobjResource(new ResourceKey(CobjResource.TypeId, 0, 1), ReadOnlyMemory<byte>.Empty);
cobj.CommonBlock.Price = 250;
cobj.CommonBlock.NameHash = FnvHash.Fnv32("my_object_name");
ReadOnlyMemory<byte> bytes = cobj.Data;
var reparsed = new CobjResource(cobj.Key, bytes);
CatalogTag tag = CatalogTagRegistry.FetchTag(0x0001);   // unknown ids give Value == "unknown"
```

**Samples 19 and 20 (compile-checked only, they need real data): SimData field, CASP properties**

```csharp
// Sample 19: SimData field edit (call ReplaceResource afterwards)
var table = simData.Tables[0];
var field = table.Schema!.Fields.First(f => f.Name == "Price");
uint price = table.GetUInt32(0, field);
table.SetUInt32(0, field, price + 1);

// Sample 20: CASP is read through plain properties
string name = casp.Name;
_ = (name, casp.BodyType, casp.AgeGender, rcol);
```

## 6. Adding, compressing, deleting

- `AddResource(key, byte[]/ReadOnlyMemory<byte>, rejectDuplicates = true)` returns the new entry, or `null` when `rejectDuplicates` and a non-deleted entry with the key exists. `AddResource(key, IResource)` stores `resource.Data`. `AddResource(key)` adds an empty resource.
- New resources are stored RAW (uncompressed) unless you call `SetCompression(entry, 0x5A42)` (zlib). Compression is applied at save time and only used when it makes the data smaller. `SetCompression(entry, 0)` forces raw. Replacing data keeps the entry's existing compression type.
- `DeleteResource(entry)` only marks the entry deleted; `entry.IsDeleted` becomes true and `Find` skips it. You can undelete before saving through `((IMutableResourceIndexEntry)entry).IsDeleted = false`. Deleted entries are dropped when saved.
- `IMutableResourceIndexEntry.Key` is settable, which is how you re-key (rename) a resource. `entry.CompressionType` is settable too.
- `package.SortResources("Instance")` sorts the index (changes save order only; does not set `IsDirty`).

**Sample 7: add, compress, duplicates, delete**

```csharp
// Sample 7: add (raw by default), compress, duplicates, delete
IResourceIndexEntry entry = package.AddResource(key, data)!;                   // stored RAW
package.SetCompression(entry, 0x5A42);                                          // zlib; compressed on save if smaller
IResourceIndexEntry? dup = package.AddResource(key, data);                      // null: duplicate rejected
IResourceIndexEntry dup2 = package.AddResource(key, data, rejectDuplicates: false)!; // allowed
int n = package.FindAll(e => e.Key == key).Count();                             // 2 (Find returns the first)
package.DeleteResource(dup2);                                                   // reversible until saved
bool deleted = dup2.IsDeleted;                                                  // true; Find() now skips it
((IMutableResourceIndexEntry)dup2).IsDeleted = false;                           // undelete
package.DeleteResource(dup2);
```

Duplicate keys: legacy and the game tolerate them, `AddResource(..., rejectDuplicates: false)` creates them, `Find` returns the first, `FindAll` returns all. `unpack` numbers them (`.1`, `.2`, ...).

## 7. Saving

| Call | Writes to | `IsDirty` afterwards |
|---|---|---|
| `SaveAsync()` | back to the file it was opened from (needs `readWrite: true` and a file path) via a temp file, then copies over the original and re-reads the index | `false` |
| `SaveAsAsync(path)` | a new file (overwritten if it exists) | unchanged (still dirty, still backed by the original) |
| `SaveToStreamAsync(stream)` | any stream, never the package's own backing stream | unchanged |

- Untouched resources are copied verbatim from the source stream (same stored bytes, same compression), so saving does not re-compress or change data you did not edit.
- Saving a clean package still rewrites the file; check `IsDirty` first if you want to avoid that.
- `SaveAsync` on a stream-opened or new package throws `InvalidOperationException`.

**Sample 8: save in place, save as, save to stream**

```csharp
// Sample 8: save in place / save as / save to stream
await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
{
    var entry = package.Find(new ResourceKey(TuningType, 0, 0x2))!;
    package.ReplaceResource(entry, "<I n=\"changed\"/>"u8.ToArray());
    package.IsDirty.Should().BeTrue();

    await package.SaveAsAsync(copy);        // writes a new file; package stays dirty, original untouched
    package.IsDirty.Should().BeTrue();

    await package.SaveAsync();              // atomic temp-file save back to `path`; IsDirty -> false
    package.IsDirty.Should().BeFalse();

    using var ms = new MemoryStream();
    await package.SaveToStreamAsync(ms);    // never changes IsDirty
}
```

## 8. Hashing

The Sims 4 uses FNV-1 (multiply, then xor; not FNV-1a) over the lower-cased ASCII text. Source of truth: legacy `FNVHash.cs`.

- `FnvHash.Fnv32(string)`, `Fnv64(string)`, `Fnv24(string)`, `Fnv32Lower`, `Fnv64Lower`; plus byte-span overloads `Fnv32(ReadOnlySpan<byte>)` and `Fnv64(ReadOnlySpan<byte>)` that hash the bytes exactly as given (no lower-casing).
- The classes `FNV32`, `FNV64`, `FNV24`, `FNV64CLIP` (`GetHash(string)`, plus `HashAlgorithm` use) are the faithful ports. `FNV64CLIP.GetHash` is the CLIP instance id (age/species prefix folded into the top bits).
- String-table key: `FnvHash.Fnv32("my_key")` is the 32-bit `keyHash` in a STBL.
- Tuning and most game resources use `FnvHash.Fnv64(name)` as the instance id.
- STBL instance ids: by convention the top byte is the locale (`0x00` English) and the lower 56 bits are the hash of a name. The libraries do not enforce this; match what the base game uses for the locale you target.

**Sample 9: hashing**

```csharp
// Sample 9: hashing (FNV-1, input lower-cased)
uint stringKey = FnvHash.Fnv32("Mod:My_String");          // STBL key hash
ulong instance = FnvHash.Fnv64("my_object_tuning");       // tuning/CAS instance (64-bit)
uint viaClass = FNV32.GetHash("MY_OBJECT");               // same as FNV32 of "my_object"
ulong clip = FNV64CLIP.GetHash("a2o_idle_x");             // CLIP instance (age/species prefix folded in)
uint h24 = FnvHash.Fnv24("abc");
// STBL instance convention: top byte = locale (0x00 = English), low 56 bits = hash of a name
ulong stblInstance = (0x00UL << 56) | (FnvHash.Fnv64("my_stbl") & 0x00FFFFFFFFFFFFFF);
```

## 9. Type tags and extensions

`ExtList.GetTag(type)` gives the short tag (`STBL`), `GetExtension(type)` the extension (`.stbl`), unknown types give `UNKN`/`.bnry`, `IsKnown(type)` tells them apart, `KnownTypes` lists everything. Data is the verbatim legacy `Extensions.txt`.

**Sample 10: ExtList**

```csharp
// Sample 10: tags and extensions
string tag = ExtList.GetTag(0x220557DA);          // "STBL"
string ext = ExtList.GetExtension(0x220557DA);    // ".stbl"
string unknown = ExtList.GetExtension(0xDEADBEEF);// ".bnry"
bool known = ExtList.IsKnown(0xDEADBEEF);        // false
```

## 10. Limits, safety and errors

- NEVER write to the game install folder (`.../The Sims 4/Data`, `GameVersion`, etc.). Open game packages read-only; edit copies (`SaveAsAsync` to a scratch folder) and only then place them in the Mods folder. Prefer `SaveAsAsync` over in-place `SaveAsync` for anything you did not create.
- Validate input before trusting it. Counts, offsets and sizes are checked against `PackageLimits`; apply the same discipline to your own file parsing (CLAUDE.md "Security for .package Parsing").
- `LibrarySettings.Checking` (default true, matches legacy) enables header and decompressed-size checks. Leave it on; the bounds checks do not depend on it.
- Wrappers throw `ResourceFormatException` for data they cannot parse. `UnsupportedResourceVersionException` (derives from it, in `TS4Tools.Wrappers`) means the resource is a version newer than legacy knows (for example a newer CASP). Legacy would silently misparse and corrupt such files on save, so the port refuses. Catch it and fall back to raw bytes (and copy the bytes untouched if you only need to move the resource).
- Wrappers: CASP family throws `UnsupportedResourceVersionException`; STBL throws plain `ResourceFormatException` for versions other than 5.
- `ResourceNotFoundException(key)` exists for callers that want a typed not-found error; `Find` itself returns `null`.

**Sample 11: library settings**

```csharp
// Sample 11: library-wide switch; defaults (true) match legacy
LibrarySettings.Checking = true;
```

**Sample 12: safe typed read with raw fallback**

```csharp
// Sample 12: wrappers may refuse data they cannot round-trip; fall back to raw bytes
static async Task<ReadOnlyMemory<byte>> SafeBytesAsync(DbpfPackage package, IResourceIndexEntry entry)
{
    try
    {
        var resource = await package.GetResourceAsync(entry);
        return resource.Data;
    }
    catch (UnsupportedResourceVersionException)   // newer than the legacy layout (e.g. CASP)
    {
        return await package.GetResourceDataAsync(entry);   // untouched raw bytes
    }
    catch (ResourceFormatException)               // malformed or unsupported content
    {
        return await package.GetResourceDataAsync(entry);
    }
}
```

## 11. Thread safety

- `DbpfPackage` serializes reads of the backing stream with an internal lock, so concurrent `GetResourceDataAsync` calls on one package are safe (the CLI's `scan --parallel` still parallelizes across separate packages, one `DbpfPackage` per file).
- Mutations (`Add/Replace/Delete/SetCompression`, `Save*`, entry property sets) are not synchronized. Use one writer at a time.
- Wrapper objects (`StblResource`, ...) are not thread safe. `ResourceHandlerRegistry` is thread-safe for lookups and registration. `ResourceHandlerRegistry.Shared` and `LibrarySettings` are process-wide statics.
- Do not open the same file read-write from two processes.

## 12. Recipes

**Sample 13 and 14: copy resources between packages, merge STBLs**

```csharp
// Sample 13: copy resources between packages (decompressed bytes in, compressed again on request)
foreach (var entry in src.FindAll(e => e.Key.ResourceType == TuningType).ToList())
{
    var data = await src.GetResourceDataAsync(entry);
    var added = dst.AddResource(entry.Key, data, rejectDuplicates: true);
    if (added is not null) dst.SetCompression(added, 0x5A42);
}

// Sample 14: merge STBLs (first definition of a key hash wins)
var merged = new StblResource(new ResourceKey(StblType, 0, 0x99), ReadOnlyMemory<byte>.Empty);
foreach (var entry in src.FindAll(e => e.Key.ResourceType == StblType).ToList())
{
    var stbl = new StblResource(entry.Key, await src.GetResourceDataAsync(entry));
    foreach (var s in stbl.Entries)
        if (!merged.ContainsKey(s.KeyHash))
            merged.Add(s.KeyHash, s.Value, s.Flags);
}
dst.AddResource(merged.Key, merged);
```

**Sample 15: bulk re-key (rename) resources**

```csharp
// Sample 15: bulk-rename (re-key) resources: IMutableResourceIndexEntry.Key is settable
foreach (var entry in package.FindAll(e => e.Key.ResourceType == 1).ToList())
{
    var mutable = (IMutableResourceIndexEntry)entry;
    mutable.Key = entry.Key with { Instance = entry.Key.Instance + 0x1000 };
}
```

To rename by name rather than by arithmetic: compute the new instance with `FnvHash.Fnv64(newName)`, set `Key`, and update the NameMap entry (`nmap.Remove(old)` then `nmap.Add(new, newName)`).

**Sample 16: extract all images as DDS**

```csharp
// Sample 16: extract every DDS/DST image as a standard .dds file
Directory.CreateDirectory(outDir);
foreach (var entry in package.FindAll(e => e.Key.ResourceType == 0x00B2D882).ToList())
{
    var image = (ImageResource)await package.GetResourceAsync(entry);
    byte[]? dds = image.ToDds();                 // DST is un-shuffled into plain DDS
    if (dds is null) continue;
    await File.WriteAllBytesAsync(Path.Combine(outDir, $"{entry.Key.Instance:X16}.dds"), dds);
    // For pixels: DdsImage.Load(dds).ToRgba32() gives R,G,B,A bytes to hand to any PNG encoder.
}
```

**Sample 17: find which package contains a key (file table)**

```csharp
// Sample 17: which package contains a key? (point CustomContentPath at any folder, e.g. a Mods dir)
await using var table = new GameFileTable(new GameFolders())
{
    CustomContentPath = dir,
    CustomContentEnabled = true,
};
SpecificResource? hit = await table.FindAsync(new ResourceKey(TuningType, 0, 0x77), FileTableKind.GameContent);
Console.WriteLine(hit?.PathPackage.Path);       // the .package that holds it
ReadOnlyMemory<byte>? data = hit is null ? null : await hit.GetDataAsync();
```

`GameFileTable` searches `Current`, then custom content (`CustomContentPath` recursively, if `CustomContentEnabled`), then installed game content (`FileTableEnabled`; install locations come from `GameFolders`/the registry/`GameFolders.xml`). Pass `FileTableKind.DDSImages` or `Thumbnails` for the other lists. Lookup uses `RK` equivalence, so group version bits are ignored. The table opens packages read-only, lazily, and `DisposeAsync` closes them.

## 13. Gotchas that trip agents

- `AddResource(key, [1, 2])` does not compile: `ReadOnlyMemory<byte>` cannot be built from a collection expression. Pass a `byte[]` (`new byte[] { 1, 2 }`).
- `GetResourceAsync` returns `IResource`; cast to the wrapper (`(StblResource)`), or pattern-match, and expect `DefaultResource` when the wrapper is not registered (for instance, when `TS4Tools.Wrappers` was never loaded into your process).
- `ReplaceResource(entry, wrapper)` is mandatory after editing a wrapper; the package never looks at wrappers on save.
- `FindAll` is lazy over a list you are about to mutate. Call `.ToList()` before adding or deleting inside the loop.
- All `Find`/`FindAll` overloads skip deleted entries; `Resources` and `ResourceCount` do not.
- `SaveAsAsync` leaves the package dirty and bound to the original file. Reopen the new file if you want to keep working on it.
- The namespace `TS4Tools.Wrappers.CasPartResource` has the same name as its main class `CasPartResource`: add a `using` for the namespace (the class is not in `TS4Tools.Wrappers` itself). The catalog types are in `TS4Tools.Wrappers.CatalogResource`.
- Group/instance values are unsigned; `ResourceKey` hex strings in manifests are 8/8/16 digits.

## 14. Headless CLI (`ts4tools`)

The CLI lives in `src/TS4Tools.Cli` (assembly `ts4tools`). See `ts4tools --help` / `ts4tools <command> --help` for the authoritative option list; if they disagree with this section, the help output wins. `AGENTS.md` has a quick-start.

Run it with `dotnet run --project src/TS4Tools.Cli -- <command> ...`, or build it (`dotnet build src/TS4Tools.Cli -c Release`) and call `src/TS4Tools.Cli/bin/Release/net10.0/ts4tools`. Commands:

```
ts4tools info    <package> [--json]
ts4tools list    <package> [--type 0xTTTTTTTT] [--json]
ts4tools unpack  <package> <outdir> [--force] [--parallel N]
ts4tools pack    <dir> <package> [--force] [--no-compress-new]
ts4tools compare <a.package> <b.package>
ts4tools scan    <path>... [--json <file>] [--markdown <file>] [--roundtrip] [--parallel N] [--types 0xT,0xT] [--all] [--quiet]
ts4tools extract <package> <key> <file>
ts4tools replace <package> <key> <file> [--output <package>]
```

`<key>` accepts every `ResourceKey.Parse` form (for example `0x220557DA-0x00000000-0x00000000DEADBEEF`).

### unpack layout

`ts4tools unpack in.package out/` writes:

- `out/manifest.json`
- `out/resources/<TTTTTTTT>_<GGGGGGGG>_<IIIIIIIIIIIIIIII>[.<n>]<ext>`: decompressed bytes. Type, group, instance are zero-padded upper-case hex. `<ext>` comes from `ExtList` (unknown types get `.bnry`). `.<n>` appears only for duplicate keys.

Manifest (formatVersion 1):

```json
{
  "formatVersion": 1,
  "source": "in.package",
  "header": { "magic": "DBPF", "major": 2, "minor": 1, "userVersionMajor": 0, "userVersionMinor": 0,
              "unused1": 0, "creationTime": 0, "updatedTime": 0, "unused2": 0,
              "indexCount": 1, "indexRecordPositionLow": 0, "indexSize": 32, "unused3": 0, "unused4": 3,
              "indexPosition": 96, "unused5": "<56 hex digits>", "indexType": "0x00000007" },
  "resources": [
    { "type": "0x220557DA", "group": "0x00000000", "instance": "0x00000000DEADBEEF",
      "file": "resources/220557DA_00000000_00000000DEADBEEF.stbl",
      "compressionType": "0x5A42", "unknown2": 1, "memSize": 1234,
      "sha256": "<lowercase hex of the decompressed bytes>" }
  ]
}
```

`type`, `group` and `instance` are hex strings. Resources are listed in index order. `compressionType` is the index label as a hex string (`0x0000` raw, `0x5A42` ZLIB, `0xFFFF` RefPack, `0xFFE0` deletion record). `unused5` is the 28 header bytes at offset 68 as 56 hex digits. The index fields (`indexCount`, `indexSize`, `indexPosition`, `indexType`, `indexRecordPositionLow`) are informational; `pack` recomputes them.

### pack behavior

- Resources are written in manifest order. Edited files are picked up. The header is restored from the manifest.
- Files in `resources/` that are not in the manifest are added; the key comes from the file name, and they are compressed by default (`--no-compress-new` stores them raw).
- Manifest entries whose file is missing are dropped, with a warning.
- Packing an unchanged directory yields a package identical resource-for-resource to the original (`ts4tools compare` proves it), with one exception: RefPack (`0xFFFF`) resources are re-encoded as ZLIB (`0x5A42`) because, like legacy, TS4Tools has no RefPack compressor; `pack` warns and `compare` reports the label change.
- An existing output package is refused unless `--force`; output goes to a temp file first. Manifest paths that escape the directory are rejected. File names may also use the legacy `S4_` export prefix.
- `--force` (on `unpack` and `pack`) allows overwriting existing output.

### scan

Classifies every resource in each package (or every `*.package` under a directory):

| class | meaning |
|---|---|
| `ok` | wrapper parsed it (and, with `--roundtrip`, re-serialized identical bytes) |
| `unwrapped` | no wrapper registered for the type (raw only) |
| `unsupported-version` | `UnsupportedResourceVersionException` (newer than legacy) |
| `parse-error` | wrapper threw while parsing |
| `roundtrip-mismatch` | with `--roundtrip`, wrapper output differs from input |
| `empty` | zero-length resource (e.g. a `0xFFE0` deletion record); not a problem |

Exit code is 1 when any resource is `unwrapped`, `unsupported-version`, `parse-error` or `roundtrip-mismatch`, or a package fails to open. `--json f` and `--markdown f` write reports, `--parallel N` sets package-level parallelism, `--types 0x..` restricts types.
`scripts/scan-unsupported.sh [gameDir]` runs a scan over the game install (read-only) and writes `docs/unsupported-resources.md`.

### extract / replace

- `ts4tools extract <package> <key> <file>` writes the decompressed bytes of one resource.
- `ts4tools replace <package> <key> <file> [--output <package>]` swaps one resource's bytes for the file. Without `--output` it edits `<package>` in place. Always pass `--output` for anything that is not your own scratch copy.

### Agent workflow: edit a package without writing C#

1. `ts4tools info my.package` and `ts4tools list my.package --type 0x03B33DDF --json` to see what is inside.
2. `ts4tools unpack my.package work/`.
3. Edit files in `work/resources/`:
   - tuning and other XML/text: edit them as text (keep UTF-8, keep line endings);
   - STBL: do not hand-edit the binary; use the library (Samples 4 and 14) to write a new `.stbl` into `work/resources/`, or `extract` one, edit it in a small C# script and `replace` it;
   - other binary: hex-edit with care, keeping sizes consistent with the format.
4. `ts4tools pack work/ out.package` (add new files named `<T>_<G>_<I>.<ext>` to add resources).
5. Verify: `ts4tools compare my.package out.package` to review exactly what changed, then `ts4tools scan out.package --roundtrip`. Note that scan exits 1 whenever any resource is merely unwrapped or newer than legacy, which is normal for game data; compare its per-type counts against a scan of the original rather than relying on the exit code.

## 15. Further reading

- `CLAUDE.md` and `AGENTS.md`: repo rules and build commands.
- `docs/real-data-validation.md`: how the library was validated against real packages.
- `docs/unsupported-resources.md`: generated list of resource types and versions the wrappers cannot handle (created by `scripts/scan-unsupported.sh`).
- `docs/legacy-analysis/`: notes on the legacy s4pi/s4pe code.
