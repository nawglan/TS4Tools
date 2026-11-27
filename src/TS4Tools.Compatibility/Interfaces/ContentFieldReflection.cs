// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 73-85, 131-138
// (property lookup by name, shared by AApiVersionedFields and the attribute helpers)

namespace s4pi.Interfaces;

/// <summary>
/// Reflection helpers for Content Field lookup.
/// </summary>
internal static class ContentFieldReflection
{
    /// <summary>
    /// Finds a public instance property by name, as legacy <c>Type.GetProperty(name)</c> does.
    /// </summary>
    /// <remarks>
    /// Legacy calls <c>Type.GetProperty(name)</c>, which throws <see cref="AmbiguousMatchException"/> when a derived
    /// type hides a property with <c>new</c> (or declares an indexer overload). Here the most-derived declaration is
    /// returned in that case, which is the member C# itself binds to; unambiguous lookups are unchanged.
    /// </remarks>
    public static PropertyInfo? FindProperty(Type t, string name)
    {
        try
        {
            return t.GetProperty(name);
        }
        catch (AmbiguousMatchException)
        {
            PropertyInfo? best = null;
            int bestDepth = -1;
            foreach (var p in t.GetProperties())
            {
                if (p.Name != name || p.GetIndexParameters().Length != 0) continue;
                int depth = Depth(p.DeclaringType);
                if (depth > bestDepth)
                {
                    best = p;
                    bestDepth = depth;
                }
            }
            return best;
        }
    }

    private static int Depth(Type? t)
    {
        int d = 0;
        for (; t != null; t = t.BaseType) d++;
        return d;
    }
}
