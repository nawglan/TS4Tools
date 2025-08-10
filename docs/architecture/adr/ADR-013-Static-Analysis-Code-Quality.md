# ADR-013: Static Analysis and Code Quality Standards

**Status:** Accepted  
**Date:** August 8, 2025  
**Deciders:** Architecture Team, Development Team, Quality Assurance Team

## Context

The TS4Tools project represents a critical modernization of legacy Sims4Tools with significantly higher quality requirements:

1. **Legacy Technical Debt**: Original codebase has accumulated significant technical debt
2. **Cross-Platform Requirements**: Code must work reliably across Windows, macOS, and Linux  
3. **Community Impact**: Tools are used by thousands of modders and content creators
4. **Long-term Maintenance**: Modern codebase must be maintainable for years to come
5. **Performance Criticality**: Package processing operations must be efficient and reliable

Without systematic code quality enforcement, the project risks recreating the maintainability issues of the legacy codebase.

## Decision

We will implement **comprehensive static analysis and code quality standards** with the following components:

1. **Multi-Layer Analysis**: Compiler warnings, Roslyn analyzers, SonarAnalyzer, and custom rules
2. **Graduated Enforcement**: Warnings → Errors progression with clear timelines
3. **Quality Gates**: Automated quality checks in CI/CD pipeline
4. **Suppression Policy**: Controlled suppression with justification requirements
5. **Team Standards**: Coding conventions and architectural guidelines

## Rationale

### Quality Requirements Analysis

#### Reliability Requirements

- Zero package corruption due to code defects
- Consistent behavior across all supported platforms
- Graceful error handling and recovery
- Memory safety for long-running operations

#### Maintainability Requirements  

- Code must be readable and understandable by new team members
- Consistent patterns and conventions across codebase
- Minimal cyclomatic complexity for testability
- Clear separation of concerns and responsibilities

#### Performance Requirements

- Static analysis can identify performance anti-patterns early
- Memory allocation analysis prevents performance degradation
- Async/await pattern enforcement for UI responsiveness
- Resource disposal verification prevents memory leaks

### Tool Selection Rationale

#### Microsoft.CodeAnalysis.Analyzers (Selected)

- **Pros**: Built-in .NET integration, comprehensive coverage
- **Cons**: Limited customization options
- **Decision**: Primary foundation for analysis

#### SonarAnalyzer.CSharp (Selected)

- **Pros**: Excellent coverage, proven enterprise tool
- **Cons**: Some false positives, configuration complexity  
- **Decision**: Secondary layer for advanced analysis

#### Custom Roslyn Analyzers (Selected for Specific Cases)

- **Pros**: Project-specific rules, exact control
- **Cons**: Development and maintenance overhead
- **Decision**: Only for critical project-specific patterns

#### Alternative Tools Considered but Rejected

- **PVS-Studio**: Excellent but licensing costs too high
- **Resharper**: IDE-specific, not CI/CD friendly
- **FxCop Legacy**: Deprecated, replaced by Roslyn analyzers

## Architecture Design

### Analysis Configuration Hierarchy

#### 1. Solution-Level Configuration (.editorconfig)

```ini
# Root EditorConfig file
root = true

[*.cs]
# Code Style Rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Language Rules  
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
csharp_style_namespace_declarations = file_scoped:warning

# Naming Conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.required_prefix = I
dotnet_naming_style.pascal_case.capitalization = pascal_case
```

#### 2. Project-Level Analysis (Directory.Build.props)

```xml
<Project>
  <PropertyGroup>
    <!-- Enable all warnings as errors (graduated approach) -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591;CS1587;CS1572</WarningsNotAsErrors>
    
    <!-- Enable nullable reference types -->
    <Nullable>enable</Nullable>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    
    <!-- Code analysis configuration -->
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)CodeAnalysis.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0.78982">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

#### 3. Custom Analyzer Rules

```xml
<!-- CodeAnalysis.ruleset -->
<RuleSet Name="TS4Tools Code Analysis Rules" ToolsVersion="16.0">
  <!-- Microsoft.CodeQuality.Analyzers -->
  <Rule Id="CA1001" Action="Error" />  <!-- Types that own disposable fields should be disposable -->
  <Rule Id="CA1002" Action="Warning" /> <!-- Do not expose generic lists -->
  <Rule Id="CA1031" Action="Info" />   <!-- Do not catch general exception types -->
  
  <!-- Microsoft.CodeQuality.CSharp.Analyzers -->
  <Rule Id="CA1802" Action="Warning" /> <!-- Use literals where appropriate -->
  <Rule Id="CA1822" Action="Suggestion" /> <!-- Mark members as static -->
  
  <!-- SonarAnalyzer.CSharp -->
  <Rule Id="S1066" Action="Warning" /> <!-- Mergeable if statements should be combined -->
  <Rule Id="S1172" Action="Warning" /> <!-- Unused method parameters should be removed -->
  <Rule Id="S3776" Action="Warning" /> <!-- Cognitive complexity should not be too high -->
