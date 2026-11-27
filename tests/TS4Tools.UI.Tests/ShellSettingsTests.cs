using Avalonia.Controls;
using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for the settings the shell reads and writes: window placement, splitters, column widths,
/// hex row size, preview zoom and the package information panel.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 146-199
/// (window and splitter settings), BrowserWidget/BrowserWidget.cs lines 46-89 (ColumnWidths),
/// MainForm.cs lines 2837-2846 (PreviewZoomFactor) and PackageInfo/PackageInfoWidget.cs.
/// </remarks>
public sealed class ShellSettingsTests
{
    [Fact]
    public void AppSettings_OldFileWithRecentFilesKey_StillLoads()
    {
        // RecentFiles was dropped from AppSettings (the MRU list lives in recent.json); settings
        // files written before that must still load, keeping every other value.
        const string json = "{\"RecentFiles\":[\"/a.package\"],\"MaxRecentFiles\":7,\"LastExportPath\":\"/out\"}";

        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        settings.Should().NotBeNull();
        settings!.MaxRecentFiles.Should().Be(7);
        settings.LastExportPath.Should().Be("/out");
    }

    [Fact]
    public void WindowPlacement_FirstRun_UsesFourFifthsOfTheScreen()
    {
        var result = WindowPlacement.Resolve(new AppSettings(), 2000, 1000);

        result.Width.Should().Be(1600);
        result.Height.Should().Be(800);
        result.X.Should().BeNull();
        result.Maximized.Should().BeFalse();
        result.BrowserWidth.Should().BeNull();
        result.PackageInfoHeight.Should().BeNull();
    }

    [Fact]
    public void WindowPlacement_RestoresSavedSizePositionAndSplitters()
    {
        var settings = new AppSettings();
        WindowPlacement.Capture(settings, 1111, 777, 40, 50, WindowPlacement.Normal, 480, 120);

        var result = WindowPlacement.Resolve(settings, 2000, 1000);

        result.Width.Should().Be(1111);
        result.Height.Should().Be(777);
        result.X.Should().Be(40);
        result.Y.Should().Be(50);
        result.Maximized.Should().BeFalse();
        result.BrowserWidth.Should().Be(480);
        result.PackageInfoHeight.Should().Be(120);
    }

    [Fact]
    public void WindowPlacement_RestoresMaximizedState()
    {
        var settings = new AppSettings();
        WindowPlacement.Capture(settings, 900, 700, 10, 20, WindowPlacement.Maximized, 300, 100);

        WindowPlacement.Resolve(settings, 2000, 1000).Maximized.Should().BeTrue();
    }

    [Fact]
    public void WindowPlacement_MinimizedState_FallsBackToDefaults()
    {
        var settings = new AppSettings();
        WindowPlacement.Capture(settings, 900, 700, 10, 20, WindowPlacement.Minimized, 300, 100);

        var result = WindowPlacement.Resolve(settings, 2000, 1000);

        result.Width.Should().Be(1600);
        result.X.Should().BeNull();
        result.BrowserWidth.Should().BeNull();
    }

    [Fact]
    public void WindowPlacement_NegativeValues_MeanNotSaved()
    {
        var settings = new AppSettings { WindowState = WindowPlacement.Normal, WindowWidth = -1, WindowHeight = -1, Splitter1Position = -1, Splitter2Position = 90 };

        var result = WindowPlacement.Resolve(settings, 1000, 500);

        result.Width.Should().Be(800);
        result.Height.Should().Be(400);
        result.X.Should().BeNull();
        result.BrowserWidth.Should().BeNull();
        result.PackageInfoHeight.Should().Be(90);
    }

    [Fact]
    public void WindowPlacement_SurvivesJsonRoundTrip()
    {
        var settings = new AppSettings();
        WindowPlacement.Capture(settings, 1000.4, 800.6, 5, 6, WindowPlacement.Normal, 410.2, 133.7);

        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!;

        loaded.WindowWidth.Should().Be(1000);
        loaded.WindowHeight.Should().Be(801);
        loaded.Splitter1Position.Should().Be(410);
        loaded.Splitter2Position.Should().Be(134);
    }

    [Fact]
    public void ColumnWidths_RoundTripThroughTheSetting()
    {
        var text = BrowserColumns.FormatWidths([81, 52, 90, 91, 141, 100, 100, 100, 120]);

        text.Should().Be("81:52:90:91:141:100:100:100:120");
        BrowserColumns.ParseWidths(text).Should().Equal(81, 52, 90, 91, 141, 100, 100, 100, 120);
    }

    [Fact]
    public void ColumnWidths_InvalidOrMissingEntries_UseDefaults()
    {
        var saved = BrowserColumns.ParseWidths("0:abc:-5");

        saved.Should().Equal(-1, -1, -1);
        BrowserColumns.WidthFor(saved, 0).Should().Be(160);
        BrowserColumns.WidthFor(saved, 2).Should().Be(125, "hex columns fit 0xXXXXXXXX");
        BrowserColumns.WidthFor(saved, 1).Should().Be(70);
        BrowserColumns.WidthFor(saved, 4).Should().Be(220, "the Instance column is wider by default");
        BrowserColumns.ParseWidths(null).Should().BeEmpty();
        BrowserColumns.ParseWidths("120:70").Should().Equal(120, 70);
        BrowserColumns.WidthFor(BrowserColumns.ParseWidths("120:70"), 0).Should().Be(120);
    }

