// Simple test to check if Object Definition Resource parsing works
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Resources.Catalog;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddTS4ToolsCore();

var provider = services.BuildServiceProvider();
var factory = provider.GetService<ObjectDefinitionResourceFactory>();

Console.WriteLine($"Factory found: {factory != null}");

if (factory != null)
{
    Console.WriteLine("Factory is registered and available");
    
    // Test creating an empty resource
    try
    {
        var resource = await factory.CreateResourceAsync(1, null, CancellationToken.None);
        Console.WriteLine($"Empty resource created successfully: {resource != null}");
        Console.WriteLine($"Resource Type ID: 0x{resource?.TypeId:X8}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating empty resource: {ex.Message}");
    }
}
else
{
    Console.WriteLine("Factory NOT found - registration issue");
}
