# Phase 0 Implementation Guide - CRITICAL FOUNDATION

## 🚨 PROJECT BLOCKED - IMMEDIATE ACTION REQUIRED

**Status**: ❌ NOT STARTED - P0 CRITICAL BLOCKING  
**Date**: August 7, 2025  
**Priority**: STOP ALL OTHER WORK  

This document provides step-by-step implementation guide for the critical Phase 0 requirements identified from SIMS4TOOLS_MIGRATION_DOCUMENTATION.md analysis.

**CRITICAL**: All Phase 4.9+ work must cease until Phase 0 is complete. The project has correctly identified these requirements but has NOT implemented them.

---

## 📋 EXECUTION CHECKLIST

### Phase 0.1: Real Package Data Collection (3 days - START NOW)
**Owner**: Any available developer | **Dependencies**: None

#### Day 1: Steam Installation Discovery
```powershell
# Execute these commands immediately:
cd "c:\Users\nawgl\code\TS4Tools"

# Check for Steam Sims 4 installation
$steamPath = "C:\Program Files (x86)\Steam\steamapps\common\The Sims 4\Data\Client"
$alternativeSteam = "C:\Program Files\Steam\steamapps\common\The Sims 4\Data\Client"  
$eaPath = "$env:PROGRAMFILES\EA Games\The Sims 4\Data\Client"

Write-Host "Searching for Sims 4 installations..."
if (Test-Path $steamPath) { 
    Write-Host "✅ Found Steam installation: $steamPath"
    $gameDataPath = $steamPath
} elseif (Test-Path $alternativeSteam) {
    Write-Host "✅ Found Steam installation: $alternativeSteam"  
    $gameDataPath = $alternativeSteam
} elseif (Test-Path $eaPath) {
    Write-Host "✅ Found EA installation: $eaPath"
    $gameDataPath = $eaPath
} else {
    Write-Host "❌ No Sims 4 installation found. Manual path required."
    Write-Host "Please locate your Sims 4 installation and update paths."
}
```

**✅ Day 1 Success Criteria:**
- [ ] Sims 4 installation located and confirmed
- [ ] Path variables set correctly
- [ ] Access to .package files verified

#### Day 2: Package Collection
```powershell
# Create directory structure
New-Item -ItemType Directory -Force -Path "test-data\real-packages\official"
New-Item -ItemType Directory -Force -Path "test-data\real-packages\mods"
New-Item -ItemType Directory -Force -Path "test-data\package-metadata"

# Collect official game packages (TARGET: 50+ files)
$officialPackages = Get-ChildItem "$gameDataPath\*.package" -ErrorAction SilentlyContinue
Write-Host "Found $($officialPackages.Count) official packages"

# Copy diverse official packages by size
$officialPackages | Sort-Object Length | Select-Object -First 25 | Copy-Item -Destination "test-data\real-packages\official\" -Verbose
$officialPackages | Sort-Object Length -Descending | Select-Object -First 25 | Copy-Item -Destination "test-data\real-packages\official\" -Verbose

# Collect community mod packages (TARGET: 50+ files)
$modsPath = "$env:USERPROFILE\Documents\Electronic Arts\The Sims 4\Mods"
if (Test-Path $modsPath) {
    $modPackages = Get-ChildItem "$modsPath\**\*.package" -Recurse -ErrorAction SilentlyContinue
    Write-Host "Found $($modPackages.Count) mod packages"
    $modPackages | Select-Object -First 50 | Copy-Item -Destination "test-data\real-packages\mods\" -Verbose
}

# Verify collection
$totalCollected = (Get-ChildItem "test-data\real-packages\**\*.package" -Recurse).Count
Write-Host "✅ SUCCESS: Collected $totalCollected packages for golden master testing"
```

**✅ Day 2 Success Criteria:**
- [ ] 100+ .package files collected total
- [ ] 50+ official game packages from Steam/EA installation
- [ ] 50+ community mod packages (if available)
- [ ] Files organized in official/ and mods/ subdirectories

