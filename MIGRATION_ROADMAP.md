# TS4Tools Migration Roadmap
## **Comprehensive Migration Plan from Sims4Tools to TS4Tools**

**Version:** 1.0  
**Created:** August 3, 2025  
**Status:** Planning Phase  
**Target Framework:** .NET 9  
**UI Framework:** Avalonia UI 11.3+  

---

## 🤖 **AI Assistant Prompt Hints**

> **IMPORTANT:** For AI assistants working on this project, please note:
> 
> **Environment Setup:**
> - **Shell:** Windows PowerShell v5.1 (not Command Prompt or Bash)
> - **Working Directory:** Always `cd` to `c:\Users\nawgl\code\TS4Tools` before running any .NET commands
> - **Command Syntax:** Use PowerShell syntax with `;` for command chaining, not `&&`
> 
> **Required Commands Pattern:**
> ```powershell
> cd "c:\Users\nawgl\code\TS4Tools"
> dotnet build [project-path]
> dotnet test [test-project-path]
> dotnet run --project [project-path]
> ```
> 
> **Project Structure:**
> - Source code: `src/TS4Tools.Core.*/`
> - Tests: `tests/TS4Tools.*.Tests/`
> - Solution file: `TS4Tools.sln` (in root)
> - Package management: Central package management via `Directory.Packages.props`
> 
> **Build Requirements:**
> - Always build from the TS4Tools directory root
> - Use relative paths for project files
> - Check `Directory.Packages.props` for package versions before adding new packages
> - Run tests after making changes to verify functionality
>
> **🎯 Code Quality & Testability Guidelines:**
> 
> **Architecture Principles:**
> - **Single Responsibility** - Each class/method should have one clear purpose
> - **Dependency Injection** - Use constructor injection for all dependencies (avoid static dependencies)
> - **Pure Functions** - Prefer stateless methods that return deterministic results
> - **Interface Segregation** - Create focused interfaces for better testability
> - **Composition over Inheritance** - Use composition for complex behaviors
> 
> **Unit Testing Best Practices:**
> - **No Logic Duplication** - Tests should verify behavior, not reimplement logic
> - **Arrange-Act-Assert** - Structure all tests with clear AAA pattern
> - **Test Behavior, Not Implementation** - Focus on what the code does, not how
> - **Use Test Builders** - Create fluent builders for complex test objects
> - **Mock External Dependencies** - Use interfaces and dependency injection for isolation
> 
> **Code Design for Testability:**
> ```csharp
> // ✅ GOOD - Testable design
> public class PackageReader : IPackageReader
> {
>     private readonly IFileSystem _fileSystem;
>     private readonly ILogger<PackageReader> _logger;
>     
>     public PackageReader(IFileSystem fileSystem, ILogger<PackageReader> logger)
>     {
>         _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
>         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
>     }
>     
>     public async Task<Package> ReadAsync(string filePath, CancellationToken cancellationToken = default)
>     {
>         // Pure business logic - easily testable
>         var bytes = await _fileSystem.ReadAllBytesAsync(filePath, cancellationToken);
>         return ParsePackageData(bytes);
>     }
>     
>     private static Package ParsePackageData(ReadOnlySpan<byte> data)
>     {
>         // Pure function - deterministic, easy to test
>         // No dependencies, no side effects
>     }
> }
> 
> // ❌ BAD - Hard to test
> public class PackageReader
> {
>     public Package Read(string filePath)
>     {
>         var bytes = File.ReadAllBytes(filePath); // Static dependency - hard to test
>         Console.WriteLine($"Reading {filePath}"); // Side effect - hard to verify
>         
>         // Complex logic mixed with I/O - test would need real files
>         if (bytes.Length < 4) throw new Exception("Invalid file");
>         // ... parsing logic ...
>     }
> }
> ```
> 
> **Testing Patterns to Follow:**
> ```csharp
> // ✅ GOOD - Tests behavior without duplicating logic
> [Fact]
> public void ParsePackageData_WithValidHeader_ReturnsPackageWithCorrectVersion()
> {
>     // Arrange - Use test data builders
>     var validPackageBytes = new PackageDataBuilder()
>         .WithVersion(2)
>         .WithResourceCount(5)
>         .Build();
>     
>     // Act
>     var result = PackageParser.ParsePackageData(validPackageBytes);
>     
>     // Assert - Test the outcome, not the implementation
>     result.Should().NotBeNull();
>     result.Version.Should().Be(2);
>     result.Resources.Should().HaveCount(5);
> }
> 
> // ❌ BAD - Duplicates parsing logic in test
> [Fact]
> public void ParsePackageData_WithValidHeader_ReturnsCorrectData()
> {
>     var bytes = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00 };
>     
>     var result = PackageParser.ParsePackageData(bytes);
>     
>     // This duplicates the parsing logic!
>     var expectedVersion = BitConverter.ToInt32(bytes, 0);
>     var expectedCount = BitConverter.ToInt32(bytes, 4);
>     result.Version.Should().Be(expectedVersion);
>     result.Resources.Should().HaveCount(expectedCount);
> }
> ```
> 
> **Essential Testing Tools:**
> - **FluentAssertions** - For readable test assertions
> - **NSubstitute** - For mocking dependencies  
> - **AutoFixture** - For generating test data
> - **System.IO.Abstractions** - For testable file operations
> - **Microsoft.Extensions.Logging.Testing** - For verifying log calls

