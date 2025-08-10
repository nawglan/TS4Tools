# ADR-004: Greenfield Migration Strategy

**Status:** Accepted  
**Date:** August 8, 2025  
**Deciders:** Architecture Team, Project Lead  

## Context

The TS4Tools project is migrating from the legacy Sims4Tools codebase built on .NET Framework 4.0. Two primary approaches were considered:

1. **In-place migration**: Gradually updating the existing 114+ project codebase
2. **Greenfield approach**: Complete rewrite extracting business logic while using modern architecture

The legacy codebase has significant technical debt including assembly loading issues that break in .NET 8+, Windows-only dependencies, and outdated architectural patterns that would be extremely difficult to modernize incrementally.

## Decision

We will adopt a **greenfield migration strategy** - creating a completely new codebase (Sims4Tools-NG) while carefully extracting and preserving the critical business logic from the original system.

## Rationale

### Critical Issues with In-Place Migration

| Issue | Impact | Greenfield Resolution |
|-------|---------|----------------------|
| **Assembly.LoadFile() Crisis** | **BLOCKING** - Breaks completely in .NET 8+ | Modern AssemblyLoadContext from day 1 |
| **Complex Build Dependencies** | 40+ interdependent legacy projects | Clean SDK-style projects |
| **Technical Debt Migration** | Would carry forward all legacy problems | Extract business logic only |
| **User Disruption Risk** | Changes could break existing installations | Original version stays stable |

### Strategic Advantages

#### 1. **Risk Elimination**

- **Zero Breaking Changes**: Original version remains untouched and stable
- **No Assembly Loading Crisis**: Modern .NET 9 patterns from start
- **User Confidence**: Current tools continue working during transition
- **Rollback Safety**: Can abandon new version without impact

#### 2. **Architecture Excellence**

- **Clean Architecture**: Proper separation of concerns from day 1
- **Modern Patterns**: Dependency injection, async/await throughout
- **Cross-Platform**: Built for Windows/Linux/macOS from start
- **Performance**: Streaming I/O and modern optimizations

#### 3. **Business Logic Preservation**

- **Domain Knowledge Migration**: Extract 114+ projects of file format expertise
- **API Compatibility**: Maintain identical public interfaces
- **Golden Master Testing**: Byte-perfect compatibility validation
- **Plugin Support**: Backward compatibility via adapter pattern

## Implementation Strategy

### Phase 0: Foundation (Months 1-3)

**Priority**: Solve assembly loading crisis and establish new architecture

```csharp
// Modern assembly loading replacement for WrapperDealer.cs:85
public interface IAssemblyLoadContextManager
{
    Assembly LoadFromPath(string assemblyPath);
}

public class ModernAssemblyLoadContextManager : IAssemblyLoadContextManager
{
    public Assembly LoadFromPath(string assemblyPath)
    {
        // .NET 9 compatible approach
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    }
}
```

### Phase 1: Core Migration (Months 4-7)  

**Priority**: DBPF package format with byte-perfect compatibility

```csharp
// Modern implementation with legacy business rules
public interface IPackageService
{
    Task<IPackage> LoadPackageAsync(string path);
    Task SavePackageAsync(IPackage package, string path);
}

public class PackageService : IPackageService
{
    // Same DBPF parsing logic as original, modern async implementation
    public async Task<IPackage> LoadPackageAsync(string path)
    {
        // Identical business rules to original Package.OpenPackage()
        // But with streaming I/O and modern patterns
    }
}
```

### Phase 2: Business Logic Migration (Months 8-9)

**Priority**: Resource wrappers with systematic testing

```csharp
// Legacy compatibility with modern implementation
public static class WrapperDealer 
{
    private static IResourceWrapperService _service;
    
    // Preserve exact API for backward compatibility
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
        => _service.GetResource(APIversion, pkg, rie);
}
```

### Golden Master Testing Strategy

**Critical Requirement**: Every migrated component must pass byte-identical tests

