using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Helpers;
using TS4Tools.Core.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        // Build a simple service container
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add helper tool services
        services.AddHelperToolServices(options =>
        {
            options.HelperDirectories = new[]
            {
                @"C:\Users\nawgl\code\Sims4Tools\s4pe Helpers"
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("TS4Tools Helper Tool Validation");
        Console.WriteLine("================================");
        Console.WriteLine();

        try
        {
            // Get the helper service
            var helperService = serviceProvider.GetRequiredService<IHelperToolService>();

            // Test 1: Get available helpers
            Console.WriteLine("1. Testing helper discovery...");
            var availableHelpers = helperService.GetAvailableHelperTools();
            Console.WriteLine($"   Found {availableHelpers.Count} helper tools:");

            foreach (var helper in availableHelpers)
            {
                Console.WriteLine($"   - {helper.Name} (ID: {helper.Id})");
                Console.WriteLine($"     Path: {helper.ExecutablePath}");
                Console.WriteLine($"     Description: {helper.Description}");
                Console.WriteLine();
            }

            // Test 2: Check if specific helpers are available
            Console.WriteLine("2. Testing helper availability checks...");
            var sampleHelperIds = new[] { "dds-viewer", "texture-tool", "model-viewer" };

            foreach (var helperId in sampleHelperIds)
            {
                var isAvailable = helperService.IsHelperToolAvailable(helperId);
                Console.WriteLine($"   Helper '{helperId}': {(isAvailable ? "Available" : "Not Available")}");
            }

            Console.WriteLine();
            Console.WriteLine("3. Testing resource type helpers...");
            var resourceTypes = new[] { "DDS", "PNG", "3D", "MODEL" };

            foreach (var resourceType in resourceTypes)
            {
                var helpers = helperService.GetHelpersForResourceType(resourceType);
                Console.WriteLine($"   Helpers for '{resourceType}': {helpers.Count}");
                foreach (var helper in helpers)
                {
                    Console.WriteLine($"     - {helper.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("✅ Helper Tool Service validation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during validation: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
