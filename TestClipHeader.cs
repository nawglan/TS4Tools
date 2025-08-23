using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Animation;
using TS4Tools.Core.Package;
using TS4Tools.Core.System;
using TS4Tools.Core.Interfaces.Resources;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Warning);
});

builder.ConfigureServices((context, services) =>
{
    services.AddTS4Tools();
    services.AddCommonResources();
    services.AddAnimationResources();
});

var host = builder.Build();
var packageManager = host.Services.GetRequiredService<IPackageManager>();
var resourceManager = host.Services.GetRequiredService<IResourceManager>();

try
{
    var packagePath = @"/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Data/Client/ClipHeader.package";
    if (!File.Exists(packagePath))
    {
        Console.WriteLine("Package file not found: " + packagePath);
        return 1;
    }

    var package = await packageManager.LoadPackageAsync(packagePath);
    var bc4a5044Resources = package.GetResourcesByType(0xBC4A5044).Take(3);

    foreach (var entry in bc4a5044Resources)
    {
        try
        {
            var resource = await resourceManager.LoadResourceAsync<IClipHeaderResource>(entry);
            Console.WriteLine($"Successfully loaded BC4A5044 resource: {entry.ResourceKey}");
            Console.WriteLine($"Version: {resource.Version}");
            Console.WriteLine($"ClipName: {resource.ClipName}");
            Console.WriteLine($"Duration: {resource.Duration}");
            Console.WriteLine($"HasValidData: {resource.HasValidData}");
            Console.WriteLine($"Explicit Namespaces: {resource.ExplicitNamespaces.Count}");
            Console.WriteLine("---");
            
            if (resource.HasValidData && !string.IsNullOrEmpty(resource.ClipName))
            {
                var json = resource.ToJsonString();
                if (json != null)
                {
                    Console.WriteLine("JSON Preview (first 500 chars):");
                    Console.WriteLine(json.Length > 500 ? json.Substring(0, 500) + "..." : json);
                }
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading resource {entry.ResourceKey}: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;
