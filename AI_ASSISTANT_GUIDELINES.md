# AI Assistant Guidelines for TS4Tools Project

> **IMPORTANT:** This document contains essential guidelines for AI assistants working on the TS4Tools migration project. These guidelines ensure consistent, high-quality contributions aligned with project standards.

## 📋 Project Overview

- **MIGRATION_ROADMAP.md** - Active planning and future phases
- **CHANGELOG.md** - Historical record of completed accomplishments and technical details
- **AI_ASSISTANT_GUIDELINES.md** (this file) - Essential guidelines for AI assistants
- **Phase Completion:** Move detailed accomplishments to CHANGELOG.md, update roadmap status to ✅ COMPLETED

## 🚀 Environment Setup

### Shell Configuration
- **Shell:** Windows PowerShell v5.1 (not Command Prompt or Bash)
- **Working Directory:** Always `cd` to `c:\Users\nawgl\code\TS4Tools` before running any .NET commands
- **Command Syntax:** Use PowerShell syntax with `;` for command chaining, not `&&`

### Required Commands Pattern
```powershell
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build [project-path]
dotnet test [test-project-path]
dotnet run --project [project-path]
```

### Project Structure
- Source code: `src/TS4Tools.Core.*/`
- Tests: `tests/TS4Tools.*.Tests/`
- Solution file: `TS4Tools.sln` (in root)
- Package management: Central package management via `Directory.Packages.props`

### Build Requirements
- Always build from the TS4Tools directory root
- Use relative paths for project files
- Check `Directory.Packages.props` for package versions before adding new packages
- Run tests after making changes to verify functionality

## 🎯 Code Quality & Testability Guidelines

### Architecture Principles
- **Single Responsibility** - Each class/method should have one clear purpose
- **Dependency Injection** - Use constructor injection for all dependencies (avoid static dependencies)
- **Pure Functions** - Prefer stateless methods that return deterministic results
- **Interface Segregation** - Create focused interfaces for better testability
- **Composition over Inheritance** - Use composition for complex behaviors

### Unit Testing Best Practices
- **No Logic Duplication** - Tests should verify behavior, not reimplement logic
- **Arrange-Act-Assert** - Structure all tests with clear AAA pattern
- **Test Behavior, Not Implementation** - Focus on what the code does, not how
- **Use Test Builders** - Create fluent builders for complex test objects
- **Mock External Dependencies** - Use interfaces and dependency injection for isolation

### Code Design for Testability

```csharp
// ✅ GOOD - Testable design
public class PackageReader : IPackageReader
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PackageReader> _logger;
    
    public PackageReader(IFileSystem fileSystem, ILogger<PackageReader> logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Package> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Pure business logic - easily testable
        var bytes = await _fileSystem.ReadAllBytesAsync(filePath, cancellationToken);
        return ParsePackageData(bytes);
    }
    
    private static Package ParsePackageData(ReadOnlySpan<byte> data)
    {
        // Pure function - deterministic, easy to test
        // No dependencies, no side effects
    }
}

// ❌ BAD - Hard to test
public class PackageReader
{
    public Package Read(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath); // Static dependency - hard to test
        Console.WriteLine($"Reading {filePath}"); // Side effect - hard to verify
        
        // Complex logic mixed with I/O - test would need real files
        if (bytes.Length < 4) throw new Exception("Invalid file");
        // ... parsing logic ...
    }
}
```

### Testing Patterns to Follow

```csharp
// ✅ GOOD - Tests behavior without duplicating logic
[Fact]
public void ParsePackageData_WithValidHeader_ReturnsPackageWithCorrectVersion()
{
    // Arrange - Use test data builders
    var validPackageBytes = new PackageDataBuilder()
        .WithVersion(2)
        .WithResourceCount(5)
        .Build();
    
    // Act
    var result = PackageParser.ParsePackageData(validPackageBytes);
    
    // Assert - Test the outcome, not the implementation
    result.Should().NotBeNull();
    result.Version.Should().Be(2);
    result.Resources.Should().HaveCount(5);
}

// ❌ BAD - Duplicates parsing logic in test
[Fact]
public void ParsePackageData_WithValidHeader_ReturnsCorrectData()
{
    var bytes = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00 };
    
    var result = PackageParser.ParsePackageData(bytes);
    
    // This duplicates the parsing logic!
    var expectedVersion = BitConverter.ToInt32(bytes, 0);
    var expectedCount = BitConverter.ToInt32(bytes, 4);
    result.Version.Should().Be(expectedVersion);
    result.Resources.Should().HaveCount(expectedCount);
}
```

### Essential Testing Tools
- **FluentAssertions** - For readable test assertions
- **NSubstitute** - For mocking dependencies  
- **AutoFixture** - For generating test data
- **System.IO.Abstractions** - For testable file operations
- **Microsoft.Extensions.Logging.Testing** - For verifying log calls

## 🔍 Static Analyzer Guidelines

