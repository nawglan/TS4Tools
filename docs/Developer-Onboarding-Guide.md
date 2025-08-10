# TS4Tools Developer Onboarding Guide

**Welcome to the TS4Tools Development Team!**

This guide will help you get up and running with the TS4Tools codebase,  
understand our architecture decisions, and contribute effectively to the project.

---

## 🚀 Quick Start (15 minutes)

### Prerequisites

- **Windows 10/11** (primary development platform)
- **Visual Studio 2022** (v17.9+) or **VS Code** with C# extension
- **.NET 9 SDK** (latest version)
- **Git** for version control
- **PowerShell 5.1+** (included with Windows)
- **Sims 4 Installation** (Steam/Origin) for Golden Master testing - see [Steam Installation Guide](#sims-4-setup-for-testing)

### Environment Setup

```powershell
# 1. Clone the repository
git clone https://github.com/nawglan/TS4Tools.git
cd TS4Tools

# 2. Restore packages and build
dotnet restore
dotnet build

# 3. Run tests to verify setup
dotnet test

# 4. Start the desktop application
dotnet run --project TS4Tools.Desktop
```

### Verify Installation

If everything is working correctly, you should see:

- ✅ Clean build with no errors
- ✅ All tests passing (95%+ success rate)
- ✅ Desktop application starts and displays main window

---

## 🚨 **CRITICAL: Golden Master Testing Requirements**

> **⚠️ MANDATORY:** All development work must pass Golden Master compatibility tests

### What is Golden Master Testing?

Golden Master Testing ensures **byte-perfect compatibility** with the original  
Sims4Tools. Every resource wrapper implementation must produce identical output  
to the legacy system.

### Sims 4 Setup for Testing

```powershell
# Option 1: Steam Installation (Recommended)
# Install from Steam (Game folder: C:\Program Files (x86)\Steam\steamapps\common\The Sims 4\)

# Option 2: EA App/Origin Installation  
# Install from EA App (Game folder: C:\Program Files\EA Games\The Sims 4\)

# Verify Golden Master tests work
cd "C:\Users\nawgl\code\TS4Tools"
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity minimal
```

### Golden Master Validation Workflow

```powershell
# Before starting any resource wrapper work
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --filter "Category!=Integration"

# After completing resource implementation
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity normal
```

**Expected Result**: All Golden Master tests should pass with **ZERO tolerance** for failures.

---

## 📋 **CRITICAL: Phase-Based Development Workflow**

TS4Tools development follows a **phase-based approach** with strict completion criteria.

### Current Phase Status

Check the current phase before starting work:

```powershell
# Always check current phase status
Get-Content "PHASE_4_14_COMPLETION_STATUS.md" | Select-String "STATUS"
```

### Phase Completion Requirements

**Before ANY phase can be marked complete:**

- [ ] All Golden Master tests passing for affected components (100%)
- [ ] Performance benchmarks within 10% of baseline
- [ ] Real Sims 4 package compatibility validated
- [ ] Assembly loading patterns use modern AssemblyLoadContext
- [ ] API signatures preserved exactly for backward compatibility

### Phase Handover Process

1. **Pre-Phase Checklist**: Check `PHASE_X_XX_CHECKLIST.md` for your assigned phase
2. **Implementation**: Follow test-driven development with Golden Master validation
3. **Completion Validation**: Run full validation sequence
4. **Documentation**: Update phase completion status
5. **Handover**: Prepare next phase prerequisites

---

## 🚨 **CRITICAL: Resource Wrapper Development Patterns**

> **Primary Work**: 95% of development involves implementing resource wrappers

### Standard Resource Wrapper Structure

Every resource wrapper follows this pattern:

```csharp
// 1. Interface Definition
public interface IMyResource : IResource
{
    // Resource-specific properties
    string MyProperty { get; }
}

// 2. Implementation with Factory Pattern
public class MyResource : IMyResource
{
    // Constructor with dependency injection
    public MyResource(int apiVersion, Stream? stream, ILogger<MyResource> logger)
    {
        // Implementation
    }
}

// 3. Factory Registration
public class MyResourceFactory : ResourceFactoryBase<IMyResource>
{
    protected override async Task<IMyResource> CreateResourceCoreAsync(
        int apiVersion, Stream? stream, CancellationToken cancellationToken)
    {
        return new MyResource(apiVersion, stream, _logger);
    }
}

// 4. Service Registration in ServiceCollectionExtensions.cs
services.AddResourceFactory<IMyResource, MyResourceFactory>();
```

### Mandatory Resource Wrapper Requirements

1. **Binary Format Compliance**: Must match legacy format exactly
2. **Factory Pattern**: All resources created via factories
3. **Dependency Injection**: Constructor injection required
4. **Golden Master Testing**: Byte-perfect compatibility tests
5. **Performance Parity**: Must meet/exceed legacy performance

---

## ⚡ **CRITICAL: Pre-Commit Validation Sequence**

> **MANDATORY**: Run this sequence before every commit

```powershell
# Navigate to project root
cd "c:\Users\nawgl\code\TS4Tools"

# 1. Code quality and formatting
.\scripts\check-quality.ps1 -Fix

# 2. Build verification (ZERO errors/warnings allowed)
dotnet build TS4Tools.sln --verbosity minimal

# 3. Test validation (100% pass rate required)  
dotnet test TS4Tools.sln --verbosity minimal

# 4. Golden Master validation (if working on resource wrappers)
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity minimal
```

**All steps MUST pass** before committing code.

---

## 🚨 **CRITICAL: SIMS4TOOLS Compatibility Context**

### The Assembly Loading Crisis

The original Sims4Tools uses `Assembly.LoadFile()` which **breaks completely**  
**in .NET 8+**. This is why we're doing a greenfield rewrite.

**Critical Rules:**

- Never use `Assembly.LoadFile()` - use `AssemblyLoadContext.LoadFromAssemblyPath()`
- All plugin loading must use modern patterns
- 100% API compatibility must be maintained for external tools

### API Compatibility Requirements

```csharp
// ✅ REQUIRED: Maintain exact method signatures
public IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry)
{
    // Modern implementation, identical signature
}

// ❌ FORBIDDEN: Breaking API changes
public async Task<IResource> GetResourceAsync(int apiVersion, IPackage package, IResourceIndexEntry entry)
{
    // This breaks compatibility even though it's "better"
}
```

### Business Logic Extraction Rules

- Extract domain knowledge, not code structures
- Modern .NET 9 implementation with identical external behavior
- Preserve plugin ecosystem (20+ resource wrappers must continue working)

---

## 📚 Architecture Overview

### Project Structure

```text
TS4Tools/
├── src/                          # Source code
│   ├── TS4Tools.Core.Interfaces/ # Contracts and abstractions
│   ├── TS4Tools.Core.System/     # Cross-platform system utilities
│   ├── TS4Tools.Core.Settings/   # Configuration management
│   ├── TS4Tools.Core.Package/    # Package reading/writing
│   ├── TS4Tools.Core.Resources/  # Resource management
│   └── ...
├── tests/                        # Unit and integration tests
├── benchmarks/                   # Performance benchmarks
├── docs/                         # Documentation
├── scripts/                      # Build and utility scripts
└── TS4Tools.Desktop/            # Avalonia UI application
```

### Key Architectural Principles

#### 1. Dependency Injection (DI)

We use Microsoft's built-in DI container for loose coupling and testability.

```csharp
// ✅ Good: Constructor injection
public class PackageReader : IPackageReader
{
    private readonly ILogger<PackageReader> _logger;
    private readonly IFileSystem _fileSystem;
    
    public PackageReader(ILogger<PackageReader> logger, IFileSystem fileSystem)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }
}

// ❌ Avoid: Static dependencies
public class PackageReader
{
    public void ReadPackage()
    {
        var logger = LogManager.GetLogger(); // Hard to test
        var fs = new FileSystem();           // Tightly coupled
    }
}
```

#### 2. Modern C# Patterns

We leverage C# 12 features for cleaner, more maintainable code.

```csharp
// ✅ Modern patterns
public record ResourceKey(uint Type, uint Group, ulong Instance);

public async Task<IResource?> LoadResourceAsync(ResourceKey key)
{
    return key switch
    {
        { Type: 0x12345678 } => await LoadTextureAsync(key),
        { Type: 0x87654321 } => await LoadMeshAsync(key),
        _ => null
    };
}

// Use nullable reference types
public IResource? FindResource(string? resourceName)
{
    if (string.IsNullOrEmpty(resourceName))
        return null;
        
    return _cache.TryGetValue(resourceName, out var resource) ? resource : null;
}
```

#### 3. Cross-Platform Design

All code should work on Windows, macOS, and Linux.

```csharp
// ✅ Cross-platform file operations
public class FileSystemService : IFileSystem
{
    public async Task<string[]> ReadAllLinesAsync(string path)
    {
        // Uses .NET cross-platform APIs
        return await File.ReadAllLinesAsync(path);
    }
}

// ❌ Avoid: Platform-specific APIs
public void ShowDialog()
{
    MessageBox.Show("Hello"); // Windows-only
}
```

---

## 🛠️ Development Workflow

### 1. Setting Up Your IDE

#### Visual Studio 2022

1. Install with ".NET desktop development" workload
2. Enable these extensions:
   - **SonarLint** for code quality
   - **GitHub Copilot** for AI assistance
   - **ReSharper** (optional, for advanced refactoring)

#### VS Code

1. Install these extensions:
   - **C#** (Microsoft)
   - **GitLens** for Git integration
   - **SonarLint** for code quality

### 2. Code Style and Standards

#### EditorConfig

We use `.editorconfig` for consistent formatting:

```ini
# Already configured in the repository
root = true

[*.cs]
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
```

#### Naming Conventions

```csharp
// Interfaces: PascalCase with 'I' prefix
public interface IResourceManager { }

// Classes: PascalCase
public class ResourceManager : IResourceManager { }

// Methods: PascalCase
public async Task LoadResourceAsync() { }

// Fields: camelCase with underscore prefix
private readonly ILogger _logger;

// Properties: PascalCase
public string ResourceType { get; set; }

// Constants: PascalCase
public const int MaxResourceSize = 1024;
```

### 3. Testing Standards

#### Unit Test Structure (AAA Pattern)

```csharp
[Test]
public async Task LoadResourceAsync_WithValidKey_ReturnsResource()
{
    // Arrange
    var key = new ResourceKey(0x12345678, 0x00000000, 0x123456789ABCDEF0);
    var mockFileSystem = Substitute.For<IFileSystem>();
    mockFileSystem.FileExistsAsync(Arg.Any<string>()).Returns(true);
    
    var resourceManager = new ResourceManager(mockFileSystem, _logger);
    
    // Act
    var result = await resourceManager.LoadResourceAsync(key);
    
    // Assert
    result.Should().NotBeNull();
    result.ResourceType.Should().Be(0x12345678);
}
```

#### Test Categories

- **Unit Tests**: Fast, isolated, no external dependencies
- **Integration Tests**: Test component interactions
- **Performance Tests**: Benchmark critical operations

### 4. Git Workflow

#### Branch Naming

```bash
# Feature branches
feature/resource-caching-improvement
feature/cross-platform-dialogs

# Bug fixes
bugfix/memory-leak-in-package-reader
bugfix/null-reference-in-settings

# Hotfixes
hotfix/critical-crash-on-startup
```

#### Commit Messages (Conventional Commits)

```bash
# Features
feat(resources): add immutable resource key implementation

# Bug fixes
fix(package): resolve memory leak in resource cache

# Documentation
docs(readme): update installation instructions

# Performance improvements
perf(loading): optimize package reading with async streams

# Refactoring
refactor(di): migrate static classes to dependency injection
```

---

## 🏗️ Common Development Tasks

### Adding a New Resource Type

#### Step 1: Create the Interface

```csharp
public interface ITextureResource : IResource
{
    int Width { get; }
    int Height { get; }
    TextureFormat Format { get; }
    byte[] PixelData { get; }
}
```

#### Step 2: Implement the Resource

```csharp
public class TextureResource : ResourceBase, ITextureResource
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public TextureFormat Format { get; private set; }
    public byte[] PixelData { get; private set; }
    
    // Constructor with dependency injection
    public TextureResource(int apiVersion, Stream stream, ILogger<TextureResource> logger)
        : base(apiVersion, stream)
    {
        _logger = logger;
        ParseTextureData();
    }
}
```

#### Step 3: Create the Factory

```csharp
public class TextureResourceFactory : ResourceFactoryBase<ITextureResource>
{
    public override IReadOnlySet<string> SupportedResourceTypes => 
        new HashSet<string> { "0x12345678" };
    
    protected override async Task<ITextureResource> CreateResourceCoreAsync(
        int apiVersion, 
        Stream? stream, 
        CancellationToken cancellationToken)
    {
        return new TextureResource(apiVersion, stream, _logger);
    }
}
```

#### Step 4: Register in DI Container

```csharp
services.AddTransient<IResourceFactory<ITextureResource>, TextureResourceFactory>();
```

#### Step 5: Add Unit Tests

```csharp
public class TextureResourceTests
{
    [Test]
    public void Constructor_WithValidStream_ParsesTextureData()
    {
        // Arrange
        var stream = CreateTestTextureStream();
        var logger = Substitute.For<ILogger<TextureResource>>();
        
        // Act
        var texture = new TextureResource(1, stream, logger);
        
        // Assert
        texture.Width.Should().Be(512);
        texture.Height.Should().Be(512);
        texture.Format.Should().Be(TextureFormat.DXT5);
    }
}
```

### Adding a New Feature

1. **Create Feature Branch**

```bash
git checkout -b feature/new-resource-export
```

1. **Follow TDD Approach**
   - Write failing tests first
   - Implement minimum code to pass tests
   - Refactor for quality

2. **Update Documentation**
   - Add XML comments to public APIs
   - Update architectural docs if needed
   - Add usage examples

3. **Performance Testing**

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ResourceExportBenchmarks
{
    [Benchmark]
    public async Task ExportResource_LargeTexture()
    {
        await _exporter.ExportAsync(_largeTexture, _outputPath);
    }
}
```

### Debugging Common Issues

#### Performance Problems

1. **Use BenchmarkDotNet for profiling**

```csharp
dotnet run --project benchmarks/TS4Tools.Benchmarks -c Release
```

1. **Memory profiling with dotMemory or PerfView**
2. **Check for async/await anti-patterns**

#### Cross-Platform Issues

1. **Test on all supported platforms**
2. **Use `Path.Combine()` instead of string concatenation**
3. **Be aware of case-sensitive file systems (Linux/macOS)**

---

## 📖 Learning Resources

### Required Reading

1. **[Migration Roadmap](MIGRATION_ROADMAP.md)** - Understand the project direction
2. **[ADR-001: .NET 9 Framework](docs/architecture/adr/ADR-001-DotNet9-Framework.md)**
3. **[ADR-002: Dependency Injection](docs/architecture/adr/ADR-002-Dependency-Injection.md)**
4. **[ADR-003: Avalonia UI](docs/architecture/adr/ADR-003-Avalonia-CrossPlatform-UI.md)**

### Recommended Learning

- **Dependency Injection**: [Microsoft DI Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- **Avalonia UI**: [Avalonia Documentation](https://docs.avaloniaui.net/)
- **Async/Await**: [Async Programming Patterns](https://docs.microsoft.com/en-us/dotnet/csharp/async)
- **Testing**: [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)

### Internal Documentation

- **[Technical Debt Registry](docs/technical-debt-registry.md)** - Known issues and improvements
- **[Performance Guidelines](docs/performance-guidelines.md)** - Optimization best practices
- **[Code Review Checklist](docs/code-review-checklist.md)** - What to look for in reviews

---

## 🛠️ **Essential Development Tools & Scripts**

### PowerShell Helper Scripts

```powershell
# Quality check and auto-fix (run before every commit)
.\scripts\check-quality.ps1 -Fix

# Create new resource wrapper boilerplate
.\scripts\New-ResourceWrapper.ps1 -ResourceName "MyResource" -ResourceType "0x12345678"

# Update project progress tracking
.\scripts\Update-Progress.ps1
```

### Development Shortcuts

```powershell
# Quick development cycle
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build TS4Tools.sln --verbosity minimal
dotnet test --filter "Category!=Integration" --verbosity minimal

# Full validation (before major commits)
dotnet clean
dotnet restore  
dotnet build TS4Tools.sln --verbosity minimal
dotnet test TS4Tools.sln --verbosity minimal
```

### BenchmarkDotNet Performance Testing

```powershell
# Run performance benchmarks
dotnet run --project benchmarks/TS4Tools.Benchmarks -c Release

# Run specific benchmark category
dotnet run --project benchmarks/TS4Tools.Benchmarks -c Release -- --filter "*ResourceParsing*"
```

---

## 🚨 **Critical Project Context**

### Why TS4Tools Exists

1. **Assembly Loading Crisis**: Original uses `Assembly.LoadFile()` which breaks in .NET 8+
2. **Technical Debt**: 114+ legacy projects with interdependencies
3. **Cross-Platform Need**: Community wants Linux/macOS support
4. **Modern Architecture**: Need for dependency injection, async/await, proper testing

### What Makes This Project Unique

- **Greenfield Rewrite**: Not migrating code, extracting business logic
- **100% API Compatibility**: External tools must work without changes
- **Golden Master Testing**: Byte-perfect validation with real game files
- **Plugin Ecosystem**: 20+ resource wrappers must continue working
- **Community Impact**: Used by thousands of Sims 4 modders worldwide

### Success Criteria

- **Performance**: Must match or exceed legacy system
- **Compatibility**: 100% backward compatibility for plugins/tools
- **Quality**: 95%+ test coverage, clean static analysis
- **Cross-Platform**: True Windows/macOS/Linux support

---

## 🎯 **Quick Reference Cheat Sheet**

### Daily Development Commands

```powershell
# Start of day
cd "c:\Users\nawgl\code\TS4Tools"
git pull
dotnet restore

# Before any work
dotnet test --filter "Category!=Integration" --verbosity minimal

# Before committing  
.\scripts\check-quality.ps1 -Fix
dotnet build TS4Tools.sln --verbosity minimal
dotnet test --verbosity minimal

# Golden Master validation (resource wrapper work)
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity minimal
```

### Emergency Troubleshooting

```powershell
# Clean build issues
dotnet clean
dotnet restore --force
dotnet build TS4Tools.sln

# Reset to known good state
git status
git stash  # if you have uncommitted changes
git pull

# Check current phase status
Get-Content "PHASE_*_COMPLETION_STATUS.md" | Select-String "STATUS"
```

### Key Files to Check Before Starting Work

1. **`MIGRATION_ROADMAP.md`** - Overall project status
2. **`PHASE_X_XX_CHECKLIST.md`** - Current phase requirements  
3. **`AI_ASSISTANT_GUIDELINES.md`** - Detailed development standards
4. **`CHANGELOG.md`** - Recent changes and completed work

---

## 🤝 Getting Help

### Communication Channels

- **Daily Standups**: Monday/Wednesday/Friday at 9:00 AM
- **Architecture Reviews**: Bi-weekly on Wednesdays
- **Code Reviews**: All PRs require 2 approvals

### Who to Ask

- **Architecture Questions**: Lead Developer
- **UI/UX Questions**: UI Team Lead  
- **Performance Issues**: Senior Developer
- **Testing Help**: QA Team Lead

### Common Questions

#### Q: Why did we choose Avalonia over MAUI?

A: See [ADR-003](docs/architecture/adr/ADR-003-Avalonia-CrossPlatform-UI.md).  
MAUI has limited Linux support, and desktop scenarios aren't as mature.

#### Q: How do I add a new configuration setting?

A: Add it to the appropriate `Options` class and register with DI:

```csharp
public class ResourceManagerOptions
{
    public bool EnableCaching { get; set; } = true;
    public int CacheSize { get; set; } = 1000;
}

// In Startup.cs
services.Configure<ResourceManagerOptions>(configuration.GetSection("ResourceManager"));
```

#### Q: When should I use async/await?

A: For any I/O operations (file access, network, database). CPU-bound  
operations generally don't need async unless you're doing long-running work.

#### Q: How do I handle cross-platform differences?

A: Use dependency injection with platform-specific implementations:

```csharp
#if WINDOWS
services.AddScoped<IDialogService, WindowsDialogService>();
#elif MACOS  
services.AddScoped<IDialogService, MacDialogService>();
#endif
```

---

## 🎯 Your First Contribution

### Week 1: Environment Setup

- [ ] Set up development environment
- [ ] Build and run all tests successfully
- [ ] Read core architecture documents
- [ ] Complete this onboarding guide

### Week 2: Small Contribution

- [ ] Pick a "good first issue" from GitHub Issues
- [ ] Create feature branch and implement solution
- [ ] Add unit tests (aim for 95%+ coverage)
- [ ] Submit PR with proper commit messages

### Week 3: Code Review Participation

- [ ] Review 2-3 PRs from other developers
- [ ] Participate in architecture discussion
- [ ] Suggest improvements or ask questions

### Month 1 Goal

- [ ] Successfully merge first feature PR
- [ ] Understand core architecture patterns
- [ ] Comfortable with development workflow
- [ ] Contributing to team discussions

---

## 📋 Checklists

### Before Committing Code

- [ ] All tests pass locally (`dotnet test`)
- [ ] Code follows style guidelines (EditorConfig)
- [ ] XML documentation added for public APIs
- [ ] Performance impact considered and tested
- [ ] Cross-platform compatibility verified

### Before Submitting PR

- [ ] Feature branch up-to-date with main
- [ ] Conventional commit messages used
- [ ] PR description explains changes and rationale
- [ ] Unit tests added/updated (95%+ coverage)
- [ ] Integration tests pass
- [ ] Documentation updated if needed

### Code Review Checklist

- [ ] Does the code follow SOLID principles?
- [ ] Are dependencies properly injected?
- [ ] Is error handling appropriate?
- [ ] Are there any potential memory leaks?
- [ ] Is the code cross-platform compatible?
- [ ] Are unit tests comprehensive?
- [ ] **Do Golden Master tests pass?** (Critical for resource wrappers)
- [ ] **Does it maintain API compatibility?** (Check method signatures)
- [ ] **Are assembly loading patterns modern?** (No `Assembly.LoadFile()`)

---

## 🚨 **Common Pitfalls & Troubleshooting**

### Golden Master Test Failures

```powershell
# If Golden Master tests fail:
# 1. Check if Sims 4 is installed and path is correct
# 2. Verify test data integrity
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity diagnostic

# 3. Regenerate test data if needed (ask team lead first)
# Note: This should be rare - most failures indicate implementation issues
```

### Resource Wrapper Common Mistakes

```csharp
// ❌ DON'T: Break API compatibility
public async Task<byte[]> GetDataAsync() // This breaks legacy compatibility

// ✅ DO: Maintain synchronous APIs with async implementation
public byte[] GetData() 
{
    return GetDataAsync().GetAwaiter().GetResult();
}

private async Task<byte[]> GetDataAsync() // Internal async implementation
{
    // Modern async implementation
}
```

### Performance Issues

```csharp
// ❌ DON'T: Block async operations
var result = SomeAsyncMethod().Result; // Can cause deadlocks

// ✅ DO: Use proper async patterns
var result = await SomeAsyncMethod().ConfigureAwait(false);

// ✅ DO: Use streaming for large files  
using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
// Process in chunks, don't load entire file
```

### Cross-Platform Issues

```csharp
// ❌ DON'T: Use Windows-specific paths
var path = "C:\\temp\\file.txt";

// ✅ DO: Use cross-platform path construction
var path = Path.Combine(Path.GetTempPath(), "file.txt");

// ❌ DON'T: Assume case-insensitive file systems
File.Exists("File.TXT"); // May fail on Linux

// ✅ DO: Use exact case or normalize
File.Exists(Path.GetFullPath("file.txt").ToLowerInvariant());
```

### Dependency Injection Problems

```csharp
// ❌ DON'T: Static dependencies (untestable)
public class MyService
{
    public void DoWork()
    {
        var logger = LogManager.GetCurrentClassLogger(); // Hard to test
    }
}

// ✅ DO: Constructor injection (testable)
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

---

**Welcome to the team!** 🎉

**Essential Success Tips:**

- **Read First**: Always check `MIGRATION_ROADMAP.md` and current `PHASE_X_XX_CHECKLIST.md` before starting
- **Test Early**: Run Golden Master tests before and after every change
- **Ask Questions**: Complex legacy system - better to ask than break compatibility
- **Follow Phases**: Work within the established phase structure
- **Validate Always**: Use `.\scripts\check-quality.ps1 -Fix` before every commit

We value clean code, comprehensive testing, and collaborative development. This project serves
thousands of Sims 4 modders worldwide - quality and compatibility are paramount.

---

**Last Updated**: August 10, 2025 *(Updated with critical Golden Master & phase requirements)*  
**Next Review**: Monthly updates based on team feedback
