# TS4Tools API Reference

## Core Interfaces

### IPackage

The main interface for working with DBPF package files.

```csharp
public interface IPackage : IDisposable, IAsyncDisposable
{
    // Properties
    PackageHeader Header { get; }
    IPackageResourceIndex ResourceIndex { get; }
    string? FilePath { get; }
    DateTime CreationTime { get; }
    DateTime UpdatedTime { get; }

    // Methods
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default);
    Task CompactAsync(CancellationToken cancellationToken = default);

    // Resource Management
    IResource? GetResource(IResourceKey key);
    void AddResource(IResourceKey key, IResource resource);
    bool RemoveResource(IResourceKey key);
}
```

#### Usage Example

```csharp
// Load package
using var package = await packageFactory.LoadFromFileAsync("example.package");

// Get package information
Console.WriteLine($"Package created: {package.CreationTime}");
Console.WriteLine($"Last modified: {package.UpdatedTime}");
Console.WriteLine($"Resource count: {package.ResourceIndex.Count}");

// Access a specific resource
var resourceKey = new ResourceKey(0x12345678, 0x87654321, 0x1234567890ABCDEF);
var resource = package.GetResource(resourceKey);
if (resource != null)
{
    var data = resource.AsBytes();
    Console.WriteLine($"Resource size: {data.Length} bytes");
}
```

### IPackageFactory

Factory service for creating and loading packages.

```csharp
public interface IPackageFactory
{
    Task<IPackage> CreateEmptyPackageAsync(CancellationToken cancellationToken = default);
    Task<IPackage> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IPackage> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
```

#### IPackageFactory Usage Example

```csharp
var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

// Create new package
var newPackage = await packageFactory.CreateEmptyPackageAsync();

// Load existing package
var existingPackage = await packageFactory.LoadFromFileAsync("existing.package");

// Load from stream
using var fileStream = File.OpenRead("another.package");
var streamPackage = await packageFactory.LoadFromStreamAsync(fileStream);
```

### IResourceKey

Identifies a unique resource within a package using Type, Group, and Instance IDs.

```csharp
public interface IResourceKey : IEquatable<IResourceKey>, IComparable<IResourceKey>
{
    uint ResourceType { get; }
    uint ResourceGroup { get; }
    ulong Instance { get; }
}
```

#### IResourceKey Usage Example

```csharp
// Create resource key
var key = new ResourceKey(
    resourceType: 0x12345678,
    resourceGroup: 0x87654321,
    instance: 0x1234567890ABCDEF
);

// Use as dictionary key
var resources = new Dictionary<IResourceKey, IResource>();
resources[key] = someResource;

// Comparison and sorting
var sortedKeys = package.ResourceIndex.Keys.OrderBy(k => k).ToList();
```

### IResource

Represents resource data within a package.

```csharp
public interface IResource
{
    Stream Stream { get; }
    byte[] AsBytes();

    // Change notification
    event EventHandler? Changed;
}
```

#### IResource Usage Example

```csharp
// Read resource data
var resource = package.GetResource(resourceKey);
if (resource != null)
{
    // As stream (efficient for large resources)
    using var stream = resource.Stream;
    var buffer = new byte[1024];
    var bytesRead = await stream.ReadAsync(buffer);

    // As byte array (convenient for small resources)
    var allData = resource.AsBytes();

    // Listen for changes
    resource.Changed += (sender, e) => Console.WriteLine("Resource modified");
}
```

## Service Extensions

### AddTS4ToolsCore()

Registers all TS4Tools services with the dependency injection container.

```csharp
public static IServiceCollection AddTS4ToolsCore(
    this IServiceCollection services,
    IConfiguration? configuration = null)
```

#### Service Registration Usage Example

```csharp
var builder = Host.CreateApplicationBuilder();

// Basic registration
builder.Services.AddTS4ToolsCore();

// With configuration
builder.Services.AddTS4ToolsCore(builder.Configuration);

var host = builder.Build();
```