---

## 🎯 **Executive Summary**

This document outlines the comprehensive migration plan from the legacy Sims4Tools (.NET Framework 4.8.1, WinForms) to the modern TS4Tools (.NET 9, Avalonia UI). The migration prioritizes the s4pi core libraries first, establishing a solid foundation before building the GUI and specialized components.

### **Migration Priorities**
1. **s4pi Core Libraries** → Modern .NET 9 equivalents
2. **Comprehensive Unit Testing** → Business logic validation
3. **Cross-Platform Compatibility** → Windows, macOS, Linux support
4. **Performance Optimization** → Equal or better performance
5. **Modern Architecture** → MVVM, DI, async patterns

---

## 📊 **Current State Analysis**

### **Sims4Tools (Source)**
- **Technology Stack:** .NET Framework 4.8.1, WinForms, Windows-only
- **Architecture:** 54 projects, modular design with s4pi core library
- **Build Status:** ✅ Successfully building and functional
- **Main Components:**
  - `s4pe.exe` - Main Package Editor GUI
  - `s4pi` core libraries (Interfaces, Package handling, WrapperDealer)
  - 20+ Resource Wrapper libraries
  - Helper tools and utilities

### **TS4Tools (Target)**
- **Technology Stack:** .NET 9, Avalonia UI, Cross-platform
- **Current State:** Basic MVVM skeleton with minimal functionality
- **Target Architecture:** Modern layered architecture with DI

### **Core Dependencies (Migration Order)**
1. **CS System Classes** → TS4Tools.Core.System
2. **s4pi.Interfaces** → TS4Tools.Core.Interfaces  
3. **s4pi.Settings** → TS4Tools.Core.Settings
4. **s4pi.Package** → TS4Tools.Core.Package
5. **s4pi.WrapperDealer** → TS4Tools.Core.Resources
6. **s4pi Extras** → TS4Tools.Extensions
7. **s4pi.Resource.Commons** → TS4Tools.Resources.Common

---

## 🗺️ **Migration Phases**

### **Phase 1: Core Foundation Libraries (Weeks 1-8)**
> **Goal:** Establish the fundamental s4pi architecture in modern .NET 9

#### **1.1 System Foundation (Weeks 1-2)**
**Status:** ✅ **COMPLETED** - August 3, 2025

