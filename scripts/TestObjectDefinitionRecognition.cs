using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;
using TS4Tools.Extensions.ResourceTypes;

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

var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();

Console.WriteLine("Testing Object Definition Resource recognition...");

// Check if our resource type is registered in the registry
var isSupported = resourceTypeRegistry.IsSupported(0xC0DB5AE7);
var tag = resourceTypeRegistry.GetTag(0xC0DB5AE7);
var extension = resourceTypeRegistry.GetExtension(0xC0DB5AE7);

Console.WriteLine($"Resource Type 0xC0DB5AE7 supported: {isSupported}");
Console.WriteLine($"Tag: {tag}");
Console.WriteLine($"Extension: {extension}");

// Test with one specific package file if it exists
var testFile = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Data/Simulation/SimulationFullBuild0.package";
if (File.Exists(testFile))
{
    Console.WriteLine($"\nTesting with package: {Path.GetFileName(testFile)}");
    
    using var package = await packageFactory.LoadFromFileAsync(testFile, readOnly: true);
    Console.WriteLine($"Package loaded with {package.ResourceIndex.Count} resources");
    
    var objectDefResources = package.ResourceIndex.Where(r => r.ResourceType == 0xC0DB5AE7).ToList();
    Console.WriteLine($"Found {objectDefResources.Count} Object Definition Resources (0xC0DB5AE7)");
    
    if (objectDefResources.Count > 0)
    {
        var first = objectDefResources.First();
        Console.WriteLine($"  First one: TGI({first.ResourceType:X8}-{first.ResourceGroup:X8}-{first.Instance:X16})");
        Console.WriteLine($"  Size: {first.FileSize} bytes");
        
        // Try to get the resource to see if it can be loaded
        try
        {
            var resource = package.GetResource(first);
            Console.WriteLine($"  Successfully loaded: {resource?.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Failed to load: {ex.Message}");
        }
    }
}
else
{
    Console.WriteLine("Test package file not found. Registry test completed.");
}
