# AI Assistant Guidelines for TS4Tools Project

> **ROLE:** You are an expert .NET migration specialist working on the TS4Tools project  
> **MISSION:** Modernize legacy Sims 4 modding tools from .NET Framework to .NET 9 via **GREENFIELD REWRITE**  
> **CONTEXT:** Large-scale business logic migration with **100% external interface compatibility** requirement  
> **APPROACH:** Extract domain knowledge, NOT code migration - modern implementation with identical external behavior

## � **PROJECT STATUS DOCUMENTATION**

### **Primary Status References**

- **Implementation Status:** See [CHANGELOG.md](CHANGELOG.md) - detailed implementation history, completed features, and technical achievements
- **Phase Completion Status:** See [MIGRATION_ROADMAP.md](MIGRATION_ROADMAP.md) - current phase status, upcoming work, and overall project progression

### **Current Status (January 13, 2025)**

- **Phase 0:** ✅ COMPLETE - All foundation requirements implemented  
- **Build Status:** ✅ 929/929 tests passing (100% success rate)
- **Critical Blockers:** ✅ RESOLVED - No P0 issues remaining
- **Next Phase:** Ready for Phase 1 progression

> **Important:** Always consult CHANGELOG.md for implementation details and MIGRATION_ROADMAP.md for phase planning before starting any work.

---

## �🚨 CRITICAL MIGRATION PHILOSOPHY

### **GREENFIELD REWRITE - NOT IN-PLACE MIGRATION**

- **Extract business logic patterns**, never copy code structures
- **Preserve 100% API compatibility** for existing tools and plugins
- **Modern .NET 9 implementation** with identical external behavior
- **Golden Master Testing** mandatory for every migrated component

## ⚡ CRITICAL DIRECTIVES (Read First)

### 🚨 MANDATORY VALIDATION BEFORE ANY WORK

```powershell
# ALWAYS execute this sequence before starting:
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build TS4Tools.sln --no-restore  # Must be ZERO errors/warnings
dotnet test TS4Tools.sln --verbosity minimal  # Must be 100% pass rate
```

### 🚨 CRITICAL SUCCESS FACTORS (MANDATORY)

1. **ASSEMBLY LOADING CRISIS RESOLUTION**
   - Replace `Assembly.LoadFile()` with `AssemblyLoadContext.LoadFromAssemblyPath()`
   - Priority P0 - BLOCKING issue that breaks .NET 9 compatibility

2. **GOLDEN MASTER TESTING MANDATORY**
   - Every migrated component must pass byte-perfect compatibility tests
   - Use real Sims 4 .package files for validation (Steam: `\Electronic Arts\The Sims 4\Data\Client\*.package`)
   - Round-trip operations must produce identical output

3. **API COMPATIBILITY PRESERVATION**
   - ALL public method signatures must remain identical
   - Existing third-party tools must work without changes
   - Plugin system must support legacy handlers via adapters

4. **BUSINESS LOGIC MIGRATION RULES**
   - Extract domain knowledge from 114+ projects (s4pi, s4pe, CS System Classes, s4pi Wrappers)
   - Never copy-paste old code structures
   - Modern async/DI implementation with identical behavior

5. **CROSS-PLATFORM REQUIREMENTS (SIMS4TOOLS CRITICAL)**
   - **Primary UI:** Avalonia UI (true cross-platform)
   - **Windows Fallback:** Optional WinForms compatibility mode (zero user friction)
   - **DDS Strategy:** Hybrid - keep high-performance native DLLs on Windows, managed fallback elsewhere
   - **Helper Tools:** All 4 major helpers (ModelViewer, ThumbnailHelper, DDSHelper, RLEHelper) need cross-platform ports
   - **File Operations:** Cross-platform paths, executable detection, process launching

6. **PERFORMANCE PARITY REQUIREMENTS**
   - **Startup Time:** ≤3-5 seconds (current baseline)
   - **Large Package Load:** Multi-GB files ≤original+10%  
   - **Memory Usage:** Stable or improved via streaming I/O
   - **UI Responsiveness:** 60+ FPS maintained
   - **Streaming I/O:** MANDATORY from Phase 1 for large file handling

7. **PLUGIN ECOSYSTEM PRESERVATION**
   - **20+ Resource Wrappers** must continue working (DeformerMapResource, CWALResource, CFLRResource, etc.)
   - **Legacy AResourceHandler Pattern** preserved via compatibility adapters
   - **Type Registration System** maintained (`WrapperDealer.TypeMap`, `WrapperDealer.Disabled`)
   - **Helper Integration APIs** unchanged (`.helper` file format, `IRunHelper` interface)

