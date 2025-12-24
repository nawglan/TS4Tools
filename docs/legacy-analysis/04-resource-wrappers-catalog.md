# Resource Wrappers Catalog

This document catalogs all 22 resource wrapper projects in the s4pi library.

## Overview

The `legacy_references/Sims4Tools/s4pi Wrappers/` directory contains wrapper assemblies that understand specific Sims 4 resource formats.

**Statistics:**
- 22 wrapper projects
- 50+ AResourceHandler implementations
- 90+ unique ResourceType codes
- Formats range from simple text to complex 3D geometry

## Wrapper Catalog

### 1. DefaultResource
**ResourceType:** `*` (wildcard fallback)

Minimal wrapper for unknown resource types. Provides raw byte access without parsing.

---

### 2. StblResource (String Table)
**ResourceType:** `0x220557DA`

Localization strings with FNV hash keys.

**Format:**
```
Header: "STBL" magic, version (5), entry count
Entries: [hash:4][flags:1][length:2][UTF-8 string]
```

---

### 3. NameMapResource
**ResourceType:** `0x0166038C`

Maps FNV64 hashes to human-readable names. Essential for debugging.

---

### 4. TextResource
**ResourceTypes:** Multiple (config-based)

Generic text/XML file wrapper.

---

### 5. ImageResource
**ResourceTypes:**
- `0x00B2D882` - DST
- `0x3453CF95`, `0xBA856C78` - RLE compressed
- `0x0D338A3A`, `0x16CCF748`, `0x3BD45407`, `0x3C1AF1F2`, `0x3C2A8647`, `0x5B282D45`, `0xCD9DE247`, `0xE18CAEE2`, `0xE254AE6E` - Thumbnails

Handles DDS, RLE-compressed, and thumbnail images.

---

### 6. CASPartResource (Create-a-Sim Parts)
**ResourceTypes:**
- `0x034AEECB` - CAS Part
- `0x00AE6C67` - Bone
- `0xAC16FBEC` - GEOM List
- `0x0354796A` - Skin Tone
- `0x71BDB8A2` - Style Look
- `0xDB43E069` - Deformer Map
- `0x025ED6F4` - Sim Outfit

Complex wrapper system for clothing, accessories, bones, and body data.

---

### 7. CatalogResource (Build/Buy Catalog)
**ResourceTypes:**
- `0x07936CE0` - CBLK (Build Item)
- `0x1D6DF1CF` - CCOL (Color)
- `0x0418FE2A` - CFEN (Fence)
- `0xB4F762C9` - CFLR (Floor)
- `0x84C23219` - CFLT (Flooring)
- `0x2FAE983E` - CFND (Foundation)
- `0xA057811C` - CFRZ (Fireplace)
- `0xE7ADA79D` - CFTR (Furniture)
- `0x319E4F1D` - COBJ (Object) - most common
- `0xA5DFFCF3` - CPLT (Palette)
- `0x1C1CF1F7` - CRAL (Railing)
- `0xF1EDBD86` - CRPT (Roof Pattern)
- `0xB0311D0F` - CRTR (Roof Trim)
- `0x3F0C529A` - CSPN (Spawn Point)
- `0x9F5CFF10` - CSTL (Stairs)
- `0x9A20CD1C` - CSTR (Stair Railing)
- `0xEBCBB16C` - CTPT (Top)
- `0xD5F0F921` - CWAL (Wall)
- `0xC0DB5AE7` - Object Definition
- `0x91EDBD3E` - Roof Style
- `0x74050B1F` - STRM (Stream)
- `0x48C28979`, `0xA8F7B517` - Other catalog types

24 different catalog resource types for Build/Buy mode items.

---

### 8. MeshChunks (3D Geometry)
**ResourceType:** `0x015A1849` (GEOM)

3D mesh data: vertices, indices, bones, UV mapping, blend weights.

---

### 9. RigResource (Skeleton)
**ResourceType:** `0x8EAF13DE`

Bone hierarchy, IK chains, and skeletal structure for animation.

---

### 10. AnimationResources
**ResourceType:** `0x6B20C4F3`

Animation clips with keyframes, events, and IK data.

---

### 11. GenericRCOLResource
**ResourceTypes:** Dynamic (config-based)

Container format for RCOL (Resource Container Object List) data. Uses pluggable block handlers.

---

### 12. s4piRCOLChunks
RCOL block types (used within GenericRCOLResource):
- FTPT (Footprint)
- LITE (Light)
- MATD (Material Definition)
- MTNF (Material Name/Flag)
- MTST (Material State)
- RSLT (Result)
- VPXY (Vertex Proxy)
- ShaderData
- SlotAdjust

---

### 13. ScriptResource
**ResourceType:** `0x073FAA07`

Encrypted Python scripts with MD5 verification.

---

