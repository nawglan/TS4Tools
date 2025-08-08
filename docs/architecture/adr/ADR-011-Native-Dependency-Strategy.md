# ADR-011: Native Dependency Strategy (Hybrid Approach)

**Status:** Accepted  
**Date:** August 8, 2025  
**Deciders:** Architecture Team, Platform Team, Performance Team

## Context

The TS4Tools migration faces significant challenges with native dependencies that were tightly coupled to Windows in the legacy Sims4Tools codebase. Key areas requiring native functionality include:

1. **DDS Compression**: DirectDrawSurface format compression/decompression for textures
2. **3D Graphics Rendering**: Complex 3D model visualization and manipulation
3. **Audio Processing**: Specialized audio format handling for Sims 4 audio resources
4. **Platform-Specific File Operations**: Windows registry, file associations, system integration

The challenge is balancing cross-platform compatibility with performance requirements and development complexity.

## Decision

We will implement a **hybrid native dependency strategy** that combines managed fallbacks with optimized native implementations where beneficial:

1. **Tier 1 (Critical)**: DDS compression - Windows native + managed fallback
2. **Tier 2 (Important)**: 3D graphics - Avalonia + Silk.NET OR maintain WinForms on Windows
3. **Tier 3 (Optional)**: Audio processing - managed-only with format limitations
4. **Tier 4 (Platform)**: System integration - platform-abstraction layer

## Rationale

### Requirements Analysis

#### Performance Requirements
- DDS operations must be fast enough for interactive use (< 500ms for typical files)
- 3D rendering needs 30+ FPS for smooth manipulation
- Audio preview should have minimal latency

#### Compatibility Requirements
- Must run on Windows 10/11, macOS 12+, Linux (Ubuntu 20+)
- Graceful degradation when native components unavailable
- No broken functionality on any supported platform

#### Maintenance Requirements
- Minimize platform-specific code maintenance
- Reduce dependency on unmaintained native libraries
- Simplify deployment and distribution

### Alternative Approaches Evaluated

#### 1. Pure Managed Approach (Rejected)
- **Pros**: Maximum compatibility, no native dependencies
- **Cons**: Poor performance for critical operations, missing functionality
- **Impact**: Unacceptable performance degradation for DDS operations

#### 2. Native-First Approach (Rejected) 
- **Pros**: Maximum performance, full feature parity
- **Cons**: Complex cross-platform maintenance, deployment issues
- **Impact**: Significant development and maintenance overhead

#### 3. Platform-Specific Builds (Rejected)
- **Pros**: Optimal performance per platform
- **Cons**: Multiple codebases, complex release management
- **Impact**: Unsustainable maintenance burden

#### 4. Hybrid Strategy (Selected)
- **Pros**: Performance where needed, compatibility everywhere
- **Cons**: Implementation complexity, testing matrix expansion
- **Impact**: Balanced approach meeting all requirements

## Architecture Design

### Abstraction Layer Pattern

```csharp
public interface IDDSCompressionService
{
    Task<byte[]> CompressAsync(byte[] imageData, DDSFormat format);
    Task<byte[]> DecompressAsync(byte[] ddsData);
    bool IsNativeAccelerationAvailable { get; }
    string ImplementationDetails { get; }
}

// Windows: Native squishinterface.dll wrapper
// Other platforms: Managed BCnEncoder implementation
```

### Service Registration Strategy

```csharp
public static class NativeDependencyConfiguration
{
    public static IServiceCollection AddNativeDependencies(this IServiceCollection services)
    {
        // DDS Compression - Tier 1 (Critical)
        if (OperatingSystem.IsWindows() && NativeLibrary.TryLoad("squishinterface_x64.dll", out _))
        {
            services.AddSingleton<IDDSCompressionService, NativeDDSCompressionService>();
        }
        else
        {
            services.AddSingleton<IDDSCompressionService, ManagedDDSCompressionService>();
        }
        
        // 3D Graphics - Tier 2 (Important)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Evaluate: Keep WinForms ModelViewer OR migrate to Silk.NET
            services.AddTransient<I3DModelViewer, WindowsModelViewer>();
        }
        else
        {
            services.AddTransient<I3DModelViewer, AvaloniaModelViewer>();
        }
        
        return services;
    }
}
```

## Implementation Strategy by Tier

### Tier 1: DDS Compression (Critical)

