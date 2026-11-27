namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/FileTable.cs lines 28-230
//   (static class FileTable, abstract class FT, GameContent/DDSImages/Thumbnails)
// Named GameFileTable (not FileTable) because a type may not share its name with its namespace
// (TS4Tools.FileTable). Legacy static state became instance state.

/// <summary>Which vendor package list of each game to use.</summary>
public enum FileTableKind
{
    /// <summary>Primary game content (excludes DDS images and thumbnails).</summary>
    GameContent,

    /// <summary>DDS image packages.</summary>
    DDSImages,

    /// <summary>Thumbnail packages.</summary>
    Thumbnails,
}

/// <summary>
/// A view over the current package, user ("custom") content and vendor (game / EP / SP) content.
/// Lists are ordered: current package, then custom content (if enabled), then vendor content
/// (if enabled; games by descending priority). Packages are opened lazily and read-only; dispose
/// (or <see cref="ResetAsync"/>) closes everything the table opened.
/// </summary>
public sealed class GameFileTable : IAsyncDisposable
{
    private const int MaxCustomContentDepth = 64;
    private static readonly string[] s_ccPatterns = ["*.package"];

    private readonly Dictionary<FileTableKind, List<PathPackageTuple>> _vendor = [];
    private List<PathPackageTuple>? _customContent;

    /// <summary>Creates a file table over the given game definitions.</summary>
    public GameFileTable(GameFolders gameFolders)
    {
        ArgumentNullException.ThrowIfNull(gameFolders);
        GameFolders = gameFolders;
    }

    /// <summary>The game definitions and settings in use.</summary>
    public GameFolders GameFolders { get; }

