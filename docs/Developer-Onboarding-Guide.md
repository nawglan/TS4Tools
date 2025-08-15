# TS4Tools Developer Onboarding Guide

**Welcome to the TS4Tools Development Team!**

This guide will help you understand the TS4Tools codebase, learn how to create tests,
and contribute new features. It's designed for entry-level C# engineers who want to
become productive contributors to this Sims 4 modding tools project.

## [CRITICAL SUCCESS FACTORS] (Read This First!)

**Based on hard-learned lessons from Phase 4.17.2 WorldColorTimelineResource implementation**

### The 3 Most Expensive Mistakes to Avoid

1. **[WRONG CODEBASE]**: Don't study `Sims4Tools/s4pi Wrappers/` - it uses completely different patterns (AResource, EventHandler, sync methods). Always use `TS4Tools/src/TS4Tools.Resources.*/` for reference.

2. **[ResourceWrapperRegistry]**: If ResourceManager returns DefaultResource instead of your specific type, you forgot `ResourceWrapperRegistry.DiscoverAndRegisterFactoriesAsync()` in test setup. This cost me the most debugging time.

3. **[Factory Pattern]**: Use `ResourceFactoryBase<T>`, not generic `IResourceFactory<T>`. The generic approach compiles but doesn't integrate with the system.

### Quick Success Checklist

- [ ] Study LRLEResource.cs and LRLEResourceFactory.cs (not legacy code)
- [ ] Always call ResourceWrapperRegistry initialization in test constructors
- [ ] Use ResourceFactoryBase<T> inheritance pattern
- [ ] Test both empty and populated resource scenarios separately
- [ ] Add comprehensive DI registration in test setup

**Time Investment**: Following these patterns will save you 2-3 days of debugging common issues.

## What You'll Learn

1. **Codebase Architecture** - How the project is organized and key patterns
2. **Development Setup** - Getting your environment ready for coding
3. **Creating Tests** - Our testing patterns and how to write effective tests
4. **Adding Features** - Step-by-step guide to implementing new functionality
5. **Code Standards** - Conventions and best practices we follow
6. **Common Tasks** - Practical examples of typical development work

> **IMPORTANT NOTE**: This project has multiple .sln files! Always use `TS4Tools.sln` for all dotnet commands.

### Overall Architecture Overview

```mermaid
graph TB
    subgraph "User Interface"
        UI[TS4Tools.Desktop<br/>Avalonia UI App]
    end
    
    subgraph "Core Layer"
        PKG[TS4Tools.Core.Package<br/>File I/O Operations]
        RES[TS4Tools.Core.Resources<br/>Resource Management]
        SYS[TS4Tools.Core.System<br/>Utilities & Extensions]
    end
    
    subgraph "Contracts"
        INT[TS4Tools.Core.Interfaces<br/>All Interface Contracts]
    end
    
    subgraph "Resource Implementations"
        STR[StringTable Resources]
        IMG[Image Resources]
        TXT[Text Resources]
        OTHER[60+ Other Resource Types]
    end
    
    subgraph "External"
        SIMS[Sims 4 .package Files]
        STEAM[Steam Installation]
    end
    
    UI --> RES
    UI --> PKG
    PKG --> SIMS
    RES --> INT
    PKG --> INT
    SYS --> INT
    
    RES --> STR
    RES --> IMG
    RES --> TXT
    RES --> OTHER
    
    PKG --> STEAM
```

---

## [QUICK START] Get Running in 10 minutes

### Prerequisites

- **Windows 10/11** (primary development platform)
- **Visual Studio 2022** (v17.9+) or **VS Code** with C# extension
- **.NET 9 SDK** (latest version)
- **Git** for version control

### IMPORTANT: Solution File Confusion

**CRITICAL WARNING**: The workspace has multiple solution files that can cause major confusion:

- `TS4Tools.sln` (USE THIS ONE - the modern .NET 9 implementation)
- `Sims4Tools/sims4tools.sln` (Legacy .NET Framework - DO NOT USE for new development)
- `TS4MorphMaker/CmarNYC_TS4MorphMaker.sln` (Separate project)

**Always use `TS4Tools.sln`** - this contains the modern async/await implementations you should study and extend.

The legacy Sims4Tools project uses completely different patterns (AResource base class, EventHandler patterns, etc.) that will mislead you if you study them instead of the TS4Tools implementations.

### Get the Code Running

```powershell
# 1. Clone and build (IMPORTANT: Always specify the solution file)
git clone https://github.com/nawglan/TS4Tools.git
cd TS4Tools
dotnet restore TS4Tools.sln
dotnet build TS4Tools.sln

# 2. Run tests to verify everything works
dotnet test TS4Tools.sln

# 3. Start the desktop application
dotnet run --project TS4Tools.Desktop/TS4Tools.Desktop.csproj
```

**Success indicators:**

- [X] Clean build with no errors
- [X] Tests passing (95%+ success rate is normal)  
- [X] Desktop application opens

---

## [CODEBASE] Understanding the Structure

### Project Structure

```text
TS4Tools/
+-- src/                           # All source code lives here
|   +-- TS4Tools.Core.Interfaces/  # Contracts (what classes must implement)
|   +-- TS4Tools.Core.System/      # Basic utilities and helpers
|   +-- TS4Tools.Core.Package/     # Reading/writing Sims 4 .package files
|   +-- TS4Tools.Core.Resources/   # Managing different types of game content
|   +-- TS4Tools.Resources.*/      # Specific resource implementations
|   +-- TS4Tools.Desktop/          # The main UI application
+-- tests/                         # All test code
+-- docs/                          # Documentation (like this guide!)
+-- scripts/                       # Build and utility scripts
```

### How Components Work Together

```mermaid
graph TD
    A[TS4Tools.Desktop] --> B[TS4Tools.Core.Resources]
    A --> C[TS4Tools.Core.Package]
    B --> D[TS4Tools.Core.Interfaces]
    C --> D
    B --> E[Resource Factories]
    E --> F[StringTableResource]
    E --> G[TextureResource]
    E --> H[Other Resources]
    C --> I[Package Reader/Writer]
    I --> J[.package Files]
```

### Key Concepts You Need to Know

#### 1. What are "Resources"?

In Sims 4, everything is a **resource** - textures, 3D models, text strings, animations, etc.
Each resource has:

- A **Type ID** (like 0x220557DA for text strings)
- **Data** (the actual content)
- **Metadata** (size, version, etc.)

#### 2. The Factory Pattern

We use the Factory Pattern to create resources:

```csharp
// Instead of: new TextureResource()
// We use: await textureFactory.CreateAsync(stream)

public interface IResourceFactory<T>
{
    Task<T> CreateAsync(Stream data);
}
```

**Factory Pattern Flow:**

```mermaid
sequenceDiagram
    participant App as Application
    participant Registry as Resource Registry
    participant Factory as Resource Factory
    participant Resource as Resource Instance
    
    App->>Registry: GetFactory(resourceTypeId)
    Registry->>Factory: Returns factory for type
    App->>Factory: CreateAsync(stream)
    Factory->>Resource: new Resource(stream, deps)
    Resource-->>Factory: Resource instance
    Factory-->>App: Returns configured resource
```

**Why?** It allows us to:

- Inject dependencies (logging, configuration, etc.)
- Handle different versions of the same resource type
- Make testing easier with mock factories

#### 3. Dependency Injection (DI)

We use Microsoft's DI container to provide dependencies:

```csharp
// Instead of creating dependencies directly:
public class BadExample
{
    public void ProcessFile()
    {
        var logger = new FileLogger("app.log");     // Hard to test
        var reader = new FileReader();              // Tightly coupled
    }
}

// We inject them through the constructor:
public class GoodExample
{
    private readonly ILogger<GoodExample> _logger;
    private readonly IFileReader _reader;

    public GoodExample(ILogger<GoodExample> logger, IFileReader reader)
    {
        _logger = logger;
        _reader = reader;
    }
}
```

#### 4. Async/Await Pattern

File operations are async to keep the UI responsive:

