namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/GameFolders.cs lines 577-592 (class PackageTag)

/// <summary>
/// A game package location together with its patch flag and priority, as described by GameFolders.xml.
/// </summary>
/// <param name="Path">Full or install-relative path of the package file.</param>
/// <param name="IsPatch">True if the package is a patch (delta) package.</param>
/// <param name="Priority">Priority; higher wins where identical keys exist.</param>
public sealed record PackageTag(string Path, bool IsPatch, int Priority);
