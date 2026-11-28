namespace TS4Tools.Resources;

/// <summary>
/// Registry that maps resource types to factories (the modern equivalent of the legacy WrapperDealer).
/// </summary>
/// <remarks>
/// <para>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 32-131.</para>
///
/// <para><b>Discovery.</b> Legacy scans every <c>*.dll</c> next to the WrapperDealer assembly and registers each
/// <c>AResourceHandler</c>'s type list (lines 87-115). Here, <see cref="DiscoverHandlers(Assembly)"/> registers each
/// concrete <see cref="IResourceFactory"/> carrying <see cref="ResourceHandlerAttribute"/>s and/or implementing
/// <see cref="IResourceTypeProvider"/>, and <see cref="LoadWrappersFromFolder"/> loads external plugin DLLs.</para>
///
/// <para><b>Precedence.</b> Legacy keeps every (type, wrapper) pair in one list, sorts it by type with the
/// unstable <c>List.Sort</c> (line 114) and takes the first non-disabled match (line 120), so when two handlers
/// claim one type the winner is unspecified (it depends on DLL enumeration order and sort internals). In
/// legacy this happens for 0x3C1AF1F2, 0x5B282D45 and 0xCD9DE247 (ImageResources.txt and ThumbnailResourceHandler,
/// both in ImageResource.dll). This registry makes the choice deterministic; for each type the claims are ordered:</para>
/// <list type="number">
/// <item>explicit <see cref="Register"/>/<see cref="RegisterOrReplace"/> calls first (earliest call wins);</item>
/// <item>then higher <see cref="ResourceHandlerAttribute.Priority"/> / <see cref="IResourceTypeProvider.ResourceTypePriority"/>;</item>
/// <item>then attribute claims before type-list claims (so the dedicated Thumbnail handler wins the three
/// overlapping thumbnail IDs over the bulk ImageResources.txt list);</item>
/// <item>then the factory's full type name, ordinal.</item>
/// </list>
/// <para>Lower-ranked claims are kept: like legacy (line 120), disabling the winning wrapper for a type falls
/// back to the next claim, and only then to <see cref="DefaultFactory"/> (line 123). All claims are listed in
/// <see cref="TypeMap"/>.</para>
///
/// <para><b>Default handler.</b> A discovered <c>[ResourceHandler(0)]</c> only replaces <see cref="DefaultFactory"/>
/// when the attribute sets <see cref="ResourceHandlerAttribute.ReplacesDefault"/>; otherwise it is ignored and listed in
/// <see cref="IgnoredDefaultHandlers"/>. Setting <see cref="DefaultFactory"/> directly always works.</para>
///
/// <para><b>UI initialisation.</b> TS4Tools.Core cannot reference TS4Tools.Wrappers, so wrappers are found at run time.
/// Recommended start-up code:</para>
/// <code>
/// var registry = new ResourceHandlerRegistry();
/// registry.DiscoverHandlers(typeof(TS4Tools.Wrappers.TextResourceFactory).Assembly); // built-in wrappers
/// var errors = registry.LoadWrappersFromFolder(pluginFolder);                      // optional plugins
/// registry.SetDisabledWrappers(SettingsService.Instance.Settings.DisabledWrappers); // "0xTTTTTTTT:Full.Type.Name"
/// ResourceHandlerRegistry.Shared = registry;   // used by DbpfPackage.GetResourceAsync by default
/// </code>
/// <para>If <see cref="Shared"/> is never assigned it is created lazily by <see cref="CreateDefault"/>, which loads the
/// TS4Tools.Wrappers assembly by name (if deployed alongside) and scans all loaded assemblies.</para>
/// </remarks>
public sealed class ResourceHandlerRegistry : IResourceHandlerRegistry
{
    private enum ClaimSource
    {
        Explicit = 0,
        Attribute = 1,
        TypeList = 2,
    }

