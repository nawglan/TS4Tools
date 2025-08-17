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

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Core.Resources.Tests;

public class ResourceManagerTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ResourceManagerOptions> _optionsMonitor;
    private readonly ILogger<ResourceManager> _logger;
    private readonly ResourceManager _resourceManager;

    public ResourceManagerTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _optionsMonitor = Substitute.For<IOptionsMonitor<ResourceManagerOptions>>();
        _logger = Substitute.For<ILogger<ResourceManager>>();

        // Setup default options
        var options = new ResourceManagerOptions();
        _optionsMonitor.CurrentValue.Returns(options);

        _resourceManager = new ResourceManager(_serviceProvider, _optionsMonitor, _logger);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var manager = new ResourceManager(_serviceProvider, _optionsMonitor, _logger);

        // Assert
        manager.Should().NotBeNull();
        var stats = manager.GetStatistics();
        stats.RegisteredFactories.Should().Be(1); // Default factory is registered
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResourceManager(null!, _optionsMonitor, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResourceManager(_serviceProvider, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("optionsMonitor");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResourceManager(_serviceProvider, _optionsMonitor, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidResourceType_ShouldCreateResource()
    {
        // Arrange
        const string resourceType = "0x12345678";
        const int apiVersion = 1;

        // Act
        var resource = await _resourceManager.CreateResourceAsync(resourceType, apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(apiVersion);

        var stats = _resourceManager.GetStatistics();
        stats.TotalResourcesCreated.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithEmptyResourceType_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _resourceManager.CreateResourceAsync("", 1);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _resourceManager.CreateResourceAsync("0x12345678", 0);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoadResourceAsync_WithValidParameters_ShouldLoadResource()
    {
        // Arrange
        var package = Substitute.For<IPackage>();
        var resourceEntry = Substitute.For<IResourceIndexEntry>();
        var resourceStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        resourceEntry["ResourceType"].Returns(TypedValue.Create("0x12345678"));
        package.GetResourceStreamAsync(resourceEntry, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<Stream?>(resourceStream));

        // Act
        var resource = await _resourceManager.LoadResourceAsync(package, resourceEntry, 1);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(1);

        var stats = _resourceManager.GetStatistics();
        stats.TotalResourcesLoaded.Should().Be(1);
    }

    [Fact]
    public async Task LoadResourceAsync_WithNullPackage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceEntry = Substitute.For<IResourceIndexEntry>();

        // Act & Assert
        var act = async () => await _resourceManager.LoadResourceAsync(null!, resourceEntry, 1);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("package");
    }

    [Fact]
    public async Task LoadResourceAsync_WithNullResourceEntry_ShouldThrowArgumentNullException()
    {
        // Arrange
        var package = Substitute.For<IPackage>();

        // Act & Assert
        var act = async () => await _resourceManager.LoadResourceAsync(package, null!, 1);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("resourceIndexEntry");
    }

    [Fact]
    public async Task LoadResourceAsync_WithForceDefaultWrapper_ShouldUseDefaultFactory()
    {
        // Arrange
        var package = Substitute.For<IPackage>();
        var resourceEntry = Substitute.For<IResourceIndexEntry>();
        var resourceStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        resourceEntry["ResourceType"].Returns(TypedValue.Create("0x12345678"));
        package.GetResourceStreamAsync(resourceEntry, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<Stream?>(resourceStream));

        // Act
        var resource = await _resourceManager.LoadResourceAsync(package, resourceEntry, 1, forceDefaultWrapper: true);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DefaultResource>();
    }

    [Fact]
    public void GetResourceTypeMap_ShouldReturnRegisteredFactories()
    {
        // Act
        var typeMap = _resourceManager.GetResourceTypeMap();

        // Assert
        typeMap.Should().NotBeNull();
        typeMap.Should().ContainKey("*"); // Default factory
        typeMap["*"].Should().Be<DefaultResourceFactory>();
    }

    [Fact]
    public void GetStatistics_ShouldReturnValidStatistics()
    {
        // Act
        var stats = _resourceManager.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalResourcesCreated.Should().Be(0);
        stats.TotalResourcesLoaded.Should().Be(0);
        stats.RegisteredFactories.Should().Be(1); // Default factory
        stats.CacheHitRatio.Should().Be(0.0);
        stats.CacheSize.Should().Be(0);
        stats.AverageCreationTimeMs.Should().Be(0.0);
        stats.AverageLoadTimeMs.Should().Be(0.0);
    }

    [Fact]
    public void RegisterFactory_WithValidFactory_ShouldRegisterSuccessfully()
    {
        // Arrange
        var testFactory = new TestResourceFactory();
        _serviceProvider.GetService<TestResourceFactory>().Returns(testFactory);

        // Act
        _resourceManager.RegisterFactory<IResource, TestResourceFactory>();

        // Assert
        var typeMap = _resourceManager.GetResourceTypeMap();
        typeMap.Should().ContainKey("0xABCDEF12");

        var stats = _resourceManager.GetStatistics();
        stats.RegisteredFactories.Should().Be(2); // Default + Test factory
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesCleanly()
    {
        // Act & Assert (Should not throw)
        _resourceManager.Dispose();
    }

    [Fact]
    public async Task LoadResourceAsync_WithCachingEnabled_ShouldCacheResources()
    {
        // Arrange
        var cachingOptions = new ResourceManagerOptions { EnableCaching = true };
        var cachingOptionsMonitor = Substitute.For<IOptionsMonitor<ResourceManagerOptions>>();
        cachingOptionsMonitor.CurrentValue.Returns(cachingOptions);

        var package = Substitute.For<IPackage>();
        var resourceEntry = Substitute.For<IResourceIndexEntry>();
        var resourceStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        resourceEntry["ResourceType"].Returns(TypedValue.Create("0x12345678"));
        package.GetResourceStreamAsync(resourceEntry, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<Stream?>(resourceStream));

        // Create a new manager with caching enabled
        var cachingManager = new ResourceManager(_serviceProvider, cachingOptionsMonitor, _logger);

        // Act - Load same resource twice
        var resource1 = await cachingManager.LoadResourceAsync(package, resourceEntry, 1);
        var resource2 = await cachingManager.LoadResourceAsync(package, resourceEntry, 1);

        // Assert
        resource1.Should().NotBeNull();
        resource2.Should().NotBeNull();

        var stats = cachingManager.GetStatistics();
        stats.TotalResourcesLoaded.Should().Be(1); // Only one actual load from package
        stats.CacheHitRatio.Should().Be(0.5); // 1 hit out of 2 requests
        stats.CacheSize.Should().BeGreaterThan(0); // Should have cached items
    }

    // Helper class for testing
    internal sealed class TestResourceFactory : IResourceFactory<IResource>
    {
        public IReadOnlySet<string> SupportedResourceTypes => new HashSet<string> { "0xABCDEF12" };
        public IReadOnlySet<uint> ResourceTypes => new HashSet<uint> { 0xABCDEF12u };
        public int Priority => 100;

        public async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new DefaultResource(apiVersion, stream);
        }

        public IResource CreateResource(Stream stream, uint resourceType)
        {
            return new DefaultResource(1, stream);
        }

        public IResource CreateEmptyResource(uint resourceType)
        {
            return new DefaultResource(1, null);
        }

        public bool CanCreateResource(uint resourceType)
        {
            return resourceType == 0xABCDEF12u;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _resourceManager?.Dispose();
        }
    }
}