### 🎯 DECISION TREE: When to Use Which Approach

- **New feature/class** → Create with tests first (TDD)
- **Legacy modernization** → Refactor with comprehensive test coverage
- **Build errors** → Fix immediately, never suppress without justification
- **Performance issues** → Benchmark first, optimize with proof
- **External dependencies** → Abstract behind interfaces for testing

### 📋 PRE-COMMIT CHECKLIST (All Must Be ✅)

- [ ] Run code quality script: `.\scripts\check-quality.ps1` (or `.\scripts\check-quality.ps1 -Fix` to auto-fix)
- [ ] Zero build errors and warnings
- [ ] All tests passing (100%)
- [ ] Static analysis clean
- [ ] Code follows modern .NET patterns
- [ ] Documentation updated

## �️ ENVIRONMENT & COMMANDS

### System Configuration

- **OS:** Windows with PowerShell v5.1
- **Command Chaining:** Use `;` not `&&`
- **Working Directory:** `c:\Users\nawgl\code\TS4Tools` (ALWAYS cd here first)
- **Project Structure:**
  - Source: `src/TS4Tools.Core.*/`
  - Tests: `tests/TS4Tools.*.Tests/`

### Standard Command Patterns

```powershell
# Full validation sequence (use before/after major changes)
cd "c:\Users\nawgl\code\TS4Tools"; dotnet clean; dotnet restore; dotnet build TS4Tools.sln --verbosity minimal; dotnet test TS4Tools.sln --verbosity minimal

# Quick development cycle
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build TS4Tools.sln [specific-project]
dotnet test TS4Tools.sln [test-project] --verbosity minimal
```

## �️ ARCHITECTURE REQUIREMENTS

### Non-Negotiable Patterns

1. **Dependency Injection** - Constructor injection only, no static dependencies
2. **Interface Segregation** - Every service behind focused interface
3. **Pure Functions** - Stateless, deterministic methods where possible
4. **Async/Await** - All I/O operations must be async
5. **Cancellation** - CancellationToken support throughout

### Code Quality Standards

```csharp
// ✅ REQUIRED PATTERN - Always use this structure
public class [ServiceName] : I[ServiceName]
{
    private readonly I[Dependency] _dependency;
    
    public [ServiceName](I[Dependency] dependency) 
        => _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    
    public async Task<Result> DoWorkAsync(Parameters parameters, CancellationToken cancellationToken = default)
    {
        // Implementation with proper error handling
    }
}
```

### 🚨 CRITICAL MIGRATION PATTERNS (MANDATORY)

#### Pattern 1: Business Logic Extraction (NOT Code Copying)

```csharp
// ❌ FORBIDDEN: Direct code copying from legacy codebase
public class WrapperDealer { /* copy-pasted from Sims4Tools */ }

// ✅ REQUIRED: Extract business logic with modern implementation
public interface IResourceWrapperService
{
    Task<IResource> GetResourceAsync(int apiVersion, IPackage package, IResourceIndexEntry entry);
    IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry); // Sync compatibility
}

public class ResourceWrapperService : IResourceWrapperService
{
    private readonly IServiceProvider _serviceProvider;
    
    // Modern async implementation extracting WrapperDealer business logic
    public async Task<IResource> GetResourceAsync(int apiVersion, IPackage package, IResourceIndexEntry entry)
    {
        // EXTRACT business rules from original WrapperDealer.GetResource()
        // MODERN implementation but IDENTICAL behavior
        var resourceType = entry.ResourceType.ToString("X8");
        var wrapperType = await GetWrapperTypeAsync(resourceType);
        
        return wrapperType == null 
            ? await CreateDefaultResourceAsync(apiVersion, package, entry)
            : await CreateTypedResourceAsync(wrapperType, apiVersion, package, entry);
    }
    
    // Compatibility wrapper - MANDATORY for existing consumers
    public IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry)
        => GetResourceAsync(apiVersion, package, entry).GetAwaiter().GetResult();
}
```

#### Pattern 2: Golden Master Testing (MANDATORY)