</RuleSet>
```

### Custom Analyzer Development

#### Project-Specific Rules

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PackageResourceAnalyzer : DiagnosticAnalyzer
{
    // Rule: Package resources must implement IDisposable correctly
    public static readonly DiagnosticDescriptor PackageResourceDisposalRule = new DiagnosticDescriptor(
        "TS4001",
        "Package resources must implement proper disposal",
        "Type '{0}' handles package resources but does not implement IDisposable",
        "Resource Management",
        DiagnosticSeverity.Error,
        true,
        "Package resources can cause memory leaks if not properly disposed");
        
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(PackageResourceDisposalRule);
        
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }
}
```

#### Architectural Pattern Enforcement

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]  
public class ServiceLifetimeAnalyzer : DiagnosticAnalyzer
{
    // Rule: Service classes must have correct lifetime registration
    // Rule: Controllers must not have singleton dependencies
    // Rule: Repository pattern must be followed for data access
}
```

## Implementation Strategy

### Phase 1: Foundation Setup (Week 1)

#### Basic Analyzer Integration

```xml
<!-- Enable basic Microsoft analyzers -->
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisMode>AllEnabledByDefault</AnalysisMode>
  <CodeAnalysisTreatWarningsAsErrors>false</CodeAnalysisTreatWarningsAsErrors>
</PropertyGroup>
```

#### Warning Resolution Strategy

1. **Critical Warnings** (Week 1): Fix immediately (resource leaks, null reference, async issues)
2. **Important Warnings** (Week 2): Address systematically (naming, style, complexity)  
3. **Style Warnings** (Week 3): Batch fix with automated tools
4. **Informational** (Ongoing): Address during normal development

### Phase 2: Advanced Analysis (Week 2)

#### SonarAnalyzer Integration

```xml
<PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0.78982">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

#### Custom Rule Development

- Develop project-specific analyzers for critical patterns
- Implement architectural constraint validation
- Create performance pattern detection rules

### Phase 3: Enforcement (Week 3)

#### Graduated Warning → Error Promotion

```xml
<!-- Week 3: Promote resolved warning categories to errors -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <!-- Only exclude warnings we're still working on -->
  <WarningsNotAsErrors>CS1591;S1135</WarningsNotAsErrors>
</PropertyGroup>
```

### Phase 4: CI/CD Integration (Week 4)

#### Quality Gate Implementation

```yaml
# Azure DevOps Pipeline
- task: DotNetCoreCLI@2
  displayName: 'Code Analysis'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration Release --verbosity normal'
    
- task: SonarCloudAnalyze@1
  displayName: 'Run SonarCloud Analysis'
  
- task: PublishCodeCoverageResults@1
  displayName: 'Publish Code Coverage'
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

## Suppression Policy and Guidelines

### Justified Suppression Categories

#### 1. External Library Integration

```csharp
// Acceptable: Third-party library requires specific pattern
#pragma warning disable CA1031 // Do not catch general exception types
try
{
    // Legacy library call that throws Exception
    var result = LegacyLibrary.ProcessPackage(data);
}
catch (Exception ex) // Required by legacy API
{
    _logger.LogError(ex, "Legacy library processing failed");
    throw new PackageProcessingException("Processing failed", ex);
}
#pragma warning restore CA1031
```

#### 2. Performance-Critical Code

```csharp
// Acceptable: Performance-critical path with justification
[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", 
    Justification = "Method accesses instance state in release builds")]
public unsafe byte* GetPackageDataPointer()
{
    // High-performance package data access
}
```

#### 3. Platform-Specific Code

```csharp
// Acceptable: Platform-specific implementation
#if WINDOWS
[SuppressMessage("Interoperability", "CA1401:PInvokesShouldNotBeVisible",
    Justification = "Windows-specific P/Invoke required for DDS compression")]
[DllImport("squishinterface_x64.dll")]
public static extern int CompressDDS(byte[] input, int length, byte[] output);
#endif
```

### Unacceptable Suppressions

```csharp
// UNACCEPTABLE: Blanket suppression without justification
#pragma warning disable CA1031 // Too broad, find specific solution

// UNACCEPTABLE: Suppressing correctness issues
#pragma warning disable CA1001 // Fix the disposable implementation instead

// UNACCEPTABLE: Suppressing security issues
#pragma warning disable CA2100 // Never suppress SQL injection warnings
```

## Quality Metrics and Monitoring

### Code Quality Dashboard

```csharp
public class CodeQualityMetrics
{
    // Maintainability metrics
    public int CyclomaticComplexity { get; set; }
    public int LinesOfCode { get; set; }
    public double MaintainabilityIndex { get; set; }
    
