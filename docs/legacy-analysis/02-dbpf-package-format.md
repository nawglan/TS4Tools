# DBPF Package Format Analysis

This document analyzes the DBPF (Database Packed File) format used by The Sims 4 for `.package` files, based on the s4pi implementation.

## Source Files

- `legacy_references/Sims4Tools/s4pi/Package/Package.cs` (677 lines)
- `legacy_references/Sims4Tools/s4pi/Package/PackageIndex.cs` (153 lines)
- `legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs` (265 lines)
- `legacy_references/Sims4Tools/s4pi/Package/Compression.cs` (233 lines)

## DBPF Header Format

The package header is exactly **96 bytes** (0x60).

### Header Layout

| Offset | Size | Field | Value/Description |
|--------|------|-------|-------------------|
| 0x00 | 4 | Magic | "DBPF" (ASCII) |
| 0x04 | 4 | Major | 2 (for Sims 4) |
| 0x08 | 4 | Minor | 1 |
| 0x0C | 4 | UserVersionMajor | 0 (unused) |
| 0x10 | 4 | UserVersionMinor | 0 (unused) |
| 0x14 | 4 | Unused1 | 0 |
| 0x18 | 4 | CreationTime | Unix timestamp (usually 0) |
| 0x1C | 4 | UpdatedTime | Unix timestamp (usually 0) |
| 0x20 | 4 | Unused2 | 0 |
| 0x24 | 4 | Indexcount | Number of index entries |
| 0x28 | 4 | IndexRecordPositionLow | Legacy/alternate index position |
| 0x2C | 4 | Indexsize | Size of index in bytes |
| 0x30 | 12 | Unused3 | Padding |
| 0x3C | 4 | Unused4 | Always 3 (historical) |
| 0x40 | 4 | Indexposition | Byte offset to index |
| 0x44 | 28 | Unused5 | Padding to 96 bytes |

### Header Reading (Package.cs:262-338)

```csharp
// Key header fields
public override byte[] Magic { get {
    byte[] res = new byte[4];
    Array.Copy(header, 0, res, 0, res.Length);
    return res;
} }

public override int Major { get { return BitConverter.ToInt32(header, 4); } }
public override int Minor { get { return BitConverter.ToInt32(header, 8); } }
public override int Indexcount { get { return BitConverter.ToInt32(header, 36); } }
public override int Indexsize { get { return BitConverter.ToInt32(header, 44); } }

// Index position: use 0x40 if set, else fall back to 0x28
public override int Indexposition { get {
    int i = BitConverter.ToInt32(header, 64);
    return i != 0 ? i : BitConverter.ToInt32(header, 40);
} }
```

### Header Validation (Package.cs:607-620)

```csharp
void CheckHeader()
{
    if (header.Length != 96)
        throw new InvalidDataException("Hit unexpected end of file.");
    if (bytesToString(Magic) != "DBPF")
        throw new InvalidDataException("Expected magic tag 'DBPF'.");
    if (!majors.Contains(Major))  // majors = { 2 }
        throw new InvalidDataException("Expected major version(s) '2'.");
    if (Minor != 1)
        throw new InvalidDataException("Expected minor version '1'.");
}
```

## Index Format

The index is located at `Indexposition` and contains `Indexcount` entries.

### Index Type Flags

The first 4 bytes of the index are the `indextype` flags:

| Bit | Meaning |
|-----|---------|
| 0x01 | ResourceType is constant (in header, not per-entry) |
| 0x02 | ResourceGroup is constant (in header, not per-entry) |
| 0x04 | InstanceHigh is constant (in header, not per-entry) |

**Optimization:** If all resources share the same Type, Group, or Instance high bits, they're stored once in the index header rather than repeated per entry.

### Index Entry Layout (36 bytes when fully expanded)

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0x00 | 4 | IndexType | Index format flags |
| 0x04 | 4 | ResourceType | Resource type identifier |
| 0x08 | 4 | ResourceGroup | Resource group identifier |
| 0x0C | 4 | InstanceHigh | Upper 32 bits of Instance |
| 0x10 | 4 | InstanceLow | Lower 32 bits of Instance |
| 0x14 | 4 | Chunkoffset | Byte offset to resource data |
| 0x18 | 4 | Filesize | Compressed size (bit 31 always set) |
| 0x1C | 4 | Memsize | Uncompressed size |
| 0x20 | 2 | Compressed | Compression flag |
| 0x22 | 2 | Unknown2 | Always 0x0001 |

### Index Reading (PackageIndex.cs:50-72)

