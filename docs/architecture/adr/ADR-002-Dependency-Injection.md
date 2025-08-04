# ADR-002: Adopt Dependency Injection Container

**Status:** Accepted  
**Date:** August 3, 2025  
**Deciders:** Architecture Team, Senior Developers  

## Context

The legacy TS4Tools codebase relies heavily on static classes, singletons, and direct instantiation patterns that create tight coupling, make testing difficult, and prevent proper separation of concerns. Modern .NET applications benefit significantly from dependency injection for testability, maintainability, and configuration management.

## Decision

We will adopt Microsoft's built-in dependency injection container (`Microsoft.Extensions.DependencyInjection`) as the primary IoC container for TS4Tools.

## Rationale

### Problems with Current Architecture
1. **Static Dependencies**: Hard to test and mock
2. **Tight Coupling**: Direct instantiation creates rigid dependencies
3. **Configuration**: Scattered throughout codebase
4. **Testing**: Difficult to unit test due to static dependencies
5. **Lifecycle Management**: Manual resource management prone to leaks

### Benefits of Dependency Injection
1. **Testability**: Easy mocking and unit testing
2. **Maintainability**: Loose coupling and clear dependencies
3. **Configuration**: Centralized configuration management
4. **Lifetime Management**: Automatic disposal and lifecycle management
5. **Cross-Platform**: Works consistently across all supported platforms

## Implementation Design

### Service Registration Architecture
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTS4Tools(this IServiceCollection services, IConfiguration configuration)
    {
        // Core services
        services.AddSingleton<IPackageFactory, PackageFactory>();
        services.AddScoped<IResourceManager, ResourceManager>();
        services.AddTransient<IResourceFactory<IResource>, DefaultResourceFactory>();
        
        // Configuration
        services.Configure<ApplicationSettings>(configuration.GetSection("TS4Tools"));
        services.Configure<ResourceManagerOptions>(configuration.GetSection("ResourceManager"));
        
        // Platform-specific services
        services.AddPlatformServices();
        
        return services;
    }
}
```

### Service Lifetimes Strategy

| Service Type | Lifetime | Justification |
|--------------|----------|---------------|
| `IPackageFactory` | Singleton | Stateless factory, expensive initialization |
| `IResourceManager` | Scoped | Per-operation caching, moderate state |
| `IResourceFactory<T>` | Transient | Lightweight, stateless factories |
| `ILogger<T>` | Singleton | Thread-safe, shared across application |
| Configuration Options | Singleton | Immutable configuration data |

### Legacy Compatibility Layer
```csharp
// Adapter for legacy static access patterns
public static class LegacyServiceLocator
{
    private static IServiceProvider? _serviceProvider;
    
    [Obsolete("Use constructor injection instead. This will be removed in Phase 3.0")]
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    [Obsolete("Use constructor injection instead. This will be removed in Phase 3.0")]
    public static T GetService<T>() where T : notnull
    {
        return _serviceProvider?.GetRequiredService<T>() 
            ?? throw new InvalidOperationException("Service locator not initialized");
    }
}
```

## Service Organization

### Core Service Categories

#### 1. Package Services
```csharp
// Package reading and writing
services.AddScoped<IPackageReader, PackageReader>();
services.AddScoped<IPackageWriter, PackageWriter>();
services.AddSingleton<IPackageFactory, PackageFactory>();
```

#### 2. Resource Services
```csharp
// Resource management and factories
services.AddScoped<IResourceManager, ResourceManager>();
services.AddTransient<IResourceFactory<IResource>, DefaultResourceFactory>();
services.RegisterResourceFactories(); // Auto-registration of all resource factories
```

#### 3. System Services
```csharp
// File system, logging, configuration
services.AddSingleton<IFileSystem, FileSystem>();
services.AddLogging(builder => builder.AddConsole().AddDebug());
services.AddOptions<ApplicationSettings>().BindConfiguration("TS4Tools");
```

#### 4. Platform Services
```csharp
// Platform-specific implementations
#if WINDOWS
services.AddScoped<IDialogService, WindowsDialogService>();
#elif MACOS
services.AddScoped<IDialogService, MacOSDialogService>();
#elif LINUX
services.AddScoped<IDialogService, LinuxDialogService>();
#endif
```

## Migration Strategy

### Phase 1: Infrastructure Setup (Week 1)
1. Add `Microsoft.Extensions.DependencyInjection` package
2. Create service registration extensions
3. Implement basic container initialization
4. Add legacy compatibility layer

### Phase 2: Core Services Migration (Week 2-3)
1. Convert static classes to injectable services
2. Update constructors to accept dependencies
3. Register services with appropriate lifetimes
4. Update unit tests to use dependency injection

### Phase 3: Legacy Cleanup (Week 4)
1. Remove static dependencies
2. Eliminate service locator pattern usage
3. Complete constructor injection adoption
4. Remove compatibility layer

## Alternative Containers Considered

### Autofac
**Pros:**
- Rich feature set
- Advanced lifetime management
- Module system

**Cons:**
- Additional complexity
- Extra dependency
- Overkill for current needs

**Decision:** Rejected due to Microsoft DI being sufficient

### Castle Windsor
**Pros:**
- Mature container
- Advanced features

**Cons:**
- Heavy dependency
- Complex configuration
- Declining maintenance

**Decision:** Rejected due to complexity and maintenance concerns

### Unity Container
**Pros:**
- Microsoft-backed
- Good performance

**Cons:**
- Less integrated with modern .NET
- Additional dependency

**Decision:** Rejected in favor of built-in container

## Implementation Examples

### Before: Static Dependencies
```csharp
// ❌ Old pattern - tightly coupled, hard to test
public class PackageReader
{
    public IPackage ReadPackage(string path)
    {
        var logger = LogManager.GetLogger("PackageReader"); // Static dependency
        var settings = Settings.Current; // Static configuration
        var fileSystem = new FileSystem(); // Direct instantiation
        
        // ... implementation
    }
}
```

### After: Dependency Injection
```csharp
// ✅ New pattern - loosely coupled, easily testable
public class PackageReader : IPackageReader
{
    private readonly ILogger<PackageReader> _logger;
    private readonly IOptions<PackageReaderSettings> _settings;
    private readonly IFileSystem _fileSystem;
    