### Analyzer Override Policy
- **Fix First** - Always attempt to resolve analyzer warnings by improving code design
- **Pragma as Last Resort** - Only use `#pragma warning disable` when fixes would harm design
- **Document Overrides** - Always include comments explaining why the override is necessary
- **Scope Minimization** - Use the narrowest possible scope for pragma directives

### Pragma Directive Best Practices

```csharp
// ✅ GOOD - Documented override with clear justification
public class AHandlerDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    // Handler suspension pattern requires mutable access for performance
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected bool suspended;
#pragma warning restore CA1051
    
    public event EventHandler<EventArgs>? ListChanged;
    
    // Override necessary: Handler pattern requires field access for performance
    // Alternative property wrapper would cause allocation in hot path
    protected void OnListChanged() 
    {
        if (!suspended) ListChanged?.Invoke(this, EventArgs.Empty);
    }
}

// ❌ BAD - Blanket suppression without explanation
#pragma warning disable CA1051, CA1002, CA1019
public class SomeClass 
{
    public List<string> Items; // Why is this public? No explanation provided
}
#pragma warning restore CA1051, CA1002, CA1019
```

### Common Justified Overrides
- **CA1051** - Public fields in handler patterns where property overhead impacts performance
- **CA1002** - Concrete collections in APIs where generic interfaces would break compatibility
- **S2933** - Fields used in critical performance paths where properties add overhead
- **CA1019** - Attribute parameter storage when reflection requires field access

## 📋 Phase Completion Protocol

### Commit Message Format
Follow this format for commit messages:
```
feat(settings): implement modern IOptions-based settings system

- Replace legacy static Settings class with reactive IOptions pattern
- Add cross-platform JSON configuration support replacing Windows Registry
- Implement strongly-typed ApplicationSettings model with validation
- Add ApplicationSettingsService with change notifications
- Create LegacySettingsAdapter for backward compatibility

WHY: Legacy settings system was Windows-only and used static globals.
Modern settings enable cross-platform deployment, dependency injection,
configuration validation, and reactive updates. Essential foundation
for remaining migration phases.

TECHNICAL IMPACT:
- 30 unit tests added with 95%+ coverage
- Cross-platform JSON/XML configuration support
- Type-safe settings with compile-time validation
- Backward compatibility maintained via adapter pattern
```

### Required Documentation Updates for Each Phase
1. **Detailed Task Breakdown** - Expand each completed task with specific technical achievements
2. **Performance Metrics** - Document specific optimizations (Span<T>, Memory<T>, async patterns)
3. **Technical Benefits** - List concrete improvements (cross-platform, type safety, etc.)
4. **Test Coverage Details** - Show specific test suites and their coverage counts
5. **Quality Metrics Table** - Update with actual vs target metrics for the phase

### Phase Summary Template
```markdown
**Technical Achievements:**
- 🚀 **Performance**: [Specific optimizations implemented]
- 🔒 **Type Safety**: [Nullable reference types, strong typing improvements]
- 🌐 **Cross-Platform**: [Platform compatibility verified]
- 📊 **Modern Patterns**: [Async/await, dependency injection, modern C# features]

**Unit Tests:**
- [x] `[TestClass]Tests` - [Description] ([X] tests passing)
- [x] `[AnotherTestClass]Tests` - [Description] ([Y] tests passing)

**Coverage Target:** X%+ - **Current: Y%** ✅
```

### Quality Assurance Process
```powershell
# Required verification steps after each phase
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build                    # Verify all projects build
dotnet test --verbosity minimal # Verify all tests pass
dotnet run --project benchmarks/TS4Tools.Benchmarks # Run performance benchmarks
```

### Documentation Consolidation
- Move detailed accomplishments to `CHANGELOG.md` for historical tracking
- Update MIGRATION_ROADMAP.md status to COMPLETED but maintain phase structure for reference
- Remove any temporary phase-specific documents after consolidation
- Maintain MIGRATION_ROADMAP.md as active planning document, CHANGELOG.md as historical record

## 🔄 Phase Continuity & Integration Guidelines

### Pre-Phase Checklist
1. **Verify Foundation** - Ensure all previous phases build and test successfully
2. **Review Dependencies** - Check what interfaces/classes the new phase will need
3. **Update Roadmap** - Mark previous phase complete with full technical detail
4. **Prepare Project Structure** - Create new project directories and references
5. **Verify Package Versions** - Ensure `Directory.Packages.props` has needed packages

### During Phase Implementation
1. **Incremental Testing** - Add tests as you implement each component
2. **Performance Monitoring** - Run benchmarks if performance-critical components
3. **Documentation as You Go** - Update XML documentation for all public APIs
4. **Static Analysis** - Resolve analyzer warnings immediately, don't accumulate debt
5. **Cross-References** - Ensure new components integrate with existing ones

### Post-Phase Validation
```powershell
# Complete verification sequence
cd "c:\Users\nawgl\code\TS4Tools"
dotnet clean
dotnet restore  
dotnet build --verbosity minimal
dotnet test --verbosity minimal
# Verify ALL tests pass, not just new ones
```