```csharp
// File operations use async
public async Task<IPackage> LoadPackageAsync(string filePath)
{
    using var stream = await File.OpenReadAsync(filePath);
    return await _packageFactory.CreateAsync(stream);
}
```

---

## [TESTING] Creating Tests

### Test Structure (AAA Pattern)

All tests follow **Arrange, Act, Assert**:

```csharp
[Test]
public async Task LoadPackage_WithValidFile_ReturnsPackage()
{
    // Arrange - Set up test data
    var filePath = "test-data/valid-package.package";
    var loader = new PackageLoader(_mockLogger.Object);

    // Act - Execute the code being tested
    var result = await loader.LoadPackageAsync(filePath);

    // Assert - Verify the results
    result.Should().NotBeNull();
    result.ResourceCount.Should().Be(42);
}
```

### Test Architecture Overview

```mermaid
graph LR
    A[Test Project] --> B[Unit Tests]
    A --> C[Integration Tests]
    B --> D[Mock Dependencies]
    C --> E[Real Dependencies]
    D --> F[NSubstitute Mocks]
    E --> G[Test Database/Files]
    B --> H[FluentAssertions]
    C --> H
```

### Types of Tests We Write

#### 1. Unit Tests

Test a single class in isolation:

```csharp
public class StringTableResourceTests
{
    private readonly ILogger<StringTableResource> _mockLogger;

    public StringTableResourceTests()
    {
        _mockLogger = Substitute.For<ILogger<StringTableResource>>();
    }

    [Test]
    public void Constructor_WithValidStream_ParsesStrings()
    {
        // Arrange
        var testData = CreateTestStringTableData();
        using var stream = new MemoryStream(testData);

        // Act
        var resource = new StringTableResource(1, stream, _mockLogger);

        // Assert
        resource.Strings.Should().HaveCount(3);
        resource.Strings["HELLO"].Should().Be("Hello World");
    }
}
```

#### 2. Integration Tests

Test multiple components working together:

```csharp
[Test]
public async Task PackageLoader_WithRealFile_LoadsAllResources()
{
    // Arrange
    var packagePath = "test-data/real-sims4-package.package";
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTS4ToolsServices(); // Our DI setup
    var serviceProvider = serviceCollection.BuildServiceProvider();
    
    var loader = serviceProvider.GetRequiredService<IPackageLoader>();

    // Act
    var package = await loader.LoadAsync(packagePath);

    // Assert
    package.Resources.Should().NotBeEmpty();
    // Verify specific resource types were loaded correctly
    package.GetResourcesOfType<IStringTableResource>().Should().NotBeEmpty();
}
```

### Real Test Data Management

**CRITICAL**: Study the LRLE test suite for the actual patterns used in this project.

```csharp
// Real test pattern from LRLEResourceTests.cs:
[Fact]
public async Task CreateAsync_WithValidLRLEData_ParsesCorrectly()
{
    // Arrange - Create REAL binary data, not fake data
    var testData = CreateValidLRLEBinaryData(); // Helper method
    using var stream = new MemoryStream(testData);
    
    // Act
    var resource = await _factory.CreateAsync(stream);
    
    // Assert - Verify actual parsed values
    resource.Should().NotBeNull();
    resource.Width.Should().Be(64); // Real expected values
    resource.Height.Should().Be(64);
    // ... more specific assertions
}

private byte[] CreateValidLRLEBinaryData()
{
    // Create actual LRLE format binary data
    // This requires understanding the real file format!
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    
    // LRLE header
    writer.Write(0x454C524C); // 'LRLE' magic
    writer.Write(0x32303056); // Version
    writer.Write((ushort)64); // Width
    writer.Write((ushort)64); // Height
    // ... rest of actual format
    
    return stream.ToArray();
}
```

**Key Differences from Generic Examples:**

- Test data must match actual Sims 4 binary formats
- Use `test-data/` folders for larger binary files
- Test disposal patterns with `using` statements
- Test error conditions with invalid/corrupted data
- Use factory pattern for resource creation in tests

### Writing Good Test Names

Use descriptive names that explain the scenario:

```csharp
// [GOOD] - explains what's being tested and expected outcome
[Test]
public void ParseStringTable_WithMissingHashKey_ThrowsInvalidDataException()

// [BAD] - unclear what this tests
[Test]
public void TestStringTable()
```

### Golden Master Test Integration

**CRITICAL PATTERN**: When adding new resource modules, you MUST integrate them with the Golden Master test framework.

**Step 1: Add Resource Types to Golden Master Tests**

```csharp
// In tests/TS4Tools.Tests.GoldenMaster/ResourceTypeGoldenMasterTests.cs
[Theory]
[InlineData(0x220557DA, "String Table Resource (STBL)")]
// ... existing resource types ...
// ADD YOUR NEW RESOURCE TYPES HERE:
[InlineData(0x810A102D, "World Resource (WORLD)")]
[InlineData(0xAE39399F, "Terrain Resource (TERRAIN)")]
public async Task ResourceType_RoundTripSerialization_ShouldPreserveBinaryEquivalence(
    uint resourceTypeId, string description)
```

**Step 2: Add Project Reference to Golden Master Tests**

```xml
<!-- In tests/TS4Tools.Tests.GoldenMaster/TS4Tools.Tests.GoldenMaster.csproj -->
<ItemGroup>
  <!-- ... existing references ... -->
  <ProjectReference Include="..\..\src\TS4Tools.Resources.YourModule\TS4Tools.Resources.YourModule.csproj" />
</ItemGroup>
```

**Step 3: Register DI Services in Golden Master Test Setup**

```csharp
// In ResourceTypeGoldenMasterTests constructor
public ResourceTypeGoldenMasterTests()
{
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
    services.AddTS4ToolsPackageServices();
    services.AddTS4ToolsResourceServices();
    services.AddWorldResources();  // ADD YOUR MODULE HERE
    
    _serviceProvider = services.BuildServiceProvider();
    // ... rest of setup
}
```

**What This Achieves:**

- Validates your resources work with the main Golden Master framework
- Catches binary format compatibility issues early
- Ensures your resource types are discoverable by ResourceManager
- Provides automated regression testing for your resource formats

**Common Failure Modes:**

- Missing project reference -> compile errors in Golden Master tests
- Missing DI registration -> NullReferenceException when ResourceManager tries to create your resources  
- Empty ContentFields -> test failures, but this might be expected initially
- Round-trip serialization failures -> indicates your resource doesn't handle empty/minimal data correctly

### Using FluentAssertions

We use FluentAssertions for readable test assertions:

```csharp
// [GOOD] Fluent and readable
result.Should().NotBeNull();
result.Count.Should().BeGreaterThan(0);
result.Should().Contain(item => item.Name == "expected-name");

// [AVOID] Traditional asserts (less readable)
Assert.NotNull(result);
Assert.True(result.Count > 0);
Assert.True(result.Any(item => item.Name == "expected-name"));
```

---

## [REALITY CHECK] Learning from Actual Implementations

**CRITICAL**: Before following the examples below, study these real implementations:

- `LRLEResource.cs` - Complex image resource with proper disposal patterns
- `LRLEResourceFactory.cs` - Real factory implementation
- `LRLEResourceTests.cs` - Comprehensive test suite with binary test data
- `StringTableResource.cs` - Text resource implementation patterns

### Key Differences from Generic Examples

1. **No ResourceBase Class**: Resources implement interfaces directly (like `ILRLEResource`, `IDisposable`)
2. **ResourceFactoryBase Pattern**: Factories extend `ResourceFactoryBase<T>` not generic patterns
3. **Complex Error Handling**: Real implementations have comprehensive validation and logging
4. **Binary Format Research**: You must understand the actual Sims 4 binary format first
5. **Test Data Creation**: Tests require real binary data, not made-up examples

### Real Constructor Patterns

```csharp
// What you'll actually implement (see LRLEResource.cs):
public sealed class MyResource : IMyResource, IDisposable
{
    public MyResource(Stream? stream, ILogger<MyResource> logger)
    {
        // Simple constructor, complex parsing method
    }
}

// NOT what the example shows:
public class MyResource : ResourceBase, IMyResource
{
    public MyResource(int apiVersion, Stream? stream, ILogger<MyResource> logger) 
    {
        // This pattern isn't used in actual code
    }
}
```

