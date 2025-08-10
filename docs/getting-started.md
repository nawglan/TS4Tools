# TS4Tools Getting Started Guide

## Introduction

TS4Tools is a modern, cross-platform .NET library for working with The Sims 4 package files (.package).  
Whether you're building mod tools, content analyzers, or custom Sims 4 utilities, TS4Tools provides a  
comprehensive API for reading, writing, and manipulating game resources.

## Why Choose TS4Tools?

- **🚀 Modern .NET 9**: Built with the latest C# features and performance optimizations
- **🌍 Cross-Platform**: Works seamlessly on Windows, macOS, and Linux
- **⚡ High Performance**: Optimized with `Span<T>`, `Memory<T>`, and async patterns
- **🛡️ Type Safe**: Full nullable reference types and comprehensive error handling
- **📦 NuGet Ready**: Easy installation via package manager
- **🎯 Focused API**: Clean, intuitive interfaces designed for real-world use cases

## What You Can Build

TS4Tools enables you to create powerful applications for The Sims 4 community:

### **Content Analysis Tools**

- Package file validators and analyzers  
- Resource dependency trackers
- Mod compatibility checkers
- Performance impact analyzers

### **Content Creation Utilities**

- Custom package builders
- Resource extractors and converters
- Batch processing tools
- Template generators

### **Mod Management Systems**

- Package organizers and catalogs
- Conflict detection systems
- Installation managers
- Backup and restore tools

### **Research and Development**

- Game data mining tools
- Format documentation generators
- Educational content viewers
- Community resource databases

## Installation

### Prerequisites

