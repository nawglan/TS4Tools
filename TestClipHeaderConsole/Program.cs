using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Resources.Animation;

var services = new ServiceCollection();
services.AddLogging(builder => 
{
    builder.ClearProviders();
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

services.AddTS4ToolsPackageServices();
services.AddTS4ToolsResourceServices();

using var serviceProvider = services.BuildServiceProvider();
var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

try
{
    var packagePath = @"/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Data/Client/ClipHeader.package";
    if (!File.Exists(packagePath))
    {
        Console.WriteLine("Package file not found: " + packagePath);
        return 1;
    }

    Console.WriteLine("Loading ClipHeader.package...");
    var package = await packageFactory.LoadFromFileAsync(packagePath, readOnly: true);
    var bc4a5044Resources = package.ResourceIndex
        .Where(r => r.ResourceType == 0xBC4A5044)
        .ToList();

    Console.WriteLine($"Found {bc4a5044Resources.Count} BC4A5044 resources");
    
    foreach (var entry in bc4a5044Resources)
    {
        Console.WriteLine($"  Resource: Type=0x{entry.ResourceType:X8}, Group=0x{entry.ResourceGroup:X8}, Instance=0x{entry.Instance:X16}");
        Console.WriteLine($"    ChunkOffset={entry.ChunkOffset}, FileSize={entry.FileSize}, MemorySize={entry.MemorySize}");
    }

    foreach (var entry in bc4a5044Resources.Take(3))
    {
        try
        {
            Console.WriteLine($"\nLoading BC4A5044 resource: Type=0x{entry.ResourceType:X8}, Group=0x{entry.ResourceGroup:X8}, Instance=0x{entry.Instance:X16}");
            Console.WriteLine($"Resource entry info: ChunkOffset={entry.ChunkOffset}, FileSize={entry.FileSize}, MemorySize={entry.MemorySize}");
            
            // Get the raw data from the package  
            Console.WriteLine("Getting resource stream...");
            var stream = await package.GetResourceStreamAsync(entry);
            if (stream == null)
            {
                Console.WriteLine($"❌ Could not get resource stream");
                continue;
            }
            
            Console.WriteLine($"📊 Stream info: Length={stream.Length}, Position={stream.Position}, CanRead={stream.CanRead}");
            
            // Read the first 32 bytes to see what we're dealing with
            var originalPos = stream.Position;
            var buffer = new byte[Math.Min(32, stream.Length)];
            await stream.ReadExactlyAsync(buffer);
            stream.Position = originalPos;
            
            Console.WriteLine($"First 32 bytes: {string.Join(" ", buffer.Select(b => $"{b:X2}"))}");
            Console.WriteLine($"As ASCII: {string.Join("", buffer.Select(b => b >= 32 && b <= 126 ? (char)b : '.'))}");
            
            // Create our ClipHeaderResource directly
            Console.WriteLine("Creating ClipHeaderResource...");
            try
            {
                var resource = new ClipHeaderResource(stream);
            
                if (resource == null)
                {
                    Console.WriteLine($"❌ Resource was not loaded");
                    continue;
                }
                
                Console.WriteLine($"✓ Successfully loaded resource");
                Console.WriteLine($"  Version: {resource.Version}");
                Console.WriteLine($"  ClipName: {resource.ClipName ?? "null"}");
                Console.WriteLine($"  ActorName: {resource.ActorName ?? "null"}");
                Console.WriteLine($"  Duration: {resource.Duration}");
                Console.WriteLine($"  HasValidData: {resource.HasValidData}");
                Console.WriteLine($"  Explicit Namespaces: {resource.ExplicitNamespaces.Count}");
                
                if (resource.HasValidData)
                {
                    var json = resource.JsonData;
                    if (json != null)
                    {
                        Console.WriteLine("\n📄 JSON Preview (first 800 chars):");
                        Console.WriteLine(json.Length > 800 ? json.Substring(0, 800) + "..." : json);
                        Console.WriteLine("\n✅ Successfully parsed BC4A5044 resource!");
                        break;
                    }
                }
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"❌ Error creating ClipHeaderResource: {innerEx.Message}");
                Console.WriteLine($"   Stack trace: {innerEx.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading resource Type=0x{entry.ResourceType:X8}: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    return 1;
}

return 0;
