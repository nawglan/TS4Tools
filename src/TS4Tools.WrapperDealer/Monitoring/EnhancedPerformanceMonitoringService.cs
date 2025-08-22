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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Enhanced performance monitoring service that integrates with the existing WrapperDealer instrumentation.
/// Provides seamless integration with the current monitoring infrastructure while adding advanced profiling capabilities.
/// </summary>
public interface IEnhancedPerformanceMonitoringService
{
    /// <summary>
    /// Records a plugin operation with enhanced performance tracking.
    /// </summary>
    /// <param name="pluginName">Name of the plugin performing the operation.</param>
    /// <param name="operationType">Type of operation being performed.</param>
    /// <param name="resourceType">Resource type being processed (optional).</param>
    /// <param name="action">Action to execute and measure.</param>
    /// <returns>Result of the action execution.</returns>
    T TrackOperation<T>(string pluginName, PluginOperationType operationType, string? resourceType, Func<T> action);

    /// <summary>
    /// Records a plugin operation with enhanced performance tracking (void return).
    /// </summary>
    /// <param name="pluginName">Name of the plugin performing the operation.</param>
    /// <param name="operationType">Type of operation being performed.</param>
    /// <param name="resourceType">Resource type being processed (optional).</param>
    /// <param name="action">Action to execute and measure.</param>
    void TrackOperation(string pluginName, PluginOperationType operationType, string? resourceType, Action action);

    /// <summary>
    /// Gets the advanced performance profiler for detailed analysis.
    /// </summary>
    PluginPerformanceProfiler Profiler { get; }

    /// <summary>
    /// Gets plugin performance analysis for a specific plugin.
    /// </summary>
    /// <param name="pluginName">Name of the plugin to analyze.</param>
    /// <returns>Performance analysis or null if plugin not found.</returns>
    PluginPerformanceAnalysis? GetPluginAnalysis(string pluginName);

    /// <summary>
    /// Gets system-wide performance summary.
    /// </summary>
    /// <returns>Comprehensive system performance summary.</returns>
    SystemPerformanceSummary GetSystemSummary();
}

/// <summary>
/// Implementation of enhanced performance monitoring service.
/// </summary>
public sealed class EnhancedPerformanceMonitoringService : IEnhancedPerformanceMonitoringService, IDisposable
{
    private readonly PluginPerformanceProfiler _profiler;
    private readonly ILogger<EnhancedPerformanceMonitoringService> _logger;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the enhanced performance monitoring service.
    /// </summary>
    /// <param name="profiler">The performance profiler instance.</param>
    /// <param name="logger">Logger for the service.</param>
    public EnhancedPerformanceMonitoringService(
        PluginPerformanceProfiler profiler,
        ILogger<EnhancedPerformanceMonitoringService> logger)
    {
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Enhanced Performance Monitoring Service initialized");
    }