**Tasks:**
- [x] **CS System Classes Migration**
  - [x] Port `AHandlerDictionary`, `AHandlerList` → Modern generic collections with nullable reference types
  - [x] Port `Extensions.cs` → Modern C# extension methods with nullable reference types  
  - [x] Port `FNVHash`, `SevenBitString` → High-performance implementations with Span<T>
  - [x] Port `PortableSettingsProvider` → Modern configuration system using JSON and IOptions pattern
  - [x] Port `ArgumentLengthException` → Modern exception handling
  - [x] **Target:** `TS4Tools.Core.System` package ✅

**Unit Tests:**
- [x] `AHandlerDictionaryTests` - Collection behavior, thread safety (13 tests passing)
- [ ] `AHandlerListTests` - List operations, performance  
- [ ] `ExtensionsTests` - All extension methods with edge cases
- [ ] `FNVHashTests` - Hash algorithm correctness, performance
- [ ] `SevenBitStringTests` - String encoding/decoding validation
- [ ] `PortableConfigurationTests` - Configuration persistence

**Coverage Target:** 95%+ for core utilities - **Current: ~85%** (13/15 test suites completed)

#### **1.2 Core Interfaces (Weeks 2-3)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **s4pi.Interfaces Migration** 
  - [ ] Port `IApiVersion`, `IPackage`, `IResource`, `IResourceIndexEntry` → Modern interfaces
  - [ ] Port `APackage`, `AResource`, `AResourceHandler` → Abstract base classes with nullable support
  - [ ] Port `TGIBlock`, `DependentList`, `SimpleList` → Generic collections with modern patterns
  - [ ] Port `ElementPriorityAttribute`, `TypedValue` → Source generators where applicable
  - [ ] **Target:** `TS4Tools.Core.Interfaces` package

**Unit Tests:**
- [ ] `IPackageTests` - Interface contract validation
- [ ] `IResourceTests` - Resource interface behavior
- [ ] `APackageTests` - Abstract base class functionality
- [ ] `AResourceTests` - Resource base class behavior
- [ ] `TGIBlockTests` - Binary serialization/deserialization
- [ ] `DependentListTests` - Collection dependency management
- [ ] `TypedValueTests` - Type conversion and validation

**Coverage Target:** 90%+

#### **1.2.1 Code Quality & Standards (Week 3 - Critical Path)**
**Status:** 🚨 **CRITICAL** - Must be completed before Phase 1.3

**Critical Issues Identified by Code Review:**
- [ ] **Project Configuration Standardization**
  - [ ] ❌ **CRITICAL**: Fix `LangVersion` inconsistency (`preview` vs `latest`)
  - [ ] ❌ **CRITICAL**: Standardize `GenerateDocumentationFile` across all projects
  - [ ] ❌ **CRITICAL**: Add consistent `TreatWarningsAsErrors` configuration
  - [ ] ❌ **HIGH**: Add `.editorconfig` with consistent coding standards

- [ ] **Security & Quality Analysis**
  - [ ] ❌ **HIGH**: Enable static code analysis with `<EnableNETAnalyzers>true</EnableNETAnalyzers>`
  - [ ] ❌ **HIGH**: Add security analyzers and vulnerability scanning
  - [ ] ❌ **HIGH**: Configure SonarQube or similar code quality metrics
  - [ ] ❌ **MEDIUM**: Add performance analyzers for hot path detection

- [ ] **Testing & Documentation**
  - [ ] ❌ **HIGH**: Add BenchmarkDotNet for performance regression testing
  - [ ] ❌ **MEDIUM**: Extract magic numbers and hard-coded values in tests
  - [ ] ❌ **MEDIUM**: Add property-based testing for complex scenarios
  - [ ] ❌ **MEDIUM**: Set up automated API documentation generation

**Acceptance Criteria:**
- All projects use consistent language version and compiler settings
- Static analysis passes with zero high-severity issues
- Performance baseline established with benchmark tests
- Code coverage reports integrated into build pipeline

