# TS4Tools Getting Started Guide

## Introduction

TS4Tools is a modern, cross-platform toolkit for working with The Sims 4 package files (.package). It provides a comprehensive set of libraries and tools for reading, writing, and manipulating Sims 4 game resources.

## Key Features

- **Modern .NET 9**: Built on the latest .NET runtime with modern C# features
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **High Performance**: Optimized with Span<T>, Memory<T>, and async patterns
- **Type Safe**: Full nullable reference type support
- **Comprehensive Testing**: 374+ tests with 95%+ code coverage
- **Dependency Injection**: Modern service-based architecture

## Architecture Overview

TS4Tools is organized into several core packages:

### Core Packages

- **TS4Tools.Core.System**: Fundamental utilities, collections, and extensions
- **TS4Tools.Core.Interfaces**: Base interfaces and contracts for all components
- **TS4Tools.Core.Settings**: Modern IOptions-based configuration system
- **TS4Tools.Core.Package**: DBPF package file operations (read/write/modify)
- **TS4Tools.Core.Resources**: Resource factory and management system
- **TS4Tools.Core.DependencyInjection**: Service registration and DI orchestration

### Extension Packages

- **TS4Tools.Extensions**: Service-based extension system with resource type registry
- **TS4Tools.Resources.Common**: Shared utilities, ViewModels, and data converters

## Quick Start

### 1. Installation

Add the TS4Tools packages to your project:

```xml
<PackageReference Include="TS4Tools.Core.Package" Version="1.0.0" />
<PackageReference Include="TS4Tools.Core.DependencyInjection" Version="1.0.0" />
```

### 2. Basic Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;

// Set up dependency injection
var builder = Host.CreateApplicationBuilder();
builder.Services.AddTS4ToolsCore();

var host = builder.Build();

// Get the package factory
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();

// Load a package file
var package = await packageFactory.LoadFromFileAsync("path/to/your/file.package");

Console.WriteLine($"Package loaded with {package.ResourceIndex.Count} resources");

// Iterate through resources
foreach (var entry in package.ResourceIndex)
{
    Console.WriteLine($"Resource: {entry.Key} (Size: {entry.Value.FileSize} bytes)");
}
```

### 3. Creating a New Package

```csharp
// Create a new empty package
var newPackage = await packageFactory.CreateEmptyPackageAsync();

// Save the package
await newPackage.SaveAsAsync("path/to/new/file.package");
```

## Configuration

TS4Tools uses the modern IOptions pattern for configuration. Create an `appsettings.json` file:

```json
{
  "TS4Tools": {
    "EnableLogging": true,
    "LogLevel": "Information",
    "TempDirectory": "temp",
    "MaxCacheSize": 1000,
    "EnableResourceCaching": true,
    "ResourceCacheTimeout": "00:30:00",
    "DefaultEncoding": "UTF-8"
  }
}
```

Register configuration in your application:

```csharp
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Services.AddTS4ToolsCore(builder.Configuration);
```

## Examples

See the `examples/` directory for complete working examples:

- **BasicPackageReader**: Simple console application for reading package files
- **PackageAnalyzer**: Advanced analysis tool with detailed reporting
- **ResourceExtractor**: Bulk resource extraction utility
- **PackageCreator**: Creating new packages with custom resources

## Next Steps

- Read the [API Reference](api-reference.md) for detailed documentation
- Explore [Advanced Features](advanced-features.md) for complex scenarios
- Check out [Performance Guide](performance-guide.md) for optimization tips
- Review [Migration Guide](migration-guide.md) if upgrading from Sims4Tools

## Support

- **Documentation**: See the `docs/` folder for comprehensive guides
- **Examples**: Check the `examples/` folder for working code samples
- **Issues**: Report issues on GitHub
- **Contributing**: See [Contributing Guide](contributing.md)