```csharp
[Theory]
[MemberData(nameof(RealSims4Packages))]
public async Task NewImplementation_ProducesIdenticalOutput(string packagePath)
{
    // Load with original implementation (reference)
    var originalPackage = Package.OpenPackage(0, packagePath, false);
    var originalBytes = originalPackage.Save();
    
    // Load with new implementation (test)
    var newPackage = await PackageService.LoadPackageAsync(packagePath);
    var newBytes = await newPackage.SerializeAsync();
    
    // MUST be byte-identical
    Assert.Equal(originalBytes, newBytes);
}
```

## Technology Stack Alignment

| Component | Decision | Rationale |
|-----------|----------|-----------|
| **Runtime** | .NET 9 | Latest performance, cross-platform (ADR-001) |
| **DI Container** | Microsoft.Extensions.DI | Standard, well-tested (ADR-002) |
| **UI Framework** | Avalonia UI | Cross-platform desktop (ADR-003) |
| **Plugin System** | Modern with legacy adapters | Backward compatibility |
| **Build System** | SDK-style projects | Modern tooling from day 1 |

## Migration Validation Criteria

### Compatibility Requirements (BLOCKING)

- ✅ 100% API compatibility with existing interfaces
- ✅ Byte-perfect package read/write compatibility  
- ✅ Existing plugin/wrapper compatibility via adapters
- ✅ Helper tool integration preserved

### Performance Requirements (HIGH)

- ✅ Startup time ≤ original +10%
- ✅ Large file handling ≤ original +10%
- ✅ Memory usage ≤ original or improved
- ✅ Cross-platform performance parity

### User Experience Requirements (HIGH)

- ✅ Drop-in replacement functionality
- ✅ Settings/preferences migration
- ✅ Identical workflow preservation
- ✅ Optional UI improvements

## Alternatives Considered

### In-Place Migration

**Pros:**

- Incremental progress visible to users
- Familiar codebase for existing contributors
- Lower initial development overhead

**Cons:**

- Assembly.LoadFile() crisis blocks progress (CRITICAL)
- Technical debt migration compounds complexity
- Risk of breaking existing user installations
- 40+ project interdependencies create update cascades
- Performance optimization limited by legacy constraints

**Verdict**: Rejected due to critical blocking issues

### Hybrid Approach (Partial Rewrite)

**Pros:**

- Could preserve some existing components
- Potentially faster initial development

**Cons:**

- Still inherits assembly loading problems
- Creates architectural inconsistency
- Doesn't solve cross-platform issues comprehensively
- Maintains some technical debt

**Verdict**: Rejected - doesn't solve core issues

## Consequences

### Positive

- **Risk-Free Development**: Original version stays stable
- **Modern Architecture**: Clean implementation from start
- **Cross-Platform Ready**: Built for all target platforms
- **Performance Optimized**: Modern patterns enable better performance
- **Maintainable Codebase**: Clean architecture reduces future maintenance

### Challenges

- **Extended Timeline**: 15 months to reach feature parity
- **Business Logic Complexity**: Requires careful domain knowledge extraction
- **Community Split**: Users on old/new versions during transition
- **Testing Overhead**: Golden master tests for every component essential

### Mitigation Strategies

- **Transparent Communication**: Regular progress updates to community
- **Beta Testing Program**: Early community validation
- **Documentation**: Comprehensive migration guides
- **Support Strategy**: Maintain old version during transition
- **Rollback Plan**: Can return to original if issues arise

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Compatibility Rate** | 99.9%+ | Automated test suite against real packages |
| **Performance Regression** | ≤ 10% | Benchmark suite vs original |
| **User Adoption** | 95%+ retention | Download metrics, user feedback |
| **Plugin Compatibility** | 100% via adapters | Existing plugin test suite |

## Related Decisions

- ADR-001: .NET 9 Framework Selection
- ADR-002: Dependency Injection Adoption  
- ADR-003: Avalonia Cross-Platform UI
- ADR-005: Assembly Loading Modernization
- ADR-006: Golden Master Testing Strategy
