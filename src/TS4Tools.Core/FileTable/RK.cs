namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/RK.cs lines 28-107 (CompareTo, Equals, Parse, TryParse)
// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217 (TryParse formats)

/// <summary>
/// Helpers for comparing and parsing resource keys the way one resource finds another.
/// Geared to how resources refer to each other: the top 5 bits of the resource group carry the
/// resource-group version and are ignored, and for type 0x736884F1 a 32-bit instance is masked to 27 bits.
/// </summary>
public static class RK
{
    private const uint GroupMask = 0x07FFFFFF;
    private const ulong InstanceMask = 0x07FFFFFF;
    private const uint SpecialType = 0x736884F1;

    /// <summary>A null (all zero) resource key.</summary>
    public static ResourceKey Null { get; } = new(0, 0, 0);

    private static ulong NormalizedInstance(ResourceKey key) =>
        key.ResourceType == SpecialType && (key.Instance >> 32) == 0 ? key.Instance & InstanceMask : key.Instance;

    /// <summary>Compares two keys with the legacy "how one resource finds another" semantics.</summary>
    public static int Compare(ResourceKey x, ResourceKey y)
    {
        var res = x.ResourceType.CompareTo(y.ResourceType);
        if (res != 0) return res;
        res = (x.ResourceGroup & GroupMask).CompareTo(y.ResourceGroup & GroupMask);
        if (res != 0) return res;
        return NormalizedInstance(x).CompareTo(NormalizedInstance(y));
    }

    /// <summary>True if both keys refer to the same resource under the legacy semantics.</summary>
    public static bool AreEquivalent(ResourceKey x, ResourceKey y) => Compare(x, y) == 0;

    /// <summary>Formats a key as 0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII.</summary>
    public static string Format(ResourceKey key) =>
        string.Create(CultureInfo.InvariantCulture, $"0x{key.ResourceType:X8}-0x{key.ResourceGroup:X8}-0x{key.Instance:X16}");

    /// <summary>Parses a key; throws <see cref="ArgumentException"/> if the text is not valid.</summary>
    public static ResourceKey Parse(string value) =>
        TryParse(value, out var result) ? result : throw new ArgumentException($"Invalid resource key: {value}", nameof(value));

    /// <summary>
    /// Accepts "0xHHHHHHHH-0xHHHHHHHH-0xHHHHHHHHHHHHHHHH" or "(key:)HHHHHHHH:HHHHHHHH:HHHHHHHHHHHHHHHH".
    /// </summary>
    public static bool TryParse(string? value, out ResourceKey result)
    {
        result = default;
        if (value is null) return false;

        var text = value.Trim().ToLowerInvariant();
        string[] tgi;
        if (text.Contains('-', StringComparison.Ordinal))
        {
            tgi = text.Split('-');
            if (tgi.Length != 3) return false;
            for (var n = 0; n < 3; n++)
            {
                if (!tgi[n].StartsWith("0x", StringComparison.Ordinal)) return false;
                tgi[n] = tgi[n][2..];
            }
        }
        else
        {
            tgi = text.Split(':');
            if (tgi.Length == 4 && tgi[0] == "key") tgi = tgi[1..];
            else if (tgi.Length != 3) return false;
        }

        if (!uint.TryParse(tgi[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var t)) return false;
        if (!uint.TryParse(tgi[1], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var g)) return false;
        if (!ulong.TryParse(tgi[2], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var i)) return false;
        result = new ResourceKey(t, g, i);
        return true;
    }
}
