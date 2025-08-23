# BC4A5044 Clip Header Resource - Binary Parsing Fixes Checklist

## Overview

This checklist provides step-by-step instructions for fixing binary parsing issues in the BC4A5044
(Clip Header) resource implementation. The current implementation successfully detects and loads
resources but has endianness and overflow issues that need to be resolved.

## Current Status

- ✅ Interface implementation complete (`IClipHeaderResource`)
- ✅ Factory registration working (`ClipHeaderResourceFactory`)
- ✅ Dependency injection integration working
- ✅ Real package detection working (227 BC4A5044 resources found in SP13)
- ❌ Binary parsing has endianness issues (version reads as 1145588803 instead of 1)
- ❌ Overflow errors when reading large resource sizes
- ❌ Property setters not updating underlying data correctly

## Prerequisites

Before starting, ensure you have:

- [ ] Access to real Sims 4 installation with SP13 package
- [ ] Test console application working (`ClipHeaderDirectTest`)
- [ ] Understanding of The Sims 4 binary format endianness (little-endian)
- [ ] Access to legacy Sims4Tools ClipResource.cs for reference

## Phase 1: Fix Binary Reading Endianness Issues

### 1.1 Identify Current Problems

- [ ] Run test: `cd /home/dez/code/TS4Tools/ClipHeaderDirectTest && dotnet run`
- [ ] Confirm version reads as 1145588803 instead of 1
- [ ] Confirm duration shows as scientific notation instead of reasonable values
- [ ] Note that JSON structure is correct but values are wrong

### 1.2 Fix BinaryReader Endianness

File: `/home/dez/code/TS4Tools/src/TS4Tools.Resources.Animation/ClipHeaderResource.cs`

- [ ] Locate `ReadFromStream` method around line 150
- [ ] Check if BinaryReader is reading little-endian correctly
- [ ] Ensure all `reader.ReadUInt32()`, `reader.ReadSingle()`, etc. calls handle endianness
- [ ] The Sims 4 uses little-endian format, which should be default for .NET on x86/x64

### 1.3 Verify Mock Data Generation

File: `/home/dez/code/TS4Tools/ClipHeaderDirectTest/Program.cs`

- [ ] Check `CreateMockClhdData()` method around line 220
- [ ] Ensure mock data writes values in correct endian format
- [ ] Verify magic number "CLHD" is written correctly
- [ ] Test with simple values first (version=1, duration=1.0f)

### 1.4 Test Endianness Fixes

- [ ] Run test console application
- [ ] Verify version now reads as 1 instead of 1145588803
- [ ] Verify duration reads as reasonable float value
- [ ] Verify other numeric fields display correctly

## Phase 2: Fix Overflow Issues

### 2.1 Investigate Overflow Sources

- [ ] Check FileSize values in package index (currently showing ~2.1GB)
- [ ] Determine if this is a package corruption or parsing issue
- [ ] Review how `GetResourceStreamAsync` handles large/compressed resources

### 2.2 Add Safety Checks

File: `/home/dez/code/TS4Tools/ClipHeaderDirectTest/Program.cs`

- [ ] Add size validation before processing resources:
```csharp

if (resourceEntry.FileSize > 100_000_000) // 100MB limit
{
    Console.WriteLine($"      ⚠️  Skipping oversized resource: {resourceEntry.FileSize} bytes");
    continue;
}

```

### 2.3 Handle Compressed Resources

- [ ] Check if resources are compressed (Compressed property)
- [ ] Verify decompression logic in package reading
- [ ] Test with smaller BC4A5044 resources first

### 2.4 Test Overflow Fixes

- [ ] Run test with size limits in place
- [ ] Process at least one small BC4A5044 resource successfully
- [ ] Verify JSON output from real game data

## Phase 3: Fix Property Setter Issues

### 3.1 Implement Property Storage

File: `/home/dez/code/TS4Tools/src/TS4Tools.Resources.Animation/ClipHeaderResource.cs`