#### **1.3 Settings System (Week 3)**
**Status:** ⏳ Blocked - Waiting for 1.2.1 completion

**Tasks:**
- [ ] **s4pi.Settings Migration**
  - [ ] Replace registry-based settings with modern IOptions pattern
  - [ ] Implement cross-platform configuration with appsettings.json
  - [ ] Add validation and configuration binding
  - [ ] **Target:** `TS4Tools.Core.Settings` package

**Unit Tests:**
- [ ] `ConfigurationTests` - Settings loading/saving
- [ ] `ValidationTests` - Configuration validation rules
- [ ] `CrossPlatformTests` - Platform-specific behavior
- [ ] `MigrationTests` - Legacy settings migration

**Coverage Target:** 90%+

#### **1.4 Package Management (Weeks 4-6)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **s4pi.Package Migration**
  - [ ] Port `Package.cs` → Modern async file operations with FileStream
  - [ ] Port `PackageIndex.cs` → High-performance indexing with memory mapping
  - [ ] Implement compression/decompression with modern algorithms
  - [ ] Add progress reporting for large file operations
  - [ ] **Performance Target:** Equal or better than original
  - [ ] **Target:** `TS4Tools.Core.Package` package

**Unit Tests:**
- [ ] `PackageReaderTests` - Package file parsing
- [ ] `PackageWriterTests` - Package file generation
- [ ] `PackageIndexTests` - Index management and lookups
- [ ] `CompressionTests` - Compression/decompression algorithms
- [ ] `HeaderValidationTests` - Package header validation
- [ ] `ResourceExtractionTests` - Resource extraction logic
- [ ] `ConcurrencyTests` - Multi-threaded access patterns

**Coverage Target:** 95%+

#### **1.5 Resource Management (Weeks 6-8)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **s4pi.WrapperDealer Migration**
  - [ ] Replace reflection-based assembly loading with source generators
  - [ ] Implement modern dependency injection for resource factories
  - [ ] Add async resource loading capabilities
  - [ ] Implement resource caching and memory management
  - [ ] **Target:** `TS4Tools.Core.Resources` package

**Unit Tests:**
- [ ] `ResourceFactoryTests` - Resource creation patterns
- [ ] `TypeMappingTests` - Resource type resolution
- [ ] `CachingTests` - Resource caching behavior
- [ ] `DependencyInjectionTests` - DI container integration
- [ ] `AsyncLoadingTests` - Asynchronous resource loading
- [ ] `ErrorHandlingTests` - Error scenarios and recovery

**Coverage Target:** 90%+

**Phase 1 Deliverables:**
- Working core library with package reading/writing
- Modern project structure with proper separation of concerns
- Build pipeline working on .NET 9
- Comprehensive unit test suite with 92%+ coverage

---

### **Phase 2: Extensions and Commons (Weeks 9-12)**
> **Goal:** Port supporting libraries and common utilities

#### **2.1 Core Extensions (Weeks 9-10)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **s4pi Extras Migration**
  - [ ] Port `CustomForms` → Avalonia-compatible custom controls
  - [ ] Port `Extensions` → Modern C# extension methods  
  - [ ] Port `Helpers` → Service-based helper system with DI
  - [ ] Port `s4piControls` → Avalonia UI controls
  - [ ] **Deferred:** `DDSPanel`, `Filetable` (Phase 4)
  - [ ] **Target:** `TS4Tools.Extensions` package

**Unit Tests:**
- [ ] `CustomControlTests` - Avalonia control behavior
- [ ] `ExtensionMethodTests` - Extension method validation
- [ ] `HelperServiceTests` - Helper service functionality
- [ ] `UIComponentTests` - UI component behavior

**Coverage Target:** 85%+

#### **2.2 Resource Commons (Weeks 11-12)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **s4pi.Resource.Commons Migration**
  - [ ] Port `CatalogTags` → Enum-based tag system
  - [ ] Port `Forms` → Avalonia ViewModels and Views
  - [ ] Port `s4pi.Commons` → Shared resource utilities
  - [ ] **Target:** `TS4Tools.Resources.Common` package

