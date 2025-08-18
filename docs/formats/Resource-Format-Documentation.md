# Resource Format Documentation

## Overview

This document provides comprehensive documentation for Sims 4 resource formats supported by TS4Tools. Each resource type includes binary format specifications, parsing details, and implementation examples.

## Document Structure

Each resource type documentation includes:

- **Format Specification**: Binary layout and field definitions
- **Header Structure**: Common header patterns and variations
- **Data Sections**: Detailed field descriptions with data types
- **Parsing Examples**: Code samples showing resource processing
- **Compatibility Notes**: Version differences and legacy support
- **Test Data**: Sample files and validation data

## Resource Type Categories

### Core Game Resources

#### String Tables (STBL)

**Resource Type**: `0x220557DA`
**Description**: Localized text content for UI elements, object names, and game text

**Binary Format:**

```
Header (8 bytes):
  00-03: Reserved (0x00000000)
  04-07: Entry Count (uint32)

Entry Table:
  For each entry:
    00-03: String ID (uint32)
    04: String Length (byte)
    05-XX: UTF-8 String Data
```

**Implementation Example:**

```csharp
public class StringTableResource : IResource
{
    private Dictionary<uint, string> _entries = new();

    protected override void Parse(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        var reserved = reader.ReadUInt32(); // Should be 0
        var entryCount = reader.ReadUInt32();

        for (int i = 0; i < entryCount; i++)
        {
            var stringId = reader.ReadUInt32();
            var length = reader.ReadByte();
            var bytes = reader.ReadBytes(length);
            var text = Encoding.UTF8.GetString(bytes);

            _entries[stringId] = text;
        }
    }
}
```

#### Image Resources (DDS/PNG)

**Resource Type**: `0x2E75C764` (DDS), `0x2F7D0004` (PNG)
**Description**: Textures, UI images, and sprite data

**DDS Format Structure:**

```
DDS Header (128 bytes):
  00-03: Magic "DDS " (0x20534444)
  04-07: Header Size (124)
  08-0B: Flags
  0C-0F: Height
  10-13: Width
  14-17: Pitch/Linear Size
  18-1B: Depth
  1C-1F: MipMap Count
  20-4F: Reserved (44 bytes)
  50-7F: Pixel Format (32 bytes)
```

**Implementation Notes:**

- Support for BC1-BC7 compression formats
- Mipmap level handling
- Alpha channel processing
- Texture streaming optimization

#### 3D Geometry (GEOM)

**Resource Type**: `0x015A1849`
**Description**: 3D mesh data including vertices, normals, UV coordinates

**Format Overview:**

```
Header:
  Magic: "GEOM" (4 bytes)
  Version: uint32
  Vertex Count: uint32
  Face Count: uint32

Vertex Data:
  Per vertex (variable size based on format):
    Position: float3 (12 bytes)
    Normal: float3 (12 bytes)
    UV: float2 (8 bytes)
    Additional data as specified
```

### Content Creation Resources

#### Animation Data (ANIM)

**Resource Type**: `0x8EAF13DE`
**Description**: Character and object animation sequences

**Structure:**

- Bone hierarchy definitions
- Keyframe data with timestamps
- Interpolation mode specifications
- Animation event markers

#### Audio Resources (AUDIO)

**Resource Type**: `0xF0582F9A`
**Description**: Sound effects, music, and voice data

**Supported Formats:**

- Ogg Vorbis compression
- PCM uncompressed audio
- Spatial audio metadata
- Loop point definitions

### World Building Resources

#### Lot Data (LOT)

**Resource Type**: `0x0604ABDA`
**Description**: Lot layout, terrain, and placement information

**Components:**

- Terrain height maps
- Object placement data
- Routing information
- Environmental settings

#### Neighborhood Data (NHBD)

**Resource Type**: `0x0355E0A6`
**Description**: World structure and neighborhood definitions

### Specialized Resources

#### Catalog Resources (CATA)

**Resource Type**: `0x319E4F1D`
**Description**: In-game catalog definitions for objects and items

