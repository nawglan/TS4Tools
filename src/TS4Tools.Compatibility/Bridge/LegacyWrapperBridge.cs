// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-115 (start-up scan: every
// concrete AResourceHandler with a parameterless constructor contributes its (type string, wrapper type) pairs;
// failures are swallowed at line 112) and lines 117-129 (lookup by "0xTTTTTTTT"; "*" is the default wrapper).

using s4pi.Interfaces;
using TS4Tools.Resources;

namespace TS4Tools.Compatibility;

/// <summary>
/// Registers legacy-style s4pi wrappers (<see cref="AResourceHandler"/> subclasses in plugin assemblies recompiled
/// against TS4Tools.Compatibility) with a TS4Tools <see cref="ResourceHandlerRegistry"/>, so the application uses
/// them like its own wrappers.
/// </summary>
/// <remarks>
/// <para>Each <c>(type string, wrapper type)</c> pair a handler lists becomes a <see cref="LegacyResourceFactory"/>
/// registered for that resource type. Differences from legacy, where every pair went into one list and the winner
/// of a contested type was unspecified (unstable sort, line 114):</para>
/// <list type="bullet">
/// <item>A type already handled by a TS4Tools wrapper keeps it (built-in wrappers win); the legacy pair is reported
/// as not used. Among legacy pairs for one type, the first by wrapper full name (ordinal) wins.</item>
/// <item><c>"*"</c> (default wrapper) pairs are reported and not used: the registry's default factory is kept, as for
/// TS4Tools plugins without <c>ReplacesDefault</c>.</item>
/// <item>Problems are returned as <see cref="WrapperLoadError"/>s instead of being swallowed.</item>
/// </list>
/// <para>Only source compatibility is provided: wrapper DLLs built for the original s4pi (.NET Framework) must be
/// recompiled against TS4Tools.Compatibility.</para>
/// </remarks>
public static class LegacyWrapperBridge
{
    /// <summary>
    /// Registers the legacy wrappers listed by the <see cref="AResourceHandler"/>s in <paramref name="assemblies"/>.
    /// </summary>
    /// <param name="registry">The registry to add the wrappers to.</param>
    /// <param name="assemblies">The plugin assemblies to scan.</param>
    /// <param name="registered">The (resource type, legacy wrapper type) pairs that were registered.</param>
    /// <returns>Problems found, including pairs that were not used.</returns>
    public static IReadOnlyList<WrapperLoadError> Register(
        ResourceHandlerRegistry registry,
        IEnumerable<Assembly> assemblies,
        out IReadOnlyList<KeyValuePair<uint, Type>> registered)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(assemblies);

        var errors = new List<WrapperLoadError>();
        var pairs = new List<(string Source, uint ResourceType, Type Wrapper)>();
        foreach (var assembly in assemblies.Distinct())
        {
            CollectPairs(assembly, pairs, errors);
        }

        var added = new List<KeyValuePair<uint, Type>>();
        foreach (var (source, resourceType, wrapper) in pairs
                     .OrderBy(p => p.ResourceType)
                     .ThenBy(p => p.Wrapper.FullName, StringComparer.Ordinal))
        {
            LegacyResourceFactory factory;
            try
            {
                factory = new LegacyResourceFactory(wrapper);
            }
            catch (ArgumentException ex)
            {
                errors.Add(new WrapperLoadError(source, wrapper.FullName, ex.Message, ex, false));
                continue;
            }

            if (registry.Register(resourceType, factory))
            {
                added.Add(new KeyValuePair<uint, Type>(resourceType, wrapper));
                continue;
            }

            var current = registry.GetFactory(resourceType);
            var owner = current is LegacyResourceFactory legacy ? legacy.WrapperType : current?.GetType();
            errors.Add(new WrapperLoadError(source, wrapper.FullName,
                $"Legacy wrapper not used for 0x{resourceType:X8}: already handled by {owner?.FullName ?? "another wrapper"}.",
                null, false));
        }

        registered = added.AsReadOnly();
        return errors.AsReadOnly();
    }

    /// <summary>
    /// Gets the loaded assemblies (in any load context) whose files are the <c>*.dll</c>s of <paramref name="folder"/>,
    /// i.e. the plugins <see cref="ResourceHandlerRegistry.LoadWrappersFromFolder"/> loaded from it.
    /// </summary>
    /// <remarks>
    /// Matched by assembly name, since the registry reuses an already loaded assembly of the same name instead of
    /// loading the folder's copy.
    /// </remarks>
    public static IReadOnlyList<Assembly> GetLoadedPluginAssemblies(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        if (!Directory.Exists(folder)) return [];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(folder, "*.dll"))
        {
            try
            {
                if (AssemblyName.GetAssemblyName(path).Name is { } name) names.Add(name);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Native or unreadable DLL: LoadWrappersFromFolder already reported it.
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name is { } n && names.Contains(n))
            .ToList()
            .AsReadOnly();
    }

    private static void CollectPairs(Assembly assembly, List<(string, uint, Type)> pairs, List<WrapperLoadError> errors)
    {
        string source = string.IsNullOrEmpty(assembly.Location) ? assembly.FullName ?? "?" : assembly.Location;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Select(t => t!).ToArray();
        }

        foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            // Plugin code can throw anything; one bad handler must not stop the others or the host.
            // Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs line 112 (swallows every failure)
            var found = new List<(string, uint, Type)>();
            try
            {
                CollectHandlerPairs(type, source, found, errors);
                pairs.AddRange(found);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                errors.Add(new WrapperLoadError(source, type.FullName, $"Could not load legacy handler: {ex.Message}", ex, false));
            }
        }
    }

    private static void CollectHandlerPairs(Type type, string source, List<(string, uint, Type)> pairs, List<WrapperLoadError> errors)
    {
        if (type.IsAbstract || !type.IsSubclassOf(typeof(AResourceHandler))) return;
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null) return;

        AResourceHandler handler;
        try
        {
            handler = (AResourceHandler)ctor.Invoke(null);
        }
        catch (TargetInvocationException ex)
        {
            errors.Add(new WrapperLoadError(source, type.FullName,
                $"Could not create legacy handler: {(ex.InnerException ?? ex).Message}", ex, false));
            return;
        }

        foreach (var (wrapper, typeStrings) in handler)
        {
            foreach (var typeString in typeStrings)
            {
                if (typeString.Trim() == "*")
                {
                    errors.Add(new WrapperLoadError(source, wrapper.FullName,
                        "Legacy default (\"*\") wrapper not used: the TS4Tools default wrapper is kept.", null, false));
                }
                else if (TryParseType(typeString, out var resourceType))
                {
                    pairs.Add((source, resourceType, wrapper));
                }
                else
                {
                    errors.Add(new WrapperLoadError(source, wrapper.FullName,
                        $"Invalid resource type \"{typeString}\" in {type.FullName}.", null, false));
                }
            }
        }
    }

    // Source: WrapperDealer.cs line 120 (type strings are "0xTTTTTTTT")
    private static bool TryParseType(string type, out uint value)
    {
        var span = type.AsSpan().Trim();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) span = span[2..];
        return uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }
}