```csharp
public PackageIndex(Stream s, Int32 indexposition, Int32 indexsize, Int32 indexcount)
{
    s.Position = indexposition;
    indextype = r.ReadUInt32();

    // Header size depends on which fields are constant
    Int32[] hdr = new Int32[Hdrsize];  // 1-4 ints
    Int32[] entry = new Int32[numFields - Hdrsize];  // 5-8 ints per entry

    hdr[0] = (int)indextype;
    for (int i = 1; i < hdr.Length; i++)
        hdr[i] = r.ReadInt32();

    for (int i = 0; i < indexcount; i++)
    {
        for (int j = 0; j < entry.Length; j++)
            entry[j] = r.ReadInt32();
        base.Add(new ResourceIndexEntry(hdr, entry));
    }
}
```

### ResourceIndexEntry Expansion (ResourceIndexEntry.cs:205-225)

The index entry is expanded from the compact form to a full 36-byte array:

```csharp
internal ResourceIndexEntry(Int32[] header, Int32[] entry)
{
    indexEntry = new byte[(header.Length + entry.Length) * 4];
    BinaryWriter w = new BinaryWriter(new MemoryStream(indexEntry));

    w.Write(header[0]);  // indexType

    uint IhGT = (uint)header[0];
    // For each field, use header value if constant, else entry value
    w.Write((IhGT & 0x01) != 0 ? header[hc++] : entry[ec++]);  // Type
    w.Write((IhGT & 0x02) != 0 ? header[hc++] : entry[ec++]);  // Group
    w.Write((IhGT & 0x04) != 0 ? header[hc++] : entry[ec++]);  // InstanceHigh
    // ... remaining fields from entry
}
```

## Compression

### Supported Formats

1. **ZLIB/DEFLATE** (preferred, header byte 0x78)
   - Standard zlib compression
   - Uses SharpZipLib's `InflaterInputStream`/`DeflaterOutputStream`

2. **RefPack/QFS** (legacy, header byte2 0xFB)
   - EA's proprietary compression from earlier games
   - Custom LZ77-variant decompression

### Compression Detection (Compression.cs:35-79)

```csharp
public static byte[] UncompressStream(Stream stream, int filesize, int memsize)
{
    byte[] header = r.ReadBytes(2);

    if (header[0] == 0x78)  // ZLIB
    {
        // Standard DEFLATE decompression
        using (InflaterInputStream decomp = new InflaterInputStream(source))
        {
            uncompressedData = new byte[memsize];
            decomp.Read(uncompressedData, 0, memsize);
        }
    }
    else if (header[1] == 0xFB)  // RefPack
    {
        uncompressedData = OldDecompress(stream, header[0]);
    }
}
```

### Compression Flag Values

When writing:
- `Compressed = 0x5A42` ("ZB") - ZLIB compressed
- `Compressed = 0x0000` - Uncompressed

When reading:
- If `Filesize == Memsize` → Uncompressed
- If `Filesize != Memsize` → Compressed

### RefPack Decompression (Compression.cs:92-168)

The legacy EA compression uses control bytes:
- `0x00-0x7F`: 2-byte command (copy 0-3 plain + 3-10 back-ref)
- `0x80-0xBF`: 3-byte command (copy 0-3 plain + 4-67 back-ref)
- `0xC0-0xDF`: 4-byte command (copy 0-3 plain + 5-1028 back-ref)
- `0xE0-0xFB`: Plain text run (4-112 bytes)
- `0xFC-0xFF`: End marker (0-3 final bytes)

## Resource Data Access

### Reading Resources (Package.cs:649-673)

```csharp
public override Stream GetResource(IResourceIndexEntry rc)
{
    ResourceIndexEntry rie = rc as ResourceIndexEntry;

    // If dirty, return the in-memory stream
    if (rie.ResourceStream != null) return rie.ResourceStream;

    // Special cases
    if (rc.Chunkoffset == 0xffffffff) return null;  // New, unsaved
    if (rc.Filesize == 1 && rc.Memsize == 0xFFFFFFFF) return null;  // Deleted

    packageStream.Position = rc.Chunkoffset;

    byte[] data;
    if (rc.Filesize == rc.Memsize)
    {
        // Uncompressed
        data = r.ReadBytes((int)rc.Filesize);
    }
    else
    {
        // Compressed
        data = Compression.UncompressStream(packageStream,
            (int)rc.Filesize, (int)rc.Memsize);
    }

    return new MemoryStream(data);
}
```

### Writing Resources (Package.cs:96-147)

