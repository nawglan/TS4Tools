using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Resources.Animation;
using TS4Tools.Core.Interfaces.Resources;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add TS4Tools services
builder.Services.AddTS4ToolsPackageServices();
builder.Services.AddTS4ToolsResourceServices();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var packageFactory = host.Services.GetRequiredService<IPackageFactory>();

try
{
    logger.LogInformation("🚀 Testing BC4A5044 ClipHeaderResource implementation...");
    
    // Test with a sample package containing BC4A5044 resources
    var packagePath = "/home/dez/code/TS4Tools/test-data/DeltaBuild0.package";
    
    if (File.Exists(packagePath))
    {
        logger.LogInformation("📦 Loading package: {PackagePath}", packagePath);
        
        var package = await packageFactory.LoadFromFileAsync(packagePath);
        logger.LogInformation("✅ Package loaded successfully with {Count} resources", package.GetResourceCount());
        
        // Look for BC4A5044 resources
        var bc4a5044Resources = package.GetResourcesByType(0xBC4A5044);
        logger.LogInformation("🔍 Found {Count} BC4A5044 resources", bc4a5044Resources.Count());
        
        foreach (var resource in bc4a5044Resources.Take(3)) // Test first 3 resources
        {
            logger.LogInformation("🎬 Processing BC4A5044 resource: {Key}", resource.Key);
            
            try
            {
                var clipResource = resource.GetResource() as IClipHeaderResource;
                if (clipResource != null)
                {
                    logger.LogInformation("📋 Clip Details:");
                    logger.LogInformation("   Version: {Version}", clipResource.Version);
                    logger.LogInformation("   Duration: {Duration:F2}s", clipResource.Duration);
                    logger.LogInformation("   ClipName: {ClipName}", clipResource.ClipName ?? "N/A");
                    logger.LogInformation("   RigName: {RigName}", clipResource.RigName ?? "N/A");
                    logger.LogInformation("   ActorName: {ActorName}", clipResource.ActorName ?? "N/A");
                    logger.LogInformation("   HasValidData: {HasValidData}", clipResource.HasValidData);
                    
                    if (clipResource.HasValidData)
                    {
                        var jsonData = clipResource.JsonData;
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            // Save JSON output to file for inspection
                            var outputFile = $"bc4a5044_output_{resource.Key.Instance:X16}.json";
                            await File.WriteAllTextAsync(outputFile, jsonData);
                            logger.LogInformation("💾 JSON output saved to: {OutputFile}", outputFile);
                            
                            // Show first 500 characters of JSON
                            var preview = jsonData.Length > 500 ? jsonData[..500] + "..." : jsonData;
                            logger.LogInformation("📄 JSON Preview:\n{JsonPreview}", preview);
                        }
                    }
                }
                else
                {
                    logger.LogWarning("⚠️  Resource could not be cast to IClipHeaderResource");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error processing BC4A5044 resource {Key}", resource.Key);
            }
            
            logger.LogInformation(""); // Empty line for readability
        }
    }
    else
    {
        logger.LogWarning("⚠️  Test package not found: {PackagePath}", packagePath);
        logger.LogInformation("📝 Create a test with a small BC4A5044 binary file...");
        
        // Create a minimal test with dummy data
        var testData = new byte[] 
        { 
            0x01, 0x00, 0x00, 0x00, // Version = 1
            0x00, 0x00, 0x00, 0x00, // Flags = 0
            0x00, 0x00, 0x80, 0x3F, // Duration = 1.0f
            // Quaternion (4 floats)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F,
            // Vector3 (3 floats)  
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // RigName length + empty string
            0x00, 0x00, 0x00, 0x00
        };
        
        using var testStream = new MemoryStream(testData);
        var testClip = new ClipHeaderResource(testStream);
        
        logger.LogInformation("🧪 Test ClipHeaderResource created:");
        logger.LogInformation("   Version: {Version}", testClip.Version);
        logger.LogInformation("   Duration: {Duration:F2}s", testClip.Duration);
        logger.LogInformation("   HasValidData: {HasValidData}", testClip.HasValidData);
        
        var testJson = testClip.ToJsonString();
        logger.LogInformation("📄 Test JSON:\n{TestJson}", testJson);
    }
    
    logger.LogInformation("✅ BC4A5044 ClipHeaderResource test completed!");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Test failed with error");
    return 1;
}

return 0;
