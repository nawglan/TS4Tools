using FluentAssertions;
using TS4Tools.Compatibility;
using TS4Tools.Package;
using TS4Tools.Resources;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// A legacy-style s4pi wrapper plugin (tests/TS4Tools.TestLegacyPlugin, recompiled against TS4Tools.Compatibility)
/// dropped into the Wrappers folder is used by the UI: Value view, property grid and commit.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-129
/// (plugin handlers are found at start-up and used for their types) and legacy_references/Sims4Tools/s4pe/MainForm.cs
/// lines 2663-2669 (ResourceChanged marks the resource dirty), 2947-2955 (string Value field is the preview) and
/// 3060-3073 (Commit writes the wrapper's data back).
/// </remarks>
public sealed class LegacyPluginBridgeTests : IDisposable
{
    private const uint CounterType = 0x7E570001;
    private const uint StblType = 0x220557DA;
    private static readonly ResourceKey CounterKey = new(CounterType, 0, 0x42);

    private readonly string _pluginFolder = Path.Combine(Path.GetTempPath(), "ts4tools-legacy-plugin-" + Guid.NewGuid().ToString("N"));

    public LegacyPluginBridgeTests()
    {
        var built = Path.Combine(AppContext.BaseDirectory, "LegacyTestPlugin", "TS4Tools.TestLegacyPlugin.dll");
        File.Exists(built).Should().BeTrue("the test plugin is copied next to the tests by the build");
        Directory.CreateDirectory(_pluginFolder);
        File.Copy(built, Path.Combine(_pluginFolder, "TS4Tools.TestLegacyPlugin.dll"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginFolder, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private ResourceHandlerRegistry CreateRegistry(out IReadOnlyList<WrapperLoadError> errors,
        out IReadOnlyDictionary<uint, Type> legacy, params string[] disabled) =>
        WrapperRegistryService.CreateRegistry(disabled, _pluginFolder, out errors, out _, out legacy);

    private static byte[] Counter(uint value) => BitConverter.GetBytes(value);

    [Fact]
    public void PluginFolder_LegacyHandler_IsRegisteredForItsType()
    {
        var registry = CreateRegistry(out var errors, out var legacy);

        var factory = registry.GetFactory(CounterType).Should().BeOfType<LegacyResourceFactory>().Subject;
        factory.WrapperType.FullName.Should().Be("LegacyTestPlugin.CounterResource");
        legacy.Should().ContainKey(CounterType).WhoseValue.Should().BeSameAs(factory.WrapperType);

        // The plugin's AResource is the host's type, so the wrapper really is an s4pi resource here.
        typeof(s4pi.Interfaces.AResource).IsAssignableFrom(factory.WrapperType).Should().BeTrue();

        // STBL stays with the built-in wrapper and "*" does not replace the default; both are reported.
        registry.GetFactory(StblType).Should().NotBeOfType<LegacyResourceFactory>();
        registry.DefaultFactory.Should().NotBeOfType<LegacyResourceFactory>();
        errors.Should().Contain(e => e.Message.Contains("0x220557DA", StringComparison.Ordinal));
        errors.Should().Contain(e => e.Message.Contains("\"*\"", StringComparison.Ordinal));
    }

    [Fact]
    public void LegacyWrapper_ShowsItsValueText()
    {
        var registry = CreateRegistry(out _, out _);

        var selection = ResourceViewerFactory.Create(CounterKey, Counter(0x1234), registry, new ResourceViewerOptions(false, true, false, false));

        selection.Kind.Should().Be(ResourceViewKind.Value);
        selection.Wrapper.Should().BeOfType<LegacyResourceAdapter>();
        var value = selection.EditorViewModel.Should().BeOfType<ValueViewerViewModel>().Subject;
        value.Text.Should().Contain("Counter: 0x00001234");
        value.Title.Should().Be("Value (CounterResource)");
    }

    [Fact]
    public void LegacyWrapper_CanBeDisabledLikeAnyWrapper()
    {
        var entry = ResourceHandlerRegistry.FormatDisabledWrapper(CounterType, typeof(LegacyResourceFactory));

        var registry = CreateRegistry(out _, out _, entry);

        registry.GetFactory(CounterType).Should().BeNull();
    }

    [Fact]
    public void ManageWrappers_ListsTheLegacyWrapperUnderItsOwnName()
    {
        var registry = CreateRegistry(out var errors, out var legacy);
        var service = new WrapperRegistryService(registry, legacy);

        var vm = new ManageWrappersViewModel(service);

        var row = vm.AllWrappers.Single(w => w.ResourceType == CounterType);
        row.WrapperName.Should().Be("CounterResource");
        row.FileName.Should().Be("TS4Tools.TestLegacyPlugin.dll");
        row.FactoryType.Should().Be<LegacyResourceFactory>();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MainWindow_PropertyGridEdit_OnLegacyWrapper_IsCommittedToThePackage()
    {
        var registry = CreateRegistry(out _, out _);
        var package = DbpfPackage.CreateNew();
        package.ResourceHandlers = registry;
        var entry = package.AddResource(CounterKey, Counter(1))!;

        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null)
        {
            ShellSettings = new AppSettings(),
            Storage = new FakeStorageService(),
            SaveSettingsAsync = () => Task.CompletedTask
        };
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources.Single();
        await vm.PendingSelectionTask;

        vm.CurrentResourceWrapper.Should().BeOfType<LegacyResourceAdapter>();
        vm.CurrentEditorViewModel.Should().BeOfType<ValueViewerViewModel>();

        // The grid shows the legacy object's own fields; editing one makes the resource committable.
        vm.TogglePropertyGridCommand.Execute(null);
        vm.CurrentPropertyGrid!.TypeName.Should().Be("CounterResource");
        vm.CurrentPropertyGrid.Properties.Single(p => p.Name == "Counter").Value = "0x00000063";
        vm.IsResourceDirty.Should().BeTrue();

        vm.CommitCommand.Execute(null);

        var data = await package.GetResourceDataAsync(entry);
        data.ToArray().Should().Equal(Counter(99));
        vm.IsPackageDirty.Should().BeTrue();
        await vm.DisposeAsync();
    }
}