```csharp
[Theory]
[MemberData(nameof(GetRealSims4Packages))]
public async Task MigratedComponent_ProducesIdenticalOutput(string packagePath)
{
    // STEP 1: Test new implementation
    var newPackage = await NewPackageService.LoadPackageAsync(packagePath);
    var newBytes = await newPackage.SerializeToBytesAsync();
    
    // STEP 2: Byte-perfect validation (MANDATORY)
    var referenceBytes = await LoadReferenceBytes(packagePath);
    Assert.Equal(referenceBytes, newBytes);
}

public static IEnumerable<object[]> GetRealSims4Packages()
{
    // CRITICAL: Must use real Sims 4 packages from Steam installation
    var steamPath = @"C:\Program Files (x86)\Steam\steamapps\common\The Sims 4\Data\Client";
    if (Directory.Exists(steamPath))
        return Directory.GetFiles(steamPath, "*.package").Take(10).Select(p => new object[] { p });
    
    return Directory.GetFiles("test-packages", "*.package").Select(p => new object[] { p });
}
```

#### Pattern 3: Assembly Loading Context (CRITICAL FIX)

```csharp
// ❌ BREAKS IN .NET 9: Original WrapperDealer.cs:89
Assembly assembly = Assembly.LoadFile(path);

// ✅ MODERN SOLUTION: Use AssemblyLoadContext
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

## 🧪 TESTING IMPERATIVES

### Test-First Approach

- **New Code:** Write tests before implementation (TDD)
- **Legacy Code:** Add tests during refactoring
- **Coverage Goal:** 100% for new code, improve existing incrementally

### Required Testing Patterns

**MANDATORY:** Use **xUnit** testing framework throughout the project (established standard with 83+ existing test files).

```csharp
// ✅ STANDARD TEST STRUCTURE - xUnit Framework
[Fact]
public async Task Should_[ExpectedBehavior]_When_[Scenario]()
{
    // Arrange - Set up test data and dependencies
    var fixture = new Fixture();
    var mockDependency = Substitute.For<IDependency>();
    var sut = new SystemUnderTest(mockDependency);
    
    // Act - Execute the method being tested
    var result = await sut.MethodAsync(parameters, CancellationToken.None);
    
    // Assert - Verify behavior with FluentAssertions
    result.Should().NotBeNull();
    result.Value.Should().Be(expectedValue);
    mockDependency.Received(1).ExpectedCall(Arg.Any<Parameter>());
}
```

### Essential Testing Tools

- **xUnit** - Primary testing framework (established project standard)
- **FluentAssertions** - Readable assertions
- **NSubstitute** - Mocking framework  
- **AutoFixture** - Test data generation
- **System.IO.Abstractions** - File system mocking

## ⚙️ STATIC ANALYSIS PROTOCOL

### Warning Resolution Strategy

1. **First:** Fix by improving code design
2. **Last Resort:** Suppress with documented justification
3. **Never:** Ignore or suppress without reason

### Common Justified Suppressions

```csharp
#pragma warning disable CA1051 // Do not declare visible instance fields
// JUSTIFICATION: Performance-critical handler pattern requires public fields for hot path access
public readonly struct HandlerData { public int Value; }
#pragma warning restore CA1051
```

## 📋 PHASE COMPLETION WORKFLOW

### Step-by-Step Process (15-Month Greenfield Timeline)

1. **Phase Start:** Verify all tests passing, read MIGRATION_ROADMAP.md and SIMS4TOOLS_MIGRATION_DOCUMENTATION.md for context
2. **During Development:** Run tests after each significant change, maintain golden master compatibility
3. **Before Completion:** Execute full validation sequence, verify SIMS4TOOLS alignment
4. **Phase End:** Update documentation, commit with proper message format

### Current Migration Phase Context (August 7, 2025)

**Phase 0: Foundation & Analysis Complete** - Golden Master Implementation Required

- ✅ Modern .NET 9 solution structure established
- ✅ Cross-platform CI/CD pipeline setup  
- ✅ Golden master test framework created
- ❌ **CRITICAL GAP:** Golden master validation implementation missing
- ❌ **CRITICAL GAP:** Real Sims 4 package compatibility testing

**Next Phase Blocked Until:** Golden master byte-perfect validation working

### Phase-Specific AI Agent Priorities (SIMS4TOOLS Alignment)

#### Phase 1: Core Foundation (Months 4-7) - DBPF PERFECTION

- **Priority 1:** Modern DbpfHeader/PackageIndex with identical parsing logic
- **Priority 2:** Resource streaming infrastructure for multi-GB files
- **Critical Success:** 100% round-trip compatibility with existing .package files

#### Phase 2: Business Logic Migration (Months 8-9) - SYSTEMATIC APPROACH  

- **Priority 1:** Port all 20+ resource wrappers with golden master tests
- **Priority 2:** Migrate compression algorithms (ZLIB, RefPack, DDS hybrid strategy)
- **Critical Success:** All existing plugins work via adapter pattern

#### Phase 3: Cross-Platform UI (Months 10-13) - EXTENSIVE TESTING

- **Priority 1:** Avalonia UI with MainForm feature parity
- **Priority 2:** Helper applications cross-platform port (ModelViewer, ThumbnailHelper, etc.)
- **Critical Success:** All user workflows identical across Win/Linux/macOS

#### Phase 4: Finalization & Launch (Months 14-15)

- **Priority 1:** Comprehensive compatibility testing (99.9%+ real-world packages)
- **Priority 2:** Community beta testing and feedback integration  
- **Critical Success:** Production-ready replacement with user adoption plan

### Mandatory Pre-Commit Sequence

```powershell
cd "c:\Users\nawgl\code\TS4Tools"

