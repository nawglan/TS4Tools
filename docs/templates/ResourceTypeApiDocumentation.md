# [ResourceTypeName] API Documentation

**Resource Type ID:** `0x[TypeID]`
**Legacy Class:** `[LegacyClassName]`
**TS4Tools Implementation:** `[NewClassName]`
**Package:** `TS4Tools.Resources.[Category]`
**Status:** [Implementation Status]

## Overview

[Brief description of what this resource type represents in The Sims 4]

## Binary Format Specification

### Header Structure

```
Offset  | Size | Type   | Description
--------|------|--------|-------------
0x00    | 4    | uint32 | Magic Number: 0x[MagicNumber]
0x04    | 2    | uint16 | Version: [VersionNumber]
0x06    | 2    | uint16 | Reserved/Flags
0x08    | 4    | uint32 | Data Length
0x0C    | ...  | ...    | [Additional header fields]
```

### Data Structure

```
[Detailed binary format description]
```

## API Reference

### Interfaces

#### I[ResourceTypeName]

```csharp
public interface I[ResourceTypeName] : IResource
{
    /// <summary>
    /// [Property description]
    /// </summary>
    [PropertyType] PropertyName { get; set; }

    /// <summary>
    /// [Method description]
    /// </summary>
    /// <param name="parameter">[Parameter description]</param>
    /// <returns>[Return value description]</returns>
    Task<[ReturnType]> MethodNameAsync([ParameterType] parameter);
}
```

### Implementation Class

#### [ResourceTypeName]

```csharp
public class [ResourceTypeName] : I[ResourceTypeName]
{
    // Implementation details
}
```

### Factory Class

#### [ResourceTypeName]Factory

```csharp
public class [ResourceTypeName]Factory : IResourceFactory
{
    // Factory implementation
}
```

## Usage Examples

### Creating from Binary Data

```csharp
// Load from package
using var package = await packageFactory.LoadFromFileAsync("example.package");
var resourceStream = await package.GetResourceStreamAsync(resourceIndex);
var resource = await resourceManager.CreateResourceAsync(0x[TypeID], resourceStream);

if (resource is I[ResourceTypeName] typedResource)
{
    // Use resource-specific properties and methods
    var value = typedResource.PropertyName;
}
```

### Creating New Resource

```csharp
// Create new empty resource
var newResource = resourceManager.CreateResource<[ResourceTypeName]>();
newResource.PropertyName = value;

// Serialize to binary
using var outputStream = await newResource.SerializeAsync();
```

### Modifying Existing Resource

```csharp
// Load, modify, and save
var resource = await LoadResource() as I[ResourceTypeName];
resource.PropertyName = newValue;

using var modifiedStream = await resource.SerializeAsync();
await package.UpdateResourceAsync(resourceIndex, modifiedStream);
```

## Migration from Legacy Implementation

### Property Mappings

| Legacy Property | TS4Tools Property | Notes |
|----------------|-------------------|--------|
| `OldPropertyName` | `NewPropertyName` | [Migration notes] |

### Method Mappings

| Legacy Method | TS4Tools Method | Notes |
|--------------|-----------------|--------|
| `OldMethod()` | `NewMethodAsync()` | [Migration notes] |

### Breaking Changes

- [List any breaking changes from legacy implementation]
- [Migration guidance for existing code]

## Implementation Notes

### Design Decisions

- [Key architectural decisions made during implementation]
- [Rationale for API design choices]

### Performance Considerations

- [Performance characteristics]
- [Memory usage patterns]
- [Optimization opportunities]

### Cross-Platform Compatibility

- [Platform-specific considerations]
- [Endianness handling]
- [File path handling]

## Testing

### Test Coverage

- âœ… Binary format parsing
- âœ… Round-trip serialization
- âœ… Property validation
- âœ… Edge case handling
- âœ… Performance benchmarks

### Golden Master Tests

```csharp
[Fact]
public async Task [ResourceTypeName]_RoundTripSerialization_ShouldPreserveBinaryEquivalence()
{
    // Golden master test implementation
}
```

## Related Resources

- **Dependencies:** [List of related resource types]
- **References:** [Resources that reference this type]
- **Documentation:** [Links to additional documentation]

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | [Date] | Initial implementation |
| [Version] | [Date] | [Change description] |

---

**Last Updated:** [Date]
**Author:** [Author Name]
**Review Status:** [Review Status]

