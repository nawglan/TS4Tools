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

    public static bool operator <(ResourceKey left, ResourceKey right) => left.CompareTo(right) < 0;
    public static bool operator >(ResourceKey left, ResourceKey right) => left.CompareTo(right) > 0;
    public static bool operator <=(ResourceKey left, ResourceKey right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ResourceKey left, ResourceKey right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Returns a string representation in TGI format.
    /// </summary>
    public override string ToString() =>
        $"T:0x{ResourceType:X8} G:0x{ResourceGroup:X8} I:0x{Instance:X16}";

    /// <summary>
    /// Parses a ResourceKey from a TGI-formatted string.
    /// </summary>
    public static ResourceKey Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        // Expected format: "T:0x00000000 G:0x00000000 I:0x0000000000000000"
        var parts = s.Split(' ');
        if (parts.Length != 3)
            throw new FormatException($"Invalid ResourceKey format: {s}");

        static uint ParseHex32(string part, char prefix)
        {
            if (!part.StartsWith($"{prefix}:0x", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"Expected {prefix}:0x prefix");
            return Convert.ToUInt32(part[4..], 16);
        }

        static ulong ParseHex64(string part, char prefix)
        {
            if (!part.StartsWith($"{prefix}:0x", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"Expected {prefix}:0x prefix");
            return Convert.ToUInt64(part[4..], 16);
        }

        return new ResourceKey(
            ParseHex32(parts[0], 'T'),
            ParseHex32(parts[1], 'G'),
            ParseHex64(parts[2], 'I'));
    }

    /// <summary>
    /// Tries to parse a ResourceKey from a string.
    /// </summary>
    public static bool TryParse(string? s, out ResourceKey result)
    {
        result = default;
        if (string.IsNullOrEmpty(s)) return false;

        try
        {
            result = Parse(s);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