    private sealed record Claim(uint ResourceType, IResourceFactory Factory, ClaimSource Source, int Priority, long Sequence);

    private static readonly Comparison<Claim> ClaimOrder = (x, y) =>
    {
        bool xe = x.Source == ClaimSource.Explicit, ye = y.Source == ClaimSource.Explicit;
        if (xe != ye) return xe ? -1 : 1;
        if (xe) return x.Sequence.CompareTo(y.Sequence);
        int c = y.Priority.CompareTo(x.Priority);
        if (c != 0) return c;
        c = x.Source.CompareTo(y.Source);
        if (c != 0) return c;
        c = string.CompareOrdinal(x.Factory.GetType().FullName, y.Factory.GetType().FullName);
        return c != 0 ? c : x.Sequence.CompareTo(y.Sequence);
    };

    private static readonly Lock SharedLock = new();
    private static ResourceHandlerRegistry? _shared;

    private readonly Lock _sync = new();
    private readonly Dictionary<uint, List<Claim>> _claims = [];
    private readonly HashSet<(uint ResourceType, Type FactoryType)> _disabled = [];
    private readonly HashSet<Type> _discoveredTypes = [];
    private readonly List<Type> _ignoredDefaultHandlers = [];
    private IResourceFactory _defaultFactory;
    private long _sequence;

    /// <summary>
    /// Creates a new resource handler registry with a <see cref="DefaultResourceFactory"/> default.
    /// </summary>
    public ResourceHandlerRegistry()
    {
        _defaultFactory = new DefaultResourceFactory();
    }

    /// <summary>
    /// Gets or sets the process-wide registry used by packages that were not given one explicitly.
    /// </summary>
    /// <remarks>
    /// Lazily initialised with <see cref="CreateDefault"/>. The UI should assign a fully configured registry at start-up.
    /// </remarks>
    public static ResourceHandlerRegistry Shared
    {
        get
        {
            lock (SharedLock)
                return _shared ??= CreateDefault();
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (SharedLock)
                _shared = value;
        }
    }

