using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// The main window view model reaches files and the main window only through
/// <see cref="IStorageService"/>, and saves settings in the closing flow.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 773-797
/// (FileOpen / FileOpenReadOnly), 985-988 (FileExit calls Close) and 231-247 (MainForm_FormClosing:
/// prompt, cancel if the package stays open, otherwise save the settings).
/// </remarks>
public sealed class StorageAndCloseTests : IDisposable
{
    private static readonly ResourceKey StblKey = new(0x220557DA, 0, 0x1111);

    private readonly List<string> _tempFiles = [];
    private readonly FakeDialogService _dialogs = new();
    private readonly FakeStorageService _storage = new();
    private readonly MockFileSystemService _fileSystem = new();
    private int _settingsSaves;

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    private MainWindowViewModel CreateViewModel() =>
        new(_fileSystem, _dialogs, _ => null)
        {
            Storage = _storage,
            SaveSettingsAsync = () =>
            {
                _settingsSaves++;
                return Task.CompletedTask;
            }
        };

    private async Task<string> WritePackageAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-storage-{Guid.NewGuid():N}.package");
        _tempFiles.Add(path);
        var package = DbpfPackage.CreateNew();
        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(1, "a");
        package.AddResource(StblKey, stbl.Data);
        await using (var stream = File.Create(path))
        {
            await package.SaveToStreamAsync(stream);
        }

        return path;
    }

    [Fact]
    public async Task OpenPackage_LoadsThePathTheStorageServicePicked()
    {
        var path = await WritePackageAsync();
        var vm = CreateViewModel();
        _storage.OpenFiles.Enqueue([path]);

        await vm.OpenPackageCommand.ExecuteAsync(null);

        vm.HasOpenPackage.Should().BeTrue();
        vm.PackagePath.Should().Be(path);
        vm.IsReadOnly.Should().BeFalse();
        _storage.RequestTitles.Should().Equal("Open Package");
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task OpenPackageReadOnly_OpensReadOnly()
    {
        var path = await WritePackageAsync();
        var vm = CreateViewModel();
        _storage.OpenFiles.Enqueue([path]);

        await vm.OpenPackageReadOnlyCommand.ExecuteAsync(null);

        vm.PackagePath.Should().Be(path);
        vm.IsReadOnly.Should().BeTrue();
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task OpenPackage_NoLocalPath_IsTreatedAsCancelled()
    {
        // A pick on non-local storage yields no local paths (TryGetLocalPath returns null).
        var vm = CreateViewModel();
        _storage.OpenFiles.Enqueue([]);

        await vm.OpenPackageCommand.ExecuteAsync(null);

        vm.HasOpenPackage.Should().BeFalse();
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task Exit_AsksTheStorageServiceToCloseTheMainWindow()
    {
        var vm = CreateViewModel();

        vm.ExitCommand.Execute(null);

        _storage.CloseRequests.Should().Be(1);
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task DialogCommands_WithoutMainWindow_DoNothing()
    {
        var vm = CreateViewModel();

        await vm.AboutCommand.ExecuteAsync(null);
        await vm.SettingsCommand.ExecuteAsync(null);
        await vm.OrganiseBookmarksCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().NotBe("Settings updated");
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task PrepareWindowClose_CleanState_StoresThenSavesSettings()
    {
        var vm = CreateViewModel();
        var order = new List<string>();
        vm.SaveSettingsAsync = () =>
        {
            order.Add("save");
            return Task.CompletedTask;
        };

        var canClose = await vm.PrepareWindowCloseAsync(() => order.Add("store"));

        canClose.Should().BeTrue();
        order.Should().Equal("store", "save");
        _dialogs.SavePromptCount.Should().Be(0);
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task PrepareWindowClose_DirtyPackage_Cancel_KeepsWindowOpenAndDoesNotSaveSettings()
    {
        var vm = CreateViewModel();
        await vm.NewPackageCommand.ExecuteAsync(null); // a new package is dirty (legacy FileNew)
        _dialogs.SaveAnswers.Enqueue(SavePromptResult.Cancel);
        var stored = false;

        var canClose = await vm.PrepareWindowCloseAsync(() => stored = true);

        canClose.Should().BeFalse();
        stored.Should().BeFalse();
        _settingsSaves.Should().Be(0);
        vm.HasOpenPackage.Should().BeTrue();
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task PrepareWindowClose_DirtyPackage_DontSave_SavesSettingsAndAllowsClose()
    {
        var vm = CreateViewModel();
        await vm.NewPackageCommand.ExecuteAsync(null);
        _dialogs.SaveAnswers.Enqueue(SavePromptResult.DontSave);

        var canClose = await vm.PrepareWindowCloseAsync();

        canClose.Should().BeTrue();
        _dialogs.SavePromptCount.Should().Be(1);
        _settingsSaves.Should().Be(1);
        await vm.DisposeAsync();
    }
}
