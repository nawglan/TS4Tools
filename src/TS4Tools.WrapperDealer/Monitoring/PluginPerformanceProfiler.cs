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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Advanced plugin performance profiler that provides detailed analysis of plugin operations.
/// Phase 4.20+ Enhancement: Comprehensive performance profiling for plugin ecosystem optimization.
/// </summary>
/// <remarks>
/// This profiler extends the basic monitoring system to provide:
/// - Detailed timing analysis with percentile calculations
/// - Memory allocation tracking per plugin
/// - Plugin dependency chain performance analysis
/// - Resource type processing hotspot identification
/// - Performance regression detection
/// - Plugin health scoring and recommendations
/// </remarks>
public sealed class PluginPerformanceProfiler : IDisposable
{
    private readonly ILogger<PluginPerformanceProfiler> _logger;
    private readonly Timer _analysisTimer;
    private readonly ConcurrentDictionary<string, PluginPerformanceProfile> _pluginProfiles;
    private readonly ConcurrentQueue<PerformanceDataPoint> _recentDataPoints;
    private readonly PerformanceAnalysisOptions _options;
    private volatile bool _disposed;
    private long _totalSamplesCollected;

    /// <summary>
    /// Initializes a new instance of the PluginPerformanceProfiler.
    /// </summary>
    /// <param name="logger">Logger for profiler operations.</param>
    /// <param name="options">Configuration options for performance analysis.</param>
    public PluginPerformanceProfiler(
        ILogger<PluginPerformanceProfiler> logger,
        PerformanceAnalysisOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new PerformanceAnalysisOptions();
        _pluginProfiles = new ConcurrentDictionary<string, PluginPerformanceProfile>();
        _recentDataPoints = new ConcurrentQueue<PerformanceDataPoint>();

        // Start periodic analysis timer
        _analysisTimer = new Timer(PerformPeriodicAnalysis, null, 
            _options.AnalysisInterval, _options.AnalysisInterval);

        _logger.LogInformation("Plugin Performance Profiler initialized with {SampleSize} sample retention", 
            _options.MaxSamplesPerPlugin);
    }

