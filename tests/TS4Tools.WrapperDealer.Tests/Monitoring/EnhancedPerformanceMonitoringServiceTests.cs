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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TS4Tools.WrapperDealer.Monitoring;

namespace TS4Tools.WrapperDealer.Tests.Monitoring;

/// <summary>
/// Unit tests for the EnhancedPerformanceMonitoringService class.
/// </summary>
public sealed class EnhancedPerformanceMonitoringServiceTests : IDisposable
{
    private readonly EnhancedPerformanceMonitoringService _monitoringService;
    private readonly PluginPerformanceProfiler _profiler;

    public EnhancedPerformanceMonitoringServiceTests()
    {
        var options = new PerformanceAnalysisOptions
        {
            EnableImmediateAnalysis = false // Disable for testing
        };

        _profiler = new PluginPerformanceProfiler(
            NullLogger<PluginPerformanceProfiler>.Instance,
            options);

        _monitoringService = new EnhancedPerformanceMonitoringService(
            _profiler,
            NullLogger<EnhancedPerformanceMonitoringService>.Instance);
    }

    [Fact]
    public void TrackOperation_WithReturnValue_RecordsAndReturnsResult()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        const int expectedResult = 42;

        // Act
        var result = _monitoringService.TrackOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => expectedResult);

        // Assert
        Assert.Equal(expectedResult, result);
        
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(1, analysis.TotalOperations);
        Assert.Equal(1, analysis.SuccessfulOperations);
        Assert.Equal(0, analysis.FailedOperations);
    }

    [Fact]
    public void TrackOperation_VoidAction_RecordsSuccessfully()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        bool actionExecuted = false;

        // Act
        _monitoringService.TrackOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => { actionExecuted = true; });

        // Assert
        Assert.True(actionExecuted);
        
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(1, analysis.TotalOperations);
        Assert.Equal(1, analysis.SuccessfulOperations);
    }

    [Fact]
    public void TrackOperation_ThrowsException_RecordsFailureAndRethrows()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var thrownException = Assert.Throws<InvalidOperationException>(() =>
            _monitoringService.TrackOperation<int>(
                pluginName,
                PluginOperationType.WrapperCreation,
                "TestResourceType",
                () => throw expectedException));

        Assert.Same(expectedException, thrownException);
        
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(1, analysis.TotalOperations);
        Assert.Equal(0, analysis.SuccessfulOperations);
        Assert.Equal(1, analysis.FailedOperations);
        Assert.Equal(1.0, analysis.FailureRate);
    }

    [Fact]
    public void TrackOperation_MeasuresExecutionTime()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        const int delayMs = 100;

        // Act
        _monitoringService.TrackOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => Thread.Sleep(delayMs));

        // Assert
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.True(analysis.AverageResponseTime.TotalMilliseconds >= delayMs * 0.8); // Allow some tolerance
    }

    [Fact]
    public async Task TrackOperation_ConcurrentExecution_ThreadSafe()
    {
        // Arrange
        const string pluginName = "ConcurrentPlugin";
        const int taskCount = 20;
        const int operationsPerTask = 50;

        // Act
        var tasks = new Task[taskCount];
        for (int i = 0; i < taskCount; i++)
        {
            int taskId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerTask; j++)
                {
                    _monitoringService.TrackOperation(
                        pluginName,
                        PluginOperationType.WrapperCreation,
                        $"ResourceType{taskId}",
                        () => Thread.Sleep(Random.Shared.Next(1, 10)));
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(taskCount * operationsPerTask, analysis.TotalOperations);
        Assert.Equal(analysis.TotalOperations, analysis.SuccessfulOperations);
    }

    [Fact]
    public void MonitoredOperation_Extension_CreatesCorrectWrapper()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        const int expectedResult = 123;

        // Act
        var wrappedOperation = _monitoringService.MonitoredOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => expectedResult);

        var result = wrappedOperation();

        // Assert
        Assert.Equal(expectedResult, result);
        
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(1, analysis.TotalOperations);
    }

    [Fact]
    public void MonitoredOperation_VoidExtension_CreatesCorrectWrapper()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        bool executed = false;

        // Act
        var wrappedOperation = _monitoringService.MonitoredOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => { executed = true; });

        wrappedOperation();

        // Assert
        Assert.True(executed);
        
        var analysis = _monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(1, analysis.TotalOperations);
    }

    [Fact]
    public void GetSystemSummary_DelegatesToProfiler()
    {
        // Arrange
        const string pluginName = "TestPlugin";
        _monitoringService.TrackOperation(
            pluginName,
            PluginOperationType.WrapperCreation,
            "TestResourceType",
            () => 42);

        // Act
        var summary = _monitoringService.GetSystemSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalSamplesCollected);
        Assert.Equal(1, summary.TotalPlugins);
    }

    [Fact]
    public void Profiler_Property_ReturnsCorrectInstance()
    {
        // Act & Assert
        Assert.Same(_profiler, _monitoringService.Profiler);
    }

    public void Dispose()
    {
        _monitoringService?.Dispose();
        _profiler?.Dispose();
    }
}