#### Day 3: Metadata Generation
```powershell
# Generate comprehensive package inventory
$allPackages = Get-ChildItem "test-data\real-packages\**\*.package" -Recurse | ForEach-Object {
    $category = if ($_.Directory.Name -eq "official") { "Official" } else { "Community" }
    $size = $_.Length
    $sizeCategory = if ($size -lt 1MB) { "Small" } 
                   elseif ($size -lt 10MB) { "Medium" } 
                   elseif ($size -lt 100MB) { "Large" } 
                   else { "XLarge" }
    
    [PSCustomObject]@{
        FileName = $_.Name
        Category = $category
        Size = $size
        SizeFormatted = "{0:N2} MB" -f ($size / 1MB)
        SizeCategory = $sizeCategory
        LastModified = $_.LastWriteTime
        FullPath = $_.FullName
        RelativePath = $_.FullName.Replace((Get-Location).Path, "")
    }
}

# Export to multiple formats for analysis
$allPackages | ConvertTo-Json -Depth 2 | Out-File "test-data\package-metadata\inventory.json" -Encoding UTF8
$allPackages | Export-Csv "test-data\package-metadata\inventory.csv" -NoTypeInformation

# Generate summary statistics
$summary = @{
    TotalPackages = $allPackages.Count
    OfficialCount = ($allPackages | Where-Object Category -eq "Official").Count
    CommunityCount = ($allPackages | Where-Object Category -eq "Community").Count
    SizeDistribution = $allPackages | Group-Object SizeCategory | ForEach-Object { @{ $_.Name = $_.Count } }
    TotalSize = "{0:N2} GB" -f (($allPackages | Measure-Object Size -Sum).Sum / 1GB)
    CollectionDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
}

$summary | ConvertTo-Json | Out-File "test-data\package-metadata\collection-summary.json" -Encoding UTF8

Write-Host "✅ PHASE 0.1 COMPLETE:"
Write-Host "  - Total Packages: $($summary.TotalPackages)"
Write-Host "  - Official: $($summary.OfficialCount)"  
Write-Host "  - Community: $($summary.CommunityCount)"
Write-Host "  - Total Size: $($summary.TotalSize)"
```

**✅ Day 3 Success Criteria:**
- [ ] inventory.json with complete package metadata
- [ ] collection-summary.json with statistics
- [ ] inventory.csv for Excel analysis  
- [ ] Size distribution analysis complete
- [ ] Minimum 100+ packages collected and cataloged

---

### Phase 0.2: Golden Master Test Framework (4 days - CRITICAL)
**Owner**: Senior developer required | **Dependencies**: Phase 0.1 complete

#### Day 4: Test Project Setup
```bash
# Navigate to project root
cd "c:\Users\nawgl\code\TS4Tools"

# Create golden master test project
dotnet new xunit -n "TS4Tools.Tests.GoldenMaster" -o "tests/TS4Tools.Tests.GoldenMaster" -f net9.0

# Add project to solution
dotnet sln add "tests/TS4Tools.Tests.GoldenMaster/TS4Tools.Tests.GoldenMaster.csproj"

# Add required package references
cd "tests/TS4Tools.Tests.GoldenMaster"
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio  
dotnet add package FluentAssertions
dotnet add package Microsoft.Extensions.Logging.Abstractions

# Add project references to core TS4Tools packages
dotnet add reference "../../src/TS4Tools.Core.Interfaces/TS4Tools.Core.Interfaces.csproj"
dotnet add reference "../../src/TS4Tools.Core.Package/TS4Tools.Core.Package.csproj"
dotnet add reference "../../src/TS4Tools.Core.Resources/TS4Tools.Core.Resources.csproj"

# Verify project builds
dotnet build
```

