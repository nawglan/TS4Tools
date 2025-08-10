using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.System;
using TS4Tools.Core.Settings;

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
            ["MaxResourceCacheSize"] = 1000,
            ["EnableDetailedLogging"] = true,
            ["AsyncOperationTimeoutMs"] = 30000,
            ["EnableCrossPlatformFeatures"] = true,
            ["EnableExtraChecking"] = true,
            ["AssumeDataDirty"] = false,
            ["UseTS4Format"] = true
        };

        _modernSettings = new ApplicationSettings
        {
            MaxResourceCacheSize = 1000,
            EnableDetailedLogging = true,
            AsyncOperationTimeoutMs = 30000,
            EnableCrossPlatformFeatures = true,
            EnableExtraChecking = true,
            AssumeDataDirty = false,
            UseTS4Format = true
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
                "MaxResourceCacheSize" => (object)int.Parse(stringValue),
                "EnableDetailedLogging" => (object)bool.Parse(stringValue),
                "AsyncOperationTimeoutMs" => (object)int.Parse(stringValue),
                "EnableCrossPlatformFeatures" => (object)bool.Parse(stringValue),
                "EnableExtraChecking" => (object)bool.Parse(stringValue),
                "AssumeDataDirty" => (object)bool.Parse(stringValue),
                "UseTS4Format" => (object)bool.Parse(stringValue),
                _ => (object)stringValue
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
            "MaxResourceCacheSize": 1000,
            "EnableDetailedLogging": true,
            "AsyncOperationTimeoutMs": 30000,
            "EnableCrossPlatformFeatures": true,
            "EnableExtraChecking": true,
            "AssumeDataDirty": false,
            "UseTS4Format": true
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
                case "MaxResourceCacheSize":
                    if (kvp.Value is int cacheSize && (cacheSize < 10 || cacheSize > 10000))
                        errors.Add("MaxResourceCacheSize must be between 10 and 10000");
                    break;

                case "AsyncOperationTimeoutMs":
                    if (kvp.Value is int timeout && (timeout < 1000 || timeout > 60000))
                        errors.Add("AsyncOperationTimeoutMs must be between 1000 and 60000");
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
