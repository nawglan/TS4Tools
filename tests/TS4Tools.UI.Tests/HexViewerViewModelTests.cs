using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="HexViewerViewModel"/>.
/// </summary>
public class HexViewerViewModelTests
{
    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new HexViewerViewModel();

        // Assert
        vm.HexContent.Should().BeEmpty();
        vm.DataLength.Should().Be(0);
        vm.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public void LoadData_WithEmptyData_ShowsNoDataMessage()
    {
        // Arrange
        var vm = new HexViewerViewModel();

        // Act
        vm.LoadData(ReadOnlyMemory<byte>.Empty);

        // Assert
        vm.HexContent.Should().Contain("No data");
        vm.DataLength.Should().Be(0);
        vm.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public void LoadData_SetsDataLength()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        vm.LoadData(data);

        // Assert
        vm.DataLength.Should().Be(4);
    }

    [Fact]
    public void LoadData_FormatsHexCorrectly()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        // Act
        vm.LoadData(data);

        // Assert
        vm.HexContent.Should().Contain("DE");
        vm.HexContent.Should().Contain("AD");
        vm.HexContent.Should().Contain("BE");
        vm.HexContent.Should().Contain("EF");
    }

    [Fact]
    public void LoadData_IncludesOffsetColumn()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        vm.LoadData(data);

        // Assert - should have offset at start
        vm.HexContent.Should().Contain("00000000");
    }

    [Fact]
    public void LoadData_IncludesAsciiColumn()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        // "Test" in ASCII bytes
        var data = new byte[] { 0x54, 0x65, 0x73, 0x74 };

        // Act
        vm.LoadData(data);

        // Assert - should contain the ASCII representation
        vm.HexContent.Should().Contain("Test");
    }

    [Fact]
    public void LoadData_NonPrintableChars_ShowsAsDots()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        // Non-printable bytes
        var data = new byte[] { 0x00, 0x01, 0x02, 0x1F };

        // Act
        vm.LoadData(data);

        // Assert - ASCII column should show dots for non-printable chars
        // The output contains the dots in the ASCII section
        vm.HexContent.Should().Contain("....");
    }

    [Fact]
    public void LoadData_SmallData_NotTruncated()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[1000]; // Well under 64KB

        // Act
        vm.LoadData(data);

        // Assert
        vm.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public void LoadData_LargeData_IsTruncated()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[100_000]; // Over 64KB limit

        // Act
        vm.LoadData(data);

        // Assert
        vm.IsTruncated.Should().BeTrue();
        vm.DataLength.Should().Be(100_000);
        vm.HexContent.Should().Contain("more bytes");
    }

    [Fact]
    public void LoadData_IncludesHeader()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        var data = new byte[] { 0x01 };

        // Act
        vm.LoadData(data);

        // Assert - should have column header with "Offset" and "ASCII"
        vm.HexContent.Should().Contain("Offset");
        vm.HexContent.Should().Contain("ASCII");
    }

    [Fact]
    public void LoadData_MultipleLines_FormatsCorrectly()
    {
        // Arrange
        var vm = new HexViewerViewModel();
        // 32 bytes = 2 lines of 16 bytes each
        var data = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            data[i] = (byte)i;
        }

        // Act
        vm.LoadData(data);

        // Assert - should have multiple offset rows
        vm.HexContent.Should().Contain("00000000");
        vm.HexContent.Should().Contain("00000010"); // Offset 16 in hex
    }
}
