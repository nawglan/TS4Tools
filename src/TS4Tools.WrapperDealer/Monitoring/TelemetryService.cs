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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Interface for telemetry reporting service.
/// Provides abstraction for different telemetry backends (Application Insights, OpenTelemetry, etc.).
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Reports performance metrics to the configured telemetry backend.
    /// </summary>
    /// <param name="summary">Performance summary to report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReportPerformanceMetricsAsync(WrapperDealerPerformanceSummary summary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports a performance warning or anomaly.
    /// </summary>
    /// <param name="operationType">Type of operation</param>
    /// <param name="message">Warning message</param>
    /// <param name="metadata">Additional metadata</param>
    Task ReportPerformanceWarningAsync(WrapperDealerOperationType operationType, string message, IDictionary<string, object>? metadata = null);

    /// <summary>
    /// Reports system health metrics.
    /// </summary>
    /// <param name="metrics">Health metrics to report</param>
    Task ReportHealthMetricsAsync(IDictionary<string, object> metrics);
}

/// <summary>
/// Configuration options for telemetry reporting.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Whether telemetry reporting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval for automatic metrics reporting.
    /// </summary>
    public TimeSpan ReportingInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to report performance warnings immediately.
    /// </summary>
    public bool ImmediateWarningReporting { get; set; } = true;

    /// <summary>
    /// Application Insights connection string (if using Application Insights).
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }

    /// <summary>
    /// OpenTelemetry endpoint (if using OpenTelemetry).
    /// </summary>
    public string? OpenTelemetryEndpoint { get; set; }

    /// <summary>
    /// Custom telemetry endpoint for TS4Tools analytics.
    /// </summary>
    public string? CustomTelemetryEndpoint { get; set; }

    /// <summary>
    /// Maximum number of metrics to batch before sending.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to include sensitive resource type names in telemetry.
    /// </summary>
    public bool IncludeResourceTypeNames { get; set; } = false;
}

/// <summary>
/// Background service that periodically reports WrapperDealer performance metrics.
/// Phase 4.20.4 - Automatic telemetry reporting with configurable intervals.
/// </summary>
public sealed class WrapperDealerTelemetryService : IDisposable
{
    private readonly IWrapperDealerMetrics _metrics;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<WrapperDealerTelemetryService> _logger;
    private readonly TelemetryOptions _options;
    private readonly Timer? _reportingTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public WrapperDealerTelemetryService(
        IWrapperDealerMetrics metrics,
        ITelemetryService telemetryService,
        ILogger<WrapperDealerTelemetryService> logger,
        IOptions<TelemetryOptions> options)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cancellationTokenSource = new CancellationTokenSource();

        if (_options.Enabled)
        {
            _reportingTimer = new Timer(ReportMetricsCallback, null, _options.ReportingInterval, _options.ReportingInterval);
            _logger.LogInformation("WrapperDealer telemetry service started with {Interval} reporting interval", _options.ReportingInterval);
        }
        else
        {
            _logger.LogInformation("Telemetry reporting is disabled");
        }
    }

    private async void ReportMetricsCallback(object? state)
    {
        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return;

        try
        {
            var summary = _metrics.GetPerformanceSummary();
            await _telemetryService.ReportPerformanceMetricsAsync(summary, _cancellationTokenSource.Token);

            // Report performance warnings if any
            if (_options.ImmediateWarningReporting && summary.PerformanceWarnings.Count > 0)
            {
                foreach (var warning in summary.PerformanceWarnings)
                {
                    await _telemetryService.ReportPerformanceWarningAsync(
                        WrapperDealerOperationType.GetResource, // Default operation type for warnings
                        warning);
                }
            }

            // Report health metrics
            var healthMetrics = new Dictionary<string, object>
            {
                ["uptime"] = summary.CollectionDuration.TotalSeconds,
                ["total_operations"] = summary.TotalOperations,
                ["overall_success_rate"] = summary.OverallSuccessRate,
                ["operations_per_second"] = summary.OverallOperationsPerSecond,
                ["warning_count"] = summary.PerformanceWarnings.Count
            };

            await _telemetryService.ReportHealthMetricsAsync(healthMetrics);

            _logger.LogDebug("Reported telemetry metrics: {TotalOps} operations, {SuccessRate:F1}% success rate", 
                summary.TotalOperations, summary.OverallSuccessRate);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting telemetry metrics");
        }
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _logger.LogInformation("WrapperDealer telemetry service stopped");
    }
}

/// <summary>
/// Default telemetry service implementation that logs metrics.
/// Can be extended to support Application Insights, OpenTelemetry, or custom backends.
/// </summary>
public class DefaultTelemetryService : ITelemetryService
{
    private readonly ILogger<DefaultTelemetryService> _logger;
    private readonly TelemetryOptions _options;

    public DefaultTelemetryService(ILogger<DefaultTelemetryService> logger, IOptions<TelemetryOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task ReportPerformanceMetricsAsync(WrapperDealerPerformanceSummary summary, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("TelemetryService.ReportPerformanceMetrics");
        
        _logger.LogInformation("Performance Summary: {TotalOps} operations, {SuccessRate:F1}% success rate, {OpsPerSec:F1} ops/sec",
            summary.TotalOperations, summary.OverallSuccessRate, summary.OverallOperationsPerSecond);

        foreach (var (operationType, stats) in summary.StatsByOperation)
        {
            if (stats.TotalOperations > 0)
            {
                _logger.LogInformation("{OperationType}: {Count} ops, {AvgMs:F1}ms avg, {P95Ms:F1}ms P95, {SuccessRate:F1}% success",
                    operationType, stats.TotalOperations, stats.AverageDuration.TotalMilliseconds, 
                    stats.P95Duration.TotalMilliseconds, stats.SuccessRate);
            }
        }

        if (summary.TopResourceTypes.Count > 0)
        {
            var resourceTypeInfo = _options.IncludeResourceTypeNames 
                ? string.Join(", ", summary.TopResourceTypes.Select(rt => $"{rt.ResourceType}:{rt.Count}"))
                : $"{summary.TopResourceTypes.Count} resource types";
            
            _logger.LogInformation("Top resource types: {ResourceTypes}", resourceTypeInfo);
        }

        return Task.CompletedTask;
    }

    public Task ReportPerformanceWarningAsync(WrapperDealerOperationType operationType, string message, IDictionary<string, object>? metadata = null)
    {
        using var activity = Activity.Current?.Source.StartActivity("TelemetryService.ReportPerformanceWarning");
        
        _logger.LogWarning("Performance Warning [{OperationType}]: {Message}", operationType, message);
        
        if (metadata != null)
        {
            foreach (var (key, value) in metadata)
            {
                _logger.LogDebug("Metadata {Key}: {Value}", key, value);
            }
        }

        return Task.CompletedTask;
    }

    public Task ReportHealthMetricsAsync(IDictionary<string, object> metrics)
    {
        using var activity = Activity.Current?.Source.StartActivity("TelemetryService.ReportHealthMetrics");
        
        _logger.LogDebug("Health Metrics: {Metrics}", string.Join(", ", metrics.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        
        return Task.CompletedTask;
    }
}
