# Phase 0: Critical Requirements Implementation

## 🚨 URGENT - MUST COMPLETE BEFORE PHASE 4.9

**Status**: ❌ NOT STARTED - BLOCKING  
**Priority**: P0 CRITICAL  
**Timeline**: 2 weeks  
**Prerequisites**: None - this should have been done first  

---

## Phase 0.1: Real Package Data Collection (3 Days)

### Objective
Collect 100+ diverse real Sims 4 package files for golden master testing

### Tasks
- [ ] **Locate Steam Installation**
  ```powershell
  $steamPath = "C:\Program Files (x86)\Steam\steamapps\common\The Sims 4\Data\Client"
  if (Test-Path $steamPath) { Get-ChildItem "$steamPath\*.package" }
  ```

- [ ] **Create Test Data Repository**
  - Create `test-data/real-packages/` directory structure
  - Copy 100+ diverse .package files with different sizes and content types
  - Document each file's purpose and resource types contained

- [ ] **Package Analysis Script**
  ```csharp
  // Create tool to analyze packages and categorize by content
  public class PackageAnalyzer
  {
      public async Task<PackageMetadata> AnalyzeAsync(string packagePath)
      {
          // Catalog resource types, file sizes, complexity
      }
  }
  ```

### Success Criteria
- ✅ 100+ real Sims 4 packages collected
- ✅ Package analysis metadata generated
- ✅ Test data organized by content type

---

## Phase 0.2: Golden Master Test Framework (1 Week)

### Objective  
Implement mandatory byte-perfect compatibility testing infrastructure

### Tasks
- [ ] **Golden Master Test Pattern Implementation**
  ```csharp
  [Theory]
  [MemberData(nameof(GetRealSims4Packages))]
  public async Task MigratedComponent_ProducesIdenticalOutput(string packagePath)
  {
      // STEP 1: Load with new implementation
      var newPackage = await NewPackageService.LoadPackageAsync(packagePath);
      var newBytes = await newPackage.SerializeToBytesAsync();
      
      // STEP 2: Load reference bytes (original format)
      var referenceBytes = await File.ReadAllBytesAsync(packagePath);
      
      // STEP 3: Byte-perfect validation (MANDATORY)
      Assert.Equal(referenceBytes.Length, newBytes.Length);
      Assert.Equal(referenceBytes, newBytes);
  }
  ```

- [ ] **Round-Trip Testing Infrastructure**
  - Automated test generation for all collected packages
  - Performance baseline capture during testing
  - Memory usage profiling for large files

- [ ] **Test Data Provider Implementation**
  ```csharp
  public static IEnumerable<object[]> GetRealSims4Packages()
  {
      var testDataPath = Path.Combine("test-data", "real-packages");
      return Directory.GetFiles(testDataPath, "*.package")
          .Select(p => new object[] { p });
  }
  ```

### Success Criteria
- ✅ Golden master test framework operational
- ✅ All collected packages can be round-trip tested
- ✅ Performance baselines captured

---

## Phase 0.3: Assembly Loading Crisis Assessment (0.5 Week)

### Objective
Early validation and fix for Assembly.LoadFile() → AssemblyLoadContext migration

### Tasks
- [ ] **Current Usage Inventory**
  ```powershell
  # Find all Assembly.LoadFile usage
  Get-ChildItem -Recurse -Include "*.cs" | Select-String "Assembly\.LoadFile"
  ```

- [ ] **AssemblyLoadContext Implementation**
  ```csharp
  public interface IAssemblyLoadContextManager
  {
      Assembly LoadFromPath(string assemblyPath);
      void UnloadContext(string contextName);
  }

  public class ModernAssemblyLoadContextManager : IAssemblyLoadContextManager
  {
      private readonly ConcurrentDictionary<string, AssemblyLoadContext> _contexts = new();
      
      public Assembly LoadFromPath(string assemblyPath)
      {
          var contextName = Path.GetFileNameWithoutExtension(assemblyPath);
          var context = _contexts.GetOrAdd(contextName, 
              _ => new AssemblyLoadContext(contextName, isCollectible: true));
          return context.LoadFromAssemblyPath(assemblyPath);
      }
  }
  ```

- [ ] **Plugin Compatibility Testing**
  - Test current resource wrappers with new loading mechanism
  - Validate AResourceHandler registration patterns
  - Design legacy adapter architecture

### Success Criteria
- ✅ All Assembly.LoadFile() usage replaced
- ✅ Plugin loading verified with new architecture
- ✅ Legacy compatibility maintained

---

## Phase 0.4: Business Logic Inventory (4 Days)

### Objective
Systematic analysis and documentation of domain logic from 114+ legacy projects

### Tasks
- [ ] **Legacy Codebase Analysis**
  - Catalog all business rules in DBPF parsing
  - Document resource wrapper parsing algorithms  
  - Inventory compression/decompression logic
  - Map plugin registration and discovery patterns

- [ ] **Critical API Documentation**
  ```csharp
  // Document exact behavior requirements
  public static class WrapperDealer 
  {
      /// <summary>
      /// CRITICAL: This method signature must be preserved exactly.
      /// Any changes will break existing plugins and tools.
      /// </summary>
      public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
      {
          // Business logic that must be preserved identically
      }
  }
  ```

- [ ] **File Format Specification Review**
  - DBPF header structure requirements
  - Resource index entry formats
  - Compression algorithm specifications

### Success Criteria
- ✅ Complete business logic inventory documented
- ✅ Critical API behaviors cataloged
- ✅ File format requirements specified

---

## Implementation Priority

### Week 1: Foundation Data Collection
- Days 1-3: **Phase 0.1** - Real package data collection
- Days 4-5: **Phase 0.3** - Assembly loading assessment

### Week 2: Testing Infrastructure  
- Days 6-10: **Phase 0.2** - Golden master test framework
- Days 11-12: **Phase 0.4** - Business logic inventory

### Week 3: Resume Development
- **Phase 4.9** and beyond can proceed safely with proper foundation

---

## Risk Mitigation

### High Risk - Late Discovery
**Problem**: Finding compatibility issues in Phase 5+ (too late to fix easily)  
**Solution**: Phase 0 catches issues early when they're cheap to fix

### Medium Risk - Timeline Impact  
**Problem**: 2 weeks additional work  
**Solution**: Prevents months of rework later if compatibility fails

### Low Risk - Development Momentum
**Problem**: Interrupts current sprint momentum  
**Solution**: Better foundation ensures faster, more confident development

---

## Next Actions

1. **IMMEDIATE**: Stop Phase 4.9 development
2. **START**: Phase 0.1 (Real package data collection)
3. **COMPLETE**: All Phase 0 requirements before resuming normal development
4. **VALIDATE**: Golden master tests pass before declaring any future phase complete

This foundation work is **MANDATORY** for project success.
