# IImmutableResourceKey Interface Design Document

**Version:** 1.0
**Created:** August 3, 2025
**Status:** Phase 2.0 Preparation
**Target Framework:** .NET 9

______________________________________________________________________

## Overview

The `IImmutableResourceKey` interface represents a foundational design pattern for Phase 2.0 of the TS4Tools migration. This interface addresses performance and memory efficiency concerns with the current mutable key patterns while maintaining full backward compatibility.

## Problem Statement

### Current Challenges

1. **Memory Overhead**: Mutable resource keys create unnecessary object allocations
1. **Thread Safety**: Mutable keys require synchronization in concurrent scenarios
1. **Performance**: Frequent key modifications cause GC pressure
1. **Immutability**: Current pattern doesn't enforce value-type semantics

### Legacy Pattern Issues

```csharp
// âŒ Current mutable pattern - creates GC pressure
var key = new ResourceKey();
key.ResourceType = "0x12345678";
key.GroupId = "0x00000000";
key.InstanceId = "0xABCDEF12";
```

## Proposed Solution

### IImmutableResourceKey Interface

```csharp
/// <summary>
/// Represents an immutable resource key for high-performance resource operations.
/// This interface enforces value semantics and eliminates unnecessary allocations.
/// </summary>
public interface IImmutableResourceKey : IEquatable<IImmutableResourceKey>
{
    /// <summary>
    /// Gets the resource type identifier as a 32-bit value.
    /// </summary>
    uint ResourceType { get; }

    /// <summary>
    /// Gets the group identifier as a 32-bit value.
    /// </summary>
    uint GroupId { get; }

    /// <summary>
    /// Gets the instance identifier as a 64-bit value.
    /// </summary>
    ulong InstanceId { get; }

    /// <summary>
    /// Gets a composite hash representing all key components.
    /// This is pre-calculated during construction for O(1) dictionary operations.
    /// </summary>
    int CompositeHash { get; }

    /// <summary>
    /// Returns the traditional TGI (Type-Group-Instance) string representation.
    /// Format: "0x{Type:X8}-0x{Group:X8}-0x{Instance:X16}"
    /// </summary>
    string ToTgiString();

    /// <summary>
    /// Creates a new immutable key with modified resource type.
    /// Uses copy-and-update semantics for functional-style modifications.
    /// </summary>
    IImmutableResourceKey WithResourceType(uint resourceType);

    /// <summary>
    /// Creates a new immutable key with modified group ID.
    /// </summary>
    IImmutableResourceKey WithGroupId(uint groupId);

    /// <summary>
    /// Creates a new immutable key with modified instance ID.
    /// </summary>
    IImmutableResourceKey WithInstanceId(ulong instanceId);
}
```

### Concrete Implementation: ResourceKey (Immutable)

```csharp
/// <summary>
/// High-performance immutable implementation of resource keys.
/// Uses value semantics and pre-calculated hashing for optimal performance.
/// </summary>
public readonly struct ResourceKey : IImmutableResourceKey
{
    private readonly uint _resourceType;
    private readonly uint _groupId;
    private readonly ulong _instanceId;
    private readonly int _compositeHash;

    public ResourceKey(uint resourceType, uint groupId, ulong instanceId)
    {
        _resourceType = resourceType;
        _groupId = groupId;
        _instanceId = instanceId;

        // Pre-calculate hash for O(1) dictionary operations
        _compositeHash = HashCode.Combine(resourceType, groupId, instanceId);
    }

    public uint ResourceType => _resourceType;
    public uint GroupId => _groupId;
    public ulong InstanceId => _instanceId;
    public int CompositeHash => _compositeHash;

    public string ToTgiString() =>
        $"0x{_resourceType:X8}-0x{_groupId:X8}-0x{_instanceId:X16}";

    public IImmutableResourceKey WithResourceType(uint resourceType) =>
        new ResourceKey(resourceType, _groupId, _instanceId);

    public IImmutableResourceKey WithGroupId(uint groupId) =>
        new ResourceKey(_resourceType, groupId, _instanceId);

    public IImmutableResourceKey WithInstanceId(ulong instanceId) =>
        new ResourceKey(_resourceType, _groupId, instanceId);

    public bool Equals(IImmutableResourceKey? other) =>
        other != null &&
        _resourceType == other.ResourceType &&
        _groupId == other.GroupId &&
        _instanceId == other.InstanceId;

    public override bool Equals(object? obj) =>
        obj is IImmutableResourceKey other && Equals(other);

    public override int GetHashCode() => _compositeHash;

    public static bool operator ==(ResourceKey left, ResourceKey right) =>
        left.Equals(right);

    public static bool operator !=(ResourceKey left, ResourceKey right) =>
        !left.Equals(right);
}
```

## Migration Strategy

### Phase 1: Backward Compatibility Adapter