    /// <inheritdoc />
    public PluginPerformanceProfiler Profiler => _profiler;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T TrackOperation<T>(
        string pluginName, 
        PluginOperationType operationType, 
        string? resourceType, 
        Func<T> action)
    {
        if (_disposed) return action();

        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        bool success = false;
        T result = default!;

        try
        {
            result = action();
            success = true;
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
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryAllocated = Math.Max(0, memoryAfter - memoryBefore);

            _profiler.RecordOperation(
                pluginName,
                operationType,
                resourceType,
                stopwatch.Elapsed,
                success,
                memoryAllocated);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrackOperation(
        string pluginName, 
        PluginOperationType operationType, 
        string? resourceType, 
        Action action)
    {
        TrackOperation(pluginName, operationType, resourceType, () =>
        {
            action();
            return 0; // Return dummy value for void action
        });
    }

    /// <inheritdoc />
    public PluginPerformanceAnalysis? GetPluginAnalysis(string pluginName)
    {
        return _profiler.GetPluginAnalysis(pluginName);
    }

    /// <inheritdoc />
    public SystemPerformanceSummary GetSystemSummary()
    {
        return _profiler.GetSystemSummary();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _profiler?.Dispose();
        _logger.LogInformation("Enhanced Performance Monitoring Service disposed");
    }
}

/// <summary>
/// Extension methods for integrating enhanced performance monitoring with WrapperDealer services.
/// </summary>
public static class EnhancedMonitoringServiceCollectionExtensions
{
    /// <summary>
    /// Adds enhanced performance monitoring services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">Optional configuration for performance analysis options.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddEnhancedPerformanceMonitoring(
        this IServiceCollection services,
        Action<PerformanceAnalysisOptions>? configureOptions = null)
    {
        // Add logging if not already present
        if (services.All(s => s.ServiceType != typeof(ILoggerFactory)))
        {
            services.AddLogging(builder =>
            {
                // Configure a basic console logger for tests
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        }

        // Configure performance analysis options
        var options = new PerformanceAnalysisOptions();
        configureOptions?.Invoke(options);

        // Register the options as singleton
        services.AddSingleton(options);

        // Register the performance profiler as singleton
        services.AddSingleton<PluginPerformanceProfiler>();

        // Register the enhanced monitoring service
        services.AddSingleton<IEnhancedPerformanceMonitoringService, EnhancedPerformanceMonitoringService>();

        return services;
    }

    /// <summary>
    /// Adds enhanced performance monitoring with predefined configuration profiles.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="profile">Performance monitoring profile to use.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddEnhancedPerformanceMonitoring(
        this IServiceCollection services,
        PerformanceMonitoringProfile profile)
    {
        // Add logging if not already present
        if (services.All(s => s.ServiceType != typeof(ILoggerFactory)))
        {
            services.AddLogging(builder =>
            {
                // Configure a basic console logger for tests
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        }

        var options = profile switch
        {
            PerformanceMonitoringProfile.Development => CreateDevelopmentOptions(),
            PerformanceMonitoringProfile.Production => CreateProductionOptions(),
            PerformanceMonitoringProfile.HighThroughput => CreateHighThroughputOptions(),
            PerformanceMonitoringProfile.Minimal => CreateMinimalOptions(),
            _ => new PerformanceAnalysisOptions()
        };

        return services.AddEnhancedPerformanceMonitoring(_ => { /* options already configured */ })
                      .AddSingleton(options);
    }

    private static PerformanceAnalysisOptions CreateDevelopmentOptions()
    {
        return new PerformanceAnalysisOptions
        {
            MaxSamplesPerPlugin = 50_000,
            MaxRecentDataPoints = 100_000,
            AnalysisWindow = TimeSpan.FromHours(2),
            AnalysisInterval = TimeSpan.FromMinutes(1),
            OperationTimeoutThreshold = TimeSpan.FromSeconds(2),
            MemoryAllocationWarningThreshold = 25 * 1024 * 1024, // 25 MB
            FailureRateWarningThreshold = 0.05, // 5%
            HealthScoreWarningThreshold = 80.0,
            EnableImmediateAnalysis = true,
            TrackMemoryAllocation = true,
            TrackThreadMetrics = true
        };
    }

    private static PerformanceAnalysisOptions CreateProductionOptions()
    {
        return new PerformanceAnalysisOptions
        {
            MaxSamplesPerPlugin = 25_000,
            MaxRecentDataPoints = 50_000,
            AnalysisWindow = TimeSpan.FromHours(1),
            AnalysisInterval = TimeSpan.FromMinutes(5),
            OperationTimeoutThreshold = TimeSpan.FromSeconds(5),
            MemoryAllocationWarningThreshold = 50 * 1024 * 1024, // 50 MB
            FailureRateWarningThreshold = 0.1, // 10%
            HealthScoreWarningThreshold = 70.0,
            EnableImmediateAnalysis = true,
            TrackMemoryAllocation = true,
            TrackThreadMetrics = false
        };
    }

    private static PerformanceAnalysisOptions CreateHighThroughputOptions()
    {
        return new PerformanceAnalysisOptions
        {
            MaxSamplesPerPlugin = 100_000,
            MaxRecentDataPoints = 200_000,
            AnalysisWindow = TimeSpan.FromMinutes(30),
            AnalysisInterval = TimeSpan.FromMinutes(10),
            OperationTimeoutThreshold = TimeSpan.FromSeconds(10),
            MemoryAllocationWarningThreshold = 100 * 1024 * 1024, // 100 MB
            FailureRateWarningThreshold = 0.15, // 15%
            HealthScoreWarningThreshold = 60.0,
            EnableImmediateAnalysis = false,
            TrackMemoryAllocation = false,
            TrackThreadMetrics = false
        };
    }

    private static PerformanceAnalysisOptions CreateMinimalOptions()
    {
        return new PerformanceAnalysisOptions
        {
            MaxSamplesPerPlugin = 5_000,
            MaxRecentDataPoints = 10_000,
            AnalysisWindow = TimeSpan.FromMinutes(30),
            AnalysisInterval = TimeSpan.FromMinutes(15),
            OperationTimeoutThreshold = TimeSpan.FromSeconds(10),
            MemoryAllocationWarningThreshold = 100 * 1024 * 1024, // 100 MB
            FailureRateWarningThreshold = 0.2, // 20%
            HealthScoreWarningThreshold = 50.0,
            EnableImmediateAnalysis = false,
            TrackMemoryAllocation = false,
            TrackThreadMetrics = false
        };
    }
}

/// <summary>
/// Predefined performance monitoring profiles for different use cases.
/// </summary>
public enum PerformanceMonitoringProfile
{
    /// <summary>
    /// Development profile with detailed tracking and frequent analysis.
    /// </summary>
    Development,

    /// <summary>
    /// Production profile with balanced performance and monitoring overhead.
    /// </summary>
    Production,

    /// <summary>
    /// High throughput profile optimized for minimal monitoring overhead.
    /// </summary>
    HighThroughput,

    /// <summary>
    /// Minimal profile with basic monitoring and low memory usage.
    /// </summary>
    Minimal
}

/// <summary>
/// Performance monitoring aspect that can be applied to plugin operations using interceptors.
/// Provides automatic performance tracking without manual instrumentation.
/// </summary>
public static class PerformanceMonitoringAspect
{
    /// <summary>
    /// Creates a performance-monitored wrapper around a plugin operation.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="monitoringService">Enhanced monitoring service instance.</param>
    /// <param name="pluginName">Name of the plugin performing the operation.</param>
    /// <param name="operationType">Type of operation being performed.</param>
    /// <param name="resourceType">Resource type being processed (optional).</param>
    /// <param name="operation">The operation to monitor.</param>
    /// <returns>Performance-monitored operation wrapper.</returns>
    public static Func<T> MonitoredOperation<T>(
        this IEnhancedPerformanceMonitoringService monitoringService,
        string pluginName,
        PluginOperationType operationType,
        string? resourceType,
        Func<T> operation)
    {
        return () => monitoringService.TrackOperation(pluginName, operationType, resourceType, operation);
    }

    /// <summary>
    /// Creates a performance-monitored wrapper around a plugin operation (void return).
    /// </summary>
    /// <param name="monitoringService">Enhanced monitoring service instance.</param>
    /// <param name="pluginName">Name of the plugin performing the operation.</param>
    /// <param name="operationType">Type of operation being performed.</param>
    /// <param name="resourceType">Resource type being processed (optional).</param>
    /// <param name="operation">The operation to monitor.</param>
    /// <returns>Performance-monitored operation wrapper.</returns>
    public static Action MonitoredOperation(
        this IEnhancedPerformanceMonitoringService monitoringService,
        string pluginName,
        PluginOperationType operationType,
        string? resourceType,
        Action operation)
    {
        return () => monitoringService.TrackOperation(pluginName, operationType, resourceType, operation);
    }
}
