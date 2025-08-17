using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Helpers;
using Xunit;

namespace TS4Tools.Core.Helpers.Tests;

public class HelperToolServiceTests
{
    private readonly HelperToolService _service;

    public HelperToolServiceTests()
    {
        _service = new HelperToolService(NullLogger<HelperToolService>.Instance);
    }

    [Fact]
    public void GetAvailableHelperTools_WhenNoHelpersLoaded_ShouldReturnEmptyList()
    {
        // Act
        var result = _service.GetAvailableHelperTools();

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void IsHelperToolAvailable_WithNonExistentHelper_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsHelperToolAvailable("NonExistentHelper");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsHelperToolAvailable_WithInvalidHelperName_ShouldThrowArgumentException(string helperName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.IsHelperToolAvailable(helperName));
    }

    [Fact]
    public void GetHelpersForResourceType_WithUnknownResourceType_ShouldReturnEmptyList()
    {
        // Act
        var result = _service.GetHelpersForResourceType(0x12345678);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentHelper_ShouldReturnFailureResult()
    {
        // Act
        var result = await _service.ExecuteAsync("NonExistentHelper", Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().Be(-1);
        result.StandardError.Should().Contain("not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithInvalidHelperName_ShouldThrowArgumentException(string helperName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExecuteAsync(helperName, Array.Empty<string>()));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ExecuteAsync("test", null!));
    }

    [Fact]
    public async Task ReloadHelpersAsync_ShouldCompleteSuccessfully()
    {
        // Act & Assert
        var act = async () => await _service.ReloadHelpersAsync();
        await act.Should().NotThrowAsync();
    }
}

public class HelperToolInfoTests
{
    [Fact]
    public void ToString_ShouldReturnExpectedFormat()
    {
        // Arrange
        var helper = new HelperToolInfo
        {
            Id = "TestHelper",
            Label = "Test Helper Tool",
            Command = "test.exe"
        };

        // Act
        var result = helper.ToString();

        // Assert
        result.Should().Be("Test Helper Tool (TestHelper)");
    }

    [Fact]
    public void Properties_WhenNotSet_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var helper = new HelperToolInfo
        {
            Id = "Test",
            Label = "Test",
            Command = "test.exe"
        };

        // Assert
        helper.Properties.Should().NotBeNull().And.BeEmpty();
    }
}

public class HelperToolResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var executionTime = TimeSpan.FromMilliseconds(100);
        var resultData = new byte[] { 1, 2, 3 };

        // Act
        var result = HelperToolResult.Success(0, "output", "error", executionTime, resultData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Be("output");
        result.StandardError.Should().Be("error");
        result.ExecutionTime.Should().Be(executionTime);
        result.ResultData.Should().BeEquivalentTo(resultData);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var executionTime = TimeSpan.FromMilliseconds(100);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = HelperToolResult.Failure(1, "output", "error", executionTime, exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().Be("output");
        result.StandardError.Should().Be("error");
        result.ExecutionTime.Should().Be(executionTime);
        result.Exception.Should().Be(exception);
        result.ResultData.Should().BeNull();
    }

    [Fact]
    public void FromException_ShouldCreateFailureResultFromException()
    {
        // Arrange
        var executionTime = TimeSpan.FromMilliseconds(100);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = HelperToolResult.FromException(exception, executionTime);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().Be(-1);
        result.StandardOutput.Should().Be(string.Empty);
        result.StandardError.Should().Be("Test exception");
        result.ExecutionTime.Should().Be(executionTime);
        result.Exception.Should().Be(exception);
        result.ResultData.Should().BeNull();
    }

    [Fact]
    public void HelperToolResult_Success_ShouldCreateSuccessResult()
    {
        // Arrange
        const int exitCode = 0;
        const string standardOutput = "Success output";
        const string standardError = "";
        var executionTime = TimeSpan.FromMilliseconds(100);

        // Act
        var result = HelperToolResult.Success(exitCode, standardOutput, standardError, executionTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(exitCode);
        result.StandardOutput.Should().Be(standardOutput);
        result.StandardError.Should().Be(standardError);
        result.ExecutionTime.Should().Be(executionTime);
        result.Exception.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(255)]
    public void HelperToolResult_Failure_ShouldCreateFailureResult(int exitCode)
    {
        // Arrange
        const string standardOutput = "Output";
        const string standardError = "Error";
        var executionTime = TimeSpan.FromMilliseconds(200);

        // Act
        var result = HelperToolResult.Failure(exitCode, standardOutput, standardError, executionTime);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().Be(exitCode);
        result.StandardOutput.Should().Be(standardOutput);
        result.StandardError.Should().Be(standardError);
        result.ExecutionTime.Should().Be(executionTime);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void HelperToolInfo_Constructor_ShouldInitializeProperties()
    {
        // Arrange
        const string id = "TestHelper";
        const string label = "Test Helper";
        const string description = "A test helper tool";
        const string command = "test.exe";
        var resourceTypes = new List<uint> { 0x12345678 };

        // Act
        var helperInfo = new HelperToolInfo
        {
            Id = id,
            Label = label,
            Description = description,
            Command = command,
            SupportedResourceTypes = resourceTypes
        };

        // Assert
        helperInfo.Id.Should().Be(id);
        helperInfo.Label.Should().Be(label);
        helperInfo.Description.Should().Be(description);
        helperInfo.Command.Should().Be(command);
        helperInfo.SupportedResourceTypes.Should().BeEquivalentTo(resourceTypes);
    }

    [Fact]
    public void HelperToolInfo_ToString_ShouldReturnLabel()
    {
        // Arrange
        const string label = "Test Helper Label";
        const string id = "test";
        var helperInfo = new HelperToolInfo
        {
            Id = id,
            Label = label,
            Command = "test.exe"
        };

        // Act
        var result = helperInfo.ToString();

        // Assert
        result.Should().Be($"{label} ({id})");
    }

    [Fact]
    public async Task ExecuteForResourceAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new HelperToolService(NullLogger<HelperToolService>.Instance);

        // Act & Assert
        await service.Invoking(s => s.ExecuteForResourceAsync("test", null!, Array.Empty<string>()))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task ReloadHelpersAsync_ShouldClearExistingHelpers()
    {
        // Arrange
        var service = new HelperToolService(NullLogger<HelperToolService>.Instance);
        var initialCount = service.GetAvailableHelperTools().Count;

        // Act
        await service.ReloadHelpersAsync();

        // Assert
        var finalCount = service.GetAvailableHelperTools().Count;
        finalCount.Should().Be(0); // No actual helper files in test environment
    }
}
