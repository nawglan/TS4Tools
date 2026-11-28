using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// FileIdentity.IsSameFile decides whether "save as" targets the open package. A string comparison (legacy s4pe
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

        FileIdentity.IsSameFile(path, path).Should().BeTrue();
    }

    [Fact]
    public void RelativeAndAbsolutePaths_AreSame()
    {
        string path = CreateFile("a.package");
        string relative = Path.GetRelativePath(Environment.CurrentDirectory, path);
        relative.Should().NotBe(path);

        FileIdentity.IsSameFile(relative, path).Should().BeTrue();
        FileIdentity.IsSameFile(Path.Combine(_dir, ".", "a.package"), path).Should().BeTrue();
    }

    [Fact]
    public void DifferentFiles_AreNotSame()
    {
        string a = CreateFile("a.package");
        string b = CreateFile("b.package");

        FileIdentity.IsSameFile(a, b).Should().BeFalse();
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

        FileIdentity.IsSameFile(link, target).Should().BeTrue();
        FileIdentity.IsSameFile(target, link).Should().BeTrue();
    }

    [SkippableFact]
    public void CaseSensitiveFileSystem_FilesDifferingOnlyByCase_AreNotSame()
    {
        string lower = CreateFile("case.package", "lower");
        string upper = Path.Combine(_dir, "CASE.package");
        File.WriteAllText(upper, "upper");
        // On a case-insensitive file system the second write replaced the first file's content.
        Skip.If(File.ReadAllText(lower) == "upper", "temp directory is on a case-insensitive file system");

        FileIdentity.IsSameFile(lower, upper).Should().BeFalse();
    }

    [SkippableFact]
    public void CaseInsensitiveFileSystem_PathsDifferingOnlyByCase_AreSame()
    {
        string lower = CreateFile("fold.package", "lower");
        string upper = Path.Combine(_dir, "FOLD.package");
        Skip.IfNot(File.Exists(upper), "temp directory is on a case-sensitive file system");

        FileIdentity.IsSameFile(lower, upper).Should().BeTrue();
    }

    [Fact]
    public void MissingFilesDifferingOnlyByCase_AreNotSame()
    {
        // Neither exists, so they cannot be shown to be one file.
        FileIdentity.IsSameFile(Path.Combine(_dir, "none.package"), Path.Combine(_dir, "NONE.package"))
            .Should().BeFalse();
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

        FileIdentity.IsSameFile(otherSpelling, path).Should().BeTrue();
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

        FileIdentity.IsSameFile(lower, upper).Should().BeFalse();
    }
}