    /// <summary>
    /// Records a plugin operation for performance analysis.
    /// </summary>
    /// <param name="pluginName">Name of the plugin performing the operation.</param>
    /// <param name="operationType">Type of operation being performed.</param>
    /// <param name="resourceType">Resource type being processed (if applicable).</param>
    /// <param name="duration">Time taken to complete the operation.</param>
    /// <param name="success">Whether the operation completed successfully.</param>
    /// <param name="memoryAllocated">Bytes allocated during the operation (if tracked).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordOperation(
        string pluginName,
        PluginOperationType operationType,
        string? resourceType,
        TimeSpan duration,
        bool success,
        long memoryAllocated = 0)
    {
        if (_disposed) return;

        var dataPoint = new PerformanceDataPoint
        {
            PluginName = pluginName,
            OperationType = operationType,
            ResourceType = resourceType,
            Duration = duration,
            Success = success,
            MemoryAllocated = memoryAllocated,
            Timestamp = DateTime.UtcNow,
            ThreadId = Thread.CurrentThread.ManagedThreadId,
            SequenceNumber = Interlocked.Increment(ref _totalSamplesCollected)
        };

        // Add to recent data points for real-time analysis
        _recentDataPoints.Enqueue(dataPoint);
        TrimRecentDataPoints();

        // Update plugin-specific profile
        var profile = _pluginProfiles.GetOrAdd(pluginName, _ => new PluginPerformanceProfile(pluginName, _options));
        profile.AddDataPoint(dataPoint);

        // Check for immediate performance alerts
        if (_options.EnableImmediateAnalysis)
        {
            CheckForPerformanceAlerts(dataPoint, profile);
        }
    }

    /// <summary>
    /// Gets comprehensive performance analysis for a specific plugin.
    /// </summary>
    /// <param name="pluginName">Name of the plugin to analyze.</param>
    /// <returns>Detailed performance analysis or null if plugin not found.</returns>
    public PluginPerformanceAnalysis? GetPluginAnalysis(string pluginName)
    {
        if (!_pluginProfiles.TryGetValue(pluginName, out var profile))
        {
            return null;
        }

        return profile.GenerateAnalysis();
    }

    /// <summary>
    /// Gets system-wide performance summary across all plugins.
    /// </summary>
    /// <returns>Comprehensive system performance summary.</returns>
    public SystemPerformanceSummary GetSystemSummary()
    {
        var allProfiles = _pluginProfiles.Values.ToList();
        var recentDataPoints = ExtractRecentDataPoints().ToList();

        var summary = new SystemPerformanceSummary
        {
            GeneratedAt = DateTime.UtcNow,
            TotalPlugins = allProfiles.Count,
            TotalSamplesCollected = _totalSamplesCollected,
            AnalysisWindowSize = _options.AnalysisWindow,
            PluginAnalyses = allProfiles.Select(p => p.GenerateAnalysis()).ToList()
        };

        // Calculate system-wide metrics
        if (recentDataPoints.Count > 0)
        {
            summary.SystemAverageResponseTime = CalculatePercentile(
                recentDataPoints.Select(dp => dp.Duration.TotalMilliseconds), 50);
            summary.SystemP95ResponseTime = CalculatePercentile(
                recentDataPoints.Select(dp => dp.Duration.TotalMilliseconds), 95);
            summary.SystemP99ResponseTime = CalculatePercentile(
                recentDataPoints.Select(dp => dp.Duration.TotalMilliseconds), 99);
            summary.OverallSuccessRate = recentDataPoints.Average(dp => dp.Success ? 1.0 : 0.0) * 100;
        }

        // Identify top performance issues
        summary.TopPerformanceIssues = IdentifyTopPerformanceIssues(allProfiles);
        summary.ResourceTypeHotspots = IdentifyResourceTypeHotspots(recentDataPoints);
        summary.PerformanceRecommendations = GenerateSystemRecommendations(summary);

        return summary;
    }

    /// <summary>
    /// Gets plugins ranked by performance health score.
    /// </summary>
    /// <returns>List of plugins ordered by performance health (best to worst).</returns>
    public IReadOnlyList<PluginHealthScore> GetPluginHealthRankings()
    {
        return _pluginProfiles.Values
            .Select(profile => new PluginHealthScore
            {
                PluginName = profile.PluginName,
                HealthScore = CalculateHealthScore(profile),
                Analysis = profile.GenerateAnalysis()
            })
            .OrderByDescending(score => score.HealthScore)
            .ToList();
    }

    /// <summary>
    /// Exports detailed performance data for external analysis tools.
    /// </summary>
    /// <param name="includeRawData">Whether to include raw data points in export.</param>
    /// <returns>Exportable performance data structure.</returns>
    public PerformanceDataExport ExportPerformanceData(bool includeRawData = false)
    {
        var export = new PerformanceDataExport
        {
            ExportTimestamp = DateTime.UtcNow,
            SystemSummary = GetSystemSummary(),
            PluginHealthRankings = GetPluginHealthRankings(),
            ProfilerConfiguration = new ProfilerConfigurationSnapshot(_options)
        };

        if (includeRawData)
        {
            export.RawDataPoints = ExtractRecentDataPoints().ToList();
        }

        return export;
    }

    /// <summary>
    /// Resets all collected performance data and profiles.
    /// </summary>
    public void Reset()
    {
        _pluginProfiles.Clear();
        
        // Clear recent data points
        while (_recentDataPoints.TryDequeue(out _)) { }
        
        Interlocked.Exchange(ref _totalSamplesCollected, 0);

        _logger.LogInformation("Plugin Performance Profiler reset - all data cleared");
    }

    private void PerformPeriodicAnalysis(object? state)
    {
        if (_disposed) return;

        try
        {
            var summary = GetSystemSummary();
            
            _logger.LogDebug("Periodic analysis: {PluginCount} plugins, {SampleCount} total samples, " +
                           "{SuccessRate:F1}% success rate",
                summary.TotalPlugins, summary.TotalSamplesCollected, summary.OverallSuccessRate);

            // Log any performance warnings
            if (summary.TopPerformanceIssues.Any())
            {
                _logger.LogWarning("Performance issues detected: {IssueCount} issues across {PluginCount} plugins",
                    summary.TopPerformanceIssues.Count, 
                    summary.TopPerformanceIssues.Select(i => i.PluginName).Distinct().Count());
            }

            // Emit health score alerts for poorly performing plugins
            var poorlyPerformingPlugins = GetPluginHealthRankings()
                .Where(score => score.HealthScore < _options.HealthScoreWarningThreshold)
                .Take(5); // Limit to top 5 worst performers

            foreach (var plugin in poorlyPerformingPlugins)
            {
                _logger.LogWarning("Plugin health warning: {PluginName} has health score {HealthScore:F1}/100",
                    plugin.PluginName, plugin.HealthScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic performance analysis");
        }
    }

    private void CheckForPerformanceAlerts(PerformanceDataPoint dataPoint, PluginPerformanceProfile profile)
    {
        // Check for operation timeout
        if (dataPoint.Duration > _options.OperationTimeoutThreshold)
        {
            _logger.LogWarning("Plugin operation timeout: {PluginName} {OperationType} took {Duration:F1}ms " +
                             "(threshold: {Threshold:F1}ms)",
                dataPoint.PluginName, dataPoint.OperationType, 
                dataPoint.Duration.TotalMilliseconds, _options.OperationTimeoutThreshold.TotalMilliseconds);
        }

        // Check for excessive memory allocation
        if (dataPoint.MemoryAllocated > _options.MemoryAllocationWarningThreshold)
        {
            _logger.LogWarning("Plugin excessive memory allocation: {PluginName} allocated {MemoryMB:F1}MB " +
                             "in single operation (threshold: {ThresholdMB:F1}MB)",
                dataPoint.PluginName, dataPoint.MemoryAllocated / (1024.0 * 1024.0),
                _options.MemoryAllocationWarningThreshold / (1024.0 * 1024.0));
        }

        // Check for operation failure patterns
        if (!dataPoint.Success)
        {
            var recentFailureRate = profile.GetRecentFailureRate(TimeSpan.FromMinutes(5));
            if (recentFailureRate > _options.FailureRateWarningThreshold)
            {
                _logger.LogWarning("Plugin high failure rate: {PluginName} has {FailureRate:F1}% failure rate " +
                                 "in last 5 minutes (threshold: {Threshold:F1}%)",
                    dataPoint.PluginName, recentFailureRate * 100, 
                    _options.FailureRateWarningThreshold * 100);
            }
        }
    }

    private void TrimRecentDataPoints()
    {
        while (_recentDataPoints.Count > _options.MaxRecentDataPoints && 
               _recentDataPoints.TryDequeue(out _))
        {
            // Remove oldest data points to maintain size limit
        }
    }

    private IEnumerable<PerformanceDataPoint> ExtractRecentDataPoints()
    {
        var cutoffTime = DateTime.UtcNow - _options.AnalysisWindow;
        return _recentDataPoints.Where(dp => dp.Timestamp >= cutoffTime);
    }

    private static double CalculatePercentile(IEnumerable<double> values, double percentile)
    {
        var sortedValues = values.OrderBy(x => x).ToArray();
        if (sortedValues.Length == 0) return 0;

        var index = (percentile / 100.0) * (sortedValues.Length - 1);
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var weight = index - lowerIndex;
        return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
    }

    private List<PerformanceIssue> IdentifyTopPerformanceIssues(List<PluginPerformanceProfile> profiles)
    {
        var issues = new List<PerformanceIssue>();

        foreach (var profile in profiles)
        {
            var analysis = profile.GenerateAnalysis();
            
            // High response time issues
            if (analysis.AverageResponseTime.TotalMilliseconds > _options.OperationTimeoutThreshold.TotalMilliseconds)
            {
                issues.Add(new PerformanceIssue
                {
                    PluginName = profile.PluginName,
                    IssueType = PerformanceIssueType.HighResponseTime,
                    Severity = PerformanceIssueSeverity.High,
                    Description = $"Average response time {analysis.AverageResponseTime.TotalMilliseconds:F1}ms " +
                                $"exceeds threshold {_options.OperationTimeoutThreshold.TotalMilliseconds:F1}ms",
                    Impact = analysis.TotalOperations
                });
            }

            // High failure rate issues
            if (analysis.FailureRate > _options.FailureRateWarningThreshold)
            {
                issues.Add(new PerformanceIssue
                {
                    PluginName = profile.PluginName,
                    IssueType = PerformanceIssueType.HighFailureRate,
                    Severity = analysis.FailureRate > 0.5 ? PerformanceIssueSeverity.Critical : PerformanceIssueSeverity.High,
                    Description = $"Failure rate {analysis.FailureRate * 100:F1}% exceeds threshold " +
                                $"{_options.FailureRateWarningThreshold * 100:F1}%",
                    Impact = analysis.TotalOperations
                });
            }

            // Memory allocation issues
            if (analysis.AverageMemoryAllocated > _options.MemoryAllocationWarningThreshold)
            {
                issues.Add(new PerformanceIssue
                {
                    PluginName = profile.PluginName,
                    IssueType = PerformanceIssueType.ExcessiveMemoryAllocation,
                    Severity = PerformanceIssueSeverity.Medium,
                    Description = $"Average memory allocation {analysis.AverageMemoryAllocated / (1024.0 * 1024.0):F1}MB " +
                                $"exceeds threshold {_options.MemoryAllocationWarningThreshold / (1024.0 * 1024.0):F1}MB",
                    Impact = analysis.TotalOperations
                });
            }
        }

        return issues.OrderByDescending(i => i.Severity).ThenByDescending(i => i.Impact).Take(10).ToList();
    }

    private List<ResourceTypeHotspot> IdentifyResourceTypeHotspots(List<PerformanceDataPoint> dataPoints)
    {
        return dataPoints
            .Where(dp => !string.IsNullOrEmpty(dp.ResourceType))
            .GroupBy(dp => dp.ResourceType!)
            .Select(group => new ResourceTypeHotspot
            {
                ResourceType = group.Key,
                TotalOperations = group.Count(),
                AverageResponseTime = TimeSpan.FromMilliseconds(group.Average(dp => dp.Duration.TotalMilliseconds)),
                FailureRate = group.Average(dp => dp.Success ? 0.0 : 1.0),
                TopPlugins = group.GroupBy(dp => dp.PluginName)
                    .Select(pluginGroup => new PluginResourceStats
                    {
                        PluginName = pluginGroup.Key,
                        OperationCount = pluginGroup.Count(),
                        AverageResponseTime = TimeSpan.FromMilliseconds(
                            pluginGroup.Average(dp => dp.Duration.TotalMilliseconds))
                    })
                    .OrderByDescending(stats => stats.OperationCount)
                    .Take(5)
                    .ToList()
            })
            .OrderByDescending(hotspot => hotspot.TotalOperations)
            .Take(20)
            .ToList();
    }

    private List<string> GenerateSystemRecommendations(SystemPerformanceSummary summary)
    {
        var recommendations = new List<string>();

        // System-wide recommendations
        if (summary.OverallSuccessRate < 95)
        {
            recommendations.Add($"Overall success rate is {summary.OverallSuccessRate:F1}%. " +
                              "Consider investigating plugin stability issues.");
        }

        if (summary.SystemP95ResponseTime > _options.OperationTimeoutThreshold.TotalMilliseconds * 0.8)
        {
            recommendations.Add($"95th percentile response time is {summary.SystemP95ResponseTime:F1}ms. " +
                              "Consider optimizing slow plugins or increasing timeout thresholds.");
        }

        // Plugin-specific recommendations
        var slowPlugins = summary.PluginAnalyses
            .Where(analysis => analysis.AverageResponseTime > _options.OperationTimeoutThreshold)
            .Take(3);

        foreach (var plugin in slowPlugins)
        {
            recommendations.Add($"Plugin '{plugin.PluginName}' has high average response time " +
                              $"({plugin.AverageResponseTime.TotalMilliseconds:F1}ms). Consider profiling and optimization.");
        }

        // Resource type recommendations
        var problematicResourceTypes = summary.ResourceTypeHotspots
            .Where(hotspot => hotspot.FailureRate > _options.FailureRateWarningThreshold)
            .Take(3);

        foreach (var resourceType in problematicResourceTypes)
        {
            recommendations.Add($"Resource type '{resourceType.ResourceType}' has high failure rate " +
                              $"({resourceType.FailureRate * 100:F1}%). Review plugins handling this resource type.");
        }

        return recommendations;
    }

    private double CalculateHealthScore(PluginPerformanceProfile profile)
    {
        var analysis = profile.GenerateAnalysis();
        double score = 100.0;

        // Penalize high response times (0-40 points)
        var responseTimePenalty = Math.Min(40, 
            (analysis.AverageResponseTime.TotalMilliseconds / _options.OperationTimeoutThreshold.TotalMilliseconds) * 40);
        score -= responseTimePenalty;

        // Penalize high failure rates (0-30 points)
        var failureRatePenalty = Math.Min(30, analysis.FailureRate * 100 * 0.3);
        score -= failureRatePenalty;

        // Penalize excessive memory allocation (0-20 points)
        if (analysis.AverageMemoryAllocated > 0)
        {
            var memoryPenalty = Math.Min(20,
                (analysis.AverageMemoryAllocated / (double)_options.MemoryAllocationWarningThreshold) * 20);
            score -= memoryPenalty;
        }

        // Penalize low operation count (indicates infrequent use or initialization issues) (0-10 points)
        if (analysis.TotalOperations < 10)
        {
            score -= (10 - analysis.TotalOperations);
        }

        return Math.Max(0, score);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _analysisTimer?.Dispose();
        _logger.LogInformation("Plugin Performance Profiler disposed");
    }
}