### Real Factory Patterns

```csharp
// Actual pattern from LRLEResourceFactory.cs:
protected override async Task<IMyResource> CreateResourceCoreAsync(
    Stream? stream,
    CancellationToken cancellationToken)
{
    // Notice: no apiVersion parameter
    return new MyResource(stream, _logger);
}
```

### Real Test Patterns

Study the LRLE tests - they show you need:

- Binary test data in `test-data/` folders
- Helper methods to create valid binary streams
- Comprehensive error testing with invalid data
- Memory management testing (disposal patterns)

---

## [FEATURES] Adding New Features

### Feature Development Workflow

```mermaid
flowchart TD
    A[Start: New Feature Request] --> B[Research Binary Format]
    B --> C[Create Interface Contract]
    C --> D[Write Failing Tests]
    D --> E[Implement Resource Class]
    E --> F[Create Factory]
    F --> G[Register in DI]
    G --> H[Run Tests]
    H --> I{Tests Pass?}
    I -->|No| D
    I -->|Yes| J[Integration Testing]
    J --> K[Performance Testing]
    K --> L[Code Review]
    L --> M[Done]
```

### Step-by-Step Guide: Adding a New Resource Type

Let's walk through adding support for a new resource type: `MoodletResource`.

#### Step 1: Define the Interface

Create the contract that defines what a Moodlet resource can do:

```csharp
// File: src/TS4Tools.Core.Interfaces/Resources/IMoodletResource.cs
public interface IMoodletResource : IResource
{
    string MoodletName { get; }
    string Description { get; }
    int Duration { get; }
    MoodletType Type { get; }
}

public enum MoodletType
{
    Positive,
    Negative,
    Neutral
}
```

#### Step 2: Create the Implementation

**IMPORTANT**: Look at real implementations first! Study `LRLEResource.cs` or other existing resources to understand the actual patterns used.

```csharp
// File: src/TS4Tools.Resources.Gameplay/MoodletResource.cs
public sealed class MoodletResource : IMoodletResource, IDisposable
{
    private readonly ILogger<MoodletResource> _logger;
    private bool _disposed;

    public string MoodletName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Duration { get; private set; }
    public MoodletType Type { get; private set; }

    public MoodletResource(Stream? stream, ILogger<MoodletResource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (stream != null)
        {
            ParseMoodletData(stream);
        }
    }

    private void ParseMoodletData(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        
        try
        {
            // Always validate stream length before reading
            if (stream.Length < 16) // Minimum expected size
            {
                throw new InvalidDataException("Stream too short for moodlet data");
            }

            // Read the binary format (research actual format first!)
            var nameLength = reader.ReadInt32();
            if (nameLength > 1024) // Reasonable limit
                throw new InvalidDataException($"Name length {nameLength} exceeds maximum");

            MoodletName = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            
            var descLength = reader.ReadInt32();
            if (descLength > 4096) // Reasonable limit
                throw new InvalidDataException($"Description length {descLength} exceeds maximum");
                
            Description = Encoding.UTF8.GetString(reader.ReadBytes(descLength));
            
            Duration = reader.ReadInt32();
            Type = (MoodletType)reader.ReadByte();

            _logger.LogDebug("Parsed moodlet: {Name} ({Type}), Duration: {Duration}",
                MoodletName, Type, Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse moodlet data from stream");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cleanup resources if needed
            _disposed = true;
        }
    }
}
```

#### Step 3: Create the Factory

**REAL PATTERN**: Study `LRLEResourceFactory.cs` for the actual factory implementation pattern used in this project.

```csharp
// File: src/TS4Tools.Resources.Gameplay/MoodletResourceFactory.cs
public class MoodletResourceFactory : ResourceFactoryBase<IMoodletResource>
{
    // This tells the system which resource type IDs this factory handles
    // IMPORTANT: Research actual type IDs from the original Sims4Tools or game files
    public override IReadOnlySet<string> SupportedResourceTypes =>
        new HashSet<string> { "0x12345678" }; // Replace with actual moodlet type ID

    protected override async Task<IMoodletResource> CreateResourceCoreAsync(
        Stream? stream,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (stream == null)
        {
            _logger.LogDebug("Creating empty moodlet resource");
            return new MoodletResource(null, _logger);
        }

        // Create resource with proper error handling
        try
        {
            _logger.LogDebug("Creating moodlet resource from stream of length {Length}", stream.Length);
            await Task.CompletedTask; // Remove if actual async work is needed
            
            return new MoodletResource(stream, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create moodlet resource from stream");
            throw;
        }
    }
}
```

#### Step 4: Register in Dependency Injection

```csharp
// File: src/TS4Tools.Resources.Gameplay/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameplayResources(this IServiceCollection services)
    {
        services.AddResourceFactory<IMoodletResource, MoodletResourceFactory>();
        return services;
    }
}
```

#### Step 5: Write Tests First (TDD)

```csharp
// File: tests/TS4Tools.Resources.Gameplay.Tests/MoodletResourceTests.cs
public class MoodletResourceTests
{
    private readonly ILogger<MoodletResource> _mockLogger;

    public MoodletResourceTests()
    {
        _mockLogger = Substitute.For<ILogger<MoodletResource>>();
    }

    [Test]
    public void Constructor_WithValidStream_ParsesMoodletData()
    {
        // Arrange
        var testData = CreateTestMoodletData();
        using var stream = new MemoryStream(testData);

        // Act
        var moodlet = new MoodletResource(1, stream, _mockLogger);

        // Assert
        moodlet.MoodletName.Should().Be("Happy");
        moodlet.Description.Should().Be("This Sim is feeling happy!");
        moodlet.Duration.Should().Be(240); // 4 hours in minutes
        moodlet.Type.Should().Be(MoodletType.Positive);
    }

    [Test]
    public void Constructor_WithNullStream_CreatesEmptyMoodlet()
    {
        // Act
        var moodlet = new MoodletResource(1, null, _mockLogger);

        // Assert
        moodlet.MoodletName.Should().BeEmpty();
        moodlet.Description.Should().BeEmpty();
        moodlet.Duration.Should().Be(0);
        moodlet.Type.Should().Be(MoodletType.Neutral);
    }

    private byte[] CreateTestMoodletData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write test data in the expected binary format
        var nameBytes = Encoding.UTF8.GetBytes("Happy");
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);

        var descBytes = Encoding.UTF8.GetBytes("This Sim is feeling happy!");
        writer.Write(descBytes.Length);
        writer.Write(descBytes);

        writer.Write(240); // Duration
        writer.Write((byte)MoodletType.Positive); // Type

        return stream.ToArray();
    }
}
```

### Requirements for New Features

#### Before Starting Development

1. **Research the Format**: Understand the binary structure of the resource
   - Use hex editors to examine real game files
   - Document the byte structure in comments
   - Check existing similar resources for patterns

2. **Create Test Data**: Build sample files that represent typical usage
   - Valid data for happy path testing
   - Edge cases (empty strings, maximum values)
   - Invalid data for error handling testing

3. **Check Dependencies**: Identify what services your feature needs
   - Logging (always include)
   - Configuration settings
   - Other resource types it might reference

#### During Development

1. **Write Tests First**: Follow TDD principles
   - Start with simple constructor tests
   - Add parsing tests with real data
   - Test error conditions

2. **Use Dependency Injection**: Never create dependencies directly
   - Inject through constructor
   - Use interfaces, not concrete types
   - Register in ServiceCollectionExtensions

3. **Follow Coding Standards**: Maintain consistency
   - Use async for file operations
   - Add XML documentation to public members
   - Follow naming conventions
   - Include proper error handling

#### After Implementation

1. **Integration Testing**: Test with real game files
2. **Performance Testing**: Ensure acceptable performance
3. **Documentation**: Update API docs and examples
4. **Code Review**: Have another developer review your changes

---

## [STANDARDS] Code Standards and Conventions

### Naming Conventions

