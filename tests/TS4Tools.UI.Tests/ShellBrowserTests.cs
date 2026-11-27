using Avalonia.Controls;
using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for the browser list (columns, sorting), the resource filter and the control panel options.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs
/// (columns lines 584-621, column click sort lines 899-935, filtered list lines 978-1010),
/// BrowserWidget/SortingListviews.cs (Compare lines 61-95), Filter/ResourceFilterWidget.cs and
/// FilterField.cs (QBE, Set, Revise, Paste), ControlPanel/ControlPanel.cs and MainForm.cs lines 2776-2850
/// (Sort, HexOnly, Names, Tags and Auto options).
/// </remarks>
public sealed class ShellBrowserTests
{
    private static readonly ResourceKey StblKey = new(0x220557DA, 0x80000000, 0x00000000000000B2);
    private static readonly ResourceKey NameMapKey = new(0x0166038C, 0, 0x00000000000000A1);
    private static readonly ResourceKey TextKey = new(0x03B33DDF, 0x00000007, 0x00000000000000C3);

    private static DbpfPackage CreatePackage()
    {
        var package = DbpfPackage.CreateNew();

        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(1, "Hello");
        package.AddResource(StblKey, stbl.Data);

        var nameMap = new NameMapResource(NameMapKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x00000000000000C3, "tuning_name");
        package.AddResource(NameMapKey, nameMap.Data);

        var text = new TextResource(TextKey, ReadOnlyMemory<byte>.Empty) { Text = "<I n=\"x\"/>" };
        package.AddResource(TextKey, text.Data);

        return package;
    }

