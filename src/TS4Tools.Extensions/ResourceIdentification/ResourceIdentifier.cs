namespace TS4Tools.Extensions.ResourceIdentification;

/// <summary>
/// Represents a modern, immutable resource identifier that combines Type, Group, Instance, and Name (TGIN).
/// This provides a more type-safe and immutable alternative to the legacy TGIN class.
/// </summary>
/// <param name="ResourceType">The resource type identifier (T).</param>
/// <param name="ResourceGroup">The resource group identifier (G).</param>
/// <param name="Instance">The resource instance identifier (I).</param>
/// <param name="Name">The optional resource name (N).</param>
public readonly record struct ResourceIdentifier(
    uint ResourceType,
    uint ResourceGroup,
    ulong Instance,
    string? Name = null) : IComparable<ResourceIdentifier>, IFormattable
{
    /// <summary>
    /// Creates a <see cref="ResourceIdentifier"/> from an <see cref="IResourceKey"/>.
    /// </summary>
    /// <param name="resourceKey">The resource key to convert.</param>
    /// <param name="name">Optional name to associate with the identifier.</param>
    /// <returns>A new <see cref="ResourceIdentifier"/> instance.</returns>
    public static ResourceIdentifier FromResourceKey(IResourceKey resourceKey, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(resourceKey);
        return new ResourceIdentifier(resourceKey.ResourceType, resourceKey.ResourceGroup, resourceKey.Instance, name);
    }

    /// <summary>
    /// Creates an <see cref="IResourceKey"/> from this identifier.
    /// Note: The name information is not included in the resource key.
    /// </summary>
    /// <returns>A new resource key instance.</returns>
    public IResourceKey ToResourceKey()
    {
        return new ResourceKey(ResourceType, ResourceGroup, Instance);
    }

    /// <summary>
    /// Gets a string representation of this identifier in TGI format.
    /// </summary>
    /// <returns>A string in the format "T:xxxxxxxx-G:xxxxxxxx-I:xxxxxxxxxxxxxxxx".</returns>
    public string ToTgiString()
    {
        return $"T:{ResourceType:X8}-G:{ResourceGroup:X8}-I:{Instance:X16}";
    }

    /// <summary>
    /// Gets a string representation of this identifier in TGIN format (includes name if present).
    /// </summary>
    /// <returns>A string in the format "T:xxxxxxxx-G:xxxxxxxx-I:xxxxxxxxxxxxxxxx-N:name" or TGI format if no name.</returns>
    public string ToTginString()
    {
        var tgi = ToTgiString();
        return string.IsNullOrEmpty(Name) ? tgi : $"{tgi}-N:{Name}";
    }

    /// <summary>
    /// Parses a TGI or TGIN string into a <see cref="ResourceIdentifier"/>.
    /// </summary>
    /// <param name="tginString">The string to parse (format: "T:xxxxxxxx-G:xxxxxxxx-I:xxxxxxxxxxxxxxxx[-N:name]").</param>
    /// <returns>A <see cref="ResourceIdentifier"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the string format is invalid.</exception>
    public static ResourceIdentifier Parse(string tginString)
    {
        if (TryParse(tginString, out var result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid TGIN format: {tginString}", nameof(tginString));
    }

    /// <summary>
    /// Tries to parse a TGI or TGIN string into a <see cref="ResourceIdentifier"/>.
    /// </summary>
    /// <param name="tginString">The string to parse.</param>
    /// <param name="result">The parsed identifier if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? tginString, out ResourceIdentifier result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(tginString))
        {
            return false;
        }

        try
        {
            var parts = tginString.Split('-');
            if (parts.Length < 3 || parts.Length > 4)
            {
                return false;
            }

            // Parse T:xxxxxxxx
            if (!parts[0].StartsWith("T:", StringComparison.OrdinalIgnoreCase) ||
                !uint.TryParse(parts[0][2..], System.Globalization.NumberStyles.HexNumber, null, out var type))
            {
                return false;
            }

            // Parse G:xxxxxxxx
            if (!parts[1].StartsWith("G:", StringComparison.OrdinalIgnoreCase) ||
                !uint.TryParse(parts[1][2..], System.Globalization.NumberStyles.HexNumber, null, out var group))
            {
                return false;
            }

            // Parse I:xxxxxxxxxxxxxxxx
            if (!parts[2].StartsWith("I:", StringComparison.OrdinalIgnoreCase) ||
                !ulong.TryParse(parts[2][2..], System.Globalization.NumberStyles.HexNumber, null, out var instance))
            {
                return false;
            }

            // Parse optional N:name
            string? name = null;
            if (parts.Length == 4)
            {
                if (!parts[3].StartsWith("N:", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                name = parts[3][2..];
            }

            result = new ResourceIdentifier(type, group, instance, name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public int CompareTo(ResourceIdentifier other)
    {
        var typeComparison = ResourceType.CompareTo(other.ResourceType);
        if (typeComparison != 0) return typeComparison;

        var groupComparison = ResourceGroup.CompareTo(other.ResourceGroup);
        if (groupComparison != 0) return groupComparison;

        var instanceComparison = Instance.CompareTo(other.Instance);
        if (instanceComparison != 0) return instanceComparison;

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return format?.ToUpperInvariant() switch
        {
            "TGI" => ToTgiString(),
            "TGIN" or null => ToTginString(),
            _ => throw new FormatException($"Invalid format string: {format}")
        };
    }

    /// <inheritdoc />
    public override string ToString() => ToTginString();

    /// <summary>
    /// Implements the less than operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if left is less than right; otherwise, false.</returns>
    public static bool operator <(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Implements the less than or equal operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if left is less than or equal to right; otherwise, false.</returns>
    public static bool operator <=(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Implements the greater than operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if left is greater than right; otherwise, false.</returns>
    public static bool operator >(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Implements the greater than or equal operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if left is greater than or equal to right; otherwise, false.</returns>
    public static bool operator >=(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) >= 0;
}
