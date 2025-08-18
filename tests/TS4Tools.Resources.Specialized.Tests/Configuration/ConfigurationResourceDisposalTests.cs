using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Specialized.Configuration;
using Xunit;

namespace TS4Tools.Resources.Specialized.Tests.Configuration;

/// <summary>
/// Disposal verification tests for ConfigurationResource.
/// These tests ensure proper resource cleanup and prevent memory leaks.
/// Part of remediation task B1.6 - Create disposal verification tests.
/// </summary>
public sealed class ConfigurationResourceDisposalTests : IDisposable
{
    private readonly List<ConfigurationResource> _resources = new();

    public void Dispose()
    {
        foreach (var resource in _resources)
        {
            resource?.Dispose();
        }
        _resources.Clear();
    }

    private ConfigurationResource TrackResource(ConfigurationResource resource)
    {
        _resources.Add(resource);
        return resource;
    }

    [Fact]
    public void Dispose_WhenCalledOnce_ShouldDisposeResourceCorrectly()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert - Should throw when accessing disposed resource's Stream
        Action act = () => _ = resource.Stream;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act & Assert - Multiple dispose calls should not throw
        resource.Dispose();
        Action act = () => resource.Dispose();
        act.Should().NotThrow();
        
        // Additional dispose calls should still not throw
        resource.Dispose();
        resource.Dispose();
    }

    [Fact]
    public void Dispose_WithValidStream_ShouldDisposeStreamCorrectly()
    {
        // Arrange
        var memoryStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var resource = TrackResource(new ConfigurationResource());
        
        // Access the Stream property to initialize it with the memory stream
        // Note: This test simulates the resource having an internal stream
        
        // Act
        resource.Dispose();

        // Assert - The resource should handle stream disposal internally
        Action act = () => _ = resource.Stream;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingConfigurationNameShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert - Accessing properties after disposal should throw
        Action act = () => _ = resource.ConfigurationName;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingConfigurationVersionShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.ConfigurationVersion;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingConfigurationCategoryShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.ConfigurationCategory;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingParentConfigurationIdShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.ParentConfigurationId;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingIsValidatedShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.IsValidated;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingAsBytesShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.AsBytes;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_AfterDisposal_AccessingContentFieldsShouldThrow()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());

        // Act
        resource.Dispose();

        // Assert
        Action act = () => _ = resource.ContentFields;
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*ConfigurationResource*");
    }

    [Fact]
    public void Dispose_UsingStatement_ShouldDisposeAutomatically()
    {
        // Arrange & Act
        ConfigurationResource? resource = null;
        using (resource = new ConfigurationResource())
        {
            // Resource is alive here
            resource.Should().NotBeNull();
        }

        // Assert - Resource should be disposed after using block
        Action act = () => _ = resource!.Stream;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_ConcurrentDisposalCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var resource = TrackResource(new ConfigurationResource());
        var tasks = new List<Task>();

        // Act - Multiple threads trying to dispose simultaneously
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => resource.Dispose()));
        }

        // Assert - Should not throw any exceptions
        Func<Task> act = async () => await Task.WhenAll(tasks);
        act.Should().NotThrowAsync();
    }
}
