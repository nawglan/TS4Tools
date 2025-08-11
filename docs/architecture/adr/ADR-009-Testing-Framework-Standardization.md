# ADR-009: Testing Framework Standardization

**Status:** Accepted
**Date:** August 8, 2025
**Deciders:** Architecture Team, Quality Assurance Team

## Context

The TS4Tools project requires a consistent testing framework across all components to ensure maintainability, developer productivity, and CI/CD pipeline efficiency. Multiple .NET testing frameworks are available, each with different strengths:

- **xUnit**: Modern, extensible, follows testing best practices
- **NUnit**: Mature, feature-rich, widely adopted
- **MSTest**: Microsoft's official framework, integrated with Visual Studio

The project currently has 83+ test files already using xUnit, establishing a de facto standard. However, this decision was never formally documented, creating potential inconsistency as the project scales.

## Decision

We will standardize on **xUnit** as the exclusive testing framework for the TS4Tools project.

## Rationale

### Technical Advantages of xUnit

1. **Modern Architecture**: Built from the ground up for .NET Core/.NET 5+
2. **Parallel Execution**: Excellent support for parallel test execution
3. **Extensibility**: Rich extensibility model for custom assertions and fixtures
4. **Community Support**: Strong ecosystem and community adoption
5. **Performance**: Generally faster execution compared to alternatives

### Project-Specific Benefits

1. **Consistency**: 83+ existing test files already use xUnit
2. **Migration Cost**: Zero migration cost since we're already standardized
3. **Team Familiarity**: Development team already experienced with xUnit patterns
4. **Tooling Integration**: Excellent integration with .NET CLI, Visual Studio, and CI/CD

### Comparison Matrix

| Feature | xUnit | NUnit | MSTest |
|---------|-------|-------|---------|
| .NET 9 Support | âœ… Excellent | âœ… Good | âœ… Good |
| Parallel Execution | âœ… Built-in | âœ… Available | âŒ Limited |
| Extensibility | âœ… Excellent | âœ… Good | âš ï¸ Limited |
| Community | âœ… Active | âœ… Mature | âš ï¸ Smaller |
| Performance | âœ… Fast | âœ… Good | âš ï¸ Slower |
| Current Usage | âœ… 83+ files | âŒ None | âŒ None |

## Architecture Implications

### Test Organization

```csharp
// Standard test class structure
public class PackageServiceTests
{
    private readonly ITestOutputHelper _output;

    public PackageServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ShouldLoadPackageSuccessfully()
    {
        // Arrange, Act, Assert pattern
    }

    [Theory]
    [InlineData("test1.package")]
    [InlineData("test2.package")]
    public void ShouldHandleMultiplePackageFormats(string filename)
    {
        // Parameterized tests
    }
}
```

### Fixture Management

```csharp
// Collection fixtures for shared state
[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

[Collection("Database Collection")]
public class DatabaseTests
{
    private readonly DatabaseFixture _fixture;

    public DatabaseTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
}
```

### Custom Assertions

```csharp
// Project-specific assertion extensions
public static class PackageAssertions
{
    public static void ShouldBeValidPackage(this Package package)
    {
        Assert.NotNull(package);
        Assert.True(package.IsValid);
        Assert.NotEmpty(package.Resources);
    }
}
```

## Implementation Guidelines

### 1. Test Structure Standards

- Use `[Fact]` for simple tests
- Use `[Theory]` with `[InlineData]` for parameterized tests
- Follow Arrange-Act-Assert pattern
- Use descriptive test method names

### 2. Assertion Libraries

- **Primary**: xUnit built-in assertions
- **Enhanced**: FluentAssertions for complex scenarios
- **Mocking**: NSubstitute (already established)

### 3. Test Categories

```csharp
// Use traits for test categorization
[Trait("Category", "Unit")]
[Trait("Category", "Integration")]
[Trait("Category", "Performance")]
```

### 4. CI/CD Integration

```xml
<!-- In test projects -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
```

## Migration Strategy

### Phase 1: Enforcement (Immediate)

- Document xUnit as mandatory in coding standards
- Update project templates to include xUnit references
- Add EditorConfig rules to enforce xUnit patterns

### Phase 2: Tooling (Week 1)

- Configure CI/CD pipelines for xUnit test discovery
- Set up test result reporting and coverage collection
- Create custom project templates with xUnit scaffolding

### Phase 3: Enhancement (Week 2)

- Develop project-specific assertion extensions
- Create shared test utilities and fixtures
- Implement performance benchmarking integration

## Benefits

### Developer Experience

- **Consistency**: Single framework reduces context switching
- **Productivity**: Rich tooling and IDE integration
- **Learning Curve**: Team already familiar with xUnit patterns

### Quality Assurance

- **Reliability**: Mature, battle-tested framework
- **Performance**: Fast test execution enables rapid feedback
- **Coverage**: Excellent code coverage integration

### Maintenance

- **Single Dependency**: Reduces framework maintenance burden
- **Community Support**: Active development and community
- **Long-term Viability**: Strong Microsoft and community backing

## Risks and Mitigations

### Risk: Framework Lock-in

- **Mitigation**: xUnit interfaces are standard .NET testing patterns
- **Impact**: Low - migration patterns are well-established if needed

### Risk: Learning Curve for New Developers

- **Mitigation**: xUnit is widely used in .NET community
- **Impact**: Low - extensive documentation and examples available

### Risk: Advanced Testing Scenarios

- **Mitigation**: xUnit extensibility covers complex scenarios
- **Impact**: Low - proven in large-scale projects

## Success Metrics

1. **Consistency**: 100% of test projects use xUnit
2. **Performance**: Test execution time < 5 minutes for full suite
3. **Coverage**: Maintain >90% code coverage with xUnit tests
4. **Developer Satisfaction**: Positive feedback on testing experience

## Related Standards

### Code Style

```csharp
// Test naming convention
public void Should_ThrowException_When_InputIsNull()
{
    // Test implementation
}
```

### Project Structure

```
tests/
â”œâ”€â”€ TS4Tools.Core.*.Tests/     # Unit tests
â”œâ”€â”€ TS4Tools.Tests.Integration/ # Integration tests
â”œâ”€â”€ TS4Tools.Tests.Common/     # Shared test utilities
â””â”€â”€ TS4Tools.Tests.Performance/ # Performance tests
```

## Consequences

### Positive

- âœ… Consistent testing approach across all components
- âœ… Reduced learning curve for developers
- âœ… Excellent tooling and CI/CD integration
- âœ… High-performance test execution
- âœ… Strong community and ecosystem support

### Negative

- âŒ Lock-in to specific testing framework (mitigated by standard patterns)
- âŒ Some advanced NUnit features not directly available
- âŒ Potential need for custom extensions for specialized scenarios

### Neutral

- ðŸ“‹ Requires documentation and training for new team members
- ðŸ“‹ Need to maintain xUnit-specific knowledge and best practices

## Related Decisions

- ADR-006: Golden Master Testing Strategy (validates xUnit choice for compatibility testing)
- ADR-002: Dependency Injection (xUnit fixtures integrate with DI patterns)
- ADR-001: .NET 9 Framework (xUnit provides excellent .NET 9 support)

---

**Implementation Status:** âœ… **COMPLETE** - Already implemented across 83+ test files
**Review Date:** September 8, 2025
**Document Owner:** Architecture Team, QA Team

