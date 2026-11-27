namespace TS4Tools;

/// <summary>
/// Unique identifier for a resource within a package (Type/Group/Instance - TGI).
/// </summary>
/// <param name="ResourceType">The resource type identifier (determines format/wrapper).</param>
/// <param name="ResourceGroup">The resource group identifier.</param>
/// <param name="Instance">The unique instance identifier (often FNV64 hash of name).</param>
public readonly record struct ResourceKey(
    uint ResourceType,
    uint ResourceGroup,
    ulong Instance) : IComparable<ResourceKey>
{
    /// <summary>
    /// Compares this key to another for ordering.
    /// </summary>
    public int CompareTo(ResourceKey other)
    {
        var typeComparison = ResourceType.CompareTo(other.ResourceType);
        if (typeComparison != 0) return typeComparison;

        var groupComparison = ResourceGroup.CompareTo(other.ResourceGroup);
        if (groupComparison != 0) return groupComparison;

        return Instance.CompareTo(other.Instance);
    }

    /// <summary>Determines whether one key is less than another.</summary>
    public static bool operator <(ResourceKey left, ResourceKey right) => left.CompareTo(right) < 0;
    /// <summary>Determines whether one key is greater than another.</summary>
    public static bool operator >(ResourceKey left, ResourceKey right) => left.CompareTo(right) > 0;
    /// <summary>Determines whether one key is less than or equal to another.</summary>
    public static bool operator <=(ResourceKey left, ResourceKey right) => left.CompareTo(right) <= 0;
    /// <summary>Determines whether one key is greater than or equal to another.</summary>
    public static bool operator >=(ResourceKey left, ResourceKey right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Returns a string representation in TGI format.
    /// </summary>
    public override string ToString() =>
        $"T:0x{ResourceType:X8} G:0x{ResourceGroup:X8} I:0x{Instance:X16}";

    /// <summary>
    /// Parses a ResourceKey from a string.
    /// </summary>
    /// <remarks>
    /// Accepted formats:
    /// <list type="bullet">
    /// <item><c>T:0xTTTTTTTT G:0xGGGGGGGG I:0xIIIIIIIIIIIIIIII</c> (the <see cref="ToString"/> format)</item>
    /// <item><c>0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII</c> (legacy)</item>
    /// <item><c>TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII</c>, optionally prefixed <c>key:</c> (legacy)</item>
    /// </list>
    /// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217
    /// </remarks>
    /// <exception cref="FormatException">The string is not in a recognised format.</exception>
    public static ResourceKey Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (!TryParse(s, out var result))
            throw new FormatException($"Invalid ResourceKey format: {s}");
        return result;
    }

    /// <summary>
    /// Tries to parse a ResourceKey from a string. See <see cref="Parse"/> for accepted formats.
    /// </summary>
    public static bool TryParse(string? s, out ResourceKey result)
    {
        result = default;
        if (string.IsNullOrEmpty(s)) return false;

        return TryParseModern(s, out result) || TryParseLegacy(s, out result);
    }

    private static bool TryParseModern(string s, out ResourceKey result)
    {
        result = default;

        // Format: "T:0x00000000 G:0x00000000 I:0x0000000000000000"
        var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;

        if (!TryStripPrefix(parts[0], "T:0x", out var t) ||
            !TryStripPrefix(parts[1], "G:0x", out var g) ||
            !TryStripPrefix(parts[2], "I:0x", out var i))
            return false;

        if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var type)) return false;
        if (!uint.TryParse(g, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var group)) return false;
        if (!ulong.TryParse(i, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var instance)) return false;

        result = new ResourceKey(type, group, instance);
        return true;

        static bool TryStripPrefix(string part, string prefix, out string rest)
        {
            rest = string.Empty;
            if (!part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            rest = part[prefix.Length..];
            return true;
        }
    }

    // Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217
    private static bool TryParseLegacy(string value, out ResourceKey result)
    {
        result = default;
        uint t, g;
        ulong i;

        // Two formats 0x{X8}-0x{X8}-0x{X16} and (key:){X8}:{X8}:{X16}
        if (value.Contains('-', StringComparison.Ordinal))
        {
            string[] tgi = value.Trim().ToLowerInvariant().Split('-');
            if (tgi.Length != 3) return false;
            foreach (var x in tgi)
                if (!x.StartsWith("0x", StringComparison.Ordinal)) return false;

            if (!uint.TryParse(tgi[0].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out t)) return false;
            if (!uint.TryParse(tgi[1].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
            if (!ulong.TryParse(tgi[2].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i)) return false;
        }
        else
        {
            string[] tgi = value.Trim().ToLowerInvariant().Split(':');
            if (tgi.Length == 4 && tgi[0] == "key")
                tgi = tgi[1..];
            else if (tgi.Length != 3) return false;

            if (!uint.TryParse(tgi[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out t)) return false;
            if (!uint.TryParse(tgi[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
            if (!ulong.TryParse(tgi[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i)) return false;
        }

        result = new ResourceKey(t, g, i);
        return true;
    }
}
