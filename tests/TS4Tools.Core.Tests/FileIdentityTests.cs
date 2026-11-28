using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// FileIdentity.Compare decides whether "save as" targets the open package. A string comparison (legacy s4pe
/// MainForm.cs FileSaveAs) misses links and gets case folding wrong on case-sensitive file systems.
/// </summary>
public sealed class FileIdentityTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ts4tools-fileid-" + Guid.NewGuid().ToString("N"));

    public FileIdentityTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch (IOException) { /* best effort */ }
    }

    private string CreateFile(string name, string content = "x")
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void IdenticalPaths_AreSame()
    {
        string path = CreateFile("a.package");

        FileIdentity.Compare(path, path).Should().Be(FileMatch.Same);
    }

    [Fact]
    public void RelativeAndAbsolutePaths_AreSame()
    {
        string path = CreateFile("a.package");
        FileIdentity.Compare(Path.Combine(_dir, ".", "a.package"), path).Should().Be(FileMatch.Same);
        FileIdentity.Compare(Path.Combine(_dir, "sub", "..", "a.package"), path).Should().Be(FileMatch.Same);

        // A path relative to the working directory exists only when both are on the same volume (on Windows CI the
        // checkout is on D: and the temp folder on C:, where GetRelativePath returns the absolute path).
        string relative = Path.GetRelativePath(Environment.CurrentDirectory, path);
        if (!Path.IsPathRooted(relative))
            FileIdentity.Compare(relative, path).Should().Be(FileMatch.Same);
    }

    [Fact]
    public void DifferentFiles_AreNotSame()
    {
        string a = CreateFile("a.package");
        string b = CreateFile("b.package");

        FileIdentity.Compare(a, b).Should().Be(FileMatch.Different);
    }

    [SkippableFact]
    public void SymbolicLinkToFile_IsSame()
    {
        string target = CreateFile("target.package");
        string link = Path.Combine(_dir, "link.package");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Skip.If(true, $"symbolic links not supported here: {ex.Message}");
        }

        FileIdentity.Compare(link, target).Should().Be(FileMatch.Same);
        FileIdentity.Compare(target, link).Should().Be(FileMatch.Same);
    }

    [SkippableFact]
    public void CaseSensitiveFileSystem_FilesDifferingOnlyByCase_AreNotSame()
    {
        string lower = CreateFile("case.package", "lower");
        string upper = Path.Combine(_dir, "CASE.package");
        File.WriteAllText(upper, "upper");
        // On a case-insensitive file system the second write replaced the first file's content.
        Skip.If(File.ReadAllText(lower) == "upper", "temp directory is on a case-insensitive file system");

        FileIdentity.Compare(lower, upper).Should().Be(FileMatch.Different);
    }

    [SkippableFact]
    public void CaseInsensitiveFileSystem_PathsDifferingOnlyByCase_AreSame()
    {
        string lower = CreateFile("fold.package", "lower");
        string upper = Path.Combine(_dir, "FOLD.package");
        Skip.IfNot(File.Exists(upper), "temp directory is on a case-sensitive file system");

        FileIdentity.Compare(lower, upper).Should().Be(FileMatch.Same);
    }

    [Fact]
    public void MissingFilesDifferingOnlyByCase_AreNotSame()
    {
        // Neither exists, so they cannot be shown to be one file.
        FileIdentity.Compare(Path.Combine(_dir, "none.package"), Path.Combine(_dir, "NONE.package"))
            .Should().Be(FileMatch.Different);
    }

    [SkippableFact]
    public void CaseInsensitiveFileSystem_FolderDifferingOnlyByCase_IsSame()
    {
        // Same file name, folder spelled differently: the case-folding decision must look at the folder component.
        string folder = Path.Combine(_dir, "Mods");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "a.package");
        File.WriteAllText(path, "x");
        string otherSpelling = Path.Combine(_dir, "mods", "a.package");
        Skip.IfNot(File.Exists(otherSpelling), "temp directory is on a case-sensitive file system");

        FileIdentity.Compare(otherSpelling, path).Should().Be(FileMatch.Same);
    }

    [SkippableFact]
    public void CaseSensitiveFileSystem_FoldersDifferingOnlyByCase_AreNotSame()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "Mods"));
        string upper = Path.Combine(_dir, "Mods", "a.package");
        File.WriteAllText(upper, "upper");
        Directory.CreateDirectory(Path.Combine(_dir, "mods"));
        string lower = Path.Combine(_dir, "mods", "a.package");
        Skip.If(File.Exists(lower) && File.ReadAllText(lower) == "upper",
            "temp directory is on a case-insensitive file system");
        File.WriteAllText(lower, "lower");

        FileIdentity.Compare(lower, upper).Should().Be(FileMatch.Different);
    }

    [SkippableFact]
    public void CaseInsensitiveFileSystem_FolderAndFileDifferingOnlyByCase_AreSame()
    {
        string folder = Path.Combine(_dir, "Mods");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "A.package");
        File.WriteAllText(path, "x");
        string otherSpelling = Path.Combine(_dir, "mods", "a.package");
        Skip.IfNot(File.Exists(otherSpelling), "temp directory is on a case-sensitive file system");

        FileIdentity.Compare(otherSpelling, path).Should().Be(FileMatch.Same);
    }

    [SkippableFact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public void UnlistableFolder_CaseOnlyDifference_IsUnknown()
    {
        Skip.If(OperatingSystem.IsWindows(), "folder permissions are set with Unix file modes");
        string folder = Path.Combine(_dir, "locked");
        Directory.CreateDirectory(folder);
        string upper = Path.Combine(folder, "A.package");
        string lower = Path.Combine(folder, "a.package");
        File.WriteAllText(upper, "upper");
        File.WriteAllText(lower, "lower");
        Skip.If(File.ReadAllText(upper) == "lower", "temp directory is on a case-insensitive file system");

        // Search (execute) but no read permission: files can be opened, the folder cannot be listed.
        File.SetUnixFileMode(folder, UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            bool canList;
            try { _ = Directory.EnumerateFileSystemEntries(folder).Any(); canList = true; }
            catch (UnauthorizedAccessException) { canList = false; }
            Skip.If(canList, "the folder can still be listed (running as root?)");

            FileIdentity.Compare(upper, lower).Should().Be(FileMatch.Unknown);
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
