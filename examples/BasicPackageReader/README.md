# Basic Package Reader Example

This example demonstrates how to use TS4Tools to read and analyze a Sims 4 package file.

## Features

- Load any .package file
- Display basic package information (creation time, modification time, resource count)
- Analyze resources by type with counts and sizes
- Show sample resource data with hex preview
- Use friendly resource type names when available

## Usage

```bash
cd examples/BasicPackageReader
dotnet run "C:\path\to\your\file.package"
```

## Example Output

```text
=== Package Information ===
File Path: C:\path\to\your\file.package
Created: 2024-01-15 10:30:45
Modified: 2024-01-15 10:30:45
Total Resources: 1,247

=== Resource Analysis ===
Type ID      Count    Total Size      Type Name
---------------------------------------------------------------------------
0x220557DA   456      2.3 MB          Image Resource
0x0333406C   289      1.8 MB          STBL Resource
0x319E4F1D   156      892.5 KB        OBJD Resource
0x0D338A3A   89       445.2 KB        Unknown
0x545AC67A   67       234.1 KB        Unknown
---------------------------------------------------------------------------
TOTAL        1,247    5.7 MB

=== Sample Resources ===
Resource Key: T=0x220557DA, G=0x00000000, I=0x1234567890ABCDEF
  Size: 4.2 KB (compressed: false)
  Position: 0x000A4B2C
  Data loaded: 4,321 bytes
  Preview: 89 50 4E 47 0D 0A 1A 0A 00 00 00 0D 49 48 44 52

...
```

## Key Concepts Demonstrated

1. **Dependency Injection Setup**: How to configure TS4Tools services
2. **Package Loading**: Using `IPackageFactory` to load packages
3. **Resource Enumeration**: Iterating through all resources in a package
4. **Resource Type Registry**: Getting friendly names for resource types
5. **Resource Data Access**: Loading and examining resource content
6. **Error Handling**: Proper exception handling for file operations

## Code Highlights

### Service Setup

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTS4ToolsCore();
builder.Services.AddTS4ToolsExtensions();
```

### Loading a Package

```csharp
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
using var package = await packageFactory.LoadFromFileAsync(packagePath);
```

### Analyzing Resources

```csharp
var resourcesByType = package.ResourceIndex
    .GroupBy(entry => entry.Key.ResourceType)
    .OrderByDescending(group => group.Count())
    .ToList();
```

### Accessing Resource Data

```csharp
var resource = package.GetResource(key);
if (resource != null)
{
    var data = resource.AsBytes();
    // Process the resource data...
}
```

## Dependencies

- TS4Tools.Core.Package
- TS4Tools.Core.DependencyInjection
- TS4Tools.Extensions
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging.Console

