# Examples Directory

This directory contains working examples demonstrating how to use TS4Tools for various scenarios.

## Available Examples

### 1. BasicPackageReader
**Purpose**: Demonstrates basic package reading and analysis functionality.

**Features**:
- Load and inspect package files
- Display package statistics and resource information
- Show resource type analysis with friendly names
- Preview resource data with hex dumps

**Usage**:
```bash
cd BasicPackageReader
dotnet run "path/to/your/package.package"
```

[View README](BasicPackageReader/README.md)

### 2. PackageCreator
**Purpose**: Shows how to create new package files from scratch.

**Features**:
- Create empty packages
- Add custom text, binary, and metadata resources
- Save packages to disk
- Verify created packages

**Usage**:
```bash
cd PackageCreator
dotnet run "path/to/output/new-package.package"
```

[View README](PackageCreator/README.md)

## Building and Running Examples

### Prerequisites
- .NET 9 SDK
- TS4Tools solution built successfully

### Build All Examples
From the TS4Tools root directory:
```bash
dotnet build examples/BasicPackageReader
dotnet build examples/PackageCreator
```

### Run Examples
Each example is a standalone console application:
```bash
# Basic Package Reader
cd examples/BasicPackageReader
dotnet run -- "C:\path\to\your\file.package"

# Package Creator
cd examples/PackageCreator
dotnet run -- "C:\path\to\output\new-file.package"
```

## Common Patterns Demonstrated

### 1. Dependency Injection Setup
All examples show proper DI configuration:
```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTS4ToolsCore();
builder.Services.AddTS4ToolsExtensions();
builder.Logging.AddConsole();
```

### 2. Service Usage
Examples demonstrate proper service usage patterns:
```csharp
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();
```

### 3. Resource Management
Proper disposal and resource management:
```csharp
using var package = await packageFactory.LoadFromFileAsync(packagePath);
// Package automatically disposed when scope exits
```

### 4. Error Handling
Comprehensive error handling with logging:
```csharp
try
{
    // Package operations
}
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed");
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Creating Your Own Examples

To create a new example project:

1. **Create project directory**:
   ```bash
   mkdir examples/YourExample
   cd examples/YourExample
   ```

2. **Create project file**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFramework>net9.0</TargetFramework>
       <Nullable>enable</Nullable>
     </PropertyGroup>
     
     <ItemGroup>
       <ProjectReference Include="..\..\src\TS4Tools.Core.Package\TS4Tools.Core.Package.csproj" />
       <ProjectReference Include="..\..\src\TS4Tools.Core.DependencyInjection\TS4Tools.Core.DependencyInjection.csproj" />
     </ItemGroup>
     
     <ItemGroup>
       <PackageReference Include="Microsoft.Extensions.Hosting" />
       <PackageReference Include="Microsoft.Extensions.Logging.Console" />
     </ItemGroup>
   </Project>
   ```

3. **Create Program.cs**:
   ```csharp
   using Microsoft.Extensions.DependencyInjection;
   using Microsoft.Extensions.Hosting;
   using TS4Tools.Core.DependencyInjection;
   
   var builder = Host.CreateApplicationBuilder(args);
   builder.Services.AddTS4ToolsCore();
   builder.Logging.AddConsole();
   
   var host = builder.Build();
   
   // Your example code here
   ```

4. **Add README.md** with usage instructions and explanation.

## Testing Examples

Examples can be tested with sample package files:

### Creating Test Data
Use the PackageCreator example to generate test packages:
```bash
cd examples/PackageCreator
dotnet run -- "test-data/sample.package"
```

### Validation
Use the BasicPackageReader to validate created packages:
```bash
cd examples/BasicPackageReader
dotnet run -- "test-data/sample.package"
```

## Integration with Documentation

These examples are referenced in the main documentation:
- [Getting Started Guide](../docs/getting-started.md) - Basic usage patterns
- [API Reference](../docs/api-reference.md) - Detailed API examples  
- [Advanced Features](../docs/advanced-features.md) - Complex scenarios

## Contributing Examples

When contributing new examples:

1. **Follow naming conventions**: Use descriptive, PascalCase names
2. **Include comprehensive README**: Explain purpose, usage, and key concepts
3. **Add error handling**: Show proper exception handling patterns
4. **Document dependencies**: List all required packages and references
5. **Test thoroughly**: Ensure examples work with various inputs
6. **Update this README**: Add your example to the list above

## Support

If you have questions about the examples:
- Check the individual README files in each example directory
- Review the main [Getting Started Guide](../docs/getting-started.md)
- See the [API Reference](../docs/api-reference.md) for detailed documentation
