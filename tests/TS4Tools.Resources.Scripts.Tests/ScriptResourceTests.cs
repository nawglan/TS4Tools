using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Unit tests for ScriptResource functionality.
/// </summary>
public sealed class ScriptResourceTests
{
    private readonly ILogger<ScriptResource> _logger;

    public ScriptResourceTests()
    {
        _logger = Substitute.For<ILogger<ScriptResource>>();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldInitializeWithDefaults()
    {
        // Act
        using var resource = new ScriptResource(_logger);

        // Assert
        resource.Version.Should().Be(1);
        resource.GameVersion.Should().BeEmpty();
        resource.Unknown2.Should().Be(0x2BC4F79F);
        resource.MD5Sum.Length.Should().Be(64);
        resource.AssemblyData.Length.Should().Be(0);
        resource.ApiVersion.Should().Be(1);
        resource.ContentFields.Should().NotBeEmpty();
    }

    [Fact]
    public void ResourceKey_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var key = new ResourceKey(0x12345678, 0x073FAA07, 0x9ABCDEF0);

        // Act
        resource.ResourceKey = key;

        // Assert
        resource.ResourceKey.Should().Be(key);
    }

    [Fact]
    public void Version_SetValue_ShouldUpdateProperty()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        resource.Version = 2;

        // Assert
        resource.Version.Should().Be(2);
    }

    [Fact]
    public void GameVersion_SetNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act & Assert
        resource.Invoking(r => r.GameVersion = null!)
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GameVersion_SetValidValue_ShouldUpdateProperty()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var gameVersion = "1.108.329.1020";

        // Act
        resource.GameVersion = gameVersion;

        // Assert
        resource.GameVersion.Should().Be(gameVersion);
    }

    [Fact]
    public void Unknown2_SetValue_ShouldUpdateProperty()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        const uint newValue = 0x12345678;

        // Act
        resource.Unknown2 = newValue;

        // Assert
        resource.Unknown2.Should().Be(newValue);
    }

    [Fact]
    public void MD5Sum_SetInvalidLength_ShouldThrowArgumentException()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var invalidMd5 = new byte[32]; // Wrong length

        // Act & Assert
        resource.Invoking(r => r.MD5Sum = invalidMd5)
            .Should().Throw<ArgumentException>()
            .WithMessage("MD5Sum must be exactly 64 bytes*");
    }

    [Fact]
    public void MD5Sum_SetValidValue_ShouldUpdateProperty()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var validMd5 = new byte[64];
        Array.Fill<byte>(validMd5, 0x42);

        // Act
        resource.MD5Sum = validMd5;

        // Assert
        resource.MD5Sum.ToArray().Should().BeEquivalentTo(validMd5);
    }

    [Fact]
    public void SetAssemblyData_WithValidData_ShouldUpdateAssemblyData()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var assemblyData = new byte[] { 0x4D, 0x5A }; // PE header start

        // Act
        resource.SetAssemblyData(assemblyData);

        // Assert
        resource.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public void SetAssemblyData_WithEmptyData_ShouldClearAssemblyData()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        resource.SetAssemblyData(new byte[] { 1, 2, 3 }); // Set some data first

        // Act
        resource.SetAssemblyData(ReadOnlySpan<byte>.Empty);

        // Assert
        resource.AssemblyData.Length.Should().Be(0);
    }

    [Fact]
    public async Task GetAssemblyInfoAsync_WithNoData_ShouldReturnEmptyInfo()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var info = await resource.GetAssemblyInfoAsync();

        // Assert
        info.FullName.Should().Be("No assembly data");
        info.Location.Should().BeEmpty();
        info.ExportedTypes.Should().BeEmpty();
        info.ReferencedAssemblies.Should().BeEmpty();
        info.Properties.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssemblyInfoAsync_WithInvalidAssembly_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        var invalidData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // Invalid assembly
        resource.SetAssemblyData(invalidData);

        // Act & Assert
        await resource.Invoking(r => r.GetAssemblyInfoAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Assembly data is not a valid .NET assembly");
    }

    [Fact]
    public void GetRawData_WithEmptyResource_ShouldReturnValidData()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);

        // Act
        var rawData = resource.GetRawData();

        // Assert
        rawData.Length.Should().BeGreaterThan(0);
        rawData[0].Should().Be(1); // Version should be first byte
    }

    [Fact]
    public void GetRawData_WithGameVersion_ShouldIncludeGameVersionData()
    {
        // Arrange
        using var resource = new ScriptResource(_logger);
        resource.Version = 2;
        resource.GameVersion = "Test";

        // Act
        var rawData = resource.GetRawData();

        // Assert
        rawData.Length.Should().BeGreaterThan(10); // Should have more data with game version
        rawData[0].Should().Be(2); // Version should be 2
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
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
    public void Dispose_AfterDisposal_ShouldClearSensitiveData()
    {
        // Arrange
        var resource = new ScriptResource(_logger);
        resource.SetAssemblyData(new byte[] { 1, 2, 3, 4 });
        resource.MD5Sum = new byte[64];

        // Act
        resource.Dispose();

        // Assert - Should clear data content but keep array structure
        // AssemblyData should be all zeros but keep original length
        resource.AssemblyData.ToArray().Should().OnlyContain(b => b == 0);
        resource.MD5Sum.ToArray().Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public async Task GetAssemblyInfoAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var resource = new ScriptResource(_logger);
        resource.Dispose();

        // Act & Assert
        await resource.Invoking(r => r.GetAssemblyInfoAsync())
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_WithDataParsing_ShouldHandleVersions(byte version)
    {
        // Arrange
        var testData = CreateTestScriptData(version);
        var resourceKey = new ResourceKey(1, 0x073FAA07, 1);

        // Act
        using var resource = new ScriptResource(resourceKey, testData, _logger);

        // Assert
        resource.Version.Should().Be(version);
        resource.ResourceKey.Should().Be(resourceKey);
    }

    [Fact]
    public void Constructor_WithInvalidData_ShouldThrowEndOfStreamException()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF }; // Too short
        var resourceKey = new ResourceKey(1, 0x073FAA07, 1);

        // Act & Assert - Constructor should throw when data is invalid
        Action action = () => { var _ = new ScriptResource(resourceKey, invalidData, _logger); };
        action.Should().Throw<EndOfStreamException>()
            .WithMessage("Unable to read beyond the end of the stream.");
    }

    /// <summary>
    /// Creates test script resource data with the specified version.
    /// </summary>
    private static byte[] CreateTestScriptData(byte version)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write version
        writer.Write(version);

        // Write game version if version > 1
        if (version > 1)
        {
            var gameVersionBytes = System.Text.Encoding.Unicode.GetBytes("TestVersion");
            writer.Write(gameVersionBytes.Length / 2);
            writer.Write(gameVersionBytes);
        }

        // Write unknown2
        writer.Write(0x2BC4F79Fu);

        // Write MD5 sum (64 bytes)
        writer.Write(new byte[64]);

        // Write entry count (0 entries)
        writer.Write((ushort)0);

        return stream.ToArray();
    }
}
