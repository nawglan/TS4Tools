namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/GameFolders.cs lines 249-575 (class Game)

/// <summary>
/// One game, expansion pack or stuff pack as described by a &lt;game&gt; element of GameFolders.xml.
/// </summary>
public sealed class Game
{
    private readonly XElement _game;
    private readonly GameFolders _folders;
    private readonly XNamespace _ns;

    private int _rgVersion = -1;
    private int _priority = -1;
    private string? _name;
    private string? _longName;
    private string? _guid;
    private string? _defaultInstallDir;
    private string? _installDir;
    private int? _suppressed;

    internal Game(XElement game, GameFolders folders)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(folders);
        _game = game;
        _folders = folders;
        // Legacy used GetDefaultNamespace(); the element's own namespace is equivalent for parsed documents
        // and also works for documents built in code.
        _ns = _game.Name.Namespace;
    }

    /// <summary>Resource group version for this game; 0 for the base game. Invalid or negative values read as 0.</summary>
    public int RGVersion
    {
        get
        {
            if (_rgVersion == -1)
            {
                var v = 0;
                var attr = _game.Attribute("rgversion");
                if (attr is null || !int.TryParse(attr.Value, out v)) v = 0;
                _rgVersion = v < 0 ? 0 : v;
            }
            return _rgVersion;
        }
    }

    /// <summary>Priority of this game; defaults to <see cref="RGVersion"/>. Higher priority is searched first.</summary>
    public int Priority
    {
        get
        {
            if (_priority == -1)
            {
                var v = 0;
                var attr = _game.Attribute("priority");
                if (attr is null || !int.TryParse(attr.Value, out v)) v = RGVersion;
                _priority = v < 0 ? RGVersion : v;
            }
            return _priority;
        }
    }

    private string GetElement(string name, string defaultValue)
    {
        var xe = _game.Element(_ns + name);
        return xe is not null && xe.Value.Length > 0 ? xe.Value : defaultValue;
    }

    /// <summary>Short name (e.g. "base").</summary>
    public string Name => _name ??= GetElement("Name", "Unk");

    /// <summary>Long display name.</summary>
    public string Longname => _longName ??= GetElement("Longname", "Unknown");

    /// <summary>Windows uninstall registry GUID, or empty.</summary>
    public string UninstallGuid => _guid ??= GetElement("GUID", "");

    /// <summary>
    /// Install location from the Windows registry; null on other platforms, if there is no GUID or no registry entry.
    /// </summary>
    public string? ConfigApplicationPath => UninstallGuid.Length == 0 ? null : _folders.ReadRegistryInstallLocation(UninstallGuid);

    /// <summary>Install directory relative to the root folder, from GameFolders.xml.</summary>
    public string DefaultInstallDir => _defaultInstallDir ??= GetElement("DefaultInstallDir", "/");

    /// <summary>
    /// Default install directory: the registry location if available, otherwise the root folder combined
    /// with <see cref="DefaultInstallDir"/>.
    /// </summary>
    public string InstallDir
    {
        get
        {
            if (_installDir is null)
            {
                var dir = ConfigApplicationPath ?? Path.Combine(_folders.RootFolder, DefaultInstallDir);
                _installDir = Path.GetFullPath(dir);
            }
            return _installDir;
        }
    }

    internal void ResetInstallDir() => _installDir = null;

    /// <summary>The install directory in effect: a user-configured one if valid, else <see cref="InstallDir"/>.</summary>
    public string UserInstallDir => _folders.GetInstallDir(this);

    /// <summary>True if suppressed by default, false if not, null if suppression is not allowed ("not-allowed").</summary>
    public bool? Suppressed
    {
        get
        {
            _suppressed ??= GetElement("Suppressed", "false") switch
            {
                "true" => 1,
                "not-allowed" => -1,
                _ => 0,
            };
            return _suppressed == -1 ? null : _suppressed != 0;
        }
    }

    /// <summary>Whether this game is currently enabled in its <see cref="GameFolders"/>.</summary>
    public bool Enabled => _folders.IsEnabled(this);

    /// <summary>Primary game content packages, highest priority first.</summary>
    public IEnumerable<PackageTag> GameContent => GetOrdered("GameContent");

    /// <summary>DDS image packages, highest priority first.</summary>
    public IEnumerable<PackageTag> DDSImages => GetOrdered("DDSImages");

    /// <summary>Thumbnail packages, highest priority first.</summary>
    public IEnumerable<PackageTag> Thumbnails => GetOrdered("Thumbnails");

    private IEnumerable<PackageTag> GetOrdered(string root) =>
        GetPackages(root).Distinct(PackageComparer.Instance).OrderByDescending(p => p.Priority);

    internal sealed class PackageComparer : IEqualityComparer<PackageTag>
    {
        public static PackageComparer Instance { get; } = new();

        public bool Equals(PackageTag? x, PackageTag? y) =>
            x is not null && y is not null && Path.GetFullPath(x.Path) == Path.GetFullPath(y.Path);

        public int GetHashCode(PackageTag obj) => obj.Path.GetHashCode(StringComparison.Ordinal);
    }

    private IEnumerable<PackageTag> GetPackages(string root)
    {
        var xRoot = _game.Element(_ns + root);
        if (xRoot is null) yield break;

        var installDir = UserInstallDir;
        foreach (var xFolder in xRoot.Elements(_ns + "Folder"))
        {
            var xPath = xFolder.Attribute("path");
            var path = xPath is not null ? Path.Combine(installDir, xPath.Value) : installDir;
            if (Directory.Exists(path))
            {
                foreach (var p in GetPackages(xFolder, path).Where(p => p.Priority >= 0))
                    yield return p;
            }
        }

        if (Directory.Exists(installDir))
        {
            foreach (var p in GetPackages(xRoot, installDir))
                yield return p;
        }
    }

    private IEnumerable<PackageTag> GetPackages(XElement e, string path)
    {
        foreach (var xPackage in e.Elements(_ns + "Package"))
        {
            if (xPackage.Value.Length == 0) continue;
            var full = Path.Combine(path, xPackage.Value);
            if (!File.Exists(full)) continue;

            // Legacy reads the attributes "IsPatch" and "Priority" (case-sensitive) while the shipped XML
            // spells them "isPatch"/"priority"; the legacy lookup therefore never matches. Kept as-is.
            var isPatch = false;
            var xIsPatch = xPackage.Attribute("IsPatch");
            if (xIsPatch is not null) _ = bool.TryParse(xIsPatch.Value, out isPatch);

            var priority = 0;
            var xPriority = xPackage.Attribute("Priority");
            if (xPriority is not null) _ = int.TryParse(xPriority.Value, out priority);

            yield return new PackageTag(full, isPatch, priority);
        }
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}
