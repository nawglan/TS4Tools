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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.WrapperDealer.Monitoring;

namespace TS4Tools.WrapperDealer.Examples;

/// <summary>
/// Example demonstrating integration of enhanced performance monitoring with WrapperDealer.
/// Shows how to set up monitoring, collect performance data, and generate reports.
/// </summary>
public class PerformanceMonitoringIntegrationExample
{
    /// <summary>
    /// Demonstrates setting up enhanced performance monitoring in a DI container.
    /// </summary>
    public static IServiceProvider SetupEnhancedMonitoring()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add enhanced performance monitoring with production profile
        services.AddEnhancedPerformanceMonitoring(PerformanceMonitoringProfile.Production);

        // Could also configure with custom options:
        /*
        services.AddEnhancedPerformanceMonitoring(options =>
        {
            options.MaxSamplesPerPlugin = 25_000;
            options.AnalysisWindow = TimeSpan.FromHours(1);
            options.OperationTimeoutThreshold = TimeSpan.FromSeconds(3);
            options.EnableImmediateAnalysis = true;
        });
        */

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Demonstrates how to instrument plugin operations with enhanced monitoring.
    /// </summary>
    public static void InstrumentPluginOperations(IServiceProvider serviceProvider)
    {
        var monitoringService = serviceProvider.GetRequiredService<IEnhancedPerformanceMonitoringService>();
        var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringIntegrationExample>>();

        // Example 1: Track a wrapper creation operation
        var wrapper = monitoringService.TrackOperation(
            pluginName: "SimDataResourcePlugin",
            operationType: PluginOperationType.WrapperCreation,
            resourceType: "SimData",
            action: () =>
            {
                // Simulate wrapper creation logic
                logger.LogDebug("Creating SimData wrapper");
                System.Threading.Thread.Sleep(50); // Simulate work
                return new { Id = Guid.NewGuid(), Type = "SimData" };
            });

        logger.LogInformation("Created wrapper: {WrapperInfo}", wrapper);

        // Example 2: Track a resource reading operation that might fail
        try
        {
            monitoringService.TrackOperation(
                pluginName: "ImageResourcePlugin",
                operationType: PluginOperationType.ResourceReading,
                resourceType: "DST",
                action: () =>
                {
                    // Simulate reading that might fail
                    if (Random.Shared.NextDouble() < 0.1) // 10% chance of failure
                    {
                        throw new InvalidDataException("Corrupted image data");
                    }
                    
                    logger.LogDebug("Successfully read DST image");
                    System.Threading.Thread.Sleep(25);
                });
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Failed to read image resource");
        }

        // Example 3: Use the monitoring aspect pattern
        var monitoredResourceValidation = monitoringService.MonitoredOperation(
            pluginName: "PackageValidatorPlugin",
            operationType: PluginOperationType.ResourceValidation,
            resourceType: "PACKAGE",
            operation: () =>
            {
                logger.LogDebug("Validating package integrity");
                System.Threading.Thread.Sleep(75);
                return true; // Validation passed
            });

        var isValid = monitoredResourceValidation();
        logger.LogInformation("Package validation result: {IsValid}", isValid);
    }

    /// <summary>
    /// Demonstrates analyzing performance data and generating reports.
    /// </summary>
    public static async Task AnalyzePerformanceDataAsync(IServiceProvider serviceProvider)
    {
        var monitoringService = serviceProvider.GetRequiredService<IEnhancedPerformanceMonitoringService>();
        var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringIntegrationExample>>();

        // Generate some sample data first
        await GenerateSamplePerformanceDataAsync(monitoringService, logger);

        // Get system-wide performance summary
        var systemSummary = monitoringService.GetSystemSummary();
        logger.LogInformation("System Performance Summary:");
        logger.LogInformation("- Total Plugins: {PluginCount}", systemSummary.TotalPlugins);
        logger.LogInformation("- Total Operations: {OperationCount}", systemSummary.TotalSamplesCollected);
        logger.LogInformation("- Overall Success Rate: {SuccessRate:F1}%", systemSummary.OverallSuccessRate);
        logger.LogInformation("- Average Response Time: {AvgTime:F1}ms", systemSummary.SystemAverageResponseTime);
        logger.LogInformation("- 95th Percentile: {P95Time:F1}ms", systemSummary.SystemP95ResponseTime);

        // Analyze individual plugin performance
        foreach (var pluginAnalysis in systemSummary.PluginAnalyses.Take(5)) // Top 5 plugins
        {
            logger.LogInformation("Plugin: {PluginName}", pluginAnalysis.PluginName);
            logger.LogInformation("  Operations: {Total} (Success: {Success}, Failed: {Failed})",
                pluginAnalysis.TotalOperations,
                pluginAnalysis.SuccessfulOperations,
                pluginAnalysis.FailedOperations);
            logger.LogInformation("  Response Time: Avg={AvgMs:F1}ms, P95={P95Ms:F1}ms",
                pluginAnalysis.AverageResponseTime.TotalMilliseconds,
                pluginAnalysis.P95ResponseTime.TotalMilliseconds);
            logger.LogInformation("  Trend: {ResponseTrend} response time, {FailureTrend} failures",
                pluginAnalysis.RecentTrend.ResponseTimeTrend,
                pluginAnalysis.RecentTrend.FailureRateTrend);
        }

        // Check for performance issues
        if (systemSummary.TopPerformanceIssues.Any())
        {
            logger.LogWarning("Performance Issues Detected:");
            foreach (var issue in systemSummary.TopPerformanceIssues.Take(3))
            {
                logger.LogWarning("  {Severity}: {Plugin} - {Description}",
                    issue.Severity, issue.PluginName, issue.Description);
            }
        }

        // Show plugin health rankings
        var healthRankings = monitoringService.Profiler.GetPluginHealthRankings();
        logger.LogInformation("Plugin Health Rankings (Top 5):");
        foreach (var ranking in healthRankings.Take(5))
        {
            logger.LogInformation("  {PluginName}: {HealthScore:F1}/100",
                ranking.PluginName, ranking.HealthScore);
        }

        // Export detailed performance data
        var exportData = monitoringService.Profiler.ExportPerformanceData(includeRawData: false);
        var exportJson = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            $"plugin_performance_report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(exportPath, exportJson);
        logger.LogInformation("Performance report exported to: {ExportPath}", exportPath);
    }

