// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-115 (wrapper discovery: *.dll next to the assembly)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 80-92 (apply DisabledWrappers at start-up)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 200-204 (persist WrapperDealer.Disabled as "type:FullName")
// Source: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs lines 140-145 (OK replaces the disabled list live)
// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs lines 23-39 (legacy plugin handlers, bridged
//         through TS4Tools.Compatibility.LegacyWrapperBridge)

using TS4Tools.Compatibility;
using TS4Tools.Resources;

namespace TS4Tools.UI.Services;

/// <summary>
/// Owns the application's <see cref="ResourceHandlerRegistry"/>: built-in wrapper discovery, plugin loading,
/// the persisted disabled-wrapper list and live enable/disable.
/// </summary>
/// <remarks>
/// <para>Legacy s4pe gets this from the static WrapperDealer: its static constructor scans every <c>*.dll</c> next to
/// the assembly (silently swallowing failures), and MainForm applies <c>Settings.DisabledWrappers</c> at start-up and
/// writes <c>WrapperDealer.Disabled</c> back when the form closes. The Manage Wrappers dialog edits
/// <c>WrapperDealer.Disabled</c> directly, so changes take effect for the next resource selected.</para>
/// <para>Here the built-in wrappers come from the TS4Tools.Wrappers assembly and third-party wrappers from a
/// <c>Wrappers</c> folder next to the executable (the app folder itself holds every framework DLL, which must not be
/// loaded as plugins). Load errors are kept so the UI can show them without blocking start-up, and changes are
/// persisted immediately rather than on exit.</para>
/// <para>Plugins may be TS4Tools wrappers (<c>IResourceFactory</c> + <c>[ResourceHandler]</c>) or legacy-style s4pi
/// wrappers (<c>AResourceHandler</c> + <c>AResource</c>) recompiled against TS4Tools.Compatibility; the latter are
/// registered through <see cref="LegacyWrapperBridge"/> for types no TS4Tools wrapper handles. Original s4pi DLLs
/// (.NET Framework) are not binary compatible and fail to load.</para>
/// </remarks>
public sealed class WrapperRegistryService
{
    /// <summary>Name of the plugin folder next to the executable.</summary>
    public const string PluginFolderName = "Wrappers";

    private static WrapperRegistryService? _instance;
    private static readonly Lock InstanceLock = new();

    private IReadOnlyList<WrapperLoadError> _loadErrors = [];
    private IReadOnlyList<string> _unmatchedDisabledEntries = [];
    private IReadOnlyDictionary<uint, Type> _legacyWrappers = new Dictionary<uint, Type>();

