# TS4Tools - Modern Sims 4 Package Editor

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Avalonia UI](https://img.shields.io/badge/Avalonia%20UI-11.3+-purple.svg)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/nawglan/TS4Tools)
[![License](https://img.shields.io/badge/license-GPL%203.0-green.svg)](LICENSE)

> **Latest Update:** Phase 4.20.7 Golden Master Testing completed (August 19, 2025)  
> **Status:** Core WrapperDealer Compatibility Layer implemented - Ready for Resource Type Implementations

## **Project Overview**

TS4Tools is a comprehensive migration and modernization of the popular Sims4Tools package editor, 
originally created by [s4ptacle](https://github.com/s4ptacle). This project transforms the legacy 
.NET Framework 4.8.1 WinForms application into a modern, cross-platform application using .NET 9 
and Avalonia UI.

> **Attribution:** This project is a modernization fork of the original 
> [Sims4Tools](https://github.com/s4ptacle/Sims4Tools) project. All credit for the original 
> design, concepts, and foundational code goes to s4ptacle and the original contributors.

### **Key Improvements**

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Modern UI**: Avalonia-based interface with dark/light themes  
- **Performance**: Async operations and memory optimizations
- **Architecture**: Clean MVVM with dependency injection
- **Testing**: Comprehensive unit and integration test coverage
- **Compatibility**: 100% API compatibility with existing community tools
- **Maintainability**: Modern C# patterns and nullable reference types

## **Current Status - Phase 4.20 WrapperDealer Compatibility**

**✅ MAJOR MILESTONES COMPLETED:**

### Phase 4.20.1 - Core WrapperDealer API ✅

- **WrapperDealer Static Class**: Complete legacy API implementation
- **TypeMap Collection**: Thread-safe resource type mapping
- **Resource Management**: Modern bridge to IResourceManager  
- **Test Coverage**: 1,393 total tests | 1,385 succeeded | 8 skipped | 0 failed

### Phase 4.20.2 - Plugin System Foundation ✅  

- **AssemblyLoadContextManager**: Modern assembly loading with plugin isolation
- **PluginDiscoveryService**: Automatic plugin discovery from standard directories
- **AResourceHandlerBridge**: Legacy AResourceHandler.Add() pattern compatibility
- **Cross-Platform Support**: Works on Windows, Linux, macOS

### Phase 4.20.3 - Security Audit & Memory Management ✅

- **Security Audit**: Comprehensive security review (all checks passed)
- **Memory Management**: Enhanced IDisposable patterns
- **DataResource Tests**: 53/53 passed with improved resource cleanup

### Phase 4.20.7 - Golden Master Testing ✅

- **Byte-Perfect Compatibility**: SHA256-based golden master validation
- **Community Plugin Support**: ModTheSims, S4PE, script mod loader compatibility verified
- **Interface Excellence**: Complete compliance with TS4Tools.Core.* interfaces

**🎯 NEXT PHASE:** Resource type implementations (18 missing types across 6 phases)

### **Migration Progress Overview**

| Phase | Status | Duration | Key Achievement |
|-------|--------|----------|-----------------|
| **Phases 1-3** | ✅ Complete | 4 days | Core foundation (24.5x acceleration) |
| **Phase 4.20** | ✅ Complete | 3 weeks | WrapperDealer compatibility layer |
| **Phases 4.21+** | 📋 Planned | 8-12 weeks | Resource implementations + s4pe migration |

**Current Progress**: ~35% complete (4 of 8 major phases)  
**Target Completion**: October-December 2025 ## **Architecture Overview**

### **Core Libraries**

```text
TS4Tools.Core.System/              # System utilities and extensions
TS4Tools.Core.Interfaces/          # Core interfaces and contracts  
TS4Tools.Core.Settings/            # Configuration management
TS4Tools.Core.Package/             # Package I/O operations
TS4Tools.Core.Resources/           # Resource management and factories
TS4Tools.Core.DependencyInjection/ # Dependency injection services
TS4Tools.Core.Helpers/             # Common helper utilities
TS4Tools.Core.Plugins/             # Plugin system infrastructure
TS4Tools.Extensions/               # UI extensions and helpers
TS4Tools.Resources.Common/         # Shared resource utilities
TS4Tools.WrapperDealer/           # Legacy compatibility layer
```

### **Resource Type Libraries (Implemented)**

```text
TS4Tools.Resources.Animation/      # Animation resources
TS4Tools.Resources.Audio/          # Audio resources  
TS4Tools.Resources.Catalog/        # Catalog resources
TS4Tools.Resources.Characters/     # Character resources
TS4Tools.Resources.Effects/        # Effects resources
TS4Tools.Resources.Geometry/       # Geometry resources
TS4Tools.Resources.Images/         # Image resources
TS4Tools.Resources.Scripts/        # Script resources
TS4Tools.Resources.Specialized/    # Specialized resource types
TS4Tools.Resources.Strings/        # String table resources
TS4Tools.Resources.Text/           # Text resources
TS4Tools.Resources.Textures/       # Texture resources
TS4Tools.Resources.Utility/        # Utility resources
TS4Tools.Resources.Visual/         # Visual resources
TS4Tools.Resources.World/          # World resources
```

### **Technology Stack**

- **Framework:** .NET 9
- **UI:** Avalonia UI 11.3+
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection
- **Testing:** xUnit, FluentAssertions, NSubstitute
- **Performance:** BenchmarkDotNet
- **Build:** Modern SDK-style projects
- **Build:** Modern SDK-style projects ## **Getting Started**

### **Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### **Development Setup**

```bash
# Clone the repository
git clone https://github.com/nawglan/TS4Tools.git
cd TS4Tools

# Restore dependencies
dotnet restore TS4Tools.sln

# Build the solution  
dotnet build TS4Tools.sln

# Run tests
dotnet test TS4Tools.sln

# Run the desktop application (when available)
dotnet run --project TS4Tools.Desktop/TS4Tools.Desktop.csproj
```

### **Project Structure**

```text
TS4Tools/
├── src/                          # Source code
│   ├── TS4Tools.Core.*/         # Core libraries (System, Interfaces, Package, etc.)
│   ├── TS4Tools.Resources.*/    # Resource type libraries (16+ implementations)
│   ├── TS4Tools.WrapperDealer/  # Legacy compatibility layer
│   ├── TS4Tools.Extensions/     # UI extensions
│   └── TS4Tools.Desktop/        # Desktop application
├── tests/                       # Test projects
│   ├── TS4Tools.Core.*.Tests/   # Core library tests
│   ├── TS4Tools.Resources.*.Tests/ # Resource library tests
│   ├── TS4Tools.Tests.Common/   # Shared test utilities
│   └── TS4Tools.Tests.GoldenMaster/ # Golden master tests
├── benchmarks/                  # Performance benchmarks
├── examples/                    # Usage examples
├── docs/                        # Documentation
├── scripts/                     # Build and utility scripts
└── README.md                    # This file
```

## **Testing Strategy**

### **Test Coverage Goals**

- **Unit Tests:** 92%+ coverage
- **Integration Tests:** 80%+ coverage  
- **Performance Tests:** 50+ benchmarks
- **Cross-Platform:** Windows, macOS, Linux validation

### **Running Tests**

```bash
# Run all tests
dotnet test TS4Tools.sln

# Run with coverage
dotnet test TS4Tools.sln --collect:"XPlat Code Coverage"

# Run performance benchmarks
dotnet run --project benchmarks/TS4Tools.Benchmarks --configuration Release

# Generate coverage report  
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
``` ## **Development Workflow**

### **Branch Strategy**

- `main` - Stable, production-ready code
- `develop` - Integration branch for completed features  
- `feature/*` - Feature development branches
- `hotfix/*` - Critical bug fixes

### **Commit Conventions**

```text
feat(core): add package reading functionality
fix(ui): resolve tree view selection issue
test(package): add integration tests for package I/O
docs(readme): update installation instructions
```

### **Quality Assurance**

- **Pre-commit hooks**: Automatic formatting and validation
- **CI/CD Pipeline**: Multiple quality gates across platforms
- **Code analyzers**: Microsoft.CodeAnalysis.NetAnalyzers and SonarAnalyzer.CSharp
- **Security scanning**: Integrated vulnerability assessment

## **Contributing**

This project is currently in active development by the core team. Once the foundation is stable,
we'll welcome community contributions.

### **Development Guidelines**

1. Follow the established architecture patterns
2. Maintain comprehensive test coverage  
3. Use modern C# patterns and nullable reference types
4. Ensure cross-platform compatibility
5. Update documentation and tracking files

## **Documentation**

- [Developer Onboarding Guide](docs/development/guidelines/developer-onboarding-guide.md) - Essential reading for new developers
- [Architecture Decision Records (ADRs)](docs/architecture/adr/README.md) - Technical decision records
- [Migration Roadmap](docs/migration/migration-roadmap.md) - Complete migration strategy
- [Phase 4.20 Checklist](docs/project-management/checklists/phase-4-20-checklist.md) - Current development status

## **Related Projects**

- [Original Sims4Tools](https://github.com/s4ptacle/Sims4Tools) - Original .NET Framework version by s4ptacle
- [s4pe Package Editor](https://github.com/s4ptacle/Sims4Tools/tree/develop/s4pe) - Original package editor component  
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) - Cross-platform UI framework

## **License**

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## **Acknowledgments**

- **[s4ptacle](https://github.com/s4ptacle)** - Original author and creator of
  [Sims4Tools](https://github.com/s4ptacle/Sims4Tools), without whom this modernization project
  would not exist
- **Original Sims4Tools Contributors** - Peter L Jones, Keyi Zhang, and all other contributors to the original project
- **The Sims 4 Modding Community** - For continuous support and feedback
- **Avalonia UI Team** - For the excellent cross-platform UI framework

---

**Status:** Active Development | **Last Updated:** August 19, 2025 