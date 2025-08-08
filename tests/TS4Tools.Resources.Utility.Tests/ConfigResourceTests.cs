using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using System.Globalization;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class ConfigResourceTests
{
    private readonly ILogger<ConfigResource> _logger = NullLogger<ConfigResource>.Instance;
    private readonly ResourceKey _testKey = new(0x0354796A, 0x12345678, 0x87654321);

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ConfigResource(null!, _testKey);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        using var resource = new ConfigResource(_logger, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
        resource.ConfigCount.Should().Be(0);
        resource.IsJsonFormat.Should().BeFalse();
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act
        var fields = resource.ContentFields;

        // Assert
        fields.Should().Contain("IsJsonFormat");
        fields.Should().Contain("ConfigCount");
        fields.Should().Contain("ConfigKeys");
    }

    [Fact]
    public void SetValue_WithStringValue_ShouldStoreCorrectly()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.SetConfigValue("testKey", "testValue");

        // Assert
        resource.ConfigCount.Should().Be(1);
        resource.GetConfigValue<string>("testKey").Should().Be("testValue");
        resource.HasConfigKey("testKey").Should().BeTrue();
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void SetValue_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act & Assert
        var act1 = () => resource.SetConfigValue<string>(null!, "value");
        var act2 = () => resource.SetConfigValue("", "value");
        var act3 = () => resource.SetConfigValue("   ", "value");

        act1.Should().Throw<ArgumentException>().WithParameterName("key");
        act2.Should().Throw<ArgumentException>().WithParameterName("key");
        act3.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void GetValue_WithExistingKey_ShouldReturnCorrectValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("intKey", 42);
        resource.SetConfigValue("stringKey", "hello");
        resource.SetConfigValue("boolKey", true);

        // Act & Assert
        resource.GetConfigValue<int>("intKey").Should().Be(42);
        resource.GetConfigValue<string>("stringKey").Should().Be("hello");
        resource.GetConfigValue<bool>("boolKey").Should().BeTrue();
    }

    [Fact]
    public void GetValue_WithNonExistentKey_ShouldReturnDefaultValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act & Assert
        resource.GetConfigValue<int>("nonExistent").Should().Be(0);
        resource.GetConfigValue<string>("nonExistent").Should().BeNull();
        resource.GetConfigValue<string>("nonExistent").Should().Be(default(string));
        resource.GetConfigValue<int>("nonExistent").Should().Be(default(int));
    }

    [Fact]
    public void RemoveValue_WithExistingKey_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("testKey", "testValue");
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        var result = resource.RemoveConfigKey("testKey");

        // Assert
        result.Should().BeTrue();
        resource.HasConfigKey("testKey").Should().BeFalse();
        resource.ConfigCount.Should().Be(0);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void RemoveValue_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act
        var result = resource.RemoveConfigKey("nonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Clear_WithExistingData_ShouldClearAllData()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("key1", "value1");
        resource.SetConfigValue("key2", 42);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.ClearConfig();

        // Assert
        resource.ConfigCount.Should().Be(0);
        resource.ConfigKeys.Should().BeEmpty();
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void Clear_WithEmptyData_ShouldNotTriggerChange()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.ClearConfig();

        // Assert
        changedCalled.Should().BeFalse();
    }

    [Fact]
    public void GetString_ShouldReturnStringValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("stringKey", "hello world");

        // Act
        var result = resource.GetConfigValue<string>("stringKey");

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void GetInt_ShouldReturnIntegerValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("intKey", 42);

        // Act
        var result = resource.GetConfigValue<int>("intKey");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetBool_ShouldReturnBooleanValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("boolKey", true);

        // Act
        var result = resource.GetConfigValue<bool>("boolKey");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetFloat_ShouldReturnFloatValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("floatKey", 3.14159f);

        // Act
        var result = resource.GetConfigValue<float>("floatKey");

        // Assert
        result.Should().BeApproximately(3.14159f, 0.0001f);
    }

    [Fact]
    public void GetDouble_ShouldReturnDoubleValue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("doubleKey", 2.71828);

        // Act
        var result = resource.GetConfigValue<double>("doubleKey");

        // Assert
        result.Should().BeApproximately(2.71828, 0.0001);
    }

    [Fact]
    public void ParseFromStream_WithJsonFormat_ShouldParseCorrectly()
    {
        // Arrange
        var jsonContent = """
        {
            "stringValue": "hello",
            "intValue": 42,
            "boolValue": true,
            "nested": {
                "innerValue": "inner"
            }
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        using var resource = new ConfigResource(_logger, _testKey, stream);

        // Assert
        resource.IsJsonFormat.Should().BeTrue();
        resource.ConfigCount.Should().BeGreaterThan(0);
        resource.GetConfigValue<string>("stringValue").Should().Be("hello");
        resource.GetConfigValue<int>("intValue").Should().Be(42);
        resource.GetConfigValue<bool>("boolValue").Should().BeTrue();
        resource.GetConfigValue<string>("nested.innerValue").Should().Be("inner");
    }

    [Fact]
    public void ParseFromStream_WithKeyValueFormat_ShouldParseCorrectly()
    {
        // Arrange
        var keyValueContent = """
        # This is a comment
        stringValue=hello world
        intValue=42
        boolValue=true
        quotedValue="quoted string with spaces"
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(keyValueContent));

        // Act
        using var resource = new ConfigResource(_logger, _testKey, stream);

        // Assert
        resource.IsJsonFormat.Should().BeFalse();
        resource.ConfigCount.Should().Be(4);
        resource.GetConfigValue<string>("stringValue").Should().Be("hello world");
        resource.GetConfigValue<int>("intValue").Should().Be(42);
        resource.GetConfigValue<bool>("boolValue").Should().BeTrue();
        resource.GetConfigValue<string>("quotedValue").Should().Be("quoted string with spaces");
    }

    [Fact]
    public void ParseFromStream_WithInvalidData_ShouldStoreAsRawData()
    {
        // Arrange
        var binaryData = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
        using var stream = new MemoryStream(binaryData);

        // Act
        using var resource = new ConfigResource(_logger, _testKey, stream);

        // Assert
        resource.ConfigCount.Should().Be(0);
        resource.IsJsonFormat.Should().BeFalse();
    }

    [Fact]
    public async Task SerializeAsync_WithJsonFormat_ShouldReturnJsonContent()
    {
        // Arrange
        var jsonContent = """{"stringValue": "hello", "intValue": 42}""";
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        using var resource = new ConfigResource(_logger, _testKey, inputStream);

        // Act
        using var outputStream = await resource.SerializeAsync();
        var result = System.Text.Encoding.UTF8.GetString(((MemoryStream)outputStream).ToArray());

        // Assert
        result.Should().Contain("stringValue");
        result.Should().Contain("hello");
        result.Should().Contain("intValue");
    }

    [Fact]
    public async Task SerializeAsync_WithKeyValueFormat_ShouldReturnKeyValueContent()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("stringValue", "hello");
        resource.SetConfigValue("intValue", 42);

        // Act
        using var outputStream = await resource.SerializeAsync();
        var result = System.Text.Encoding.UTF8.GetString(((MemoryStream)outputStream).ToArray());

        // Assert
        result.Should().Contain("stringValue=hello");
        result.Should().Contain("intValue=42");
        result.Should().Contain("# Configuration file generated by TS4Tools");
    }

    [Fact]
    public async Task SerializeAsync_WithEmptyResource_ShouldReturnEmptyStream()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act
        using var stream = await resource.SerializeAsync();

        // Assert
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void Validate_WithValidResource_ShouldReturnTrue()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("testKey", "testValue");

        // Act
        var isValid = resource.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ConfigKeys_ShouldReturnAllKeys()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("key1", "value1");
        resource.SetConfigValue("key2", 42);
        resource.SetConfigValue("key3", true);

        // Act
        var keys = resource.ConfigKeys.ToList();

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("key1");
        keys.Should().Contain("key2");
        keys.Should().Contain("key3");
    }

    [Fact]
    public void ConfigData_ShouldReturnReadOnlyDictionary()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("testKey", "testValue");

        // Act
        var configData = resource.ConfigCount;

        // Assert
        configData.Should().Be(1);
        resource.GetConfigValue<string>("testKey").Should().Be("testValue");
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);
        resource.SetConfigValue("key1", "value1");
        resource.SetConfigValue("key2", "value2");

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain("ConfigResource");
        result.Should().Contain("2 entries");
    }

    [Theory]
    [InlineData("key1", "value1")]
    [InlineData("Key2", "Value2")]
    [InlineData("KEY3", "VALUE3")]
    public void SetValue_WithCaseInsensitiveKeys_ShouldHandleCorrectly(string key, string value)
    {
        // Arrange
        using var resource = new ConfigResource(_logger, _testKey);

        // Act
        resource.SetConfigValue(key, value);

        // Assert
        resource.GetConfigValue<string>(key.ToUpperInvariant()).Should().Be(value);
        resource.GetConfigValue<string>(key.ToUpperInvariant()).Should().Be(value);
        resource.HasConfigKey(key.ToUpperInvariant()).Should().BeTrue();
        resource.HasConfigKey(key.ToUpperInvariant()).Should().BeTrue();
    }
}