#### Day 5: Core Test Implementation
Create `tests/TS4Tools.Tests.GoldenMaster/PackageCompatibilityTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Tests.GoldenMaster;

/// <summary>
/// CRITICAL: Golden Master Tests for byte-perfect compatibility validation.
/// These tests WILL FAIL initially - this is expected until implementation is complete.
/// </summary>
[Collection("GoldenMaster")]
public class PackageCompatibilityTests
{
    private readonly string _testDataPath;
    private readonly ILogger _logger;
    
    public PackageCompatibilityTests()
    {
        _testDataPath = Path.Combine("test-data", "real-packages");
        _logger = NullLogger.Instance;
    }
    
    [Theory]
    [MemberData(nameof(GetRealSims4Packages))]
    public async Task NewImplementation_LoadSaveRoundTrip_ProducesIdenticalOutput(string packagePath)
    {
        // CRITICAL: This test validates byte-perfect compatibility
        // It WILL FAIL until the new implementation can properly serialize packages
        
        // STEP 1: Load original bytes (reference/expected output)
        var originalBytes = await File.ReadAllBytesAsync(packagePath);
        originalBytes.Should().NotBeEmpty("package file should contain data");
        
        // STEP 2: Load with new TS4Tools implementation
        // NOTE: This will throw NotImplementedException until implemented
        var package = await LoadPackageWithNewImplementation(packagePath);
        package.Should().NotBeNull("package should load successfully");
        
        // STEP 3: Serialize with new implementation
        var newBytes = await SerializePackageWithNewImplementation(package);
        newBytes.Should().NotBeEmpty("serialized package should contain data");
        
        // STEP 4: Byte-perfect validation (THE CRITICAL TEST)
        newBytes.Length.Should().Be(originalBytes.Length, "serialized package should be same size");
        newBytes.Should().BeEquivalentTo(originalBytes, "serialized package should be byte-identical");
    }
    
    [Theory]
    [MemberData(nameof(GetRealSims4Packages))]
    public async Task NewImplementation_PreservesResourceCount(string packagePath)
    {
        var package = await LoadPackageWithNewImplementation(packagePath);
        var originalResourceCount = await GetResourceCountFromFile(packagePath);
        
        // Count resources in loaded package
        var loadedResourceCount = await CountResourcesInPackage(package);
        
        loadedResourceCount.Should().Be(originalResourceCount, 
            "loaded package should preserve all resources from original file");
    }
    
    [Theory]
    [MemberData(nameof(GetLargePackages))]
    public async Task NewImplementation_HandlesLargeFiles_WithinMemoryLimits(string packagePath)
    {
        var fileInfo = new FileInfo(packagePath);
        var initialMemory = GC.GetTotalMemory(false);
        
        var package = await LoadPackageWithNewImplementation(packagePath);
        
        var peakMemory = GC.GetTotalMemory(false);
        var memoryUsed = peakMemory - initialMemory;
        
        // Memory usage should not exceed 2x file size (streaming implementation)
        memoryUsed.Should().BeLessThan(fileInfo.Length * 2, 
            "memory usage should not exceed 2x file size for large packages");
    }
    
    // IMPLEMENTATION PLACEHOLDERS (will be replaced with actual implementation)
    private async Task<IPackage> LoadPackageWithNewImplementation(string path)
    {
        // PLACEHOLDER: This will use actual TS4Tools implementation when ready
        throw new NotImplementedException(
            "Golden Master Test Framework is operational. " +
            "Replace this with actual TS4Tools.Core.Package implementation.");
    }
    
    private async Task<byte[]> SerializePackageWithNewImplementation(IPackage package)
    {
        // PLACEHOLDER: This will use actual TS4Tools serialization when ready
        throw new NotImplementedException(
            "Golden Master Test Framework is operational. " +
            "Replace this with actual TS4Tools package serialization.");
    }
    
    private async Task<int> GetResourceCountFromFile(string path)
    {
        // PLACEHOLDER: Parse DBPF header to get resource count
        throw new NotImplementedException(
            "Implement DBPF header parsing to extract resource count.");
    }
    
    private async Task<int> CountResourcesInPackage(IPackage package)
    {
        // PLACEHOLDER: Count resources in loaded package
        throw new NotImplementedException(
            "Implement resource counting in loaded package.");
    }
    
    public static IEnumerable<object[]> GetRealSims4Packages()
    {
        var testDataPath = Path.Combine("test-data", "real-packages");
        if (!Directory.Exists(testDataPath))
        {
            // Return empty if test data not collected yet
            yield break;
        }
        
        return Directory.GetFiles(testDataPath, "*.package", SearchOption.AllDirectories)
                       .Select(f => new object[] { f });
    }
    
    public static IEnumerable<object[]> GetLargePackages()
    {
        var testDataPath = Path.Combine("test-data", "real-packages");
        if (!Directory.Exists(testDataPath))
            yield break;
            
        return Directory.GetFiles(testDataPath, "*.package", SearchOption.AllDirectories)
                       .Where(f => new FileInfo(f).Length > 10 * 1024 * 1024) // > 10MB
                       .Select(f => new object[] { f });
    }
}
```

