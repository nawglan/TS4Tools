// Main entry point for LRLE Examples
// Demonstrates different usage patterns of the LRLE resource system

using TS4Tools.Examples.LRLE;

namespace TS4Tools.Examples.LRLE;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== TS4Tools LRLE Examples ===\n");

        if (args.Length > 0 && args[0] == "--advanced")
        {
            Console.WriteLine("Running Advanced LRLE Example...\n");
            var advancedExample = new AdvancedLRLEExample();
            await advancedExample.RunBatchProcessingAsync();
        }
        else
        {
            Console.WriteLine("Running Basic LRLE Example...\n");
            var basicExample = new BasicLRLEExample();
            await basicExample.CompressPngToLrleAsync();
            await basicExample.DecompressLrleToPngAsync();
        }

        Console.WriteLine("\n=== Examples Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
