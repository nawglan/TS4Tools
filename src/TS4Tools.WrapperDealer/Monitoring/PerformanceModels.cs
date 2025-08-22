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

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Represents a single performance data point for plugin operation analysis.
/// </summary>
public sealed record PerformanceDataPoint
{
    /// <summary>
    /// Name of the plugin that performed the operation.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Type of operation performed by the plugin.
    /// </summary>
    public required PluginOperationType OperationType { get; init; }

    /// <summary>
    /// Resource type being processed (if applicable).
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Time taken to complete the operation.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Number of bytes allocated during the operation.
    /// </summary>
    public long MemoryAllocated { get; init; }

    /// <summary>
    /// Timestamp when the operation was recorded.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Thread ID where the operation was performed.
    /// </summary>
    public int ThreadId { get; init; }

    /// <summary>
    /// Sequence number for ordering data points.
    /// </summary>
    public long SequenceNumber { get; init; }
}

/// <summary>
/// Types of operations that can be performed by plugins.
/// </summary>
public enum PluginOperationType
{
    /// <summary>
    /// Plugin initialization operation.
    /// </summary>
    Initialization,

    /// <summary>
    /// Resource wrapper creation operation.
    /// </summary>
    WrapperCreation,

    /// <summary>
    /// Resource reading/parsing operation.
    /// </summary>
    ResourceReading,

    /// <summary>
    /// Resource writing/serialization operation.
    /// </summary>
    ResourceWriting,

    /// <summary>
    /// Resource validation operation.
    /// </summary>
    ResourceValidation,

    /// <summary>
    /// Plugin dependency resolution operation.
    /// </summary>
    DependencyResolution,

    /// <summary>
    /// Assembly loading operation.
    /// </summary>
    AssemblyLoading,

    /// <summary>
    /// Cache operation (read/write/invalidate).
    /// </summary>
    CacheOperation,

    /// <summary>
    /// Metadata extraction operation.
    /// </summary>
    MetadataExtraction,

    /// <summary>
    /// Plugin disposal/cleanup operation.
    /// </summary>
    Disposal
}

/// <summary>
/// Configuration options for performance analysis system.
/// </summary>
public sealed class PerformanceAnalysisOptions
{
    /// <summary>
    /// Maximum number of performance samples to retain per plugin.
    /// Default: 10,000 samples.
    /// </summary>
    public int MaxSamplesPerPlugin { get; set; } = 10_000;

    /// <summary>
    /// Maximum number of recent data points to keep in memory for real-time analysis.
    /// Default: 50,000 data points.
    /// </summary>
    public int MaxRecentDataPoints { get; set; } = 50_000;

