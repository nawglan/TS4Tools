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
/// Interface for WrapperDealer performance metrics collection.
/// Phase 4.20.4 - Optimization and Monitoring implementation.
/// </summary>
public interface IWrapperDealerMetrics
{
    /// <summary>
    /// Records the duration of a GetResource operation.
    /// </summary>
    /// <param name="resourceType">The resource type being retrieved</param>
    /// <param name="duration">The operation duration</param>
    /// <param name="success">Whether the operation succeeded</param>
    void RecordGetResourceOperation(string resourceType, TimeSpan duration, bool success);

    /// <summary>
    /// Records the duration of a CreateNewResource operation.
    /// </summary>
    /// <param name="resourceType">The resource type being created</param>
    /// <param name="duration">The operation duration</param>
    /// <param name="success">Whether the operation succeeded</param>
    void RecordCreateResourceOperation(string resourceType, TimeSpan duration, bool success);

    /// <summary>
    /// Records the duration of a TypeMap lookup operation.
    /// </summary>
    /// <param name="resourceType">The resource type being looked up</param>
    /// <param name="duration">The lookup duration</param>
    /// <param name="found">Whether the type was found</param>
    void RecordTypeMapLookup(string resourceType, TimeSpan duration, bool found);

    /// <summary>
    /// Records plugin loading performance.
    /// </summary>
    /// <param name="pluginName">The plugin being loaded</param>
    /// <param name="duration">The loading duration</param>
    /// <param name="success">Whether loading succeeded</param>
    void RecordPluginLoadOperation(string pluginName, TimeSpan duration, bool success);

    /// <summary>
    /// Records wrapper registration performance.
    /// </summary>
    /// <param name="wrapperType">The wrapper type being registered</param>
    /// <param name="resourceTypeCount">Number of resource types being registered</param>
    /// <param name="duration">The registration duration</param>
    void RecordWrapperRegistration(string wrapperType, int resourceTypeCount, TimeSpan duration);

    /// <summary>
    /// Gets performance statistics for a specific operation type.
    /// </summary>
    /// <param name="operationType">The type of operation</param>
    /// <returns>Performance statistics</returns>
    WrapperDealerPerformanceStats GetPerformanceStats(WrapperDealerOperationType operationType);

    /// <summary>
    /// Gets comprehensive performance summary.
    /// </summary>
    /// <returns>Complete performance summary</returns>
    WrapperDealerPerformanceSummary GetPerformanceSummary();

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the total number of operations recorded.
    /// </summary>
    long TotalOperations { get; }

    /// <summary>
    /// Gets the metrics collection start time.
    /// </summary>
    DateTime CollectionStartTime { get; }
}

/// <summary>
/// Types of WrapperDealer operations for performance tracking.
/// </summary>
public enum WrapperDealerOperationType
{
    /// <summary>GetResource() operations</summary>
    GetResource,
    
    /// <summary>CreateNewResource() operations</summary>
    CreateResource,
    
    /// <summary>TypeMap lookup operations</summary>
    TypeMapLookup,
    
    /// <summary>Plugin loading operations</summary>
    PluginLoad,
    
    /// <summary>Wrapper registration operations</summary>
    WrapperRegistration
}

/// <summary>
/// Performance statistics for a specific operation type.
/// </summary>
public record WrapperDealerPerformanceStats
{
    /// <summary>Total number of operations</summary>
    public long TotalOperations { get; init; }
    
    /// <summary>Number of successful operations</summary>
    public long SuccessfulOperations { get; init; }
    
    /// <summary>Number of failed operations</summary>
    public long FailedOperations { get; init; }
    
    /// <summary>Average operation duration</summary>
    public TimeSpan AverageDuration { get; init; }
    
    /// <summary>Minimum operation duration</summary>
    public TimeSpan MinimumDuration { get; init; }
    
    /// <summary>Maximum operation duration</summary>
    public TimeSpan MaximumDuration { get; init; }
    
    /// <summary>95th percentile duration</summary>
    public TimeSpan P95Duration { get; init; }
    
    /// <summary>99th percentile duration</summary>
    public TimeSpan P99Duration { get; init; }
    
    /// <summary>Success rate as a percentage</summary>
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100.0 : 0.0;
    
    /// <summary>Operations per second</summary>
    public double OperationsPerSecond { get; init; }
}

/// <summary>
/// Comprehensive performance summary for all WrapperDealer operations.
/// </summary>
public record WrapperDealerPerformanceSummary
{
    /// <summary>Performance statistics by operation type</summary>
    public IReadOnlyDictionary<WrapperDealerOperationType, WrapperDealerPerformanceStats> StatsByOperation { get; init; } = new Dictionary<WrapperDealerOperationType, WrapperDealerPerformanceStats>();
    
    /// <summary>Total operations across all types</summary>
    public long TotalOperations { get; init; }
    
    /// <summary>Overall success rate</summary>
    public double OverallSuccessRate { get; init; }
    
    /// <summary>Collection duration</summary>
    public TimeSpan CollectionDuration { get; init; }
    
    /// <summary>Overall operations per second</summary>
    public double OverallOperationsPerSecond { get; init; }
    
    /// <summary>Most frequently used resource types</summary>
    public IReadOnlyList<(string ResourceType, long Count)> TopResourceTypes { get; init; } = Array.Empty<(string, long)>();
    
    /// <summary>Performance regression indicators</summary>
    public IReadOnlyList<string> PerformanceWarnings { get; init; } = Array.Empty<string>();
}