### Integration Testing Between Phases
- Test that new components work with previously migrated ones
- Verify performance hasn't regressed from baseline
- Ensure cross-platform compatibility maintained
- Check that dependency injection patterns work correctly

### Quality Gate Criteria (All Must Pass)
- ✅ Build success across all projects
- ✅ All unit tests passing (100% pass rate)
- ✅ Static analysis clean (no high-severity issues)
- ✅ Performance benchmarks meet baseline
- ✅ Documentation complete for all public APIs
- ✅ Integration tests with previous components pass

## 📊 Progress Tracking Guidelines

### Progress Overview Maintenance
Always update the Progress Overview section in MIGRATION_ROADMAP.md:
- Update completion percentage (phases completed / 63 total phases)
- Update current target phase (next phase to be started)
- Update sprint metrics (tests passing, coverage, static analysis, build status)
- Update "Last Updated" date to current date
- Add newly completed phases to "✅ Completed Phases" list

### Sprint Metrics Update Guide
```powershell
# Get current test count
cd "c:\Users\nawgl\code\TS4Tools"
dotnet test --verbosity minimal | Select-String "Test summary"
# Look for: "Test summary: total: X, failed: 0, succeeded: X, skipped: 0"

# Update the metrics in Progress Overview:
# - Tests Passing: X/X (100%) ✅
# - Code Coverage: 95%+ ✅ (or actual measured percentage)
# - Static Analysis: All critical issues resolved ✅
# - Build Status: Clean builds across all projects ✅
```

### Future Phase Preparation
Before starting the next phase, update its status from:
`**Status:** ⏳ Not Started` to `**Status:** 🎯 **READY TO START**`

## 📝 Documentation Standards

### Project Structure Documentation
- Always include updated directory tree showing new components
- Mark completed packages with ✅ and their completion dates
- Show relationships between packages and their dependencies
- Document new project files and their purposes

### Status Update Format
```markdown
#### **X.Y Phase Name (Week Z)**
**Status:** ✅ **COMPLETED** - [Date]

**Tasks:**
- [x] **[Component] Migration**
  - [x] [Specific achievement with technical detail]
  - [x] [Another achievement with performance impact]
  - ✅ [Specific modern features implemented]
  - ✅ [Cross-platform compatibility verified]
  - [x] **Target:** `TS4Tools.Core.[Package]` package ✅

**Technical Achievements:**
- 🚀 **Performance**: [Detailed list]
- 🔒 **Type Safety**: [Specific improvements]
- 🌐 **Cross-Platform**: [Compatibility verified]
- 📊 **Modern Patterns**: [Features implemented]
```

### Multi-Phase Summary Sections
After completing multiple related phases, add comprehensive summary sections:
```markdown
### **🎉 Phase X.Y-X.Z Summary: [Milestone] Complete**

**Project Structure Established:**
```
[Updated directory tree with all new components]
```

**Technical Decisions & Benefits Realized:**
- 🌐 **Cross-Platform Ready**: [Specific platforms verified]
- 🚀 **Performance Optimized**: [Specific optimizations implemented]
- 🔒 **Type Safe**: [Type safety improvements]
- 🏗️ **Modern Architecture**: [Architectural patterns implemented]
- 📊 **Quality Assured**: [Quality measures implemented]
- 🧪 **Test Driven**: [Test coverage and counts]
- 📚 **Well Documented**: [Documentation completeness]

**Quality Metrics Achieved:**
| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| [Metric 1] | [Target] | [Actual] | [Status] |
```

## 🚀 Performance Guidelines

### Modern .NET Patterns
- Use `Span<T>` and `Memory<T>` for zero-allocation scenarios
- Implement async/await patterns for all I/O operations
- Use `CancellationToken` support throughout
- Prefer `IAsyncEnumerable<T>` for streaming operations

### Memory Management
- Implement proper `IDisposable` and `IAsyncDisposable` patterns
- Use `using` statements for automatic resource cleanup
- Avoid unnecessary allocations in hot paths
- Use object pooling for frequently allocated objects

### Performance Testing
- Run BenchmarkDotNet benchmarks for critical paths
- Establish performance baselines before optimization
- Monitor memory usage and GC pressure
- Test with realistic data sizes

## 🎯 Current Project Status

**Overall Completion:** 33% (21/63 total phases completed)  
**Current Phase:** Phase 4.2 Ready to Start  
**Last Updated:** August 5, 2025  

**Recent Achievements:**
- ✅ All Foundation Phases (1-3) Complete
- ✅ Phase 4.1.1-4.1.7 Complete
- ✅ Phase 4.7 Testing Quality Remediation Complete
- ✅ 90/90 tests passing (100% success rate)

**Next Priority:** Begin Phase 4.2 Core Game Content Wrappers according to MIGRATION_ROADMAP.md

---

*This document should be consulted before starting any work on the TS4Tools project to ensure consistency with established patterns and standards.*
