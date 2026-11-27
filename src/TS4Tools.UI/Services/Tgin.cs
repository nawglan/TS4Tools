using TS4Tools.Extensions;

namespace TS4Tools.UI.Services;

/// <summary>
/// A resource key plus optional name, convertible to and from the standard s4pe export file name
/// (<c>S4_TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII[_Name]%%+TAG.ext</c>).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/TGIN.cs lines 35-226 and
/// legacy_references/Sims4Tools/s4pi Extras/Extensions/FileNameConverter.cs lines 30-96.
/// </remarks>
public readonly partial record struct Tgin(uint ResourceType, uint ResourceGroup, ulong Instance, string? ResourceName = null)
{
    // Source: FileNameConverter.cs lines 32-42 (TS4 "Group!ID.type" names for tray/household files)
    private static readonly Dictionary<string, string> Ts4ResourceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "trayitem", "0x2a8a5e22" },
        { "blueprint", "0x3924de26" },
        { "bpi", "0xd33c281e" },
        { "householdbinary", "0xb3c438f0" },
        { "hhi", "0x3bd45407" },
        { "sgi", "0x56278554" },
        { "room", "0x370efd6e" },
        { "rmi", "0x00de5ac5" }
    };

    // Characters escaped in names. Legacy uses Path.GetInvalidFileNameChars() + GetInvalidPathChars() + '-'
    // (TGIN.cs lines 211-224) on Windows; the Windows set is used on every platform so exported names
    // are identical (and importable) everywhere.
    private static readonly char[] EscapedChars = BuildEscapedChars();

    /// <summary>Gets the resource key.</summary>
    public ResourceKey Key => new(ResourceType, ResourceGroup, Instance);

    /// <summary>Creates a TGIN from a key and name.</summary>
    public static Tgin From(ResourceKey key, string? name = null) =>
        new(key.ResourceType, key.ResourceGroup, key.Instance, name);

    /// <summary>
    /// Parses a file name (or path) in the export format. Parts that cannot be parsed are left zero,
    /// as in legacy (a file with no TGIN in its name yields 0-0-0 with no name).
    /// </summary>
    /// <remarks>Source: TGIN.cs lines 107-151 (implicit operator TGIN(string))</remarks>
    public static Tgin Parse(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var value = Path.GetFileName(fileName);
        if (IsTs4FileName(value))
        {
            value = ConvertToPackageManagerConvention(value);
        }

        value = Path.GetFileNameWithoutExtension(value);

        if (value.StartsWith("s4_", StringComparison.OrdinalIgnoreCase))
        {
            value = value[3..];
        }

        var marker = value.IndexOf("%%+", StringComparison.Ordinal);
        if (marker >= 0)
        {
            value = value[..marker];
        }

        uint type = 0, group = 0;
        ulong instance = 0;
        string? name = null;

        var parts = value.Split(['_', '-'], 4);
        if (parts.Length >= 3)
        {
            // Legacy parses all three in one try block: a failure leaves the later fields zero.
            if (TryParseHex32(parts[0], out var t))
            {
                type = t;
                if (TryParseHex32(parts[1], out var g))
                {
                    group = g;
                    if (ulong.TryParse(StripHex(parts[2]), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var i))
                    {
                        instance = i;
                    }
                }
            }

            if (parts.Length == 4)
            {
                name = Unescape(parts[3]);
            }
        }

        return new Tgin(type, group, instance, name);
    }

    /// <summary>
    /// Formats the export file name, using the ExtList tag and extension (or the "*" fallback).
    /// </summary>
    /// <remarks>Source: TGIN.cs lines 160-180 (implicit operator string(TGIN))</remarks>
    public string ToFileName()
    {
        var extension = string.Concat(ExtList.Get(ResourceType));
        return string.IsNullOrEmpty(ResourceName)
            ? $"S4_{ResourceType:X8}_{ResourceGroup:X8}_{Instance:X16}%%+{extension}"
            : $"S4_{ResourceType:X8}_{ResourceGroup:X8}_{Instance:X16}_{Escape(ResourceName)}%%+{extension}";
    }

    /// <inheritdoc />
    public override string ToString() => ToFileName();

    // Source: FileNameConverter.cs lines 47-59
    private static bool IsTs4FileName(string path) =>
        !string.IsNullOrWhiteSpace(path) && Ts4FileNameRegex().IsMatch(path);

    // Source: FileNameConverter.cs lines 64-78
    private static string ConvertToPackageManagerConvention(string ts4Name)
    {
        var parts = ts4Name.Split('!', '.');
        if (parts.Length != 3 || !Ts4ResourceMap.TryGetValue(parts[2], out var type))
        {
            return ts4Name;
        }

        return $"S4_{type}_{parts[0]}_{parts[1]}.{parts[2]}";
    }

    private static bool TryParseHex32(string s, out uint value) =>
        uint.TryParse(StripHex(s), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

    // Convert.ToUInt32(s, 16) accepts an optional "0x" prefix.
    private static string StripHex(string s) =>
        s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;

    // Source: TGIN.cs lines 194-209 (%xx -> char)
    private static string Unescape(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '%' && i + 2 < value.Length &&
                byte.TryParse(value.AsSpan(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                sb.Append((char)b);
                i += 2;
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }

    // Source: TGIN.cs lines 211-224 (bad char -> %xx)
    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (Array.IndexOf(EscapedChars, c) >= 0)
            {
                sb.Append('%').Append(((uint)c).ToString("x2", CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static char[] BuildEscapedChars()
    {
        var chars = new List<char>();
        for (var c = 0; c < 32; c++)
        {
            chars.Add((char)c);
        }
        chars.AddRange(['"', '<', '>', '|', ':', '*', '?', '\\', '/', '-']);
        return [.. chars];
    }

    [GeneratedRegex("0x[0-9a-f]{8}!0x[0-9a-f]{16}\\.(trayitem|blueprint|bpi|householdbinary|hhi|sgi|room|rmi)", RegexOptions.IgnoreCase)]
    private static partial Regex Ts4FileNameRegex();
}