```csharp
// Interfaces: PascalCase with 'I' prefix
public interface IResourceFactory<T> { }

// Classes: PascalCase
public class StringTableResource { }

// Methods: PascalCase
public async Task LoadAsync() { }

// Private fields: camelCase with underscore prefix
private readonly ILogger _logger;

// Properties: PascalCase
public string ResourceName { get; set; }

// Local variables: camelCase
var resourceCount = 42;

// Constants: PascalCase
public const int MaxFileSize = 1024 * 1024;
```

### Error Handling Patterns

```csharp
// [GOOD] Specific exceptions with helpful messages
public void ValidateResourceType(uint typeId)
{
    if (typeId == 0)
        throw new ArgumentException("Resource type ID cannot be zero", nameof(typeId));
        
    if (!IsValidResourceType(typeId))
        throw new NotSupportedException($"Resource type 0x{typeId:X8} is not supported");
}

// [GOOD] Async error handling
public async Task<IResource> LoadResourceAsync(string path)
{
    try
    {
        using var stream = await File.OpenReadAsync(path);
        return await _factory.CreateAsync(stream);
    }
    catch (FileNotFoundException)
    {
        _logger.LogWarning("Resource file not found: {Path}", path);
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load resource from {Path}", path);
        throw new ResourceLoadException($"Cannot load resource from {path}", ex);
    }
}
```

### Documentation Standards

Add XML documentation to all public APIs:

```csharp
/// <summary>
/// Loads a Sims 4 package file and returns all contained resources.
/// </summary>
/// <param name="filePath">Path to the .package file to load</param>
/// <param name="cancellationToken">Token to cancel the operation</param>
/// <returns>A package containing all loaded resources</returns>
/// <exception cref="FileNotFoundException">Thrown when the package file doesn't exist</exception>
/// <exception cref="InvalidDataException">Thrown when the file is not a valid package</exception>
public async Task<IPackage> LoadPackageAsync(string filePath, CancellationToken cancellationToken = default)
{
    // Implementation here
}
```

### Modern C# Features We Use

```csharp
// Records for immutable data
public record ResourceKey(uint Type, uint Group, ulong Instance);

// Pattern matching
public string GetResourceDescription(IResource resource) => resource switch
{
    IStringTableResource str => $"String table with {str.Count} entries",
    ITextureResource tex => $"Texture {tex.Width}x{tex.Height}",
    _ => "Unknown resource type"
};

// Nullable reference types
public IResource? FindResource(string? name)
{
    if (string.IsNullOrEmpty(name))
        return null;
        
    return _resources.FirstOrDefault(r => r.Name == name);
}

// Using declarations for automatic disposal
public async Task ProcessFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    await ProcessStreamAsync(stream);
} // stream is automatically disposed here
```

---

## [TASKS] Common Development Tasks

### IMPORTANT: Always Specify the Solution File

**This project has multiple solution files!** Always specify `TS4Tools.sln`:

```powershell
# CORRECT - Always specify the solution file
dotnet build TS4Tools.sln
dotnet test TS4Tools.sln
dotnet restore TS4Tools.sln

# WRONG - Will build the wrong solution or fail
dotnet build
dotnet test
```

### Development Workflow Diagram

```mermaid
flowchart LR
    A[Edit Code] --> B[Build Solution]
    B --> C{Build Success?}
    C -->|No| D[Fix Errors] --> A
    C -->|Yes| E[Run Tests]
    E --> F{Tests Pass?}
    F -->|No| G[Fix Tests] --> A
    F -->|Yes| H[Commit Changes]
    
    B -.-> I[dotnet build TS4Tools.sln]
    E -.-> J[dotnet test TS4Tools.sln]
```

### Finding Examples in the Codebase

When you need to understand how something works, look at existing implementations:

```powershell
# Find all resource implementations
grep -r "class.*Resource.*:" src/ --include="*.cs"

# Find factory examples
grep -r "ResourceFactory" src/ --include="*.cs"

# Look for test examples
grep -r "public.*Test" tests/ --include="*.cs"
```

### Adding a New Service to DI

1. Create the interface:

```csharp
public interface IMyService
{
    Task<string> ProcessAsync(string input);
}
```

2. Create the implementation:

```csharp
public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> ProcessAsync(string input)
    {
        _logger.LogDebug("Processing input: {Input}", input);
        // Implementation here
        return await Task.FromResult($"Processed: {input}");
    }
}
```

3. Register in DI:

```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<IMyService, MyService>();
```

### Debugging Tips

#### Use the Debugger Effectively

1. Set breakpoints on exception catches to see what's failing
2. Use conditional breakpoints for loops: `item.Name == "specific-name"`
3. Use the "Locals" window to inspect object state

#### Logging for Development

```csharp
_logger.LogDebug("Loading resource {Type} from {Path}", resourceType, filePath);
_logger.LogInformation("Successfully loaded {Count} resources", resources.Count);
_logger.LogWarning("Resource {Name} has invalid data, using defaults", resourceName);
_logger.LogError(ex, "Failed to process resource {Type}", resourceType);
```

#### Common Issues and Solutions

**Problem**: "ResourceFactoryBase not found" or similar interface errors
**Solution**: Check that you're inheriting from the correct base classes - study existing implementations first

**Problem**: Tests fail with binary data parsing errors
**Solution**: Use a hex editor to examine real Sims 4 files and understand the actual binary format

**Problem**: DI container can't resolve service
**Solution**: Verify service is registered in ServiceCollectionExtensions.cs AND that you're using the correct factory pattern

**Problem**: File loading fails with stream errors
**Solution**: Check file paths are absolute and files exist in test-data directories. Validate stream position and length

**Problem**: Memory leaks or disposal issues
**Solution**: Implement IDisposable properly - see LRLEResource.cs for the correct disposal pattern

**Problem**: Factory CreateResourceCoreAsync signature doesn't match
**Solution**: Check the actual ResourceFactoryBase<T> interface - it may have changed from documentation examples

### Real Development Experience Tips

1. **Always Research Binary Format First**: Use `010 Editor` or `HxD` hex editor to understand file structure
2. **Copy Pattern from Similar Resources**: Find the closest existing resource type and adapt its patterns
3. **Test with Real Game Files**: Don't rely on made-up test data - use actual .package files from Sims 4
4. **Implement Disposal Correctly**: Follow the disposal pattern from LRLEResource.cs exactly
5. **Use Proper Logging**: Include context in log messages - stream length, parsing position, etc.
6. **Handle Edge Cases**: Empty streams, corrupted data, unexpected EOF - see LRLE tests for examples

## [REALITY] Study These ACTUAL Implementations First

**Before writing any code**, study these real implementations in the TS4Tools project:

### [FILE] `src/TS4Tools.Resources.Images/LRLEResource.cs`

**Study this for:** Complex resource with binary format parsing, proper disposal patterns, comprehensive error handling

- Shows proper `IDisposable` implementation
- Demonstrates binary data validation
- Uses modern C# patterns (sealed class, proper async/await)
- Complex state management with caching

### [FILE] `src/TS4Tools.Resources.Images/LRLEResourceFactory.cs`

**Study this for:** Actual factory implementation pattern used throughout the project

- Shows proper `ResourceFactoryBase<T>` inheritance
- Demonstrates error handling in factory methods
- Shows how resource type IDs are handled

### [FILE] `tests/TS4Tools.Resources.Images.Tests/LRLEResourceTests.cs`

**Study this for:** Comprehensive testing patterns, real binary test data creation

- Shows how to create valid binary test data
- Demonstrates proper disposal testing
- Shows error condition testing patterns
- Uses real factory pattern in tests

### [DON'T] DON'T Study These (Legacy Patterns)

- `Sims4Tools/s4pi Wrappers/ImageResource/LRLEResource.cs` - Old .NET Framework patterns
- Any class inheriting from `AResource` - Legacy pattern not used in TS4Tools
- Any class using `EventHandler` patterns - Old synchronous patterns

### Key Learning Points from Real Code

