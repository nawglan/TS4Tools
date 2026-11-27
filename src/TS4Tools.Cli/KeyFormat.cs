// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217 (key text formats, via ResourceKey.Parse)
// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 32-82 (file extensions, via ExtList)
// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/TGIN.cs lines 105-186 (s4pe export names
//   "S4_TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII%%+TAG.ext"; the unpack layout drops the "S4_" prefix and "%%+TAG" part
//   but still accepts an "S4_" prefix when parsing)

using TS4Tools.Extensions;

namespace TS4Tools.Cli;

/// <summary>Text formats for resource keys, hex numbers and unpacked resource file names.</summary>
internal static partial class KeyFormat
{
    /// <summary>Formats a key as <c>TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII</c> (accepted by <see cref="ResourceKey.Parse"/>).</summary>
    public static string Compact(ResourceKey key) =>
        string.Create(CultureInfo.InvariantCulture, $"{key.ResourceType:X8}:{key.ResourceGroup:X8}:{key.Instance:X16}");

    public static string Hex32(uint value) => "0x" + value.ToString("X8", CultureInfo.InvariantCulture);

    public static string Hex64(ulong value) => "0x" + value.ToString("X16", CultureInfo.InvariantCulture);

    public static string Hex16(ushort value) => "0x" + value.ToString("X4", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a resource key in any <see cref="ResourceKey.Parse"/> format, plus the unpack file-name stem
    /// <c>TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII</c>.
    /// </summary>
    /// <exception cref="UsageException">Not a recognised key.</exception>
    public static ResourceKey ParseKey(string text)
    {
        if (ResourceKey.TryParse(text, out var key))
            return key;
        if (TryParseFileName(text, out key, out _))
            return key;
        throw new UsageException(
            $"invalid resource key '{text}' (expected TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII, 0xT-0xG-0xI or 'T:0x.. G:0x.. I:0x..')");
    }

    /// <summary>Parses a 32-bit hex number with or without a 0x prefix.</summary>
    public static bool TryParseHex32(string? text, out uint value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var span = text.AsSpan().Trim();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) span = span[2..];
        return span.Length is > 0 and <= 8 &&
               uint.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Parses a 64-bit hex number with or without a 0x prefix.</summary>
    public static bool TryParseHex64(string? text, out ulong value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var span = text.AsSpan().Trim();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) span = span[2..];
        return span.Length is > 0 and <= 16 &&
               ulong.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Parses a 16-bit hex number with or without a 0x prefix.</summary>
    public static bool TryParseHex16(string? text, out ushort value)
    {
        value = 0;
        if (!TryParseHex32(text, out uint wide) || wide > ushort.MaxValue) return false;
        value = (ushort)wide;
        return true;
    }

    /// <summary>Parses a comma-separated list of 32-bit hex type ids.</summary>
    /// <exception cref="UsageException">An element is not a hex number.</exception>
    public static HashSet<uint> ParseTypeList(string text)
    {
        var set = new HashSet<uint>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseHex32(part, out uint type))
                throw new UsageException($"invalid resource type '{part}' (expected hex, e.g. 0x220557DA)");
            set.Add(type);
        }
        if (set.Count == 0)
            throw new UsageException("empty resource type list");
        return set;
    }

    /// <summary>
    /// Unpacked file name: <c>TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII[.n]&lt;ext&gt;</c>, where <c>.n</c> (1, 2, ...) only marks
    /// the second and later entries sharing a key and the extension comes from <see cref="ExtList"/> (".bnry" if unknown).
    /// </summary>
    public static string FileName(ResourceKey key, int duplicateIndex)
    {
        string stem = string.Create(CultureInfo.InvariantCulture,
            $"{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}");
        string dup = duplicateIndex > 0 ? "." + duplicateIndex.ToString(CultureInfo.InvariantCulture) : string.Empty;
        return stem + dup + SafeExtension(key.ResourceType);
    }

    private static string SafeExtension(uint resourceType)
    {
        // Guard against a list entry that is not a plain ".ext" (separators, or digits only, which would read as a .n suffix).
        string ext = ExtList.GetExtension(resourceType);
        return ExtensionRegex().IsMatch(ext) ? ext : ".bnry";
    }

    /// <summary>
    /// Parses an unpacked file name (see <see cref="FileName"/>); any extension is accepted, so a user can drop in
    /// e.g. <c>220557DA_80000000_0123456789ABCDEF.stbl</c>.
    /// </summary>
    public static bool TryParseFileName(string fileName, out ResourceKey key, out int duplicateIndex)
    {
        key = default;
        duplicateIndex = 0;
        var m = FileNameRegex().Match(fileName);
        if (!m.Success) return false;
        uint t = uint.Parse(m.Groups[1].ValueSpan, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        uint g = uint.Parse(m.Groups[2].ValueSpan, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        ulong i = ulong.Parse(m.Groups[3].ValueSpan, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        if (m.Groups[4].Success &&
            !int.TryParse(m.Groups[4].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out duplicateIndex))
            return false;
        key = new ResourceKey(t, g, i);
        return true;
    }

    [GeneratedRegex(@"^\.(?=[0-9]*[A-Za-z_])[A-Za-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionRegex();

    [GeneratedRegex(@"^(?:[Ss]4_)?([0-9A-Fa-f]{8})_([0-9A-Fa-f]{8})_([0-9A-Fa-f]{16})(?:\.([0-9]{1,9}))?(?:\..*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNameRegex();
}