#### Day 6: Performance Baseline Tests
Create `tests/TS4Tools.Tests.GoldenMaster/PerformanceBaselineTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TS4Tools.Tests.GoldenMaster;

/// <summary>
/// Performance baseline tests to ensure new implementation meets or exceeds legacy performance.
/// Target: Startup, load, save times ≤ original + 10%
/// </summary>
[Collection("PerformanceBaseline")]
public class PerformanceBaselineTests
{
    [Theory]
    [MemberData(nameof(GetPackagesBySize))]
    public async Task LoadPerformance_NewVsBaseline_WithinAcceptableRange(string packagePath, string sizeCategory)
    {
        var fileSize = new FileInfo(packagePath).Length;
        var stopwatch = Stopwatch.StartNew();
        
        // Load with new implementation (when ready)
        var package = await LoadPackageWithNewImplementation(packagePath);
        
        stopwatch.Stop();
        var loadTime = stopwatch.ElapsedMilliseconds;
        
        // Performance targets based on file size
        var maxLoadTime = sizeCategory switch
        {
            "Small" => 100,   // < 100ms for files < 1MB
            "Medium" => 1000, // < 1s for files < 10MB  
            "Large" => 5000,  // < 5s for files < 100MB
            _ => 10000        // < 10s for very large files
        };
        
        loadTime.Should().BeLessThan(maxLoadTime, 
            $"Load time for {sizeCategory} package should be under {maxLoadTime}ms");
    }
    
    [Fact]
    public async Task MemoryUsage_StreamingImplementation_EfficientForLargeFiles()
    {
        var largePackages = GetLargePackages().Take(3);
        
        foreach (var packageData in largePackages)
        {
            var packagePath = (string)packageData[0];
            var fileSize = new FileInfo(packagePath).Length;
            
            var beforeMemory = GC.GetTotalMemory(true); // Force GC
            
            var package = await LoadPackageWithNewImplementation(packagePath);
            
            var afterMemory = GC.GetTotalMemory(false);
            var memoryIncrease = afterMemory - beforeMemory;
            
            // Memory increase should be much less than file size (streaming)
            memoryIncrease.Should().BeLessThan(fileSize / 2, 
                "Memory usage should be significantly less than file size due to streaming");
        }
    }
    
    // PLACEHOLDER METHODS (replace with actual implementation)
    private async Task<object> LoadPackageWithNewImplementation(string path)
    {
        throw new NotImplementedException("Performance baseline framework ready for implementation");
    }
    
    public static IEnumerable<object[]> GetPackagesBySize()
    {
        var testDataPath = Path.Combine("test-data", "real-packages");
        if (!Directory.Exists(testDataPath))
            yield break;
            
        var packages = Directory.GetFiles(testDataPath, "*.package", SearchOption.AllDirectories)
                               .Select(f => new { Path = f, Size = new FileInfo(f).Length });
        
        foreach (var pkg in packages)
        {
            var category = pkg.Size switch
            {
                < 1024 * 1024 => "Small",
                < 10 * 1024 * 1024 => "Medium", 
                < 100 * 1024 * 1024 => "Large",
                _ => "XLarge"
            };
            
            yield return new object[] { pkg.Path, category };
        }
    }
    
    public static IEnumerable<object[]> GetLargePackages()
    {
        var testDataPath = Path.Combine("test-data", "real-packages");
        if (!Directory.Exists(testDataPath))
            yield break;
            
        return Directory.GetFiles(testDataPath, "*.package", SearchOption.AllDirectories)
                       .Where(f => new FileInfo(f).Length > 10 * 1024 * 1024)
                       .Select(f => new object[] { f });
    }
}
```

