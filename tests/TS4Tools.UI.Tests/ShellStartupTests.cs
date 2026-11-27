using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for command line handling, the start up update check and the Help menu.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 401-545 (command line),
/// lines 201-226 (MainForm_Shown opens the package), lines 2455-2565 (Help menu) and
/// Settings/UpdateChecker.cs lines 68-80.
/// </remarks>
public sealed class ShellStartupTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    private string WritePackage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-shell-{Guid.NewGuid():N}.package");
        _tempFiles.Add(path);
        var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(0x220557DA, 0, 1), new byte[] { 1, 2, 3 });
        package.SaveAsAsync(path).AsTask().GetAwaiter().GetResult();
        package.Dispose();
        return path;
    }

    private static MainWindowViewModel CreateViewModel(AppSettings? settings = null)
        => new(new MockFileSystemService(), new FakeDialogService(), _ => null)
        {
            ShellSettings = settings ?? new AppSettings(),
            StartupUpdateCheck = _ => Task.FromResult<UpdateCheckResult?>(null)
        };

    [Fact]
    public void CommandLine_PackageArgument_IsTheFileToOpen()
    {
        var options = CommandLineOptions.Parse(["/tmp/a.package"], _ => true);

        options.PackagePath.Should().Be("/tmp/a.package", "an absolute POSIX path is not an option");
        options.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CommandLine_MissingFile_IsAnError()
    {
        var options = CommandLineOptions.Parse(["missing.package"], _ => false);

        options.PackagePath.Should().BeNull();
        options.Errors.Should().ContainSingle().Which.Should().Contain("File not found").And.Contain("missing.package");
    }

    [Fact]
    public void CommandLine_SecondPackage_IsAnError()
    {
        var options = CommandLineOptions.Parse(["a.package", "b.package"], _ => true);

        options.PackagePath.Should().Be("a.package");
        options.Errors.Should().ContainSingle().Which.Should().Be("Can only accept one package on command line");
    }

    [Fact]
    public void CommandLine_Options_AreRecognised_WithDashOrSlash()
    {
        var options = CommandLineOptions.Parse(["-test", "/help", "-HELP"], _ => false);

        options.Test.Should().BeTrue();
        options.ShowHelp.Should().BeTrue();
        options.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CommandLine_UnknownOption_IsAnError()
    {
        var options = CommandLineOptions.Parse(["-frobnicate"], _ => true);

        options.Errors.Should().ContainSingle().Which.Should().Be("Invalid command line option: 'frobnicate'");
    }

    [Fact]
    public void CommandLine_Import_CollectsFilesUpToTheNextOption()
    {
        var options = CommandLineOptions.Parse(["-import", "a.bin", "b.bin", "-test"], _ => true);

        options.ImportFiles.Should().Equal("a.bin", "b.bin");
        options.Test.Should().BeTrue();
        options.PackagePath.Should().BeNull();
    }

    [Fact]
    public void CommandLine_ImportWithoutFiles_IsAnError()
    {
        CommandLineOptions.Parse(["-import"], _ => true).Errors.Should().ContainSingle()
            .Which.Should().Be("-import requires one or more files");
    }

    [Fact]
    public async Task Startup_OpensThePackageFromTheCommandLine()
    {
        var path = WritePackage();
        var vm = CreateViewModel();

        await vm.RunStartupAsync([path]);

        vm.HasOpenPackage.Should().BeTrue();
        vm.PackagePath.Should().Be(path);
        vm.Resources.Should().HaveCount(1);
        vm.PackageInfo.Rows.Should().Contain(r => r.Field == "Magic" && r.Value == "DBPF");
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task Startup_WithoutArguments_OpensNothing()
    {
        var vm = CreateViewModel();

        await vm.RunStartupAsync([]);

        vm.HasOpenPackage.Should().BeFalse();
    }

    [Fact]
    public async Task Startup_MissingFile_ReportsTheProblemAndOpensNothing()
    {
        var vm = CreateViewModel();

        await vm.RunStartupAsync([Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid().ToString("N") + ".package")]);

        vm.HasOpenPackage.Should().BeFalse();
        vm.StatusMessage.Should().Contain("File not found");
    }

    private static UpdateCheckResult NewVersion() => new()
    {
        Success = true, UpdateAvailable = true, LatestVersion = "9.9.9", DownloadUrl = "https://example.test/download"
    };

    [Fact]
    public async Task Startup_ChecksForUpdates_WhenEnabled_AndAsksToVisitTheDownloadSite()
    {
        var calls = 0;
        var ops = new FakeResourceOpsDialogService { ChoiceAnswer = 0 };
        var opened = new List<string>();
        var vm = CreateViewModel(new AppSettings { AutoCheckForUpdates = true });
        vm.ResourceOpsDialogs = ops;
        vm.OpenExternal = opened.Add;
        vm.StartupUpdateCheck = _ =>
        {
            calls++;
            return Task.FromResult<UpdateCheckResult?>(NewVersion());
        };

        await vm.RunStartupAsync([]);

        // Source: UpdateChecker.cs lines 105-114
        calls.Should().Be(1);
        vm.StatusMessage.Should().Contain("9.9.9");
        ops.Messages.Should().ContainSingle().Which.Should().Be(MainWindowViewModel.UpdateAvailableText);
        opened.Should().Equal("https://example.test/download");
    }

    [Fact]
    public async Task Startup_UpdateAvailable_No_DoesNotOpenTheDownloadSite()
    {
        var ops = new FakeResourceOpsDialogService { ChoiceAnswer = 1 };
        var opened = new List<string>();
        var vm = CreateViewModel(new AppSettings { AutoCheckForUpdates = true });
        vm.ResourceOpsDialogs = ops;
        vm.OpenExternal = opened.Add;
        vm.StartupUpdateCheck = _ => Task.FromResult<UpdateCheckResult?>(NewVersion());

        await vm.RunStartupAsync([]);

        ops.Messages.Should().ContainSingle();
        opened.Should().BeEmpty();
    }

    [Fact]
    public async Task Startup_UpToDateOrNotDue_ShowsNoDialog()
    {
        var ops = new FakeResourceOpsDialogService();
        var vm = CreateViewModel(new AppSettings { AutoCheckForUpdates = true });
        vm.ResourceOpsDialogs = ops;
        vm.StartupUpdateCheck = _ => Task.FromResult<UpdateCheckResult?>(new UpdateCheckResult { Success = true });

        await vm.RunStartupAsync([]);

        ops.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Startup_FailedCheck_ReportsTheProblem()
    {
        var ops = new FakeResourceOpsDialogService();
        var vm = CreateViewModel(new AppSettings { AutoCheckForUpdates = true });
        vm.ResourceOpsDialogs = ops;
        vm.StartupUpdateCheck = _ => Task.FromResult<UpdateCheckResult?>(new UpdateCheckResult { Success = false, ErrorMessage = "offline" });

        await vm.RunStartupAsync([]);

        // Source: UpdateChecker.cs lines 117-127
        ops.Messages.Should().ContainSingle().Which.Should().Contain("will try again later").And.Contain("offline");
    }

    [Fact]
    public async Task Startup_DoesNotCheckForUpdates_WhenDisabled()
    {
        var calls = 0;
        var vm = CreateViewModel(new AppSettings { AutoCheckForUpdates = false });
        vm.StartupUpdateCheck = _ =>
        {
            calls++;
            return Task.FromResult<UpdateCheckResult?>(null);
        };

        await vm.RunStartupAsync([]);

        calls.Should().Be(0);
    }

    [Fact]
    public async Task Startup_FailingUpdateCheck_DoesNotThrow()
    {
        var vm = CreateViewModel();
        vm.StartupUpdateCheck = _ => throw new InvalidOperationException("offline");

        await vm.Invoking(v => v.RunStartupAsync([])).Should().NotThrowAsync();
    }

    [Fact]
    public void HelpContents_UsesBundledFilesForTheCulture_ElseTheOnlineManual()
    {
        var baseDir = Path.Combine("app");
        var folders = new HashSet<string>
        {
            Path.Combine(baseDir, "HelpFiles"),
            Path.Combine(baseDir, "HelpFiles", "de")
        };
        var files = new HashSet<string> { Path.Combine(baseDir, "HelpFiles", "de", "Contents.htm") };

        MainWindowViewModel.ResolveHelpContents(baseDir, "de-DE", folders.Contains, files.Contains)
            .Should().Be(Path.Combine(baseDir, "HelpFiles", "de", "Contents.htm"));
        MainWindowViewModel.ResolveHelpContents(baseDir, "fr-FR", folders.Contains, files.Contains)
            .Should().Be(MainWindowViewModel.HelpUrl, "no Contents.htm in the base folder");
        MainWindowViewModel.ResolveHelpContents(baseDir, "en-US", _ => false, _ => false)
            .Should().Be(MainWindowViewModel.HelpUrl);
    }

    [Fact]
    public void HelpContentsCommand_OpensTheHelp()
    {
        var vm = CreateViewModel();
        string? opened = null;
        vm.OpenExternal = target => opened = target;

        vm.HelpContentsCommand.Execute(null);

        opened.Should().NotBeNull();
        (opened == MainWindowViewModel.HelpUrl || opened!.EndsWith("Contents.htm", StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public void HelpTexts_MatchLegacy()
    {
        MainWindowViewModel.WarrantyText.Should().Contain("Disclaimer of Warranty.")
            .And.Contain("THERE IS NO WARRANTY FOR THE PROGRAM")
            .And.Contain("Limitation of Liability.");
        MainWindowViewModel.LicenceText.Should().Contain("GNU General Public Licence version 3");
        MainWindowViewModel.LicenceUrl.Should().Be("https://www.gnu.org/licenses/gpl-3.0.html");
        CommandLineOptions.HelpText.Should().Contain("import").And.Contain("test").And.Contain("help");
    }
}
