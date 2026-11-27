namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/GameFolders.cs lines 29-247 (static class GameFolders)
// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/GameFoldersForm.cs (settings model: InstallDirs / EPsDisabled)

/// <summary>
/// The XML definition of game folder default locations and the resulting list of known games,
/// plus the user's settings (per-game install directory overrides and disabled games).
/// </summary>
/// <remarks>
/// Unlike legacy this is an instance (no static state) so it can be configured and tested.
/// Cross-platform: the Windows registry is only consulted on Windows; elsewhere install directories
/// come from <see cref="RootFolder"/> or from <see cref="InstallDirs"/> / <see cref="SetInstallDir"/>.
/// </remarks>
public sealed class GameFolders
{
    private const string ResourceName = "TS4Tools.FileTable.GameFolders.xml";
    private static readonly XNamespace s_ns = "http://sims3.drealm.info/terms/gamefolders/1.0";

    private readonly XElement _root;
    private readonly Func<string, string?> _registryReader;
    private string? _rootFolder;
    private List<Game>? _games;
    private Dictionary<Game, string> _installDirs = [];
    private List<Game>? _gamesEnabled;

    /// <summary>Loads the embedded GameFolders.xml.</summary>
    public GameFolders() : this(LoadEmbedded(), null)
    {
    }

    /// <summary>Uses a supplied definition document and optional registry reader (for tests / non-default setups).</summary>
    /// <param name="definition">A GameFolders.xml document.</param>
    /// <param name="registryReader">Maps an uninstall GUID to an install location; null uses the Windows registry when on Windows.</param>
    public GameFolders(XDocument definition, Func<string, string?>? registryReader = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _root = definition.Element(s_ns + "gamefolders")
            ?? throw new InvalidDataException("GameFolders definition has no <gamefolders> root element.");
        _registryReader = registryReader ?? DefaultRegistryReader;
    }

    private static XDocument LoadEmbedded()
    {
        using var stream = typeof(GameFolders).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        return XDocument.Load(stream);
    }

    private static string? DefaultRegistryReader(string guid) =>
        OperatingSystem.IsWindows() ? ReadWindowsRegistry(guid) : null;

    [SupportedOSPlatform("windows")]
    private static string? ReadWindowsRegistry(string guid)
    {
        var wow = Environment.Is64BitProcess ? @"\Wow6432Node" : "";
        var key = "SOFTWARE" + wow + @"\Microsoft\Windows\CurrentVersion\Uninstall\" + guid;
        try
        {
            using var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key);
            return rk?.GetValue("InstallLocation") as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    internal string? ReadRegistryInstallLocation(string guid) => _registryReader(guid);

    /// <summary>
    /// Root folder under which <see cref="Game.DefaultInstallDir"/> values are resolved. Defaults to the
    /// XML entry for this platform/bitness (falling back to the default root folder); may be overridden.
    /// </summary>
    public string RootFolder
    {
        get
        {
            if (_rootFolder is null)
            {
                var vendor = OperatingSystem.IsWindows() ? "Microsoft" : RuntimeInformation.OSDescription;
                if (OperatingSystem.IsMacOS()) vendor = "MacOSX";
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) vendor = "Unix";
                var os = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";

                var root = _root.Elements(s_ns + "rootfolder")
                    .Where(x => x.Attributes("vendor").Any(a => a.Value == vendor))
                    .FirstOrDefault(x => x.Attributes("os").Any(a => a.Value == os))
                    ?? _root.Elements(s_ns + "defaultrootfolder").FirstOrDefault();
                _rootFolder = root is not null ? root.Value : "/";
            }
            return _rootFolder;
        }
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _rootFolder = value;
            // Cached install dirs depend on the root.
            foreach (var g in Games) g.ResetInstallDir();
        }
    }

    /// <summary>All games defined in the XML, in definition order.</summary>
    public IReadOnlyList<Game> Games => _games ??= _root.Elements(s_ns + "game").Select(g => new Game(g, this)).ToList();

    /// <summary>Finds a game by its short name, or null.</summary>
    public Game? ByName(string value) => Games.FirstOrDefault(x => x.Name == value);

    /// <summary>Finds a game by resource group version, or null.</summary>
    public Game? ByRGVersion(int value) => Games.FirstOrDefault(x => x.RGVersion == value);

    /// <summary>
    /// User overrides of install directories in the legacy "Name=Path;Name=Path" form. Entries for unknown games,
    /// non-existent directories or directories equal to the default are dropped when set.
    /// </summary>
    public string InstallDirs
    {
        get => string.Join(';', _installDirs.Select(kvp => kvp.Key.Name + "=" + kvp.Value));
        set
        {
            if (value is null)
            {
                _installDirs = [];
                return;
            }
            var result = new Dictionary<Game, string>();
            foreach (var part in value.Split(';'))
            {
                var xy = part.Split('=');
                if (xy.Length != 2) continue;
                var game = ByName(xy[0]);
                if (game is null || !Directory.Exists(xy[1])) continue;
                if (Path.GetFullPath(xy[1]) == game.InstallDir) continue;
                result[game] = xy[1];
            }
            _installDirs = result;
        }
    }

    /// <summary>Overrides the install directory of <paramref name="game"/>; null or "" clears the override.</summary>
    public void SetInstallDir(Game game, string? path)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (string.IsNullOrEmpty(path)) _installDirs.Remove(game);
        else _installDirs[game] = path;
    }

    /// <summary>The configured install directory of <paramref name="game"/> if it exists, otherwise its default.</summary>
    public string GetInstallDir(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return _installDirs.TryGetValue(game, out var dir) && Directory.Exists(dir) ? dir : game.InstallDir;
    }

    /// <summary>
    /// Semicolon separated names of games that are disabled. Setting enables all games except the listed ones;
    /// games that are not allowed to be suppressed always stay enabled.
    /// </summary>
    public string EPsDisabled
    {
        get
        {
            var enabled = _gamesEnabled;
            return string.Join(';', Games.Where(g => enabled is not null && !enabled.Contains(g)).Select(g => g.Name));
        }
        set
        {
            var split = (value ?? "").Split(';').Distinct().ToArray();
            _gamesEnabled = Games.Where(g => !g.Suppressed.HasValue || !split.Contains(g.Name)).ToList();
        }
    }

    /// <summary>True if <paramref name="game"/> takes part in the file table.</summary>
    public bool IsEnabled(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return !game.Suppressed.HasValue || (_gamesEnabled is not null ? _gamesEnabled.Contains(game) : !game.Suppressed.Value);
    }

    /// <summary>
    /// Default location of the user's Sims 4 Mods folder (Documents/Electronic Arts/The Sims 4/Mods),
    /// which exists on Windows and macOS; null if the Documents folder cannot be determined.
    /// Not a legacy feature: legacy only had a user-chosen custom content path.
    /// </summary>
    public static string? DefaultModsFolder()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify);
        return string.IsNullOrEmpty(docs) ? null : Path.Combine(docs, "Electronic Arts", "The Sims 4", "Mods");
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Games.Count} games");
}