#### Windows Implementation
```csharp
public class NativeDDSCompressionService : IDDSCompressionService
{
    [DllImport("squishinterface_x64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CompressImage(byte[] rgba, int width, int height, byte[] output, int flags);
    
    public async Task<byte[]> CompressAsync(byte[] imageData, DDSFormat format)
    {
        // Use native squish library for optimal performance
        // Same performance as legacy Sims4Tools
    }
    
    public bool IsNativeAccelerationAvailable => true;
}
```

#### Cross-Platform Fallback
```csharp
public class ManagedDDSCompressionService : IDDSCompressionService  
{
    private readonly BCnEncoder _encoder = new BCnEncoder();
    
    public async Task<byte[]> CompressAsync(byte[] imageData, DDSFormat format)
    {
        // Use BCnEncoder managed library
        // 2-3x slower but fully functional
    }
    
    public bool IsNativeAccelerationAvailable => false;
}
```

### Tier 2: 3D Graphics (Important)

#### Decision Matrix for 3D Graphics
| Option | Performance | Cross-Platform | Maintenance | Decision |
|--------|-------------|----------------|-------------|----------|
| Keep WinForms on Windows | ✅ Excellent | ❌ Windows-only | ✅ No changes | 🤔 Evaluate |
| Silk.NET + Avalonia | ⚠️ Good | ✅ Full | ⚠️ New dependency | 🎯 Preferred |
| Pure Avalonia 3D | ⚠️ Limited | ✅ Full | ✅ Minimal | ⚠️ Backup |

#### Implementation Approach
```csharp
// Phase 1: Abstract the interface
public interface I3DModelViewer
{
    Task LoadModelAsync(Stream modelData);
    void SetCamera(Vector3 position, Vector3 target);
    Task<byte[]> CaptureScreenshotAsync();
}

// Phase 2: Platform implementations
#if WINDOWS
public class WindowsModelViewer : I3DModelViewer { /* WinForms-based */ }
#endif

public class AvaloniaModelViewer : I3DModelViewer { /* Silk.NET + Avalonia */ }
```

### Tier 3: Audio Processing (Optional)

#### Managed-Only Approach
```csharp
public class ManagedAudioService : IAudioService
{
    // Use NAudio for Windows, cross-platform alternatives for others
    // Accept format limitations on non-Windows platforms
    // Focus on core functionality rather than perfect compatibility
}
```

### Tier 4: System Integration (Platform-Specific)

#### Platform Abstraction
```csharp
public interface ISystemIntegrationService
{
    Task<bool> RegisterFileAssociationsAsync(string[] extensions);
    Task<string[]> GetInstalledGamePathsAsync();
    Task<bool> IsElevatedAsync();
}

// Platform-specific implementations with graceful degradation
```

## Deployment Strategy

### Native Library Management

#### Windows Deployment
```xml
<!-- In .csproj files -->
<ItemGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
  <NativeLibrary Include="native\win-x64\squishinterface_x64.dll" />
  <NativeLibrary Include="native\win-x64\squishinterface_Win32.dll" />
</ItemGroup>
```

#### Cross-Platform Deployment
```csharp
public class NativeLibraryLoader
{
    public static bool TryLoadOptimizedLibrary(string libraryName)
    {
        var architecture = RuntimeInformation.OSArchitecture;
        var platform = GetPlatformIdentifier();
        
        var libraryPath = Path.Combine("native", platform, architecture.ToString(), libraryName);
        return NativeLibrary.TryLoad(libraryPath, out _);
    }
}
```

### Feature Detection and Graceful Degradation

```csharp
public class CapabilityDetectionService
{
    public ApplicationCapabilities DetectCapabilities()
    {
        return new ApplicationCapabilities
        {
            HasNativeDDSCompression = CanLoadDDSLibrary(),
            Has3DModelViewer = CanInitialize3DRenderer(),
            HasAudioPlayback = CanInitializeAudio(),
            SupportsFileAssociations = CanRegisterFileTypes()
        };
    }
}
```

## Performance Considerations

### Benchmarking Requirements
```csharp
[MemoryDiagnoser]
public class DDSCompressionBenchmark
{
    [Benchmark]
    public byte[] NativeCompression() => _nativeService.Compress(_testImage);
    
    [Benchmark]  
    public byte[] ManagedCompression() => _managedService.Compress(_testImage);
    
    // Target: Native should be < 2x performance difference from managed
}
```

### Performance Targets
- **DDS Compression**: Managed implementation must be within 3x performance of native
- **3D Rendering**: 30+ FPS for basic model viewing
- **Memory Usage**: No more than 20% overhead for abstraction layers

