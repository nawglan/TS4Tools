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
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Add helper tool services - no options needed, uses default configuration
        services.AddHelperToolServices();

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

            foreach (var helperName in availableHelpers)
            {
                Console.WriteLine($"   - {helperName}");
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
            var resourceTypes = new uint[] { 0x1C0532FA, 0xBA856C78, 0x2E75C764 }; // Example resource type IDs

            foreach (var resourceType in resourceTypes)
            {
                var helpers = helperService.GetHelpersForResourceType(resourceType);
                Console.WriteLine($"   Helpers for resource type '0x{resourceType:X8}': {helpers.Count}");
                foreach (var helper in helpers)
                {
                    Console.WriteLine($"     - {helper.Label} (ID: {helper.Id})");
                }
            }

            // Test 4: Check helper directories
            Console.WriteLine();
            Console.WriteLine("4. Checking helper directories...");
            var helperDir = @"C:\Users\nawgl\code\Sims4Tools\s4pe Helpers";
            if (Directory.Exists(helperDir))
            {
                Console.WriteLine($"   Helper directory exists: {helperDir}");
                var subdirs = Directory.GetDirectories(helperDir);
                Console.WriteLine($"   Found {subdirs.Length} subdirectories:");
                foreach (var subdir in subdirs)
                {
                    Console.WriteLine($"     - {Path.GetFileName(subdir)}");

                    // Look for .helper files
                    var helperFiles = Directory.GetFiles(subdir, "*.helper");
                    if (helperFiles.Length > 0)
                    {
                        Console.WriteLine($"       Found {helperFiles.Length} .helper file(s):");
                        foreach (var file in helperFiles)
                        {
                            Console.WriteLine($"         - {Path.GetFileName(file)}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"   ⚠️  Helper directory not found: {helperDir}");
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
