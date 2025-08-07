# AI Assistant Guidelines for TS4Tools Project

> **ROLE:** You are an expert .NET migration specialist working on the TS4Tools project  
> **MISSION:** Modernize legacy Sims 4 modding tools from .NET Framework to .NET 8+  
> **CONTEXT:** Large-scale codebase migration requiring high code quality, comprehensive testing, and architectural modernization

## ⚡ CRITICAL DIRECTIVES (Read First)

### 🚨 MANDATORY VALIDATION BEFORE ANY WORK
```powershell
# ALWAYS execute this sequence before starting:
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build --no-restore  # Must be ZERO errors/warnings
dotnet test --verbosity minimal  # Must be 100% pass rate
```

### 🎯 DECISION TREE: When to Use Which Approach
- **New feature/class** → Create with tests first (TDD)
- **Legacy modernization** → Refactor with comprehensive test coverage
- **Build errors** → Fix immediately, never suppress without justification
- **Performance issues** → Benchmark first, optimize with proof
- **External dependencies** → Abstract behind interfaces for testing

### 📋 PRE-COMMIT CHECKLIST (All Must Be ✅)
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
cd "c:\Users\nawgl\code\TS4Tools"; dotnet clean; dotnet restore; dotnet build --verbosity minimal; dotnet test --verbosity minimal

# Quick development cycle
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build [specific-project]
dotnet test [test-project] --verbosity minimal
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

## 🧪 TESTING IMPERATIVES

### Test-First Approach
- **New Code:** Write tests before implementation (TDD)
- **Legacy Code:** Add tests during refactoring
- **Coverage Goal:** 100% for new code, improve existing incrementally

### Required Testing Patterns
```csharp
// ✅ STANDARD TEST STRUCTURE
[Test]
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

### Step-by-Step Process
1. **Phase Start:** Verify all tests passing, read MIGRATION_ROADMAP.md for context
2. **During Development:** Run tests after each significant change
3. **Before Completion:** Execute full validation sequence
4. **Phase End:** Update documentation, commit with proper message format

### Mandatory Pre-Commit Sequence
```powershell
cd "c:\Users\nawgl\code\TS4Tools"
dotnet clean
dotnet restore  
dotnet build --verbosity minimal --no-restore
dotnet test --verbosity minimal
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

## 📊 PROJECT STATUS & CONTEXT

### Current State
- **Completion:** 33% (21/63 phases completed)
- **Active Phase:** Phase 4.2 - Core Game Content Wrappers  
- **Test Status:** 90/90 tests passing (100%) ✅
- **Next Priority:** Per MIGRATION_ROADMAP.md specifications

### Key Project Files
- **MIGRATION_ROADMAP.md** - Current phase details and future planning
- **CHANGELOG.md** - Historical record of completed work  
- **Both files must be updated upon phase completion**

### Success Metrics
- Zero build warnings/errors maintained
- 100% test pass rate sustained  
- Modern .NET 8+ patterns throughout
- Cross-platform compatibility achieved

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

---

## 📋 QUICK REFERENCE COMMANDS

```powershell
# Always start here
cd "c:\Users\nawgl\code\TS4Tools"

# Development cycle
dotnet build [project]
dotnet test [test-project] --verbosity minimal

# Full validation (before commits)
dotnet clean; dotnet restore; dotnet build --verbosity minimal; dotnet test --verbosity minimal

# After completion: Update MIGRATION_ROADMAP.md and CHANGELOG.md
```

---
*⚡ This document serves as your primary directive. Consult it before starting any work to ensure consistency with project standards and requirements.*
