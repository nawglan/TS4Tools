namespace TS4Tools.Extensions;

/// <summary>
/// Maps resource type IDs to their short tag (e.g. "STBL") and file extension (e.g. ".stbl").
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 32-82
/// Data:   legacy_references/Sims4Tools/s4pi Extras/Extensions/Extensions.txt (embedded verbatim)
///
/// Legacy format: one entry per line, "0xTTTTTTTT TAG .ext", whitespace separated;
/// lines starting with ';' are comments; lines with fewer than two fields are skipped;
/// the "*" entry is the fallback for unknown types; duplicate keys are an error.
/// </remarks>
public static class ExtList
{
    private const string ResourceName = "TS4Tools.Extensions.Extensions.txt";
    private const string FallbackKey = "*";

    private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> Entries = new(Load);

    /// <summary>
    /// Gets the tag and extension fields for <paramref name="resourceType"/>,
    /// or the "*" fallback entry ("UNKN", ".bnry") when the type is not listed.
    /// </summary>
    // Source: ExtList.cs lines 71-80 (uint indexer delegates to "0x" + X8 string key, falls back to "*")
    public static IReadOnlyList<string> Get(uint resourceType) =>
        Entries.Value.TryGetValue($"0x{resourceType:X8}", out var parts) ? parts : Entries.Value[FallbackKey];

    /// <summary>
    /// Returns true if <paramref name="resourceType"/> has its own entry (not the fallback).
    /// </summary>
    public static bool IsKnown(uint resourceType) => Entries.Value.ContainsKey($"0x{resourceType:X8}");

    /// <summary>
    /// Gets the short tag for a resource type (e.g. "STBL"), or "UNKN" for unknown types.
    /// </summary>
    public static string GetTag(uint resourceType) => Get(resourceType)[0];

    /// <summary>
    /// Gets the file extension for a resource type (e.g. ".stbl"), or ".bnry" for unknown types.
    /// </summary>
    public static string GetExtension(uint resourceType)
    {
        var parts = Get(resourceType);
        return parts.Count > 1 ? parts[1] : Entries.Value[FallbackKey][1];
    }

    /// <summary>
    /// Parses the legacy Extensions.txt format.
    /// </summary>
    // Source: ExtList.cs lines 46-69
    internal static Dictionary<string, IReadOnlyList<string>> Parse(TextReader reader)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(';'))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            if (!result.TryAdd(parts[0], parts[1..]))
                throw new InvalidOperationException($"ExtList already contains entry for key '{parts[0]}'");
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> Load()
    {
        using var stream = typeof(ExtList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found");
        using var reader = new StreamReader(stream);
        var result = Parse(reader);
        if (!result.ContainsKey(FallbackKey))
            throw new InvalidOperationException("Extensions list is missing the '*' fallback entry");
        return result;
    }
}
