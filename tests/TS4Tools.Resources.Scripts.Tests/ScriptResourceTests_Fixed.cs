using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Fixed unit tests for ScriptResource functionality.
/// </summary>
public sealed class ScriptResourceTests_Fixed
{
    private readonly ILogger<ScriptResource> _logger;

    public ScriptResourceTests_Fixed()
    {
        _logger = Substitute.For<ILogger<ScriptResource>>();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldCreateResource()
    {
        // Act
        using var resource = new ScriptResource(_logger);

        // Assert
        resource.Should().NotBeNull();
        resource.Version.Should().Be(1);
        // Default constructor creates ResourceKey with (0, 0x073FAA07, 0)
        resource.ResourceKey.Should().Be(new ResourceKey(0, 0x073FAA07, 0));
    }

    [Fact]
    public void Constructor_WithData_ShouldParseResource()
    {
        // Arrange
        var resourceKey = new ResourceKey(1, 0x073FAA07, 1);
        var testData = CreateTestScriptData();

        // Act
        using var resource = new ScriptResource(resourceKey, testData, _logger);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(resourceKey);
        resource.Version.Should().Be(1);
    }

    [Fact]
    public void Version_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        resource.Version = 2;

        // Assert
        resource.Version.Should().Be(2);
    }

    [Fact]
    public void GameVersion_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        resource.GameVersion = "1.0.0";

        // Assert
        resource.GameVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void Unknown2_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        resource.Unknown2 = 0x12345678;

        // Assert
        resource.Unknown2.Should().Be(0x12345678);
    }

    [Fact]
    public void SetAssemblyData_WithValidData_ShouldSetData()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        resource.SetAssemblyData(testData);

        // Assert
        resource.AssemblyData.ToArray().Should().BeEquivalentTo(testData);
    }

    [Fact]
    public async Task GetAssemblyInfoAsync_WithNoData_ShouldReturnEmptyInfo()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var info = await resource.GetAssemblyInfoAsync();

        // Assert
        info.Should().NotBeNull();
        info.FullName.Should().Be("No assembly data");
    }

    [Fact]
    public void ContentFields_ShouldContainExpectedFields()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var fields = resource.ContentFields;

        // Assert
        fields.Should().Contain("Version");
        fields.Should().Contain("GameVersion");
        fields.Should().Contain("Unknown2");
        fields.Should().Contain("AssemblyData");
    }

    [Fact]
    public void IndexerByName_WithValidField_ShouldReturnValue()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var versionValue = resource["Version"];

        // Assert
        versionValue.Type.Should().Be(typeof(byte));
        versionValue.Value.Should().Be((byte)1);
    }

    [Fact]
    public void IndexerByIndex_WithValidIndex_ShouldReturnValue()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var firstFieldValue = resource[0];

        // Assert
        firstFieldValue.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldDisposeGracefully()
    {
        // Arrange
        var resource = new ScriptResource(_logger);

        // Act & Assert
        resource.Invoking(r => r.Dispose()).Should().NotThrow();
    }

    private static byte[] CreateTestScriptData()
    {
        // Create a minimal test script resource data
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write version
        writer.Write((byte)1);
        
        // Write unknown2
        writer.Write(0x2BC4F79FU);
        
        // Write MD5 sum (64 bytes)
        writer.Write(new byte[64]);
        
        // Write entry count
        writer.Write((ushort)0);

        return stream.ToArray();
    }
}
