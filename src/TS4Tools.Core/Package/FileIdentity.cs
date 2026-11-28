// No legacy equivalent: s4pe compared paths as strings (legacy_references/Sims4Tools/s4pe/MainForm.cs FileSaveAs).
// Saving "as" the open file must be detected reliably, because writing a new file over the open one either truncates
// the file being read or, on Linux/macOS, leaves the package attached to the replaced (unlinked) file.

namespace TS4Tools.Package;

/// <summary>
/// Decides whether two paths name the same file.
/// </summary>
public static class FileIdentity
{
    /// <summary>
    /// Returns true when <paramref name="first"/> and <paramref name="second"/> refer to the same file.
    /// </summary>
    /// <remarks>
    /// <para>Symbolic links to files are followed. Paths that differ only in letter case are the same file when the
    /// file system folds case (both names exist, but the folder lists only one of them), whatever the operating
    /// system: case-sensitive macOS/Windows folders and case-insensitive Linux mounts are both handled.</para>
    /// <para>Hard links and symbolic links in parent folders are not detected.</para>
    /// </remarks>
    public static bool IsSameFile(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        string a = Resolve(first);
        string b = Resolve(second);
        if (string.Equals(a, b, StringComparison.Ordinal))
            return true;
        if (!string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return false;

        // Only the letter case differs. On a case-sensitive file system at most one of them may exist, or both exist
        // as two separate folder entries.
        if (!File.Exists(a) || !File.Exists(b))
            return false;

        string? folder = Path.GetDirectoryName(a);
        if (string.IsNullOrEmpty(folder))
            return true;
        string nameA = Path.GetFileName(a);
        string nameB = Path.GetFileName(b);
        bool sawA = false, sawB = false;
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
            {
                string name = Path.GetFileName(entry);
                if (string.Equals(name, nameA, StringComparison.Ordinal)) sawA = true;
                if (string.Equals(name, nameB, StringComparison.Ordinal)) sawB = true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Cannot list the folder: assume the same file, the safe answer for "may I write a new file over it?".
            return true;
        }
        return !(sawA && sawB);
    }

    private static string Resolve(string path)
    {
        string full = Path.GetFullPath(path);
        try
        {
            var info = new FileInfo(full);
            if (info.LinkTarget != null && info.ResolveLinkTarget(returnFinalTarget: true) is { } target)
                return Path.GetFullPath(target.FullName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Broken or unreadable link: compare the link path itself.
        }
        return full;
    }
}
