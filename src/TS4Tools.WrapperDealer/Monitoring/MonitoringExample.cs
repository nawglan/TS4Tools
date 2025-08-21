/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Example usage and integration test for WrapperDealer monitoring system.
/// Phase 4.20.4 - Demonstrates proper setup and usage patterns.
/// </summary>
public static class MonitoringExample
{
    /// <summary>
    /// Example of setting up monitoring services in a console application.
    /// </summary>
    public static void ExampleSetup()
    {
        // Setup dependency injection container
        var services = new ServiceCollection();
        
        // Add basic logging (null logger for example)
        services.AddSingleton<ILogger<WrapperDealerMetrics>>(provider => 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WrapperDealerMetrics>.Instance);
        services.AddSingleton<ILogger<DefaultTelemetryService>>(provider => 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultTelemetryService>.Instance);
        services.AddSingleton<ILogger<WrapperDealerTelemetryService>>(provider => 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WrapperDealerTelemetryService>.Instance);
        
        // Add monitoring with configuration
        services.AddWrapperDealerMonitoring(options =>
        {
            options.Enabled = true;
            options.ReportingInterval = TimeSpan.FromMinutes(1);
            options.ImmediateWarningReporting = true;
            options.IncludeResourceTypeNames = false; // For privacy
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get monitoring services
        var metrics = serviceProvider.GetRequiredService<IWrapperDealerMetrics>();
        var telemetryService = serviceProvider.GetRequiredService<WrapperDealerTelemetryService>();
        
        // Example usage
        ExampleUsage(metrics);
        
        // Cleanup
        telemetryService.Dispose();
        serviceProvider.Dispose();
    }

    /// <summary>
    /// Example of recording various WrapperDealer operations.
    /// </summary>
    /// <param name="metrics">Metrics collection service</param>
    public static void ExampleUsage(IWrapperDealerMetrics metrics)
    {
        // Simulate GetResource operations
        for (int i = 0; i < 100; i++)
        {
            var duration = TimeSpan.FromMilliseconds(Random.Shared.Next(10, 200));
            var success = Random.Shared.NextDouble() > 0.05; // 95% success rate
            var resourceType = GetRandomResourceType();
            
            metrics.RecordGetResourceOperation(resourceType, duration, success);
        }

        // Simulate TypeMap lookups
        for (int i = 0; i < 50; i++)
        {
            var duration = TimeSpan.FromMilliseconds(Random.Shared.Next(1, 20));
            var found = Random.Shared.NextDouble() > 0.1; // 90% found rate
            var resourceType = GetRandomResourceType();
            
            metrics.RecordTypeMapLookup(resourceType, duration, found);
        }

        // Simulate plugin loading
        metrics.RecordPluginLoadOperation("ExamplePlugin.dll", TimeSpan.FromSeconds(2.5), true);
        metrics.RecordPluginLoadOperation("AnotherPlugin.dll", TimeSpan.FromSeconds(1.8), true);

        // Simulate wrapper registration
        metrics.RecordWrapperRegistration("ObjectWrapper", 15, TimeSpan.FromMilliseconds(150));
        metrics.RecordWrapperRegistration("TextureWrapper", 8, TimeSpan.FromMilliseconds(120));

        // Get and display performance summary
        var summary = metrics.GetPerformanceSummary();
        DisplayPerformanceSummary(summary);
    }

    /// <summary>
    /// Display performance summary in a readable format.
    /// </summary>
    /// <param name="summary">Performance summary to display</param>
    public static void DisplayPerformanceSummary(WrapperDealerPerformanceSummary summary)
    {
        Console.WriteLine("=== WrapperDealer Performance Summary ===");
        Console.WriteLine($"Collection Duration: {summary.CollectionDuration:hh\\:mm\\:ss}");
        Console.WriteLine($"Total Operations: {summary.TotalOperations:N0}");
        Console.WriteLine($"Overall Success Rate: {summary.OverallSuccessRate:F1}%");
        Console.WriteLine($"Operations/Second: {summary.OverallOperationsPerSecond:F1}");
        Console.WriteLine();

        Console.WriteLine("Operation Statistics:");
        foreach (var (operationType, stats) in summary.StatsByOperation)
        {
            if (stats.TotalOperations > 0)
            {
                Console.WriteLine($"  {operationType}:");
                Console.WriteLine($"    Total: {stats.TotalOperations:N0} operations");
                Console.WriteLine($"    Success Rate: {stats.SuccessRate:F1}%");
                Console.WriteLine($"    Average Duration: {stats.AverageDuration.TotalMilliseconds:F1}ms");
                Console.WriteLine($"    P95 Duration: {stats.P95Duration.TotalMilliseconds:F1}ms");
                Console.WriteLine($"    P99 Duration: {stats.P99Duration.TotalMilliseconds:F1}ms");
                Console.WriteLine();
            }
        }

        if (summary.TopResourceTypes.Count > 0)
        {
            Console.WriteLine("Top Resource Types:");
            foreach (var (resourceType, count) in summary.TopResourceTypes)
            {
                Console.WriteLine($"  {resourceType}: {count:N0} operations");
            }
            Console.WriteLine();
        }

        if (summary.PerformanceWarnings.Count > 0)
        {
            Console.WriteLine("Performance Warnings:");
            foreach (var warning in summary.PerformanceWarnings)
            {
                Console.WriteLine($"  ⚠️  {warning}");
            }
        }
    }

    /// <summary>
    /// Get a random resource type for simulation.
    /// </summary>
    /// <returns>Random resource type name</returns>
    private static string GetRandomResourceType()
    {
        var resourceTypes = new[]
        {
            "0x319E4F1D", // OBJD - Object Definition
            "0x015A1849", // BHV - Behavior 
            "0x034AEECB", // STR - String
            "0x0C772E27", // LOOT - Loot
            "0x0355E0A6", // SIM - Sim
            "0x319E4F1D", // OBJD
            "0x0354796A", // BUFF - Buff
            "0x0358B08A", // ASPIRATION
            "0x025C95B6"  // TRAIT
        };
        
        return resourceTypes[Random.Shared.Next(resourceTypes.Length)];
    }
}

/// <summary>
/// Performance monitoring integration for actual WrapperDealer operations.
/// Phase 4.20.4 - Instrumentation hooks for the WrapperDealer system.
/// </summary>
public static class WrapperDealerInstrumentation
{
    private static IWrapperDealerMetrics? _metrics;

    /// <summary>
    /// Initialize instrumentation with metrics collection service.
    /// Should be called during application startup after DI container is configured.
    /// </summary>
    /// <param name="metrics">Metrics collection service</param>
    public static void Initialize(IWrapperDealerMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>
    /// Instrument a GetResource operation for performance tracking.
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type identifier</param>
    /// <param name="operation">Operation to instrument</param>
    /// <returns>Result of the operation</returns>
    public static T? InstrumentGetResource<T>(string resourceType, Func<T?> operation) where T : class
    {
        if (_metrics == null)
            return operation();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        T? result = null;
        bool success = false;

        try
        {
            result = operation();
            success = result != null;
            return result;
        }
        catch
        {
            success = false;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordGetResourceOperation(resourceType, stopwatch.Elapsed, success);
        }
    }

    /// <summary>
    /// Instrument a TypeMap lookup operation for performance tracking.
    /// </summary>
    /// <param name="resourceType">Resource type to look up</param>
    /// <param name="lookup">Lookup operation</param>
    /// <returns>Lookup result</returns>
    public static bool InstrumentTypeMapLookup(string resourceType, Func<bool> lookup)
    {
        if (_metrics == null)
            return lookup();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool found = false;

        try
        {
            found = lookup();
            return found;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordTypeMapLookup(resourceType, stopwatch.Elapsed, found);
        }
    }

    /// <summary>
    /// Check if instrumentation is enabled.
    /// </summary>
    public static bool IsEnabled => _metrics != null;
}