    /// <summary>
    /// Time window for performance analysis calculations.
    /// Default: 1 hour.
    /// </summary>
    public TimeSpan AnalysisWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Interval for performing periodic analysis and generating reports.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan AnalysisInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Threshold for operation timeout warnings.
    /// Default: 5 seconds.
    /// </summary>
    public TimeSpan OperationTimeoutThreshold { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Threshold for memory allocation warnings (in bytes).
    /// Default: 50 MB.
    /// </summary>
    public long MemoryAllocationWarningThreshold { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Threshold for failure rate warnings (0.0 to 1.0).
    /// Default: 0.1 (10%).
    /// </summary>
    public double FailureRateWarningThreshold { get; set; } = 0.1;

    /// <summary>
    /// Health score threshold below which warnings are generated.
    /// Default: 70.0 (out of 100).
    /// </summary>
    public double HealthScoreWarningThreshold { get; set; } = 70.0;

    /// <summary>
    /// Whether to enable immediate analysis and alerting for performance issues.
    /// Default: true.
    /// </summary>
    public bool EnableImmediateAnalysis { get; set; } = true;

    /// <summary>
    /// Percentiles to calculate for response time analysis.
    /// Default: 50th, 95th, 99th percentiles.
    /// </summary>
    public double[] ResponseTimePercentiles { get; set; } = [50, 95, 99];

    /// <summary>
    /// Whether to track memory allocation for each operation.
    /// Default: true (may have performance impact).
    /// </summary>
    public bool TrackMemoryAllocation { get; set; } = true;

    /// <summary>
    /// Whether to track thread-level performance metrics.
    /// Default: false (reduces overhead).
    /// </summary>
    public bool TrackThreadMetrics { get; set; } = false;
}

/// <summary>
/// Comprehensive performance analysis for a specific plugin.
/// </summary>
public sealed record PluginPerformanceAnalysis
{
    /// <summary>
    /// Name of the plugin being analyzed.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Time period covered by this analysis.
    /// </summary>
    public required TimeSpan AnalysisPeriod { get; init; }

    /// <summary>
    /// Timestamp when the analysis was generated.
    /// </summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Total number of operations recorded for this plugin.
    /// </summary>
    public required long TotalOperations { get; init; }

    /// <summary>
    /// Number of successful operations.
    /// </summary>
    public required long SuccessfulOperations { get; init; }

    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public required long FailedOperations { get; init; }

    /// <summary>
    /// Overall failure rate (0.0 to 1.0).
    /// </summary>
    public required double FailureRate { get; init; }

    /// <summary>
    /// Average response time across all operations.
    /// </summary>
    public required TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Median response time (50th percentile).
    /// </summary>
    public required TimeSpan MedianResponseTime { get; init; }

    /// <summary>
    /// 95th percentile response time.
    /// </summary>
    public required TimeSpan P95ResponseTime { get; init; }

    /// <summary>
    /// 99th percentile response time.
    /// </summary>
    public required TimeSpan P99ResponseTime { get; init; }

    /// <summary>
    /// Fastest recorded operation time.
    /// </summary>
    public required TimeSpan FastestOperation { get; init; }

    /// <summary>
    /// Slowest recorded operation time.
    /// </summary>
    public required TimeSpan SlowestOperation { get; init; }

    /// <summary>
    /// Average memory allocated per operation (in bytes).
    /// </summary>
    public required long AverageMemoryAllocated { get; init; }

    /// <summary>
    /// Total memory allocated across all operations (in bytes).
    /// </summary>
    public required long TotalMemoryAllocated { get; init; }

    /// <summary>
    /// Performance breakdown by operation type.
    /// </summary>
    public required IReadOnlyList<OperationTypeStats> OperationTypeBreakdown { get; init; }

    /// <summary>
    /// Performance breakdown by resource type.
    /// </summary>
    public required IReadOnlyList<ResourceTypeStats> ResourceTypeBreakdown { get; init; }

    /// <summary>
    /// Recent performance trend indicators.
    /// </summary>
    public required PerformanceTrend RecentTrend { get; init; }

    /// <summary>
    /// Plugin-specific performance recommendations.
    /// </summary>
    public required IReadOnlyList<string> Recommendations { get; init; }
}

/// <summary>
/// Performance statistics for a specific operation type.
/// </summary>
public sealed record OperationTypeStats
{
    /// <summary>
    /// Type of operation.
    /// </summary>
    public required PluginOperationType OperationType { get; init; }

    /// <summary>
    /// Number of operations of this type.
    /// </summary>
    public required long OperationCount { get; init; }

    /// <summary>
    /// Average response time for this operation type.
    /// </summary>
    public required TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Failure rate for this operation type.
    /// </summary>
    public required double FailureRate { get; init; }

    /// <summary>
    /// Percentage of total plugin operations.
    /// </summary>
    public required double PercentageOfTotal { get; init; }
}

/// <summary>
/// Performance statistics for a specific resource type.
/// </summary>
public sealed record ResourceTypeStats
{
    /// <summary>
    /// Resource type identifier.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Number of operations for this resource type.
    /// </summary>
    public required long OperationCount { get; init; }

    /// <summary>
    /// Average response time for this resource type.
    /// </summary>
    public required TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Failure rate for this resource type.
    /// </summary>
    public required double FailureRate { get; init; }

    /// <summary>
    /// Percentage of total plugin operations.
    /// </summary>
    public required double PercentageOfTotal { get; init; }
}

/// <summary>
/// Performance trend analysis over recent time periods.
/// </summary>
public sealed record PerformanceTrend
{
    /// <summary>
    /// Trend direction for response times.
    /// </summary>
    public required TrendDirection ResponseTimeTrend { get; init; }

    /// <summary>
    /// Trend direction for failure rates.
    /// </summary>
    public required TrendDirection FailureRateTrend { get; init; }

    /// <summary>
    /// Trend direction for operation volume.
    /// </summary>
    public required TrendDirection OperationVolumeTrend { get; init; }

    /// <summary>
    /// Percentage change in average response time.
    /// </summary>
    public required double ResponseTimeChangePercent { get; init; }

    /// <summary>
    /// Percentage change in failure rate.
    /// </summary>
    public required double FailureRateChangePercent { get; init; }

    /// <summary>
    /// Percentage change in operation volume.
    /// </summary>
    public required double OperationVolumeChangePercent { get; init; }

    /// <summary>
    /// Time period used for trend calculation.
    /// </summary>
    public required TimeSpan TrendPeriod { get; init; }
}

/// <summary>
/// Direction of a performance trend.
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Performance is improving.
    /// </summary>
    Improving,

    /// <summary>
    /// Performance is stable.
    /// </summary>
    Stable,

    /// <summary>
    /// Performance is degrading.
    /// </summary>
    Degrading,

    /// <summary>
    /// Insufficient data to determine trend.
    /// </summary>
    Unknown
}

/// <summary>
/// System-wide performance summary across all plugins.
/// </summary>
public sealed record SystemPerformanceSummary
{
    /// <summary>
    /// Timestamp when the summary was generated.
    /// </summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Total number of plugins in the system.
    /// </summary>
    public required int TotalPlugins { get; init; }

