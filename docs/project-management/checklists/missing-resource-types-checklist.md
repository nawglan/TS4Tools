# Missing Resource Types Implementation Checklist

This checklist covers all unknown resource types identified by the PackageAnalysisScript that need to be
implemented in TS4Tools. The resource types are listed in order of occurrence frequency to prioritize the
most common ones first.

## Overview

Based on the analysis of game packages, the following resource types are currently unknown/unimplemented
and need resource factory implementations in TS4Tools. This represents a comprehensive catalog of all
missing resource types from the top 20 most common unknown types.

## High Priority Resource Types (10,000+ occurrences)

### 1. Resource Type 0xBC4A5044 (147,270 occurrences)

- [x] Research resource type purpose and format (Animation Resource)
- [x] Create TS4Tools.Resources.Animation project
- [x] Implement IAnimationResource interface
- [x] Create AnimationResourceFactory class
- [x] Add unit tests for parsing and unparsing
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0xBC4A5044, "Animation Resource (BC4A5044)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Add resource type mapping to test dictionary
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 2. Resource Type 0x545AC67A (134,352 occurrences)

- [x] Research resource type purpose and format (Material/SWB Resource)
- [x] Create TS4Tools.Resources.Materials project
- [x] Implement IMaterialResource interface
- [x] Create MaterialResourceFactory class
- [x] Add unit tests for parsing and unparsing
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x545AC67A, "Material Resource (SWB)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Add resource type mapping to test dictionary
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 3. Resource Type 0x3C2A8647 (118,015 occurrences)

- [x] Research resource type purpose and format (ThumbnailResource)
- [x] Update existing ThumbnailResourceFactory to support 0x3C2A8647
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x3C2A8647, "Thumbnail Resource (Additional Type)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Factory already registered in service collection (VisualResourceFactories)
- [x] Update documentation

### 4. Resource Type 0x3C1AF1F2 (85,271 occurrences)

- [x] Research resource type purpose and format (PNG Thumbnail Resource)
- [x] Add to existing TS4Tools.Resources.Images project
- [x] Add resource type constant to ImageResource class
- [x] Update ImageResourceFactory to support 0x3C1AF1F2
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x3C1AF1F2, "PNG Thumbnail Resource")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Factory already registered in service collection
- [x] Update documentation

### 5. Resource Type 0xFD04E3BE (84,311 occurrences)

- [x] Research resource type purpose and format (Audio Configuration)
- [x] Create AudioConfigurationResource class in TS4Tools.Resources.Audio project
- [x] Implement IResource interface
- [x] Create AudioConfigurationResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0xFD04E3BE, "Audio Configuration Resource")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 6. Resource Type 0x01A527DB (68,071 occurrences)

- [x] Research resource type purpose and format (Audio SNR - voice/audio)
- [x] Add to existing TS4Tools.Resources.Audio project  
- [x] Add resource type constant to SoundResource class
- [x] Update SoundResourceFactory to support 0x01A527DB
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x01A527DB, "Audio SNR Resource (Voice/Audio)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Factory already registered in service collection
- [x] Update documentation

### 7. Resource Type 0x01D10F34 (40,286 occurrences)

- [x] Research resource type purpose and format (MLOD - Object Geometry LODs)
- [x] Create MLODResource class in TS4Tools.Resources.Geometry project
- [x] Implement IResource interface
- [x] Create MLODResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x01D10F34, "MLOD Resource (Object Geometry LODs)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 8. Resource Type 0x81CA1A10 (24,727 occurrences)

- [x] Research resource type purpose and format (MTBL - Material Table)
- [x] Create MTBLResource class in TS4Tools.Resources.Materials project
- [x] Implement IResource interface with MTBLEntry support
- [x] Create MTBLResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x81CA1A10, "MTBL Resource (Material Table)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 9. Resource Type 0xD5F0F921 (13,573 occurrences)

- [x] Research resource type purpose and format (CWAL - Wall Pattern Catalog)
- [x] Create CWALResource class in TS4Tools.Resources.Catalog project
- [x] Implement IResource interface with CatalogCommonData support
- [x] Create CWALResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0xD5F0F921, "CWAL Resource (Wall Pattern Catalog)")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation
- [ ] Register factory in service collection
- [ ] Update documentation