    // Quality metrics  
    public int TotalWarnings { get; set; }
    public int SuppressedWarnings { get; set; }
    public double CodeCoverage { get; set; }
    
    // Trend analysis
    public List<QualityTrend> WeeklyTrends { get; set; }
}
```

### Quality Gates for CI/CD

```yaml
quality_gates:
  # Blocking conditions
  - total_warnings: 0  # No warnings allowed in main branch
  - code_coverage: ">= 85%"  # Minimum coverage requirement
  - duplicated_lines: "< 3%"  # Duplication threshold
  - maintainability_rating: "A"  # SonarQube maintainability rating
  
  # Warning conditions
  - cognitive_complexity: "> 15"  # Flag complex methods
  - file_length: "> 500 lines"   # Flag large files
```

## Tool Integration and Workflow

### IDE Integration

```json
// VS Code settings.json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": false,
  
  // Code formatting
  "editor.formatOnSave": true,
  "csharp.format.enable": true,
  
  // Problem highlighting
  "problems.decorations.enabled": true
}
```

### Pre-commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run code analysis on changed files
dotnet build --verbosity quiet --nologo --no-restore
if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please fix errors before committing."
    exit 1
fi

# Run code formatting check
dotnet format --verify-no-changes --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Code formatting issues found. Run 'dotnet format' to fix."
    exit 1
fi

echo "✅ Code quality checks passed"
exit 0
```

## Benefits

### Development Benefits

- **Early Issue Detection**: Problems caught during development, not production
- **Consistent Code Quality**: Automated enforcement reduces review overhead
- **Knowledge Transfer**: Consistent patterns help new developers understand codebase
- **Refactoring Safety**: Analysis helps ensure refactoring doesn't break patterns

### Maintenance Benefits

- **Reduced Technical Debt**: Prevents accumulation of quality issues
- **Faster Bug Resolution**: Higher quality code has fewer subtle bugs
- **Easier Testing**: Lower complexity makes code more testable
- **Documentation**: Analysis rules serve as living documentation of standards

### Project Benefits

- **Community Confidence**: High-quality codebase builds user trust
- **Long-term Viability**: Maintainable code ensures project longevity
- **Performance**: Analysis catches performance anti-patterns early
- **Cross-Platform Reliability**: Quality enforcement reduces platform-specific issues

## Implementation Challenges and Solutions

### Challenge: Analysis Rule Overwhelm

- **Solution**: Graduated rollout with weekly targets
- **Metrics**: Track weekly warning reduction progress
- **Success Criteria**: Zero warnings in main branch within 4 weeks

### Challenge: Developer Productivity Impact

- **Solution**: IDE integration and automated fixing where possible
- **Metrics**: Measure time spent on analysis-related fixes
- **Success Criteria**: < 5% of development time spent on analysis fixes

### Challenge: False Positives

- **Solution**: Careful suppression policy with documentation
- **Metrics**: Track suppression reasons and validity
- **Success Criteria**: < 10% of suppressions are false positives

## Success Metrics

### Quality Metrics

1. **Zero Warnings**: Main branch has zero analysis warnings
2. **Code Coverage**: > 85% test coverage maintained
3. **Maintainability**: SonarQube maintainability rating A or B
4. **Cyclomatic Complexity**: Average method complexity < 5

### Process Metrics

1. **Build Success Rate**: > 95% of builds pass quality gates
2. **Review Efficiency**: < 30 minutes average PR review time for quality issues
3. **Developer Satisfaction**: Positive feedback on analysis tool helpfulness
4. **Defect Reduction**: 50% reduction in quality-related bugs

## Consequences

### Positive

- ✅ Consistent, high-quality codebase with automated enforcement
- ✅ Early detection of bugs, performance issues, and maintainability problems
- ✅ Improved developer knowledge through analysis rule education
- ✅ Reduced technical debt accumulation over time
- ✅ Enhanced confidence in cross-platform reliability

### Negative

- ❌ Initial productivity impact during rule adoption period
- ❌ Potential developer frustration with strict enforcement
- ❌ Analysis tool maintenance and configuration overhead
- ❌ False positive investigation and suppression management

### Neutral

- 📋 Need for team education on analysis rules and patterns
- 📋 Regular review and updates of quality standards
- 📋 Integration with development workflow and tools

## Related Decisions

- ADR-009: Testing Framework Standardization (quality analysis applies to test code)
- ADR-001: .NET 9 Framework (enables modern analysis capabilities)
- ADR-002: Dependency Injection (analysis validates DI patterns)
- ADR-011: Native Dependency Strategy (analysis validates platform abstractions)

---

**Implementation Status:** 🚧 **IN PROGRESS** - Basic analyzers enabled, graduated rollout underway  
**Review Date:** September 8, 2025  
**Document Owner:** Architecture Team, Development Team
