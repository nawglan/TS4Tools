# Plugin System Architecture

## Overview

The TS4Tools Plugin System provides a modern, secure, and extensible architecture for loading and managing third-party plugins while maintaining compatibility with the legacy Sims4Tools ecosystem. Built on .NET 9's `AssemblyLoadContext`, the system addresses critical security and compatibility issues found in the original `Assembly.LoadFile()` approach.

## Architecture Components

### Core Interfaces

#### IPluginLoadContext

Manages the lifecycle of plugin assemblies with proper isolation:

```csharp
public interface IPluginLoadContext : IDisposable
{
    string Name { get; }
    string PluginPath { get; }
    bool IsLoaded { get; }
    Task<Assembly> LoadAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default);
    Task UnloadAsync();
    IEnumerable<Type> GetExportedTypes<T>() where T : class;
}
```

#### IPluginManager

Central coordination for plugin discovery, loading, and management:

```csharp
public interface IPluginManager : IDisposable
{
    Task<IPluginLoadContext> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default);
    Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default);
    IReadOnlyList<IPluginLoadContext> GetLoadedPlugins();
    Task<IEnumerable<T>> GetPluginExports<T>() where T : class;
}
```

#### IPluginDiscoveryService

Handles plugin discovery and metadata validation:

```csharp
public interface IPluginDiscoveryService
{
    Task<IEnumerable<PluginDescriptor>> DiscoverPluginsAsync(string searchPath, CancellationToken cancellationToken = default);
    Task<PluginDescriptor?> LoadPluginDescriptorAsync(string pluginPath, CancellationToken cancellationToken = default);
    bool ValidatePluginCompatibility(PluginDescriptor descriptor);
}
```

## Key Features

### 1. Assembly Isolation

Each plugin loads in its own `AssemblyLoadContext`, providing:

- **Security**: Isolated execution environments
- **Stability**: Plugin crashes don't affect the host application
- **Memory Management**: Proper cleanup when plugins are unloaded
- **Version Isolation**: Multiple versions of dependencies can coexist

### 2. Legacy Compatibility

The system maintains backward compatibility with existing Sims4Tools plugins through:

- **Adapter Pattern**: Legacy `AResourceHandler` plugins work without changes
- **API Bridging**: Modern interfaces wrap legacy functionality
- **Gradual Migration**: Plugins can be updated incrementally

### 3. Modern Plugin Development

New plugins can leverage modern .NET features:

- **Dependency Injection**: Full IoC container integration
- **Async Operations**: Non-blocking plugin operations
- **Configuration**: Modern configuration binding
- **Logging**: Structured logging with Microsoft.Extensions.Logging

## Implementation Details

### Plugin Loading Process

1. **Discovery Phase**

   ```csharp
   var plugins = await pluginDiscovery.DiscoverPluginsAsync(pluginDirectory);
   ```

2. **Validation Phase**

   ```csharp
   var validPlugins = plugins.Where(p => pluginDiscovery.ValidatePluginCompatibility(p));
   ```

3. **Loading Phase**

   ```csharp
   foreach (var plugin in validPlugins)
   {
       var context = await pluginManager.LoadPluginAsync(plugin.AssemblyPath);
       // Register plugin exports with DI container
   }
   ```

### Plugin Descriptor Format

```json
{
  "name": "MyPlugin",
  "version": "1.0.0",
  "apiVersion": "4.13.0",
  "description": "Custom resource handler plugin",
  "author": "Plugin Developer",
  "dependencies": [
    "TS4Tools.Core.Resources >= 4.13.0"
  ],
  "exports": [
    {
      "type": "ResourceHandler",
      "resourceTypes": ["0x12345678", "0x87654321"]
    }
  ]
}
```

### Security Considerations

1. **Sandboxing**: Plugins run with limited permissions
2. **API Surface**: Only approved APIs are exposed to plugins
3. **Validation**: All plugin assemblies are validated before loading
4. **Resource Limits**: Memory and CPU usage monitoring