    /// <summary>
    /// Creates a service around an existing registry (used by tests and by <see cref="Instance"/>).
    /// </summary>
    /// <param name="registry">The registry.</param>
    /// <param name="legacyWrappers">Legacy wrappers bridged into <paramref name="registry"/>, if any.</param>
    public WrapperRegistryService(ResourceHandlerRegistry registry, IReadOnlyDictionary<uint, Type>? legacyWrappers = null)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        if (legacyWrappers != null) _legacyWrappers = legacyWrappers;
    }

    /// <summary>
    /// The process-wide service. Wraps <see cref="ResourceHandlerRegistry.Shared"/> until <see cref="Initialize"/> runs.
    /// </summary>
    public static WrapperRegistryService Instance
    {
        get
        {
            lock (InstanceLock)
                return _instance ??= new WrapperRegistryService(ResourceHandlerRegistry.Shared);
        }
    }

    /// <summary>The registry used to resolve resource wrappers.</summary>
    public ResourceHandlerRegistry Registry { get; private set; }

    /// <summary>Problems found while loading plugin wrappers (native DLLs excluded).</summary>
    public IReadOnlyList<WrapperLoadError> LoadErrors => _loadErrors;

    /// <summary>Persisted disabled entries that no longer match a registered wrapper.</summary>
    public IReadOnlyList<string> UnmatchedDisabledEntries => _unmatchedDisabledEntries;

    /// <summary>
    /// The legacy s4pi wrapper type registered (as a <see cref="LegacyResourceFactory"/>) for each resource type.
    /// </summary>
    public IReadOnlyDictionary<uint, Type> LegacyWrappers => _legacyWrappers;

    /// <summary>
    /// Raised after the enabled/disabled wrapper set changes so open views can re-resolve their resource.
    /// </summary>
    public event EventHandler? WrappersChanged;

    /// <summary>Gets the default plugin folder (<c>&lt;app folder&gt;/Wrappers</c>).</summary>
    public static string DefaultPluginFolder => Path.Combine(AppContext.BaseDirectory, PluginFolderName);

    /// <summary>
    /// Builds a registry: built-in wrappers, plugins from <paramref name="pluginFolder"/> (if it exists) and the
    /// persisted disabled list.
    /// </summary>
    /// <param name="disabledWrappers">Persisted entries, <c>0xTTTTTTTT:Full.Type.Name</c>.</param>
    /// <param name="pluginFolder">Folder of plugin DLLs, or null for none.</param>
    /// <param name="loadErrors">Managed-assembly load problems (native DLLs are dropped, like legacy's catch-all).</param>
    /// <param name="unmatched">Disabled entries that matched no wrapper.</param>
    public static ResourceHandlerRegistry CreateRegistry(
        IEnumerable<string> disabledWrappers,
        string? pluginFolder,
        out IReadOnlyList<WrapperLoadError> loadErrors,
        out IReadOnlyList<string> unmatched) =>
        CreateRegistry(disabledWrappers, pluginFolder, out loadErrors, out unmatched, out _);

    /// <summary>
    /// Builds a registry as <see cref="CreateRegistry(IEnumerable{string}, string?, out IReadOnlyList{WrapperLoadError}, out IReadOnlyList{string})"/>
    /// and also returns the legacy wrappers that were bridged in.
    /// </summary>
    /// <param name="disabledWrappers">Persisted entries, <c>0xTTTTTTTT:Full.Type.Name</c>.</param>
    /// <param name="pluginFolder">Folder of plugin DLLs, or null for none.</param>
    /// <param name="loadErrors">Managed-assembly load problems (native DLLs are dropped, like legacy's catch-all).</param>
    /// <param name="unmatched">Disabled entries that matched no wrapper.</param>
    /// <param name="legacyWrappers">Legacy wrapper type registered per resource type.</param>
    public static ResourceHandlerRegistry CreateRegistry(
        IEnumerable<string> disabledWrappers,
        string? pluginFolder,
        out IReadOnlyList<WrapperLoadError> loadErrors,
        out IReadOnlyList<string> unmatched,
        out IReadOnlyDictionary<uint, Type> legacyWrappers)
    {
        ArgumentNullException.ThrowIfNull(disabledWrappers);

        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(TS4Tools.Wrappers.TextResourceFactory).Assembly);

        loadErrors = [];
        legacyWrappers = new Dictionary<uint, Type>();
        if (!string.IsNullOrEmpty(pluginFolder) && Directory.Exists(pluginFolder))
        {
            // Load TS4Tools.Compatibility into the default context before any plugin: plugin load contexts share
            // assemblies already loaded there, so a legacy plugin's AResourceHandler/AResource are the host's types
            // even if the plugin folder ships its own copy of TS4Tools.Compatibility.dll.
            _ = typeof(LegacyWrapperBridge).Assembly;

            // Source: WrapperDealer.cs line 112 swallows every load failure; native DLLs are expected noise.
            var errors = registry.LoadWrappersFromFolder(pluginFolder)
                .Where(e => !e.IsNotManagedAssembly)
                .ToList();

            // Legacy-style plugins: AResourceHandler subclasses in the assemblies just loaded.
            errors.AddRange(LegacyWrapperBridge.Register(
                registry, LegacyWrapperBridge.GetLoadedPluginAssemblies(pluginFolder), out var bridged));
            legacyWrappers = bridged.ToDictionary(kv => kv.Key, kv => kv.Value);
            loadErrors = errors.AsReadOnly();
        }

        unmatched = registry.SetDisabledWrappers(disabledWrappers);
        return registry;
    }

    /// <summary>
    /// Initialises the process-wide registry from settings and installs it as <see cref="ResourceHandlerRegistry.Shared"/>.
    /// </summary>
    /// <param name="settings">Application settings (DisabledWrappers is read).</param>
    /// <param name="pluginFolder">Plugin folder; defaults to <see cref="DefaultPluginFolder"/>.</param>
    public static WrapperRegistryService Initialize(AppSettings settings, string? pluginFolder = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var registry = CreateRegistry(settings.DisabledWrappers, pluginFolder ?? DefaultPluginFolder,
            out var errors, out var unmatched, out var legacyWrappers);
        ResourceHandlerRegistry.Shared = registry;

        lock (InstanceLock)
        {
            _instance ??= new WrapperRegistryService(registry);
            _instance.Registry = registry;
            _instance._loadErrors = errors;
            _instance._unmatchedDisabledEntries = unmatched;
            _instance._legacyWrappers = legacyWrappers;
            return _instance;
        }
    }

    /// <summary>
    /// Replaces the disabled set, persists it to <paramref name="settings"/> and notifies listeners.
    /// </summary>
    /// <param name="disabled">The (resource type, factory type) pairs to disable.</param>
    /// <param name="settings">Settings to update; the caller saves them.</param>
    /// <remarks>Source: ManageWrappersDialog.cs lines 140-145 and MainForm.cs lines 200-204.</remarks>
    public void SetDisabled(IEnumerable<KeyValuePair<uint, Type>> disabled, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(disabled);
        ArgumentNullException.ThrowIfNull(settings);

        Registry.ClearDisabled();
        foreach (var (type, factory) in disabled)
        {
            Registry.DisableWrapper(type, factory);
        }

        settings.DisabledWrappers = [.. Registry.GetDisabledWrappers()];
        WrappersChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Builds a one-line, non-blocking summary of plugin load problems, or null if there were none.
    /// </summary>
    public static string? DescribeLoadErrors(IReadOnlyList<WrapperLoadError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0) return null;

        var files = errors.Select(e => Path.GetFileName(e.Path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var first = errors[0];
        return $"{errors.Count} wrapper plugin problem(s) in {string.Join(", ", files)}: {first.Message}" +
               " (see Settings > Manage Wrappers)";
    }
}