### Service Lifetimes

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IPackageFactory` | Singleton | Thread-safe factory for package creation |
| `IPackageService` | Scoped | High-level package operations |
| `IResourceManager` | Singleton | Resource type management and caching |
| `IApplicationSettingsService` | Singleton | Configuration access with change notifications |

## Configuration Options

### ApplicationSettings

Main configuration class for TS4Tools.

```csharp
public class ApplicationSettings
{
    [Required]
    public bool EnableLogging { get; set; } = true;

    [Required]
    public string LogLevel { get; set; } = "Information";

    [Required]
    [DirectoryPath]
    public string TempDirectory { get; set; } = "temp";

    [Range(1, 10000)]
    public int MaxCacheSize { get; set; } = 1000;

    public bool EnableResourceCaching { get; set; } = true;

    public TimeSpan ResourceCacheTimeout { get; set; } = TimeSpan.FromMinutes(30);

    [Required]
    public string DefaultEncoding { get; set; } = "UTF-8";
}
```

#### Configuration File Example

```json
{
  "TS4Tools": {
    "EnableLogging": true,
    "LogLevel": "Debug",
    "TempDirectory": "/tmp/ts4tools",
    "MaxCacheSize": 2000,
    "EnableResourceCaching": true,
    "ResourceCacheTimeout": "01:00:00",
    "DefaultEncoding": "UTF-8"
  }
}
```

## Resource Type Registry

### IResourceTypeRegistry

Service for managing resource type mappings and metadata.

```csharp
public interface IResourceTypeRegistry
{
    void RegisterResourceType(uint typeId, string name, string description);
    bool TryGetResourceTypeName(uint typeId, out string name);
    IEnumerable<uint> GetRegisteredTypes();
    bool IsTypeRegistered(uint typeId);
}
```

#### IResourceTypeRegistry Usage Example

```csharp
var registry = serviceProvider.GetRequiredService<IResourceTypeRegistry>();

// Register custom resource type
registry.RegisterResourceType(0x12345678, "CustomResource", "My custom resource type");

// Check if type is known
if (registry.TryGetResourceTypeName(0x12345678, out var typeName))
{
    Console.WriteLine($"Resource type: {typeName}");
}

// List all registered types
foreach (var typeId in registry.GetRegisteredTypes())
{
    if (registry.TryGetResourceTypeName(typeId, out var name))
    {
        Console.WriteLine($"0x{typeId:X8}: {name}");
    }
}
```

## Error Handling

### Common Exceptions

- **PackageFormatException**: Invalid package file format
- **ResourceNotFoundException**: Requested resource not found
- **PackageCorruptedException**: Package file is corrupted
- **ArgumentLengthException**: Invalid argument length (custom exception)

#### Example Error Handling

```csharp
try
{
    var package = await packageFactory.LoadFromFileAsync("invalid.package");
}
catch (PackageFormatException ex)
{
    Console.WriteLine($"Invalid package format: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Package file not found: {ex.FileName}");
}
catch (PackageCorruptedException ex)
{
    Console.WriteLine($"Package is corrupted: {ex.Message}");
}
```

## Performance Considerations

### Async Operations

All I/O operations are async and support cancellation:

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var package = await packageFactory.LoadFromFileAsync(
        "large-package.package",
        cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}
```

### Memory Management

Use `using` statements for proper resource disposal:

```csharp
// Automatic disposal
using var package = await packageFactory.LoadFromFileAsync("example.package");

// Manual disposal if needed
var package = await packageFactory.LoadFromFileAsync("example.package");
try
{
    // Work with package
}
finally
{
    await package.DisposeAsync();
}
```

### Resource Caching

Configure caching for better performance:

```json
{
  "TS4Tools": {
    "EnableResourceCaching": true,
    "MaxCacheSize": 2000,
    "ResourceCacheTimeout": "00:30:00"
  }
}
```

## Extension Points

### Custom Resource Types

Create custom resource implementations:

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

    protected virtual void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
```

### Service Registration Extensions

Create custom service registration methods:

```csharp
public static class MyServiceExtensions
{
    public static IServiceCollection AddMyCustomServices(
        this IServiceCollection services)
    {
        services.AddTS4ToolsCore();
        services.AddSingleton<IMyCustomService, MyCustomService>();
        return services;
    }
}
```