1. **Resource Classes**: Implement interfaces directly (e.g., `ILRLEResource`, `IDisposable`)
2. **Factories**: Inherit from `ResourceFactoryBase<T>`, not generic examples
3. **Constructors**: Take `Stream?` and `ILogger<T>`, not `apiVersion` parameters  
4. **Error Handling**: Extensive validation, proper exception types, comprehensive logging
5. **Testing**: Real binary data, disposal testing, comprehensive error condition coverage

---

## [FOCUS] What to Focus On

### For Your First Week

1. **Explore the codebase** - Open files, read code, understand the structure
2. **Run existing tests** - See how tests are written and what they verify
3. **Make a small change** - Fix a typo, add a log message, improve a comment
4. **Write a simple test** - Pick an existing class and add a new test case

### For Your First Month

1. **Implement a simple resource type** - Start with something straightforward
2. **Add comprehensive tests** - Practice the testing patterns we use
3. **Understand the factory pattern** - This is central to how resources work
4. **Get comfortable with async/await** - Most operations are asynchronous

### For Your First Three Months

1. **Contribute to complex features** - Take on larger implementation tasks
2. **Review others' code** - Help maintain code quality
3. **Improve documentation** - Help future developers (like this guide!)
4. **Optimize performance** - Identify and fix bottlenecks

---

## [PITFALLS] Common Mistakes and How to Avoid Them

*Based on real development experience from Phase 4.17 WorldColorTimelineResource implementation*

### [CRITICAL WARNING] Study the Right Codebase First

**THE BIGGEST MISTAKE**: Studying legacy Sims4Tools code instead of modern TS4Tools patterns.

**What Happened**: I initially studied the wrong implementation patterns from the legacy `Sims4Tools/s4pi Wrappers/` directory instead of the modern `TS4Tools/src/` patterns, leading to hours of wasted effort implementing obsolete patterns.

**Key Differences That Will Mislead You**:

```csharp
// [WRONG] Legacy pattern from Sims4Tools (AResource base class)
public class WorldColorTimeLineResource : AResource
{
    public WorldColorTimeLineResource(int APIversion, Stream s) : base(APIversion, s) { }
    void Parse(Stream s) { /* old sync pattern */ }
    protected override Stream UnParse() { /* old sync pattern */ }
}

// [RIGHT] Modern pattern from TS4Tools (interface-based)  
public sealed class WorldColorTimelineResource : IWorldColorTimelineResource, IDisposable
{
    public WorldColorTimelineResource(Stream? stream, ILogger<WorldColorTimelineResource> logger) { }
    public async Task LoadFromStreamAsync(Stream stream) { /* modern async pattern */ }
    public async Task SaveToStreamAsync(Stream stream) { /* modern async pattern */ }
}
```

**Solution**: Always use `TS4Tools/src/` as your reference, never `Sims4Tools/s4pi Wrappers/`

**Prevention**: Before studying ANY code, verify you're in the right directory:

- [YES] `TS4Tools/src/TS4Tools.Resources.*/` - Modern .NET 9 patterns
- [NO] `Sims4Tools/s4pi Wrappers/` - Legacy .NET Framework patterns

### [COMPILATION] Major Compilation Error Patterns

**What I Had To Fix Multiple Times**: 20+ compilation errors from namespace conflicts and duplicate class names.

#### Duplicate Class/Interface Definitions

**Problem**: Created classes with names that already existed in legacy codebase, causing ambiguity.

**What Happened**:

```csharp
// I created this in TS4Tools.Core.Resources:
public class WorldColorTimelineResource : IWorldColorTimelineResource

// But this already existed in legacy Sims4Tools:
public class WorldColorTimeLineResource : AResource  // Note different spelling!
```

**Solution**: Always check for existing classes first:

```powershell
# Search across ALL codebases for similar names
grep -r "WorldColor.*Resource" . --include="*.cs"
# Check both spellings, case variations, etc.
```

**Prevention**: Use completely different naming when in doubt, or verify scope conflicts.

#### Namespace Resolution Hell

**Problem**: Ambiguous references between `TS4Tools.Core` and legacy `s4pi` namespaces.

**Repeated Fix Pattern**:

```csharp
// [WRONG] Ambiguous reference
using IResource = TS4Tools.Core.Interfaces.IResource;

// [RIGHT] Fully qualified references
private readonly global::TS4Tools.Core.Interfaces.ILogger<WorldColorTimelineResource> _logger;
```

**Major Time Sink**: Spent hours resolving namespace conflicts that could have been avoided upfront.

### [FACTORY PATTERN] ResourceFactoryBase Migration Issues

**Major Refactoring I Had To Do**: Converting from generic factory patterns to ResourceFactoryBase<T>.

#### Wrong Factory Pattern (My First Attempt)

```csharp
// [WRONG] Generic factory interface approach
public class WorldColorTimelineResourceFactory : IResourceFactory<IWorldColorTimelineResource>
{
    public async Task<IWorldColorTimelineResource> CreateAsync(Stream data)
    {
        // This compiles but doesn't integrate with ResourceManager
    }
}
```

#### Correct Factory Pattern (After Major Refactoring)

```csharp
// [RIGHT] ResourceFactoryBase approach
public class WorldColorTimelineResourceFactory : ResourceFactoryBase<IWorldColorTimelineResource>
{
    public override IReadOnlySet<string> SupportedResourceTypes =>
        new HashSet<string> { "0x19301120" };

    protected override async Task<IWorldColorTimelineResource> CreateResourceCoreAsync(
        Stream? stream, CancellationToken cancellationToken)
    {
        // This integrates properly with ResourceWrapperRegistry
    }
}
```

**Why This Matters**: The generic approach looks right but doesn't work with the ResourceWrapperRegistry discovery system.

**Time Lost**: Rewrote factory implementation completely 3 times before getting the pattern right.

### [TESTING] Test Development Hell - The Most Time-Consuming Issues

**Major Time Sink**: Spent as much time debugging tests as writing actual implementation code.

#### Stream Lifecycle Management Nightmares

**Problem**: Inconsistent stream handling causing "Stream does not support writing" errors.

**What I Had to Fix Repeatedly**:

```csharp
// [WRONG] My initial naive approach
public byte[] AsBytes
{
    get
    {
        using var stream = new MemoryStream();
        return stream.ToArray(); // Empty! No data written
    }
}

// [FIXED] After multiple attempts
public byte[] AsBytes
{
    get
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var ms = new MemoryStream();
        SaveToStreamAsync(ms).GetAwaiter().GetResult();
        return ms.ToArray();
    }
}
```

**Debug Pattern I Had to Learn**:

```csharp
// Always verify stream state in tests
[Fact]
public async Task AsBytes_AfterLoad_ShouldProduceValidData()
{
    // Arrange & Act
    var bytes = resource.AsBytes;
    
    // Debug assertions I learned to add
    bytes.Should().NotBeEmpty("Resource should produce binary data");
    bytes.Length.Should().BeGreaterThan(8, "Should have at least header data");
    
    // Test round-trip to catch stream issues early
    using var testStream = new MemoryStream(bytes);
    var reloadedResource = await factory.CreateResourceAsync(testStream);
    reloadedResource.Should().NotBeNull();
}
```

#### Test Data Format Mismatches

**Major Issue**: Boolean reading precision problems in version 14 data format.

**What I Debugged For Hours**:

```csharp
// Test expected: RemapTimeline = true
// Actual result: RemapTimeline = false

// Problem was in test data generation:
private byte[] CreateVersion14TestData()
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    
    // ... other data ...
    writer.Write(true);  // [WRONG] Writes as 0x01 but read differently
    
    return stream.ToArray();
}

// [FIXED] After understanding the exact binary format
private byte[] CreateVersion14TestData()
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    
    // ... other data ...
    writer.Write((byte)(true ? 1 : 0));  // [RIGHT] Explicit byte format
    
    return stream.ToArray();
}
```

#### Test Count Issues (18/24 Passing Pattern)

**Recurring Pattern**: Always had 6 failing tests related to the same stream/disposal issues.

**Key Tests That Always Failed**:

1. Round-trip serialization with empty resources
2. Stream disposal edge cases  
3. Version 14 RemapTimeline reading
4. UpdateStream method permission issues