- [ ] Add private dictionary for custom properties: `private readonly Dictionary<string, object?> _properties = new();`
- [ ] Update `SetProperty` method to store values in dictionary
- [ ] Update `GetProperty` method to retrieve from dictionary
- [ ] Handle special properties that should update main fields (clipName, duration, etc.)

### 3.2 Update Core Properties

- [ ] Make sure `SetProperty("clipName", value)` updates `ClipName` property
- [ ] Make sure `SetProperty("duration", value)` updates `Duration` property
- [ ] Ensure property changes are reflected in JSON output
- [ ] Add validation for property types and ranges

### 3.3 Test Property Manipulation

- [ ] Run Test 3 in console application
- [ ] Verify properties can be set and retrieved
- [ ] Verify modified properties appear in JSON output
- [ ] Test round-trip serialization with modified properties

## Phase 4: Comprehensive Testing

### 4.1 Test with Real Game Data

- [ ] Process multiple real BC4A5044 resources from SP13 package
- [ ] Verify version, duration, and other fields show reasonable values
- [ ] Compare output with known animation lengths from game
- [ ] Save JSON output for manual inspection

### 4.2 Performance Testing

- [ ] Test with 10+ resources to ensure no memory leaks
- [ ] Verify proper disposal of streams and resources
- [ ] Check processing time for typical resources

### 4.3 Integration Testing

- [ ] Test factory registration in larger application context
- [ ] Verify ResourceManager correctly routes BC4A5044 requests
- [ ] Test with different API versions if applicable

## Phase 5: Code Quality and Documentation

### 5.1 Code Review

- [ ] Remove debug console output from production code
- [ ] Add proper exception handling for malformed resources
- [ ] Ensure all public methods have XML documentation
- [ ] Follow established coding standards in the project

### 5.2 Add Unit Tests

Directory: `/home/dez/code/TS4Tools/tests/`

- [ ] Create unit tests for ClipHeaderResource parsing
- [ ] Test with known good binary data
- [ ] Test error conditions (truncated data, invalid magic, etc.)
- [ ] Test property manipulation scenarios

### 5.3 Update Documentation

- [ ] Document BC4A5044 format in `/home/dez/code/TS4Tools/docs/formats/`
- [ ] Add usage examples to developer documentation
- [ ] Update API documentation if needed

## Validation Checklist

Run this final validation to ensure all issues are resolved:

- [ ] Test console shows version as 1 (not 1145588803)
- [ ] Duration shows as reasonable float (not scientific notation)
- [ ] No overflow exceptions when processing real resources
- [ ] Properties can be set and retrieved correctly
- [ ] JSON output contains sensible values
- [ ] At least 3 real BC4A5044 resources process successfully
- [ ] No memory leaks or resource disposal issues
- [ ] Integration with ResourceManager works correctly

## Troubleshooting Common Issues

### Magic Number Reading Incorrectly

- Check byte order in BinaryReader
- Verify stream position is correct before reading
- Consider if decompression is needed first

### Still Getting Overflow

- Add more granular size checking
- Process uncompressed size vs compressed size
- Check for package file corruption

### Properties Not Persisting

- Verify SetProperty stores in correct location
- Check if serialization includes custom properties
- Ensure property names match expected keys

## Reference Files

- Primary implementation: `/home/dez/code/TS4Tools/src/TS4Tools.Resources.Animation/ClipHeaderResource.cs`
- Test application: `/home/dez/code/TS4Tools/ClipHeaderDirectTest/Program.cs`
- Interface definition: `/home/dez/code/TS4Tools/src/TS4Tools.Core.Interfaces/Resources/IClipHeaderResource.cs`
- Legacy reference: `/home/dez/code/Sims4Tools/s4pi Wrappers/AnimationResourceWrappers/ClipResource.cs`

## Success Criteria

The implementation is considered fixed when:
1. All numeric values parse correctly from binary data
2. No overflow exceptions occur with real game resources
3. Property manipulation works as expected
4. JSON output contains meaningful animation data
5. Integration tests pass with real SP13 package data

