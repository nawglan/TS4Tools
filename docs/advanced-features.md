# TS4Tools Advanced Features Guide

## Performance Optimization

### Memory Management

TS4Tools is designed with performance in mind, utilizing modern .NET features for optimal memory usage.

#### Span<T> and Memory<T> Usage

```csharp
// Efficient string operations without allocation
ReadOnlySpan<char> span = resourceName.AsSpan();
var hash = FNVHash.Calculate(span);

// Memory-efficient binary operations
ReadOnlySpan<byte> data = resource.AsBytes().AsSpan();
var processed = ProcessBinaryData(data);
```

#### Stream-Based Operations

For large resources, prefer stream-based operations:

```csharp
// Good for large resources
using var stream = resource.Stream;
await ProcessLargeResourceAsync(stream);

// Avoid for large resources (loads entire content into memory)
var allBytes = resource.AsBytes();
```

### Async Patterns

All I/O operations support async/await with proper cancellation:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var package = await packageFactory.LoadFromFileAsync(
        largePath, 
        cts.Token);
        
    await package.CompactAsync(cts.Token);
    await package.SaveAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

### Resource Caching

Configure intelligent caching for frequently accessed resources:

```json
{
  "TS4Tools": {
    "EnableResourceCaching": true,
    "MaxCacheSize": 2000,
    "ResourceCacheTimeout": "00:30:00"
  }
}
```

## Advanced Configuration

### Custom Configuration Sources

Combine multiple configuration sources:

```csharp
var builder = Host.CreateApplicationBuilder();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables("TS4TOOLS_")
    .AddCommandLine(args);

builder.Services.AddTS4ToolsCore(builder.Configuration);
```

### Environment-Specific Settings

Create environment-specific configuration files:

**appsettings.Development.json**:
```json
{
  "TS4Tools": {
    "LogLevel": "Debug",
    "EnableResourceCaching": false,
    "TempDirectory": "temp/debug"
  }
}
```

**appsettings.Production.json**:
```json
{
  "TS4Tools": {
    "LogLevel": "Warning",
    "EnableResourceCaching": true,
    "MaxCacheSize": 5000,
    "TempDirectory": "/var/tmp/ts4tools"
  }
}
```

### Configuration Validation

Implement custom validation:

```csharp
public class CustomApplicationSettings : ApplicationSettings, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxCacheSize > 10000)
        {
            yield return new ValidationResult(
                "Cache size cannot exceed 10,000 entries",
                new[] { nameof(MaxCacheSize) });
        }
        
        if (!Directory.Exists(TempDirectory))
        {
            yield return new ValidationResult(
                $"Temp directory does not exist: {TempDirectory}",
                new[] { nameof(TempDirectory) });
        }
    }
}
```

## Resource Management

### Custom Resource Types

Create specialized resource implementations:

```csharp
public class ImageResource : IResource
{
    private readonly byte[] _imageData;
    private readonly string _format;
    
    public ImageResource(byte[] imageData, string format)
    {
        _imageData = imageData;
        _format = format;
    }
    
    public Stream Stream => new MemoryStream(_imageData);
    public byte[] AsBytes() => _imageData;
    
    public string Format => _format;
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    public event EventHandler? Changed;
    
    public void Resize(int newWidth, int newHeight)
    {
        // Implementation for resizing
        Width = newWidth;
        Height = newHeight;
        OnChanged();
    }
    
    protected virtual void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
```

### Resource Factories

Implement resource factories for specific types:

```csharp
public interface IImageResourceFactory
{
    Task<ImageResource> CreateFromFileAsync(string imagePath);
    Task<ImageResource> CreateFromStreamAsync(Stream imageStream, string format);
}

public class ImageResourceFactory : IImageResourceFactory
{
    public async Task<ImageResource> CreateFromFileAsync(string imagePath)
    {
        var data = await File.ReadAllBytesAsync(imagePath);
        var format = Path.GetExtension(imagePath).ToLowerInvariant();
        return new ImageResource(data, format);
    }
    
    public async Task<ImageResource> CreateFromStreamAsync(Stream imageStream, string format)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        return new ImageResource(memoryStream.ToArray(), format);
    }
}
```

### Resource Transformation Pipeline

Create processing pipelines for resources:

```csharp
public interface IResourceProcessor<T> where T : IResource
{
    Task<T> ProcessAsync(T resource, CancellationToken cancellationToken = default);
}

public class ImageCompressionProcessor : IResourceProcessor<ImageResource>
{
    private readonly ILogger<ImageCompressionProcessor> _logger;
    
    public ImageCompressionProcessor(ILogger<ImageCompressionProcessor> logger)
    {
        _logger = logger;
    }
    
    public async Task<ImageResource> ProcessAsync(ImageResource resource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compressing image resource");
        
        // Implement compression logic
        var compressedData = await CompressImageAsync(resource.AsBytes(), cancellationToken);
        
        return new ImageResource(compressedData, resource.Format);
    }
    
    private async Task<byte[]> CompressImageAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        // Implementation details...
        return imageData; // Placeholder
    }
}
```

## Package Operations

### Batch Operations

Process multiple packages efficiently:

