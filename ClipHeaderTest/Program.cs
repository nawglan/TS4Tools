using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Animation;
using TS4Tools.Tests.Common;

namespace ClipHeaderTest;

/// <summary>
/// Simple test script to verify our BC4A5044 (Clip Header) implementation works with real data.
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("BC4A5044 Clip Header Resource Test");
            Console.WriteLine("==================================");
            Console.WriteLine();

            // Try to use a real ClipHeader.package from the game installation
            var clipHeaderPaths = new[]
            {
                "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/SP53/ClipHeader.package",
                "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/SP43/ClipHeader.package",
                "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/EP15/ClipHeader.package"
            };

            string? packagePath = null;
            foreach (var path in clipHeaderPaths)
            {
                if (File.Exists(path))
                {
                    packagePath = path;
                    break;
                }
            }

            if (packagePath == null)
            {
                // Fallback to TestPackageDiscovery if no real packages found
                var testResult = await TestPackageDiscovery.GetTestPackagesAsync(
                    filterResourceTypes: [0xBC4A5044],  // Only BC4A5044
                    maxPackagesToAnalyze: 50
                );

                if (!testResult.Resources.ContainsKey("0xBC4A5044"))
                {
                    Console.WriteLine("❌ No BC4A5044 resources found in test packages");
                    return 1;
                }

                packagePath = testResult.Resources["0xBC4A5044"];
            }
            Console.WriteLine($"📦 Testing with package: {Path.GetFileName(packagePath)}");
            Console.WriteLine($"📁 Full path: {packagePath}");
            Console.WriteLine();

            // Set up services
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning).AddConsole());
            services.AddTS4ToolsPackageServices();
            services.AddTS4ToolsResourceServices();

            using var serviceProvider = services.BuildServiceProvider();
            var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();
            var resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Load the package and find BC4A5044 resources
            using var package = await packageFactory.LoadFromFileAsync(packagePath, readOnly: true);
            
            var clipHeaderResources = package.ResourceIndex
                .Where(r => r.ResourceType == 0xBC4A5044)
                .ToList();

            Console.WriteLine($"🔍 Found {clipHeaderResources.Count} BC4A5044 resources in package");
            Console.WriteLine();

            foreach (var (resource, index) in clipHeaderResources.Select((r, i) => (r, i)))
            {
                try
                {
                    Console.WriteLine($"📄 Testing resource #{index + 1}:");
                    Console.WriteLine($"   Resource Type: 0x{resource.ResourceType:X8}");
                    Console.WriteLine($"   Resource Group: 0x{resource.ResourceGroup:X8}");
                    Console.WriteLine($"   Instance: 0x{resource.Instance:X16}");
                    Console.WriteLine($"   File Size: {resource.FileSize} bytes");
                    Console.WriteLine($"   Memory Size: {resource.MemorySize} bytes");

                    // Get the resource data using ResourceManager - this should automatically use our ClipHeaderResourceFactory
                    using var resourceInstance = await resourceManager.LoadResourceAsync(
                        package, 
                        resource, 
                        apiVersion: 1);
                    
                    if (resourceInstance == null)
                    {
                        Console.WriteLine("   ❌ Failed to get resource instance");
                        continue;
                    }

                    Console.WriteLine($"   ✅ Got resource instance of type: {resourceInstance.GetType().Name}");

                    // Check if it's our ClipHeaderResource
                    if (resourceInstance is IClipHeaderResource clipHeader)
                    {
                        Console.WriteLine($"   🎉 Successfully cast to IClipHeaderResource!");
                        Console.WriteLine($"   📋 Version: {clipHeader.Version}");
                        Console.WriteLine($"   🎬 Clip Name: '{clipHeader.ClipName ?? "null"}'");
                        Console.WriteLine($"   ⏱️  Duration: {clipHeader.Duration}s");
                        Console.WriteLine($"   🎞️  Frame Rate: {clipHeader.FrameRate} fps");
                        Console.WriteLine($"   🏷️  Flags: 0x{clipHeader.Flags:X8}");
                        Console.WriteLine($"   📊 Has JSON Data: {clipHeader.HasValidJsonData}");
                    
                        if (clipHeader.HasValidJsonData)
                        {
                            Console.WriteLine($"   📝 JSON Data Length: {clipHeader.JsonData?.Length ?? 0} characters");
                            
                            // Pretty print JSON if it's not too long
                            if (!string.IsNullOrEmpty(clipHeader.JsonData) && clipHeader.JsonData.Length < 500)
                            {
                                try
                                {
                                    var jsonObj = JsonDocument.Parse(clipHeader.JsonData);
                                    var formatted = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                                    Console.WriteLine($"   📋 JSON Content:");
                                    foreach (var line in formatted.Split('\n').Take(10))
                                    {
                                        Console.WriteLine($"      {line}");
                                    }
                                    if (formatted.Split('\n').Length > 10)
                                    {
                                        Console.WriteLine($"      ... (truncated, {formatted.Split('\n').Length - 10} more lines)");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"   📋 JSON Content (raw): {clipHeader.JsonData.Substring(0, Math.Min(200, clipHeader.JsonData.Length))}...");
                                }
                            }
                            else if (!string.IsNullOrEmpty(clipHeader.JsonData))
                            {
                                Console.WriteLine($"   📋 JSON Content (preview): {clipHeader.JsonData.Substring(0, Math.Min(100, clipHeader.JsonData.Length))}...");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️  Resource is not a ClipHeaderResource (type: {resourceInstance.GetType().Name})");
                    }

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Failed to parse resource #{index + 1}: {ex.Message}");
                    Console.WriteLine($"   🔍 Exception details: {ex.GetType().Name}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("✅ Test completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine(ex.ToString());
            return 1;
        }
    }
}