# Option 1: Use automated quality check script (RECOMMENDED)
.\scripts\check-quality.ps1

# Option 2: Manual sequence (if script unavailable)
dotnet clean
dotnet restore  
dotnet build TS4Tools.sln --verbosity minimal --no-restore
dotnet test TS4Tools.sln --verbosity minimal

# All steps must succeed with no errors/warnings
```

### Required Documentation Updates

- **MIGRATION_ROADMAP.md:** Mark phase ✅ COMPLETED with date
- **CHANGELOG.md:** Document technical achievements and improvements
- **Progress Metrics:** Update completion percentage (current: 33%, 21/63 phases)

### Commit Message Format

```
feat(component): brief description

- Specific technical change 1
- Specific technical change 2  
- Test coverage improvements

WHY: [Business/technical justification]
TECHNICAL IMPACT: [Performance, maintainability, or compatibility improvements]
```

## 🔄 INTEGRATION & VALIDATION

### Cross-Phase Dependencies

- **Before Starting:** Verify foundation components are stable
- **During Integration:** Run full test suite, not just affected tests  
- **After Integration:** Validate with clean build from scratch

### Performance Validation

- **Modern .NET Patterns:** `Span<T>`, `Memory<T>`, async/await, `CancellationToken`
- **Benchmarking:** Use BenchmarkDotNet for critical paths
- **Memory:** Monitor GC pressure and allocation patterns

### Technology Stack Requirements (SIMS4TOOLS Alignment)

#### Core Framework Requirements

| Component | Current (Legacy) | Target (TS4Tools) | Migration Complexity | Compatibility Impact |
|-----------|------------------|-------------------|----------------------|----------------------|
| **Runtime** | .NET Framework 4.0 | **.NET 9 (greenfield)** | **LOW** | **MUST maintain API compatibility** |
| **Build System** | Legacy .csproj | **Modern SDK-style projects** | **LOW (greenfield)** | **MUST maintain assembly compatibility** |
| **Plugin System** | Legacy assembly loading | **Modern plugin architecture with legacy adapters** | **MEDIUM** | **MUST support existing plugins** |

#### UI Framework Strategy (CRITICAL DECISION)

| Framework | Role | Pros | Cons | SIMS4TOOLS Requirement |
|-----------|------|------|------|------------------------|
| **Avalonia UI** | **Primary** | True cross-platform, modern | Learning curve | **MANDATORY - Primary cross-platform choice** |
| **WinForms** | **Windows Fallback** | Familiar to users | Windows-only | **OPTIONAL - Reduce user friction on Windows** |
| **MAUI** | **REJECTED** | Microsoft support | Desktop overkill, performance overhead | **NOT RECOMMENDED - Wrong choice per SIMS4TOOLS** |

#### Native Dependencies Strategy

| Component | Current Dependency | Target Strategy | Implementation Priority |
|-----------|-------------------|-----------------|------------------------|
| **DDS Compression** | Native Win32/x64 DLLs | **Hybrid: Keep native on Windows + managed fallback** | **PHASE 2** |
| **3D Graphics** | WPF + Helix 3D | **Avalonia + Silk.NET OR keep WinForms ModelViewer on Windows** | **PHASE 3** |
| **File I/O** | Windows APIs | **Cross-platform APIs + async/streaming from day 1** | **PHASE 1** |

#### Helper Applications Migration

| Helper Tool | Current Tech | Target Strategy | Cross-Platform Priority |
|-------------|-------------|-----------------|------------------------|
| **ModelViewer** | WPF + Helix 3D | **Avalonia + Silk.NET OR optional Windows-only mode** | **HIGH** |
| **ThumbnailHelper** | WinForms | **Avalonia port** | **MEDIUM** |
| **DDSHelper** | WinForms | **Cross-platform with ImageSharp** | **HIGH** |
| **RLEHelper** | WinForms | **Cross-platform port** | **MEDIUM** |

## 📊 PROJECT STATUS & CONTEXT

### Current State (August 7, 2025)

- **Completion:** Phase 0 Framework Complete - Golden Master Validation Implementation Required
- **Active Priority:** Golden Master Testing Implementation (BLOCKING)
- **Test Status:** 90/90 tests passing (100%) ✅
- **Next Priority:** Per MIGRATION_ROADMAP.md and SIMS4TOOLS_MIGRATION_DOCUMENTATION.md specifications

### Key Project Files

- **MIGRATION_ROADMAP.md** - Current phase details and future planning (15-month timeline)
- **SIMS4TOOLS_MIGRATION_DOCUMENTATION.md** - Comprehensive migration requirements and constraints
- **CHANGELOG.md** - Historical record of completed work  
- **All files must be updated upon phase completion**

### Success Metrics (SIMS4TOOLS Alignment)

- **100% File Format Compatibility** - Byte-perfect round-trip testing with real Sims 4 packages
- **99.9%+ Real-World Package Compatibility** - Golden master validation against Steam installation packages
- **Performance Parity** - Startup ≤3-5s, large file handling ≤original+10%, memory usage stable/improved
- **100% API Surface Compatibility** - All existing third-party tools work without changes
- **Cross-Platform Functionality** - Win/Linux/macOS core features identical
- **Plugin Ecosystem Preservation** - All 20+ existing resource wrappers work via adapters

## 🎯 DECISION FRAMEWORK FOR AI ASSISTANTS

### When to Read Additional Context

- **Unknown errors:** Use `get_errors` tool to see actual compiler/analyzer messages
- **Test failures:** Read test output and related source files
- **Architecture questions:** Review existing patterns in similar classes
- **Performance concerns:** Check for existing benchmarks and patterns

### When to Create vs. Modify

- **New feature:** Create with TDD approach, tests first
- **Bug fix:** Add regression test, then fix
- **Refactoring:** Ensure tests exist, then modernize
- **Performance:** Benchmark existing, optimize, verify improvement

### Self-Validation Questions

1. "Does my code follow the required patterns shown above?"
2. "Are all dependencies injected through interfaces?"  
3. "Do I have comprehensive tests for the behavior?"
4. "Will this code build without warnings?"
5. "Have I updated relevant documentation?"
6. "Does this maintain SIMS4TOOLS compatibility requirements?"
7. "Have I validated with golden master tests using real Sims 4 packages?"

### User Migration Strategy (SIMS4TOOLS CRITICAL)

#### Progressive Enhancement Strategy

```csharp
// Allow users to opt into new features gradually
public interface IFeatureFlagService
{
    bool IsFeatureEnabled(string featureName);
    Task EnableFeatureAsync(string featureName, bool enabled);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly Dictionary<string, bool> _featureFlags = new()
    {
        { "StreamingPackageLoading", false }, // Off by default, user can enable
        { "AvaloniaUI", false },             // Falls back to WinForms if disabled
        { "AsyncFileOperations", true },      // Enabled by default for performance
        { "CrossPlatformHelpers", false }    // Platform-specific by default
    };
}
```

#### User Data Migration Requirements

```csharp
// MANDATORY: Preserve user settings across migration
public interface IUserDataMigrationService
{
    Task<MigrationResult> MigrateUserSettingsAsync(string oldVersion, string newVersion);
    Task<MigrationResult> MigrateRecentFilesAsync();
    Task<MigrationResult> MigrateCustomPluginConfigAsync();
    Task ValidateMigrationIntegrityAsync();
}
```

#### Rollback Strategy (Enterprise Critical)

```csharp
// MANDATORY: Ability to rollback if new version has issues
public interface IRollbackService
{
    Task<bool> CanRollbackAsync(Version fromVersion, Version toVersion);
    Task<RollbackResult> RollbackToVersionAsync(Version targetVersion);
    Task CreateRollbackPointAsync(string description);
}
```

### Community Impact Management

#### Plugin Ecosystem Transition

- **Legacy Support:** All existing AResourceHandler plugins MUST work via adapters
- **Documentation:** Clear migration path for plugin developers
- **Compatibility Testing:** Validate with real third-party plugins during beta
- **Support Timeline:** Legacy version LTS mode during transition period

#### Helper Tool Integration

- **Backward Compatibility:** All .helper files continue working
- **Cross-Platform Discovery:** Automatic detection of tools across platforms  
- **Execution Abstraction:** Platform-specific process launching patterns

---

## 📋 QUICK REFERENCE COMMANDS

```powershell
# Always start here
cd "c:\Users\nawgl\code\TS4Tools"