/// <summary>
/// Integration tests for the enhanced performance monitoring dependency injection extensions.
/// </summary>
public class EnhancedMonitoringServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEnhancedPerformanceMonitoring_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEnhancedPerformanceMonitoring();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<PerformanceAnalysisOptions>());
        Assert.NotNull(serviceProvider.GetService<PluginPerformanceProfiler>());
        Assert.NotNull(serviceProvider.GetService<IEnhancedPerformanceMonitoringService>());
    }

    [Fact]
    public void AddEnhancedPerformanceMonitoring_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        const int customMaxSamples = 12345;

        // Act
        services.AddEnhancedPerformanceMonitoring(options =>
        {
            options.MaxSamplesPerPlugin = customMaxSamples;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<PerformanceAnalysisOptions>();
        Assert.Equal(customMaxSamples, options.MaxSamplesPerPlugin);
    }

    [Theory]
    [InlineData(PerformanceMonitoringProfile.Development)]
    [InlineData(PerformanceMonitoringProfile.Production)]
    [InlineData(PerformanceMonitoringProfile.HighThroughput)]
    [InlineData(PerformanceMonitoringProfile.Minimal)]
    public void AddEnhancedPerformanceMonitoring_WithProfile_ConfiguresCorrectly(PerformanceMonitoringProfile profile)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEnhancedPerformanceMonitoring(profile);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<PerformanceAnalysisOptions>();
        Assert.NotNull(options);
        
        var monitoringService = serviceProvider.GetRequiredService<IEnhancedPerformanceMonitoringService>();
        Assert.NotNull(monitoringService);

        // Verify profile-specific settings
        switch (profile)
        {
            case PerformanceMonitoringProfile.Development:
                Assert.True(options.EnableImmediateAnalysis);
                Assert.True(options.TrackMemoryAllocation);
                Assert.True(options.TrackThreadMetrics);
                break;
            case PerformanceMonitoringProfile.Production:
                Assert.True(options.EnableImmediateAnalysis);
                Assert.True(options.TrackMemoryAllocation);
                Assert.False(options.TrackThreadMetrics);
                break;
            case PerformanceMonitoringProfile.HighThroughput:
                Assert.False(options.EnableImmediateAnalysis);
                Assert.False(options.TrackMemoryAllocation);
                Assert.False(options.TrackThreadMetrics);
                break;
            case PerformanceMonitoringProfile.Minimal:
                Assert.False(options.EnableImmediateAnalysis);
                Assert.False(options.TrackMemoryAllocation);
                Assert.False(options.TrackThreadMetrics);
                break;
        }
    }

    [Fact]
    public void ServiceRegistration_CreatesUniqueInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnhancedPerformanceMonitoring();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetService<IEnhancedPerformanceMonitoringService>();
        var service2 = serviceProvider.GetService<IEnhancedPerformanceMonitoringService>();

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2); // Should be singleton
    }
}

/// <summary>
/// Performance tests to verify the monitoring system doesn't introduce significant overhead.
/// </summary>
public class PerformanceMonitoringOverheadTests
{
    [Fact]
    public void TrackOperation_MinimalOverhead()
    {
        // Arrange
        var options = new PerformanceAnalysisOptions
        {
            EnableImmediateAnalysis = false,
            TrackMemoryAllocation = false
        };

        using var profiler = new PluginPerformanceProfiler(
            NullLogger<PluginPerformanceProfiler>.Instance,
            options);

        using var monitoringService = new EnhancedPerformanceMonitoringService(
            profiler,
            NullLogger<EnhancedPerformanceMonitoringService>.Instance);

        const int operationCount = 10000;
        const string pluginName = "PerfTestPlugin";

        // Act - Measure overhead of tracking
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < operationCount; i++)
        {
            monitoringService.TrackOperation(
                pluginName,
                PluginOperationType.WrapperCreation,
                "TestResourceType",
                () => i * 2); // Trivial operation
        }
        
        stopwatch.Stop();

        // Assert - Overhead should be minimal (< 1ms per operation on average)
        var averageOverheadMs = stopwatch.Elapsed.TotalMilliseconds / operationCount;
        Assert.True(averageOverheadMs < 1.0, 
            $"Average overhead per operation ({averageOverheadMs:F3}ms) exceeds 1ms threshold");

        // Verify all operations were recorded
        var analysis = monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(operationCount, analysis.TotalOperations);
    }

    [Fact]
    public async Task ConcurrentTracking_ScalesWell()
    {
        // Arrange
        var options = new PerformanceAnalysisOptions
        {
            EnableImmediateAnalysis = false,
            TrackMemoryAllocation = false
        };

        using var profiler = new PluginPerformanceProfiler(
            NullLogger<PluginPerformanceProfiler>.Instance,
            options);

        using var monitoringService = new EnhancedPerformanceMonitoringService(
            profiler,
            NullLogger<EnhancedPerformanceMonitoringService>.Instance);

        const int threadCount = 4; // Use constant instead of Environment.ProcessorCount
        const int operationsPerThread = 1000;
        const string pluginName = "ConcurrentPerfTestPlugin";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    monitoringService.TrackOperation(
                        pluginName,
                        PluginOperationType.WrapperCreation,
                        "TestResourceType",
                        () => Thread.CurrentThread.ManagedThreadId);
                }
            });
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalOperations = threadCount * operationsPerThread;
        var averageOverheadMs = stopwatch.Elapsed.TotalMilliseconds / totalOperations;
        
        Assert.True(averageOverheadMs < 2.0, 
            $"Average concurrent overhead per operation ({averageOverheadMs:F3}ms) exceeds 2ms threshold");

        var analysis = monitoringService.GetPluginAnalysis(pluginName);
        Assert.NotNull(analysis);
        Assert.Equal(totalOperations, analysis.TotalOperations);
    }
}
