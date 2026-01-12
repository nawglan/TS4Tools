using FluentAssertions;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="IFileSystemService"/> abstraction and its usage in ViewModels.
/// </summary>
public class FileSystemServiceTests
{
    [Fact]
    public void FileSystemService_Instance_IsSingleton()
    {
        // Act
        var instance1 = FileSystemService.Instance;
        var instance2 = FileSystemService.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void FileSystemService_CombinePath_CombinesPaths()
    {
        // Arrange
        var service = FileSystemService.Instance;

        // Act
        var result = service.CombinePath("foo", "bar", "baz");

        // Assert
        result.Should().Contain("foo");
        result.Should().Contain("bar");
        result.Should().Contain("baz");
    }

    [Fact]
    public void FileSystemService_GetFileName_ExtractsFileName()
    {
        // Arrange
        var service = FileSystemService.Instance;

        // Act
        var result = service.GetFileName("/path/to/file.txt");

        // Assert
        result.Should().Be("file.txt");
    }

    [Fact]
    public void FileSystemService_GetDirectoryName_ExtractsDirectory()
    {
        // Arrange
        var service = FileSystemService.Instance;

        // Act
        var result = service.GetDirectoryName("/path/to/file.txt");

        // Assert
        result.Should().Contain("path");
        result.Should().Contain("to");
    }

    [Fact]
    public void FileSystemService_GetTempPath_ReturnsPath()
    {
        // Arrange
        var service = FileSystemService.Instance;

        // Act
        var result = service.GetTempPath();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FileSystemService_GetApplicationDataPath_ReturnsPath()
    {
        // Arrange
        var service = FileSystemService.Instance;

        // Act
        var result = service.GetApplicationDataPath();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MockFileSystemService_FileExists_ReturnsFalseForNonExistentFile()
    {
        // Arrange
        var mock = new MockFileSystemService();

        // Act
        var result = mock.FileExists("/nonexistent/file.txt");

        // Assert
        result.Should().BeFalse();
        mock.OperationLog.Should().Contain("FileExists: /nonexistent/file.txt");
    }

    [Fact]
    public void MockFileSystemService_FileExists_ReturnsTrueForSetupFile()
    {
        // Arrange
        var mock = new MockFileSystemService();
        mock.SetupFile("/test/file.bin", [0x01, 0x02, 0x03]);

        // Act
        var result = mock.FileExists("/test/file.bin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MockFileSystemService_ReadAllBytesAsync_ReturnsSetupContent()
    {
        // Arrange
        var mock = new MockFileSystemService();
        byte[] expected = [0xDE, 0xAD, 0xBE, 0xEF];
        mock.SetupFile("/test/data.bin", expected);

        // Act
        var result = await mock.ReadAllBytesAsync("/test/data.bin");

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task MockFileSystemService_ReadAllBytesAsync_ThrowsForNonExistentFile()
    {
        // Arrange
        var mock = new MockFileSystemService();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            mock.ReadAllBytesAsync("/nonexistent/file.bin"));
    }

    [Fact]
    public async Task MockFileSystemService_WriteAllBytesAsync_StoresContent()
    {
        // Arrange
        var mock = new MockFileSystemService();
        byte[] content = [0x01, 0x02, 0x03, 0x04];

        // Act
        await mock.WriteAllBytesAsync("/output/file.bin", content);

        // Assert
        mock.WrittenFiles.Should().ContainKey("/output/file.bin");
        mock.WrittenFiles["/output/file.bin"].Should().BeEquivalentTo(content);
    }

    [Fact]
    public void MockFileSystemService_ReadAllText_ReturnsSetupContent()
    {
        // Arrange
        var mock = new MockFileSystemService();
        mock.SetupTextFile("/test/config.json", "{\"key\": \"value\"}");

        // Act
        var result = mock.ReadAllText("/test/config.json");

        // Assert
        result.Should().Be("{\"key\": \"value\"}");
    }

    [Fact]
    public void MockFileSystemService_WriteAllText_StoresContent()
    {
        // Arrange
        var mock = new MockFileSystemService();

        // Act
        mock.WriteAllText("/output/file.txt", "Hello, World!");

        // Assert
        mock.WrittenTextFiles.Should().ContainKey("/output/file.txt");
        mock.WrittenTextFiles["/output/file.txt"].Should().Be("Hello, World!");
    }

    [Fact]
    public void MockFileSystemService_CreateDirectory_TracksCreation()
    {
        // Arrange
        var mock = new MockFileSystemService();

        // Act
        mock.CreateDirectory("/new/directory");

        // Assert
        mock.CreatedDirectories.Should().Contain("/new/directory");
        mock.OperationLog.Should().Contain("CreateDirectory: /new/directory");
    }

    [Fact]
    public void MockFileSystemService_DeleteFile_RemovesFile()
    {
        // Arrange
        var mock = new MockFileSystemService();
        mock.SetupFile("/test/file.bin", [0x01]);

        // Act
        mock.DeleteFile("/test/file.bin");

        // Assert
        mock.FileExists("/test/file.bin").Should().BeFalse();
    }

    [Fact]
    public void MockFileSystemService_GetLastWriteTimeUtc_ReturnsSetTime()
    {
        // Arrange
        var mock = new MockFileSystemService();
        var expectedTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        mock.SetupLastWriteTime("/test/file.bin", expectedTime);

        // Act
        var result = mock.GetLastWriteTimeUtc("/test/file.bin");

        // Assert
        result.Should().Be(expectedTime);
    }

    [Fact]
    public void MainWindowViewModel_Constructor_AcceptsMockFileSystem()
    {
        // Arrange
        var mockFileSystem = new MockFileSystemService();

        // Act
        var vm = new MainWindowViewModel(mockFileSystem);

        // Assert
        vm.Should().NotBeNull();
        vm.Title.Should().Be("TS4Tools");
    }

    [Fact]
    public void MainWindowViewModel_WithMockFileSystem_RecentFilesPathUsesAppData()
    {
        // Arrange
        var mockFileSystem = new MockFileSystemService();
        mockFileSystem.SetupTextFile(
            mockFileSystem.CombinePath(mockFileSystem.GetApplicationDataPath(), "TS4Tools", "recent.json"),
            "[\"/test/package1.package\", \"/test/package2.package\"]");

        // Act
        var vm = new MainWindowViewModel(mockFileSystem);

        // Assert
        vm.RecentFiles.Should().HaveCount(2);
        mockFileSystem.OperationLog.Should().Contain(op => op.StartsWith("FileExists:"));
    }

    [Fact]
    public void MainWindowViewModel_WithMockFileSystem_NoRecentFilesWhenFileNotExists()
    {
        // Arrange
        var mockFileSystem = new MockFileSystemService();
        // Don't set up any recent files

        // Act
        var vm = new MainWindowViewModel(mockFileSystem);

        // Assert
        vm.RecentFiles.Should().BeEmpty();
    }

    [Fact]
    public void MainWindowViewModel_DefaultConstructor_UsesRealFileSystem()
    {
        // Act - should not throw
        var vm = new MainWindowViewModel();

        // Assert
        vm.Should().NotBeNull();
    }
}
