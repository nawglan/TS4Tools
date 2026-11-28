using FluentAssertions;
using TS4Tools.UI.Services;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// SettingsService saves (legacy_references/Sims4Tools/s4pe/Properties/Settings.settings is saved in place by .NET;
/// here a save goes to a temp file that is moved into place). Uses a temp settings path, never the user's file.
/// </summary>
public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ts4tools-settings-" + Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public SettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Directory.Delete(_dir, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best effort */ }
    }

    private string[] TempFiles() => Directory.GetFiles(_dir, "settings.json.tmp*");

    [Fact]
    public async Task OverlappingSaves_AllSucceed_AndLeaveValidJsonAndNoTempFiles()
    {
        var service = SettingsService.CreateForTesting(SettingsPath);
        var failures = new List<Exception>();
        service.SaveFailed += (_, e) => { lock (failures) failures.Add(e); };

        var saves = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            service.Settings.Bookmarks.Add($"bookmark{i}");
            saves.Add(service.SaveAsync());
        }
        await Task.WhenAll(saves);

        failures.Should().BeEmpty("overlapping saves must not collide on a shared temp file");
        TempFiles().Should().BeEmpty();
        var reloaded = SettingsService.CreateForTesting(SettingsPath);
        await reloaded.LoadForTestingAsync();
        reloaded.Settings.Bookmarks.Should().HaveCount(20, "the last save wrote the newest snapshot");
    }

    [Fact]
    public async Task CancelledSave_LeavesNoTempFile_AndKeepsExistingFile()
    {
        var service = SettingsService.CreateForTesting(SettingsPath);
        service.Settings.Bookmarks.Add("kept");
        await service.SaveAsync();
        var before = await File.ReadAllTextAsync(SettingsPath);

        service.Settings.Bookmarks.Add("not saved");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var act = async () => await service.SaveAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        TempFiles().Should().BeEmpty();
        (await File.ReadAllTextAsync(SettingsPath)).Should().Be(before);
    }

    [SkippableFact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public async Task UnwritableFolder_RaisesSaveFailed()
    {
        Skip.If(OperatingSystem.IsWindows(), "folder permissions are set with Unix file modes");
        var service = SettingsService.CreateForTesting(SettingsPath);
        File.SetUnixFileMode(_dir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        bool canWrite;
        try { File.WriteAllText(Path.Combine(_dir, "probe"), "x"); canWrite = true; }
        catch (UnauthorizedAccessException) { canWrite = false; }
        Skip.If(canWrite, "the folder is still writable (running as root?)");

        Exception? failure = null;
        service.SaveFailed += (_, e) => failure = e;
        await service.SaveAsync();

        failure.Should().NotBeNull();
        File.Exists(SettingsPath).Should().BeFalse();
    }

    [Fact]
    public async Task Load_RemovesTempFilesLeftByInterruptedSaves()
    {
        var service = SettingsService.CreateForTesting(SettingsPath);
        service.Settings.Bookmarks.Add("saved");
        await service.SaveAsync();
        string stale = SettingsPath + ".tmp-deadbeef";
        await File.WriteAllTextAsync(stale, "{ partial");
        string unrelated = Path.Combine(_dir, "other.json.tmp-deadbeef");
        await File.WriteAllTextAsync(unrelated, "keep");

        var reloaded = SettingsService.CreateForTesting(SettingsPath);
        await reloaded.LoadForTestingAsync();

        File.Exists(stale).Should().BeFalse();
        File.Exists(unrelated).Should().BeTrue("only this settings file's temp files are removed");
        reloaded.Settings.Bookmarks.Should().Equal("saved");
    }
}
