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
using System.Linq;
using System.Threading;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Maintains performance profile and analysis for a single plugin.
/// Thread-safe implementation for concurrent data collection and analysis.
/// </summary>
internal sealed class PluginPerformanceProfile
{
    private readonly string _pluginName;
    private readonly PerformanceAnalysisOptions _options;
    private readonly ConcurrentQueue<PerformanceDataPoint> _dataPoints;
    private readonly object _analysisLock = new();
    
    // Cached analysis results to avoid recalculation
    private volatile PluginPerformanceAnalysis? _cachedAnalysis;
    private long _lastAnalysisTimeTicks;
    private long _totalDataPoints;

    // Quick access statistics (updated atomically)
    private long _totalOperations;
    private long _successfulOperations;
    private long _failedOperations;
    private long _totalMemoryAllocated;

    /// <summary>
    /// Initializes a new performance profile for the specified plugin.
    /// </summary>
    /// <param name="pluginName">Name of the plugin to profile.</param>
    /// <param name="options">Performance analysis configuration options.</param>
    public PluginPerformanceProfile(string pluginName, PerformanceAnalysisOptions options)
    {
        _pluginName = pluginName ?? throw new ArgumentNullException(nameof(pluginName));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dataPoints = new ConcurrentQueue<PerformanceDataPoint>();
    }

    /// <summary>
    /// Gets the name of the plugin being profiled.
    /// </summary>
    public string PluginName => _pluginName;

    /// <summary>
    /// Adds a new performance data point to the profile.
    /// </summary>
    /// <param name="dataPoint">Performance data point to add.</param>
    public void AddDataPoint(PerformanceDataPoint dataPoint)
    {
        if (dataPoint.PluginName != _pluginName)
        {
            throw new ArgumentException($"Data point plugin name '{dataPoint.PluginName}' " +
                                      $"does not match profile plugin name '{_pluginName}'");
        }

        _dataPoints.Enqueue(dataPoint);
        Interlocked.Increment(ref _totalDataPoints);

        // Update quick access statistics atomically
        Interlocked.Increment(ref _totalOperations);
        if (dataPoint.Success)
        {
            Interlocked.Increment(ref _successfulOperations);
        }
        else
        {
            Interlocked.Increment(ref _failedOperations);
        }
        
        if (dataPoint.MemoryAllocated > 0)
        {
            Interlocked.Add(ref _totalMemoryAllocated, dataPoint.MemoryAllocated);
        }

        // Trim old data points if we exceed the limit
        TrimOldDataPoints();

        // Invalidate cached analysis
        _cachedAnalysis = null;
    }

    /// <summary>
    /// Gets the recent failure rate for the specified time window.
    /// </summary>
    /// <param name="timeWindow">Time window to analyze.</param>
    /// <returns>Failure rate (0.0 to 1.0) in the specified time window.</returns>
    public double GetRecentFailureRate(TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        var recentDataPoints = GetRecentDataPoints(cutoffTime).ToList();
        
        if (recentDataPoints.Count == 0)
        {
            return 0.0;
        }

        var failedCount = recentDataPoints.Count(dp => !dp.Success);
        return (double)failedCount / recentDataPoints.Count;
    }

    /// <summary>
    /// Generates comprehensive performance analysis for this plugin.
    /// Uses caching to avoid expensive recalculation for frequent requests.
    /// </summary>
    /// <returns>Detailed performance analysis.</returns>
    public PluginPerformanceAnalysis GenerateAnalysis()
    {
        // Check if we can use cached analysis (updated within last minute)
        var cached = _cachedAnalysis;
        var lastAnalysisTime = new DateTime(Interlocked.Read(ref _lastAnalysisTimeTicks));
        if (cached != null && DateTime.UtcNow - lastAnalysisTime < TimeSpan.FromMinutes(1))
        {
            return cached;
        }

        lock (_analysisLock)
        {
            // Double-check after acquiring lock
            cached = _cachedAnalysis;
            lastAnalysisTime = new DateTime(Interlocked.Read(ref _lastAnalysisTimeTicks));
            if (cached != null && DateTime.UtcNow - lastAnalysisTime < TimeSpan.FromMinutes(1))
            {
                return cached;
            }

            var analysis = PerformDetailedAnalysis();
            _cachedAnalysis = analysis;
            Interlocked.Exchange(ref _lastAnalysisTimeTicks, DateTime.UtcNow.Ticks);
            return analysis;
        }
    }

