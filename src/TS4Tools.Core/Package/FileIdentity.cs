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

        // Only the letter case differs, in the file name and/or in folder names. On a case-sensitive file system at
        // most one of the two may exist, or the differing names are separate entries of their folder.
        if (!File.Exists(a) || !File.Exists(b))
            return false;
        return SameEntryIgnoringCase(a, b);
    }

    /// <summary>
    /// For two existing paths equal except for letter case, decides from the folder listings whether every component
    /// that differs in case names the same entry (the file system folds case) or two separate entries.
    /// </summary>
    private static bool SameEntryIgnoringCase(string a, string b)
    {
        while (!string.Equals(a, b, StringComparison.Ordinal))
        {
            string? parentA = Path.GetDirectoryName(a);
            string? parentB = Path.GetDirectoryName(b);
            string nameA = Path.GetFileName(a);
            string nameB = Path.GetFileName(b);

            // Roots (e.g. "C:\" vs "c:\") differing only in case are the same volume.
            if (parentA is null || parentB is null || nameA.Length == 0)
                return true;

            if (!string.Equals(nameA, nameB, StringComparison.Ordinal) && BothNamesListed(parentA, nameA, nameB))
                return false;

            a = parentA;
            b = parentB;
        }
        return true;
    }

    private static bool BothNamesListed(string folder, string nameA, string nameB)
    {
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
            // Cannot list the folder: assume one entry, the safe answer for "may I write a new file over it?".
            return false;
        }
        return sawA && sawB;
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
