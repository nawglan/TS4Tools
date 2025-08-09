# Binary Format Documentation Template

**Resource Type:** [ResourceTypeName]  
**Type ID:** `0x[TypeID]`  
**Endianness:** Little Endian  
**Version Documented:** [Version]  

## Format Overview

[Brief description of the binary format's purpose and structure]

## File Structure

### Overall Layout

```
[File Layout Diagram]
+-------------------+
| Header            | 
+-------------------+
| Data Section 1    |
+-------------------+
| Data Section 2    |
+-------------------+
| ...               |
+-------------------+
```

## Detailed Format Specification

### Header (Offset 0x00)

| Offset | Size | Type   | Name | Description |
|--------|------|--------|------|-------------|
| 0x00   | 4    | uint32 | Magic | Magic number: `0x[MagicValue]` |
| 0x04   | 2    | uint16 | Version | Format version number |
| 0x06   | 2    | uint16 | Flags | Bit flags for format options |
| 0x08   | 4    | uint32 | DataSize | Size of data section in bytes |
| 0x0C   | 4    | uint32 | Count | Number of entries/records |
| 0x10   | ... | ... | ... | [Additional header fields] |

#### Flag Definitions

| Bit | Name | Description |
|-----|------|-------------|
| 0   | [FlagName] | [Flag description] |
| 1   | [FlagName] | [Flag description] |
| ... | ... | ... |

### Data Section Format

[Detailed description of data section structure]

#### Entry Format (if applicable)

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00   | [Size] | [Type] | [Name] | [Description] |
| ...    | ...    | ...    | ...    | ... |

### String Storage (if applicable)

- **Encoding:** UTF-8
- **Null Termination:** [Yes/No]
- **Length Prefix:** [Yes/No - format]

### Compression (if applicable)

- **Algorithm:** [Compression type]
- **Applied to:** [Which sections]
- **Decompression:** [Process description]

## Format Variations

### Version Differences

#### Version 1
- [Version 1 specific details]

#### Version 2  
- [Version 2 changes from v1]

### Platform Variations

- **PC:** [PC-specific notes]
- **Console:** [Console-specific differences]

## Implementation Notes

### Parsing Guidelines

1. **Magic Number Validation:** Always verify magic number first
2. **Version Handling:** Check version and handle appropriately  
3. **Bounds Checking:** Validate all size fields before reading
4. **Error Handling:** [Specific error conditions to check]

### Common Pitfalls

- [List of common parsing errors]
- [Edge cases to be aware of]

### Performance Considerations

- [Memory usage patterns]
- [Streaming vs. full read recommendations]
- [Caching opportunities]

## Examples

### Minimal Valid File

```
Hex Dump:
00000000: [hex bytes]
00000010: [hex bytes]
...
```

### Complex Example

```
Hex Dump:
[Annotated hex dump showing complex structure]
```

## Reference Implementation

### Parsing Logic (Pseudo-code)

```
function parseResource(stream):
    header = parseHeader(stream)
    validateMagic(header.magic)
    
    switch header.version:
        case 1: return parseV1(stream, header)
        case 2: return parseV2(stream, header)
        default: throw UnsupportedVersion
```

### Serialization Logic (Pseudo-code)

```
function serializeResource(resource):
    stream = createStream()
    writeHeader(stream, resource)
    writeData(stream, resource)
    return stream
```

## Test Cases

### Valid Test Data

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| Minimal | Smallest valid file | Parse successfully |
| Typical | Common use case | Parse successfully |
| Maximum | Largest valid file | Parse successfully |

### Invalid Test Data

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| Bad Magic | Wrong magic number | Throw InvalidDataException |
| Bad Version | Unsupported version | Throw UnsupportedVersionException |
| Truncated | Incomplete data | Throw EndOfStreamException |

## Tools and Utilities

### Hex Editors
- [Recommended hex editors]
- [Useful plugins/templates]

### Analysis Tools
- [Tools for analyzing this format]
- [Custom utilities developed]

## Related Formats

- **Similar Formats:** [Related resource types]
- **Dependencies:** [Formats this depends on]
- **References:** [Formats that reference this]

## Change History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | [Date] | [Author] | Initial documentation |
| [Version] | [Date] | [Author] | [Changes] |

---

**Documentation Status:** [Draft/Review/Final]  
**Last Updated:** [Date]  
**Validated Against:** [Game version/sample files]
