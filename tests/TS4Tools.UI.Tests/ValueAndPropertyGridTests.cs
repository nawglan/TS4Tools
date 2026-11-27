using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Controls;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// The generic Value dump and the nested/list property grid, including commit through to the package.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 290-434
/// (ValueBuilder format), legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs (expandable
/// fields, enum/flags/TGI editing) and NewGridForm.cs (list add/insert/copy/delete), MainForm.cs lines 3060-3073 (commit).
/// </remarks>
public sealed class ValueAndPropertyGridTests
{
    private static readonly ResourceKey CobjKey = new(0x319E4F1D, 0, 0x4444);

    private static CatalogTag KnownTag => CatalogTagRegistry.AllTags().First(t => t.Index != 0);

    private static CobjResource CreateCobj()
    {
        var cobj = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);
        cobj.CommonBlock.Price = 123;
        cobj.CommonBlock.Tags.Add(KnownTag.Index);
        cobj.CommonBlock.ProductStyles.Add(new TgiReference(0x1122334455667788, 0x00B2D882, 0));
        return cobj;
    }

    #region ValueBuilder

    [Fact]
    public void ValueBuilder_DumpsNestedObjectsAndListsInLegacyFormat()
    {
        var text = ValueBuilder.Build(CreateCobj());

        text.Should().Contain("Version: 0x");
        text.Should().Contain("--- CatalogCommon: CommonBlock ---");
        text.Should().Contain("   Price: 0x0000007B");
        text.Should().Contain("--- CatalogTagList: Tags (0x1) ---");
        text.Should().Contain($"[0]: 0x{KnownTag.Index:X8} ({KnownTag.Value})", "catalog tags are named");
        text.Should().Contain("[0]: 0x00B2D882-0x00000000-0x1122334455667788", "TGI values use the T-G-I form");
        text.Should().NotContain("Data:");
    }

    [Fact]
    public void ValueBuilder_BoundsHugeLists()
    {
        var holder = new { Items = Enumerable.Range(0, ValueBuilder.MaxListElements + 10).ToArray() };
        var text = ValueBuilder.Build(holder);
        text.Should().Contain("(10 more)");
    }

    [Fact]
    public void ValueViewer_ShowsWrapperValue()
    {
        var vm = new ValueViewerViewModel();
        vm.LoadWrapper(CreateCobj());
        vm.Kind.Should().Be(ValueViewKind.Value);
        vm.Text.Should().Contain("CommonBlock");
        vm.CanRefresh.Should().BeTrue();
    }

    #endregion

    #region Property grid

    private static PropertyItem Child(PropertyItem item, string name) => item.Children.Single(c => c.Name == name);

    [Fact]
    public void Grid_NestedObject_ExpandsAndEditsInPlace()
    {
        var cobj = CreateCobj();
        using var grid = new PropertyGridViewModel();
        grid.LoadResource(cobj);
        var changes = 0;
        grid.PropertyValueChanged += (_, _) => changes++;

        var common = grid.Properties.Single(p => p.Name == nameof(AbstractCatalogResource.CommonBlock));
        common.HasChildren.Should().BeTrue();
        Child(common, nameof(CatalogCommon.Price)).Value = "0x200";

        cobj.CommonBlock.Price.Should().Be(0x200);
        changes.Should().Be(1, "edits in nested rows bubble to the top-level row");
    }

    [Fact]
    public void Grid_CustomList_AddEditRemove()
    {
        var cobj = CreateCobj();
        using var grid = new PropertyGridViewModel();
        grid.LoadResource(cobj);
        var tags = Child(grid.Properties.Single(p => p.Name == "CommonBlock"), "Tags");

        tags.IsCollection.Should().BeTrue();
        tags.Children.Should().ContainSingle().Which.Value.Should().Contain(KnownTag.Value);
        tags.CanAddItem.Should().BeTrue();

        tags.AddItem();
        cobj.CommonBlock.Tags.Count.Should().Be(2);
        tags.Children.Should().HaveCount(2);

        tags.Children[1].Value = "0x00000042";
        cobj.CommonBlock.Tags[1].Should().Be(0x42u);

        tags.Children[0].CanRemove.Should().BeTrue();
        tags.Children[0].RemoveItem();
        cobj.CommonBlock.Tags.Count.Should().Be(1);
        cobj.CommonBlock.Tags[0].Should().Be(0x42u);
    }

    [Fact]
    public void Grid_StructElements_EditByTgiStringAndFields()
    {
        var cobj = CreateCobj();
        using var grid = new PropertyGridViewModel();
        grid.LoadResource(cobj);
        var styles = Child(grid.Properties.Single(p => p.Name == "CommonBlock"), "ProductStyles");

        styles.Children[0].Value = "0x01234567-0x00000001-0x00000000000000FF";
        cobj.CommonBlock.ProductStyles[0].Should().Be(new TgiReference(0xFF, 0x01234567, 1));

        // Editing a field of the (record struct) element writes the element back into the list.
        Child(styles.Children[0], nameof(TgiReference.Group)).Value = "0x00000009";
        cobj.CommonBlock.ProductStyles[0].Group.Should().Be(9u);

        styles.Children[0].DuplicateItem();
        cobj.CommonBlock.ProductStyles.Count.Should().Be(2);
        cobj.CommonBlock.ProductStyles[1].Should().Be(cobj.CommonBlock.ProductStyles[0]);
    }

    [Fact]
    public void Grid_EnumAndArrays()
    {
        var holder = new GridHolder();
        var enumItem = new PropertyItem(holder, typeof(GridHolder).GetProperty(nameof(GridHolder.Mode))!);
        enumItem.EditorKind.Should().Be(PropertyEditorKind.Enum);
        enumItem.EnumNames.Should().Contain(nameof(SeekOrigin.End));
        enumItem.EnumName = nameof(SeekOrigin.End);
        holder.Mode.Should().Be(SeekOrigin.End);

        var flags = new PropertyItem(holder, typeof(GridHolder).GetProperty(nameof(GridHolder.Attributes))!);
        flags.IsFlags.Should().BeTrue();
        Child(flags, nameof(FileAttributes.Hidden)).BoolValue = true;
        holder.Attributes.Should().HaveFlag(FileAttributes.Hidden);

        var array = new PropertyItem(holder, typeof(GridHolder).GetProperty(nameof(GridHolder.Values))!);
        array.AddItem();
        holder.Values.Should().HaveCount(3, "arrays are resized through the owning property");
        array.Children[0].Value = "7";
        holder.Values[0].Should().Be(7);
        array.Children[2].RemoveItem();
        holder.Values.Should().Equal(7, 2);
    }

    [Fact]
    public void Grid_ReadOnlyListDeclaration_IsNotResizable()
    {
        var holder = new GridHolder();
        var item = new PropertyItem(holder, typeof(GridHolder).GetProperty(nameof(GridHolder.ReadOnlyItems))!);
        item.CanAddItem.Should().BeFalse();
        item.Children.Should().HaveCount(2);
        item.Children[0].CanRemove.Should().BeFalse();
    }

    private sealed class GridHolder
    {
        public SeekOrigin Mode { get; set; } = SeekOrigin.Begin;

        public FileAttributes Attributes { get; set; } = FileAttributes.Normal;

        public int[] Values { get; set; } = [1, 2];

        public IReadOnlyList<string> ReadOnlyItems { get; } = new List<string> { "a", "b" };
    }

    [Fact]
    public async Task Grid_NestedAndListEdits_CommitToPackage()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(CobjKey, CreateCobj().Data);
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null) { ShellSettings = new AppSettings() };
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources.Single(r => r.Key == CobjKey);
        await vm.PendingSelectionTask;

        vm.CurrentEditorViewModel.Should().BeOfType<CatalogViewerViewModel>();
        vm.TogglePropertyGridCommand.Execute(null);
        var common = vm.CurrentPropertyGrid!.Properties.Single(p => p.Name == "CommonBlock");
        Child(common, "Price").Value = "0x00000999";
        var tags = Child(common, "Tags");
        tags.AddItem();
        tags.Children[1].Value = "0x00000055";

        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var data = await package.GetResourceDataAsync(package.Find(CobjKey)!);
        var reread = new CobjResource(CobjKey, data);
        reread.CommonBlock.Price.Should().Be(0x999u);
        reread.CommonBlock.Tags.Count.Should().Be(2);
        reread.CommonBlock.Tags[1].Should().Be(0x55u);
    }

    #endregion
}