#### Script Resources (SCRIPT)

**Resource Type**: `0x791F5C85`
**Description**: Game logic and behavior scripts

## Format Validation

### Validation Patterns

Each resource format includes validation rules:

```csharp
public class FormatValidator
{
    public ValidationResult ValidateStringTable(byte[] data)
    {
        var result = new ValidationResult();

        // Check minimum size
        if (data.Length < 8)
        {
            result.AddError("STBL file too small for header");
            return result;
        }

        // Validate reserved field
        var reserved = BitConverter.ToUInt32(data, 0);
        if (reserved != 0)
        {
            result.AddWarning($"Non-zero reserved field: 0x{reserved:X8}");
        }

        // Validate entry count vs file size
        var entryCount = BitConverter.ToUInt32(data, 4);
        if (entryCount > (data.Length - 8) / 5) // Minimum 5 bytes per entry
        {
            result.AddError("Entry count exceeds possible file size");
        }

        return result;
    }
}
```

### Golden Master Testing

Format compatibility is validated using golden master tests:

```csharp
[Theory]
[InlineData("ClientStrings0.package")]
[InlineData("ClientDeltaBuild0.package")]
public async Task ValidateStringTableFormat(string packageFile)
{
    // Load original package
    var originalData = await LoadTestPackage(packageFile);

    // Parse with TS4Tools
    var resource = await StringTableResource.ParseAsync(originalData);

    // Re-serialize
    var newData = await resource.SerializeAsync();

    // Validate byte-for-byte identical
    Assert.Equal(originalData, newData);
}
```

## Binary Format Specifications

### Common Header Patterns

Many resources share common header structures:

#### Standard Resource Header

```
00-03: Magic Bytes (resource-specific)
04-07: Version (uint32)
08-0B: Data Size (uint32)
0C-0F: Flags (uint32)
```

#### Extended Header (Used by newer formats)

```
00-03: Magic Bytes
04-07: Header Size (uint32)
08-0B: Version Major (uint16)
0C-0D: Version Minor (uint16)
0E-11: Data Sections Count (uint32)
12-15: Reserved (uint32)
```

### Data Type Conventions

Standard data types used across formats:

- **uint8**: 8-bit unsigned integer
- **uint16**: 16-bit unsigned integer (little-endian)
- **uint32**: 32-bit unsigned integer (little-endian)
- **float32**: 32-bit IEEE 754 float
- **string7**: 7-bit encoded string (SevenBitString)
- **stringU8**: UTF-8 encoded string with length prefix

### Compression Handling

Resources may use various compression methods:

```csharp
public static class CompressionHandler
{
    public static byte[] Decompress(byte[] data, CompressionType type)
    {
        return type switch
        {
            CompressionType.None => data,
            CompressionType.Zlib => ZlibDecompress(data),
            CompressionType.RefPack => RefPackDecompress(data),
            CompressionType.LZ4 => LZ4Decompress(data),
            _ => throw new NotSupportedException($"Compression type {type} not supported")
        };
    }
}
```

## Parser Implementation Guidelines

### Standard Parsing Pattern

```csharp
public abstract class ResourceParser<T> where T : IResource, new()
{
    public async Task<T> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var resource = new T();

        try
        {
            // Validate stream
            if (stream.Length < GetMinimumSize())
                throw new InvalidDataException("Resource data too small");

            // Read header
            var header = await ReadHeaderAsync(stream, cancellationToken);
            ValidateHeader(header);

            // Parse content based on version
            await ParseContentAsync(stream, header, resource, cancellationToken);

            return resource;
        }
        catch (Exception ex)
        {
            resource.Dispose();
            throw new ParseException($"Failed to parse {typeof(T).Name}", ex);
        }
    }

    protected abstract int GetMinimumSize();
    protected abstract Task<ResourceHeader> ReadHeaderAsync(Stream stream, CancellationToken cancellationToken);
    protected abstract void ValidateHeader(ResourceHeader header);
    protected abstract Task ParseContentAsync(Stream stream, ResourceHeader header, T resource, CancellationToken cancellationToken);
}
```

