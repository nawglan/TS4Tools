using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests the <see cref="IResourceEditorViewModel.ContentChanged"/> signal that drives Commit enablement
/// (legacy_references/Sims4Tools/s4pe/MainForm.cs lines 700-705, resource_ResourceChanged).
/// </summary>
public class EditorChangeSignalTests
{
    [Fact]
    public void StblEditor_EditingValue_RaisesContentChanged()
    {
        var resource = new StblResource(new ResourceKey(0x220557DA, 0, 0), ReadOnlyMemory<byte>.Empty);
        resource.Add(1, "a");
        using var vm = new StblEditorViewModel();
        vm.LoadResource(resource);
        var count = 0;
        vm.ContentChanged += (_, _) => count++;

        vm.Entries[0].Value = "b";

        count.Should().Be(1);
    }

    [Fact]
    public void StblEditor_EditingNewlyAddedEntry_WritesThroughToResource()
    {
        var resource = new StblResource(new ResourceKey(0x220557DA, 0, 0), ReadOnlyMemory<byte>.Empty);
        using var vm = new StblEditorViewModel();
        vm.LoadResource(resource);
        vm.NewKeyText = "0x00000005";
        vm.NewValueText = "added";
        vm.AddEntryCommand.Execute(null);

        vm.Entries.Single().Value = "edited";

        resource[5].Should().Be("edited");
    }

    [Fact]
    public void StblEditor_Dispose_StopsForwardingResourceChanges()
    {
        var resource = new StblResource(new ResourceKey(0x220557DA, 0, 0), ReadOnlyMemory<byte>.Empty);
        resource.Add(1, "a");
        var vm = new StblEditorViewModel();
        vm.LoadResource(resource);
        var count = 0;
        vm.ContentChanged += (_, _) => count++;
        var entry = vm.Entries[0];

        vm.Dispose();
        entry.Value = "b";
        resource.Add(2, "c");

        count.Should().Be(0);
        resource[1].Should().Be("a", "entry edits are no longer written through after dispose");
    }

    [Fact]
    public void NameMapEditor_Delete_RaisesContentChanged()
    {
        var resource = new NameMapResource(new ResourceKey(0x0166038C, 0, 0), ReadOnlyMemory<byte>.Empty);
        resource.Add(7, "seven");
        using var vm = new NameMapEditorViewModel();
        vm.LoadResource(resource);
        var count = 0;
        vm.ContentChanged += (_, _) => count++;

        vm.SelectedEntry = vm.Entries[0];
        vm.DeleteEntryCommand.Execute(null);

        count.Should().Be(1);
        resource.ContainsKey(7).Should().BeFalse();
    }

    [Fact]
    public void TextEditor_TypingText_RaisesContentChangedAndGetDataIncludesIt()
    {
        var resource = new TextResource(new ResourceKey(0x03B33DDF, 0, 0), ReadOnlyMemory<byte>.Empty) { Text = "old" };
        using var vm = new TextEditorViewModel();
        vm.LoadResource(resource);
        var count = 0;
        vm.ContentChanged += (_, _) => count++;

        vm.Text = "new";

        count.Should().Be(1);
        new TextResource(resource.Key, vm.GetData()).Text.Should().Be("new");
    }

    [Fact]
    public void PropertyGrid_SettingValue_RaisesPropertyValueChanged()
    {
        var resource = new TextResource(new ResourceKey(0x03B33DDF, 0, 0), ReadOnlyMemory<byte>.Empty) { Text = "old" };
        using var vm = new PropertyGridViewModel();
        vm.LoadResource(resource);
        var count = 0;
        vm.PropertyValueChanged += (_, _) => count++;

        vm.Properties.Single(p => p.Name == nameof(TextResource.Text)).Value = "new";

        count.Should().Be(1);
        resource.Text.Should().Be("new");
    }

    [Fact]
    public void PropertyGrid_Dispose_StopsRaisingPropertyValueChanged()
    {
        var resource = new TextResource(new ResourceKey(0x03B33DDF, 0, 0), ReadOnlyMemory<byte>.Empty) { Text = "old" };
        var vm = new PropertyGridViewModel();
        vm.LoadResource(resource);
        var item = vm.Properties.Single(p => p.Name == nameof(TextResource.Text));
        var count = 0;
        vm.PropertyValueChanged += (_, _) => count++;

        vm.Dispose();
        item.Value = "new";

        count.Should().Be(0);
    }
}