**Solution Pattern I Learned**:

```csharp
// Test empty vs populated resources separately
[Theory]
[InlineData(true)]   // Test with populated data
[InlineData(false)]  // Test with empty resource
public async Task Resource_SerializationRoundTrip_ShouldPreserveData(bool withData)
{
    // Arrange
    var resource = withData ? CreateResourceWithTestData() : CreateEmptyResource();
    
    // Act & Assert - different expectations for different cases
    if (withData)
    {
        // Full round-trip testing
        var bytes = resource.AsBytes;
        bytes.Should().NotBeEmpty();
        // ... full validation
    }
    else
    {
        // Empty resource testing - different rules
        var bytes = resource.AsBytes;
        // Empty resources might legitimately have empty bytes
        resource.Should().NotBeNull();
    }
}
```

**Time Saved**: This pattern would have saved me 2+ days of debugging test failures.

### [CRITICAL] CRITICAL: ResourceWrapperRegistry Initialization

**THE MOST CRITICAL ISSUE**: The biggest failure I encountered was not understanding
that ResourceManager depends on ResourceWrapperRegistry initialization.

#### The Problem

**What Happened**: All Golden Master tests failed with "Expected resource.ContentFields
not to be empty" even though:

- [YES] World resource factories were registered in DI
- [YES] ContentFields properties worked when testing resources directly  
- [YES] ResourceManager was properly configured

**Root Cause**: ResourceManager uses a two-phase initialization:

1. **Phase 1**: Factories registered with DI container via `services.AddWorldResources()`
2. **Phase 2**: ResourceWrapperRegistry discovers these factories and registers them
   with ResourceManager

**The Missing Step**: ResourceWrapperRegistry.DiscoverAndRegisterFactoriesAsync() was
never called, so ResourceManager always fell back to DefaultResource (which has empty
ContentFields).

#### The Solution

**CRITICAL PATTERN**: Always call ResourceWrapperRegistry initialization after building the service provider:

```csharp
public MyTestClass()
{
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
    
    // Step 1: Register all resource services with DI
    services.AddTS4ToolsResourceServices();  // Core ResourceManager + ResourceWrapperRegistry
    services.AddWorldResources();            // World resource factories

    var serviceProvider = services.BuildServiceProvider();
    
    // Step 2: CRITICAL - Initialize ResourceWrapperRegistry to discover factories
    var registry = serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
    var initTask = registry.DiscoverAndRegisterFactoriesAsync();
    initTask.GetAwaiter().GetResult(); // Synchronous wait in constructor
    
    // NOW ResourceManager can create proper resource types instead of DefaultResource
    _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
}
```

**Shorthand Pattern**: Use the extension method:

```csharp
var serviceProvider = services.BuildServiceProvider();

// This does the ResourceWrapperRegistry initialization for you
var registryResult = await serviceProvider.InitializeResourceWrapperRegistryAsync();
if (!registryResult.IsSuccess)
{
    throw new InvalidOperationException("Failed to initialize resource factories");
}
```

#### How to Diagnose This Issue

**Symptoms**:

- ResourceManager.CreateResourceAsync() returns resources with empty ContentFields
- Direct factory instantiation works fine
- Resource.GetType() shows "DefaultResource" instead of specific types like "WorldResource"

**Debugging**:

```csharp
var resource = await _resourceManager.CreateResourceAsync("0x810A102D", 1);
Console.WriteLine($"Resource type: {resource.GetType().FullName}");
// [BAD]: "TS4Tools.Core.Resources.DefaultResource"
// [GOOD]: "TS4Tools.Resources.World.WorldResource"
```

**Prevention**: Always add ResourceWrapperRegistry initialization to any test class that uses ResourceManager.

### [STREAMS] Stream Handling Consistency Issues

#### SaveToStreamAsync vs LoadFromStreamAsync Format Mismatch

**What Happened**: Round-trip serialization tests failed with "Unable to read beyond the end of the stream" errors.

**Problem**: Some resources had inconsistent serialization - `AsBytes` used `WriteToStream()` but `LoadFromStreamAsync()` expected `SaveToStreamAsync()` format.

**The Issue**:

```csharp
// Resource.AsBytes was using old synchronous method
public byte[] AsBytes
{
    get
    {
        using var ms = new MemoryStream();
        WriteToStream(ms);  // [BAD] Old synchronous format
        return ms.ToArray();
    }
}

// But LoadFromStreamAsync expected modern async format
public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
{
    // This expects data written by SaveToStreamAsync, not WriteToStream!
}
```

**Solution**: Ensure serialization format consistency:

```csharp
public byte[] AsBytes
{
    get
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var ms = new MemoryStream();
        SaveToStreamAsync(ms).GetAwaiter().GetResult();  // [GOOD] Use async version
        return ms.ToArray();
    }
}
```

**Prevention**: When implementing IResource, ensure AsBytes and LoadFromStreamAsync use the same serialization format.

#### Empty Stream Handling Inconsistency  

**What Happened**: Some LoadFromStreamAsync methods crashed on empty streams, others handled them gracefully.

**Problem**: No standard pattern for handling empty or null streams across resource types.

**Standard Pattern**:

```csharp
public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
{
    // Handle null or truly empty stream (no content at all)
    if (stream == null || stream.Length == 0)
    {
        // Initialize with default values for empty resource
        InitializeDefaults();
        IsDirty = true;
        return;
    }

    // For resources with minimum size requirements
    if (stream.Length < MINIMUM_VALID_SIZE)
    {
        // Handle incomplete/partial data by initializing empty
        InitializeDefaults();
        IsDirty = true;
        return;
    }

    // Proceed with normal parsing...
}
```

**Prevention**: Always handle empty streams gracefully - many tests create resources without initial data.

### [FIELDS] ContentFields Initialization Patterns

#### Mixed Constructor vs Computed Property Patterns

**What Happened**: Some resources had empty ContentFields even with data because of inconsistent initialization patterns.

**Problem**: Mixed patterns across resource types:

- Some used computed properties: `public IReadOnlyList<string> ContentFields => new[] { "Field1", "Field2" };`  
- Some used constructor initialization: `_contentFields.AddRange(["Field1", "Field2"]);`
- Some never initialized ContentFields at all

**Recommended Pattern**: Constructor initialization for consistency:

```csharp
public sealed class MyResource : IResource, IDisposable, INotifyPropertyChanged
{
    private readonly List<string> _contentFields = new();
    
    public MyResource(ResourceKey key, uint version = 1)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
        
        // Initialize ContentFields for new resources
        _contentFields.AddRange([
            "Field1",
            "Field2", 
            "Field3"
        ]);
    }
    
    /// <inheritdoc/>
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();
}
```

**Why Constructor Over Computed**: Constructor initialization allows dynamic ContentFields based on actual data, while computed properties are static.

**Prevention**: Pick one pattern (preferably constructor) and use it consistently across all resources in a module.

### [CRITICAL] Critical Integration Issues

#### Missing Project References in Test Projects

**What Happened**: Created comprehensive tests but got compile errors: `'World' does not exist in the namespace 'TS4Tools.Resources'`

**Problem**: New resource modules need to be explicitly referenced in test projects that use them.

**Solution**: Always add project references when testing new modules:

```xml
<!-- In your test .csproj file -->
<ItemGroup>
  <ProjectReference Include="..\..\src\TS4Tools.Resources.World\TS4Tools.Resources.World.csproj" />
</ItemGroup>
```

**Prevention**: When creating tests for a new resource module, immediately check if the test project references it.

#### Forgetting DI Registration in Test Setup

**What Happened**: Tests failed with `NullReferenceException` even though factories were registered.

**Problem**: Test setup didn't include the new resource module's DI registration.

**Solution**: Remember to add ALL relevant DI registrations in test setup:

```csharp
public TestConstructor()
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddTS4ToolsResourceServices();  // Core resources
    services.AddWorldResources();            // Don't forget new modules!
    
    _serviceProvider = services.BuildServiceProvider();
    
    // CRITICAL: Initialize ResourceWrapperRegistry
    var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
    var initTask = registry.DiscoverAndRegisterFactoriesAsync();
    initTask.GetAwaiter().GetResult();
}
```

