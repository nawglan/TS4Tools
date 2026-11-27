// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 26-132

using s4pi.Interfaces;
using ResourceHandlerRegistry = TS4Tools.Resources.ResourceHandlerRegistry;
using ResourceKey = TS4Tools.ResourceKey;

namespace s4pi.WrapperDealer;

/// <summary>
/// Responsible for associating ResourceType in the IResourceIndexEntry with a particular class (a "wrapper") that
/// understands it, or the default wrapper.
/// </summary>
/// <remarks>
/// <para><b>Legacy wrappers.</b> Every concrete <see cref="AResourceHandler"/> subclass (with a public parameterless
/// constructor) in the loaded assemblies registers its <c>(resource type string, wrapper type)</c> pairs, exactly as
/// legacy's start-up scan did (WrapperDealer.cs lines 87-115). Assemblies loaded later — for example plugins loaded
/// by <see cref="ResourceHandlerRegistry.LoadWrappersFromFolder"/> — are scanned as they load. (Legacy scanned every
/// DLL next to WrapperDealer.dll; there is no implicit folder scan here.) Wrapper types are constructed with their
/// <c>(int APIversion, Stream s)</c> constructor.</para>
/// <para><b>TS4Tools wrappers.</b> When no enabled legacy wrapper claims a type, the TS4Tools factory registered in
/// <see cref="Registry"/> for that type is used, and the result is returned as a <see cref="TS4ToolsResource"/>.
/// Failing that, a legacy <c>"*"</c> wrapper, then the registry's default factory, is used.</para>
/// <para><b>Lookup order</b> for a type string <c>t</c> (<c>"0xTTTTTTTT"</c>, or <c>"*"</c> when <c>AlwaysDefault</c>):
/// legacy wrapper for <c>t</c> → TS4Tools factory for <c>t</c> → legacy <c>"*"</c> wrapper → TS4Tools default factory.
/// A pair in <see cref="Disabled"/> is skipped; TS4Tools factories appear in <see cref="TypeMap"/> with their factory
/// type, so they can be disabled the same way.</para>
/// <para>Legacy sorts the map with an unstable sort, so which of two wrappers for the same type won was unspecified;
/// here pairs are ordered by type string, then wrapper full name (ordinal).</para>
/// </remarks>
public static class WrapperDealer
{
    private static readonly Lock sync = new();
    private static readonly HashSet<Assembly> scanned = [];
    private static readonly List<KeyValuePair<string, Type>> typeMap = [];
    private static readonly List<KeyValuePair<string, Type>> disabled = [];
    private static ResourceHandlerRegistry? registry;

    // Source: WrapperDealer.cs lines 87-115
    static WrapperDealer()
    {
        AppDomain.CurrentDomain.AssemblyLoad += (_, e) => RegisterAssembly(e.LoadedAssembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            RegisterAssembly(assembly);
    }

    /// <summary>
    /// Create a new resource of the requested type.
    /// </summary>
    /// <param name="APIversion">The version of the API to use.</param>
    /// <param name="resourceType">The resource type of the resource ("0xTTTTTTTT", or "*" for the default wrapper).</param>
    /// <returns>A new, empty resource of the requested type.</returns>
    public static IResource CreateNewResource(int APIversion, string resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);
        _ = TryParseType(resourceType, out var t); // "*" (or an unparsable string) gives type 0
        return WrapperForType(resourceType, APIversion, null, new ResourceKey(t, 0, 0));
    }

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper.
    /// </summary>
    /// <param name="APIversion">The version of the API to use.</param>
    /// <param name="pkg">Package containing <paramref name="rie"/>.</param>
    /// <param name="rie">Identifies resource to be returned.</param>
    /// <returns>A resource from the package.</returns>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie) => GetResource(APIversion, pkg, rie, false);

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper or the default wrapper.
    /// </summary>
    /// <param name="APIversion">The version of the API to use.</param>
    /// <param name="pkg">Package containing <paramref name="rie"/>.</param>
    /// <param name="rie">Identifies resource to be returned.</param>
    /// <param name="AlwaysDefault">When true, indicates WrapperDealer should ignore all wrappers and return the default wrapper.</param>
    /// <returns>A resource from the package.</returns>
    /// <remarks>Source: WrapperDealer.cs lines 64-67</remarks>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie, bool AlwaysDefault)
    {
        ArgumentNullException.ThrowIfNull(rie);
        if (pkg is not APackage apkg)
            throw new ArgumentException("The package must be an s4pi.Interfaces.APackage.", nameof(pkg));
        var key = new ResourceKey(rie.ResourceType, rie.ResourceGroup, rie.Instance);
        return WrapperForType(AlwaysDefault ? "*" : rie["ResourceType"], APIversion, apkg.GetResource(rie), key);
    }

