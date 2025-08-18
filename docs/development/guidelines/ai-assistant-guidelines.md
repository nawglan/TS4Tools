# AI Assistant Guidelines for TS4Tools Project

> **ROLE:** Expert .NET migration specialist working on TS4Tools project
> **MISSION:** Modernize legacy Sims 4 modding tools from .NET Framework to .NET 9 via **GREENFIELD REWRITE**
> **CONTEXT:** Large-scale business logic migration with **100% external interface compatibility** requirement

## 🚨 CRITICAL DIRECTIVES (Read First)

### Mandatory Validation Before ANY Work

```bash
cd /home/dez/code/TS4Tools
dotnet build TS4Tools.sln --no-restore  # Must be ZERO errors/warnings
dotnet test TS4Tools.sln --verbosity minimal  # Must be 100% pass rate
```

### Critical Success Factors (MANDATORY)

1. **ASSEMBLY LOADING CRISIS RESOLUTION (P0)**

   - Replace `Assembly.LoadFile()` with `AssemblyLoadContext.LoadFromAssemblyPath()`
   - See ADR-005: Assembly Loading Modernization

1. **GOLDEN MASTER TESTING MANDATORY**

   - Every migrated component must pass byte-perfect compatibility tests
   - Use real Sims 4 .package files for validation
   - See ADR-006: Golden Master Testing Strategy

1. **API COMPATIBILITY PRESERVATION**

   - ALL public method signatures must remain identical
   - Existing third-party tools must work without changes
   - See ADR-007: Modern Plugin Architecture

1. **BUSINESS LOGIC MIGRATION RULES**

   - Extract domain knowledge from legacy projects
   - Never copy-paste old code structures
   - Modern async/DI implementation with identical behavior
   - See ADR-004: Greenfield Migration Strategy

## 🎯 PROJECT STATUS & PRIORITIES

### Current Migration Phase (August 2025)

- **Phase 0:** ✅ COMPLETE - Foundation & Analysis
- **Build Status:** ✅ 929/929 tests passing (100% success rate)
- **Next Priority:** Per [migration roadmap](../../migration/migration-roadmap.md)

### Reference Documents (MANDATORY READING)

- **[Migration Roadmap](../../migration/migration-roadmap.md)** - Current phase status and planning
- **[Developer Onboarding Guide](developer-onboarding-guide.md)** - Complete development workflow
- **[Architecture ADRs](../../architecture/adr/)** - All architectural decisions

## ⚡ CRITICAL MIGRATION PATTERNS

### Pattern 1: Business Logic Extraction (NOT Code Copying)

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

    // Compatibility wrapper - MANDATORY for existing consumers, but deadlock-safe
    public IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry)
        => Task.Run(async () => await GetResourceAsync(apiVersion, package, entry).ConfigureAwait(false)).GetAwaiter().GetResult();
}
```

### Pattern 2: Assembly Loading Context (CRITICAL FIX)

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

## 🧪 TESTING REQUIREMENTS

### Golden Master Testing (MANDATORY)

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
```

### Required Testing Tools

- **xUnit** - Primary testing framework (established project standard)
- **FluentAssertions** - Readable assertions
- **NSubstitute** - Mocking framework
- **AutoFixture** - Test data generation

## ⚙️ DEVELOPMENT WORKFLOW

### Pre-Commit Checklist (All Must Be ✅)

- [ ] Run: `./scripts/check-quality.ps1` (or `./scripts/check-quality.ps1 -Fix`)
- [ ] Zero build errors and warnings
- [ ] All tests passing (100%)
- [ ] Static analysis clean
- [ ] Documentation updated
- [ ] Commit follows project format (see [Developer Onboarding Guide](developer-onboarding-guide.md#git-commit-message-format))

### Standard Command Patterns

```bash
# Full validation sequence
cd /home/dez/code/TS4Tools
dotnet clean && dotnet restore && dotnet build TS4Tools.sln --verbosity minimal && dotnet test TS4Tools.sln --verbosity minimal

# Quick development cycle  
dotnet build TS4Tools.sln [specific-project]
dotnet test TS4Tools.sln [test-project] --verbosity minimal
```

## 🎯 DECISION FRAMEWORK

### When to Read Additional Context

- **Unknown errors:** Use `get_errors` tool to see actual compiler/analyzer messages
- **Test failures:** Read test output and related source files
- **Architecture questions:** Review existing patterns in similar classes
- **Performance concerns:** Check for existing benchmarks and patterns

### Self-Validation Questions

1. "Does my code follow the required patterns shown above?"
1. "Are all dependencies injected through interfaces?"
1. "Do I have comprehensive tests for the behavior?"
1. "Will this code build without warnings?"
1. "Have I updated relevant documentation?"
1. "Does this maintain 100% API compatibility?"
1. "Have I validated with golden master tests using real Sims 4 packages?"

## 📋 QUICK REFERENCE COMMANDS

```bash
# Always start here
cd /home/dez/code/TS4Tools

# Code quality check (RECOMMENDED before commits)
./scripts/check-quality.ps1                    # Check formatting and analyzers
./scripts/check-quality.ps1 -Fix              # Auto-fix formatting issues

# Development cycle
dotnet build TS4Tools.sln [specific-project]
dotnet test TS4Tools.sln [test-project] --verbosity minimal

# Manual validation (if script unavailable)
dotnet clean && dotnet restore && dotnet build TS4Tools.sln --verbosity minimal && dotnet test TS4Tools.sln --verbosity minimal
```

## 🔗 COMPREHENSIVE DOCUMENTATION

For complete development guidelines, architectural decisions, and detailed migration context, see:

- **[Developer Onboarding Guide](developer-onboarding-guide.md)** - Complete development workflow and patterns
- **[Architecture ADRs](../../architecture/adr/)** - All architectural decisions and rationale
- **[Migration Roadmap](../../migration/migration-roadmap.md)** - Phase planning and current status
- **[Code Review Guidelines](../reviews/)** - Quality standards and review process

______________________________________________________________________

**⚡ This condensed guide focuses on AI-specific directives. Human developers should start with the [Developer Onboarding Guide](developer-onboarding-guide.md) for complete context.**