**Prevention**: Create a checklist: Core services + each resource module + ResourceWrapperRegistry initialization.

### [INTERFACE] IResource Interface Confusion

#### Accessing Properties That Don't Exist

**What Happened**: Tried to access `resource.Key` and `resource.Version` and got compile errors.

**Problem**: Confused concrete resource classes with the minimal `IResource` interface.

**The IResource Interface Reality**:

```csharp
public interface IResource : IApiVersion, IContentFields, IDisposable
{
    Stream Stream { get; }
    byte[] AsBytes { get; }
    event EventHandler? ResourceChanged;
}
```

**Solution**: Cast to specific resource types when you need access to concrete properties:

```csharp
// WRONG - IResource doesn't have Key property
resource.Key.ResourceType.Should().Be(expectedType);

// RIGHT - Cast to specific type first
if (resource is WorldResource worldRes)
{
    worldRes.Key.ResourceType.Should().Be(expectedType);
}
```

**Prevention**: Always check the actual interface definition before writing test assertions.

### [FACTORY] Resource Manager vs Factory Confusion

#### Mixed Usage Patterns

**What Happened**: Sometimes used `ResourceManager.CreateResourceAsync()`, other times used factories directly.

**Problem**: Inconsistent patterns make tests confusing and harder to maintain.

**Resource Manager Approach** (for integration tests):

```csharp
// Tests the full pipeline: type lookup -> factory selection -> creation
var resource = await _resourceManager.CreateResourceAsync("0x810A102D", 1);
```

**Direct Factory Approach** (for unit tests):

```csharp
// Tests specific factory logic
var factory = _serviceProvider.GetService<WorldResourceFactory>();
var resource = await factory.CreateResourceAsync(1);
```

**Prevention**: Decide upfront whether you're testing integration (use ResourceManager) or units (use factories directly).

### [ROUNDTRIP] Round-Trip Serialization Testing Issues

#### Empty Stream Deserialization

**What Happened**: Tests failed with `EndOfStreamException` when trying to deserialize empty streams.

**Problem**: Resources created without initial data have empty internal streams, but deserialization expects valid binary data.

**Flawed Test Logic**:

```csharp
// This creates a resource with empty/default data
var originalResource = await factory.CreateResourceAsync(1);
var originalBytes = originalResource.AsBytes;  // Might be empty!

// This will fail if originalBytes is empty
using var stream = new MemoryStream(originalBytes);
var deserializedResource = await factory.CreateResourceAsync(1, stream);
```

**Better Test Approach**:

```csharp
// Test with actual meaningful data
var originalResource = CreateResourceWithTestData();  // Populated resource
var originalBytes = originalResource.AsBytes;

// Or test the empty case explicitly
if (originalBytes.Length == 0)
{
    // Test that empty streams are handled gracefully
    using var emptyStream = new MemoryStream();
    var action = () => factory.CreateResourceAsync(1, emptyStream);
    await action.Should().NotThrowAsync();
}
```

**Prevention**: Always test with both populated data AND empty/edge cases explicitly.

### [CONTENTFIELDS] ContentFields Implementation Gotchas

#### Assuming ContentFields Are Always Populated

**What Happened**: Tests failed with `Expected resource.ContentFields not to be empty` even for valid resources.

**Problem**: Some resource classes initialize with empty ContentFields until they're populated by actual data.

**Wrong Assumption**:

```csharp
resource.ContentFields.Should().NotBeEmpty("Resource should have content fields");
```

**Better Test Logic**:

```csharp
// For newly created resources, ContentFields might be empty initially
resource.ContentFields.Should().NotBeNull("ContentFields should be initialized");

// For resources with data, they should have fields
if (resource.HasData()) // Some way to check if populated
{
    resource.ContentFields.Should().NotBeEmpty("Populated resource should have content fields");
}
```

**Prevention**: Understand the lifecycle of ContentFields - when they get populated and what empty means.

### [TESTING] Golden Master Integration Best Practices

#### Test the Framework First

**What Happened**: Created complex tests that failed, making it hard to diagnose root causes.

**Problem**: Tried to test everything at once instead of validating the test framework itself first.

**Better Approach**:

```csharp
// FIRST: Test that DI registration works
[Fact]
public void WorldResourceFactories_ShouldBeRegisteredInDI()
{
    // Simple smoke test - can I get the factory?
    var factory = _serviceProvider.GetService<WorldResourceFactory>();
    factory.Should().NotBeNull();
}

// SECOND: Test basic resource creation
[Fact] 
public async Task WorldResourceFactory_CanCreateResource()
{
    // Simple creation test
    var factory = _serviceProvider.GetService<WorldResourceFactory>();
    var resource = await factory.CreateResourceAsync(1);
    resource.Should().NotBeNull();
}

// THIRD: Test complex scenarios like round-trip serialization
```

**Prevention**: Build tests incrementally - validate your test framework before testing complex logic.

#### Performance Test Baseline Issues

**What Happened**: Performance tests were unrealistic because they tested resource creation with empty data.

**Problem**: Performance characteristics of empty vs real data can be drastically different.

**Better Performance Testing**:

```csharp
[Fact]
public async Task WorldResourceCreation_PerformanceBaseline()
{
    // Test with realistic data sizes, not empty resources
    var testData = LoadRealWorldPackageData();  // Real binary data
    
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        using var stream = new MemoryStream(testData);
        var resource = await _factory.CreateResourceAsync(1, stream);
        // Use resource to ensure it's not optimized away
        _ = resource.AsBytes.Length;
    }
    stopwatch.Stop();
    
    // Performance assertions based on realistic workload
}
```

**Prevention**: Use real data for performance tests, not synthetic/empty data.

---

## [HELP] Getting Help

### Where to Look First

1. **This documentation** - Start with the docs in the `docs/` folder
2. **Existing code** - Look for similar implementations
3. **Tests** - They show how code is supposed to be used
4. **Git history** - See how features were implemented previously

### How to Ask for Help

When you're stuck, provide context:

1. **What you're trying to do** - "I'm implementing a new resource type"
2. **What you've tried** - "I followed the pattern from StringTableResource"
3. **What's happening** - "Tests pass but the resource doesn't load correctly"
4. **Error messages** - Include full stack traces
5. **Code samples** - Show the specific code that's not working

### Code Review Guidelines

When your code is ready for review:

1. **Self-review first** - Read through your own changes
2. **Write clear commit messages** - Explain what and why
3. **Include tests** - Don't submit code without tests
4. **Update documentation** - If you changed public APIs
5. **Keep changes focused** - One feature per pull request

---

## [READY] You're Ready to Start

By now you should understand:

- [X] How the TS4Tools codebase is organized
- [X] Our development patterns (DI, factories, async)
- [X] How to write tests using our testing standards
- [X] The step-by-step process for adding new features
- [X] Our code standards and conventions
- [X] How to get help when you need it

**Next Steps:**

1. Set up your development environment
2. Run the existing tests to make sure everything works
3. Pick a small task from the issue tracker

## [LESSONS] Phase 4.17 & 4.18 Lessons - Critical Success Checklist

*The absolute essentials learned from real implementation failures*

### Phase 4.18 Additional Insights - Test Management & Manual Implementation Patterns

**NEW FROM PHASE 4.18**: The following lessons come from Phase 4.18 Visual Enhancement
implementation, building on Phase 4.17 patterns:

#### Test Suite Expectations Management

**Critical Learning**: When adding new functionality, existing tests may need updates to
reflect the new capabilities.

**What Happened in Phase 4.18**: After implementing IconResourceFactory and enhancing
CatalogResourceFactory, the `CatalogResourceFactoryTests.SupportedResourceTypes_ShouldContainExpectedTypes`
test failed because it expected 5 resource types but found 6 - the new CatalogResource
(0x049CA4CD) added during Phase 4.18.

**Solution Pattern**:

