# Package Creator Example

This example demonstrates how to create a new Sims 4 package file from scratch using TS4Tools.

## Features

- Create a new empty package
- Add custom text resources
- Add custom binary resources
- Add JSON metadata resources
- Save the package to disk
- Verify the created package by loading it back

## Usage

```bash
cd examples/PackageCreator
dotnet run "C:\path\to\output\new-package.package"
```

## Example Output

```text
=== Creating New Package ===
Output path: C:\path\to\output\new-package.package

Added text resource: 421 bytes
Added binary resource: 40 bytes
Added metadata resource: 687 bytes

=== Package Statistics ===
Total resources: 3
  Custom Resource: T=0x12345678, Size=421 bytes
  Custom Resource: T=0x87654321, Size=40 bytes
  Custom Resource: T=0xABCDEF00, Size=687 bytes

Saving package...
Package saved successfully!
File size: 1.2 KB
Created: 2024-08-03 14:30:45

=== Verification ===
Verification successful!
  Resources loaded: 3
  Package path: C:\path\to\output\new-package.package
  Creation time: 2024-08-03 14:30:45
  Sample resource data length: 421 bytes
  Text preview: This is a sample text resource created by TS4Tools PackageCreator example...
```

## Key Concepts Demonstrated

1. **Package Creation**: Using `IPackageFactory.CreateEmptyPackageAsync()`
2. **Resource Addition**: Adding resources with custom resource keys
3. **Custom Resource Types**: Creating resources with arbitrary type IDs
4. **Resource Implementation**: Custom `IResource` implementation
5. **Package Persistence**: Saving packages to disk
6. **Package Verification**: Loading created packages to verify integrity

## Code Highlights

### Creating a New Package

```csharp
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
using var package = await packageFactory.CreateEmptyPackageAsync();
```

### Creating Resource Keys

```csharp
var resourceKey = new ResourceKey(
    resourceType: 0x12345678,  // Custom type ID
    resourceGroup: 0x00000000, // Default group
    instance: 0x1000000000000001 // Unique instance ID
);
```

### Custom Resource Implementation

```csharp
public class CustomResource : IResource
{
    private readonly MemoryStream _stream;

    public CustomResource(byte[] data)
    {
        _stream = new MemoryStream(data);
    }

    public Stream Stream => _stream;
    public byte[] AsBytes() => _stream.ToArray();
    public event EventHandler? Changed;
}
```

### Adding Resources to Package

```csharp
var textResource = new CustomResource(textBytes);
package.AddResource(textResourceKey, textResource);
```

### Saving the Package

```csharp
await package.SaveAsAsync(outputPath);
```

## Resource Types Created

This example creates three types of resources:

1. **Text Resource (0x12345678)**: UTF-8 encoded text content
2. **Binary Resource (0x87654321)**: Sample binary data (simulated PNG header)
3. **Metadata Resource (0xABCDEF00)**: JSON metadata about the package

## File Structure

The created package follows the standard DBPF format used by The Sims 4:

- **Package Header**: Contains magic number, version, and index information
- **Resource Index**: Maps resource keys to file positions and sizes
- **Resource Data**: The actual resource content stored sequentially

## Use Cases

This example is useful for:

- Creating mod packages programmatically
- Batch processing and conversion tools
- Custom content creation pipelines
- Testing and development scenarios
- Educational purposes for understanding package structure

## Dependencies

- TS4Tools.Core.Package
- TS4Tools.Core.DependencyInjection
- TS4Tools.Extensions
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging.Console