    private static async Task<MainWindowViewModel> CreateAsync(List<object>? created = null, AppSettings? settings = null)
    {
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), model =>
        {
            created?.Add(model);
            return new Border();
        })
        {
            ShellSettings = settings ?? new AppSettings { Sort = true }
        };
        await vm.AttachPackageAsync(CreatePackage(), "");
        return vm;
    }

    private static async Task SelectAsync(MainWindowViewModel vm, ResourceKey key)
    {
        vm.SelectedResource = vm.Resources.Single(r => r.Key == key);
        await vm.PendingSelectionTask;
    }

    private static string[] Instances(MainWindowViewModel vm)
        => [.. vm.FilteredResources.Select(r => r.InstanceText)];

    [Fact]
    public void Columns_AreTheLegacyBrowserColumns()
    {
        BrowserColumns.All.Should().Equal(
            "Name", "Tag", "Type", "Group", "Instance", "Chunkoffset", "Filesize", "Memsize", "Compressed");
        BrowserColumns.FilterFields.Should().Equal("Name", "Tag", "Type", "Group", "Instance", "Compressed");
    }

    [Fact]
    public async Task Items_ShowIndexEntryColumns()
    {
        var vm = await CreateAsync();
        var entry = vm.Resources.First();

        entry.TypeText.Should().Be("0x220557DA");
        entry.GroupText.Should().Be("0x80000000");
        entry.InstanceText.Should().Be("0x00000000000000B2");
        entry.Tag.Should().NotBeNullOrEmpty();
        entry.FileSizeText.Should().StartWith("0x").And.HaveLength(10);
        entry.GetColumnText(BrowserColumns.Compressed).Should().MatchRegex("^0x[0-9A-F]{4}$");
        vm.Resources.Single(r => r.Key == NameMapKey).MemorySize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SortByColumn_FirstClickAscending_SecondClickDescending()
    {
        var vm = await CreateAsync();
        var instance = BrowserColumns.All.ToList().IndexOf(BrowserColumns.Instance);

        vm.SortByColumn(instance);
        Instances(vm).Should().Equal(
            "0x00000000000000A1", "0x00000000000000B2", "0x00000000000000C3");
        vm.SortOrder.Should().Be(BrowserSortOrder.Ascending);

        vm.SortByColumn(instance);
        Instances(vm).Should().Equal(
            "0x00000000000000C3", "0x00000000000000B2", "0x00000000000000A1");
        vm.SortOrder.Should().Be(BrowserSortOrder.Descending);
    }

    [Fact]
    public async Task SortByColumn_OtherColumn_StartsAscendingAgain()
    {
        var vm = await CreateAsync();
        var columns = BrowserColumns.All.ToList();

        vm.SortByColumn(columns.IndexOf(BrowserColumns.Instance));
        vm.SortByColumn(columns.IndexOf(BrowserColumns.Instance));
        vm.SortByColumn(columns.IndexOf(BrowserColumns.Type));

        vm.SortColumn.Should().Be(columns.IndexOf(BrowserColumns.Type));
        vm.SortOrder.Should().Be(BrowserSortOrder.Ascending);
        vm.FilteredResources.Select(r => r.TypeText).Should().Equal("0x0166038C", "0x03B33DDF", "0x220557DA");
    }

    [Fact]
    public async Task SortByColumn_IsCaseInsensitiveText()
    {
        var vm = await CreateAsync();

        // Name column: only the instance with a NameMap name has text; empty names sort first.
        vm.SortByColumn(BrowserColumns.All.ToList().IndexOf(BrowserColumns.Name));

        vm.FilteredResources.Select(r => r.NameText).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SortByColumn_StoresSortStateInSettings()
    {
        var settings = new AppSettings();
        var vm = await CreateAsync(settings: settings);
        var group = BrowserColumns.All.ToList().IndexOf(BrowserColumns.Group);

        vm.SortByColumn(group);
        vm.SortByColumn(group);

        settings.ColumnToSort.Should().Be(group);
        settings.SortOrder.Should().Be((int)BrowserSortOrder.Descending);
    }

    [Fact]
    public async Task SortDisabled_KeepsPackageOrder_AndEnablingSortsAgain()
    {
        var vm = await CreateAsync();
        var packageOrder = vm.Resources.Select(r => r.Key).ToList();
        vm.SortByColumn(BrowserColumns.All.ToList().IndexOf(BrowserColumns.Instance));
        vm.OnSortOptionChanged(false);

        vm.FilteredResources.Select(r => r.Key).Should().Equal(packageOrder);

        vm.OnSortOptionChanged(true);
        Instances(vm).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task LoadShellSettings_RestoresSortAndOptions()
    {
        var settings = new AppSettings
        {
            ColumnToSort = 4,
            SortOrder = (int)BrowserSortOrder.Descending,
            Sort = true,
            HexOnly = true,
            UseNames = false,
            UseTags = false,
            AutoDisplayMode = 1
        };
        var vm = await CreateAsync(settings: settings);

        vm.SortColumn.Should().Be(4);
        vm.SortOrder.Should().Be(BrowserSortOrder.Descending);
        vm.HexOnly.Should().BeTrue();
        vm.UseNames.Should().BeFalse();
        vm.UseTags.Should().BeFalse();
        vm.AutoChoice.Should().Be(1);
        Instances(vm).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Filter_ByTypeExact_ListsOnlyMatchingAndCountsThem()
    {
        var vm = await CreateAsync();
        vm.VisibleCount.Should().Be(3);

        var type = vm.Filter[BrowserColumns.Type];
        type.IsChecked = true;
        type.EntryText = "0x220557DA";
        vm.Filter.IsEnabled = true;
        vm.Filter.Set();

        vm.FilteredResources.Select(r => r.Key).Should().Equal(StblKey);
        vm.VisibleCount.Should().Be(1);
        type.AppliedText.Should().Be("0x220557DA");
    }

    [Fact]
    public async Task Filter_TypeIsExact_ButNameIsSubstring()
    {
        var vm = await CreateAsync();
        vm.Filter.IsEnabled = true;

        vm.Filter[BrowserColumns.Type].IsChecked = true;
        vm.Filter[BrowserColumns.Type].EntryText = "557DA";
        vm.Filter.Set();
        vm.VisibleCount.Should().Be(0, "type filters must match the whole value");

        vm.Filter[BrowserColumns.Type].IsChecked = false;
        vm.Filter[BrowserColumns.Name].IsChecked = true;
        vm.Filter[BrowserColumns.Name].EntryText = "ning_NA";
        vm.Filter.Set();
        vm.FilteredResources.Select(r => r.Key).Should().Equal(TextKey);
    }

    [Fact]
    public async Task Filter_OnTagColumn()
    {
        var vm = await CreateAsync();
        var tag = vm.Resources.Single(r => r.Key == StblKey).Tag;
        vm.Filter.IsEnabled = true;

        vm.Filter[BrowserColumns.Tag].IsChecked = true;
        vm.Filter[BrowserColumns.Tag].EntryText = tag;
        vm.Filter.Set();

        vm.FilteredResources.Should().OnlyContain(r => r.Tag.Contains(tag, StringComparison.OrdinalIgnoreCase));
        vm.FilteredResources.Should().Contain(r => r.Key == StblKey);
    }

    [Fact]
    public async Task Filter_OnGroupAndInstanceAndCompressed()
    {
        var vm = await CreateAsync();
        vm.Filter.IsEnabled = true;

        vm.Filter[BrowserColumns.Group].IsChecked = true;
        vm.Filter[BrowserColumns.Group].EntryText = "0x00000007";
        vm.Filter.Set();
        vm.FilteredResources.Select(r => r.Key).Should().Equal(TextKey);

        vm.Filter[BrowserColumns.Group].IsChecked = false;
        vm.Filter[BrowserColumns.Instance].IsChecked = true;
        vm.Filter[BrowserColumns.Instance].EntryText = "0x00000000000000A1";
        vm.Filter.Set();
        vm.FilteredResources.Select(r => r.Key).Should().Equal(NameMapKey);

        vm.Filter[BrowserColumns.Instance].IsChecked = false;
        vm.Filter[BrowserColumns.Compressed].IsChecked = true;
        vm.Filter[BrowserColumns.Compressed].EntryText = "0xFFFF";
        vm.Filter.Set();
        vm.VisibleCount.Should().Be(0);
    }

    [Fact]
    public async Task Filter_UncheckedField_IsIgnoredOnSet()
    {
        var vm = await CreateAsync();
        vm.Filter.IsEnabled = true;
        vm.Filter[BrowserColumns.Type].EntryText = "0x220557DA";

        vm.Filter.Set();

        vm.VisibleCount.Should().Be(3);
        vm.Filter[BrowserColumns.Type].AppliedText.Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_NotEnabled_DoesNotRestrictList_UntilEnabled()
    {
        var vm = await CreateAsync();
        vm.Filter[BrowserColumns.Type].IsChecked = true;
        vm.Filter[BrowserColumns.Type].EntryText = "0x220557DA";
        vm.Filter.Set();
        vm.VisibleCount.Should().Be(3);

        vm.Filter.IsEnabled = true;
        vm.VisibleCount.Should().Be(1);

        vm.Filter.IsEnabled = false;
        vm.VisibleCount.Should().Be(3);
    }

    [Fact]
    public async Task Filter_ReviseCopiesAppliedBackIntoEntry()
    {
        var vm = await CreateAsync();
        var type = vm.Filter[BrowserColumns.Type];
        type.IsChecked = true;
        type.EntryText = "0x220557DA";
        vm.Filter.Set();

        type.EntryText = "changed";
        vm.Filter.Revise();

        type.EntryText.Should().Be("0x220557DA");
    }

    [Fact]
    public async Task Filter_InvalidRegex_IsReplacedByPreviousValue()
    {
        var vm = await CreateAsync();
        var type = vm.Filter[BrowserColumns.Type];
        type.IsChecked = true;
        type.EntryText = "0x220557DA";
        vm.Filter.Set();

        type.EntryText = "([";
        vm.Filter.Set();

        type.EntryText.Should().Be("0x220557DA");
        type.AppliedText.Should().Be("0x220557DA");
    }

    [Fact]
    public async Task Filter_QueryByExample_FillsFieldsFromSelectedResource()
    {
        var vm = await CreateAsync();
        await SelectAsync(vm, TextKey);

        vm.FilterQbeCommand.Execute(null);

        vm.Filter[BrowserColumns.Type].EntryText.Should().Be("0x03B33DDF");
        vm.Filter[BrowserColumns.Group].EntryText.Should().Be("0x00000007");
        vm.Filter[BrowserColumns.Instance].EntryText.Should().Be("0x00000000000000C3");
        vm.Filter[BrowserColumns.Name].EntryText.Should().Be("tuning_name");
        vm.Filter[BrowserColumns.Tag].EntryText.Should().Be(vm.SelectedResource!.Tag);

        // QBE only fills the entries; ticking and setting applies them.
        foreach (var field in vm.Filter.Fields) field.IsChecked = true;
        vm.Filter.IsEnabled = true;
        vm.FilterSetCommand.Execute(null);
        vm.FilteredResources.Select(r => r.Key).Should().Equal(TextKey);
    }

    [Fact]
    public async Task Filter_QueryByExample_WithoutSelection_DoesNothing()
    {
        var vm = await CreateAsync();

        vm.FilterQbeCommand.Execute(null);

        vm.Filter.Fields.Should().OnlyContain(f => f.EntryText.Length == 0);
    }

    [Fact]
    public async Task Filter_PasteResourceKey_FillsTypeGroupInstance()
    {
        var vm = await CreateAsync();

        vm.Filter.PasteResourceKey(new ResourceKey(0x220557DA, 0x80000000, 0xB2));

        vm.Filter[BrowserColumns.Type].EntryText.Should().Be("0x220557DA");
        vm.Filter[BrowserColumns.Group].EntryText.Should().Be("0x80000000");
        vm.Filter[BrowserColumns.Instance].EntryText.Should().Be("0x00000000000000B2");
        vm.Filter[BrowserColumns.Name].EntryText.Should().BeEmpty();
    }

    [Fact]
    public async Task Rebuild_KeepsTheSelectedResourceThatStillPassesTheFilter()
    {
        var vm = await CreateAsync();
        await SelectAsync(vm, StblKey);
        var selected = vm.SelectedResource;

        vm.SortByColumn(BrowserColumns.All.ToList().IndexOf(BrowserColumns.Instance));
        await vm.PendingSelectionTask;

        vm.SelectedResource.Should().BeSameAs(selected);
        vm.FilteredResources.Should().Contain(selected!);
    }

    [Fact]
    public async Task Rebuild_ClearsSelectionOfResourceFilteredOut()
    {
        var vm = await CreateAsync();
        await SelectAsync(vm, StblKey);

        vm.Filter[BrowserColumns.Type].IsChecked = true;
        vm.Filter[BrowserColumns.Type].EntryText = "0x0166038C";
        vm.Filter.IsEnabled = true;
        vm.Filter.Set();
        await vm.PendingSelectionTask;

        vm.SelectedResource.Should().BeNull();
        vm.SelectedResources.Should().BeEmpty();
    }

    [Fact]
    public async Task Rebuild_KeepsMultiSelection()
    {
        var vm = await CreateAsync();
        var items = vm.Resources.Take(2).ToList();
        foreach (var item in items) vm.SelectedResources.Add(item);

        vm.SortByColumn(BrowserColumns.All.ToList().IndexOf(BrowserColumns.Instance));

        vm.SelectedResources.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task HexOnly_ShowsHexViewerForWrappedResource_AndReloadsSelection()
    {
        var created = new List<object>();
        var vm = await CreateAsync(created);
        await SelectAsync(vm, StblKey);
        vm.CurrentEditorViewModel.Should().BeOfType<StblEditorViewModel>();

        vm.OnHexOnlyOptionChanged(true);
        await vm.PendingSelectionTask;

        vm.CurrentEditorViewModel.Should().BeOfType<HexViewerViewModel>();
        vm.ShellSettings.HexOnly.Should().BeTrue();

        vm.OnHexOnlyOptionChanged(false);
        await vm.PendingSelectionTask;
        vm.CurrentEditorViewModel.Should().BeOfType<StblEditorViewModel>();
    }

    [Fact]
    public async Task UseNamesAndUseTags_AreStoredAndExposedForColumnVisibility()
    {
        var settings = new AppSettings();
        var vm = await CreateAsync(settings: settings);

        vm.OnUseNamesOptionChanged(false);
        vm.OnUseTagsOptionChanged(false);

        vm.UseNames.Should().BeFalse();
        vm.UseTags.Should().BeFalse();
        settings.UseNames.Should().BeFalse();
        settings.UseTags.Should().BeFalse();
        // names stay available for filtering and sorting even when the column is hidden
        vm.Resources.Single(r => r.Key == TextKey).NameText.Should().Be("tuning_name");
    }

    [Fact]
    public async Task AutoOff_ShowsNothing_AutoPreview_ShowsViewer_AutoHex_ShowsHexDump()
    {
        var created = new List<object>();
        var vm = await CreateAsync(created);

        vm.OnAutoModeChanged(0);
        await SelectAsync(vm, TextKey);
        vm.EditorContent.Should().BeNull("auto off shows nothing on selection");
        vm.CurrentEditorViewModel.Should().BeOfType<TextEditorViewModel>();

        vm.OnAutoModeChanged(2);
        await SelectAsync(vm, StblKey);
        vm.EditorContent.Should().NotBeNull();
        created.Last().Should().BeOfType<StblEditorViewModel>();

        vm.OnAutoModeChanged(1);
        await Task.Delay(100);
        created.Clear();
        await SelectAsync(vm, TextKey);
        vm.EditorContent.Should().NotBeNull();
        created.Should().ContainSingle(m => m is HexViewerViewModel);
        vm.ShellSettings.AutoDisplayMode.Should().Be(1);
    }

    [Fact]
    public async Task ChangingAutoMode_UpdatesTheShownPreviewOfTheSelection()
    {
        var vm = await CreateAsync();
        vm.OnAutoModeChanged(2);
        await SelectAsync(vm, TextKey);
        vm.EditorContent.Should().NotBeNull();

        vm.OnAutoModeChanged(0);
        await Task.Delay(100);
        vm.EditorContent.Should().BeNull();

        vm.OnAutoModeChanged(2);
        await Task.Delay(100);
        vm.EditorContent.Should().NotBeNull();
    }
}
