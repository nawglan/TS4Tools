using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Helpers;

// Simple test to verify helper tool service can discover helpers
var service = new HelperToolService(NullLogger<HelperToolService>.Instance);

Console.WriteLine("Testing TS4Tools Helper Tool Service...");

try
{
    // Test reloading helpers
    await service.ReloadHelpersAsync();

    // Get available helpers
    var availableHelpers = service.GetAvailableHelperTools();

    Console.WriteLine($"✅ Found {availableHelpers.Count} available helper tools:");
    foreach (var helper in availableHelpers)
    {
        Console.WriteLine($"  - {helper}");
    }

    // Test with a simple helper if available
    if (availableHelpers.Count > 0)
    {
        var firstHelper = availableHelpers[0];
        var isAvailable = service.IsHelperToolAvailable(firstHelper);
        Console.WriteLine($"✅ Helper '{firstHelper}' availability: {isAvailable}");

        // Test simple execution (echo command should work on Windows)
        if (firstHelper.Contains("Echo", StringComparison.OrdinalIgnoreCase))
        {
            var result = await service.ExecuteAsync(firstHelper, new[] { "test" });
            Console.WriteLine($"✅ Test execution result: Success={result.IsSuccess}, ExitCode={result.ExitCode}");
        }
    }

    Console.WriteLine("✅ Helper tool service validation completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error during helper tool validation: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