    /// <inheritdoc/>
    public IResourceFactory DefaultFactory
    {
        get => _defaultFactory;
        set => _defaultFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<uint> RegisteredTypes
    {
        get
        {
            lock (_sync)
                return _claims.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets every (resource type, factory type) claim, sorted by resource type and then precedence
    /// (the first entry for a type is the winner unless disabled).
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs line 72 (TypeMap)</remarks>
    public IReadOnlyList<KeyValuePair<uint, Type>> TypeMap
    {
        get
        {
            lock (_sync)
            {
                return _claims.OrderBy(kv => kv.Key)
                    .SelectMany(kv => kv.Value.Select(c => new KeyValuePair<uint, Type>(kv.Key, c.Factory.GetType())))
                    .ToList()
                    .AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets discovered <c>[ResourceHandler(0)]</c> factory types that were not installed as the default
    /// because they did not set <see cref="ResourceHandlerAttribute.ReplacesDefault"/>.
    /// </summary>
    public IReadOnlyList<Type> IgnoredDefaultHandlers
    {
        get
        {
            lock (_sync)
                return _ignoredDefaultHandlers.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the highest-precedence claim whose (type, factory type) pair is not disabled.
    /// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs line 120
    /// </remarks>
    public IResourceFactory? GetFactory(uint resourceType)
    {
        lock (_sync)
        {
            if (!_claims.TryGetValue(resourceType, out var list)) return null;
            foreach (var claim in list)
            {
                if (!_disabled.Contains((resourceType, claim.Factory.GetType())))
                    return claim.Factory;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the factory for a resource type, or the default factory if not found or disabled.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 117-124</remarks>
    public IResourceFactory GetFactoryOrDefault(uint resourceType)
    {
        return GetFactory(resourceType) ?? _defaultFactory;
    }

    /// <summary>
    /// Gets the factory type that would handle <paramref name="resourceType"/> (the default factory's type
    /// if no enabled wrapper is registered).
    /// </summary>
    public Type GetWrapperType(uint resourceType) => GetFactoryOrDefault(resourceType).GetType();

    /// <summary>
    /// Creates a wrapper for resource data, mirroring legacy <c>WrapperDealer.GetResource(…, AlwaysDefault)</c>.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 53-67</remarks>
    public IResource CreateResource(ResourceKey key, ReadOnlyMemory<byte> data, bool alwaysDefault = false)
    {
        var factory = alwaysDefault ? _defaultFactory : GetFactoryOrDefault(key.ResourceType);
        return factory.Create(key, data);
    }

    /// <summary>
    /// Creates a new, empty resource of the given type, mirroring legacy <c>WrapperDealer.CreateNewResource</c>.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 40-43</remarks>
    public IResource CreateNewResource(ResourceKey key) => GetFactoryOrDefault(key.ResourceType).CreateEmpty(key);

    /// <summary>
    /// Creates a new, empty resource of the given type with group and instance 0.
    /// </summary>
    public IResource CreateNewResource(uint resourceType) => CreateNewResource(new ResourceKey(resourceType, 0, 0));

    /// <summary>
    /// Registers a factory for a resource type unless one is already registered.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <param name="factory">The factory to register.</param>
    /// <returns>True if registered; false if any factory (explicit or discovered) already claims the type.</returns>
    public bool Register(uint resourceType, IResourceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        lock (_sync)
        {
            if (_claims.TryGetValue(resourceType, out var list) && list.Count > 0)
                return false;
            AddClaim(new Claim(resourceType, factory, ClaimSource.Explicit, 0, _sequence++));
            return true;
        }
    }

    /// <summary>
    /// Registers a factory for a resource type, replacing all existing claims for it.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <param name="factory">The factory to register.</param>
    public void RegisterOrReplace(uint resourceType, IResourceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        lock (_sync)
        {
            _claims.Remove(resourceType);
            AddClaim(new Claim(resourceType, factory, ClaimSource.Explicit, 0, _sequence++));
        }
    }

    /// <summary>
    /// Removes all claims for a resource type.
    /// </summary>
    /// <param name="resourceType">The resource type code.</param>
    /// <returns>True if unregistered, false if not found.</returns>
    public bool Unregister(uint resourceType)
    {
        lock (_sync)
            return _claims.Remove(resourceType);
    }

    #region Disabled wrappers

    /// <summary>
    /// Gets the disabled (resource type, factory type) pairs.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 80, 85</remarks>
    public IReadOnlyCollection<KeyValuePair<uint, Type>> Disabled
    {
        get
        {
            lock (_sync)
            {
                return _disabled.OrderBy(d => d.ResourceType).ThenBy(d => d.FactoryType.FullName, StringComparer.Ordinal)
                    .Select(d => new KeyValuePair<uint, Type>(d.ResourceType, d.FactoryType))
                    .ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Disables the wrapper <paramref name="factoryType"/> for <paramref name="resourceType"/>; lookups then fall
    /// back to the next claim for the type, or to the default factory.
    /// </summary>
    /// <returns>True if the pair was newly disabled.</returns>
    public bool DisableWrapper(uint resourceType, Type factoryType)
    {
        ArgumentNullException.ThrowIfNull(factoryType);
        lock (_sync)
            return _disabled.Add((resourceType, factoryType));
    }

    /// <summary>
    /// Re-enables a previously disabled wrapper pair.
    /// </summary>
    /// <returns>True if the pair was disabled.</returns>
    public bool EnableWrapper(uint resourceType, Type factoryType)
    {
        ArgumentNullException.ThrowIfNull(factoryType);
        lock (_sync)
            return _disabled.Remove((resourceType, factoryType));
    }

    /// <summary>
    /// Re-enables all wrappers.
    /// </summary>
    public void ClearDisabled()
    {
        lock (_sync)
            _disabled.Clear();
    }

    /// <summary>
    /// Replaces the disabled list from persisted strings (as stored in the UI's <c>SettingsService.DisabledWrappers</c>).
    /// </summary>
    /// <param name="entries">
    /// Entries of the form <c>0xTTTTTTTT:Full.Factory.TypeName</c> (legacy s4pe format, see
    /// legacy_references/Sims4Tools/s4pe/MainForm.cs lines 80-92 and 200-204). Surrounding whitespace (legacy appends
    /// "\n") is ignored; the type id is hex with or without "0x".
    /// </param>
    /// <returns>Entries that did not match any registered (type, factory type) claim and were ignored.</returns>
    public IReadOnlyList<string> SetDisabledWrappers(IEnumerable<string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var unmatched = new List<string>();
        lock (_sync)
        {
            _disabled.Clear();
            foreach (var raw in entries)
            {
                if (raw is null) continue;
                var entry = raw.Trim();
                if (entry.Length == 0) continue;

                // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 86-87
                var kv = entry.Split(':', 2);
                if (kv.Length != 2 || !TryParseTypeId(kv[0], out var resourceType))
                {
                    unmatched.Add(entry);
                    continue;
                }

                var claim = _claims.TryGetValue(resourceType, out var list)
                    ? list.Find(c => string.Equals(c.Factory.GetType().FullName, kv[1], StringComparison.Ordinal))
                    : null;
                if (claim is null)
                {
                    unmatched.Add(entry);
                    continue;
                }
                _disabled.Add((resourceType, claim.Factory.GetType()));
            }
        }
        return unmatched.AsReadOnly();
    }

    /// <summary>
    /// Gets the disabled list in the persisted string format <c>0xTTTTTTTT:Full.Factory.TypeName</c>.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 200-204</remarks>
    public IReadOnlyList<string> GetDisabledWrappers() =>
        Disabled.Select(kv => FormatDisabledWrapper(kv.Key, kv.Value)).ToList().AsReadOnly();

    /// <summary>
    /// Formats one disabled-wrapper entry as <c>0xTTTTTTTT:Full.Factory.TypeName</c>.
    /// </summary>
    public static string FormatDisabledWrapper(uint resourceType, Type factoryType)
    {
        ArgumentNullException.ThrowIfNull(factoryType);
        return $"0x{resourceType:X8}:{factoryType.FullName}";
    }

    private static bool TryParseTypeId(string text, out uint value)
    {
        var span = text.AsSpan().Trim();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            span = span[2..];
        return uint.TryParse(span, NumberStyles.HexNumber,
            CultureInfo.InvariantCulture, out value);
    }

    #endregion

    #region Discovery

    /// <summary>
    /// Discovers and registers all resource handlers from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <remarks>
    /// Types that cannot be instantiated are skipped; use <see cref="LoadWrappersFromFolder"/> to get errors reported.
    /// Scanning the same assembly twice is a no-op.
    /// </remarks>
    public void DiscoverHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        DiscoverHandlersCore(assembly, assembly.Location, errors: null);
    }

    /// <summary>
    /// Discovers and registers all resource handlers from all loaded assemblies.
    /// </summary>
    public void DiscoverAllHandlers()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
        {
            try
            {
                DiscoverHandlers(assembly);
            }
            catch (Exception ex) when (ex is ReflectionTypeLoadException or FileNotFoundException or FileLoadException or NotSupportedException)
            {
                // Assemblies that cannot be reflected over cannot contain usable handlers.
            }
        }
    }

    /// <summary>
    /// Loads wrapper plugin assemblies (<c>*.dll</c>) from a folder and registers their handlers.
    /// </summary>
    /// <param name="folder">The folder to scan (not recursive).</param>
    /// <param name="searchPattern">File pattern, default <c>*.dll</c>.</param>
    /// <returns>Every problem encountered; an empty list means all DLLs loaded cleanly.</returns>
    /// <remarks>
    /// <para>Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-115 (scans *.dll next to
    /// the assembly and swallows all errors at line 112). Here errors are returned instead.</para>
    /// <para>Each plugin is loaded into its own <see cref="AssemblyLoadContext"/>; dependencies already loaded in the
    /// default context (TS4Tools.Interfaces, TS4Tools.Core, ...) are shared so plugin factories implement the same
    /// <see cref="IResourceFactory"/>. A DLL whose assembly is already loaded (e.g. TS4Tools.Wrappers next to the app)
    /// is scanned from the loaded copy. Only types with <see cref="ResourceHandlerAttribute"/> (or implementing
    /// <see cref="IResourceTypeProvider"/>) are registered. Native DLLs are reported with
    /// <see cref="WrapperLoadError.IsNotManagedAssembly"/> set so callers can ignore them.</para>
    /// </remarks>
    public IReadOnlyList<WrapperLoadError> LoadWrappersFromFolder(string folder, string searchPattern = "*.dll")
    {
        ArgumentNullException.ThrowIfNull(folder);
        var errors = new List<WrapperLoadError>();
        if (!Directory.Exists(folder))
        {
            errors.Add(new WrapperLoadError(folder, null, "Wrapper folder does not exist.", null, false));
            return errors.AsReadOnly();
        }

        foreach (var path in Directory.GetFiles(folder, searchPattern).OrderBy(p => p, StringComparer.Ordinal))
        {
            Assembly assembly;
            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                assembly = FindLoadedAssembly(name)
                    ?? new WrapperLoadContext(path).LoadFromAssemblyPath(Path.GetFullPath(path));
            }
            catch (BadImageFormatException ex)
            {
                errors.Add(new WrapperLoadError(path, null, "Not a managed .NET assembly.", ex, true));
                continue;
            }
            catch (Exception ex) when (ex is FileLoadException or FileNotFoundException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                errors.Add(new WrapperLoadError(path, null, ex.Message, ex, false));
                continue;
            }

            DiscoverHandlersCore(assembly, path, errors);
        }

        return errors.AsReadOnly();
    }

    private static Assembly? FindLoadedAssembly(AssemblyName name) =>
        AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a =>
            string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));

    private void DiscoverHandlersCore(Assembly assembly, string? path, List<WrapperLoadError>? errors)
    {
        string source = string.IsNullOrEmpty(path) ? assembly.FullName ?? "?" : path;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Select(t => t!).ToArray();
            foreach (var loaderException in ex.LoaderExceptions)
            {
                if (loaderException is not null)
                    errors?.Add(new WrapperLoadError(source, null, loaderException.Message, loaderException, false));
            }
        }

        // Deterministic order regardless of metadata order
        foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            // Plugin code (attributes, constructors, type lists) can throw anything; one bad type must not stop the
            // rest from loading or the host from starting.
            // Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs line 112 (swallows every failure)
            try
            {
                DiscoverHandlerType(type, source, errors);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                errors?.Add(new WrapperLoadError(source, type.FullName, $"Could not load handler: {ex.Message}", ex, false));
            }
        }
    }

    private void DiscoverHandlerType(Type type, string source, List<WrapperLoadError>? errors)
    {
        if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || !typeof(IResourceFactory).IsAssignableFrom(type))
            return;

        var attributes = type.GetCustomAttributes<ResourceHandlerAttribute>(inherit: false).ToList();
        bool isProvider = typeof(IResourceTypeProvider).IsAssignableFrom(type);
        if (attributes.Count == 0 && !isProvider)
            return;

        lock (_sync)
        {
            if (!_discoveredTypes.Add(type))
                return;
        }

        IResourceFactory? factory;
        try
        {
            factory = (IResourceFactory?)Activator.CreateInstance(type);
        }
        catch (Exception ex) when (ex is TargetInvocationException or MissingMethodException or MemberAccessException or TypeLoadException or NotSupportedException)
        {
            errors?.Add(new WrapperLoadError(source, type.FullName,
                $"Could not create factory: {(ex.InnerException ?? ex).Message}", ex, false));
            return;
        }

        if (factory is null)
            return;

        IReadOnlyCollection<uint> listedTypes = [];
        int listPriority = 0;
        if (factory is IResourceTypeProvider provider)
        {
            try
            {
                // Materialised here so a lazy list that throws is reported, not thrown mid-registration.
                listedTypes = [.. provider.ResourceTypes];
                listPriority = provider.ResourceTypePriority;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                errors?.Add(new WrapperLoadError(source, type.FullName,
                    $"Could not read resource type list: {ex.Message}", ex, false));
            }
        }

        lock (_sync)
        {
            foreach (var attr in attributes)
            {
                if (attr.ResourceType == 0)
                {
                    if (attr.ReplacesDefault)
                        _defaultFactory = factory;
                    else if (type != _defaultFactory.GetType())
                        _ignoredDefaultHandlers.Add(type);
                    continue;
                }
                AddClaim(new Claim(attr.ResourceType, factory, ClaimSource.Attribute, attr.Priority, _sequence++));
            }

            foreach (var resourceType in listedTypes)
            {
                // An attribute on the same factory for the same type already covers it.
                if (_claims.TryGetValue(resourceType, out var existing) && existing.Exists(c => ReferenceEquals(c.Factory, factory)))
                    continue;
                AddClaim(new Claim(resourceType, factory, ClaimSource.TypeList, listPriority, _sequence++));
            }
        }
    }

    private void AddClaim(Claim claim)
    {
        if (!_claims.TryGetValue(claim.ResourceType, out var list))
        {
            list = [];
            _claims[claim.ResourceType] = list;
        }
        list.Add(claim);
        list.Sort(ClaimOrder);
    }

    #endregion

    /// <summary>
    /// Creates a registry with handlers from TS4Tools.Wrappers (loaded by name if present) and all loaded assemblies.
    /// </summary>
    public static ResourceHandlerRegistry CreateDefault()
    {
        try
        {
            // Core cannot reference Wrappers at compile time; load it by name so the default registry
            // still knows the built-in wrappers when the assembly is deployed next to the app.
            Assembly.Load(new AssemblyName("TS4Tools.Wrappers"));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            // Wrappers not deployed: fall back to whatever is loaded.
        }

        var registry = new ResourceHandlerRegistry();
        registry.DiscoverAllHandlers();
        return registry;
    }

    /// <summary>
    /// Load context for one wrapper plugin; shares assemblies already loaded by the host.
    /// </summary>
    private sealed class WrapperLoadContext(string pluginPath) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath))
    {
        private readonly AssemblyDependencyResolver _resolver = new(Path.GetFullPath(pluginPath));
        private readonly string _folder = Path.GetDirectoryName(Path.GetFullPath(pluginPath))!;

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Share host assemblies (TS4Tools.Interfaces etc.) so types unify.
            if (FindLoadedAssembly(assemblyName) is not null)
                return null;

            var resolved = _resolver.ResolveAssemblyToPath(assemblyName);
            if (resolved is not null)
                return LoadFromAssemblyPath(resolved);

            var candidate = Path.Combine(_folder, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }
}

/// <summary>
/// A problem encountered while loading wrapper plugins.
/// </summary>
/// <param name="Path">The DLL path (or assembly name).</param>
/// <param name="TypeName">The factory type involved, if any.</param>
/// <param name="Message">A human-readable description.</param>
/// <param name="Exception">The underlying exception, if any.</param>
/// <param name="IsNotManagedAssembly">True when the file is not a .NET assembly (e.g. a native library); usually safe to ignore.</param>
public sealed record WrapperLoadError(string Path, string? TypeName, string Message, Exception? Exception, bool IsNotManagedAssembly);