## Migration and Testing Strategy

### Phase 1: Abstraction Implementation (Week 1)
- Create service interfaces for all native dependencies
- Implement Windows native versions (existing functionality)
- Create basic managed fallbacks

### Phase 2: Cross-Platform Implementation (Week 2-3)
- Implement managed DDS compression using BCnEncoder
- Evaluate 3D graphics options (Silk.NET vs. maintained WinForms)
- Test audio processing alternatives

### Phase 3: Integration and Optimization (Week 4)
- Performance testing and optimization
- Deployment packaging for multiple platforms
- User experience testing on all platforms

### Testing Matrix
| Platform | Native DDS | Managed DDS | 3D Graphics | Audio |
|----------|------------|-------------|-------------|-------|
| Windows 11 x64 | ✅ | ✅ | ✅ | ✅ |
| Windows 10 x64 | ✅ | ✅ | ✅ | ✅ |
| macOS Intel | ❌ | ✅ | ✅ | ⚠️ |
| macOS ARM | ❌ | ✅ | ✅ | ⚠️ |
| Ubuntu 20+ | ❌ | ✅ | ✅ | ⚠️ |

## Risk Management

### Risk: Performance Degradation on Non-Windows
- **Mitigation**: Benchmark managed implementations, optimize hotpaths
- **Acceptance Criteria**: < 3x performance penalty for critical operations
- **Fallback**: Document performance differences, provide user choice

### Risk: Complex Build and Deployment
- **Mitigation**: Automate platform-specific packaging, comprehensive CI/CD testing
- **Acceptance Criteria**: Single command deployment for all platforms
- **Fallback**: Platform-specific installation guides

### Risk: Feature Parity Issues
- **Mitigation**: Comprehensive feature matrix testing, user documentation
- **Acceptance Criteria**: Core functionality works on all platforms
- **Fallback**: Platform-specific feature documentation

## Benefits

### User Benefits
- **Cross-Platform Support**: TS4Tools works on user's preferred OS
- **Performance**: Native acceleration where available, functional everywhere
- **Reliability**: Graceful degradation prevents crashes from missing dependencies

### Developer Benefits
- **Maintainability**: Clear abstraction boundaries, testable components
- **Flexibility**: Can optimize per-platform without affecting other platforms
- **Future-Proofing**: Easy to add new native implementations or remove old ones

### Operational Benefits
- **Deployment Simplicity**: Automated detection and fallback
- **Support Reduction**: Clear capability reporting helps with user issues
- **Monitoring**: Track which implementations are used in the field

## Success Metrics

1. **Performance**: Native implementations within 10% of legacy performance
2. **Compatibility**: 95%+ feature functionality on all target platforms  
3. **Reliability**: < 1% crash rate related to native dependency issues
4. **User Satisfaction**: No user complaints about missing critical functionality

## Implementation Status

### Completed
- ✅ Architecture design and interfaces defined
- ✅ Native DDS compression wrapper (Windows)

### In Progress  
- 🚧 Managed DDS compression implementation (BCnEncoder)
- 🚧 3D graphics evaluation (Silk.NET vs WinForms)

### Planned
- ⏳ Audio processing abstraction
- ⏳ Platform integration services
- ⏳ Comprehensive testing matrix

## Consequences

### Positive
- ✅ True cross-platform functionality with performance optimization
- ✅ Graceful degradation maintains usability on all platforms
- ✅ Clear upgrade path as native libraries become available
- ✅ Reduced risk from native dependency issues

### Negative  
- ❌ Implementation complexity with multiple code paths
- ❌ Testing overhead for platform/implementation combinations
- ❌ Deployment complexity with conditional native libraries
- ❌ Documentation overhead for platform-specific differences

### Neutral
- 📋 Performance monitoring required to validate approach
- 📋 User education about platform-specific capabilities
- 📋 Regular evaluation of native vs managed performance

## Related Decisions

- ADR-001: .NET 9 Framework (enables modern native interop patterns)
- ADR-003: Avalonia Cross-Platform UI (affects 3D graphics implementation choice)
- ADR-010: Feature Flag Architecture (enables platform-specific feature toggles)
- ADR-008: Cross-Platform File Format Compatibility (validates approach)

---

**Implementation Status:** 🚧 **IN PROGRESS** - Architecture complete, implementations underway  
**Review Date:** September 8, 2025  
**Document Owner:** Architecture Team, Platform Team
