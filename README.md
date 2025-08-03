# TS4Tools - Modern Sims 4 Package Editor

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Avalonia UI](https://img.shields.io/badge/Avalonia%20UI-11.3+-purple.svg)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/nawglan/TS4Tools)
[![License](https://img.shields.io/badge/license-GPL%203.0-green.svg)](LICENSE)

> **🚧 Project Status: In Development - Migration Phase**  
> This is the next-generation, cross-platform version of Sims4Tools, built with modern .NET 9 and Avalonia UI.

## 🎯 **Project Overview**

TS4Tools is a comprehensive migration and modernization of the popular Sims4Tools package editor. The project transforms the legacy .NET Framework 4.8.1 WinForms application into a modern, cross-platform application using .NET 9 and Avalonia UI.

### **Key Improvements**
- ✅ **Cross-Platform**: Runs on Windows, macOS, and Linux
- ✅ **Modern UI**: Avalonia-based interface with dark/light themes
- ✅ **Performance**: Async operations and memory optimizations
- ✅ **Architecture**: Clean MVVM with dependency injection
- ✅ **Testing**: Comprehensive unit and integration test coverage
- ✅ **Maintainability**: Modern C# patterns and nullable reference types

## 📋 **Migration Status**

**Current Phase:** Planning and Architecture Design  
**Overall Progress:** 0% (28-week migration plan)

| Phase | Status | Progress | Target |
|-------|--------|----------|---------|
| Core Foundation | ⏳ Not Started | 0% | Week 8 |
| Extensions & Commons | ⏳ Pending | 0% | Week 12 |
| Architecture Integration | ⏳ Pending | 0% | Week 14 |
| Basic GUI | ⏳ Pending | 0% | Week 18 |
| Resource Wrappers | ⏳ Pending | 0% | Week 24 |
| Advanced Features | ⏳ Pending | 0% | Week 28 |

For detailed progress tracking, see:
- 📋 [Migration Roadmap](MIGRATION_ROADMAP.md) - Comprehensive migration plan
- 📊 [Task Tracker](TASK_TRACKER.md) - Day-to-day progress tracking

## 🏗️ **Architecture Overview**

### **Core Libraries**
```
TS4Tools.Core.System/           # System utilities and extensions
TS4Tools.Core.Interfaces/       # Core interfaces and contracts
TS4Tools.Core.Settings/         # Configuration management
TS4Tools.Core.Package/          # Package I/O operations
TS4Tools.Core.Resources/        # Resource management and factories
TS4Tools.Extensions/            # UI extensions and helpers
TS4Tools.Resources.Common/      # Shared resource utilities
```

### **Technology Stack**
- **Framework:** .NET 9
- **UI:** Avalonia UI 11.3+
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection
- **Testing:** xUnit, FluentAssertions, NSubstitute
- **Performance:** BenchmarkDotNet
- **Build:** Modern SDK-style projects

## 🚀 **Getting Started**

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
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application (when available)
dotnet run --project TS4Tools.Desktop
```

### **Project Structure**
```
TS4Tools/
├── src/                        # Source code
│   ├── TS4Tools.Core.*/       # Core libraries
│   ├── TS4Tools.Extensions/   # UI extensions
│   ├── TS4Tools/              # Main application library
│   └── TS4Tools.Desktop/      # Desktop application
├── tests/                      # Test projects
│   ├── TS4Tools.Tests.Unit/   # Unit tests
│   ├── TS4Tools.Tests.Integration/ # Integration tests
│   └── TS4Tools.Tests.Performance/ # Performance tests
├── docs/                       # Documentation
├── scripts/                    # Build and utility scripts
├── MIGRATION_ROADMAP.md       # Comprehensive migration plan
├── TASK_TRACKER.md           # Daily progress tracking
└── README.md                  # This file
```

## 🧪 **Testing Strategy**

### **Test Coverage Goals**
- **Unit Tests:** 92%+ coverage
- **Integration Tests:** 80%+ coverage  
- **Performance Tests:** 50+ benchmarks
- **Cross-Platform:** Windows, macOS, Linux validation

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run performance benchmarks
dotnet run --project TS4Tools.Tests.Performance --configuration Release

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

## 📈 **Development Workflow**

### **Task Management**
Use the provided PowerShell script to update task progress:

```powershell
# Mark a task as completed
./scripts/Update-Progress.ps1 -TaskId "1.1.AHandlerDictionary" -Status "Completed" -Notes "Migrated with full test coverage"

# Mark a task as in progress  
./scripts/Update-Progress.ps1 -TaskId "1.2.IPackage" -Status "InProgress"

# Mark a task as blocked
./scripts/Update-Progress.ps1 -TaskId "1.3.Settings" -Status "Blocked" -Notes "Waiting for design decision"
```

### **Branch Strategy**
- `main` - Stable, production-ready code
- `develop` - Integration branch for completed features
- `feature/*` - Feature development branches
- `hotfix/*` - Critical bug fixes

### **Commit Conventions**
```
feat(core): add package reading functionality
fix(ui): resolve tree view selection issue  
test(package): add integration tests for package I/O
docs(readme): update installation instructions
```

## 🤝 **Contributing**

This project is currently in active development by the core team. Once the foundation is stable, we'll welcome community contributions.

### **Development Guidelines**
1. Follow the established architecture patterns
2. Maintain comprehensive test coverage
3. Use modern C# patterns and nullable reference types
4. Ensure cross-platform compatibility
5. Update documentation and tracking files

## 📚 **Documentation**

- [Migration Roadmap](MIGRATION_ROADMAP.md) - Complete migration strategy
- [Task Tracker](TASK_TRACKER.md) - Current progress and sprint planning
- [Architecture Decisions](docs/adr/) - Technical decision records (TBD)
- [API Documentation](docs/api/) - Generated API documentation (TBD)

## 🔗 **Related Projects**

- [Original Sims4Tools](https://github.com/s4ptacle/Sims4Tools) - Legacy .NET Framework version
- [s4pe](https://github.com/s4ptacle/Sims4Tools/tree/develop/s4pe) - Original package editor
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) - Cross-platform UI framework

## 📜 **License**

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## 🙏 **Acknowledgments**

- **Original Sims4Tools Team** - Peter L Jones, Keyi Zhang, and all contributors
- **The Sims 4 Modding Community** - For continuous support and feedback
- **Avalonia UI Team** - For the excellent cross-platform UI framework

---

**🔄 Status:** Active Development | **📅 Last Updated:** August 3, 2025