    /// <summary>
    /// Root folder for user content (e.g. the Mods folder); searched recursively for ".package" files.
    /// Changing it discards the cached custom content list.
    /// </summary>
    public string? CustomContentPath
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _ = DisposeListAsync(Interlocked.Exchange(ref _customContent, null));
        }
    }

    /// <summary>When true, packages in <see cref="CustomContentPath"/> are included in the view.</summary>
    public bool CustomContentEnabled { get; set; }

    /// <summary>When true, vendor packages are included (subject to <see cref="GameFolders.EPsDisabled"/>).</summary>
    public bool FileTableEnabled { get; set; }

    /// <summary>
    /// The current package; when not null it is searched first. The table never disposes it.
    /// </summary>
    public PathPackageTuple? Current { get; set; }

    /// <summary>Packages found under <see cref="CustomContentPath"/>, depth-first (sub-folders before files).</summary>
    public IReadOnlyList<PathPackageTuple> CustomContent => _customContent ??= EnumerateCustomContent(CustomContentPath).ToList();

    /// <summary>Current + custom content + vendor game content.</summary>
    public IReadOnlyList<PathPackageTuple> GameContent => GetList(FileTableKind.GameContent);

    /// <summary>Current + custom content + vendor DDS images.</summary>
    public IReadOnlyList<PathPackageTuple> DDSImages => GetList(FileTableKind.DDSImages);

    /// <summary>Current + custom content + vendor thumbnails.</summary>
    public IReadOnlyList<PathPackageTuple> Thumbnails => GetList(FileTableKind.Thumbnails);

    /// <summary>True if there is any game content in the view.</summary>
    public bool IsOK => GameContent.Count > 0;

    /// <summary>The ordered search list for <paramref name="kind"/>; empty if nothing is enabled.</summary>
    public IReadOnlyList<PathPackageTuple> GetList(FileTableKind kind)
    {
        var res = new List<PathPackageTuple>();
        if (Current is not null) res.Add(Current);
        if (CustomContentEnabled) res.AddRange(CustomContent);
        if (FileTableEnabled) res.AddRange(GetVendorContent(kind));
        return res;
    }

    /// <summary>Compares two lists by package path, in order (legacy: null equals null).</summary>
    public static bool IsEqual(IReadOnlyList<PathPackageTuple>? ppt1, IReadOnlyList<PathPackageTuple>? ppt2)
    {
        if (ppt1 is null || ppt2 is null) return ppt1 is null && ppt2 is null;
        return ppt1.Count == ppt2.Count && ppt1.Zip(ppt2).All(p => p.First.Equals(p.Second));
    }

    /// <summary>
    /// Looks <paramref name="key"/> up through the ordered list for <paramref name="kind"/>, using
    /// <see cref="RK"/> equivalence. Unreadable packages are skipped.
    /// </summary>
    /// <returns>The first match or null.</returns>
    public async ValueTask<SpecificResource?> FindAsync(ResourceKey key, FileTableKind kind = FileTableKind.GameContent, CancellationToken cancellationToken = default)
    {
        var source = kind switch
        {
            FileTableKind.GameContent => SpecificResource.SourceGameContent,
            FileTableKind.DDSImages => SpecificResource.SourceDdsImages,
            _ => SpecificResource.SourceThumbnails,
        };

        foreach (var ppt in GetList(kind))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var package = await ppt.TryGetPackageAsync(cancellationToken).ConfigureAwait(false);
            var rie = package?.Find(e => RK.AreEquivalent(e.Key, key));
            if (rie is not null)
                return new SpecificResource(ppt, rie, source, GameFolders, key);
        }
        return null;
    }

    /// <summary>
    /// Returns the legacy <see cref="SpecificResource.PPSource"/> label for a package in this table.
    /// </summary>
    public string GetSource(PathPackageTuple ppt)
    {
        ArgumentNullException.ThrowIfNull(ppt);
        if (Current is not null && Current.Equals(ppt)) return SpecificResource.SourceCurrent;
        if (CustomContentEnabled && CustomContent.Contains(ppt)) return SpecificResource.SourceCustomContent;
        if (FileTableEnabled)
        {
            if (GetVendorContent(FileTableKind.GameContent).Contains(ppt)) return SpecificResource.SourceGameContent;
            if (GetVendorContent(FileTableKind.DDSImages).Contains(ppt)) return SpecificResource.SourceDdsImages;
            if (GetVendorContent(FileTableKind.Thumbnails).Contains(ppt)) return SpecificResource.SourceThumbnails;
        }
        return SpecificResource.SourceUnknown;
    }

    /// <summary>
    /// Vendor package tags for <paramref name="kind"/>: enabled games by descending priority,
    /// de-duplicated by full path.
    /// </summary>
    public IEnumerable<PackageTag> GetGamePackages(FileTableKind kind) =>
        GameFolders.Games
            .OrderByDescending(g => g.Priority)
            .Where(g => g.Enabled)
            .SelectMany(g => kind switch
            {
                FileTableKind.GameContent => g.GameContent,
                FileTableKind.DDSImages => g.DDSImages,
                _ => g.Thumbnails,
            })
            .Distinct(Game.PackageComparer.Instance);

    private List<PathPackageTuple> GetVendorContent(FileTableKind kind)
    {
        if (!_vendor.TryGetValue(kind, out var list))
        {
            list = GetGamePackages(kind).Where(p => File.Exists(p.Path)).Select(p => new PathPackageTuple(p.Path)).ToList();
            _vendor[kind] = list;
        }
        return list;
    }

    /// <summary>
    /// Clears the table: disposes packages it opened (not <see cref="Current"/>), and resets
    /// <see cref="Current"/>, <see cref="CustomContentEnabled"/> and <see cref="FileTableEnabled"/>.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        Current = null;
        CustomContentEnabled = false;
        FileTableEnabled = false;
        await DisposeListAsync(Interlocked.Exchange(ref _customContent, null)).ConfigureAwait(false);
        foreach (var list in _vendor.Values) await DisposeListAsync(list).ConfigureAwait(false);
        _vendor.Clear();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ResetAsync();

    private static async Task DisposeListAsync(List<PathPackageTuple>? list)
    {
        if (list is null) return;
        foreach (var ppt in list) await ppt.DisposeAsync().ConfigureAwait(false);
    }

    private static IEnumerable<PathPackageTuple> EnumerateCustomContent(string? ccPath, int depth = 0)
    {
        if (string.IsNullOrEmpty(ccPath) || !Directory.Exists(ccPath) || depth > MaxCustomContentDepth) yield break;

        var options = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
        };

        // Depth-first: sub-folders first, then this folder's files (sorted for a deterministic order).
        foreach (var dir in Directory.GetDirectories(ccPath, "*", options).Order(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var ppt in EnumerateCustomContent(dir, depth + 1))
                yield return ppt;
        }

        foreach (var pattern in s_ccPatterns)
        {
            foreach (var path in Directory.GetFiles(ccPath, pattern, options).Order(StringComparer.OrdinalIgnoreCase))
                yield return new PathPackageTuple(path);
        }
    }
}
