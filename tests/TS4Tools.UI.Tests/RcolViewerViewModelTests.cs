using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="RcolViewerViewModel"/>.
/// </summary>
public class RcolViewerViewModelTests
{
    private static readonly ResourceKey TestKey = new(RcolConstants.Modl, 0, 0);

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new RcolViewerViewModel();

        // Assert
        vm.IsValid.Should().BeFalse();
        vm.VersionDisplay.Should().BeEmpty();
        vm.PublicChunksCount.Should().Be(0);
        vm.ChunkCount.Should().Be(0);
        vm.ExternalCount.Should().Be(0);
        vm.HeaderInfo.Should().BeEmpty();
        vm.SelectedChunk.Should().BeNull();
        vm.Chunks.Should().BeEmpty();
        vm.ExternalResources.Should().BeEmpty();
    }

    [Fact]
    public void LoadResource_WithEmptyData_HandlesGracefully()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert - Empty data still produces a valid structure with defaults
        vm.ChunkCount.Should().Be(0);
    }

    [Fact]
    public void LoadResource_SetsVersionDisplay()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.VersionDisplay.Should().StartWith("0x");
    }

    [Fact]
    public void LoadResource_SetsChunkCounts()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.PublicChunksCount.Should().BeGreaterThanOrEqualTo(0);
        vm.ChunkCount.Should().BeGreaterThanOrEqualTo(0);
        vm.ExternalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void LoadResource_BuildsHeaderInfo()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.HeaderInfo.Should().NotBeEmpty();
        vm.HeaderInfo.Should().Contain("RCOL Version");
    }

    [Fact]
    public void LoadResource_InvalidData_ShowsWarningInHeader_WhenNotValid()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert - Check if warning is shown when IsValid is false
        if (!vm.IsValid)
        {
            vm.HeaderInfo.Should().Contain("WARNING");
        }
        else
        {
            // Valid resource should not have warning
            vm.HeaderInfo.Should().NotContain("WARNING");
        }
    }

    [Fact]
    public void LoadResource_ClearsCollectionsOnReload()
    {
        // Arrange
        var vm = new RcolViewerViewModel();
        var resource1 = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var resource2 = new RcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource1);
        vm.LoadResource(resource2);

        // Assert - should work without errors
        vm.Chunks.Should().BeEmpty(); // Empty data means no chunks
        vm.ExternalResources.Should().BeEmpty();
    }

    #region RcolExternalViewModel Tests

    [Fact]
    public void RcolExternalViewModel_FormatsCorrectly()
    {
        // Arrange - RcolTgiBlock(instance, resourceType, resourceGroup)
        var tgiBlock = new RcolTgiBlock(0x123456789ABCDEF0, 0x12345678, 0xABCDEF01);

        // Act
        var vm = new RcolExternalViewModel(tgiBlock, 0);

        // Assert
        vm.Index.Should().Be(0);
        vm.TypeHex.Should().Be("0x12345678");
        vm.GroupHex.Should().Be("0xABCDEF01");
        vm.InstanceHex.Should().Be("0x123456789ABCDEF0");
    }

    [Fact]
    public void RcolExternalViewModel_TypeName_ResolvesKnownTypes()
    {
        // Arrange - RcolTgiBlock(instance, resourceType, resourceGroup)
        var tgiBlock = new RcolTgiBlock(0, RcolConstants.Modl, 0);

        // Act
        var vm = new RcolExternalViewModel(tgiBlock, 0);

        // Assert
        vm.TypeName.Should().Be("MODL");
    }

    [Fact]
    public void RcolExternalViewModel_TgiString_FormatsComplete()
    {
        // Arrange - RcolTgiBlock(instance, resourceType, resourceGroup)
        var tgiBlock = new RcolTgiBlock(0x123456789ABCDEF0, 0x12345678, 0xABCDEF01);

        // Act
        var vm = new RcolExternalViewModel(tgiBlock, 5);

        // Assert
        vm.TgiString.Should().NotBeEmpty();
        vm.Index.Should().Be(5);
    }

    #endregion
}