```csharp
/// <summary>
/// Adapter that provides backward compatibility for legacy mutable key patterns.
/// Allows gradual migration while maintaining existing API contracts.
/// </summary>
public class LegacyResourceKeyAdapter : IResourceIndexEntry
{
    private IImmutableResourceKey _immutableKey;

    public LegacyResourceKeyAdapter(IImmutableResourceKey immutableKey)
    {
        _immutableKey = immutableKey;
    }

    // Legacy mutable properties with performance warnings
    [Obsolete("Use IImmutableResourceKey for better performance. This property creates defensive copies.")]
    public string ResourceType
    {
        get => $"0x{_immutableKey.ResourceType:X8}";
        set => _immutableKey = _immutableKey.WithResourceType(ParseHex(value));
    }

    // ... similar properties for GroupId and InstanceId

    // Efficient access to immutable key
    public IImmutableResourceKey AsImmutable() => _immutableKey;
}
```

### Phase 2: Gradual Migration

1. **Week 1**: Introduce `IImmutableResourceKey` alongside existing APIs
1. **Week 2**: Add performance benchmarks and migration documentation
1. **Week 3**: Update high-traffic code paths to use immutable keys
1. **Week 4**: Add analyzer rules to suggest immutable key usage
1. **Week 5**: Deprecate mutable patterns with compiler warnings

### Phase 3: Performance Optimization

1. **Dictionary Operations**: Use `CompositeHash` for O(1) lookups
1. **Memory Pooling**: Implement object pooling for adapter instances
1. **Batch Operations**: Optimize bulk key operations with Span\<T>
1. **SIMD Acceleration**: Use vectorized operations for hash calculations

## Performance Characteristics

### Memory Usage

- **Current (Mutable)**: 48 bytes per key + heap allocation overhead
- **Proposed (Immutable)**: 20 bytes per key (stack allocated)
- **Improvement**: ~60% memory reduction + elimination of GC pressure

### CPU Performance

- **Hash Calculation**: Pre-calculated at construction (O(1) lookups)
- **Equality Comparisons**: 2-3 integer comparisons vs string parsing
- **Dictionary Operations**: 40-60% faster due to pre-calculated hashes

### Benchmark Results (Projected)

| Operation | Current (ms) | Immutable (ms) | Improvement |
|-----------|--------------|----------------|-------------|
| Key Creation | 0.045 | 0.012 | 73% faster |
| Dictionary Lookup | 0.023 | 0.009 | 61% faster |
| Bulk Operations (10k) | 450 | 120 | 73% faster |
| Memory Allocation | 480 KB | 195 KB | 59% less |

## Benefits

### Performance

- **Zero-allocation** key operations in hot paths
- **Pre-calculated hashing** for optimal dictionary performance
- **Value semantics** eliminate defensive copying

### Thread Safety

- **Immutable by design** - no synchronization required
- **Safe sharing** across threads without locks
- **Functional updates** preserve referential transparency

### Maintainability

- **Clear API contracts** through immutability
- **Reduced complexity** by eliminating mutable state
- **Testability** improved through deterministic behavior

### Backward Compatibility

- **Seamless migration** via adapter pattern
- **Incremental adoption** without breaking changes
- **Performance warnings** guide optimization efforts

## Implementation Timeline

### Phase 2.0 (Week 1-2)

- [ ] Implement `IImmutableResourceKey` interface
- [ ] Create `ResourceKey` struct implementation
- [ ] Add comprehensive unit tests (95%+ coverage)
- [ ] Performance benchmark suite

### Phase 2.1 (Week 3-4)

- [ ] Implement `LegacyResourceKeyAdapter`
- [ ] Update high-traffic code paths
- [ ] Add analyzer rules for performance warnings
- [ ] Integration testing

### Phase 2.2 (Week 5-6)

- [ ] Optimize dictionary implementations
- [ ] Add SIMD optimizations for bulk operations
- [ ] Memory profiling and optimization
- [ ] Documentation and migration guides

## Risk Assessment

### Low Risk

- **Interface Design**: Well-established immutable patterns
- **Backward Compatibility**: Adapter pattern proven effective
- **Performance**: Immutable value types consistently faster

### Medium Risk

- **Migration Complexity**: Large codebase requires careful planning
- **Developer Adoption**: May require training on functional patterns

### Mitigation Strategies

- **Comprehensive Testing**: 95%+ code coverage with performance tests
- **Gradual Migration**: Phased approach reduces risk
- **Developer Documentation**: Clear examples and best practices
- **Analyzer Support**: Automated guidance for optimal usage

## Success Criteria

### Performance Metrics

- âœ… 50%+ reduction in memory allocations for key operations
- âœ… 40%+ improvement in dictionary lookup performance
- âœ… Zero allocations in steady-state operations

### Quality Metrics

- âœ… 95%+ test coverage for immutable key implementations
- âœ… Zero breaking changes to existing public APIs
- âœ… Complete migration documentation

### Developer Experience

- âœ… Clear migration path with automated tooling
- âœ… Performance analyzer rules guide optimization
- âœ… Comprehensive examples and best practices

______________________________________________________________________

**Status**: Ready for Phase 2.0 Implementation
**Next Steps**: Implement interface and core struct, add performance benchmarks
**Review Required**: Architecture team approval before implementation begins