```csharp
public override void SaveAs(Stream s)
{
    w.Write(header);

    // Determine optimal index type (which fields can be constants)
    List<uint> lT = new List<uint>(), lG = new List<uint>(), lIh = new List<uint>();
    this.Index.ForEach(x => {
        if (!lT.Contains(x.ResourceType)) lT.Add(x.ResourceType);
        if (!lG.Contains(x.ResourceGroup)) lG.Add(x.ResourceGroup);
        if (!lIh.Contains((uint)(x.Instance >> 32))) lIh.Add((uint)(x.Instance >> 32));
    });
    uint indexType = (lIh.Count <= 1 ? 0x04 : 0) |
                     (lG.Count <= 1 ? 0x02 : 0) |
                     (lT.Count <= 1 ? 0x01 : 0);

    PackageIndex newIndex = new PackageIndex(indexType);
    foreach (IResourceIndexEntry ie in this.Index)
    {
        if (ie.IsDeleted) continue;

        ResourceIndexEntry newIE = (ie as ResourceIndexEntry).Clone();
        newIndex.Add(newIE);

        byte[] value = packedChunk(ie);  // Compress if beneficial
        newIE.Chunkoffset = (uint)s.Position;
        w.Write(value);

        if (value.Length < newIE.Memsize)
        {
            newIE.Compressed = 0x5A42;  // Mark as compressed
            newIE.Filesize = (uint)value.Length;
        }
        else
        {
            newIE.Compressed = 0x0000;
            newIE.Filesize = newIE.Memsize;
        }
    }

    long indexpos = s.Position;
    newIndex.Save(w);

    // Update header with final values
    setIndexcount(w, newIndex.Count);
    setIndexsize(w, newIndex.Size);
    setIndexposition(w, (int)indexpos);
}
```

## Package Lifecycle

### Opening (Package.cs:237-240)

```csharp
public static new IPackage OpenPackage(int APIversion, string packagePath, bool readwrite)
{
    return new Package(APIversion, new FileStream(packagePath,
        FileMode.Open,
        readwrite ? FileAccess.ReadWrite : FileAccess.Read,
        FileShare.ReadWrite));
}
```

### Creating New (Package.cs:527-545)

```csharp
private Package(int requestedVersion, int major = 2)
{
    using (MemoryStream ms = new MemoryStream(new byte[headerSize]))
    {
        BinaryWriter bw = new BinaryWriter(ms);
        bw.Write(stringToBytes("DBPF"));
        bw.Write(major);  // 2
        bw.Write(minor);  // 1
        setIndexsize(bw, (new PackageIndex()).Size);
        setIndexversion(bw);
        setIndexposition(bw, headerSize);  // Index starts at 96
        setUnused4(bw, 3);
        header = ms.ToArray();
    }
}
```

### Saving In-Place (Package.cs:54-88)

Uses temp file + file locking:
1. Save to temp file
2. Lock original header
3. Copy temp content to original
4. Truncate to new size
5. Delete temp file
6. Reload header and invalidate index

## Rewrite Recommendations

### Keep
1. **DBPF header structure** - Must match exactly for game compatibility
2. **Index type optimization** - Reduces file size for uniform packages
3. **Compression support** - Both ZLIB and RefPack for reading
4. **TGI lookup** - Fundamental to the format

### Modernize
1. **Use `Span<T>`/`Memory<T>`** for binary operations instead of `byte[]` + BitConverter
2. **BinaryPrimitives** instead of BitConverter for explicit endianness
3. **Async I/O** with `FileStream` async methods
4. **Memory-mapped files** for large packages
5. **Replace SharpZipLib** with `System.IO.Compression` (built-in)

### Performance Improvements
1. **Lazy index loading** - Don't read full index until needed
2. **Resource caching** - LRU cache for frequently accessed resources
3. **Streaming decompression** - Don't materialize full byte arrays
4. **Parallel decompression** - Decompress multiple resources concurrently

### Security Hardening
```csharp
// CRITICAL: Validate all sizes before allocation
if (indexcount < 0 || indexcount > MAX_RESOURCES)
    throw new PackageFormatException($"Invalid index count: {indexcount}");

if (memsize > MAX_RESOURCE_SIZE)
    throw new PackageFormatException($"Resource too large: {memsize}");

if (chunkoffset + filesize > stream.Length)
    throw new PackageFormatException("Resource extends beyond file");
```

### Write Only Latest Format
- Always write ZLIB (0x78xx), never RefPack
- Always use major=2, minor=1
- Calculate optimal indextype for each save