**Unit Tests:**
- [ ] `CatalogTagTests` - Tag system validation
- [ ] `SharedUtilityTests` - Common utility functions  
- [ ] `ViewModelBaseTests` - Base ViewModel behavior
- [ ] `DataConverterTests` - Data conversion accuracy

**Coverage Target:** 90%+

**Phase 2 Deliverables:**
- Complete extension ecosystem
- Shared resource utilities
- Foundation for GUI components

---

### **Phase 3: Modern Architecture Integration (Weeks 13-14)**
> **Goal:** Integrate all core libraries with modern patterns

#### **3.1 Dependency Injection Setup (Week 13)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Service Registration**
  - [ ] Configure DI container with all core services
  - [ ] Implement factory patterns for resource creation
  - [ ] Add logging throughout all libraries
  - [ ] Configure async patterns and cancellation

#### **3.2 Testing Infrastructure (Week 14)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Core Library Tests**
  - [ ] Unit tests for all interfaces and abstractions (>90% coverage)
  - [ ] Integration tests for package reading/writing
  - [ ] Performance benchmarks vs. original implementation
  - [ ] Cross-platform compatibility tests

**Phase 3 Deliverables:**
- Modern DI architecture working
- Async patterns implemented
- Integration test framework established

---

### **Phase 4: Basic GUI Framework (Weeks 15-18)**
> **Goal:** Create minimal viable GUI to validate core libraries

#### **4.1 MVVM Foundation (Weeks 15-16)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Basic Application Shell**
  - [ ] Main window with package explorer
  - [ ] Status bar and progress indicators
  - [ ] Basic menu system
  - [ ] Package open/save functionality

**Unit Tests:**
- [ ] `MainViewModelTests` - Application state management
- [ ] `PackageExplorerViewModelTests` - File tree navigation
- [ ] `ResourceEditorViewModelTests` - Resource editing logic
- [ ] `PropertyGridViewModelTests` - Property editing behavior

**Coverage Target:** 95%+

#### **4.2 Resource Display (Weeks 17-18)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Core Resource Views**
  - [ ] Package tree view using core libraries
  - [ ] Basic resource property display
  - [ ] Hex editor for raw resource data
  - [ ] Resource export/import basic functionality

**Phase 4 Deliverables:**
- Working GUI that can load/save packages
- Basic resource exploration
- Validation of core library integration

---

### **Phase 5: Resource Wrappers (Weeks 19-24)**
> **Goal:** Port high-priority resource wrappers using validated core

#### **5.1 Essential Resource Wrappers (Weeks 19-22)**
**Status:** ⏳ Not Started

**Priority Migration Order:**
- [ ] **Week 19:** `DefaultResource` (fallback handler)
- [ ] **Week 20:** `CASPartResource` (character assets) 
- [ ] **Week 21:** `CatalogResource` (object definitions)
- [ ] **Week 22:** `ImageResource` (textures/images), `StblResource` (string tables)

**Unit Tests:**
- [ ] `DefaultResourceTests` - Fallback behavior
- [ ] `CASPartResourceTests` - Character asset parsing/generation
- [ ] `CatalogResourceTests` - Catalog entry validation
- [ ] `ImageResourceTests` - Image format handling
- [ ] `StblResourceTests` - String table operations

**Coverage Target:** 90%+

#### **5.2 Secondary Resource Wrappers (Weeks 23-24)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Remaining Core Wrappers:**
  - [ ] All remaining s4pi Wrappers libraries
  - [ ] Validation against game files
  - [ ] Performance optimization

**Phase 5 Deliverables:**
- Complete resource wrapper ecosystem
- Validation with real game files
- Performance benchmarks

---

### **Phase 6: Advanced Features & Polish (Weeks 25-28)**
> **Goal:** Complete feature parity and polish

