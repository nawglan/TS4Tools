using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Resources.Animation;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

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
var resourceManager = host.Services.GetRequiredService<IResourceManager>();

// Register our BC4A5044 factory manually
resourceManager.RegisterFactory<IClipHeaderResource, ClipHeaderResourceFactory>();
logger.LogInformation("🔧 Manually registered BC4A5044 ClipHeaderResource factory");

try
{
    logger.LogInformation("🚀 Testing BC4A5044 ClipHeaderResource implementation...");
    
    // Get install directory and try multiple packages
    var installDir = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4";
    var packagePaths = new[]
    {
        Path.Combine(installDir, "EP03", "thumbnails.package"),
        Path.Combine(installDir, "EP15", "ClipHeader.package"),
        Path.Combine(installDir, "GP12", "ClientFullBuild0.package"),
    };
    
    var foundPackage = false;
    
    foreach (var packagePath in packagePaths)
    {
        logger.LogInformation("🎮 Checking package: {PackagePath}", packagePath);
        
        if (!File.Exists(packagePath))
        {
            logger.LogInformation("⏭️  Package not found, skipping...");
            continue;
        }
        
        foundPackage = true;
    
        logger.LogInformation("📦 Loading package: {PackagePath}", packagePath);
        
        var package = await packageFactory.LoadFromFileAsync(packagePath);
        logger.LogInformation("✅ Package loaded successfully with {Count} resources", package.ResourceCount);
        
        // Look for BC4A5044 resources
        var bc4a5044Resources = package.ResourceIndex.Where(r => r.ResourceType == 0xBC4A5044).ToList();
        logger.LogInformation("🔍 Found {Count} BC4A5044 resources in package", bc4a5044Resources.Count);
        
        if (bc4a5044Resources.Count == 0)
        {
            logger.LogInformation("⏭️  No BC4A5044 resources found, trying next package...");
            continue;
        }
        
        foreach (var resourceEntry in bc4a5044Resources.Take(3)) // Test first 3 resources
        {
            logger.LogInformation("🎬 Processing BC4A5044 resource: Type={ResourceType:X8}, Group={Group:X8}, Instance={Instance:X16}",
                resourceEntry.ResourceType, resourceEntry.ResourceGroup, resourceEntry.Instance);
            logger.LogInformation("🔍 Resource details: FileSize={FileSize}, MemorySize={MemorySize}, ChunkOffset={ChunkOffset}, Compressed={Compressed}",
                resourceEntry.FileSize, resourceEntry.MemorySize, resourceEntry.ChunkOffset, resourceEntry.Compressed);
            
            // Skip resources that are obviously corrupted (huge file sizes)
            if (resourceEntry.FileSize > 1000000) // 1MB limit for safety
            {
                logger.LogWarning("⚠️  Skipping resource with large FileSize ({FileSize}), likely corrupted", resourceEntry.FileSize);
                continue;
            }
            
            try
            {
                var resource = await resourceManager.LoadResourceAsync(package, resourceEntry, 1);
                var clipResource = resource as IClipHeaderResource;
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
                            var outputFile = $"bc4a5044_output_{resourceEntry.Instance:X16}.json";
                            await File.WriteAllTextAsync(outputFile, jsonData);
                            logger.LogInformation("💾 JSON output saved to: {OutputFile}", outputFile);
                            
                            // Show first 500 characters of JSON
                            var preview = jsonData.Length > 500 ? jsonData[..500] + "..." : jsonData;
                            logger.LogInformation("📄 JSON Preview:\n{JsonPreview}", preview);
                            
                            // Success! Stop processing more resources for now
                            logger.LogInformation("✅ Successfully processed BC4A5044 resource!");
                            return 0;
                        }
                    }
                }
                else
                {
                    logger.LogWarning("⚠️  Resource could not be cast to IClipHeaderResource, type: {ResourceType}", resource?.GetType().Name ?? "null");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error processing resource {Instance:X16}", resourceEntry.Instance);
            }
            
            logger.LogInformation(""); // Empty line for readability
        }
        
        break; // Exit after processing the first package with BC4A5044 resources
    }
    
    if (!foundPackage)
    {
        logger.LogWarning("⚠️  No packages found. Checking for other test packages...");
        
        // Try test-data directory as fallback
        var testDataDir = "/home/dez/code/TS4Tools/test-data";
        if (Directory.Exists(testDataDir))
        {
            var packageFiles = Directory.GetFiles(testDataDir, "*.package");
            if (packageFiles.Length > 0)
            {
                logger.LogInformation("📁 Found test packages in test-data:");
                foreach (var file in packageFiles)
                {
                    logger.LogInformation("  📦 {FileName}", Path.GetFileName(file));
                }
                
                // Try to load the first package
                var testPackagePath = packageFiles[0];
                logger.LogInformation("📦 Loading test package: {PackagePath}", testPackagePath);
                
                var testPackage = await packageFactory.LoadFromFileAsync(testPackagePath);
                var testBc4a5044Resources = testPackage.ResourceIndex.Where(r => r.ResourceType == 0xBC4A5044).ToList();
                logger.LogInformation("🔍 Found {Count} BC4A5044 resources in test package", testBc4a5044Resources.Count);
            }
            else
            {
                logger.LogWarning("📦 No package files found in test-data directory");
            }
        }
        else
        {
            logger.LogWarning("📁 test-data directory not found");
        }
        
        // Create a minimal test with dummy data
        logger.LogInformation("📝 Creating test with dummy data...");
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