```csharp
public class BatchPackageProcessor
{
    private readonly IPackageFactory _packageFactory;
    private readonly ILogger<BatchPackageProcessor> _logger;
    
    public BatchPackageProcessor(IPackageFactory packageFactory, ILogger<BatchPackageProcessor> logger)
    {
        _packageFactory = packageFactory;
        _logger = logger;
    }
    
    public async Task ProcessDirectoryAsync(string inputDirectory, string outputDirectory, 
        Func<IPackage, Task> processor, CancellationToken cancellationToken = default)
    {
        var packageFiles = Directory.GetFiles(inputDirectory, "*.package", SearchOption.AllDirectories);
        
        _logger.LogInformation("Processing {Count} packages", packageFiles.Length);
        
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = packageFiles.Select(async filePath =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessSinglePackageAsync(filePath, outputDirectory, processor, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
    }
    
    private async Task ProcessSinglePackageAsync(string inputPath, string outputDirectory,
        Func<IPackage, Task> processor, CancellationToken cancellationToken)
    {
        try
        {
            using var package = await _packageFactory.LoadFromFileAsync(inputPath, cancellationToken);
            
            await processor(package);
            
            var outputPath = Path.Combine(outputDirectory, Path.GetFileName(inputPath));
            await package.SaveAsAsync(outputPath, cancellationToken);
            
            _logger.LogInformation("Processed: {FileName}", Path.GetFileName(inputPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process: {FileName}", Path.GetFileName(inputPath));
        }
    }
}
```

### Package Validation

Implement comprehensive package validation:

```csharp
public interface IPackageValidator
{
    Task<ValidationResult> ValidateAsync(IPackage package, CancellationToken cancellationToken = default);
}

public class PackageValidator : IPackageValidator
{
    private readonly ILogger<PackageValidator> _logger;
    
    public PackageValidator(ILogger<PackageValidator> logger)
    {
        _logger = logger;
    }
    
    public async Task<ValidationResult> ValidateAsync(IPackage package, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        
        // Validate header
        ValidateHeader(package.Header, result);
        
        // Validate resources
        await ValidateResourcesAsync(package, result, cancellationToken);
        
        // Validate integrity
        await ValidateIntegrityAsync(package, result, cancellationToken);
        
        return result;
    }
    
    private void ValidateHeader(PackageHeader header, ValidationResult result)
    {
        if (header.MajorVersion < 2)
        {
            result.AddError("Unsupported package version");
        }
        
        if (header.ResourceCount == 0)
        {
            result.AddWarning("Package contains no resources");
        }
    }
    
    private async Task ValidateResourcesAsync(IPackage package, ValidationResult result, CancellationToken cancellationToken)
    {
        foreach (var entry in package.ResourceIndex)
        {
            try
            {
                var resource = package.GetResource(entry.Key);
                if (resource == null)
                {
                    result.AddError($"Resource not accessible: {entry.Key}");
                    continue;
                }
                
                var data = resource.AsBytes();
                if (data.Length != entry.Value.FileSize)
                {
                    result.AddError($"Resource size mismatch: {entry.Key}");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Resource validation failed: {entry.Key} - {ex.Message}");
            }
        }
    }
    
    private async Task ValidateIntegrityAsync(IPackage package, ValidationResult result, CancellationToken cancellationToken)
    {
        // Implement integrity checks (checksums, etc.)
        _logger.LogDebug("Validating package integrity");
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public bool IsValid => Errors.Count == 0;
    
    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
}
```

## Cross-Platform Considerations

### File Path Handling

Use proper path handling for cross-platform compatibility:

```csharp
// Good - platform agnostic
var packagePath = Path.Combine(baseDirectory, "packages", "example.package");
var tempPath = Path.Combine(Path.GetTempPath(), "ts4tools", Guid.NewGuid().ToString());

// Bad - Windows specific
var packagePath = baseDirectory + "\\packages\\example.package";
```

### Directory Separator Handling

```csharp
public static class PathHelper
{
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
            
        // Replace all separators with the platform-appropriate one
        return path.Replace('\\', Path.DirectorySeparatorChar)
                  .Replace('/', Path.DirectorySeparatorChar);
    }
    
    public static string ToUniversalPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
            
        // Use forward slashes for universal representation
        return path.Replace('\\', '/');
    }
}
```

### Platform-Specific Services

Implement platform-specific functionality:

```csharp
public interface IPlatformService
{
    string GetDefaultPackageDirectory();
    Task<bool> IsPackageFileAsync(string filePath);
    Task OpenPackageInDefaultEditorAsync(string filePath);
}

public class WindowsPlatformService : IPlatformService
{
    public string GetDefaultPackageDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "Electronic Arts", "The Sims 4", "Mods");
    }
    
    public async Task<bool> IsPackageFileAsync(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".package", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task OpenPackageInDefaultEditorAsync(string filePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}

// Register platform-specific services
public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        }
        else
        {
            services.AddSingleton<IPlatformService, DefaultPlatformService>();
        }
        
        return services;
    }
}
```

## Integration Patterns

### Plugin Architecture

Create extensible plugin systems:

```csharp
public interface ITS4ToolsPlugin
{
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
    Task<bool> CanHandleResourceAsync(IResourceKey resourceKey);
    Task ProcessResourceAsync(IResource resource, IServiceProvider serviceProvider);
}

public class PluginManager
{
    private readonly List<ITS4ToolsPlugin> _plugins = new();
    private readonly ILogger<PluginManager> _logger;
    
    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
    }
    
    public async Task LoadPluginsFromDirectoryAsync(string pluginDirectory, IServiceProvider serviceProvider)
    {
        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll");
        
        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ITS4ToolsPlugin).IsAssignableFrom(t));
                
                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (ITS4ToolsPlugin)Activator.CreateInstance(pluginType)!;
                    await plugin.InitializeAsync(serviceProvider);
                    _plugins.Add(plugin);
                    
                    _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.Name, plugin.Version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {PluginFile}", pluginFile);
            }
        }
    }
    
    public async Task<ITS4ToolsPlugin?> FindHandlerAsync(IResourceKey resourceKey)
    {
        foreach (var plugin in _plugins)
        {
            if (await plugin.CanHandleResourceAsync(resourceKey))
            {
                return plugin;
            }
        }
        
        return null;
    }
}
```

This advanced features guide provides comprehensive examples of how to extend and optimize TS4Tools for production use cases.