### 10. Resource Type 0x00010000 (12,631 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.00010000 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 11. Resource Type 0xD3044521 (12,375 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.D3044521 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 12. Resource Type 0xD382BF57 (12,060 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.D382BF57 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 13. Resource Type 0x1B192049 (12,021 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.1B192049 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 14. Resource Type 0x3453CF95 (12,010 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.3453CF95 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 15. Resource Type 0x02D5DF13 (11,378 occurrences)

- [x] Research resource type purpose and format (JAZZ Animation State Machine)
- [x] Add to existing TS4Tools.Resources.Animation project
- [x] Implement IJazzResource interface
- [x] Create JazzResource class for XML-based animation state machines
- [x] Create JazzResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x02D5DF13, "JAZZ Animation State Machine Resource")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 16. Resource Type 0x03B4C61D (10,637 occurrences)

- [x] Research resource type purpose and format (LITE Light Resource)
- [x] Add to existing TS4Tools.Resources.Effects project
- [x] Implement ILightResource interface
- [x] Create LightResource class for lighting properties
- [x] Create LightResourceFactory class
- [x] Add golden master tests with s4pi compatibility
  - [x] Add `[InlineData(0x03B4C61D, "LITE Light Resource")]` to ResourceTypeGoldenMasterTests.cs
  - [x] Verify test passes round-trip serialization
- [x] Register factory in service collection
- [x] Update documentation

### 17. Resource Type 0x01D0E75D (10,282 occurrences)

> **Note**: This may already be implemented as part of Geometry resources - verify first

- [ ] Verify if already implemented in TS4Tools.Resources.Geometry
- [ ] If not implemented, create TS4Tools.Resources.01D0E75D project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

## Medium Priority Resource Types (5,000-9,999 occurrences)

### 18. Resource Type 0x025ED6F4 (9,959 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.025ED6F4 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 19. Resource Type 0x00015A42 (9,173 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.00015A42 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

### 20. Resource Type 0x71BDB8A2 (6,801 occurrences)

- [ ] Research resource type purpose and format
- [ ] Create TS4Tools.Resources.71BDB8A2 project
- [ ] Implement IResource interface
- [ ] Create resource factory class
- [ ] Add unit tests for parsing and unparsing
- [ ] Add golden master tests with s4pi compatibility
- [ ] Register factory in service collection
- [ ] Update documentation

## Implementation Guidelines

### General Implementation Pattern

For each resource type, follow this standard pattern:

1. **Research Phase**
   - Analyze samples from game packages
   - Identify data structure and format
   - Research any legacy s4pi implementations
   - Document purpose and usage

2. **Project Setup**
   - Create new project: `TS4Tools.Resources.[TypeName]`
   - Add project references to Core and Interfaces
   - Set up project structure following existing patterns

3. **Core Implementation**
   - Implement `IResource` interface
   - Create resource factory inheriting from `ResourceFactoryBase<T>`
   - Implement parsing and unparsing methods
   - Add proper error handling and validation

4. **Testing**
   - Create comprehensive unit tests
   - Add golden master tests for s4pi compatibility:
     - Add `[InlineData]` entry to `ResourceTypeGoldenMasterTests.cs`
     - Include resource type ID and descriptive name
     - Add resource type mapping to the dictionary in the test class
     - Verify test execution with `dotnet test --filter "ResourceType_RoundTripSerialization_ShouldPreserveBinaryEquivalence"`
   - Test with various sample files
   - Verify round-trip parsing/unparsing

5. **Integration**
   - Register factory in service collection
   - Update main project references
   - Add to resource factory registry
   - Update documentation

### Golden Master Testing Requirements

Golden master tests are **mandatory** for every new resource type implementation. These tests ensure
byte-perfect compatibility with the legacy s4pi library, which is critical for maintaining backwards
compatibility and ensuring correct round-trip serialization.

#### Required Steps for Golden Master Tests

1. **Add InlineData Entry**
   - Open `tests/TS4Tools.Tests.GoldenMaster/ResourceTypeGoldenMasterTests.cs`
   - Add new `[InlineData(0x[ResourceTypeID], "[Descriptive Name]")]` entry
   - Use format: `[InlineData(0xBC4A5044, "Animation Resource (BC4A5044)")]`