- **.NET 9 SDK** or later ([Download here](https://dotnet.microsoft.com/download/dotnet/9.0))
- **C# IDE** (Visual Studio, VS Code, or JetBrains Rider)
- **Basic C# knowledge** for development

### Install via NuGet

Add TS4Tools to your project using your preferred method:

#### Package Manager Console

```powershell
Install-Package TS4Tools.Core.Package
Install-Package TS4Tools.Core.DependencyInjection
```

#### .NET CLI

```bash
dotnet add package TS4Tools.Core.Package
dotnet add package TS4Tools.Core.DependencyInjection
```

#### PackageReference (in your .csproj file)

```xml
<PackageReference Include="TS4Tools.Core.Package" Version="1.0.0" />
<PackageReference Include="TS4Tools.Core.DependencyInjection" Version="1.0.0" />
```

## Quick Start Examples

### Basic Package Reading

Here's how to get started with reading Sims 4 package files:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;

// Set up dependency injection (one-time setup)
var builder = Host.CreateApplicationBuilder();
builder.Services.AddTS4ToolsCore();
var host = builder.Build();

// Get the package factory service
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();

// Load a package file
var package = await packageFactory.LoadFromFileAsync(@"C:\MyMods\MyMod.package");

Console.WriteLine($"Package loaded successfully!");
Console.WriteLine($"Contains {package.ResourceIndex.Count} resources");

// List all resources in the package
foreach (var entry in package.ResourceIndex)
{
    Console.WriteLine($"Resource Type: 0x{entry.Key.ResourceType:X8}");
    Console.WriteLine($"Resource Group: 0x{entry.Key.ResourceGroup:X8}");
    Console.WriteLine($"Instance ID: 0x{entry.Key.InstanceId:X16}");
    Console.WriteLine($"Size: {entry.Value.FileSize:N0} bytes");
    Console.WriteLine("---");
}
```

### Working with Resources

```csharp
// Find a specific resource by type
var stringResources = package.ResourceIndex.Keys
    .Where(key => key.ResourceType == 0x220557DA) // String Table resources
    .ToList();

if (stringResources.Any())
{
    Console.WriteLine($"Found {stringResources.Count} string table resources");
}

// Get resource data
foreach (var resourceKey in stringResources.Take(5)) // First 5 resources
{
    var resourceEntry = package.ResourceIndex[resourceKey];
    var resourceData = await package.GetResourceDataAsync(resourceKey);
    
    Console.WriteLine($"Resource {resourceKey.InstanceId:X16}: {resourceData.Length} bytes");
}
```

### Creating New Packages

```csharp
// Create a new empty package
var newPackage = await packageFactory.CreateEmptyPackageAsync();

// Add metadata (optional but recommended)
newPackage.Header.CreatedBy = "MyToolName v1.0";
newPackage.Header.CreationTime = DateTime.UtcNow;

// TODO: Add resources to the package (see Advanced Examples)

// Save the package
await newPackage.SaveAsAsync(@"C:\MyOutput\NewPackage.package");

Console.WriteLine("New package created successfully!");
```

### Error Handling Best Practices

```csharp
try
{
    var package = await packageFactory.LoadFromFileAsync(packagePath);
    // Process package...
}
catch (FileNotFoundException)
{
    Console.WriteLine($"Package file not found: {packagePath}");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine($"Access denied to file: {packagePath}");
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"Invalid package format: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Configuration (Optional)

TS4Tools works great with default settings, but you can customize behavior as needed.

### Basic Configuration

Create an `appsettings.json` file in your application root:

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

### Using Configuration in Your Application

```csharp
var builder = Host.CreateApplicationBuilder();

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

// Apply configuration to TS4Tools
builder.Services.AddTS4ToolsCore(builder.Configuration);

var host = builder.Build();
```

### Configuration Options Explained

- **EnableLogging**: Enable/disable internal logging (default: true)
- **LogLevel**: Minimum log level (Debug, Information, Warning, Error)
- **TempDirectory**: Temporary file storage location (default: system temp)
- **MaxCacheSize**: Maximum number of cached resources (default: 1000)  
- **EnableResourceCaching**: Cache frequently accessed resources (default: true)
- **ResourceCacheTimeout**: How long to cache resources (default: 30 minutes)
- **DefaultEncoding**: Text encoding for string resources (default: UTF-8)

## Practical Examples

### Example 1: Mod Analyzer Tool

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;

class ModAnalyzer
{
    private readonly IPackageFactory _packageFactory;
    
    public ModAnalyzer(IPackageFactory packageFactory)
    {
        _packageFactory = packageFactory;
    }
    
    public async Task AnalyzeMod(string packagePath)
    {
        var package = await _packageFactory.LoadFromFileAsync(packagePath);
        
        Console.WriteLine($"=== Analyzing: {Path.GetFileName(packagePath)} ===");
        Console.WriteLine($"Total Resources: {package.ResourceIndex.Count}");
        
        // Group resources by type
        var resourcesByType = package.ResourceIndex.Keys
            .GroupBy(key => key.ResourceType)
            .OrderBy(group => group.Key)
            .ToList();
        
        foreach (var group in resourcesByType)
        {
            var typeName = GetResourceTypeName(group.Key);
            Console.WriteLine($"  {typeName}: {group.Count()} resources");
        }
    }
    
    private static string GetResourceTypeName(uint resourceType)
    {
        return resourceType switch
        {
            0x220557DA => "String Tables",
            0x00B2D882 => "Images (BMP)",
            0x2F7D0004 => "Images (PNG)",
            0x319E4F1D => "Catalog Resources",
            0x48C28979 => "Catalog Object",
            _ => $"Unknown (0x{resourceType:X8})"
        };
    }
}

// Usage
var builder = Host.CreateApplicationBuilder();
builder.Services.AddTS4ToolsCore();
builder.Services.AddTransient<ModAnalyzer>();

var host = builder.Build();
var analyzer = host.Services.GetRequiredService<ModAnalyzer>();

await analyzer.AnalyzeMod(@"C:\MyMods\MyMod.package");
```

### Example 2: Batch Resource Extractor

```csharp
class ResourceExtractor
{
    private readonly IPackageFactory _packageFactory;
    
    public ResourceExtractor(IPackageFactory packageFactory)
    {
        _packageFactory = packageFactory;
    }
    
    public async Task ExtractAllImages(string packagePath, string outputDirectory)
    {
        var package = await _packageFactory.LoadFromFileAsync(packagePath);
        
        // Find all image resources
        var imageResources = package.ResourceIndex.Keys
            .Where(key => IsImageResource(key.ResourceType))
            .ToList();
        
        Console.WriteLine($"Found {imageResources.Count} image resources");
        
        Directory.CreateDirectory(outputDirectory);
        
        foreach (var resourceKey in imageResources)
        {
            try
            {
                var resourceData = await package.GetResourceDataAsync(resourceKey);
                var extension = GetImageExtension(resourceKey.ResourceType);
                var filename = $"{resourceKey.InstanceId:X16}{extension}";
                var outputPath = Path.Combine(outputDirectory, filename);
                
                await File.WriteAllBytesAsync(outputPath, resourceData);
                Console.WriteLine($"Extracted: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract {resourceKey.InstanceId:X16}: {ex.Message}");
            }
        }
    }
    
    private static bool IsImageResource(uint resourceType)
    {
        return resourceType switch
        {
            0x00B2D882 => true, // BMP
            0x2F7D0004 => true, // PNG
            0x2F7D0005 => true, // TGA
            0x2F7D0002 => true, // JPEG
            0x2F7D0003 => true, // DDS
            _ => false
        };
    }
    
    private static string GetImageExtension(uint resourceType)
    {
        return resourceType switch
        {
            0x00B2D882 => ".bmp",
            0x2F7D0004 => ".png",
            0x2F7D0005 => ".tga",
            0x2F7D0002 => ".jpg",
            0x2F7D0003 => ".dds",
            _ => ".bin"
        };
    }
}
```

## Common Resource Types

When working with Sims 4 packages, you'll encounter these resource types frequently:

| Resource Type | Hex Value    | Description | Common Use Cases |
|---------------|--------------|-------------|------------------|
| String Tables | `0x220557DA` | Localized text strings | UI text, names, descriptions |
| Catalog Objects | `0x48C28979` | Object definitions | Furniture, decorations |
| Catalog CAS | `0x319E4F1D` | Create-A-Sim items | Clothing, hair, accessories |
| BMP Images | `0x00B2D882` | Bitmap images | Icons, thumbnails |
| PNG Images | `0x2F7D0004` | PNG images | UI elements, textures |
| DDS Images | `0x2F7D0003` | DirectDraw Surface | Game textures |
| TGA Images | `0x2F7D0005` | Targa images | High-quality assets |

## Performance Tips

### Memory Management

```csharp
// ✅ Good: Dispose packages when done
using var package = await packageFactory.LoadFromFileAsync(path);
// Package automatically disposed here

// ✅ Good: Process resources in batches for large packages
var resources = package.ResourceIndex.Keys.Take(100);
foreach (var resource in resources)
{
    // Process resource
}
```

### Async Best Practices

```csharp
// ✅ Good: Use ConfigureAwait(false) in libraries
var package = await packageFactory.LoadFromFileAsync(path).ConfigureAwait(false);

// ✅ Good: Process multiple packages concurrently
var packages = await Task.WhenAll(
    packagePaths.Select(path => packageFactory.LoadFromFileAsync(path))
);
```

## Troubleshooting

### Common Issues

#### "FileNotFoundException" when loading packages

- Verify the file path is correct and the file exists
- Check file permissions (read access required)
- Ensure the file is not locked by another application

#### "InvalidDataException" when reading packages

- The file may be corrupted or not a valid Sims 4 package
- Try opening the file in the official Sims 4 tools first
- Check if the file is actually a different format (zip, rar, etc.)

#### "OutOfMemoryException" with large packages

- Process resources in smaller batches
- Use streaming APIs when available
- Consider increasing application memory limits

#### Resources appear empty or corrupted

- Some resources may be compressed or encoded
- Use appropriate resource-specific libraries for parsing
- Check if the resource type is supported

## Complete Example Application

Here's a complete console application that demonstrates the key concepts:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddTS4ToolsCore();
        builder.Logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise
        
        var host = builder.Build();
        var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
        
        // Get package path from user
        Console.WriteLine("Enter the path to a Sims 4 package file:");
        var packagePath = Console.ReadLine();
        
        if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
        {
            Console.WriteLine("Invalid file path!");
            return;
        }
        
        try
        {
            // Load and analyze the package
            Console.WriteLine("Loading package...");
            using var package = await packageFactory.LoadFromFileAsync(packagePath);
            
            Console.WriteLine($"\n✅ Successfully loaded: {Path.GetFileName(packagePath)}");
            Console.WriteLine($"📦 Total resources: {package.ResourceIndex.Count:N0}");
            Console.WriteLine($"💾 File size: {new FileInfo(packagePath).Length:N0} bytes");
            
            // Show resource breakdown
            var breakdown = package.ResourceIndex.Keys
                .GroupBy(key => key.ResourceType)
                .OrderByDescending(group => group.Count())
                .Take(10)
                .ToList();
            
            Console.WriteLine("\n🔍 Top 10 Resource Types:");
            foreach (var group in breakdown)
            {
                Console.WriteLine($"  0x{group.Key:X8}: {group.Count():N0} resources");
            }
            
            Console.WriteLine("\n✨ Analysis complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
```

## Next Steps

### Learning More

- **[API Reference](api-reference.md)** - Comprehensive API documentation
- **[Advanced Features](advanced-features.md)** - Complex scenarios and advanced usage
- **[Performance Guide](performance-guide.md)** - Optimization tips and best practices
- **[Examples Repository](../examples/)** - Complete working code samples

### Building Your Application

1. **Start Simple**: Begin with basic package reading and resource enumeration
2. **Add Features**: Gradually add resource extraction, analysis, or creation features  
3. **Optimize**: Use the performance guide to optimize for your specific use case
4. **Test Thoroughly**: Test with various package types and sizes from the community

### Community and Support

- **GitHub Issues**: Report bugs, request features, or ask questions
- **Documentation**: Comprehensive guides in the `docs/` folder
- **Examples**: Real-world code samples in the `examples/` folder
- **API Reference**: Detailed method and class documentation

### Contributing

If you'd like to contribute to TS4Tools development:

- Review the **[Developer Onboarding Guide](Developer-Onboarding-Guide.md)**
- Check the **[Migration Roadmap](../MIGRATION_ROADMAP.md)** for current priorities
- Browse **[Architecture Decision Records](architecture/adr/README.md)** to understand design decisions

---

**Happy coding!** 🎮✨

Last updated: August 10, 2025