    /// <summary>
    /// Total number of performance samples collected.
    /// </summary>
    public required long TotalSamplesCollected { get; init; }

    /// <summary>
    /// Time window used for analysis.
    /// </summary>
    public required TimeSpan AnalysisWindowSize { get; init; }

    /// <summary>
    /// System-wide average response time (50th percentile).
    /// </summary>
    public double SystemAverageResponseTime { get; set; }

    /// <summary>
    /// System-wide 95th percentile response time.
    /// </summary>
    public double SystemP95ResponseTime { get; set; }

    /// <summary>
    /// System-wide 99th percentile response time.
    /// </summary>
    public double SystemP99ResponseTime { get; set; }

    /// <summary>
    /// Overall success rate across all plugins (0.0 to 100.0).
    /// </summary>
    public double OverallSuccessRate { get; set; }

    /// <summary>
    /// Performance analysis for each plugin.
    /// </summary>
    public required IReadOnlyList<PluginPerformanceAnalysis> PluginAnalyses { get; init; }

    /// <summary>
    /// Top performance issues identified across the system.
    /// </summary>
    public IReadOnlyList<PerformanceIssue> TopPerformanceIssues { get; set; } = [];

    /// <summary>
    /// Resource types that are performance hotspots.
    /// </summary>
    public IReadOnlyList<ResourceTypeHotspot> ResourceTypeHotspots { get; set; } = [];

    /// <summary>
    /// System-wide performance recommendations.
    /// </summary>
    public IReadOnlyList<string> PerformanceRecommendations { get; set; } = [];
}

/// <summary>
/// Represents a performance issue detected in the system.
/// </summary>
public sealed record PerformanceIssue
{
    /// <summary>
    /// Name of the plugin associated with the issue.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Type of performance issue.
    /// </summary>
    public required PerformanceIssueType IssueType { get; init; }

    /// <summary>
    /// Severity level of the issue.
    /// </summary>
    public required PerformanceIssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Impact measure (typically operation count affected).
    /// </summary>
    public required long Impact { get; init; }
}

/// <summary>
/// Types of performance issues that can be detected.
/// </summary>
public enum PerformanceIssueType
{
    /// <summary>
    /// Operations are taking too long to complete.
    /// </summary>
    HighResponseTime,

