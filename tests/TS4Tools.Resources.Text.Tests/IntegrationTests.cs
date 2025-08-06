using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Text.Tests;

/// <summary>
/// Simple integration tests to verify core functionality works.
/// </summary>
public class IntegrationTests
{
    /// <summary>
    /// Tests that a TextResource can be successfully created with valid constructor parameters.
    /// </summary>
    [Fact]
    public void TextResource_CanBeCreated_WithValidConstructor()
    {
        // Arrange
        var logger = NullLogger<TextResource>.Instance;
        var resourceKey = new ResourceKey(0x03B33DDF, 0x00000000, 0x123456789ABCDEF0UL);

        // Act
        using var textResource = new TextResource(resourceKey, logger, 1);

        // Assert
        textResource.Should().NotBeNull();
        textResource.Content.Should().BeEmpty();
        textResource.Encoding.Should().Be(Encoding.UTF8);
    }

    /// <summary>
    /// Tests that a TextResourceFactory can be successfully created with valid constructor parameters.
    /// </summary>
    [Fact]
    public void TextResourceFactory_CanBeCreated_WithValidConstructor()
    {
        // Arrange
        var logger = NullLogger<TextResourceFactory>.Instance;

        // Act
        var factory = new TextResourceFactory(logger);

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
        factory.Priority.Should().Be(50);
    }

    /// <summary>
    /// Tests that the TextResourceFactory can create a resource with a null stream input.
    /// </summary>
    [Fact]
    public async Task TextResourceFactory_CanCreateResource_WithNullStream()
    {
        // Arrange
        var logger = NullLogger<TextResourceFactory>.Instance;
        var factory = new TextResourceFactory(logger);

        // Act
        var resource = await factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Content.Should().BeEmpty();
        resource.Encoding.Should().Be(Encoding.UTF8);
    }

    /// <summary>
    /// Tests that the TextResourceFactory can create a resource from a stream containing string data.
    /// </summary>
    [Fact]
    public async Task TextResourceFactory_CanCreateResource_WithStringStream()
    {
        // Arrange
        var logger = NullLogger<TextResourceFactory>.Instance;
        var factory = new TextResourceFactory(logger);
        var testContent = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));

        // Act
        var resource = await factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Content.Should().Be(testContent);
        resource.Encoding.Should().Be(Encoding.UTF8);
    }
}