    /// <summary>
    /// Generates sample performance data for demonstration purposes.
    /// </summary>
    private static async Task GenerateSamplePerformanceDataAsync(
        IEnhancedPerformanceMonitoringService monitoringService,
        ILogger logger)
    {
        logger.LogInformation("Generating sample performance data...");

        var plugins = new[]
        {
            ("SimDataResourcePlugin", new[] { "SimData", "NameMap" }),
            ("ImageResourcePlugin", new[] { "DST", "DDS", "RLE2" }),
            ("ModelResourcePlugin", new[] { "GEOM", "MLOD", "MODL" }),
            ("AudioResourcePlugin", new[] { "JAZZ", "SFX" }),
            ("PackageValidatorPlugin", new[] { "PACKAGE" })
        };

        var operationTypes = new[]
        {
            PluginOperationType.WrapperCreation,
            PluginOperationType.ResourceReading,
            PluginOperationType.ResourceWriting,
            PluginOperationType.ResourceValidation
        };

        var tasks = new List<Task>();

        foreach (var (pluginName, resourceTypes) in plugins)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var resourceType = resourceTypes[Random.Shared.Next(resourceTypes.Length)];
                    var operationType = operationTypes[Random.Shared.Next(operationTypes.Length)];

                    monitoringService.TrackOperation(
                        pluginName,
                        operationType,
                        resourceType,
                        () =>
                        {
                            // Simulate variable operation times
                            var baseTime = operationType switch
                            {
                                PluginOperationType.WrapperCreation => 20,
                                PluginOperationType.ResourceReading => 50,
                                PluginOperationType.ResourceWriting => 80,
                                PluginOperationType.ResourceValidation => 30,
                                _ => 40
                            };

                            var jitter = Random.Shared.Next(-10, 20);
                            var sleepTime = Math.Max(1, baseTime + jitter);
                            
                            System.Threading.Thread.Sleep(sleepTime);

                            // Simulate occasional failures
                            if (Random.Shared.NextDouble() < 0.05) // 5% failure rate
                            {
                                throw new InvalidOperationException("Simulated plugin failure");
                            }

                            return i;
                        });

                    // Small delay between operations
                    await Task.Delay(Random.Shared.Next(5, 25));
                }
            }));
        }

        await Task.WhenAll(tasks);
        logger.LogInformation("Sample data generation completed");
    }

    /// <summary>
    /// Demonstrates creating a background service that monitors plugin performance.
    /// </summary>
    public class PerformanceMonitoringBackgroundService : BackgroundService
    {
        private readonly IEnhancedPerformanceMonitoringService _monitoringService;
        private readonly ILogger<PerformanceMonitoringBackgroundService> _logger;

        public PerformanceMonitoringBackgroundService(
            IEnhancedPerformanceMonitoringService monitoringService,
            ILogger<PerformanceMonitoringBackgroundService> logger)
        {
            _monitoringService = monitoringService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var summary = _monitoringService.GetSystemSummary();
                    
                    // Log periodic performance summary
                    _logger.LogInformation("Performance Health Check: {PluginCount} plugins, " +
                                         "{SuccessRate:F1}% success rate, " +
                                         "{AvgResponseTime:F1}ms avg response time",
                        summary.TotalPlugins,
                        summary.OverallSuccessRate,
                        summary.SystemAverageResponseTime);

                    // Alert on critical performance issues
                    var criticalIssues = summary.TopPerformanceIssues
                        .Where(issue => issue.Severity == PerformanceIssueSeverity.Critical)
                        .ToList();

                    foreach (var issue in criticalIssues)
                    {
                        _logger.LogError("CRITICAL Performance Issue: {Plugin} - {Description}",
                            issue.PluginName, issue.Description);
                    }

                    // Alert on plugins with very low health scores
                    var unhealthyPlugins = _monitoringService.Profiler.GetPluginHealthRankings()
                        .Where(ranking => ranking.HealthScore < 50)
                        .ToList();

                    foreach (var plugin in unhealthyPlugins)
                    {
                        _logger.LogWarning("Unhealthy Plugin: {PluginName} (Health Score: {HealthScore:F1})",
                            plugin.PluginName, plugin.HealthScore);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during performance monitoring health check");
                }

                // Wait 5 minutes before next check
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Example showing how to extend monitoring with custom metrics.
    /// </summary>
    public class CustomPerformanceMetrics
    {
        private readonly IEnhancedPerformanceMonitoringService _monitoringService;
        private readonly ILogger _logger;

        public CustomPerformanceMetrics(
            IEnhancedPerformanceMonitoringService monitoringService,
            ILogger<CustomPerformanceMetrics> logger)
        {
            _monitoringService = monitoringService;
            _logger = logger;
        }

        /// <summary>
        /// Tracks custom performance metrics for cache operations.
        /// </summary>
        public void TrackCachePerformance(string pluginName, string operation, bool cacheHit, TimeSpan duration)
        {
            _monitoringService.TrackOperation(
                pluginName,
                PluginOperationType.CacheOperation,
                $"Cache_{operation}_{(cacheHit ? "Hit" : "Miss")}",
                () =>
                {
                    // Simulate the cache operation time
                    if (duration > TimeSpan.Zero)
                    {
                        System.Threading.Thread.Sleep(duration);
                    }
                    return cacheHit;
                });

            if (cacheHit)
            {
                _logger.LogDebug("Cache hit for {Plugin}:{Operation} in {Duration:F1}ms",
                    pluginName, operation, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug("Cache miss for {Plugin}:{Operation} in {Duration:F1}ms",
                    pluginName, operation, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Generates a custom performance report focusing on cache efficiency.
        /// </summary>
        public CacheEfficiencyReport GenerateCacheEfficiencyReport()
        {
            var summary = _monitoringService.GetSystemSummary();
            var cacheHotspots = summary.ResourceTypeHotspots
                .Where(hotspot => hotspot.ResourceType.StartsWith("Cache_"))
                .ToList();

            var cacheHitOperations = cacheHotspots
                .Where(hotspot => hotspot.ResourceType.Contains("_Hit"))
                .Sum(hotspot => hotspot.TotalOperations);

            var totalCacheOperations = cacheHotspots
                .Sum(hotspot => hotspot.TotalOperations);

            var cacheHitRate = totalCacheOperations > 0 
                ? (double)cacheHitOperations / totalCacheOperations 
                : 0.0;

            return new CacheEfficiencyReport
            {
                CacheHitRate = cacheHitRate,
                TotalCacheOperations = totalCacheOperations,
                CacheHitOperations = cacheHitOperations,
                AverageCacheHitTime = cacheHotspots
                    .Where(hotspot => hotspot.ResourceType.Contains("_Hit"))
                    .Average(hotspot => hotspot.AverageResponseTime.TotalMilliseconds),
                AverageCacheMissTime = cacheHotspots
                    .Where(hotspot => hotspot.ResourceType.Contains("_Miss"))
                    .Average(hotspot => hotspot.AverageResponseTime.TotalMilliseconds)
            };
        }
    }

    /// <summary>
    /// Custom report for cache efficiency analysis.
    /// </summary>
    public record CacheEfficiencyReport
    {
        public double CacheHitRate { get; init; }
        public int TotalCacheOperations { get; init; }
        public int CacheHitOperations { get; init; }
        public double AverageCacheHitTime { get; init; }
        public double AverageCacheMissTime { get; init; }
    }
}

/// <summary>
/// Console application demonstrating the enhanced performance monitoring integration.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up dependency injection with enhanced monitoring
        var serviceProvider = PerformanceMonitoringIntegrationExample.SetupEnhancedMonitoring();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Enhanced Performance Monitoring Demo Started");

            // Demonstrate plugin instrumentation
            PerformanceMonitoringIntegrationExample.InstrumentPluginOperations(serviceProvider);

            // Generate and analyze performance data
            await PerformanceMonitoringIntegrationExample.AnalyzePerformanceDataAsync(serviceProvider);

            logger.LogInformation("Enhanced Performance Monitoring Demo Completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in performance monitoring demo");
        }
        finally
        {
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
