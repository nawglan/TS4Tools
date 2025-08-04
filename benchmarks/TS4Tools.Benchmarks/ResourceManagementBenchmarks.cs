using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;
using System.IO;

namespace TS4Tools.Benchmarks;

/// <summary>
/// Benchmarks comparing legacy vs modern resource management performance.
/// Focuses on memory allocation patterns and processing speed improvements.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[MarkdownExporter]
public class ResourceManagementBenchmarks
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IResourceManager _resourceManager;
    private readonly byte[] _testResourceData;
    private readonly Stream _testResourceStream;
    private readonly List<(uint Type, uint Group, ulong Instance)> _testKeys;
    
    public ResourceManagementBenchmarks()
    {
        // Setup dependency injection container for modern benchmarks
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IResourceManager, ResourceManager>();
        
        _serviceProvider = services.BuildServiceProvider();
        _resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
        
        // Create test data
        _testResourceData = GenerateTestResourceData(1024 * 1024); // 1MB test resource
        _testResourceStream = new MemoryStream(_testResourceData);
        
        // Generate test resource keys for bulk operations
        _testKeys = GenerateTestKeys(1000);
    }
    
    [Params(100, 1000, 10000)]
    public int ResourceCount { get; set; }
    
    [Benchmark(Baseline = true)]
    public void LegacyResourceCreation()
    {
        // Simulate legacy resource creation with string parsing and allocations
        var resources = new List<LegacyResourceSimulation>();
        
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            
            // Legacy pattern: string-based operations with allocations
            var resource = new LegacyResourceSimulation
            {
                ResourceType = $"0x{key.Type:X8}",           // String allocation
                GroupId = $"0x{key.Group:X8}",              // String allocation
                InstanceId = $"0x{key.Instance:X16}",       // String allocation
                Data = new byte[_testResourceData.Length]    // Array allocation
            };
            
            // Simulate parsing overhead
            resource.ParsedType = uint.Parse(resource.ResourceType[2..], System.Globalization.NumberStyles.HexNumber);
            resource.ParsedGroup = uint.Parse(resource.GroupId[2..], System.Globalization.NumberStyles.HexNumber);
            resource.ParsedInstance = ulong.Parse(resource.InstanceId[2..], System.Globalization.NumberStyles.HexNumber);
            
            Array.Copy(_testResourceData, resource.Data, _testResourceData.Length);
            resources.Add(resource);
        }
    }
    
    [Benchmark]
    public async Task ModernResourceCreation()
    {
        // Modern resource creation with minimal allocations
        var resources = new List<IResource>();
        
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            
            // Reset stream position for each iteration
            _testResourceStream.Position = 0;
            
            // Modern pattern: direct value types, no string parsing
            var resource = await _resourceManager.CreateResourceAsync(
                $"0x{key.Type:X8}", 
                1, // API version
                _testResourceStream);
            
            resources.Add(resource);
        }
    }
    
    [Benchmark(Baseline = true)]
    public void LegacyResourceLookup()
    {
        // Simulate legacy lookup with string-based dictionary
        var cache = new Dictionary<string, LegacyResourceSimulation>();
        
        // Populate cache
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            var tgiKey = $"0x{key.Type:X8}-0x{key.Group:X8}-0x{key.Instance:X16}";
            
            cache[tgiKey] = new LegacyResourceSimulation
            {
                ResourceType = $"0x{key.Type:X8}",
                GroupId = $"0x{key.Group:X8}",
                InstanceId = $"0x{key.Instance:X16}"
            };
        }
        
        // Perform lookups
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            var tgiKey = $"0x{key.Type:X8}-0x{key.Group:X8}-0x{key.Instance:X16}";
            _ = cache.TryGetValue(tgiKey, out var resource);
        }
    }
    
    [Benchmark]
    public void ModernResourceLookup()
    {
        // Modern lookup with value-type keys and pre-calculated hashes
        var cache = new Dictionary<ModernResourceKey, IResource>(ResourceCount);
        
        // Populate cache with value-type keys
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            var resourceKey = new ModernResourceKey(key.Type, key.Group, key.Instance);
            
            // Simulate resource (null for benchmark purposes)
            cache[resourceKey] = null!;
        }
        
        // Perform lookups with pre-calculated hash codes
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            var resourceKey = new ModernResourceKey(key.Type, key.Group, key.Instance);
            _ = cache.TryGetValue(resourceKey, out var resource);
        }
    }
    
    [Benchmark(Baseline = true)]
    public void LegacyBulkProcessing()
    {
        // Legacy pattern with blocking I/O and sequential processing
        var results = new List<ProcessedResource>();
        
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            
            // Simulate blocking I/O
            Thread.Sleep(1); // Simulate I/O delay
            
            // Simulate processing with string operations
            var processed = new ProcessedResource
            {
                Key = $"0x{key.Type:X8}-0x{key.Group:X8}-0x{key.Instance:X16}",
                ProcessedData = ProcessResourceData(_testResourceData),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };
            
            results.Add(processed);
        }
    }
    
    [Benchmark]
    public async Task ModernBulkProcessing()
    {
        // Modern pattern with async/await and concurrent processing
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount); // Limit concurrency
        var tasks = new List<Task<ProcessedResource>>();
        
        for (int i = 0; i < ResourceCount; i++)
        {
            var key = _testKeys[i % _testKeys.Count];
            
            tasks.Add(ProcessResourceAsync(key, semaphore));
        }
        
        var results = await Task.WhenAll(tasks);
    }
    
    private async Task<ProcessedResource> ProcessResourceAsync(
        (uint Type, uint Group, ulong Instance) key, 
        SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            // Simulate async I/O
            await Task.Delay(1);
            
            return new ProcessedResource
            {
                Key = $"0x{key.Type:X8}-0x{key.Group:X8}-0x{key.Instance:X16}",
                ProcessedData = ProcessResourceData(_testResourceData),
                Timestamp = DateTime.UtcNow.ToString("O") // ISO 8601 format
            };
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    private static byte[] ProcessResourceData(byte[] input)
    {
        // Simulate resource processing (simple transformation)
        var output = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ 0xAA); // Simple XOR transformation
        }
        return output;
    }
    
    private static byte[] GenerateTestResourceData(int size)
    {
        var random = new Random(42); // Fixed seed for reproducible benchmarks
        var data = new byte[size];
        random.NextBytes(data);
        return data;
    }
    
    private static List<(uint Type, uint Group, ulong Instance)> GenerateTestKeys(int count)
    {
        var random = new Random(42);
        var keys = new List<(uint, uint, ulong)>(count);
        
        for (int i = 0; i < count; i++)
        {
            keys.Add((
                Type: (uint)random.Next(0x10000000, 0x7FFFFFFF),
                Group: (uint)random.Next(0, 0x7FFFFFFF),
                Instance: ((ulong)random.Next() << 32) | (ulong)random.Next()
            ));
        }
        
        return keys;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _testResourceStream?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

// Simulation classes for benchmark comparison
internal class LegacyResourceSimulation
{
    public string ResourceType { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    // Parsed values (performance overhead)
    public uint ParsedType { get; set; }
    public uint ParsedGroup { get; set; }
    public ulong ParsedInstance { get; set; }
}

internal readonly struct ModernResourceKey : IEquatable<ModernResourceKey>
{
    private readonly uint _type;
    private readonly uint _group;
    private readonly ulong _instance;
    private readonly int _hashCode;
    
    public ModernResourceKey(uint type, uint group, ulong instance)
    {
        _type = type;
        _group = group;
        _instance = instance;
        _hashCode = HashCode.Combine(type, group, instance);
    }
    
    public uint Type => _type;
    public uint Group => _group;
    public ulong Instance => _instance;
    
    public bool Equals(ModernResourceKey other) =>
        _type == other._type && _group == other._group && _instance == other._instance;
    
    public override bool Equals(object? obj) =>
        obj is ModernResourceKey other && Equals(other);
    
    public override int GetHashCode() => _hashCode;
    
    public static bool operator ==(ModernResourceKey left, ModernResourceKey right) =>
        left.Equals(right);
    
    public static bool operator !=(ModernResourceKey left, ModernResourceKey right) =>
        !left.Equals(right);
}

internal class ProcessedResource
{
    public string Key { get; set; } = string.Empty;
    public byte[] ProcessedData { get; set; } = Array.Empty<byte>();
    public string Timestamp { get; set; } = string.Empty;
}