    /// <summary>
    /// High rate of operation failures.
    /// </summary>
    HighFailureRate,

    /// <summary>
    /// Excessive memory allocation during operations.
    /// </summary>
    ExcessiveMemoryAllocation,

    /// <summary>
    /// Plugin performance is degrading over time.
    /// </summary>
    PerformanceDegradation,

    /// <summary>
    /// Resource contention or threading issues.
    /// </summary>
    ResourceContention,

    /// <summary>
    /// Inefficient operation patterns.
    /// </summary>
    InefficientOperations
}

/// <summary>
/// Severity levels for performance issues.
/// </summary>
public enum PerformanceIssueSeverity
{
    /// <summary>
    /// Low severity - minor impact.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - noticeable impact.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - significant impact.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - system stability at risk.
    /// </summary>
    Critical
}

/// <summary>
/// Resource type that represents a performance hotspot.
/// </summary>
public sealed record ResourceTypeHotspot
{
    /// <summary>
    /// Resource type identifier.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Total number of operations for this resource type.
    /// </summary>
    public required int TotalOperations { get; init; }

    /// <summary>
    /// Average response time for this resource type.
    /// </summary>
    public required TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Failure rate for this resource type.
    /// </summary>
    public required double FailureRate { get; init; }

    /// <summary>
    /// Top plugins processing this resource type.
    /// </summary>
    public required IReadOnlyList<PluginResourceStats> TopPlugins { get; init; }
}

/// <summary>
/// Performance statistics for a plugin processing a specific resource type.
/// </summary>
public sealed record PluginResourceStats
{
    /// <summary>
    /// Name of the plugin.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Number of operations performed by this plugin.
    /// </summary>
    public required int OperationCount { get; init; }

    /// <summary>
    /// Average response time for this plugin.
    /// </summary>
    public required TimeSpan AverageResponseTime { get; init; }
}

/// <summary>
/// Plugin health score with associated analysis.
/// </summary>
public sealed record PluginHealthScore
{
    /// <summary>
    /// Name of the plugin.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Health score (0.0 to 100.0, higher is better).
    /// </summary>
    public required double HealthScore { get; init; }

    /// <summary>
    /// Detailed performance analysis for the plugin.
    /// </summary>
    public required PluginPerformanceAnalysis Analysis { get; init; }
}

/// <summary>
/// Comprehensive performance data export structure.
/// </summary>
public sealed record PerformanceDataExport
{
    /// <summary>
    /// Timestamp when the export was generated.
    /// </summary>
    public required DateTime ExportTimestamp { get; init; }

    /// <summary>
    /// System-wide performance summary.
    /// </summary>
    public required SystemPerformanceSummary SystemSummary { get; init; }

    /// <summary>
    /// Plugin health rankings.
    /// </summary>
    public required IReadOnlyList<PluginHealthScore> PluginHealthRankings { get; init; }

    /// <summary>
    /// Configuration used by the profiler.
    /// </summary>
    public required ProfilerConfigurationSnapshot ProfilerConfiguration { get; init; }

    /// <summary>
    /// Raw performance data points (optional, for detailed analysis).
    /// </summary>
    public IReadOnlyList<PerformanceDataPoint>? RawDataPoints { get; set; }
}

/// <summary>
/// Snapshot of profiler configuration at export time.
/// </summary>
public sealed record ProfilerConfigurationSnapshot
{
    /// <summary>
    /// Configuration options used by the profiler.
    /// </summary>
    public PerformanceAnalysisOptions Options { get; init; }

    /// <summary>
    /// Timestamp when the configuration was captured.
    /// </summary>
    public DateTime ConfigurationTimestamp { get; init; } = DateTime.UtcNow;

    public ProfilerConfigurationSnapshot(PerformanceAnalysisOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }
}