# Code quality check (RECOMMENDED before commits)
.\scripts\check-quality.ps1                    # Check formatting and analyzers
.\scripts\check-quality.ps1 -Fix              # Auto-fix formatting issues
.\scripts\check-quality.ps1 -Verbose          # Detailed output

# Development cycle
dotnet build TS4Tools.sln [specific-project]
dotnet test TS4Tools.sln [test-project] --verbosity minimal

# Manual validation (if script unavailable)
dotnet clean; dotnet restore; dotnet build TS4Tools.sln --verbosity minimal; dotnet test TS4Tools.sln --verbosity minimal

# After completion: Update MIGRATION_ROADMAP.md and CHANGELOG.md
```

---

## 🎯 **COMPREHENSIVE ALIGNMENT SUMMARY**

### ✅ **Critical Requirements Fully Addressed**

1. **SIMS4TOOLS Greenfield Approach** - Modern .NET 9 architecture with business logic extraction
2. **15-Month Timeline Integration** - Phase-specific guidance aligned with MIGRATION_ROADMAP.md
3. **Assembly Loading Crisis** - P0 priority with AssemblyLoadContext implementation patterns
4. **Golden Master Testing** - Mandatory byte-perfect validation with real Sims 4 packages
5. **Cross-Platform Strategy** - Avalonia primary + WinForms fallback, hybrid DDS compression
6. **Plugin Ecosystem Preservation** - Legacy adapter patterns for 20+ resource wrappers
7. **Performance Requirements** - Streaming I/O, startup ≤3-5s, memory efficiency targets
8. **User Migration Strategy** - Feature flags, data migration, rollback capabilities

### 🚨 **AI Agent Priority Matrix (Updated)**

| Priority | Component | Implementation Rule | Success Criteria |
|----------|-----------|-------------------|------------------|
| **P0** | Golden Master Implementation | Byte-perfect round-trip testing | 100% compatibility with real packages |
| **P0** | Assembly Loading Fix | `AssemblyLoadContext` patterns | .NET 9 compatibility achieved |
| **P1** | DBPF Format Migration | Modern async with identical parsing | All .package files load/save identically |
| **P1** | Resource Wrapper Migration | Extract business logic, modern patterns | All 20+ wrappers work via adapters |
| **P2** | Cross-Platform UI | Avalonia primary, WinForms fallback | Feature parity across Win/Linux/macOS |
| **P2** | Helper Tool Integration | Cross-platform ports with compatibility | All 4 major helpers work cross-platform |

### 📋 **Decision Framework Integration**

**When working on any phase:**

1. **Read SIMS4TOOLS_MIGRATION_DOCUMENTATION.md** for context and constraints
2. **Check MIGRATION_ROADMAP.md** for current phase priorities and timeline
3. **Validate against golden master tests** with real Sims 4 packages
4. **Ensure API compatibility** - no breaking changes to public interfaces
5. **Test cross-platform** - verify Win/Linux/macOS functionality
6. **Document migration decisions** - reference original code locations and business logic extraction

### 🔗 **Key Integration Points**

- **Business Logic Extraction:** Extract domain knowledge from 114+ legacy projects, never copy code
- **Technology Stack Alignment:** .NET 9, Avalonia UI, modern async patterns, cross-platform abstractions  
- **Testing Strategy:** Golden master tests + unit tests + integration tests + cross-platform validation
- **Community Impact:** Plugin ecosystem preservation, helper tool compatibility, user data migration
- **Timeline Execution:** 15-month greenfield approach with phase-specific deliverables and quality gates

**⚡ This AI Assistant Guidelines document now comprehensively reflects all critical requirements from SIMS4TOOLS_MIGRATION_DOCUMENTATION.md and provides phase-specific guidance for successful greenfield migration execution.**
