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
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Implementation of WrapperDealer performance metrics collection.
/// Phase 4.20.4 - Optimization and Monitoring implementation.
/// Thread-safe metrics collection with performance regression detection.
/// </summary>
public sealed class WrapperDealerMetrics : IWrapperDealerMetrics
{
    private readonly ILogger<WrapperDealerMetrics> _logger;
    private readonly ConcurrentDictionary<WrapperDealerOperationType, ConcurrentQueue<OperationMetric>> _metrics;
    private readonly ConcurrentDictionary<string, long> _resourceTypeCounts;
    private readonly object _lockObject = new();
    private long _totalOperations;

    /// <inheritdoc />
    public DateTime CollectionStartTime { get; }

    /// <inheritdoc />
    public long TotalOperations => _totalOperations;

    public WrapperDealerMetrics(ILogger<WrapperDealerMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new ConcurrentDictionary<WrapperDealerOperationType, ConcurrentQueue<OperationMetric>>();
        _resourceTypeCounts = new ConcurrentDictionary<string, long>();
        CollectionStartTime = DateTime.UtcNow;

        // Initialize queues for all operation types
        foreach (var operationType in Enum.GetValues<WrapperDealerOperationType>())
        {
            _metrics[operationType] = new ConcurrentQueue<OperationMetric>();
        }

        _logger.LogInformation("WrapperDealer performance metrics collection started at {StartTime}", CollectionStartTime);
    }

    /// <inheritdoc />
    public void RecordGetResourceOperation(string resourceType, TimeSpan duration, bool success)
    {
        RecordOperation(WrapperDealerOperationType.GetResource, resourceType, duration, success);
    }

    /// <inheritdoc />
    public void RecordCreateResourceOperation(string resourceType, TimeSpan duration, bool success)
    {
        RecordOperation(WrapperDealerOperationType.CreateResource, resourceType, duration, success);
    }

    /// <inheritdoc />
    public void RecordTypeMapLookup(string resourceType, TimeSpan duration, bool found)
    {
        RecordOperation(WrapperDealerOperationType.TypeMapLookup, resourceType, duration, found);
    }

    /// <inheritdoc />
    public void RecordPluginLoadOperation(string pluginName, TimeSpan duration, bool success)
    {
        RecordOperation(WrapperDealerOperationType.PluginLoad, pluginName, duration, success);
    }

    /// <inheritdoc />
    public void RecordWrapperRegistration(string wrapperType, int resourceTypeCount, TimeSpan duration)
    {
        RecordOperation(WrapperDealerOperationType.WrapperRegistration, wrapperType, duration, true, resourceTypeCount);
    }

    /// <inheritdoc />
    public WrapperDealerPerformanceStats GetPerformanceStats(WrapperDealerOperationType operationType)
    {
        if (!_metrics.TryGetValue(operationType, out var queue))
        {
            return new WrapperDealerPerformanceStats();
        }

        var operations = queue.ToArray();
        if (operations.Length == 0)
        {
            return new WrapperDealerPerformanceStats();
        }

        var successful = operations.Count(o => o.Success);
        var durations = operations.Select(o => o.Duration).OrderBy(d => d).ToArray();
        var collectionDuration = DateTime.UtcNow - CollectionStartTime;

        return new WrapperDealerPerformanceStats
        {
            TotalOperations = operations.Length,
            SuccessfulOperations = successful,
            FailedOperations = operations.Length - successful,
            AverageDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinimumDuration = durations.First(),
            MaximumDuration = durations.Last(),
            P95Duration = GetPercentile(durations, 0.95),
            P99Duration = GetPercentile(durations, 0.99),
            OperationsPerSecond = collectionDuration.TotalSeconds > 0 ? operations.Length / collectionDuration.TotalSeconds : 0
        };
    }

