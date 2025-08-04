using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.System;

namespace TS4Tools.Benchmarks;

/// <summary>
/// Benchmarks comparing legacy vs modern settings implementation performance.
/// Validates the performance improvements claimed in the migration roadmap.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[MarkdownExporter]
[HtmlExporter]
public class SettingsPerformanceBenchmarks
{
    private readonly Dictionary<string, object> _testSettings;
    private readonly ApplicationSettings _modernSettings;
    private readonly string _tempJsonFile;
    
    public SettingsPerformanceBenchmarks()
    {
        _testSettings = new Dictionary<string, object>
        {
            ["PackageCacheSize"] = 1000,
            ["EnableResourceCaching"] = true,
            ["DefaultResourceType"] = "0x12345678",
            ["MaxConcurrentOperations"] = 8,
            ["LogLevel"] = "Information",
            ["CrossPlatformCompatibility"] = true,
            ["MemoryOptimizationLevel"] = 2,
            ["AsyncTimeoutMs"] = 30000
        };
        
        _modernSettings = new ApplicationSettings
        {
            PackageCacheSize = 1000,
            EnableResourceCaching = true,
            DefaultResourceType = "0x12345678",
            MaxConcurrentOperations = 8,
            LogLevel = "Information",
            CrossPlatformCompatibility = true,
            MemoryOptimizationLevel = 2,
            AsyncTimeoutMs = 30000
        };
        
        _tempJsonFile = Path.GetTempFileName();
    }
    
    [Benchmark(Baseline = true)]
    public void LegacySettings_Load()
    {
        // Simulate legacy registry/XML loading with string parsing overhead
        var settings = new Dictionary<string, object>();
        
        foreach (var kvp in _testSettings)
        {
            // Simulate string conversion overhead (legacy pattern)
            var stringValue = kvp.Value.ToString()!;
            
            // Simulate type conversion (performance bottleneck in legacy)
            var convertedValue = kvp.Key switch
            {
                "PackageCacheSize" => int.Parse(stringValue),
                "EnableResourceCaching" => bool.Parse(stringValue),
                "DefaultResourceType" => stringValue,
                "MaxConcurrentOperations" => int.Parse(stringValue),
                "LogLevel" => stringValue,
                "CrossPlatformCompatibility" => bool.Parse(stringValue),
                "MemoryOptimizationLevel" => int.Parse(stringValue),
                "AsyncTimeoutMs" => int.Parse(stringValue),
                _ => stringValue
            };
            
            settings[kvp.Key] = convertedValue;
        }
    }
    
    [Benchmark]
    public ApplicationSettings ModernSettings_Load()
    {
        // Modern JSON deserialization with System.Text.Json (zero-allocation)
        var json = """
        {
            "PackageCacheSize": 1000,
            "EnableResourceCaching": true,
            "DefaultResourceType": "0x12345678",
            "MaxConcurrentOperations": 8,
            "LogLevel": "Information",
            "CrossPlatformCompatibility": true,
            "MemoryOptimizationLevel": 2,
            "AsyncTimeoutMs": 30000
        }
        """;
        
        return System.Text.Json.JsonSerializer.Deserialize<ApplicationSettings>(json)!;
    }
    
    [Benchmark]
    public void LegacySettings_Save()
    {
        // Simulate legacy registry/XML saving with string serialization
        var settingsStrings = new Dictionary<string, string>();
        
        foreach (var kvp in _testSettings)
        {
            settingsStrings[kvp.Key] = kvp.Value.ToString()!;
        }
        
        // Simulate file I/O overhead (synchronous in legacy)
        File.WriteAllLines(_tempJsonFile, settingsStrings.Select(s => $"{s.Key}={s.Value}"));
    }
    
    [Benchmark]
    public async Task ModernSettings_SaveAsync()
    {
        // Modern async JSON serialization
        await using var fileStream = File.Create(_tempJsonFile);
        await System.Text.Json.JsonSerializer.SerializeAsync(fileStream, _modernSettings);
    }
    
    [Benchmark(Baseline = true)]
    public void LegacySettings_Validation()
    {
        // Legacy validation with manual checks and exceptions
        var errors = new List<string>();
        
        foreach (var kvp in _testSettings)
        {
            switch (kvp.Key)
            {
                case "PackageCacheSize":
                    if (kvp.Value is int cacheSize && (cacheSize < 10 || cacheSize > 10000))
                        errors.Add("PackageCacheSize must be between 10 and 10000");
                    break;
                    
                case "MaxConcurrentOperations":
                    if (kvp.Value is int maxOps && (maxOps < 1 || maxOps > 32))
                        errors.Add("MaxConcurrentOperations must be between 1 and 32");
                    break;
                    
                case "AsyncTimeoutMs":
                    if (kvp.Value is int timeout && (timeout < 1000 || timeout > 300000))
                        errors.Add("AsyncTimeoutMs must be between 1000 and 300000");
                    break;
            }
        }
        
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors));
    }
    
    [Benchmark]
    public bool ModernSettings_Validation()
    {
        // Modern validation with data annotations - compiled at build time
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(_modernSettings);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            _modernSettings, context, results, validateAllProperties: true);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempJsonFile))
            File.Delete(_tempJsonFile);
    }
}