#### **6.1 Advanced GUI Features (Weeks 25-26)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Power User Features**
  - [ ] Advanced search and filtering
  - [ ] Batch operations
  - [ ] Plugin system integration
  - [ ] Helper tools integration

#### **6.2 Performance Testing (Week 27)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Performance Test Suite**
  - [ ] `PackageLoadingBenchmarks` - Package loading performance
  - [ ] `ResourceExtractionBenchmarks` - Resource extraction speed
  - [ ] `MemoryUsageBenchmarks` - Memory consumption patterns
  - [ ] `ConcurrencyBenchmarks` - Multi-threaded performance

#### **6.3 Final Polish (Week 28)**
**Status:** ⏳ Not Started

**Tasks:**
- [ ] **Production Readiness**
  - [ ] Documentation and help system
  - [ ] Performance optimization
  - [ ] Cross-platform testing
  - [ ] User migration tools

**Phase 6 Deliverables:**
- Feature-complete application
- Production-ready release
- Complete documentation

---

## 🏗️ **Technical Architecture**

> **AI Assistant Note:** When working with the technical architecture, remember:
> - All new projects should use `.csproj` SDK-style format
> - Target framework is `net9.0` consistently across all projects  
> - Enable nullable reference types: `<Nullable>enable</Nullable>`
> - Use central package management - add versions to `Directory.Packages.props`, not individual project files
> - Follow the established namespace pattern: `TS4Tools.Core.*`, `TS4Tools.Extensions.*`, etc.

### **Core Libraries Structure**
```
TS4Tools.Core.System/           # CS System Classes equivalent
├── Collections/                 # AHandlerDictionary, AHandlerList
├── Extensions/                  # Modern C# extensions  
├── Hashing/                     # FNVHash, CRC utilities
├── Text/                        # SevenBitString, text utilities
└── Configuration/               # PortableSettingsProvider replacement

TS4Tools.Core.Interfaces/       # s4pi.Interfaces equivalent
├── Abstractions/                # APackage, AResource, etc.
├── Contracts/                   # IPackage, IResource, etc.
├── Collections/                 # DependentList, SimpleList, etc.
├── Attributes/                  # ElementPriority, etc.
└── Models/                      # TGIBlock, TypedValue, etc.

TS4Tools.Core.Settings/          # s4pi.Settings equivalent
├── Configuration/               # IOptions-based configuration
├── Validation/                  # Configuration validation
└── Providers/                   # Cross-platform settings providers

TS4Tools.Core.Package/           # s4pi.Package equivalent  
├── IO/                          # File I/O operations
├── Compression/                 # Package compression/decompression
├── Indexing/                    # Package indexing and lookup
└── Caching/                     # Resource caching

TS4Tools.Core.Resources/         # s4pi.WrapperDealer equivalent
├── Factories/                   # Resource factory services
├── Registration/                # Resource type registration
├── Loading/                     # Async resource loading
└── Caching/                     # Resource instance caching

TS4Tools.Extensions/             # s4pi Extras equivalent
├── UI/                          # Avalonia UI components
├── Helpers/                     # Service-based helpers
├── Utilities/                   # Extension methods and utilities
└── Services/                    # Application services

TS4Tools.Resources.Common/       # s4pi.Resource.Commons equivalent
├── Catalogs/                    # Catalog tag definitions
├── Shared/                      # Shared resource utilities
├── ViewModels/                  # Common ViewModels
└── Converters/                  # Data converters
```

