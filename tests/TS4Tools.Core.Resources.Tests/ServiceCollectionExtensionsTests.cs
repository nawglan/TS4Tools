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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Core.Resources.Tests;

public class ResourceManagerOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new ResourceManagerOptions();

        // Assert
        options.EnableCaching.Should().BeTrue();
        options.MaxCacheSize.Should().Be(1000);
        options.MaxCacheMemoryMB.Should().Be(100);
        options.CacheExpirationMinutes.Should().Be(30);
        options.EnableStrictValidation.Should().BeTrue();
        options.ThrowOnMissingHandler.Should().BeTrue();
        options.LoadTimeoutSeconds.Should().Be(30);
        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Assert
        ResourceManagerOptions.SectionName.Should().Be("ResourceManager");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new ResourceManagerOptions();

        // Act
        options.EnableCaching = false;
        options.MaxCacheSize = 500;
        options.MaxCacheMemoryMB = 50;
        options.CacheExpirationMinutes = 15;
        options.EnableStrictValidation = false;
        options.ThrowOnMissingHandler = false;
        options.LoadTimeoutSeconds = 60;
        options.EnableMetrics = false;

        // Assert
        options.EnableCaching.Should().BeFalse();
        options.MaxCacheSize.Should().Be(500);
        options.MaxCacheMemoryMB.Should().Be(50);
        options.CacheExpirationMinutes.Should().Be(15);
        options.EnableStrictValidation.Should().BeFalse();
        options.ThrowOnMissingHandler.Should().BeFalse();
        options.LoadTimeoutSeconds.Should().Be(60);
        options.EnableMetrics.Should().BeFalse();
    }
}

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResourceManager_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = Substitute.For<IConfiguration>();
        var configSection = Substitute.For<IConfigurationSection>();

        configuration.GetSection("ResourceManager").Returns(configSection);

        // Act
        var result = services.AddResourceManager(configuration);

        // Assert
        result.Should().BeSameAs(services);
        services.Should().Contain(sd => sd.ServiceType == typeof(IResourceManager));
        services.Should().Contain(sd => sd.ServiceType == typeof(DefaultResourceFactory));
    }

    [Fact]
    public void AddResourceManager_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddResourceManager(null!, configuration);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddResourceManager_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddResourceManager((IConfiguration)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void AddResourceManager_WithConfigureAction_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<ResourceManagerOptions> configureOptions = options =>
        {
            options.EnableCaching = false;
            options.MaxCacheSize = 500;
        };

        // Act
        var result = services.AddResourceManager(configureOptions);

        // Assert
        result.Should().BeSameAs(services);
        services.Should().Contain(sd => sd.ServiceType == typeof(IResourceManager));
        services.Should().Contain(sd => sd.ServiceType == typeof(DefaultResourceFactory));
    }

    [Fact]
    public void AddResourceManager_WithNullConfigureAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddResourceManager((Action<ResourceManagerOptions>)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configureOptions");
    }

    [Fact]
    public void AddResourceFactory_WithValidParameters_ShouldRegisterFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddResourceFactory<IResource, TestResourceFactory>();

        // Assert
        result.Should().BeSameAs(services);
        services.Should().Contain(sd => sd.ServiceType == typeof(TestResourceFactory));
    }

    [Fact]
    public void AddResourceFactory_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddResourceFactory<IResource, TestResourceFactory>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddResourceFactory_WithDifferentLifetime_ShouldRespectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResourceFactory<IResource, TestResourceFactory>(ServiceLifetime.Transient);

        // Assert
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(TestResourceFactory));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    // Helper class for testing
    internal class TestResourceFactory : IResourceFactory<IResource>
    {
        public IReadOnlySet<string> SupportedResourceTypes => new HashSet<string> { "0x12345678" };
        public IReadOnlySet<uint> ResourceTypes => new HashSet<uint> { 0x12345678u };
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
            return resourceType == 0x12345678u;
        }
    }
}