    private PluginPerformanceAnalysis PerformDetailedAnalysis()
    {
        var analysisWindow = _options.AnalysisWindow;
        var cutoffTime = DateTime.UtcNow - analysisWindow;
        var recentDataPoints = GetRecentDataPoints(cutoffTime).ToList();

        if (recentDataPoints.Count == 0)
        {
            return CreateEmptyAnalysis();
        }

        // Calculate basic statistics
        var totalOps = recentDataPoints.Count;
        var successfulOps = recentDataPoints.Count(dp => dp.Success);
        var failedOps = totalOps - successfulOps;
        var failureRate = (double)failedOps / totalOps;

        // Calculate response time statistics
        var responseTimes = recentDataPoints.Select(dp => dp.Duration.TotalMilliseconds).OrderBy(x => x).ToArray();
        var averageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average());
        var medianResponseTime = TimeSpan.FromMilliseconds(CalculatePercentile(responseTimes, 50));
        var p95ResponseTime = TimeSpan.FromMilliseconds(CalculatePercentile(responseTimes, 95));
        var p99ResponseTime = TimeSpan.FromMilliseconds(CalculatePercentile(responseTimes, 99));
        var fastestOperation = TimeSpan.FromMilliseconds(responseTimes.First());
        var slowestOperation = TimeSpan.FromMilliseconds(responseTimes.Last());

        // Calculate memory statistics
        var totalMemory = recentDataPoints.Sum(dp => dp.MemoryAllocated);
        var averageMemory = totalMemory / totalOps;