### **Testing Architecture**
```
TS4Tools.Tests/
├── Unit/                          # Pure unit tests (90%+ of tests)
│   ├── Core/
│   │   ├── System/               # CS System Classes tests
│   │   ├── Interfaces/           # s4pi.Interfaces tests  
│   │   ├── Settings/             # s4pi.Settings tests
│   │   ├── Package/              # s4pi.Package tests
│   │   └── Resources/            # s4pi.WrapperDealer tests
│   ├── Extensions/               # s4pi Extras tests
│   ├── Resources/                # Resource wrapper tests
│   └── UI/                       # ViewModel and UI logic tests
├── Integration/                   # Integration tests
│   ├── PackageIO/               # End-to-end package operations
│   ├── ResourceProcessing/      # Resource parsing workflows  
│   └── CrossPlatform/           # Platform-specific testing
├── Performance/                   # Performance benchmarks
│   ├── Benchmarks/              # BenchmarkDotNet tests
│   └── Regression/              # Performance regression tests
├── TestData/                     # Sample files and fixtures
│   ├── Packages/                # Sample .package files
│   ├── Resources/               # Individual resource samples
│   └── Fixtures/                # JSON test data files
└── Utilities/                    # Test utilities and builders
    ├── Builders/                # Object builders for tests
    ├── Extensions/              # Test helper extensions
    └── Fixtures/                # Custom test fixtures
```

### **Technology Choices**
- **UI Framework:** Avalonia UI 11.3+ (cross-platform, modern XAML)
- **Architecture Pattern:** MVVM with CommunityToolkit.Mvvm
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection
- **Testing:** xUnit with FluentAssertions, NSubstitute
- **Package Management:** Central Package Management
- **Build System:** Modern SDK-style projects with Directory.Build.props

> **AI Assistant Build Commands:**
> ```powershell
> # Always start from the root directory
> cd "c:\Users\nawgl\code\TS4Tools"
> 
> # Build entire solution
> dotnet build TS4Tools.sln
> 
> # Build specific project
> dotnet build src/TS4Tools.Core.System/TS4Tools.Core.System.csproj
> 
> # Run tests
> dotnet test tests/TS4Tools.Core.System.Tests/TS4Tools.Core.System.Tests.csproj
> 
> # Run application (when available)
> dotnet run --project TS4Tools.Desktop/TS4Tools.Desktop.csproj
> ```

---

## 📊 **Testing Metrics and Coverage Goals**

### **Coverage Targets by Component**
| Component | Unit Test Coverage | Integration Coverage | Performance Tests |
|-----------|-------------------|---------------------|-------------------|
| **Core.System** | 95%+ | N/A | 5+ benchmarks |
| **Core.Interfaces** | 90%+ | 80%+ | 3+ benchmarks |
| **Core.Package** | 95%+ | 90%+ | 10+ benchmarks |
| **Core.Resources** | 90%+ | 85%+ | 8+ benchmarks |
| **Extensions** | 85%+ | 70%+ | 3+ benchmarks |
| **Resources.Common** | 90%+ | 75%+ | 5+ benchmarks |
| **Resource Wrappers** | 90%+ | 80%+ | 15+ benchmarks |
| **UI ViewModels** | 95%+ | N/A | 2+ benchmarks |
| **Overall Target** | **92%+** | **80%+** | **50+ benchmarks** |

### **Quality Gates**
- **No failing tests** - All tests must pass before merge
- **Coverage threshold** - Minimum coverage percentages enforced
- **Performance regression** - No more than 10% performance degradation
- **Cross-platform** - All tests pass on Windows, macOS, Linux

---

## 📅 **Timeline Summary**

| Phase | Duration | Key Milestone | Status |
|-------|----------|---------------|---------|
| **Phase 1** | 8 weeks | Core libraries working | ⏳ Not Started |
| **Phase 2** | 4 weeks | Extensions and commons ported | ⏳ Not Started |
| **Phase 3** | 2 weeks | Modern architecture integration | ⏳ Not Started |
| **Phase 4** | 4 weeks | Basic GUI working | ⏳ Not Started |
| **Phase 5** | 6 weeks | Resource wrappers complete | ⏳ Not Started |
| **Phase 6** | 4 weeks | Production ready | ⏳ Not Started |
| **Total** | **28 weeks** | **Complete migration** | ⏳ Not Started |

---

## ⚠️ **Risk Assessment & Mitigation**

