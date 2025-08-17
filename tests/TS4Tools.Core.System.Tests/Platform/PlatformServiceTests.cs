using FluentAssertions;
using TS4Tools.Core.System.Platform;
using Xunit;

namespace TS4Tools.Core.System.Tests.Platform;

/// <summary>
/// Unit tests for <see cref="PlatformService"/>.
/// </summary>
public class PlatformServiceTests
{
    private readonly PlatformService _platformService;

    public PlatformServiceTests()
    {
        _platformService = new PlatformService();
    }

    [Fact]
    public void CurrentPlatform_ShouldReturnValidPlatformType()
    {
        // Act
        var platform = _platformService.CurrentPlatform;

        // Assert
        platform.Should().BeOneOf(PlatformType.Windows, PlatformType.MacOS, PlatformType.Linux, PlatformType.Unknown);
    }

    [Fact]
    public void GetConfigurationDirectory_ShouldReturnNonEmptyPath()
    {
        // Act
        var configDir = _platformService.GetConfigurationDirectory();

        // Assert
        configDir.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(configDir).Should().BeTrue();
    }

    [Fact]
    public void GetApplicationDataDirectory_ShouldReturnNonEmptyPath()
    {
        // Act
        var dataDir = _platformService.GetApplicationDataDirectory();

        // Assert
        dataDir.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(dataDir).Should().BeTrue();
    }

    [Theory]
    [InlineData("validfilename.txt", true)]
    [InlineData("valid_filename123.dat", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("file\0name.txt", false)]
    public void IsValidFileName_ShouldValidateFileNamesCorrectly(string fileName, bool expectedValid)
    {
        // Act
        var isValid = _platformService.IsValidFileName(fileName);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("CON", false)] // Windows reserved name
    [InlineData("PRN.txt", false)] // Windows reserved name with extension
    [InlineData("valid.txt", true)]
    [InlineData("com1", false)] // Case insensitive
    public void IsValidFileName_WindowsReservedNames_ShouldBeHandledCorrectly(string fileName, bool expectedValid)
    {
        // Arrange - Only test on Windows, skip on other platforms
        if (_platformService.CurrentPlatform != PlatformType.Windows)
        {
            return; // Skip test on non-Windows platforms
        }

        // Act
        var isValid = _platformService.IsValidFileName(fileName);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizeFileName_WithInvalidInput_ShouldReturnSafeDefault(string? input)
    {
        // Act
        var sanitized = _platformService.SanitizeFileName(input ?? "");

        // Assert
        sanitized.Should().NotBeNullOrWhiteSpace();
        sanitized.Should().Be("unnamed");
    }

    [Theory]
    [InlineData("valid_filename.txt")]
    [InlineData("file<>name.txt")] // Invalid chars should be replaced
    [InlineData("CON")] // Reserved name should be handled (Windows)
    [InlineData("file\0name")] // Null char should be removed
    public void SanitizeFileName_WithVariousInputs_ShouldReturnValidNames(string input)
    {
        // Act
        var sanitized = _platformService.SanitizeFileName(input);

        // Assert
        sanitized.Should().NotBeNullOrWhiteSpace();
        _platformService.IsValidFileName(sanitized).Should().BeTrue();

        // Platform-specific behavior
        if (_platformService.CurrentPlatform == PlatformType.Windows && input == "CON")
        {
            sanitized.Should().Contain("CON").And.Contain("_");
        }
    }

    [Fact]
    public void GetLineEnding_ShouldReturnPlatformAppropriateLineEnding()
    {
        // Act
        var lineEnding = _platformService.GetLineEnding();

        // Assert
        lineEnding.Should().NotBeNullOrEmpty();

        // Verify platform-specific line endings
        lineEnding.Should().BeOneOf("\r\n", "\n");

        if (_platformService.CurrentPlatform == PlatformType.Windows)
        {
            lineEnding.Should().Be("\r\n");
        }
        else
        {
            lineEnding.Should().Be("\n");
        }
    }

    [Fact]
    public void SupportsApplicationManifest_ShouldReturnTrueOnlyForWindows()
    {
        // Act
        var supportsManifest = _platformService.SupportsApplicationManifest;

        // Assert
        if (_platformService.CurrentPlatform == PlatformType.Windows)
        {
            supportsManifest.Should().BeTrue();
        }
        else
        {
            supportsManifest.Should().BeFalse();
        }
    }

    [Fact]
    public void Instance_ShouldReturnSameSingletonInstance()
    {
        // Act
        var instance1 = PlatformService.Instance;
        var instance2 = PlatformService.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void SanitizeFileName_WithVeryLongName_ShouldTruncateToValidLength()
    {
        // Arrange
        var longName = new string('a', 300); // Longer than most filesystem limits

        // Act
        var sanitized = _platformService.SanitizeFileName(longName);

        // Assert
        sanitized.Should().NotBeNullOrWhiteSpace();
        sanitized.Length.Should().BeLessOrEqualTo(255);
        _platformService.IsValidFileName(sanitized).Should().BeTrue();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void IsValidFileName_UnixReservedNames_ShouldBeInvalid(string fileName)
    {
        // Skip test on Windows using early return - this is intentional for cross-platform testing
        if (_platformService.CurrentPlatform == PlatformType.Windows)
        {
            return; // Skip test on Windows - this test only applies to Unix-like platforms
        }

        // Act
        var isValid = _platformService.IsValidFileName(fileName);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ConfigurationAndDataDirectories_ShouldBeDifferent()
    {
        // Act
        var configDir = _platformService.GetConfigurationDirectory();
        var dataDir = _platformService.GetApplicationDataDirectory();

        // Assert
        configDir.Should().NotBe(dataDir, "Configuration and data directories should be different");
    }
}