## Usage Examples

### Creating a Resource Handler Plugin

```csharp
[Plugin("CustomImageHandler", "1.0.0")]
public class CustomImageResourceHandler : IResourceHandler
{
    private readonly ILogger<CustomImageResourceHandler> _logger;
    
    public CustomImageResourceHandler(ILogger<CustomImageResourceHandler> logger)
    {
        _logger = logger;
    }
    
    public bool CanHandle(uint resourceType) => resourceType == 0x2E75C764;
    
    public async Task<IResource> CreateResourceAsync(Stream data, IResourceKey key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating custom image resource for key {Key}", key);
        return new CustomImageResource(data, key);
    }
}
```

### Plugin Registration

```csharp
// In plugin's entry point
public class PluginModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IResourceHandler, CustomImageResourceHandler>();
        services.AddScoped<ICustomService, CustomServiceImplementation>();
    }
}
```

## Migration from Legacy System

### Legacy Assembly.LoadFile() Issues

The original system used `Assembly.LoadFile()` which caused:

- **Security Vulnerabilities**: Unrestricted assembly loading
- **Memory Leaks**: No proper assembly unloading
- **Version Conflicts**: DLL hell with conflicting dependencies
- **Cross-Platform Issues**: Platform-specific loading behaviors

### Modern AssemblyLoadContext Benefits

- **Proper Isolation**: Each plugin in separate context
- **Clean Unloading**: Memory is properly reclaimed
- **Dependency Management**: Better handling of plugin dependencies
- **Cross-Platform**: Consistent behavior across Windows/Linux/macOS

## Performance Characteristics

### Plugin Loading Performance

- **Cold Start**: ~50-100ms per plugin (depending on size)
- **Warm Start**: ~10-20ms for cached descriptors
- **Memory Overhead**: ~2-5MB per loaded context
- **Unload Time**: ~10-50ms with proper cleanup

### Best Practices

1. **Lazy Loading**: Load plugins only when needed
2. **Caching**: Cache plugin metadata for faster startup
3. **Batch Operations**: Load multiple plugins concurrently
4. **Resource Management**: Proper disposal of plugin contexts

## Testing Strategy

### Unit Tests

- Plugin discovery and validation logic
- Assembly loading and isolation
- Error handling and recovery scenarios

### Integration Tests

- End-to-end plugin loading workflows
- Legacy plugin compatibility
- Resource handler registration and execution

### Performance Tests

- Plugin loading time benchmarks
- Memory usage monitoring
- Concurrent plugin loading scenarios

## Future Enhancements

### Planned Features

1. **Hot Reloading**: Dynamic plugin updates without restart
2. **Plugin Marketplace**: Centralized plugin distribution
3. **Advanced Sandboxing**: More granular security controls
4. **Plugin Analytics**: Usage tracking and performance monitoring

### API Evolution

The plugin system is designed for backward compatibility while allowing gradual evolution:

- New interfaces can be added without breaking existing plugins
- Legacy adapters will be maintained for several major versions
- Migration tools will help upgrade legacy plugins to modern APIs

## Troubleshooting

### Common Issues

1. **Plugin Not Loading**: Check plugin descriptor validity
2. **Dependency Conflicts**: Verify plugin dependencies
3. **Performance Issues**: Monitor plugin resource usage
4. **Legacy Compatibility**: Use adapter pattern for old plugins

### Debugging

Enable detailed plugin logging:

```json
{
  "Logging": {
    "LogLevel": {
      "TS4Tools.Core.Plugins": "Debug"
    }
  }
}
```

## References

- [Microsoft AssemblyLoadContext Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)
- [.NET Plugin Architecture Best Practices](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- [Legacy Sims4Tools Plugin System](../legacy/plugin-system-analysis.md)

---

*Last Updated: August 8, 2025*  
*For implementation details, see TS4Tools.Core.Plugins source code*