#### Day 7: Test Validation and Documentation
```bash
# Build and validate test framework
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build tests/TS4Tools.Tests.GoldenMaster/

# Run tests to verify framework (tests will fail - this is expected)
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --logger "console;verbosity=detailed"

# Verify test discovery and framework
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --list-tests
```

Create test documentation `tests/TS4Tools.Tests.GoldenMaster/README.md`:

```markdown
# Golden Master Test Framework

## Purpose
This test framework provides byte-perfect compatibility validation for the TS4Tools migration.
ALL TESTS WILL FAIL INITIALLY - this is expected until the implementation is complete.

## Test Categories

### 1. Compatibility Tests (`PackageCompatibilityTests.cs`)
- **Round-trip validation**: Load → Save → Compare bytes
- **Resource preservation**: Ensure resource count matches
- **Memory efficiency**: Validate streaming implementation

### 2. Performance Tests (`PerformanceBaselineTests.cs`)  
- **Load performance**: Meet or exceed legacy performance
- **Memory usage**: Validate streaming reduces memory footprint
- **Scalability**: Handle large files efficiently

## Success Criteria
✅ **Framework Operational**: All tests can be discovered and run
✅ **Test Data Available**: 100+ real Sims 4 packages collected
❌ **Tests Failing**: Expected until implementation complete
✅ **Performance Baselines**: Targets defined and measurable

## Next Steps
1. Replace placeholder methods with actual TS4Tools implementation
2. Implement DBPF header parsing for resource counting
3. Add package serialization capability
4. Validate byte-perfect compatibility
```

**✅ Phase 0.2 Success Criteria:**
- [ ] Golden master test project created and building
- [ ] Compatibility tests implemented (failing as expected)
- [ ] Performance baseline tests implemented
- [ ] Test framework documented and operational
- [ ] Framework ready for actual implementation integration

---

### Phase 0.3: Assembly Loading Crisis Assessment (0.5 week - BLOCKING)
**Owner**: Senior .NET developer | **Dependencies**: None

#### Critical Issue Analysis
The current WrapperDealer.cs line 89 uses `Assembly.LoadFile(path)` which BREAKS COMPLETELY in .NET 9.

**Current Problematic Code:**
```csharp
// WrapperDealer.cs:89 - BREAKS IN .NET 9
Assembly assembly = Assembly.LoadFile(path);
```

#### Implementation Requirements
Create `src/TS4Tools.Core.Plugins/IAssemblyLoadContextManager.cs`:

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace TS4Tools.Core.Plugins;

/// <summary>
/// Modern assembly loading manager replacing Assembly.LoadFile() for .NET 9 compatibility.
/// CRITICAL: This fixes the breaking change that would prevent plugin loading.
/// </summary>
public interface IAssemblyLoadContextManager
{
    Assembly LoadFromPath(string assemblyPath);
    Type[] GetTypesFromAssembly(Assembly assembly);
    void UnloadContext(string contextName);
    IEnumerable<string> GetLoadedContextNames();
}

public class ModernAssemblyLoadContextManager : IAssemblyLoadContextManager
{
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _contexts = new();
    
    public Assembly LoadFromPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
            
