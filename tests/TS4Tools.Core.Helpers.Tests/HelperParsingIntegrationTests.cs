using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using TS4Tools.Core.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace TS4Tools.Core.Helpers.Tests;

/// <summary>
/// Test-specific helper tool service that searches in custom directories
/// </summary>
internal sealed class TestHelperToolService : HelperToolService
{
    private readonly string[] _customSearchPaths;

    public TestHelperToolService(ILogger<HelperToolService> logger, params string[] customSearchPaths)
        : base(logger)
    {
        _customSearchPaths = customSearchPaths;
    }

    protected override IEnumerable<string> GetHelperSearchPathsCore()
    {
        return _customSearchPaths;
    }
}

public sealed class HelperParsingIntegrationTests : IDisposable
{
    private readonly TestHelperToolService _service;
    private readonly string _testHelpersDirectory;
    private readonly ITestOutputHelper _output;

    public HelperParsingIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _testHelpersDirectory = Path.Combine(Path.GetTempPath(), "TS4Tools_TestHelpers", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testHelpersDirectory);
        _service = new TestHelperToolService(NullLogger<HelperToolService>.Instance, _testHelpersDirectory);
    }

    [Fact]
    public async Task ReloadHelpersAsync_WithValidHelperFile_ShouldParseCorrectly()
    {
        // Arrange
        var systemCommand = OperatingSystem.IsWindows() ? "notepad.exe" : "echo";
        var helperContent = $@"// Test Helper
ResourceType: 0x00B2D882 0x2F7D0004
Label: Test Image Editor
Command: {systemCommand}
Arguments: /edit ""{{}}""
Desc: Edit images with test editor
ReadOnly: false
";

        var helperFile = Path.Combine(_testHelpersDirectory, "TestHelper.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // Act
        await _service.ReloadHelpersAsync();

        // Assert
        var availableHelpers = _service.GetAvailableHelperTools();
        availableHelpers.Should().Contain("TestHelper");

        var helpers = _service.GetHelpersForResourceType(0x00B2D882);
        helpers.Should().HaveCount(1);

        var helper = helpers.First();
        helper.Id.Should().Be("TestHelper");
        helper.Label.Should().Be("Test Image Editor");
        helper.Command.Should().Be(systemCommand);
        helper.Arguments.Should().Be("/edit \"{}\"");
        helper.Description.Should().Be("Edit images with test editor");
        helper.IsReadOnly.Should().BeFalse();
        helper.SupportedResourceTypes.Should().Contain(0x00B2D882);
        helper.SupportedResourceTypes.Should().Contain(0x2F7D0004);
    }

    [Fact]
    public async Task ReloadHelpersAsync_WithCommentedHelperFile_ShouldIgnoreComments()
    {
        // Arrange
        var helperContent = @"// This is a comment
/* Block comment start
   Still in comment
*/ ResourceType: 0x12345678
// Another comment
Label: Test Helper /* inline comment */ Tool
Command: notepad.exe
// Description comment
Desc: Test description
";

        var helperFile = Path.Combine(_testHelpersDirectory, "CommentTest.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // DEBUG: Add debugging to understand cross-platform failures
        _output.WriteLine("=== CROSS-PLATFORM DEBUG: Comment Test Helper ===");
        _output.WriteLine($"Operating System: {Environment.OSVersion}");
        _output.WriteLine($"Is Windows: {OperatingSystem.IsWindows()}");
        _output.WriteLine($"Is Linux: {OperatingSystem.IsLinux()}");
        _output.WriteLine($"Is macOS: {OperatingSystem.IsMacOS()}");
        _output.WriteLine($"Test Helpers Directory: {_testHelpersDirectory}");
        _output.WriteLine($"Helper File Path: {helperFile}");
        _output.WriteLine($"Helper File Exists: {File.Exists(helperFile)}");
        _output.WriteLine("Original Helper Content:");
        _output.WriteLine(helperContent);
        _output.WriteLine("=== Character Analysis ===");
        var hexOutput = new StringBuilder();
        for (int i = 0; i < helperContent.Length && i < 200; i++)
        {
            var c = helperContent[i];
            hexOutput.Append($"{(int)c:X2}");
            if (c == '\n') hexOutput.Append("(LF)");
            else if (c == '\r') hexOutput.Append("(CR)");
            else if (c == ' ') hexOutput.Append("(SP)");
            hexOutput.Append(" ");
            if ((i + 1) % 20 == 0) 
            {
                _output.WriteLine(hexOutput.ToString());
                hexOutput.Clear();
            }
        }
        if (hexOutput.Length > 0)
        {
            _output.WriteLine(hexOutput.ToString());
        }
        _output.WriteLine("=== End Character Analysis ===");
        _output.WriteLine("=== End Original Content ===");

        // Verify file was written correctly
        var readBackContent = await File.ReadAllTextAsync(helperFile);
        _output.WriteLine("Read Back Content:");
        _output.WriteLine(readBackContent);
        _output.WriteLine("=== End Read Back Content ===");
        _output.WriteLine($"Content matches: {helperContent == readBackContent}");

        // Act
        await _service.ReloadHelpersAsync();

        // DEBUG: Check what helpers were loaded
        var allAvailableHelpers = _service.GetAvailableHelperTools();
        _output.WriteLine($"Available helpers count: {allAvailableHelpers.Count()}");
        _output.WriteLine($"Available helpers: [{string.Join(", ", allAvailableHelpers)}]");

        var helpers = _service.GetHelpersForResourceType(0x12345678);
        _output.WriteLine($"Helpers for ResourceType 0x12345678: {helpers.Count()}");

        if (helpers.Any())
        {
            var helper = helpers.First();
            _output.WriteLine($"Helper ID: {helper.Id}");
            _output.WriteLine($"Helper Label: '{helper.Label}'");
            _output.WriteLine($"Helper Command: '{helper.Command}'");
            _output.WriteLine($"Helper Description: '{helper.Description}'");
            _output.WriteLine($"Helper ResourceTypes: [{string.Join(", ", helper.SupportedResourceTypes.Select(rt => $"0x{rt:X8}"))}]");
        }
        else
        {
            _output.WriteLine("No helpers found for ResourceType 0x12345678");

            // Check if any helpers were loaded at all
            var allHelpers = _service.GetAvailableHelperTools().ToList();
            if (allHelpers.Count == 0)
            {
                _output.WriteLine("ERROR: No helpers loaded at all!");
            }
            else
            {
                _output.WriteLine($"Other helpers loaded: {string.Join(", ", allHelpers)}");
            }
        }
        _output.WriteLine("=== END CROSS-PLATFORM DEBUG ===");

        // Assert
        helpers.Should().HaveCount(1, "Expected exactly 1 helper to be loaded for ResourceType 0x12345678");

        var foundHelper = helpers.First();
        foundHelper.Label.Should().Be("Test Helper  Tool");
        foundHelper.Command.Should().Be("notepad.exe");
        foundHelper.Description.Should().Be("Test description");
    }

    [Fact]
    public async Task ReloadHelpersAsync_WithMinimalHelperFile_ShouldParseWithDefaults()
    {
        // Arrange
        var systemCommand = OperatingSystem.IsWindows() ? "notepad.exe" : "echo";
        var helperContent = $@"Label: Minimal Helper
Command: {systemCommand}
";

        var helperFile = Path.Combine(_testHelpersDirectory, "Minimal.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // Act
        await _service.ReloadHelpersAsync();

        // Assert
        var availableHelpers = _service.GetAvailableHelperTools();
        availableHelpers.Should().Contain("Minimal");

        // Should work even with no resource types (universal helper)
        var helpers = _service.GetHelpersForResourceType(0x12345678);
        helpers.Should().BeEmpty(); // No resource types specified
    }

    [Fact]
    public async Task ReloadHelpersAsync_WithSystemCommandHelper_ShouldMarkAsAvailableIfFound()
    {
        // Arrange - Use a system command that should exist on most systems
        var systemCommand = OperatingSystem.IsWindows() ? "notepad.exe" : "echo";

        var helperContent = $@"Label: System Command Test
Command: {systemCommand}
ResourceType: 0x12345678
";

        var helperFile = Path.Combine(_testHelpersDirectory, "SystemCommand.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // Act
        await _service.ReloadHelpersAsync();

        // Assert
        var helpers = _service.GetHelpersForResourceType(0x12345678);
        helpers.Should().HaveCount(1);

        var helper = helpers.First();
        helper.Command.Should().Be(systemCommand);

        // Availability depends on whether the system command exists
        if (OperatingSystem.IsWindows())
        {
            helper.IsAvailable.Should().BeTrue(); // notepad.exe should exist on Windows
        }
    }

    [Fact]
    public async Task ReloadHelpersAsync_WithInvalidHelperFile_ShouldIgnoreFile()
    {
        // Arrange - Missing required fields
        var helperContent = @"// Invalid helper - missing Label and Command
ResourceType: 0x12345678
Desc: This helper is invalid
";

        var helperFile = Path.Combine(_testHelpersDirectory, "Invalid.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // Act & Assert - Should not throw
        await _service.ReloadHelpersAsync();

        var availableHelpers = _service.GetAvailableHelperTools();
        availableHelpers.Should().NotContain("Invalid");
    }

    [Theory]
    [InlineData("0x12345678", 0x12345678U)]
    [InlineData("12345678", 0x12345678U)]
    [InlineData("0xABCDEF00", 0xABCDEF00U)]
    [InlineData("abcdef00", 0xABCDEF00U)]
    public async Task ReloadHelpersAsync_WithDifferentResourceTypeFormats_ShouldParseCorrectly(string resourceTypeString, uint expectedValue)
    {
        // Arrange
        var systemCommand = OperatingSystem.IsWindows() ? "notepad.exe" : "echo";
        var helperContent = $@"Label: Format Test
Command: {systemCommand}
ResourceType: {resourceTypeString}
";

        var helperFile = Path.Combine(_testHelpersDirectory, "FormatTest.helper");
        await File.WriteAllTextAsync(helperFile, helperContent);

        // Act
        await _service.ReloadHelpersAsync();

        // Assert
        var helpers = _service.GetHelpersForResourceType(expectedValue);
        helpers.Should().HaveCount(1);
        helpers.First().SupportedResourceTypes.Should().Contain(expectedValue);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testHelpersDirectory))
            {
                Directory.Delete(_testHelpersDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