    [Fact]
    public void ColumnWidths_AreStoredInTheSettingsFile()
    {
        var settings = new AppSettings { ColumnWidths = "10:20" };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);

        System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!.ColumnWidths.Should().Be("10:20");
    }

    [Fact]
    public void HexViewer_UsesTheHexRowSizeSetting()
    {
        var data = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        var vm = new HexViewerViewModel { RowSize = 8 };

        vm.LoadData(data);

        var lines = vm.HexContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        // header + ruler + 5 rows of 8 bytes
        lines.Should().HaveCount(7);
        lines[2].Should().StartWith("00000000  00 01 02 03 04 05 06 07 ");
        lines[3].Should().StartWith("00000008  08 09");
    }

    [Fact]
    public void HexViewer_DefaultRowSizeComesFromSettings_AndInvalidFallsBackTo16()
    {
        HexViewerViewModel.ClampRowSize(0).Should().Be(16);
        HexViewerViewModel.ClampRowSize(-4).Should().Be(16);
        HexViewerViewModel.ClampRowSize(32).Should().Be(32);
        HexViewerViewModel.ClampRowSize(100000).Should().Be(256);

        var vm = new HexViewerViewModel();
        vm.RowSize.Should().Be(HexViewerViewModel.ClampRowSize(SettingsService.Instance.Settings.HexRowSize));
    }

    [Fact]
    public void PreviewZoom_IsReadFromSettingsAndStoredWhenChanged()
    {
        var settings = new AppSettings { PreviewZoomFactor = 1.5f };
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null) { ShellSettings = settings };

        vm.PreviewZoom.Should().Be(1.5);

        vm.ZoomPreview(2);

        vm.PreviewZoom.Should().Be(3);
        settings.PreviewZoomFactor.Should().Be(3f);
    }

    [Fact]
    public void PreviewZoom_IsLimited()
    {
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null) { ShellSettings = new AppSettings() };

        for (var i = 0; i < 20; i++) vm.ZoomPreview(10);
        vm.PreviewZoom.Should().Be(MainWindowViewModel.MaxPreviewZoom);

        for (var i = 0; i < 40; i++) vm.ZoomPreview(0.1);
        vm.PreviewZoom.Should().Be(MainWindowViewModel.MinPreviewZoom);
    }

    [Fact]
    public void PreviewZoom_NonPositiveSetting_FallsBackToNormal()
    {
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null)
        {
            ShellSettings = new AppSettings { PreviewZoomFactor = 0 }
        };

        vm.PreviewZoom.Should().Be(1.0);
    }

    [Fact]
    public async Task PreviewZoom_AppliesOnlyToTextAndHexPreviews()
    {
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => new Border())
        {
            ShellSettings = new AppSettings { PreviewZoomFactor = 2f }
        };
        vm.EffectivePreviewZoom.Should().Be(1.0, "nothing is previewed");

        await vm.AttachPackageAsync(DbpfPackage.CreateNew(), "");
        vm.EditorContent = new Border();
        vm.EffectivePreviewZoom.Should().Be(1.0, "other previews are not zoomed");
    }

    [Fact]
    public async Task PackageInfo_ShowsHeaderFieldsOfTheOpenPackage()
    {
        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null) { ShellSettings = new AppSettings() };
        vm.PackageInfo.Rows.Should().OnlyContain(r => r.Value.Length == 0);
        vm.PackageInfo.Rows.Select(r => r.Field).Should().Equal(
            "Magic", "Major", "Minor", "UserVersionMajor", "UserVersionMinor", "Unused1", "CreationTime", "UpdatedTime",
            "Unused2", "Indexcount", "IndexRecordPositionLow", "Indexsize", "Unused3", "Unused4", "Indexposition",
            "Unused5", "Indextype");

        var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(0x220557DA, 0, 1), new byte[] { 1, 2, 3 });
        await vm.AttachPackageAsync(package, "");

        var rows = vm.PackageInfo.Rows.ToDictionary(r => r.Field, r => r.Value);
        rows["Magic"].Should().Be("DBPF");
        rows["Major"].Should().Be($"0x{package.MajorVersion:X8}");
        rows["Minor"].Should().Be($"0x{package.MinorVersion:X8}");
        rows["Indextype"].Should().Be($"0x{package.IndexType:X8}");
        rows["Unused5"].Should().HaveLength(56);
        rows["CreationTime"].Should().StartWith("0x").And.HaveLength(10);

        await vm.ClosePackageCommand.ExecuteAsync(null);
        vm.PackageInfo.Rows.Should().OnlyContain(r => r.Value.Length == 0);
    }

    [Fact]
    public void AutoCheckForUpdates_IsAPersistedSetting()
    {
        var settings = new AppSettings { AutoCheckForUpdates = false };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);

        System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!.AutoCheckForUpdates.Should().BeFalse();
        new AppSettings().AutoDisplayMode.Should().Be(2, "legacy defaults Auto to Preview");
    }
}
