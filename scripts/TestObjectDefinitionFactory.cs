using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Resources.Catalog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddTS4ToolsCore();
    })
    .Build();

var serviceProvider = host.Services;

// Test 1: Check if factory is registered
var factory = serviceProvider.GetService<ObjectDefinitionResourceFactory>();
Console.WriteLine($"ObjectDefinitionResourceFactory found: {factory != null}");

if (factory != null)
{
    Console.WriteLine($"Supported resource types: {string.Join(", ", factory.SupportedResourceTypes)}");
    Console.WriteLine($"Resource type IDs: {string.Join(", ", factory.ResourceTypes.Select(x => $"0x{x:X8}"))}");
    Console.WriteLine($"Priority: {factory.Priority}");
    
    // Test 2: Check if it can handle our resource type
    var canHandle = factory.CanCreateResource(0xC0DB5AE7);
    Console.WriteLine($"Can handle 0xC0DB5AE7: {canHandle}");
}

// Test 3: Check resource factory registry
var allFactories = serviceProvider.GetServices<IResourceFactory>().ToList();
Console.WriteLine($"Total registered factories: {allFactories.Count}");

foreach (var resourceFactory in allFactories)
{
    if (resourceFactory.SupportedResourceTypes.Contains("0xC0DB5AE7"))
    {
        Console.WriteLine($"Factory for 0xC0DB5AE7: {resourceFactory.GetType().Name}");
    }
}

// Test 4: Try to create empty resource
try
{
    if (factory != null)
    {
        var emptyResource = factory.CreateEmptyResource(0xC0DB5AE7);
        Console.WriteLine($"Created empty resource: {emptyResource?.GetType().Name}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating empty resource: {ex.Message}");
}