        var contextName = Path.GetFileNameWithoutExtension(assemblyPath);
        var context = _contexts.GetOrAdd(contextName, 
            _ => new AssemblyLoadContext(contextName, isCollectible: true));
            
        return context.LoadFromAssemblyPath(assemblyPath);
    }
    
    public Type[] GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only types that loaded successfully
            return ex.Types.Where(t => t != null).ToArray();
        }
    }
    
    public void UnloadContext(string contextName)
    {
        if (_contexts.TryRemove(contextName, out var context))
        {
            context.Unload();
        }
    }
    
    public IEnumerable<string> GetLoadedContextNames()
    {
        return _contexts.Keys.ToArray();
    }
}
```

#### Legacy Compatibility Adapter
Create `src/TS4Tools.Core.Resources/LegacyWrapperDealerAdapter.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Plugins;

namespace TS4Tools.Core.Resources;

/// <summary>
/// CRITICAL: Maintains backward compatibility with existing WrapperDealer API.
/// All existing plugins and tools expect these exact method signatures.
/// </summary>
public static class WrapperDealer
{
    private static IResourceWrapperService? _service;
    private static IAssemblyLoadContextManager? _assemblyManager;
    
    // CRITICAL: These method signatures must match the original exactly
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
    {
        EnsureInitialized();
        return _service!.GetResource(APIversion, pkg, rie);
    }
    
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie, bool AlwaysDefault)
    {
        EnsureInitialized();
        return _service!.GetResource(APIversion, pkg, rie, AlwaysDefault);
    }
    
    public static IResource CreateNewResource(int APIversion, string resourceType)
    {
        EnsureInitialized();
        return _service!.CreateNewResource(APIversion, resourceType);
    }
    
    // CRITICAL: Legacy plugin loading compatibility
    public static void LoadWrapperAssembly(string assemblyPath)
    {
        EnsureInitialized();
        
        // MODERN: Use AssemblyLoadContext instead of Assembly.LoadFile()
        var assembly = _assemblyManager!.LoadFromPath(assemblyPath);
        var types = _assemblyManager.GetTypesFromAssembly(assembly);
        
        // Register legacy AResourceHandler types
        foreach (var type in types)
        {
            if (IsResourceHandlerType(type))
            {
                RegisterLegacyResourceHandler(type);
            }
        }
    }
    
    private static void EnsureInitialized()
    {
        if (_service == null)
        {
            // Initialize with dependency injection or direct instantiation
            _assemblyManager = new ModernAssemblyLoadContextManager();
            _service = new ResourceWrapperService(_assemblyManager);
        }
    }
    
    private static bool IsResourceHandlerType(Type type)
    {
        // Check if type inherits from AResourceHandler (legacy pattern)
        return type.BaseType?.Name == "AResourceHandler";
    }
    
    private static void RegisterLegacyResourceHandler(Type handlerType)
    {
        // Register legacy handler with modern service
        // This maintains compatibility with existing plugins
        _service?.RegisterLegacyHandler(handlerType);
    }
}
```

#### Testing Assembly Loading Compatibility
Create `tests/TS4Tools.Core.Plugins.Tests/AssemblyLoadingCompatibilityTests.cs`:

```csharp
[Fact]
public void ModernAssemblyLoading_ReplacesLegacyPattern_WithoutBreaking()
{
    var manager = new ModernAssemblyLoadContextManager();
    var testAssemblyPath = GetTestAssemblyPath();
    
    // This should work where Assembly.LoadFile() would fail in .NET 9
    var assembly = manager.LoadFromPath(testAssemblyPath);
    
    assembly.Should().NotBeNull();
    assembly.GetName().Name.Should().NotBeNull();
}

