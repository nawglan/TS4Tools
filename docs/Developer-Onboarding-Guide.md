# TS4Tools Developer Onboarding Guide

**Welcome to the TS4Tools Development Team!**

This guide will help you get up and running with the TS4Tools codebase, understand our architecture decisions, and contribute effectively to the project.

---

## 🚀 Quick Start (15 minutes)

### Prerequisites
- **Windows 10/11** (primary development platform)
- **Visual Studio 2022** (v17.9+) or **VS Code** with C# extension
- **.NET 9 SDK** (latest version)
- **Git** for version control
- **PowerShell 5.1+** (included with Windows)

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

## 📚 Architecture Overview

### Project Structure
```
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

1. **Create the Interface**
```csharp
public interface ITextureResource : IResource
{
    int Width { get; }
    int Height { get; }
    TextureFormat Format { get; }
    byte[] PixelData { get; }
}
```

2. **Implement the Resource**
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

3. **Create the Factory**
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

4. **Register in DI Container**
```csharp
services.AddTransient<IResourceFactory<ITextureResource>, TextureResourceFactory>();
```

5. **Add Unit Tests**
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

2. **Follow TDD Approach**
   - Write failing tests first
   - Implement minimum code to pass tests
   - Refactor for quality

3. **Update Documentation**
   - Add XML comments to public APIs
   - Update architectural docs if needed
   - Add usage examples

4. **Performance Testing**
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

2. **Memory profiling with dotMemory or PerfView**
3. **Check for async/await anti-patterns**

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
A: See [ADR-003](docs/architecture/adr/ADR-003-Avalonia-CrossPlatform-UI.md). MAUI has limited Linux support, and desktop scenarios aren't as mature.

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
A: For any I/O operations (file access, network, database). CPU-bound operations generally don't need async unless you're doing long-running work.

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

---

**Welcome to the team!** 🎉

Remember: We value clean code, comprehensive testing, and collaborative development. Don't hesitate to ask questions – we're all here to help you succeed and build amazing tools for the Sims 4 community.

---

**Last Updated**: August 3, 2025  
**Next Review**: Monthly updates based on team feedback