### 14. TxtcResource (Texture Compositor)
**ResourceTypes:** `0x033A1435`, `0x0341ACC9`

Texture layering and composition instructions.

---

### 15. ComplateResource
**ResourceType:** `0x044AE110`

Color palette/template resources.

---

### 16. DataResource
**ResourceType:** `0x545AC67A`

Generic structured data container.

---

### 17. ModularResource
**ResourceType:** `0xCF9A4ACE`

Modular container with TGI block references.

---

### 18. ObjKeyResource
**ResourceType:** `0x02DC343F`

Object key for catalog entries.

---

### 19. NGMPHashMapResource
**ResourceType:** `0xF3A38370`

Hash map for material properties.

---

### 20. MiscellaneousResource
**ResourceTypes:**
- `0xBDD82221` - AUEV (Audio Event)
- `0x81CA1A10` - MTBL (Model Table)
- `0xC5F6763E` - Sim Modifier
- `0x16CA6BC4` - THUM (Thumbnail)
- `0xB0118C15` - TMLT (Timeline)
- `0x76BCF80C` - TRIM
- `0x71A449C9` - Skybox Texture
- `0x19301120` - World Color Timeline

Mixed resource types for modifiers, timelines, and visual elements.

---

### 21. UserCAStPresetResource
**ResourceType:** `0x0591B1AF`

Saved CAS appearance presets.

---

### 22. JazzResource
Inherits from GenericRCOLResource. Animation/audio RCOL chunks.

---

## Common Wrapper Patterns

### Parse/UnParse

```csharp
public class MyResource : AResource
{
    private void Parse(Stream s)
    {
        BinaryReader r = new BinaryReader(s);
        magic = r.ReadUInt32();
        version = r.ReadUInt16();
        // ... read fields
    }

    protected override Stream UnParse()
    {
        MemoryStream ms = new MemoryStream();
        BinaryWriter w = new BinaryWriter(ms);
        w.Write(magic);
        w.Write(version);
        // ... write fields
        return ms;
    }
}
```

### Nested Elements with DependentList

```csharp
public class EntryList : DependentList<Entry>
{
    protected override Entry CreateElement(Stream s)
        => new Entry(1, handler, s);

    protected override void WriteElement(Stream s, Entry e)
        => e.UnParse(s);
}
```

### TGI References

Many wrappers use TGIBlock lists for cross-references:

```csharp
[TGIBlockListContentField("TGIBlocks")]
public int TextureIndex { get; set; }

public DependentList<TGIBlock> TGIBlocks { get; set; }
```

## Rewrite Priorities

### Phase 1: Core (Essential)
1. DefaultResource - Fallback for unknown types
2. StblResource - Localization
3. NameMapResource - Debugging
4. TextResource - Configuration files

### Phase 2: Content (High Value)
1. CASPartResource - Character customization
2. CatalogResource - Build/Buy items
3. ImageResource - Textures/thumbnails
4. MeshChunks - 3D models

### Phase 3: Advanced (Complete Coverage)
1. RigResource - Skeletons
2. AnimationResources - Animations
3. GenericRCOLResource + chunks
4. Remaining wrappers

## Rewrite Recommendations

### Simplify Wrapper Registration

Replace reflection-based discovery:

```csharp
// Old: Handler class + reflection
public class StblResourceHandler : AResourceHandler
{
    public StblResourceHandler()
    {
        Add(typeof(StblResource), new List<string> { "0x220557DA" });
    }
}

// New: Attribute-based
[ResourceType(0x220557DA)]
public class StblResource : IResource { }
```

### Modern Binary Parsing

```csharp
// Old
uint magic = reader.ReadUInt32();
ushort version = reader.ReadUInt16();

// New
Span<byte> header = stackalloc byte[6];
stream.ReadExactly(header);
uint magic = BinaryPrimitives.ReadUInt32LittleEndian(header);
ushort version = BinaryPrimitives.ReadUInt16LittleEndian(header[4..]);
```

### Async Support

```csharp
public interface IResourceWrapper
{
    ValueTask ParseAsync(Stream s, CancellationToken ct = default);
    ValueTask<Stream> UnParseAsync(CancellationToken ct = default);
}
```

### Validation

```csharp
public class StblResource
{
    private void Parse(Stream s)
    {
        var magic = reader.ReadUInt32();
        if (magic != STBL_MAGIC)
            throw new ResourceFormatException(
                $"Invalid STBL magic: expected 0x{STBL_MAGIC:X8}, got 0x{magic:X8}");

        var version = reader.ReadUInt16();
        if (version != 5)
            throw new ResourceFormatException(
                $"Unsupported STBL version: {version}");

        var entryCount = reader.ReadUInt64();
        if (entryCount > MAX_ENTRIES)
            throw new ResourceFormatException(
                $"Entry count exceeds limit: {entryCount} > {MAX_ENTRIES}");
    }
}
```