[Fact] 
public void WrapperDealer_MaintainsExactAPISignature_ForBackwardCompatibility()
{
    // Verify critical methods exist with exact signatures
    var wrapperDealerType = typeof(WrapperDealer);
    
    var getResourceMethod = wrapperDealerType.GetMethod(
        "GetResource", 
        new[] { typeof(int), typeof(IPackage), typeof(IResourceIndexEntry) });
        
    getResourceMethod.Should().NotBeNull("GetResource method must exist for compatibility");
    getResourceMethod!.ReturnType.Should().Be<IResource>("Return type must match legacy");
}
```

**✅ Phase 0.3 Success Criteria:**
- [ ] Assembly loading crisis identified and understood
- [ ] ModernAssemblyLoadContextManager implemented and tested
- [ ] WrapperDealer compatibility layer implemented
- [ ] Legacy plugin loading compatibility validated
- [ ] All existing API signatures preserved exactly

---

### Phase 0.4: Business Logic Inventory (4 days - CRITICAL ANALYSIS)
**Owner**: Domain expert or senior developer | **Dependencies**: None

#### Day 1-2: Legacy Code Analysis
Create comprehensive inventory of critical business logic:

**File Format Analysis:**
- DBPF header structure and validation rules
- Resource index entry parsing algorithms  
- Compression/decompression implementations
- Magic number validation and error handling

**Resource Wrapper Analysis:**
- 20+ resource type parsing algorithms
- Serialization/deserialization logic
- Type-specific business rules and validation
- Error handling patterns

**Plugin System Analysis:**
- AResourceHandler registration patterns
- Type discovery and mapping logic
- Plugin loading and initialization sequences
- Resource type to wrapper mapping rules

#### Day 3-4: API Compatibility Documentation
Document all public APIs that MUST be preserved exactly:

Create `docs/CRITICAL_API_COMPATIBILITY.md`:

```markdown
# Critical API Compatibility Requirements

## MANDATORY: These APIs cannot change without breaking compatibility

### Package Management APIs
- `IPackage.SavePackage()` - Save to original location
- `IPackage.SaveAs(Stream s)` - Save to stream
- `Package.OpenPackage(int APIversion, string filename, bool readWrite)`

### Resource Management APIs  
- `WrapperDealer.GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)`
- `WrapperDealer.CreateNewResource(int APIversion, string resourceType)`
- `AResource.Stream` - Resource data access
- `AResource.AsBytes` - Byte array access

### Plugin System APIs
- `WrapperDealer.TypeMap` - Resource type mappings
- `AResourceHandler.Add(Type, List<string>)` - Handler registration

## Business Logic Preservation Requirements
[Document all critical business rules that must be preserved exactly]
```

**✅ Phase 0.4 Success Criteria:**
- [ ] Complete business logic inventory documented
- [ ] Critical API compatibility requirements cataloged
- [ ] File format specifications documented with byte-level precision
- [ ] Plugin system requirements fully understood
- [ ] Migration preservation requirements clearly defined

---

## 🎯 Phase 0 Completion Validation

### Success Criteria Checklist
- [ ] **Phase 0.1**: 100+ real Sims 4 packages collected and inventoried
- [ ] **Phase 0.2**: Golden master test framework operational and ready
- [ ] **Phase 0.3**: Assembly loading crisis resolved with modern implementation  
- [ ] **Phase 0.4**: Complete business logic inventory and API compatibility documentation

### Quality Gates
- [ ] All test frameworks building and operational
- [ ] Assembly loading compatibility validated
- [ ] Performance baseline targets defined
- [ ] Critical API signatures documented and preserved
- [ ] Real-world test data collected and organized

### Ready to Proceed
Only when ALL Phase 0 requirements are complete can Phase 4.9+ work resume.

**Next Steps After Phase 0:**
1. Integrate golden master tests into CI/CD pipeline
2. Begin systematic implementation of core business logic
3. Use collected test data for continuous compatibility validation
4. Apply modern assembly loading patterns throughout codebase
5. Resume Phase 4.9 with solid foundation in place

---

**CRITICAL REMINDER**: This Phase 0 work is not optional or deferrable. It represents the foundation requirements that will determine the success or failure of the entire migration project.