### Error Handling

Robust error handling for malformed data:

```csharp
public class ParseException : Exception
{
    public long StreamPosition { get; }
    public string ResourceType { get; }

    public ParseException(string message, long position, string resourceType)
        : base($"{message} at position 0x{position:X8} in {resourceType}")
    {
        StreamPosition = position;
        ResourceType = resourceType;
    }
}
```

## Version Compatibility

### Format Evolution

Resource formats evolve across Sims 4 game versions:

- **Base Game (1.0)**: Original format specifications
- **Get Together (1.12)**: Extended lot data formats
- **City Living (1.25)**: Apartment and venue data
- **High School Years (1.90)**: New interaction systems
- **Growing Together (1.98)**: Family dynamics data

### Backward Compatibility

```csharp
public class VersionHandler
{
    public static IResourceParser GetParser(uint resourceType, uint version)
    {
        return (resourceType, version) switch
        {
            (0x220557DA, >= 5) => new StringTableV5Parser(),
            (0x220557DA, >= 3) => new StringTableV3Parser(),
            (0x220557DA, _) => new StringTableV1Parser(),

            (0x2E75C764, _) => new DDSResourceParser(),

            _ => new GenericResourceParser()
        };
    }
}
```

## Testing and Validation

### Test Data Organization

```
test-data/
â”œâ”€â”€ packages/
â”‚   â”œâ”€â”€ base-game/          # Original EA packages
â”‚   â”œâ”€â”€ expansion-packs/    # EP-specific resources
â”‚   â””â”€â”€ custom/            # Community-created content
â”œâ”€â”€ individual-resources/
â”‚   â”œâ”€â”€ stbl/              # String table samples
â”‚   â”œâ”€â”€ dds/               # Texture samples
â”‚   â””â”€â”€ geom/              # Geometry samples
â””â”€â”€ validation/
    â”œâ”€â”€ malformed/         # Invalid data for error testing
    â””â”€â”€ edge-cases/        # Boundary condition tests
```

### Comprehensive Format Testing

```csharp
[TestClass]
public class FormatCompatibilityTests
{
    [DataRow("Base Game", "ClientStrings0.package")]
    [DataRow("Get Together", "EP01ClientStrings.package")]
    [TestMethod]
    public async Task ValidateAllResourceTypes(string version, string packageFile)
    {
        var package = await LoadPackage(packageFile);
        var errors = new List<string>();

        foreach (var resource in package.Resources)
        {
            try
            {
                await ValidateResourceFormat(resource);
            }
            catch (Exception ex)
            {
                errors.Add($"{resource.Key}: {ex.Message}");
            }
        }

        Assert.AreEqual(0, errors.Count,
            $"Format validation errors in {version}:\n{string.Join("\n", errors)}");
    }
}
```

## Documentation Standards

### Format Documentation Template

Each resource type should include:

1. **Overview**: Purpose and usage in game
1. **Binary Layout**: Detailed byte-level format
1. **Field Descriptions**: Each field with data type and purpose
1. **Version History**: Changes across game versions
1. **Implementation Example**: Working parser code
1. **Test Cases**: Validation and edge case tests
1. **Known Issues**: Limitations and workarounds

### Maintenance Guidelines

- Update documentation when format changes are discovered
- Validate documentation against actual game files
- Include performance characteristics for large files
- Document any custom extensions or optimizations

## Future Enhancements

### Planned Improvements

1. **Format Discovery**: Automatic format detection from magic bytes
1. **Schema Validation**: JSON Schema for format specifications
1. **Visual Format Editor**: GUI tool for format exploration
1. **Performance Profiling**: Detailed parsing performance metrics

### Research Areas

- **New Resource Types**: Emerging formats in latest game versions
- **Optimization Opportunities**: Faster parsing algorithms
- **Format Prediction**: Machine learning for format recognition
- **Cross-Game Compatibility**: Support for other EA game formats

______________________________________________________________________

*Last Updated: August 8, 2025*
*For complete format specifications, see individual resource type documentation*