2. **Update Resource Type Mapping**
   - Add entry to the resource type dictionary in the test class
   - Map the resource type ID to the appropriate resource type enum/identifier

3. **Verify Test Execution**
   - Run the specific test: `dotnet test --filter "ResourceType_RoundTripSerialization_ShouldPreserveBinaryEquivalence"`
   - Ensure the new resource type passes the round-trip serialization test
   - Fix any compatibility issues before marking implementation complete

4. **Test Coverage Validation**
   - Golden master tests must cover all implemented resource types
   - New resource types without golden master tests will fail CI/CD pipeline
   - Tests validate that TS4Tools produces identical output to legacy s4pi library

### Quality Standards

- **100% s4pi Compatibility**: All implementations must be byte-perfect compatible with legacy s4pi
- **Comprehensive Testing**: Minimum 90% code coverage with unit tests
- **Golden Master Testing**:
  - Validate against known good s4pi outputs for round-trip serialization
  - Required `[InlineData]` entry in `ResourceTypeGoldenMasterTests.cs`
  - Must pass `ResourceType_RoundTripSerialization_ShouldPreserveBinaryEquivalence` test
  - Ensures byte-perfect compatibility with legacy s4pi library
- **Performance**: Efficient parsing and memory usage
- **Documentation**: Complete XML documentation and usage examples

### Priority Guidelines

1. **Start with highest occurrence count**: Focus on resource types with 10,000+ occurrences first
2. **Group related types**: Look for patterns that might indicate related resource formats
3. **Leverage existing patterns**: Use successful implementations as templates
4. **Incremental delivery**: Complete and test each resource type before moving to the next

## Progress Tracking

- Total Unknown Resource Types: 20 (from top priority list)
- Completed: 11 (Animation, Materials, Thumbnails, MLOD, MTBL, CWAL, Audio Config, PNG Thumbnails, Audio SNR, JAZZ, LITE)
- In Progress: 0
- Remaining: 9

### Completed Resource Types

1. **0xBC4A5044** - Animation Resource (147,270 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Animation
   
2. **0x545AC67A** - Material/SWB Resource (134,352 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Materials

3. **0x3C2A8647** - Thumbnail Resource (118,015 occurrences) ✅
   - Updated existing ThumbnailResourceFactory
   - Project: TS4Tools.Resources.Visual

4. **0x3C1AF1F2** - PNG Thumbnail Resource (85,271 occurrences) ✅
   - Added to existing ImageResourceFactory
   - Project: TS4Tools.Resources.Images

5. **0xFD04E3BE** - Audio Configuration Resource (84,311 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Audio

6. **0x01A527DB** - Audio SNR Resource (68,071 occurrences) ✅
   - Added to existing SoundResourceFactory
   - Project: TS4Tools.Resources.Audio

7. **0x01D10F34** - MLOD Resource (40,286 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Geometry

8. **0x81CA1A10** - MTBL Resource (24,727 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Materials

9. **0xD5F0F921** - CWAL Resource (13,573 occurrences) ✅
   - Full implementation with golden master tests
   - Project: TS4Tools.Resources.Catalog

10. **0x02D5DF13** - JAZZ Animation State Machine Resource (11,378 occurrences) ✅
    - Full implementation with golden master tests
    - Project: TS4Tools.Resources.Animation
    - XML-based animation state machines with validation

11. **0x03B4C61D** - LITE Light Resource (10,637 occurrences) ✅
    - Full implementation with golden master tests
    - Project: TS4Tools.Resources.Effects
    - Lighting properties with multiple light types and falloff modes

**Total Coverage Added:** ~727,911 resource occurrences now supported!

## Notes

- Resource type IDs are represented in hexadecimal format (0x prefix)
- Occurrence counts based on analysis of game package files
- Some resource types may have legacy s4pi implementations to reference
- Implementation order should prioritize high-occurrence types for maximum impact
- Each implementation should be thoroughly tested before marking complete
- **Golden master tests are mandatory** - implementations without them will fail CI/CD
- Golden master tests ensure byte-perfect compatibility with legacy s4pi library
- Use `dotnet test --filter "ResourceType_RoundTripSerialization_ShouldPreserveBinaryEquivalence"` to validate
- This list represents the TOP 20 most common unknown resource types
- Additional unknown types exist but with lower occurrence counts
- Focus on completing these 20 types first for maximum coverage improvement
