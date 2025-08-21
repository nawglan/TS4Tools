using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Resources;
using TS4Tools.Extensions.ResourceTypes;
using TS4Tools.Resources.Catalog;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection()
    .Build();

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddTS4ToolsCore(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
    })
    .Build();

// Test 1: ResourceTypeRegistry recognition
var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();
Console.WriteLine("=== ResourceTypeRegistry Test ===");
Console.WriteLine($"0xC0DB5AE7 supported: {resourceTypeRegistry.IsSupported(0xC0DB5AE7)}");
Console.WriteLine($"Tag: {resourceTypeRegistry.GetTag(0xC0DB5AE7)}");
Console.WriteLine($"Extension: {resourceTypeRegistry.GetExtension(0xC0DB5AE7)}");

// Test 2: Factory registration and functionality
var factory = host.Services.GetService<ObjectDefinitionResourceFactory>();
Console.WriteLine("\n=== Factory Test ===");
Console.WriteLine($"ObjectDefinitionResourceFactory found: {factory != null}");

if (factory != null)
{
    Console.WriteLine($"Supported types: {string.Join(", ", factory.SupportedResourceTypes)}");
    Console.WriteLine($"Can handle 0xC0DB5AE7: {factory.CanCreateResource(0xC0DB5AE7)}");
    Console.WriteLine($"Priority: {factory.Priority}");
    
    // Test creating a resource
    try
    {
        var resource = factory.CreateEmptyResource(0xC0DB5AE7);
        Console.WriteLine($"Successfully created: {resource.GetType().Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to create resource: {ex.Message}");
    }
}

// Test 3: Generic IResourceFactory discovery
var allFactories = host.Services.GetServices<IResourceFactory>().ToList();
Console.WriteLine($"\n=== Factory Discovery Test ===");
Console.WriteLine($"Total factories found: {allFactories.Count}");

var objDefFactories = allFactories.Where(f => f.SupportedResourceTypes.Contains("0xC0DB5AE7")).ToList();
Console.WriteLine($"Factories that support 0xC0DB5AE7: {objDefFactories.Count}");

foreach (var f in objDefFactories)
{
    Console.WriteLine($"  - {f.GetType().Name} (Priority: {f.Priority})");
}

Console.WriteLine("\n=== Summary ===");
bool registryOk = resourceTypeRegistry.IsSupported(0xC0DB5AE7);
bool factoryOk = factory != null && factory.CanCreateResource(0xC0DB5AE7);
bool discoveryOk = objDefFactories.Count > 0;

Console.WriteLine($"✓ ResourceTypeRegistry: {(registryOk ? "PASS" : "FAIL")}");
Console.WriteLine($"✓ Factory Creation: {(factoryOk ? "PASS" : "FAIL")}");
Console.WriteLine($"✓ Factory Discovery: {(discoveryOk ? "PASS" : "FAIL")}");

if (registryOk && factoryOk && discoveryOk)
{
    Console.WriteLine("\n🎉 All tests PASSED! Object Definition Resources should now be recognized as known instead of unknown in PackageAnalysisScript.");
}
else
{
    Console.WriteLine("\n❌ Some tests FAILED. There may still be issues with the implementation.");
}
