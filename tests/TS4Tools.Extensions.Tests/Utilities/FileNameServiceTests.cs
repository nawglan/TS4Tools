using Microsoft.Extensions.Logging.Abstractions;

namespace TS4Tools.Extensions.Tests.Utilities;

/// <summary>
/// Tests for the <see cref="FileNameService"/> class.
/// </summary>
public sealed class FileNameServiceTests
{
    private readonly IResourceTypeRegistry _mockTypeRegistry;
    private readonly FileNameService _fileNameService;

    public FileNameServiceTests()
    {
        _mockTypeRegistry = Substitute.For<IResourceTypeRegistry>();
        _fileNameService = new FileNameService(_mockTypeRegistry, NullLogger<FileNameService>.Instance);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        _fileNameService.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullTypeRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FileNameService(null!, NullLogger<FileNameService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("typeRegistry");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FileNameService(_mockTypeRegistry, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GetFileName_WithResourceKeyAndBaseName_ReturnsBaseNameWithCorrectExtension()
    {
        // Arrange
        var resourceKey = new ResourceKey(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);
        const string baseName = "MyCustomFile";
        const string expectedExtension = ".test";

        _mockTypeRegistry.GetExtension(0x12345678).Returns(expectedExtension);

        // Act
        var fileName = _fileNameService.GetFileName(resourceKey, baseName);

        // Assert
        fileName.Should().Be("MyCustomFile.test");
    }

    [Fact]
    public void GetFileName_WithResourceKeyAndNoBaseName_GeneratesFromTgi()
    {
        // Arrange
        var resourceKey = new ResourceKey(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);
        const string expectedTag = "TEST";
        const string expectedExtension = ".test";

        _mockTypeRegistry.GetTag(0x12345678).Returns(expectedTag);
        _mockTypeRegistry.GetExtension(0x12345678).Returns(expectedExtension);

        // Act
        var fileName = _fileNameService.GetFileName(resourceKey);

        // Assert
        fileName.Should().Be("TEST_12345678_9ABCDEF0_FEDCBA9876543210.test");
    }

    [Fact]
    public void GetFileName_WithIdentifierAndName_UsesNameInFileName()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "MyResource");
        const string expectedExtension = ".test";

        _mockTypeRegistry.GetExtension(0x12345678).Returns(expectedExtension);

        // Act
        var fileName = _fileNameService.GetFileName(identifier);

        // Assert
        fileName.Should().Be("MyResource.test");
    }

    [Fact]
    public void GetFileName_WithUnknownResourceType_UsesDatExtension()
    {
        // Arrange
        var resourceKey = new ResourceKey(0xDEADBEEF, 0x9ABCDEF0, 0xFEDCBA9876543210);

        _mockTypeRegistry.GetExtension(0xDEADBEEF).Returns((string?)null);
        _mockTypeRegistry.GetTag(0xDEADBEEF).Returns((string?)null);

        // Act
        var fileName = _fileNameService.GetFileName(resourceKey);

        // Assert
        fileName.Should().EndWith(".dat");
        fileName.Should().Contain("UNKN_DEADBEEF_9ABCDEF0_FEDCBA9876543210");
    }

    [Fact]
    public void GetFileName_WithNullResourceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _fileNameService.GetFileName((IResourceKey)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceKey");
    }

    [Theory]
    [InlineData("normal_filename", "normal_filename")]
    [InlineData("file<>name", "file__name")]
    [InlineData("file:name", "file_name")]
    [InlineData("file|name", "file_name")]
    [InlineData("file?name", "file_name")]
    [InlineData("file*name", "file_name")]
    [InlineData("file\"name", "file_name")]
    [InlineData("file/name", "file_name")]
    [InlineData("file\\name", "file_name")]
    [InlineData("CON", "_CON")]
    [InlineData("PRN", "_PRN")]
    [InlineData("AUX", "_AUX")]
    [InlineData("NUL", "_NUL")]
    [InlineData("COM1", "_COM1")]
    [InlineData("LPT1", "_LPT1")]
    public void SanitizeFileName_WithVariousInputs_ReturnsCorrectResult(string input, string expected)
    {
        // Act
        var result = _fileNameService.SanitizeFileName(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizeFileName_WithInvalidInput_ThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        var act = () => _fileNameService.SanitizeFileName(invalidInput!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SanitizeFileName_WithVeryLongFileName_TruncatesCorrectly()
    {
        // Arrange
        var longName = new string('a', 300);

        // Act
        var result = _fileNameService.SanitizeFileName(longName);

        // Assert
        result.Length.Should().BeLessOrEqualTo(240);
        result.Should().NotEndWith(" ");
        result.Should().NotEndWith(".");
    }

    [Fact]
    public void SanitizeFileName_WithEmptyAfterSanitization_ReturnsUnnamed()
    {
        // Arrange
        const string inputWithOnlyInvalidChars = "<>|?*";

        // Act
        var result = _fileNameService.SanitizeFileName(inputWithOnlyInvalidChars);

        // Assert
        result.Should().Be("unnamed");
    }

    [Fact]
    public void GetUniqueFileName_WithNonExistentFile_ReturnsOriginalName()
    {
        // Arrange
        using var tempDir = CreateTempDirectory();
        const string fileName = "test.txt";

        // Act
        var result = _fileNameService.GetUniqueFileName(fileName, tempDir.FullName);

        // Assert
        result.Should().Be(fileName);
    }

    [Fact]
    public void GetUniqueFileName_WithExistingFile_ReturnsNumberedName()
    {
        // Arrange
        using var tempDir = CreateTempDirectory();
        const string fileName = "test.txt";
        var filePath = Path.Combine(tempDir.FullName, fileName);
        File.WriteAllText(filePath, "test content");

        // Act
        var result = _fileNameService.GetUniqueFileName(fileName, tempDir.FullName);

        // Assert
        result.Should().Be("test_001.txt");
    }

    [Fact]
    public void GetUniqueFileName_WithMultipleExistingFiles_ReturnsCorrectNumber()
    {
        // Arrange
        using var tempDir = CreateTempDirectory();
        const string fileName = "test.txt";
        
        // Create several existing files
        File.WriteAllText(Path.Combine(tempDir.FullName, "test.txt"), "content");
        File.WriteAllText(Path.Combine(tempDir.FullName, "test_001.txt"), "content");
        File.WriteAllText(Path.Combine(tempDir.FullName, "test_002.txt"), "content");

        // Act
        var result = _fileNameService.GetUniqueFileName(fileName, tempDir.FullName);

        // Assert
        result.Should().Be("test_003.txt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetUniqueFileName_WithInvalidFileName_ThrowsArgumentException(string? invalidFileName)
    {
        // Act & Assert
        var act = () => _fileNameService.GetUniqueFileName(invalidFileName!, "c:\\temp");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetUniqueFileName_WithInvalidDirectory_ThrowsArgumentException(string? invalidDirectory)
    {
        // Act & Assert
        var act = () => _fileNameService.GetUniqueFileName("test.txt", invalidDirectory!);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Creates a temporary directory that will be cleaned up automatically.
    /// </summary>
    /// <returns>A disposable temporary directory.</returns>
    private static TempDirectory CreateTempDirectory()
    {
        return new TempDirectory();
    }

    /// <summary>
    /// Represents a temporary directory that cleans itself up when disposed.
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirectoryInfo { get; }

        public string FullName => DirectoryInfo.FullName;

        public TempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            DirectoryInfo = Directory.CreateDirectory(tempPath);
        }

        public void Dispose()
        {
            try
            {
                if (DirectoryInfo.Exists)
                {
                    DirectoryInfo.Delete(true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
