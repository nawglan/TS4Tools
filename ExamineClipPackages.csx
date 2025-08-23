#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Extensions.DependencyInjection, 9.0.0"
#r "nuget: Microsoft.Extensions.Logging, 9.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 9.0.0"

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Simple test to examine raw bytes of a BC4A5044 resource
// This will help us understand the actual binary format

var gameDir = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4";
var dataDir = Path.Combine(gameDir, "Data");

Console.WriteLine("Looking for BC4A5044 resources in game packages...");

if (!Directory.Exists(dataDir))
{
    Console.WriteLine($"Game data directory not found: {dataDir}");
    return;
}

// Look for package files that might contain BC4A5044
var packageFiles = Directory.GetFiles(dataDir, "*.package", SearchOption.AllDirectories)
    .Where(f => Path.GetFileName(f).ToLower().Contains("clip"))
    .Take(3)
    .ToArray();

Console.WriteLine($"Found {packageFiles.Length} potential clip package files:");
foreach (var file in packageFiles)
{
    Console.WriteLine($"  {Path.GetFileName(file)} ({new FileInfo(file).Length:N0} bytes)");
}

// For now, let's examine the raw bytes to understand the format
// We'll look at the beginning of any BC4A5044 resource we can find

foreach (var packageFile in packageFiles)
{
    Console.WriteLine($"\nExamining: {Path.GetFileName(packageFile)}");
    
    try
    {
        using var fileStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read);
        
        // Simple package header examination (this is just for understanding, not full parsing)
        var header = new byte[96]; // Standard package header size
        fileStream.Read(header, 0, header.Length);
        
        Console.WriteLine($"  Package header (first 32 bytes): {Convert.ToHexString(header[..32])}");
        
        // Check if this looks like a Sims 4 package
        var magic = BitConverter.ToUInt32(header, 0);
        var magicStr = Encoding.ASCII.GetString(header, 0, 4);
        Console.WriteLine($"  Magic: 0x{magic:X8} ('{magicStr}')");
        
        if (magicStr == "DBPF")
        {
            Console.WriteLine("  ✓ Valid Sims 4 package file");
            
            // Read some basic package info
            var majorVersion = BitConverter.ToUInt32(header, 4);
            var minorVersion = BitConverter.ToUInt32(header, 8);
            Console.WriteLine($"  Version: {majorVersion}.{minorVersion}");
            
        }
        else
        {
            Console.WriteLine("  ✗ Not a valid Sims 4 package file");
            continue;
        }
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error reading {packageFile}: {ex.Message}");
    }
}

Console.WriteLine("\nNext step: We need to implement proper package parsing to find BC4A5044 resources");
Console.WriteLine("The complex JSON structure you showed indicates this is a sophisticated binary format");
Console.WriteLine("that needs to be parsed according to the ClipResource specification from Sims4Tools.");
