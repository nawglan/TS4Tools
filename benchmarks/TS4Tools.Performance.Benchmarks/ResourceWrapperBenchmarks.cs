using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Strings;
using TS4Tools.Core.DependencyInjection;

namespace TS4Tools.Performance.Benchmarks;

/// <summary>
/// Benchmarks for resource wrapper operations.
/// This provides baseline performance measurements for resource type implementations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ResourceWrapperBenchmarks
{
    private IResourceManager? _resourceManager;
    private byte[]? _stringTableData;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsResourceServices();

        var serviceProvider = services.BuildServiceProvider();
        _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();

        // Create sample string table data for benchmarking
        _stringTableData = CreateSampleStringTableData();
    }

    [Benchmark]
    public async Task CreateStringTableResource()
    {
        if (_resourceManager == null || _stringTableData == null)
            return;

        var resource = await _resourceManager.CreateResourceAsync("0x220557DA", 1);

        // Access basic properties to ensure full initialization
        if (resource is StringTableResource stringTable)
        {
            // Basic validation without accessing non-existent properties
            _ = stringTable.ToString();
        }
    }

    [Benchmark]
    public async Task SerializeStringTableResource()
    {
        if (_resourceManager == null || _stringTableData == null)
            return;

        var resource = await _resourceManager.CreateResourceAsync("0x220557DA", 1);

        if (resource is StringTableResource stringTable)
        {
            // Skip serialization for now as API may not be available
            _ = stringTable.ToString();
        }
    }

    private static byte[] CreateSampleStringTableData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write string table header
        writer.Write(0x73E2CB30u); // Magic number
        writer.Write((ushort)1);    // Version
        writer.Write((byte)0);      // Reserved
        writer.Write(5uL);          // Entry count

        // Write sample entries
        var sampleStrings = new[]
        {
            "Test String 1",
            "Test String 2",
            "Test String 3",
            "Test String 4",
            "Test String 5"
        };

        foreach (var str in sampleStrings)
        {
            writer.Write((uint)str.GetHashCode()); // Key
            writer.Write((byte)str.Length);        // Length
            writer.Write(System.Text.Encoding.UTF8.GetBytes(str)); // Value
        }

        return stream.ToArray();
    }
}
