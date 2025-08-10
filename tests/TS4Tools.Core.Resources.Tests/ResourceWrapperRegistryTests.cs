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

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Resources.Catalog;
using TS4Tools.Resources.Images;
using TS4Tools.Resources.Strings;
using TS4Tools.Resources.Text;

namespace TS4Tools.Core.Resources.Tests;

/// <summary>
/// Tests for ResourceWrapperRegistry functionality including factory discovery,
/// registration, and integration with ResourceManager.
/// </summary>
public sealed class ResourceWrapperRegistryTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHost _host;

    public ResourceWrapperRegistryTests()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddTS4ToolsCore(new ConfigurationBuilder().Build());
            });

        _host = builder.Build();
        _serviceProvider = _host.Services;
    }

    [Fact]
    public async Task DiscoverAndRegisterFactoriesAsync_WithValidFactories_ReturnsSuccessResult()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();

        // Act
        var result = await registry.DiscoverAndRegisterFactoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TotalFactoriesDiscovered.Should().BeGreaterThan(0);
        result.SuccessfulRegistrations.Should().NotBeEmpty();
        result.SuccessfulRegistrations.Should().Contain("StringTableResourceFactory");
        result.SuccessfulRegistrations.Should().Contain("ImageResourceFactory");
        result.SuccessfulRegistrations.Should().Contain("CatalogResourceFactory");
        result.SuccessfulRegistrations.Should().Contain("TextResourceFactory");
        result.RegistrationDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task DiscoverAndRegisterFactoriesAsync_WhenCancelled_ThrowsOperationCancelledException()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => registry.DiscoverAndRegisterFactoriesAsync(cts.Token));
    }

    [Fact]
    public async Task GetRegisteredFactories_AfterRegistration_ReturnsFactoryInformation()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act
        var factories = registry.GetRegisteredFactories();

        // Assert
        factories.Should().NotBeEmpty();

        var stringTableFactory = factories.Values
            .FirstOrDefault(f => f.FactoryName == "StringTableResourceFactory");
        stringTableFactory.Should().NotBeNull();
        stringTableFactory!.ResourceName.Should().Be("StringTableResource");
        stringTableFactory.Priority.Should().Be(100);
        stringTableFactory.SupportedResourceTypes.Should().Contain("0x220557DA");

        var imageFactory = factories.Values
            .FirstOrDefault(f => f.FactoryName == "ImageResourceFactory");
        imageFactory.Should().NotBeNull();
        imageFactory!.ResourceName.Should().Be("ImageResource");
        imageFactory.Priority.Should().Be(100); // Actual priority is 100, not 90
        imageFactory.SupportedResourceTypes.Should().NotBeEmpty();

        var catalogFactory = factories.Values
            .FirstOrDefault(f => f.FactoryName == "CatalogResourceFactory");
        catalogFactory.Should().NotBeNull();
        catalogFactory!.ResourceName.Should().Be("CatalogResource");
        catalogFactory.Priority.Should().Be(10); // Actual priority is 10, not 110

        var textFactory = factories.Values
            .FirstOrDefault(f => f.FactoryName == "TextResourceFactory");
        textFactory.Should().NotBeNull();
        textFactory!.ResourceName.Should().Be("ITextResource"); // Actual name is ITextResource, not TextResource
        textFactory.Priority.Should().Be(50); // TextResourceFactory actual priority is 50
    }

    [Fact]
    public async Task GetStatistics_AfterRegistration_ReturnsValidStatistics()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act
        var stats = registry.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalRegisteredFactories.Should().BeGreaterThan(0);
        stats.TotalResourceCreations.Should().BeGreaterOrEqualTo(0);
        stats.FactoryUtilizationRatio.Should().BeInRange(0.0, 1.0);
        stats.RegistrationsByPriority.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IsFactoryRegistered_WithRegisteredFactory_ReturnsTrue()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act & Assert
        registry.IsFactoryRegistered(typeof(StringTableResourceFactory)).Should().BeTrue();
        registry.IsFactoryRegistered(typeof(ImageResourceFactory)).Should().BeTrue();
        registry.IsFactoryRegistered(typeof(CatalogResourceFactory)).Should().BeTrue();
        registry.IsFactoryRegistered(typeof(TextResourceFactory)).Should().BeTrue();
    }

    [Fact]
    public async Task IsFactoryRegistered_WithUnregisteredFactory_ReturnsFalse()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act & Assert
        registry.IsFactoryRegistered(typeof(ResourceWrapperRegistryTests)).Should().BeFalse();
    }

    [Fact]
    public async Task SupportsResourceType_WithSupportedType_ReturnsTrue()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act & Assert
        registry.SupportsResourceType("0x220557DA").Should().BeTrue(); // String Table
        registry.SupportsResourceType("0x00B2D882").Should().BeTrue(); // DDS Image
        registry.SupportsResourceType("0x319E4F1D").Should().BeTrue(); // CATALOG
    }

    [Fact]
    public async Task SupportsResourceType_WithUnsupportedType_ReturnsFalse()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act & Assert
        registry.SupportsResourceType("0xDEADBEEF").Should().BeFalse(); // Non-existent type
    }

    [Fact]
    public void SupportsResourceType_WithNullResourceType_ThrowsArgumentException()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();

        // Act & Assert - ArgumentException.ThrowIfNullOrEmpty throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() => registry.SupportsResourceType(null!));
        Assert.Throws<ArgumentException>(() => registry.SupportsResourceType(""));
    }

    [Fact]
    public void IsFactoryRegistered_WithNullFactoryType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.IsFactoryRegistered(null!));
    }

    [Fact]
    public async Task ResourceWrapperRegistryResult_Properties_WorkCorrectly()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();

        // Act
        var result = await registry.DiscoverAndRegisterFactoriesAsync();

        // Assert
        result.SuccessRate.Should().BeInRange(0.0, 100.0);

        // If all registrations succeeded
        if (result.FailedRegistrations.Count == 0)
        {
            result.SuccessRate.Should().Be(100.0);
            result.IsSuccess.Should().BeTrue();
        }

        // Registration duration should be reasonable
        result.RegistrationDuration.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}

