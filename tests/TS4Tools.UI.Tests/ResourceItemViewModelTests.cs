using FluentAssertions;
using TS4Tools.UI.ViewModels;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="ResourceItemViewModel"/>.
/// </summary>
public class ResourceItemViewModelTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var key = new ResourceKey(0x220557DA, 0x12345678, 0xABCDEF0123456789);
        const uint fileSize = 1024;

        // Act
        var vm = new ResourceItemViewModel(key, fileSize);

        // Assert
        vm.Key.Should().Be(key);
        vm.FileSize.Should().Be(fileSize);
    }

    [Fact]
    public void TypeName_KnownType_ReturnsName()
    {
        // Arrange - STBL type
        var key = new ResourceKey(0x220557DA, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("String Table (STBL)");
    }

    [Fact]
    public void TypeName_NameMap_ReturnsName()
    {
        // Arrange
        var key = new ResourceKey(0x0166038C, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("Name Map");
    }

    [Fact]
    public void TypeName_SimData_ReturnsName()
    {
        // Arrange
        var key = new ResourceKey(0x545AC67A, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("SimData (DATA)");
    }

    [Fact]
    public void TypeName_DDS_ReturnsName()
    {
        // Arrange
        var key = new ResourceKey(0x00B2D882, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("Image (DDS/DST)");
    }

    [Fact]
    public void TypeName_PNG_ReturnsName()
    {
        // Arrange
        var key = new ResourceKey(0x00B00000, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("Image (PNG)");
    }

    [Fact]
    public void TypeName_TuningXML_ReturnsName()
    {
        // Arrange
        var key = new ResourceKey(0x03B33DDF, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("Tuning (XML)");
    }

    [Fact]
    public void TypeName_UnknownType_ReturnsHexString()
    {
        // Arrange
        var key = new ResourceKey(0xDEADBEEF, 0, 0);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.TypeName.Should().Be("Unknown (0xDEADBEEF)");
    }

    [Fact]
    public void DisplayKey_FormatsCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0xABCDEF01, 0xFEDCBA9876543210);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.DisplayKey.Should().Be("T: 0x12345678 | G: 0xABCDEF01 | I: 0xFEDCBA9876543210");
    }

    [Fact]
    public void DisplayKey_WithZeros_FormatsWithLeadingZeros()
    {
        // Arrange
        var key = new ResourceKey(0x00000001, 0x00000002, 0x0000000000000003);
        var vm = new ResourceItemViewModel(key, 100);

        // Act & Assert
        vm.DisplayKey.Should().Be("T: 0x00000001 | G: 0x00000002 | I: 0x0000000000000003");
    }
}