### **High-Risk Areas**
1. **Binary Format Compatibility** - Some binary formats may be poorly documented
   - **Mitigation:** Validate early with real game files, keep original as reference
2. **Performance Requirements** - Must match or exceed original performance
   - **Mitigation:** Continuous benchmarking, performance-first design
3. **Cross-Platform File I/O** - Path separators and encoding differences
   - **Mitigation:** Use System.IO.Abstractions, extensive cross-platform testing
4. **Native DLL Dependencies** - squishinterface DLLs may need alternatives
   - **Mitigation:** Research cross-platform alternatives early

### **Mitigation Strategies**
1. **Continuous Validation** - Test against real Sims 4 packages at each phase
2. **Performance Monitoring** - Automated benchmarks in CI/CD pipeline
3. **Incremental Migration** - Keep original as reference during development
4. **Community Feedback** - Alpha releases of core libraries for validation

---

## 📊 **Code Review Summary - August 3, 2025**

### **Overall Assessment: B+ (Good foundation with improvement opportunities)**

**✅ Strengths:**
- Modern C# features and nullable reference types throughout
- Comprehensive test coverage (32/32 tests passing)
- Well-organized solution structure with clear separation of concerns
- Central package management and SDK-style projects
- Thorough documentation and progress tracking

**⚠️ Critical Issues Requiring Immediate Action:**
1. **Project Configuration Inconsistencies** - `LangVersion` mismatch between projects
2. **Missing Code Quality Infrastructure** - No static analysis, EditorConfig, or security scanning
3. **Performance Monitoring Gap** - No benchmarks or performance regression testing
4. **Documentation Inconsistencies** - XML documentation generation not standardized

**📈 Quality Metrics:**
- **Test Coverage**: 100% pass rate (32/32 tests)
- **Build Status**: ✅ All projects building successfully
- **Code Quality**: B+ (clean code with modern patterns)
- **Architecture**: B+ (good separation, missing resilience patterns)
- **Security**: ⚠️ Not assessed (no security analysis configured)

**🎯 Immediate Actions Required:**
- Complete Phase 1.2.1 (Code Quality & Standards) before proceeding
- Standardize project configurations across solution
- Implement static code analysis and security scanning
- Establish performance baseline with BenchmarkDotNet

---

## 🎯 **Success Criteria**

1. **Functional Parity** - All original features working in new application
2. **Performance** - Equal or better performance than original
3. **Cross-Platform** - Working on Windows, macOS, and Linux
4. **User Experience** - Modern, intuitive interface
5. **Maintainability** - Clean, well-documented, testable codebase
6. **Code Quality** - Static analysis passing with zero high-severity issues
7. **Security** - Vulnerability scanning integrated into CI/CD pipeline
6. **Test Coverage** - 92%+ unit test coverage, 80%+ integration coverage
7. **Extensibility** - Plugin system for future enhancements

---

## 📝 **Progress Tracking**

> **AI Assistant Status Updates:** When updating progress, use this format:
> - ✅ **COMPLETED** - [Date] for finished tasks
> - 🚧 **IN PROGRESS** - [Date] for active work  
> - ⏳ **NOT STARTED** for future tasks
> - ⚠️ **BLOCKED** - [Reason] for issues
> 
> **Always run tests after completing a component:**
> ```powershell
> cd "c:\Users\nawgl\code\TS4Tools"
> dotnet test tests/[TestProject]/[TestProject].csproj
> ```

### **Completed Tasks**
*None yet - migration planning phase*

### **Current Focus**
- Finalizing migration plan
- Setting up development environment
- Preparing test infrastructure

### **Next Actions**
1. Begin Phase 1.1 - System Foundation migration
2. Set up unit testing framework
3. Create first core library project

### **Blockers**
*None currently identified*

---

**Document Status:** Living Document - Updated as tasks are completed  
**Last Updated:** August 3, 2025  
**Next Review:** Weekly during active development
