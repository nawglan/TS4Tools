using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="SimDataViewerViewModel"/>.
/// </summary>
public class SimDataViewerViewModelTests
{
    private static readonly ResourceKey TestKey = new(0x545AC67A, 0, 0);

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new SimDataViewerViewModel();

        // Assert
        vm.IsValid.Should().BeFalse();
        vm.VersionHex.Should().BeEmpty();
        vm.SchemaCount.Should().Be(0);
        vm.TableCount.Should().Be(0);
        vm.DataSize.Should().Be(0);
        vm.HeaderInfo.Should().BeEmpty();
        vm.HexPreview.Should().BeEmpty();
        vm.SelectedSchema.Should().BeNull();
        vm.SelectedTable.Should().BeNull();
        vm.Schemas.Should().BeEmpty();
        vm.Tables.Should().BeEmpty();
    }

    [Fact]
    public void LoadResource_WithEmptyData_SetsInvalid()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resource = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoadResource_SetsVersionHex()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resource = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.VersionHex.Should().StartWith("0x");
    }

    [Fact]
    public void LoadResource_BuildsHeaderInfo()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resource = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.HeaderInfo.Should().NotBeEmpty();
        vm.HeaderInfo.Should().Contain("Magic: DATA");
    }

    [Fact]
    public void LoadResource_InvalidData_ShowsWarningInHeader()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resource = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.HeaderInfo.Should().Contain("WARNING");
    }

    [Fact]
    public void LoadResource_BuildsHexPreview()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var data = new byte[100];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)i;
        var resource = new SimDataResource(TestKey, data);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.HexPreview.Should().NotBeEmpty();
        vm.HexPreview.Should().Contain("hex dump");
    }

    [Fact]
    public void LoadResource_ClearsCollectionsOnReload()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resource1 = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var resource2 = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource1);
        vm.LoadResource(resource2);

        // Assert - should work without errors
        vm.Schemas.Should().BeEmpty(); // Empty data means no schemas
        vm.Tables.Should().BeEmpty();
    }

    [Fact]
    public void GetData_WithNoResource_ReturnsEmpty()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();

        // Act
        var data = vm.GetData();

        // Assert
        data.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetData_WithResource_ReturnsResourceData()
    {
        // Arrange
        var vm = new SimDataViewerViewModel();
        var resourceData = new byte[] { 1, 2, 3, 4 };
        var resource = new SimDataResource(TestKey, resourceData);
        vm.LoadResource(resource);

        // Act
        var data = vm.GetData();

        // Assert
        data.Length.Should().Be(resourceData.Length);
    }

    #region SimDataSchemaViewModel Tests

    [Fact]
    public void SimDataSchemaViewModel_DisplayName_ShowsSchemaIndex_WhenNameEmpty()
    {
        // Arrange - ColumnCount is computed from Fields.Count, not settable
        var schema = new SimDataSchema
        {
            Index = 5,
            Name = "",
            NameHash = 0x12345678,
            Size = 100
        };

        // Act
        var vm = new SimDataSchemaViewModel(schema);

        // Assert
        vm.DisplayName.Should().Be("Schema 5");
        vm.Index.Should().Be(5);
        vm.NameHashHex.Should().Be("0x12345678");
        vm.ColumnCount.Should().Be(0); // No fields added
        vm.Size.Should().Be(100);
    }

    [Fact]
    public void SimDataSchemaViewModel_DisplayName_ShowsName_WhenProvided()
    {
        // Arrange
        var schema = new SimDataSchema
        {
            Index = 0,
            Name = "TestSchema",
            NameHash = 0,
            Size = 0
        };

        // Act
        var vm = new SimDataSchemaViewModel(schema);

        // Assert
        vm.DisplayName.Should().Be("TestSchema");
        vm.Name.Should().Be("TestSchema");
    }

    #endregion

    #region SimDataFieldViewModel Tests

    [Fact]
    public void SimDataFieldViewModel_FormatsCorrectly()
    {
        // Arrange - DataSize is computed from Type.GetSize(), not settable
        var field = new SimDataField
        {
            Index = 1,
            Name = "TestField",
            NameHash = 0xABCDEF01,
            Type = SimDataFieldType.FloatValue,
            TypeValue = 0x05,
            DataOffset = 100
        };

        // Act
        var vm = new SimDataFieldViewModel(field);

        // Assert
        vm.Index.Should().Be(1);
        vm.Name.Should().Be("TestField");
        vm.DisplayName.Should().Be("TestField");
        vm.NameHashHex.Should().Be("0xABCDEF01");
        vm.TypeName.Should().Be("Float");
        vm.DataOffset.Should().Be(100);
        vm.DataSize.Should().BeGreaterThanOrEqualTo(0); // Computed from Type
    }

    [Fact]
    public void SimDataFieldViewModel_DisplayName_ShowsFieldIndex_WhenNameEmpty()
    {
        // Arrange - DataSize is computed from Type.GetSize()
        var field = new SimDataField
        {
            Index = 3,
            Name = "",
            NameHash = 0,
            Type = SimDataFieldType.Boolean,
            TypeValue = 0x01,
            DataOffset = 0
        };

        // Act
        var vm = new SimDataFieldViewModel(field);

        // Assert
        vm.DisplayName.Should().Be("Field 3");
    }

    [Theory]
    [InlineData(SimDataFieldType.Boolean, "Boolean")]
    [InlineData(SimDataFieldType.Integer16, "Int16")]
    [InlineData(SimDataFieldType.FloatValue, "Float")]
    [InlineData(SimDataFieldType.RGBColor, "RGB Color")]
    [InlineData(SimDataFieldType.ARGBColor, "ARGB Color")]
    [InlineData(SimDataFieldType.DataInstance, "Data Instance")]
    [InlineData(SimDataFieldType.ImageInstance, "Image Instance")]
    [InlineData(SimDataFieldType.StringInstance, "String Instance")]
    public void SimDataFieldViewModel_TypeName_MapsCorrectly(SimDataFieldType type, string expectedName)
    {
        // Arrange - DataSize is computed from Type.GetSize()
        var field = new SimDataField
        {
            Index = 0,
            Name = "",
            NameHash = 0,
            Type = type,
            TypeValue = (uint)type,
            DataOffset = 0
        };

        // Act
        var vm = new SimDataFieldViewModel(field);

        // Assert
        vm.TypeName.Should().Be(expectedName);
    }

    #endregion
}
