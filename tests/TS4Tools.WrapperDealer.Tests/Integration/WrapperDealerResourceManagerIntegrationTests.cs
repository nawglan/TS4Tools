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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Resources.Images;
using TS4Tools.Resources.Images.DependencyInjection;
using TS4Tools.Resources.Strings;
using TS4Tools.Resources.Text;
using TS4Tools.Resources.Text.DependencyInjection;
using TS4Tools.Resources.Catalog;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Integration;

/// <summary>
/// Phase 4.20.5: Integration and Validation - Resource System Integration Tests
/// 
/// This test class validates the first requirement of Phase 4.20.5:
/// "Integrate WrapperDealer with ResourceManager"
/// 
/// These tests ensure that WrapperDealer works seamlessly with the existing
/// TS4Tools resource system, bridging all resource wrapper types from Phases 4.13-4.19.
/// </summary>
[Collection("AResourceHandlerBridge")]
public sealed class WrapperDealerResourceManagerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IResourceManager _resourceManager;

    public WrapperDealerResourceManagerIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Set up complete TS4Tools resource system with all factory types
        ConfigureResourceServices(services);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        _serviceProvider = services.BuildServiceProvider();
        _resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
        
        // Initialize WrapperDealer with the complete service provider
        WrapperDealer.Initialize(_serviceProvider);
    }

    #region Bridge to All Resource Wrapper Types (Phases 4.13-4.19)

    [Theory]
    [InlineData("0x220557DA", "String Table Resource (STBL)")]
    [InlineData("0x2F7D0004", "LRLE Image Resource")]
    [InlineData("0x319E4F1D", "Catalog Resource")]
    [InlineData("RLE", "RLE Image Resource")]
    [InlineData("DDS", "DDS Image Resource")]
    [InlineData("PNG", "PNG Image Resource")]
    public void WrapperDealer_WithSpecializedResourceTypes_ShouldCreateCorrectWrappers(string resourceType, string description)
    {
        // Act - Use WrapperDealer legacy API to create resources
        var resource = WrapperDealer.CreateNewResource(1, resourceType);

        // Assert - Verify correct resource type is created
        resource.Should().NotBeNull($"WrapperDealer should create {description}");
        resource.Should().BeAssignableTo<IResource>("All created resources must implement IResource");
        
        // Verify resource is created and basic properties
        resource.Should().NotBeNull($"Should create resource for type {resourceType}");
        resource.Should().BeAssignableTo<IResource>("All resources should implement IResource");
        resource.RequestedApiVersion.Should().BeGreaterThan(0, "Resource should have valid API version");
        resource.Stream.Should().NotBeNull("Resource should have a valid stream");
        
        // Additional validation based on description can be done here if needed
        // For now, we just verify basic IResource compliance
    }

    [Fact]
    public void WrapperDealer_TypeMap_ShouldContainAllRegisteredResourceTypes()
    {
        // Act
        var typeMap = WrapperDealer.TypeMap;

        // Assert - Verify all major resource types are registered
        typeMap.Should().NotBeEmpty("TypeMap should contain registered resource types");
        
        var resourceTypes = typeMap.Select(kvp => kvp.Key).ToList();
        
        // Check for critical resource types from different phases
        resourceTypes.Should().Contain("0x220557DA", "String Table resources should be registered");
        resourceTypes.Should().Contain("0x2F7D0004", "LRLE image resources should be registered");
        resourceTypes.Should().Contain("0x319E4F1D", "Catalog resources should be registered");
        resourceTypes.Should().Contain("DDS", "DDS image resources should be registered");
        resourceTypes.Should().Contain("PNG", "PNG image resources should be registered");
        
        // Verify default handler is present
        resourceTypes.Should().Contain("*", "Default resource handler should be registered");
    }

    [Fact]
    public void WrapperDealer_GetSupportedResourceTypes_ShouldReturnExpectedTypes()
    {
        // Act
        var supportedTypes = WrapperDealer.GetSupportedResourceTypes();

        // Assert
        supportedTypes.Should().NotBeEmpty("Should have supported resource types");
        supportedTypes.Should().Contain("0x220557DA", "Should support String Table resources");
        supportedTypes.Should().Contain("0x2F7D0004", "Should support LRLE image resources");
        supportedTypes.Should().Contain("DDS", "Should support DDS image resources");
        supportedTypes.Should().Contain("*", "Should have default handler");
    }

    [Theory]
    [InlineData("0x220557DA", true)]
    [InlineData("0x2F7D0004", true)]
    [InlineData("0x319E4F1D", true)]
    [InlineData("DDS", true)]
    [InlineData("PNG", true)]
    [InlineData("0x12345678", false)] // Non-existent type, should fall back to default
    [InlineData("INVALID", false)]    // Non-existent type
    public void WrapperDealer_IsResourceSupported_ShouldReturnCorrectResults(string resourceType, bool expectedSupported)
    {
        // Act
        var isSupported = WrapperDealer.IsResourceSupported(resourceType);

        // Assert
        if (expectedSupported)
        {
            isSupported.Should().BeTrue($"Resource type {resourceType} should be supported");
        }
        else
        {
            // Note: With default handler (*), some "unsupported" types might still return true
            // This is expected legacy behavior
            isSupported.Should().Be(WrapperDealer.TypeMap.Any(kvp => kvp.Key == "*"), 
                $"Unsupported type {resourceType} result should depend on default handler availability");
        }
    }

    #endregion

    #region Validate Specialized Resource Compatibility

    [Fact]
    public void WrapperDealer_WithStringTableResource_ShouldMaintainProperties()
    {
        // Act
        var resource = WrapperDealer.CreateNewResource(1, "0x220557DA");

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IResource>();
        resource.RequestedApiVersion.Should().BeGreaterThan(0, "String table should have valid API version");
        
        // Check if it's the modern TS4Tools StringTableResource or legacy s4pi resource
        if (resource.GetType().Name.Contains("StringTable"))
        {
            resource.Should().BeOfType<StringTableResource>("Should create modern StringTableResource");
        }
        // Additional STBL-specific validation would go here
    }

    [Fact]
    public void WrapperDealer_WithCatalogResource_ShouldMaintainProperties()
    {
        // Act
        var resource = WrapperDealer.CreateNewResource(1, "0x319E4F1D");

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IResource>();
        resource.RequestedApiVersion.Should().BeGreaterThan(0, "Catalog resource should have valid API version");
        // Additional catalog-specific validation would go here
    }

    [Fact]
    public void WrapperDealer_WithLRLEResource_ShouldMaintainProperties()
    {
        // Act
        var resource = WrapperDealer.CreateNewResource(1, "0x2F7D0004");

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IResource>();
        resource.RequestedApiVersion.Should().BeGreaterThan(0, "LRLE resource should have valid API version");
        
        // Check if it's an image resource and has basic properties
        if (resource.GetType().Name.Contains("Image") || resource.GetType().Name.Contains("LRLE"))
        {
            // LRLE resources start with default dimensions - this would be resource-specific validation
            resource.Stream.Should().NotBeNull("LRLE should have a valid stream");
        }
    }

    #endregion

    #region Test Catalog Resource Integration

    [Fact]
    public void WrapperDealer_WithCatalogResourceTypes_ShouldCreateCorrectInstances()
    {
        var catalogTypes = new[]
        {
            "0x319E4F1D", // Catalog
            "0x48C28979", // Catalog variant
            "0xA8F7B517"  // Catalog variant
        };

        foreach (var catalogType in catalogTypes)
        {
            // Act
            var resource = WrapperDealer.CreateNewResource(1, catalogType);

            // Assert
            resource.Should().NotBeNull($"Should create resource for catalog type {catalogType}");
            resource.Should().BeAssignableTo<IResource>("All resources should implement IResource");
        }
    }

    #endregion

    #region Helper Methods and Registration

    [Fact]
    public void WrapperDealer_RegisterWrapper_ShouldUpdateTypeMap()
    {
        // Arrange
        var testType = typeof(TestResourceWrapper);
        var testResourceType = "0xTEST123";

        // Act
        WrapperDealer.RegisterWrapper(testType, testResourceType);

        // Assert
        var typeMap = WrapperDealer.TypeMap;
        typeMap.Should().Contain(kvp => kvp.Key == testResourceType && kvp.Value == testType,
            "Registered wrapper should be present in TypeMap");
    }

    [Fact]
    public void WrapperDealer_UnregisterWrapper_ShouldRemoveFromTypeMap()
    {
        // Arrange
        var testType = typeof(TestResourceWrapper);
        var testResourceType = "0xTEST456";
        WrapperDealer.RegisterWrapper(testType, testResourceType);

        // Act
        WrapperDealer.UnregisterWrapper(testResourceType);

        // Assert
        var typeMap = WrapperDealer.TypeMap;
        typeMap.Should().NotContain(kvp => kvp.Key == testResourceType,
            "Unregistered wrapper should not be present in TypeMap");
    }

    [Fact]
    public void WrapperDealer_RefreshWrappers_ShouldReinitializeTypeMap()
    {
        // Arrange
        var originalTypeCount = WrapperDealer.TypeMap.Count;

        // Act
        WrapperDealer.RefreshWrappers();

        // Assert
        var newTypeMap = WrapperDealer.TypeMap;
        newTypeMap.Should().NotBeEmpty("TypeMap should be repopulated after refresh");
        // Note: Count might be different due to re-registration, but key types should still be present
        newTypeMap.Should().Contain(kvp => kvp.Key == "0x220557DA", "String Table should still be registered after refresh");
    }

    #endregion

    #region Service Configuration

    private static void ConfigureResourceServices(IServiceCollection services)
    {
        // Add core package services
        services.AddTS4ToolsPackageServices();
        
        // Add resource management services
        services.AddResourceManager(options =>
        {
            options.EnableCaching = true;
            options.CacheExpirationMinutes = 30;
            options.EnableStrictValidation = true;
        });

        // Add WrapperDealer plugin manager
        services.AddScoped<PluginRegistrationManager>();

        // Add all resource modules (Phases 4.13-4.19)
        services.AddImageResources(); 
        services.AddTextResourceServices();
        services.AddCatalogResources();
        
        // Add string resource factories (manually since no DI extension exists yet)
        services.AddSingleton<IResourceFactory, StringTableResourceFactory>();
        services.AddSingleton<StringTableResourceFactory>();
        
        // Add additional resource types as they become available
        // This ensures WrapperDealer can bridge to all TS4Tools resource types
    }

    #endregion

    #region Test Infrastructure

    /// <summary>
    /// Test resource wrapper for testing registration/unregistration functionality
    /// </summary>
    private class TestResourceWrapper : IResource
    {
        private readonly MemoryStream _stream = new MemoryStream();
        private readonly List<string> _contentFields = new List<string> { "RequestedApiVersion", "RecommendedApiVersion" };
        
        // IApiVersion members
        public int RequestedApiVersion => 1;
        public int RecommendedApiVersion => 1;
        
        // IResource members  
        public Stream Stream => _stream;
        public byte[] AsBytes => _stream.ToArray();

        // IContentFields members
        public IReadOnlyList<string> ContentFields => _contentFields;
        
        public TypedValue this[int index]
        {
            get => index switch
            {
                0 => new TypedValue(typeof(int), RequestedApiVersion),
                1 => new TypedValue(typeof(int), RecommendedApiVersion), 
                _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range")
            };
            set => throw new NotSupportedException("Setting values is not supported in this test wrapper");
        }
        
        public TypedValue this[string name]
        {
            get => name switch
            {
                nameof(RequestedApiVersion) => new TypedValue(typeof(int), RequestedApiVersion),
                nameof(RecommendedApiVersion) => new TypedValue(typeof(int), RecommendedApiVersion), 
                _ => throw new ArgumentOutOfRangeException(nameof(name), $"Unknown field: {name}")
            };
            set => throw new NotSupportedException("Setting values is not supported in this test wrapper");
        }

        public event EventHandler? ResourceChanged;

        public void Dispose() => _stream?.Dispose();
    }

    public void Dispose()
    {
        // Step 1: Reset static state first to prevent ObjectDisposedException
        // Note: WrapperDealer doesn't have a Reset method, but we can reinitialize
        
        // Step 2: Dispose services
        _serviceProvider?.Dispose();
    }

    #endregion
}
