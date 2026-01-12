using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="TextEditorViewModel"/>.
/// </summary>
public class TextEditorViewModelTests
{
    private static readonly ResourceKey TestKey = new(0x03B33DDF, 0, 0); // _XML type

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new TextEditorViewModel();

        // Assert
        vm.Text.Should().BeEmpty();
        vm.LineCount.Should().Be(0);
        vm.CharacterCount.Should().Be(0);
        vm.IsXml.Should().BeFalse();
        vm.HasBom.Should().BeFalse();
        vm.IsModified.Should().BeFalse();
    }

    [Fact]
    public void LoadResource_PopulatesText()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Hello, World!";

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.Text.Should().Be("Hello, World!");
        vm.IsModified.Should().BeFalse();
    }

    [Fact]
    public void LoadResource_SetsHasBom()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Test";
        resource.HasBom = true;

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.HasBom.Should().BeTrue();
    }

    [Fact]
    public void LoadResource_DetectsXml()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root />";

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.IsXml.Should().BeTrue();
    }

    [Fact]
    public void LoadResource_UpdatesStats()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Line 1\nLine 2\nLine 3";

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.LineCount.Should().Be(3);
        vm.CharacterCount.Should().Be(20);
    }

    [Fact]
    public void TextChange_UpdatesStats()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Initial";
        vm.LoadResource(resource);

        // Act
        vm.Text = "Line 1\nLine 2";

        // Assert
        vm.LineCount.Should().Be(2);
        vm.CharacterCount.Should().Be(13);
    }

    [Fact]
    public void TextChange_SetsIsModified()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Original";
        vm.LoadResource(resource);

        // Act
        vm.Text = "Modified";

        // Assert
        vm.IsModified.Should().BeTrue();
    }

    [Fact]
    public void TextChange_DetectsXmlContent()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Not XML";
        vm.LoadResource(resource);
        vm.IsXml.Should().BeFalse();

        // Act
        vm.Text = "<root>XML content</root>";

        // Assert
        vm.IsXml.Should().BeTrue();
    }

    [Fact]
    public void ApplyChangesCommand_UpdatesResource()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Original";
        vm.LoadResource(resource);

        vm.Text = "Modified";
        vm.HasBom = true;

        // Act
        vm.ApplyChangesCommand.Execute(null);

        // Assert
        resource.Text.Should().Be("Modified");
        resource.HasBom.Should().BeTrue();
        vm.IsModified.Should().BeFalse();
    }

    [Fact]
    public void ApplyChangesCommand_WithNoResource_DoesNothing()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        vm.Text = "Some text";

        // Act - should not throw
        vm.ApplyChangesCommand.Execute(null);

        // Assert
        vm.Text.Should().Be("Some text");
    }

    [Fact]
    public void RevertChangesCommand_RestoresOriginal()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Original";
        resource.HasBom = false;
        vm.LoadResource(resource);

        vm.Text = "Modified";
        vm.HasBom = true;

        // Act
        vm.RevertChangesCommand.Execute(null);

        // Assert
        vm.Text.Should().Be("Original");
        vm.HasBom.Should().BeFalse();
        vm.IsModified.Should().BeFalse();
    }

    [Fact]
    public void RevertChangesCommand_WithNoResource_DoesNothing()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        vm.Text = "Some text";

        // Act - should not throw
        vm.RevertChangesCommand.Execute(null);

        // Assert
        vm.Text.Should().Be("Some text");
    }

    [Fact]
    public void GetData_ReturnsResourceData()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Test content";
        vm.LoadResource(resource);

        // Act
        var data = vm.GetData();

        // Assert
        data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetData_WithNoResource_ReturnsEmpty()
    {
        // Arrange
        var vm = new TextEditorViewModel();

        // Act
        var data = vm.GetData();

        // Assert
        data.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void LineCount_HandlesCrlf()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "Line 1\r\nLine 2\r\nLine 3";
        vm.LoadResource(resource);

        // Assert - CRLF should be normalized to count lines correctly
        vm.LineCount.Should().Be(3);
    }

    [Fact]
    public void LineCount_EmptyText_ReturnsZero()
    {
        // Arrange
        var vm = new TextEditorViewModel();
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "";
        vm.LoadResource(resource);

        // Assert
        vm.LineCount.Should().Be(0);
    }
}