    /// <summary>
    /// Retrieve the resource wrappers known to WrapperDealer: legacy pairs and TS4Tools factory pairs
    /// (the registry default factory under "*").
    /// </summary>
    /// <remarks>A copy; changing it has no effect.</remarks>
    public static ICollection<KeyValuePair<string, Type>> TypeMap
    {
        get
        {
            var res = new List<KeyValuePair<string, Type>>();
            lock (sync)
                res.AddRange(typeMap);
            var reg = Registry;
            res.AddRange(reg.TypeMap.Select(kv => new KeyValuePair<string, Type>(FormatType(kv.Key), kv.Value)));
            res.Add(new KeyValuePair<string, Type>("*", reg.DefaultFactory.GetType()));
            return res;
        }
    }

    /// <summary>
    /// Access the (type string, wrapper type) pairs that are disabled. Add a pair from <see cref="TypeMap"/>
    /// to disable it; remove it to re-enable.
    /// </summary>
    /// <remarks>A live collection, as in legacy. Not thread-safe for concurrent modification (as in legacy).</remarks>
    public static ICollection<KeyValuePair<string, Type>> Disabled => disabled;

    /// <summary>
    /// The TS4Tools registry consulted when no legacy wrapper claims a type.
    /// Defaults to <see cref="ResourceHandlerRegistry.Shared"/>.
    /// </summary>
    public static ResourceHandlerRegistry Registry
    {
        get
        {
            ResourceHandlerRegistry? r;
            lock (sync)
                r = registry;
            // Outside the lock: creating the shared registry may load assemblies, which re-enters RegisterAssembly.
            return r ?? ResourceHandlerRegistry.Shared;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (sync)
                registry = value;
        }
    }

    #region Implementation

    private static void RegisterAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic) return;
        lock (sync)
        {
            if (!scanned.Add(assembly)) return;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }
#pragma warning disable CA1031 // Legacy ignores assemblies it cannot reflect over (WrapperDealer.cs line 112)
        catch
        {
            return;
        }
#pragma warning restore CA1031

        var found = new List<KeyValuePair<string, Type>>();
        foreach (var t in types)
        {
            if (t.IsAbstract || !t.IsSubclassOf(typeof(AResourceHandler))) continue;
            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor == null) continue;

            AResourceHandler arh;
            try
            {
                arh = (AResourceHandler)ctor.Invoke(null);
            }
#pragma warning disable CA1031 // A failing handler is skipped, as in legacy
            catch
            {
                continue;
            }
#pragma warning restore CA1031

            foreach (var (wrapper, keys) in arh)
                foreach (var s in keys)
                    found.Add(new KeyValuePair<string, Type>(s, wrapper));
        }

        if (found.Count == 0) return;
        lock (sync)
        {
            typeMap.AddRange(found);
            // Source: WrapperDealer.cs line 114 (sort by key); made stable and deterministic
            var sorted = typeMap
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ThenBy(x => x.Value.FullName, StringComparer.Ordinal)
                .ToList();
            typeMap.Clear();
            typeMap.AddRange(sorted);
        }
    }

    // Source: WrapperDealer.cs lines 117-129
    private static IResource WrapperForType(string type, int APIversion, Stream? s, ResourceKey key)
    {
        var t = FindLegacy(type);
        if (t != null) return Construct(t, APIversion, s);

        var reg = Registry;
        if (type != "*" && TryParseType(type, out var resourceType))
        {
            var factory = reg.GetFactory(resourceType);
            if (factory != null && !IsDisabled(type, factory.GetType()))
                return new TS4ToolsResource(APIversion, factory, key, s);
        }

        t = FindLegacy("*");
        if (t != null) return Construct(t, APIversion, s);

        var def = reg.DefaultFactory;
        if (!IsDisabled("*", def.GetType()))
            return new TS4ToolsResource(APIversion, def, key, s);

        throw new InvalidOperationException("Could not find a resource handler");
    }

    private static Type? FindLegacy(string type)
    {
        lock (sync)
            return typeMap.Find(x => !disabled.Contains(x) && x.Key == type).Value;
    }

    private static bool IsDisabled(string type, Type wrapper) => disabled.Contains(new KeyValuePair<string, Type>(type, wrapper));

    // Source: WrapperDealer.cs line 128 (exceptions from the wrapper constructor propagate as TargetInvocationException)
    private static IResource Construct(Type t, int APIversion, Stream? s)
    {
        var ctor = t.GetConstructor([typeof(int), typeof(Stream)])
            ?? throw new InvalidOperationException($"Wrapper {t.FullName} has no (int APIversion, Stream s) constructor");
        return (IResource)ctor.Invoke([APIversion, s]);
    }

    private static string FormatType(uint resourceType) => "0x" + resourceType.ToString("X8", CultureInfo.InvariantCulture);

    private static bool TryParseType(string type, out uint value)
    {
        var span = type.AsSpan().Trim();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) span = span[2..];
        return uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    #endregion
}
