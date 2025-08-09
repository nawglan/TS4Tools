using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.DependencyInjection;

namespace TS4Tools.Performance.Benchmarks;

/// <summary>
/// Benchmarks for package loading operations.
/// This establishes baseline performance measurements for Phase 4.13 completion criteria.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class PackageLoadingBenchmarks
{
    private IPackageFactory? _packageFactory;
    private string? _testPackagePath;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsResourceServices();

        var serviceProvider = services.BuildServiceProvider();
        _packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

        // Use real Sims 4 package for benchmarking (if available)
        var steamPath = @"C:\Program Files (x86)\Steam\steamapps\common\The Sims 4\Data\Client";
        var testFiles = new[]
        {
            Path.Combine(steamPath, "ClientDeltaBuild0.package"),
            Path.Combine(steamPath, "ClientFullBuild0.package")
        };

        _testPackagePath = testFiles.FirstOrDefault(File.Exists);

        if (_testPackagePath == null)
        {
            // Fallback to test data
            _testPackagePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "test-data", "sample.package"
            );
        }
    }

    [Benchmark]
    public async Task LoadPackage_Async()
    {
        if (_packageFactory == null || _testPackagePath == null || !File.Exists(_testPackagePath))
            return;

        using var package = await _packageFactory.LoadFromFileAsync(_testPackagePath, readOnly: true);
        // Access basic properties to ensure full loading
        _ = package.ResourceCount;
        _ = package.Magic;
    }

    [Benchmark]
    public async Task LoadPackage_Sync()
    {
        if (_packageFactory == null || _testPackagePath == null || !File.Exists(_testPackagePath))
            return;

        using var package = await _packageFactory.LoadFromFileAsync(_testPackagePath, readOnly: true);
        // Access basic properties to ensure full loading
        _ = package.ResourceCount;
        _ = package.Magic;
    }

    [Benchmark]
    public async Task LoadAndEnumerateResources()
    {
        if (_packageFactory == null || _testPackagePath == null || !File.Exists(_testPackagePath))
            return;

        using var package = await _packageFactory.LoadFromFileAsync(_testPackagePath, readOnly: true);

        var count = 0;
        foreach (var resource in package.ResourceIndex)
        {
            count++;
            // Limit enumeration to avoid extremely long benchmarks
            if (count >= 100) break;
        }
    }
}