    public PackageReader(
        ILogger<PackageReader> logger,
        IOptions<PackageReaderSettings> settings,
        IFileSystem fileSystem)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }
    
    public async Task<IPackage> ReadPackageAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading package from {Path}", path);
        // ... implementation using injected dependencies
    }
}
```

### Unit Testing Benefits
```csharp
[Test]
public async Task ReadPackageAsync_WithValidPath_ReturnsPackage()
{
    // Arrange
    var logger = Substitute.For<ILogger<PackageReader>>();
    var settings = Options.Create(new PackageReaderSettings());
    var fileSystem = Substitute.For<IFileSystem>();
    
    fileSystem.FileExistsAsync(Arg.Any<string>()).Returns(true);
    fileSystem.OpenReadAsync(Arg.Any<string>()).Returns(new MemoryStream());
    
    var reader = new PackageReader(logger, settings, fileSystem);
    
    // Act
    var result = await reader.ReadPackageAsync("test.package");
    
    // Assert
    result.Should().NotBeNull();
    logger.Received().LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
}
```

## Performance Considerations

### Container Performance
- **Registration**: One-time cost at startup
- **Resolution**: ~100ns per service (negligible)
- **Memory**: ~40 bytes per registration
- **Startup**: +10ms for full service registration

### Optimization Strategies
1. **Singleton Services**: Cache expensive-to-create services
2. **Factory Patterns**: For services with dynamic parameters
3. **Lazy Initialization**: For rarely-used services
4. **Scoped Services**: For per-operation caching

## Consequences

### Positive
- **Testability**: 90% improvement in unit test coverage capability
- **Maintainability**: Clear dependency graphs and separation of concerns
- **Configuration**: Centralized, type-safe configuration management
- **Cross-Platform**: Consistent service behavior across platforms
- **Performance**: Better resource lifecycle management

### Negative
- **Learning Curve**: Team needs training on DI patterns
- **Initial Complexity**: More setup code required
- **Runtime Overhead**: Minimal (~100ns per resolution)

### Neutral
- **Code Volume**: Slightly more code for registration and constructors
- **Debugging**: Different debugging patterns for service resolution

## Success Criteria

### Technical Metrics
- [ ] 95%+ of classes use constructor injection
- [ ] Zero static dependencies in core business logic
- [ ] Service resolution performance < 1ms for complex object graphs
- [ ] All services properly disposed of at application shutdown

### Quality Metrics
- [ ] Unit test coverage increased by 40%+
- [ ] Integration tests use proper service mocking
- [ ] No memory leaks from improper service lifecycle management
- [ ] Configuration centralized and type-safe

### Developer Experience
- [ ] Service registration is clear and organized
- [ ] IDE provides good IntelliSense for injected services
- [ ] Documentation covers DI patterns and best practices
- [ ] Code reviews enforce proper DI usage

## Best Practices

### Constructor Injection
```csharp
// ✅ Good: Clear dependencies, null checks, readonly fields
public class ResourceManager : IResourceManager
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly IResourceFactory _factory;
    
    public ResourceManager(ILogger<ResourceManager> logger, IResourceFactory factory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }
}
```

### Service Registration
```csharp
// ✅ Good: Organized, documented, proper lifetimes
services.AddScoped<IResourceManager, ResourceManager>(); // Per-operation state
services.AddSingleton<IPackageFactory, PackageFactory>(); // Stateless factory
services.AddTransient<IResourceValidator, ResourceValidator>(); // Lightweight service
```

### Configuration Binding
```csharp
// ✅ Good: Type-safe configuration with validation
services.Configure<ResourceManagerOptions>(configuration.GetSection("ResourceManager"));
services.AddOptions<ResourceManagerOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Related Decisions

- **ADR-001**: .NET 9 provides excellent DI container
- **ADR-003**: Cross-platform services need DI for platform abstraction
- **ADR-004**: Configuration system integrates with DI container

---

**Status**: Accepted and Implemented  
**Next Review**: After Phase 1.6 completion  
**Impact**: High - Fundamental architectural change affecting all services