    /// <inheritdoc />
    public WrapperDealerPerformanceSummary GetPerformanceSummary()
    {
        var statsByOperation = new Dictionary<WrapperDealerOperationType, WrapperDealerPerformanceStats>();
        long totalOps = 0;
        long totalSuccessful = 0;

        foreach (var operationType in Enum.GetValues<WrapperDealerOperationType>())
        {
            var stats = GetPerformanceStats(operationType);
            statsByOperation[operationType] = stats;
            totalOps += stats.TotalOperations;
            totalSuccessful += stats.SuccessfulOperations;
        }

        var collectionDuration = DateTime.UtcNow - CollectionStartTime;
        var topResourceTypes = _resourceTypeCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToArray();

        var warnings = DetectPerformanceWarnings(statsByOperation);

        return new WrapperDealerPerformanceSummary
        {
            StatsByOperation = statsByOperation,
            TotalOperations = totalOps,
            OverallSuccessRate = totalOps > 0 ? (double)totalSuccessful / totalOps * 100.0 : 0.0,
            CollectionDuration = collectionDuration,
            OverallOperationsPerSecond = collectionDuration.TotalSeconds > 0 ? totalOps / collectionDuration.TotalSeconds : 0,
            TopResourceTypes = topResourceTypes,
            PerformanceWarnings = warnings
        };
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lockObject)
        {
            foreach (var queue in _metrics.Values)
            {
                while (queue.TryDequeue(out _)) { }
            }
            _resourceTypeCounts.Clear();
            _totalOperations = 0;
        }

        _logger.LogInformation("WrapperDealer performance metrics reset at {ResetTime}", DateTime.UtcNow);
    }

    private void RecordOperation(WrapperDealerOperationType operationType, string identifier, TimeSpan duration, bool success, int? additionalData = null)
    {
        var metric = new OperationMetric
        {
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            Success = success,
            Identifier = identifier,
            AdditionalData = additionalData
        };

        _metrics[operationType].Enqueue(metric);
        
        // Track resource type usage
        if (operationType == WrapperDealerOperationType.GetResource || 
            operationType == WrapperDealerOperationType.CreateResource ||
            operationType == WrapperDealerOperationType.TypeMapLookup)
        {
            _resourceTypeCounts.AddOrUpdate(identifier, 1, (_, count) => count + 1);
        }

        Interlocked.Increment(ref _totalOperations);

        // Log performance warnings for slow operations
        if (duration > TimeSpan.FromMilliseconds(100))
        {
            _logger.LogWarning("Slow {OperationType} operation for {Identifier}: {Duration}ms", 
                operationType, identifier, duration.TotalMilliseconds);
        }

        // Log failures
        if (!success)
        {
            _logger.LogWarning("Failed {OperationType} operation for {Identifier}", operationType, identifier);
        }
    }

    private static TimeSpan GetPercentile(TimeSpan[] sortedDurations, double percentile)
    {
        if (sortedDurations.Length == 0) return TimeSpan.Zero;
        
        var index = (int)Math.Ceiling(sortedDurations.Length * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedDurations.Length - 1));
        return sortedDurations[index];
    }

    private List<string> DetectPerformanceWarnings(IReadOnlyDictionary<WrapperDealerOperationType, WrapperDealerPerformanceStats> stats)
    {
        var warnings = new List<string>();

        foreach (var (operationType, operationStats) in stats)
        {
            if (operationStats.TotalOperations == 0) continue;

            // Check success rate
            if (operationStats.SuccessRate < 95.0)
            {
                warnings.Add($"{operationType} has low success rate: {operationStats.SuccessRate:F1}%");
            }

            // Check average duration thresholds
            var thresholds = new Dictionary<WrapperDealerOperationType, TimeSpan>
            {
                [WrapperDealerOperationType.GetResource] = TimeSpan.FromMilliseconds(50),
                [WrapperDealerOperationType.CreateResource] = TimeSpan.FromMilliseconds(100),
                [WrapperDealerOperationType.TypeMapLookup] = TimeSpan.FromMilliseconds(10),
                [WrapperDealerOperationType.PluginLoad] = TimeSpan.FromSeconds(5),
                [WrapperDealerOperationType.WrapperRegistration] = TimeSpan.FromMilliseconds(200)
            };

            if (thresholds.TryGetValue(operationType, out var threshold) && operationStats.AverageDuration > threshold)
            {
                warnings.Add($"{operationType} average duration ({operationStats.AverageDuration.TotalMilliseconds:F1}ms) exceeds threshold ({threshold.TotalMilliseconds}ms)");
            }

            // Check P99 duration for outliers
            if (operationStats.P99Duration > operationStats.AverageDuration * 5)
            {
                warnings.Add($"{operationType} has high P99 outliers: {operationStats.P99Duration.TotalMilliseconds:F1}ms vs average {operationStats.AverageDuration.TotalMilliseconds:F1}ms");
            }
        }

        return warnings;
    }

    private record OperationMetric
    {
        public DateTime Timestamp { get; init; }
        public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public string Identifier { get; init; } = string.Empty;
        public int? AdditionalData { get; init; }
    }
}