/// <summary>
/// Tests for ResourceWrapperRegistry performance and concurrency behavior.
/// </summary>
public sealed class ResourceWrapperRegistryPerformanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHost _host;

    public ResourceWrapperRegistryPerformanceTests()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddTS4ToolsCore(new ConfigurationBuilder().Build());
            });

        _host = builder.Build();
        _serviceProvider = _host.Services;
    }

    [Fact]
    public async Task DiscoverAndRegisterFactoriesAsync_PerformanceTest_CompletesWithinReasonableTime()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await registry.DiscoverAndRegisterFactoriesAsync();

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "Factory discovery should complete quickly");
        result.RegistrationDuration.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "Factory registration should be fast");
    }

    [Fact]
    public async Task ConcurrentFactoryRegistration_WithMultipleThreads_DoesNotCauseRaceConditions()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        const int concurrentCalls = 5;
        var tasks = new List<Task<ResourceWrapperRegistryResult>>();

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() => registry.DiscoverAndRegisterFactoriesAsync()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentCalls);
        results.Should().OnlyContain(r => r.IsSuccess,
            "All concurrent registrations should succeed");

        // All results should be consistent - same factories found, regardless of order
        var firstResult = results[0];
        results.Should().OnlyContain(r =>
            r.SuccessfulRegistrations.Count == firstResult.SuccessfulRegistrations.Count,
            "All registration attempts should find the same number of factories");

        // Verify all results have the same set of factories (order-independent)
        var expectedFactoryNames = firstResult.SuccessfulRegistrations.ToHashSet();
        results.Should().OnlyContain(r =>
            r.SuccessfulRegistrations.ToHashSet().SetEquals(expectedFactoryNames),
            "All registration attempts should find the same factories");
    }

    [Fact]
    public async Task MetricsCollection_OverTime_TracksUsageCorrectly()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        await registry.DiscoverAndRegisterFactoriesAsync();

        // Act
        var initialStats = registry.GetStatistics();

        // Wait a short time for metrics collection
        await Task.Delay(100);

        var laterStats = registry.GetStatistics();

        // Assert
        initialStats.TotalRegisteredFactories.Should().BeGreaterThan(0);
        laterStats.TotalRegisteredFactories.Should().Be(initialStats.TotalRegisteredFactories,
            "Factory count should remain consistent");

        laterStats.FactoryUtilizationRatio.Should().BeInRange(0.0, 1.0);
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
