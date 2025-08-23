using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using Xunit;

namespace TS4Tools.Tests.GoldenMaster;

/// <summary>
/// Golden Master tests for package index parsing compatibility.
/// Validates that the package index overflow fix correctly handles DBPF file format
/// while maintaining compatibility with existing package files.
/// 
/// These tests focus on integration-level validation using real package data
/// to ensure the arithmetic overflow issue is resolved.
/// </summary>
[Collection("GoldenMaster")]
public class PackageIndexCompatibilityTests
{
    /// <summary>
    /// Validates that BC4A5044 resources from real packages now report reasonable file sizes
    /// instead of the ~2.1GB overflow values that were causing exceptions.
    /// 
    /// This is a Golden Master test that ensures the package index overflow fix works correctly
    /// with real Sims 4 package data.
    /// </summary>
    [Fact]
    public async Task RealPackage_BC4A5044Resources_ReportReasonableFileSizes_GoldenMaster()
    {
        // Arrange: Set up services and locate SP13 package
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning)); // Minimize noise
        services.AddTS4ToolsResourceServices();
        services.AddTS4ToolsPackageServices();

        using var serviceProvider = services.BuildServiceProvider();
        var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

        // Try to find the SP13 package that was causing the overflow issues
        var sp13PackagePath = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Delta/SP13/SimulationPreload.package";
        
        // Skip test if SP13 package not available (development environment)
        if (!File.Exists(sp13PackagePath))
        {
            // This is expected in CI/development environments without full Sims 4 installation
            return;
        }

        // Act: Load the package
        using var package = await packageFactory.LoadFromFileAsync(sp13PackagePath);

        // Assert: Validate package loads successfully
        package.Should().NotBeNull("package should load without exceptions");
        package.ResourceCount.Should().BeGreaterThan(1000, "SP13 should contain many resources");

        // Find BC4A5044 resources (the ones that were causing overflow)
        var bc4a5044Resources = package.ResourceIndex
            .Where(r => r.ResourceType == 0xBC4A5044)
            .ToList();

        bc4a5044Resources.Should().NotBeEmpty("SP13 should contain BC4A5044 animation resources");
        bc4a5044Resources.Count.Should().BeGreaterThan(200, "SP13 contains 227+ BC4A5044 resources");

        // Validate that the specific problematic resources now have reasonable sizes
        var problematicInstances = new[]
        {
            0xCF5B885875D13303UL, // Was reporting 2147483918 bytes
            0x98276765F571E110UL, // Was reporting 2147483979 bytes  
            0x6B019DAA626C6A9CUL  // Was reporting 2147483974 bytes
        };

        foreach (var instanceId in problematicInstances)
        {
            var entry = bc4a5044Resources.FirstOrDefault(r => r.Instance == instanceId);
            if (entry != null)
            {
                // CRITICAL: These resources were reporting ~2.1GB, should now be reasonable
                entry.FileSize.Should().BeLessThan(100_000_000u, 
                    $"Instance 0x{instanceId:X16} should not report gigabyte-sized file (overflow bug)");
                
                entry.FileSize.Should().BeGreaterThan(0u, 
                    $"Instance 0x{instanceId:X16} should have non-zero file size");

                // The fix should result in sizes around 270-331 bytes for these specific resources
                entry.FileSize.Should().BeInRange(100u, 1000u, 
                    $"Instance 0x{instanceId:X16} should have reasonable size after overflow fix");

                // Compressed size should be reasonable (was 23106 bytes for these resources)
                if (entry.Compressed == 0xFFFF) // If compressed
                {
                    entry.MemorySize.Should().BeInRange(20000u, 30000u, 
                        $"Instance 0x{instanceId:X16} compressed size should be reasonable");
                }
            }
        }
    }

    /// <summary>
    /// Validates that GetResourceStreamAsync no longer throws arithmetic overflow exceptions
    /// for BC4A5044 resources that were previously causing issues.
    /// 
    /// This test ensures the complete fix - not just reasonable file sizes, but also
    /// successful resource stream creation.
    /// </summary>
    [Fact]
    public async Task RealPackage_BC4A5044Resources_CreateStreamsWithoutOverflow_GoldenMaster()
    {
        // Arrange: Set up services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsResourceServices();
        services.AddTS4ToolsPackageServices();

        using var serviceProvider = services.BuildServiceProvider();
        var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

        var sp13PackagePath = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Delta/SP13/SimulationPreload.package";
        
        if (!File.Exists(sp13PackagePath))
        {
            return; // Skip in development environments
        }

        using var package = await packageFactory.LoadFromFileAsync(sp13PackagePath);

        // Act & Assert: Try to create streams for BC4A5044 resources
        var bc4a5044Resources = package.ResourceIndex
            .Where(r => r.ResourceType == 0xBC4A5044)
            .Take(5) // Test first 5 to keep test time reasonable
            .ToList();

        foreach (var entry in bc4a5044Resources)
        {
            // This should not throw OverflowException anymore
            long streamLength = 0;
            var act = async () =>
            {
                using var stream = await package.GetResourceStreamAsync(entry);
                streamLength = stream?.Length ?? 0;
            };

            await act.Should().NotThrowAsync<OverflowException>(
                $"GetResourceStreamAsync should not overflow for instance 0x{entry.Instance:X16}");

            // Validate stream has reasonable length
            streamLength.Should().BeGreaterThan(0, "stream should have content");
            streamLength.Should().BeLessThan(100_000_000, "stream should not be gigabytes");
        }
    }

    /// <summary>
    /// Validates that all resources in the package (not just BC4A5044) have reasonable file sizes.
    /// This ensures the fix doesn't just work for animation resources but across all resource types.
    /// </summary>
    [Fact]
    public async Task RealPackage_AllResources_HaveReasonableFileSizes_GoldenMaster()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsResourceServices();
        services.AddTS4ToolsPackageServices();

        using var serviceProvider = services.BuildServiceProvider();
        var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();

        var sp13PackagePath = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4/Delta/SP13/SimulationPreload.package";
        
        if (!File.Exists(sp13PackagePath))
        {
            return;
        }

        using var package = await packageFactory.LoadFromFileAsync(sp13PackagePath);

        // Act & Assert: Check all resources for reasonable file sizes
        var oversizedResources = package.ResourceIndex
            .Where(r => r.FileSize > 100_000_000) // 100MB threshold for "reasonable"
            .ToList();

        // There should be very few (if any) resources over 100MB in a typical Sims 4 package
        oversizedResources.Count.Should().BeLessThan(10, 
            "most resources should be under 100MB, excessive large sizes indicate overflow bug");

        // Check for the specific overflow pattern (2GB+ sizes)
        var overflowResources = package.ResourceIndex
            .Where(r => r.FileSize > 2_000_000_000) // 2GB threshold indicates overflow
            .ToList();

        overflowResources.Should().BeEmpty(
            "no resources should report 2GB+ sizes, this indicates the overflow bug is still present");

        // Validate FileSize vs MemorySize relationship for compressed resources
        var compressedResources = package.ResourceIndex
            .Where(r => r.Compressed == 0xFFFF)
            .Take(100) // Sample to keep test time reasonable
            .ToList();

        foreach (var entry in compressedResources)
        {
            if (entry.MemorySize > 0) // Skip zero memory size resources
            {
                // For compressed resources, FileSize should be reasonable relative to MemorySize
                // (though the exact relationship varies by compression algorithm and content)
                var ratio = (double)entry.FileSize / entry.MemorySize;
                ratio.Should().BeInRange(0.1, 20.0, 
                    $"FileSize/MemorySize ratio should be reasonable for compressed resource 0x{entry.Instance:X16}");
            }
        }
    }
}