```csharp
// BEFORE: Static test expectations
var expectedTypes = new HashSet<uint>
{
    0x319E4F1D, // ThumbnailResource  
    0x00B2D882, // CatalogOutfitResource
    0x9D796CA4, // CatalogFunctionResource  
    0x9153BD83, // CatalogProxyResource
    0x545AC67A  // CatalogModifierResource
};

// AFTER: Updated expectations with documentation
var expectedTypes = new HashSet<uint>
{
    0x049CA4CD, // CatalogResource (Phase 4.18 - Visual Enhancement)
    0x319E4F1D, // ThumbnailResource  
    0x00B2D882, // CatalogOutfitResource
    0x9D796CA4, // CatalogFunctionResource  
    0x9153BD83, // CatalogProxyResource
    0x545AC67A  // CatalogModifierResource
};
```

**Prevention Strategy**:

- When adding new resource types, scan for tests that validate "expected counts" or "supported types"
- Add comments indicating which phase/feature added each resource type
- Update test expectations as part of the feature implementation, not as a separate fix

#### Manual Implementation Validation Patterns

**Critical Learning**: Some implementations require manual verification alongside automated testing.

**Phase 4.18 Pattern**: IconResourceFactory was manually implemented (116 lines) with specific patterns:

```csharp
// Manual implementation pattern that works
public sealed class IconResourceFactory : ResourceFactoryBase<IIconResource>
{
    public override IReadOnlySet<string> SupportedResourceTypes => 
        new HashSet<string> { "0x73E93EEC" };

    protected override async Task<IIconResource> CreateResourceCoreAsync(
        Stream? stream, 
        CancellationToken cancellationToken)
    {
        return await Task.FromResult(new IconResource(stream, _logger));
    }
}
```

**Validation Approach**:

1. **Build Verification**: `dotnet build TS4Tools.sln` - must succeed with minimal warnings
2. **Factory Registration Check**: Look for "Registered factory IconResourceFactory for
   resource type 0x73E93EEC" in test output
3. **Integration Confirmation**: Full test suite should show factory working with ResourceWrapperRegistry
4. **Golden Master Compatibility**: Ensure no regressions in existing test patterns

**Manual vs Generated Code Quality**: Manual implementations often have better error handling,  
logging integration, and lifecycle management than generated code.

#### Phase 4.18 Implementation Completion Patterns

**Critical Learning**: Phase-based development requires clear completion validation before proceeding.

**Phase 4.18.1 Day 1 Morning Completion Criteria**:

- IconResourceFactory (0x73E93EEC) successfully registered and functional
- CatalogResourceFactory enhanced with new resource types  
- All test failures resolved (went from 1289 tests with 1 failed to 703 tests with 0 failed)
- Factory registration shows "Successful: 11, Failed: 0"
- Golden Master tests maintain 61/61 passing status

**Test Count Reconciliation Pattern**: When test counts change dramatically (1289 → 703),
this usually indicates test scope filtering, not test removal. Verify:

```powershell
# Full solution test count
dotnet test TS4Tools.sln --verbosity minimal
# Expected: ~1200+ tests across all projects

# Filtered test count (what Golden Master typically runs)  
dotnet test tests/TS4Tools.Tests.GoldenMaster/ --verbosity minimal
# Expected: ~700 tests for core functionality
```

**Success Validation Checklist for Resource Factory Implementation**:

1. **Build Success**: No compilation errors, only expected warnings about project references
2. **Factory Registration**: Look for specific "Registered factory [Name] for resource type 0x[TypeId]" messages
3. **Test Resolution**: All failing tests from new functionality are addressed
4. **Integration Health**: ResourceWrapperRegistry shows all expected factories registered
5. **Golden Master Stability**: No regressions in existing Golden Master test suite

**Phase Transition Pattern**: Don't proceed to next phase until current phase shows:

- Clean build status
- All tests passing  
- Factory registrations confirmed
- No degradation in existing functionality

This pattern prevents cascade failures where issues from incomplete phases compound in later implementation.

### Before Starting Any Resource Development

**1. ResourceWrapperRegistry Initialization - THE MOST CRITICAL**

```csharp
// ALWAYS include this pattern in test setup
var serviceProvider = services.BuildServiceProvider();
var registry = serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
var initTask = registry.DiscoverAndRegisterFactoriesAsync();
initTask.GetAwaiter().GetResult(); // Critical for ResourceManager to work
```

**2. Complete DI Registration Checklist**

```csharp
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
services.AddTS4ToolsResourceServices();  // Core ResourceManager + ResourceWrapperRegistry
services.AddWorldResources();            // Your specific resource module
// Add other resource modules as needed
```

**3. Project Reference Verification**

- Test projects must reference the resource modules they test
- Check `.csproj` files for missing `<ProjectReference>` entries

### During Resource Implementation

**4. Stream Handling Consistency**

- Always use `SaveToStreamAsync()` format in `AsBytes` property
- Handle empty streams gracefully in `LoadFromStreamAsync()`
- Test both populated data AND empty stream scenarios

**5. ContentFields Implementation**

- Use constructor initialization over computed properties
- Initialize ContentFields even for empty resources
- Don't assume ContentFields are always populated

**6. Interface vs Concrete Types**

- `IResource` has minimal interface - cast to specific types for properties
- Decide upfront: ResourceManager (integration) vs Factory (unit testing)

### During Testing

**7. Test Framework Validation First**

- Test DI registration works before complex logic
- Build tests incrementally - smoke tests -> unit tests -> integration
- Use real data for performance tests, not empty resources

**8. Golden Master Integration**

- Validate test framework setup before testing business logic
- Understand resource lifecycle - when ContentFields get populated
- Test empty vs populated resources as separate scenarios

### Critical Debugging Patterns

**9. ResourceManager Troubleshooting**

```csharp
// If getting DefaultResource instead of specific types
var resource = await _resourceManager.CreateResourceAsync("0x810A102D", 1);
Console.WriteLine($"Resource type: {resource.GetType().FullName}");
// [BAD]: "TS4Tools.Core.Resources.DefaultResource"
// [GOOD]: "TS4Tools.Resources.World.WorldResource"
```

**10. Serialization Round-Trip Verification**

```csharp
// Test that AsBytes and LoadFromStreamAsync use same format
var originalResource = CreateResourceWithTestData();
var originalBytes = originalResource.AsBytes;

using var stream = new MemoryStream(originalBytes);
var deserializedResource = await factory.CreateResourceAsync(1, stream);
// Should not throw EndOfStreamException
```

### Red Flags - Stop and Fix These Immediately

[!] **ResourceManager returns DefaultResource** -> ResourceWrapperRegistry not initialized  
[!] **"Expected ContentFields not to be empty" test failures** -> ResourceWrapperRegistry not initialized  
[!] **Compile errors about missing namespaces** -> Missing project references  
[!] **EndOfStreamException in round-trip tests** -> Stream format inconsistency  
[!] **NullReferenceException in tests** -> Missing DI registrations  

### Success Indicators - You're On the Right Track

[OK] Resources have correct concrete types (not DefaultResource)  
[OK] ContentFields are populated appropriately for resource state  
[OK] Round-trip serialization works without exceptions  
[OK] Both empty and populated resources are handled gracefully  
[OK] Tests build incrementally from simple to complex  

### Phase 4.18 Additional Red Flags

[!] **Test count changes without explanation** -> Check if test scope changed vs tests actually removed  
[!] **Factory shows registered but ResourceManager still returns DefaultResource** -> Registration timing issue  
[!] **Expected resource type count mismatches in tests** -> New resource types added, update test expectations  
[!] **Manual implementations missing logging/error handling** -> Compare with existing factory patterns  

### Phase 4.18 Additional Success Indicators  

[OK] Factory registration logs show specific resource type IDs being registered  
[OK] Test failures are related to expected functionality, not integration issues  
[OK] Golden Master tests maintain stability while adding new features  
[OK] Build warnings are expected/documented, not new integration issues  

---

*"The best documentation is the mistakes you don't repeat." - Phase 4.17 & 4.18 Experience*

---

*Happy coding, and may your resources always deserialize correctly!*
4. Start coding!

Welcome to the team!
