using TS4Tools.Package;

namespace TS4Tools.Core.Tests.FileTable;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ts4tools-ft-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch (IOException) { /* best effort */ }
    }
}

internal static class FileTableTestSupport
{
    public static async Task<string> WritePackageAsync(string path, params (ResourceKey Key, byte[] Data)[] resources)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        using var pkg = DbpfPackage.CreateNew();
        foreach (var (key, data) in resources) pkg.AddResource(key, data);
        await pkg.SaveAsAsync(path);
        return path;
    }

    public static XDocument Definition(string rootFolder, params (string Name, int Rg, int? Priority, string InstallDir, string Package)[] games)
    {
        XNamespace ns = "http://sims3.drealm.info/terms/gamefolders/1.0";
        var root = new XElement(ns + "gamefolders", new XElement(ns + "defaultrootfolder", rootFolder));
        foreach (var g in games)
        {
            var game = new XElement(ns + "game", new XAttribute("rgversion", g.Rg),
                new XElement(ns + "Name", g.Name),
                new XElement(ns + "GUID", "{" + g.Name + "}"),
                new XElement(ns + "DefaultInstallDir", g.InstallDir),
                new XElement(ns + "Suppressed", g.Rg == 0 ? "not-allowed" : "false"),
                new XElement(ns + "GameContent",
                    new XElement(ns + "Folder", new XAttribute("path", "Data"), new XElement(ns + "Package", "missing-" + g.Name + ".package")),
                    new XElement(ns + "Package", g.Package)),
                new XElement(ns + "DDSImages", new XElement(ns + "Package", "dds-" + g.Package)),
                new XElement(ns + "Thumbnails"));
            if (g.Priority is { } p) game.Add(new XAttribute("priority", p));
            root.Add(game);
        }
        return new XDocument(root);
    }
}
