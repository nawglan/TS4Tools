using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Text.Tests;

/// <summary>
/// Tests for TextResourceFactory focusing on factory behavior without duplicating business logic.
/// </summary>
public class TextResourceFactoryTests
{
    private readonly ILogger<TextResourceFactory> _logger;
    private readonly TextResourceFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResourceFactoryTests"/> class.
    /// </summary>
    public TextResourceFactoryTests()
    {
        _logger = NullLogger<TextResourceFactory>.Instance;
        _factory = new TextResourceFactory(_logger);
    }

    /// <summary>
    /// Tests that the TextResourceFactory constructor creates a valid instance when provided with a valid logger.
    /// </summary>
    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act
        var factory = new TextResourceFactory(_logger);

        // Assert
        factory.Should().NotBeNull();
        factory.Name.Should().NotBeNullOrEmpty();
        factory.Description.Should().NotBeNullOrEmpty();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that the TextResourceFactory constructor throws ArgumentNullException when provided with a null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TextResourceFactory(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that the Name property returns a non-empty string.
    /// </summary>
    [Fact]
    public void Name_ShouldReturnNonEmptyString()
    {
        // Act & Assert
        _factory.Name.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that the Description property returns a non-empty string.
    /// </summary>
    [Fact]
    public void Description_ShouldReturnNonEmptyString()
    {
        // Act & Assert
        _factory.Description.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that the SupportedResourceTypes collection contains known text resource type identifiers.
    /// </summary>
    [Fact]
    public void SupportedResourceTypes_ShouldContainKnownTextResourceTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("0x03B33DDF"); // TS4 XML tuning
        supportedTypes.Should().Contain("0x0069453E"); // TS4 XML objective
        supportedTypes.Should().Contain("0x339BC5BD"); // TS4 XML statistic
    }

    /// <summary>
    /// Tests that CanCreateResource returns true for supported text resource types.
    /// </summary>
    /// <param name="resourceType">The resource type to test.</param>
    [Theory]
    [InlineData(0x03B33DDF)] // TS4 XML tuning
    [InlineData(0x0069453E)] // TS4 XML objective  
    [InlineData(0x339BC5BD)] // TS4 XML statistic
    [InlineData(0x6017E896)] // TS4 XML buff
    public void CanCreateResource_WithSupportedResourceTypes_ShouldReturnTrue(uint resourceType)
    {
        // Act
        var canCreate = _factory.CanCreateResource(resourceType);

        // Assert
        canCreate.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanCreateResource returns false for unsupported resource types.
    /// </summary>
    /// <param name="resourceType">The resource type to test.</param>
    [Theory]
    [InlineData(0x2E75C769)] // Texture resource (not text)
    [InlineData(0x00000000)] // Invalid type
    [InlineData(0x12345678)] // Random unsupported type
    [InlineData(0x220557DA)] // String table (handled by different factory)
    public void CanCreateResource_WithUnsupportedResourceTypes_ShouldReturnFalse(uint resourceType)
    {
        // Act
        var canCreate = _factory.CanCreateResource(resourceType);

        // Assert
        canCreate.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CreateResource returns a TextResource when given a supported resource type.
    /// </summary>
    [Fact]
    public void CreateResource_WithSupportedResourceType_ShouldReturnTextResource()
    {
        // Arrange
        using var stream = new MemoryStream();
        const uint resourceType = 0x03B33DDF;

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<TextResource>();

        // Verify the resource implements ITextResource
        var textResource = resource.Should().BeAssignableTo<ITextResource>().Subject;
        textResource.Content.Should().BeEmpty(); // New resource should have empty content
    }

    /// <summary>
    /// Tests that CreateResource throws ArgumentNullException when given a null stream.
    /// </summary>
    [Fact]
    public void CreateResource_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _factory.CreateResource(null!, 0x03B33DDF);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");
    }

    /// <summary>
    /// Tests that CreateResource throws ArgumentException when given an unsupported resource type.
    /// </summary>
    [Fact]
    public void CreateResource_WithUnsupportedResourceType_ShouldThrowArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();
        const uint unsupportedType = 0x2E75C769; // Texture type

        // Act & Assert
        var act = () => _factory.CreateResource(stream, unsupportedType);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*0x2E75C769*");
    }

    /// <summary>
    /// Tests that CreateResource with a stream containing data returns a TextResource with the correct content.
    /// </summary>
    [Fact]
    public void CreateResourceWithStream_WithSupportedResourceType_ShouldReturnTextResourceWithContent()
    {
        // Arrange
        const uint resourceType = 0x03B33DDF;
        var content = "<?xml version=\"1.0\"?><tuning>Test content</tuning>";
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(contentBytes);

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<TextResource>();

        // Verify content was loaded
        var textResource = resource.Should().BeAssignableTo<ITextResource>().Subject;
        textResource.Content.Should().Be(content);
        textResource.IsXml.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CreateEmptyResource returns an empty TextResource when given a supported resource type.
    /// </summary>
    [Fact]
    public void CreateEmptyResource_WithSupportedResourceType_ShouldReturnEmptyTextResource()
    {
        // Arrange
        const uint resourceType = 0x03B33DDF;

        // Act
        var resource = _factory.CreateEmptyResource(resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<TextResource>();

        // Verify the resource implements ITextResource and is empty
        var textResource = resource.Should().BeAssignableTo<ITextResource>().Subject;
        textResource.Content.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CreateEmptyResource throws ArgumentException when given an unsupported resource type.
    /// </summary>
    [Fact]
    public void CreateEmptyResource_WithUnsupportedResourceType_ShouldThrowArgumentException()
    {
        // Arrange
        const uint unsupportedType = 0x2E75C769; // Texture type

        // Act & Assert
        var act = () => _factory.CreateEmptyResource(unsupportedType);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*0x2E75C769*");
    }

    /// <summary>
    /// Tests that the supported resource types are properly loaded from the embedded resource file.
    /// </summary>
    [Fact]
    public void LoadSupportedResourceTypes_ShouldLoadFromEmbeddedResource()
    {
        // Act
        var supportedTypes = _factory.ResourceTypes; // Use ResourceTypes for uint values

        // Assert
        // Verify we loaded a reasonable number of types (should be 80+ based on the resource file)
        supportedTypes.Should().HaveCountGreaterThan(10);

        // Verify some specific types that should definitely be included
        var expectedTypes = new uint[]
        {
            0x03B33DDF, // TS4 XML tuning
            0x0069453E, // TS4 XML objective
            0x339BC5BD, // TS4 XML statistic
            0x6017E896, // TS4 XML buff
            0x75CF3B61, // TS4 XML trait
            0xA96BFC5E, // TS4 XML skill
        };

        foreach (var expectedType in expectedTypes)
        {
            supportedTypes.Should().Contain(expectedType,
                $"because 0x{expectedType:X8} should be a supported text resource type");
        }
    }

    /// <summary>
    /// Tests that the SupportedResourceTypes collection is read-only and cannot be modified.
    /// </summary>
    [Fact]
    public void SupportedResourceTypes_ShouldBeReadOnly()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().BeAssignableTo<IReadOnlySet<string>>();

        // Verify we can't modify the collection by trying to cast to mutable type
        supportedTypes.Should().NotBeAssignableTo<HashSet<string>>();
    }

    /// <summary>
    /// Tests that the factory is thread-safe and can handle concurrent resource creation.
    /// </summary>
    [Fact]
    public async Task Factory_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<ITextResource>>();

        // Act - Create resources from multiple threads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _factory.CreateResourceAsync(1, null)));
        }

        var resources = await Task.WhenAll(tasks);

        // Assert
        resources.Should().HaveCount(10);
        resources.Should().OnlyContain(r => r is TextResource);
    }
}