        // Generate operation type breakdown
        var operationTypeBreakdown = recentDataPoints
            .GroupBy(dp => dp.OperationType)
            .Select(group => new OperationTypeStats
            {
                OperationType = group.Key,
                OperationCount = group.Count(),
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    group.Average(dp => dp.Duration.TotalMilliseconds)),
                FailureRate = group.Average(dp => dp.Success ? 0.0 : 1.0),
                PercentageOfTotal = (double)group.Count() / totalOps * 100
            })
            .OrderByDescending(stats => stats.OperationCount)
            .ToList();

        // Generate resource type breakdown
        var resourceTypeBreakdown = recentDataPoints
            .Where(dp => !string.IsNullOrEmpty(dp.ResourceType))
            .GroupBy(dp => dp.ResourceType!)
            .Select(group => new ResourceTypeStats
            {
                ResourceType = group.Key,
                OperationCount = group.Count(),
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    group.Average(dp => dp.Duration.TotalMilliseconds)),
                FailureRate = group.Average(dp => dp.Success ? 0.0 : 1.0),
                PercentageOfTotal = (double)group.Count() / totalOps * 100
            })
            .OrderByDescending(stats => stats.OperationCount)
            .ToList();

        // Calculate performance trends
        var recentTrend = CalculatePerformanceTrends(recentDataPoints);

        // Generate recommendations
        var recommendations = GenerateRecommendations(recentDataPoints, operationTypeBreakdown, resourceTypeBreakdown);

        return new PluginPerformanceAnalysis
        {
            PluginName = _pluginName,
            AnalysisPeriod = analysisWindow,
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = totalOps,
            SuccessfulOperations = successfulOps,
            FailedOperations = failedOps,
            FailureRate = failureRate,
            AverageResponseTime = averageResponseTime,
            MedianResponseTime = medianResponseTime,
            P95ResponseTime = p95ResponseTime,
            P99ResponseTime = p99ResponseTime,
            FastestOperation = fastestOperation,
            SlowestOperation = slowestOperation,
            AverageMemoryAllocated = averageMemory,
            TotalMemoryAllocated = totalMemory,
            OperationTypeBreakdown = operationTypeBreakdown,
            ResourceTypeBreakdown = resourceTypeBreakdown,
            RecentTrend = recentTrend,
            Recommendations = recommendations
        };
    }

    private PluginPerformanceAnalysis CreateEmptyAnalysis()
    {
        return new PluginPerformanceAnalysis
        {
            PluginName = _pluginName,
            AnalysisPeriod = _options.AnalysisWindow,
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = 0,
            SuccessfulOperations = 0,
            FailedOperations = 0,
            FailureRate = 0.0,
            AverageResponseTime = TimeSpan.Zero,
            MedianResponseTime = TimeSpan.Zero,
            P95ResponseTime = TimeSpan.Zero,
            P99ResponseTime = TimeSpan.Zero,
            FastestOperation = TimeSpan.Zero,
            SlowestOperation = TimeSpan.Zero,
            AverageMemoryAllocated = 0,
            TotalMemoryAllocated = 0,
            OperationTypeBreakdown = [],
            ResourceTypeBreakdown = [],
            RecentTrend = new PerformanceTrend
            {
                ResponseTimeTrend = TrendDirection.Unknown,
                FailureRateTrend = TrendDirection.Unknown,
                OperationVolumeTrend = TrendDirection.Unknown,
                ResponseTimeChangePercent = 0.0,
                FailureRateChangePercent = 0.0,
                OperationVolumeChangePercent = 0.0,
                TrendPeriod = _options.AnalysisWindow
            },
            Recommendations = ["No performance data available - plugin may not be actively used"]
        };
    }

    private IEnumerable<PerformanceDataPoint> GetRecentDataPoints(DateTime cutoffTime)
    {
        return _dataPoints.Where(dp => dp.Timestamp >= cutoffTime);
    }

    private void TrimOldDataPoints()
    {
        // Remove excess data points to maintain memory limits
        while (_dataPoints.Count > _options.MaxSamplesPerPlugin && _dataPoints.TryDequeue(out _))
        {
            // Data point is automatically removed by TryDequeue
        }
    }

    private static double CalculatePercentile(double[] sortedValues, double percentile)
    {
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

    private PerformanceTrend CalculatePerformanceTrends(List<PerformanceDataPoint> dataPoints)
    {
        var trendPeriod = TimeSpan.FromMinutes(30); // Analyze last 30 minutes for trends
        var midPoint = DateTime.UtcNow - (trendPeriod / 2);

        var olderPoints = dataPoints.Where(dp => dp.Timestamp < midPoint).ToList();
        var newerPoints = dataPoints.Where(dp => dp.Timestamp >= midPoint).ToList();

        if (olderPoints.Count < 5 || newerPoints.Count < 5)
        {
            return new PerformanceTrend
            {
                ResponseTimeTrend = TrendDirection.Unknown,
                FailureRateTrend = TrendDirection.Unknown,
                OperationVolumeTrend = TrendDirection.Unknown,
                ResponseTimeChangePercent = 0.0,
                FailureRateChangePercent = 0.0,
                OperationVolumeChangePercent = 0.0,
                TrendPeriod = trendPeriod
            };
        }

        // Calculate average response times
        var olderAvgResponseTime = olderPoints.Average(dp => dp.Duration.TotalMilliseconds);
        var newerAvgResponseTime = newerPoints.Average(dp => dp.Duration.TotalMilliseconds);
        var responseTimeChange = ((newerAvgResponseTime - olderAvgResponseTime) / olderAvgResponseTime) * 100;

        // Calculate failure rates
        var olderFailureRate = olderPoints.Average(dp => dp.Success ? 0.0 : 1.0);
        var newerFailureRate = newerPoints.Average(dp => dp.Success ? 0.0 : 1.0);
        var failureRateChange = olderFailureRate > 0 
            ? ((newerFailureRate - olderFailureRate) / olderFailureRate) * 100 
            : (newerFailureRate > 0 ? 100.0 : 0.0);

        // Calculate operation volume change
        var olderOpsPerMinute = olderPoints.Count / (trendPeriod.TotalMinutes / 2);
        var newerOpsPerMinute = newerPoints.Count / (trendPeriod.TotalMinutes / 2);
        var volumeChange = olderOpsPerMinute > 0 
            ? ((newerOpsPerMinute - olderOpsPerMinute) / olderOpsPerMinute) * 100 
            : (newerOpsPerMinute > 0 ? 100.0 : 0.0);

        return new PerformanceTrend
        {
            ResponseTimeTrend = DetermineTrendDirection(responseTimeChange, isInverse: true), // Lower is better
            FailureRateTrend = DetermineTrendDirection(failureRateChange, isInverse: true),   // Lower is better
            OperationVolumeTrend = DetermineTrendDirection(volumeChange, isInverse: false),  // Higher is better
            ResponseTimeChangePercent = responseTimeChange,
            FailureRateChangePercent = failureRateChange,
            OperationVolumeChangePercent = volumeChange,
            TrendPeriod = trendPeriod
        };
    }

    private static TrendDirection DetermineTrendDirection(double changePercent, bool isInverse)
    {
        const double significantChangeThreshold = 10.0; // 10% change threshold

        if (Math.Abs(changePercent) < significantChangeThreshold)
        {
            return TrendDirection.Stable;
        }

        bool isImproving = isInverse ? changePercent < 0 : changePercent > 0;
        return isImproving ? TrendDirection.Improving : TrendDirection.Degrading;
    }

    private List<string> GenerateRecommendations(
        List<PerformanceDataPoint> dataPoints,
        List<OperationTypeStats> operationStats,
        List<ResourceTypeStats> resourceStats)
    {
        var recommendations = new List<string>();

        // Response time recommendations
        var avgResponseTime = dataPoints.Average(dp => dp.Duration.TotalMilliseconds);
        if (avgResponseTime > _options.OperationTimeoutThreshold.TotalMilliseconds)
        {
            recommendations.Add($"Average response time ({avgResponseTime:F1}ms) exceeds threshold. " +
                              "Consider optimizing critical code paths.");
        }

        // Failure rate recommendations
        var failureRate = dataPoints.Average(dp => dp.Success ? 0.0 : 1.0);
        if (failureRate > _options.FailureRateWarningThreshold)
        {
            recommendations.Add($"High failure rate ({failureRate * 100:F1}%) detected. " +
                              "Review error handling and input validation.");
        }

        // Memory allocation recommendations
        var avgMemory = dataPoints.Average(dp => dp.MemoryAllocated);
        if (avgMemory > _options.MemoryAllocationWarningThreshold)
        {
            recommendations.Add($"High memory allocation ({avgMemory / (1024.0 * 1024.0):F1}MB average) detected. " +
                              "Consider object pooling or reduced allocations.");
        }

        // Operation type specific recommendations
        var slowestOperation = operationStats.OrderByDescending(s => s.AverageResponseTime).FirstOrDefault();
        if (slowestOperation?.AverageResponseTime.TotalMilliseconds > 1000)
        {
            recommendations.Add($"Operation type '{slowestOperation.OperationType}' is slow " +
                              $"({slowestOperation.AverageResponseTime.TotalMilliseconds:F1}ms average). " +
                              "Focus optimization efforts here.");
        }

        // Resource type specific recommendations
        var problematicResource = resourceStats.OrderByDescending(s => s.FailureRate).FirstOrDefault();
        if (problematicResource?.FailureRate > 0.1)
        {
            recommendations.Add($"Resource type '{problematicResource.ResourceType}' has high failure rate " +
                              $"({problematicResource.FailureRate * 100:F1}%). " +
                              "Review processing logic for this resource type.");
        }

        // Volume-based recommendations
        if (dataPoints.Count < 10)
        {
            recommendations.Add("Low operation volume detected. Plugin may not be actively used or " +
                              "may have initialization issues.");
        }

        // Threading recommendations
        var threadCount = dataPoints.Select(dp => dp.ThreadId).Distinct().Count();
        if (threadCount > Environment.ProcessorCount * 2)
        {
            recommendations.Add($"High thread usage ({threadCount} threads) detected. " +
                              "Consider limiting concurrent operations to improve performance.");
        }

        return recommendations.Count > 0 ? recommendations : ["Plugin performance is within normal parameters"];
    }
}
