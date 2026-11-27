// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs (package enumeration semantics used by the corpus)
// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 64-67 (wrapper lookup per resource type)

namespace TS4Tools.Wrappers.Tests.RealData;

/// <summary>
/// Configuration for the real-data validation corpus: which packages to scan and how many resources to sample.
/// </summary>
/// <remarks>
/// <para>All paths are opened read-only. Nothing from the corpus is copied into the repository.</para>
/// <para>Environment variables:</para>
/// <list type="bullet">
/// <item><c>TS4TOOLS_GAME_DIR</c> - Sims 4 install root (default <see cref="DefaultGameDir"/>).</item>
/// <item><c>TS4TOOLS_MODS_DIR</c> - mods folder (default <see cref="DefaultModsDir"/>).</item>
/// <item><c>TS4TOOLS_REALDATA_CAP</c> - max resources sampled per type per package (default 200; 0 = all).</item>
/// <item><c>TS4TOOLS_REALDATA_PACKAGES</c> - explicit package list (separated by <see cref="Path.PathSeparator"/>),
/// replacing the default sample.</item>
/// <item><c>TS4TOOLS_REALDATA_MODS</c> - number of mod packages in the default sample (default 6).</item>
/// <item><c>TS4TOOLS_REALDATA_TYPES</c> - comma-separated hex resource types to check (default: all).</item>
/// <item><c>TS4TOOLS_REALDATA_DUMP</c> - directory; the first failing resources of each type are written there
/// (for local investigation only - never commit them).</item>
/// <item><c>TS4TOOLS_REALDATA_WRITE_DOCS</c> - when <c>1</c>, the summary test also rewrites
/// <c>docs/real-data-validation.md</c>.</item>
/// </list>
/// </remarks>
internal static class RealDataCorpus
{
    public const string DefaultGameDir = "/mnt/ai/SteamLibrary/steamapps/common/The Sims 4";
    public const string DefaultModsDir = "/mnt/ai/sims_mods";
    public const int DefaultCap = 200;

    public static string GameDir => Env("TS4TOOLS_GAME_DIR") ?? DefaultGameDir;
    public static string ModsDir => Env("TS4TOOLS_MODS_DIR") ?? DefaultModsDir;

    /// <summary>Per type, per package sample cap; <c>int.MaxValue</c> means unlimited.</summary>
    public static int Cap
    {
        get
        {
            var v = Env("TS4TOOLS_REALDATA_CAP");
            if (v is null || !int.TryParse(v, out var n) || n < 0) return DefaultCap;
            return n == 0 ? int.MaxValue : n;
        }
    }

    public static bool WriteDocs => Env("TS4TOOLS_REALDATA_WRITE_DOCS") == "1";

    /// <summary>
    /// Game packages in the default sample, relative to <see cref="GameDir"/>. Chosen to cover the base game
    /// (client + simulation), the delta layer, an expansion, a game pack and a stuff pack, and the
    /// string/thumbnail/clip-header packages. Missing files are ignored.
    /// </summary>
    private static readonly string[] DefaultGamePackages =
    [
        "Data/Client/ClientFullBuild0.package",
        "Data/Client/Strings_ENG_US.package",
        "Data/Client/ClipHeader.package",
        "Data/Client/thumbnails2.package",
        "Delta/EP01/ClientDeltaBuild0.package",
        "Delta/EP01/SimulationDeltaBuild0.package",
        "Delta/EP01/SimulationPreload.package",
        "Delta/GP01/ClientDeltaBuild0.package",
        "Delta/GP01/SimulationPreload.package",
        "Delta/SP01/ClientDeltaBuild0.package",
    ];

    public static bool Available => Directory.Exists(GameDir) || Directory.Exists(ModsDir);

    /// <summary>Resolves the package list (existing files only), in a stable order.</summary>
    public static IReadOnlyList<string> Packages()
    {
        var explicitList = Env("TS4TOOLS_REALDATA_PACKAGES");
        if (explicitList is not null)
        {
            return explicitList.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(File.Exists)
                .ToList();
        }

        var result = new List<string>();
        foreach (var rel in DefaultGamePackages)
        {
            var p = Path.Combine(GameDir, rel);
            if (File.Exists(p)) result.Add(p);
        }

        result.AddRange(SampleMods());
        return result;
    }

    /// <summary>
    /// Picks mod packages deterministically: the largest packages by size, de-duplicated by file name
    /// (mod folders often contain copies of the same package).
    /// </summary>
    private static readonly Lazy<List<string>> SampledMods = new(SampleMods);

    private static List<string> SampleMods()
    {
        if (!Directory.Exists(ModsDir)) return [];

        var count = int.TryParse(Env("TS4TOOLS_REALDATA_MODS"), out var n) && n >= 0 ? n : 6;
        return Directory.EnumerateFiles(ModsDir, "*.package", SearchOption.AllDirectories)
            .Select(p => new FileInfo(p))
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(f => f.FullName, StringComparer.Ordinal).First())
            .OrderByDescending(f => f.Length)
            .ThenBy(f => f.FullName, StringComparer.Ordinal)
            .Take(count)
            .Select(f => f.FullName)
            .ToList();
    }

    /// <summary>Makes a corpus path printable without leaking absolute install locations into the report.</summary>
    public static string Display(string path)
    {
        if (path.StartsWith(GameDir, StringComparison.Ordinal))
            return "game:" + path[GameDir.Length..].TrimStart('/', '\\');
        if (path.StartsWith(ModsDir, StringComparison.Ordinal))
        {
            // Never print mod file names: the committed report must not reveal the contents of a
            // contributor's personal Mods folder. Label by rank in the deterministic sample instead.
            var index = SampledMods.Value.IndexOf(path);
            var size = new FileInfo(path).Length / (1024.0 * 1024.0);
            return index >= 0
                ? $"mods:mod-{index + 1} ({size:F0} MB)"
                : $"mods:mod ({size:F0} MB)";
        }
        return Path.GetFileName(path);
    }

    private static readonly HashSet<uint>? TypeFilter = ParseTypes(Env("TS4TOOLS_REALDATA_TYPES"));
    private static readonly ConcurrentDictionary<uint, int> DumpCounts = new();

    public static bool IncludeType(uint type) => TypeFilter is null || TypeFilter.Contains(type);

    private static HashSet<uint>? ParseTypes(string? v)
    {
        if (v is null) return null;
        return v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => uint.Parse(s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToHashSet();
    }

    /// <summary>Writes up to 5 failing resources per type to <c>TS4TOOLS_REALDATA_DUMP</c>, if set.</summary>
    public static void Dump(ResourceKey key, byte[] data)
    {
        var dir = Env("TS4TOOLS_REALDATA_DUMP");
        if (dir is null) return;
        if (DumpCounts.AddOrUpdate(key.ResourceType, 1, (_, n) => n + 1) > 5) return;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, $"{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}.bin"), data);
    }

    private static string? Env(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
}
