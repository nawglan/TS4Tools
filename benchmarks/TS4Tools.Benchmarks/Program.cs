using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text;
using TS4Tools.Core.System.Collections;
using TS4Tools.Core.System.Hashing;

namespace TS4Tools.Benchmarks;

/// <summary>
/// Entry point for running benchmarks
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

/// <summary>
/// Benchmarks for AHandlerDictionary performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AHandlerDictionaryBenchmarks
{
    private TestDictionary _dictionary = null!;
    private Dictionary<int, string> _standardDictionary = null!;
    private readonly int[] _keys = Enumerable.Range(0, 1000).ToArray();
    
    private class TestDictionary : AHandlerDictionary<int, string>
    {
        public TestDictionary(EventHandler? handler) : base(handler) { }
    }
    
    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new TestDictionary((_, _) => { });
        _standardDictionary = new Dictionary<int, string>();
        
        // Pre-populate dictionaries
        for (int i = 0; i < 1000; i++)
        {
            _dictionary[i] = $"Value{i}";
            _standardDictionary[i] = $"Value{i}";
        }
    }
    
    [Benchmark]
    public void AHandlerDictionary_Add()
    {
        var dict = new TestDictionary((_, _) => { });
        for (int i = 0; i < 1000; i++)
        {
            dict.Add(i, $"Value{i}");
        }
    }
    
    [Benchmark]
    public void StandardDictionary_Add()
    {
        var dict = new Dictionary<int, string>();
        for (int i = 0; i < 1000; i++)
        {
            dict.Add(i, $"Value{i}");
        }
    }
    
    [Benchmark]
    public void AHandlerDictionary_Lookup()
    {
        foreach (var key in _keys)
        {
            _ = _dictionary[key];
        }
    }
    
    [Benchmark]
    public void StandardDictionary_Lookup()
    {
        foreach (var key in _keys)
        {
            _ = _standardDictionary[key];
        }
    }
}

/// <summary>
/// Benchmarks for FNV hash algorithm performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class FnvHashBenchmarks
{
    private readonly byte[] _smallData = "Hello World"u8.ToArray();
    private readonly byte[] _mediumData = new byte[1024];
    private readonly byte[] _largeData = new byte[64 * 1024];
    
    [GlobalSetup]
    public void Setup()
    {
        Random.Shared.NextBytes(_mediumData);
        Random.Shared.NextBytes(_largeData);
    }
    
    [Benchmark]
    public uint FNV32_SmallData() => Fnv32.GetHash(Encoding.UTF8.GetString(_smallData));
    
    [Benchmark]
    public uint FNV32_MediumData() => Fnv32.GetHash(Encoding.UTF8.GetString(_mediumData));
    
    [Benchmark]
    public uint FNV32_LargeData() => Fnv32.GetHash(Encoding.UTF8.GetString(_largeData));
    
    [Benchmark]
    public ulong FNV64_SmallData() => Fnv64.GetHash(Encoding.UTF8.GetString(_smallData));
    
    [Benchmark]
    public ulong FNV64_MediumData() => Fnv64.GetHash(Encoding.UTF8.GetString(_mediumData));
    
    [Benchmark]
    public ulong FNV64_LargeData() => Fnv64.GetHash(Encoding.UTF8.GetString(_largeData));
}
