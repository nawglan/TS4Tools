using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Text.Tests;

/// <summary>
/// Tests for TextResource class focusing on behavior validation without duplicating implementation logic.
/// </summary>
public class TextResourceTests
{
    private readonly ILogger<TextResource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResourceTests"/> class.
    /// </summary>
    public TextResourceTests()
    {
        _logger = NullLogger<TextResource>.Instance;
    }

    /// <summary>
    /// Tests that the TextResource constructor creates a valid instance when provided with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var resourceKey = CreateTestResourceKey();

        // Act
        var resource = new TextResource(resourceKey, _logger);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(resourceKey);
        resource.Content.Should().BeEmpty();
        resource.Encoding.Should().Be(Encoding.UTF8);
    }

    /// <summary>
    /// Tests that the TextResource constructor throws ArgumentNullException when provided with a null resource key.
    /// </summary>
    [Fact]
    public void Constructor_WithNullResourceKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TextResource(null!, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("resourceKey");
    }

    /// <summary>
    /// Tests that the TextResource constructor throws ArgumentNullException when provided with a null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceKey = CreateTestResourceKey();

        // Act & Assert
        var act = () => new TextResource(resourceKey, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that setting the Content property updates the content and raises PropertyChanged event.
    /// </summary>
    [Fact]
    public void Content_WhenSet_ShouldUpdateAndNotifyPropertyChanged()
    {
        // Arrange
        var resource = CreateTextResource();
        var newContent = "Updated content";
        var propertyChangedRaised = false;

        resource.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(resource.Content))
                propertyChangedRaised = true;
        };

        // Act
        resource.Content = newContent;

        // Assert
        resource.Content.Should().Be(newContent);
        propertyChangedRaised.Should().BeTrue();
    }

    /// <summary>
    /// Tests that setting the Content property to the same value does not raise PropertyChanged event.
    /// </summary>
    [Fact]
    public void Content_WhenSetToSameValue_ShouldNotNotifyPropertyChanged()
    {
        // Arrange
        var resource = CreateTextResource();
        var content = "Same content";
        resource.Content = content;

        var propertyChangedRaised = false;
        resource.PropertyChanged += (_, _) => propertyChangedRaised = true;

        // Act
        resource.Content = content;

        // Assert
        propertyChangedRaised.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the Content property correctly stores various values including null and empty strings.
    /// </summary>
    /// <param name="input">The input value to set.</param>
    /// <param name="expected">The expected stored value.</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "  ")]
    [InlineData("test content", "test content")]
    public void Content_WithVariousValues_ShouldStoreCorrectly(string? input, string expected)
    {
        // Arrange
        var resource = CreateTextResource();

        // Act
        resource.Content = input!;

        // Assert
        resource.Content.Should().Be(expected);
    }

    /// <summary>
    /// Tests that the IsXml property correctly detects XML content in various formats.
    /// </summary>
    /// <param name="content">The content to test.</param>
    /// <param name="expectedIsXml">Whether the content should be detected as XML.</param>
    [Theory]
    [InlineData("<?xml version=\"1.0\"?><root></root>", true)]
    [InlineData("  <?xml version=\"1.0\"?><root></root>", true)]
    [InlineData("<root><element>value</element></root>", true)]
    [InlineData("  <root></root>  ", true)]
    [InlineData("not xml content", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsXml_WithVariousContent_ShouldDetectCorrectly(string content, bool expectedIsXml)
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = content;

        // Act & Assert
        resource.IsXml.Should().Be(expectedIsXml);
    }

    /// <summary>
    /// Tests that the IsJson property correctly detects JSON content in various formats.
    /// </summary>
    /// <param name="content">The content to test.</param>
    /// <param name="expectedIsJson">Whether the content should be detected as JSON.</param>
    [Theory]
    [InlineData("{\"key\": \"value\"}", true)]
    [InlineData("  {\"key\": \"value\"}  ", true)]
    [InlineData("[{\"item\": 1}, {\"item\": 2}]", true)]
    [InlineData("  []  ", true)]
    [InlineData("not json content", false)]
    [InlineData("{invalid json", false)]
    [InlineData("", false)]
    public void IsJson_WithVariousContent_ShouldDetectCorrectly(string content, bool expectedIsJson)
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = content;

        // Act & Assert
        resource.IsJson.Should().Be(expectedIsJson);
    }

    /// <summary>
    /// Tests that the LineEndings property correctly detects line ending styles in various content formats.
    /// </summary>
    /// <param name="content">The content to test.</param>
    /// <param name="expected">The expected line ending style.</param>
    [Theory]
    [InlineData("line1\r\nline2\r\nline3", LineEndingStyle.CrLf)]
    [InlineData("line1\nline2\nline3", LineEndingStyle.Lf)]
    [InlineData("line1\rline2\rline3", LineEndingStyle.Cr)]
    [InlineData("line1\r\nline2\nline3", LineEndingStyle.Mixed)]
    [InlineData("single line", LineEndingStyle.Lf)] // Default when no line endings
    [InlineData("", LineEndingStyle.Lf)] // Default for empty content
    public void LineEndings_WithVariousContent_ShouldDetectCorrectly(string content, LineEndingStyle expected)
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = content;

        // Act & Assert
        resource.LineEndings.Should().Be(expected);
    }

    /// <summary>
    /// Tests that AsXmlDocument returns a valid XmlDocument when the content is valid XML.
    /// </summary>
    [Fact]
    public void AsXmlDocument_WithValidXml_ShouldReturnDocument()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "<?xml version=\"1.0\"?><root><element>value</element></root>";

        // Act
        var xmlDoc = resource.AsXmlDocument();

        // Assert
        xmlDoc.Should().NotBeNull();
        xmlDoc!.DocumentElement!.Name.Should().Be("root");
        xmlDoc.DocumentElement["element"]!.InnerText.Should().Be("value");
    }

    /// <summary>
    /// Tests that AsXmlDocument throws XmlException when the content is invalid XML.
    /// </summary>
    [Fact]
    public void AsXmlDocument_WithInvalidXml_ShouldThrowXmlException()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "<invalid><xml>";

        // Act & Assert
        var act = () => resource.AsXmlDocument();
        act.Should().Throw<System.Xml.XmlException>();
    }

    /// <summary>
    /// Tests that AsXmlDocument returns null when the content is not XML.
    /// </summary>
    [Fact]
    public void AsXmlDocument_WithNonXmlContent_ShouldReturnNull()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "This is not XML content";

        // Act
        var result = resource.AsXmlDocument();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that AsJsonElement returns a valid JsonElement when the content is valid JSON.
    /// </summary>
    [Fact]
    public void AsJsonElement_WithValidJson_ShouldReturnElement()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "{\"key\": \"value\", \"number\": 42}";

        // Act
        var jsonElement = resource.AsJsonElement();

        // Assert
        jsonElement.Should().NotBeNull();
        jsonElement!.Value.GetProperty("key").GetString().Should().Be("value");
        jsonElement.Value.GetProperty("number").GetInt32().Should().Be(42);
    }

    /// <summary>
    /// Tests that AsJsonElement throws JsonException when the content is invalid JSON.
    /// </summary>
    [Fact]
    public void AsJsonElement_WithInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "{invalid json}";

        // Act & Assert
        var act = () => resource.AsJsonElement();
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    /// <summary>
    /// Tests that AsJsonElement returns null when the content is not JSON.
    /// </summary>
    [Fact]
    public void AsJsonElement_WithNonJsonContent_ShouldReturnNull()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "This is not JSON content";

        // Act
        var result = resource.AsJsonElement();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that NormalizeLineEndings correctly converts mixed line endings to the specified target style.
    /// </summary>
    /// <param name="input">The input content with mixed line endings.</param>
    /// <param name="targetStyle">The target line ending style.</param>
    /// <param name="expected">The expected normalized content.</param>
    [Theory]
    [InlineData("line1\r\nline2\nline3\r", LineEndingStyle.CrLf, "line1\r\nline2\r\nline3\r\n")]
    [InlineData("line1\r\nline2\nline3\r", LineEndingStyle.Lf, "line1\nline2\nline3\n")]
    [InlineData("line1\r\nline2\nline3\r", LineEndingStyle.Cr, "line1\rline2\rline3\r")]
    public void NormalizeLineEndings_WithMixedEndings_ShouldNormalizeCorrectly(
        string input, LineEndingStyle targetStyle, string expected)
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = input;

        // Act
        var result = resource.NormalizeLineEndings(targetStyle);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that ToBytes correctly converts the content to bytes using different encodings.
    /// </summary>
    /// <param name="content">The content to convert.</param>
    /// <param name="encodingName">The name of the encoding to use.</param>
    [Theory]
    [InlineData("Hello World!", "UTF-8")]
    [InlineData("Hello World!", "UTF-16")]
    [InlineData("Héllo Wörld! 你好", "UTF-8")]
    public void ToBytes_WithDifferentEncodings_ShouldConvertCorrectly(string content, string encodingName)
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = content;
        var targetEncoding = Encoding.GetEncoding(encodingName);

        // Act
        var bytes = resource.ToBytes(targetEncoding);

        // Assert
        var decodedContent = targetEncoding.GetString(bytes);
        decodedContent.Should().Be(content);
    }

    /// <summary>
    /// Tests that ToBytes throws ArgumentNullException when provided with a null encoding.
    /// </summary>
    [Fact]
    public void ToBytes_WithNullEncoding_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = CreateTextResource();

        // Act & Assert
        var act = () => resource.ToBytes(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetEncoding");
    }

    /// <summary>
    /// Tests that ToBinaryAsync returns the content as UTF-8 encoded bytes.
    /// </summary>
    [Fact]
    public async Task ToBinaryAsync_ShouldReturnUtf8Bytes()
    {
        // Arrange
        var resource = CreateTextResource();
        var content = "Test content for serialization";
        resource.Content = content;

        // Act
        var bytes = await resource.ToBinaryAsync();

        // Assert
        var decodedContent = Encoding.UTF8.GetString(bytes);
        decodedContent.Should().Be(content);
    }

    /// <summary>
    /// Tests that the constructor with stream correctly loads content from the provided stream.
    /// </summary>
    [Fact]
    public void ConstructorWithStream_ShouldLoadContentCorrectly()
    {
        // Arrange
        var content = "Test content from stream";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var resourceKey = CreateTestResourceKey();

        // Act
        var resource = new TextResource(resourceKey, stream, _logger);

        // Assert
        resource.Content.Should().Be(content);
        resource.Encoding.Should().Be(Encoding.UTF8);
    }

    /// <summary>
    /// Tests that the constructor with stream throws ArgumentNullException when provided with null data.
    /// </summary>
    [Fact]
    public void ConstructorWithStream_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceKey = CreateTestResourceKey();

        // Act & Assert
        var act = () => new TextResource(resourceKey, null!, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("data");
    }

    /// <summary>
    /// Tests that content changes properly invalidate cached properties like IsXml and LineEndings.
    /// </summary>
    [Fact]
    public void ContentChanges_ShouldInvalidateCache()
    {
        // Arrange
        var resource = CreateTextResource();
        resource.Content = "<?xml version=\"1.0\"?><root></root>";

        // Access properties to cache them
        var originalIsXml = resource.IsXml;
        var originalLineEndings = resource.LineEndings;

        // Act
        resource.Content = "This is not XML";

        // Assert
        originalIsXml.Should().BeTrue();
        resource.IsXml.Should().BeFalse(); // Cache should be invalidated
        resource.LineEndings.Should().Be(LineEndingStyle.Lf); // Cache should be invalidated
    }

    private TextResource CreateTextResource()
    {
        var resourceKey = CreateTestResourceKey();
        return new TextResource(resourceKey, _logger);
    }

    private static ResourceKey CreateTestResourceKey()
    {
        return new ResourceKey(0x03B33DDF, 0x00000000, 0x123456789ABCDEF0UL);
    }
}
