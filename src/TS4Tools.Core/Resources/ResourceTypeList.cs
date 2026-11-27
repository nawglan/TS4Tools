namespace TS4Tools.Resources;

/// <summary>
/// Parses legacy resource-type config lists such as TextResources.txt / ImageResources.txt.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 131-143 and
/// legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ImageResource.cs lines 73-85:
/// each line is split once on a space and the first token is the resource type ("0xXXXXXXXX").
/// Lines whose first token is not a hex type id are skipped (legacy would register them as
/// strings that can never match a real type).
/// </remarks>
public static class ResourceTypeList
{
    /// <summary>
    /// Parses a type list.
    /// </summary>
    /// <param name="text">The file contents.</param>
    /// <returns>The resource types, in file order, without duplicates.</returns>
    public static IReadOnlyList<uint> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<uint>();
        var seen = new HashSet<uint>();
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var token = line.TrimStart('﻿').Split(' ', 2)[0].Trim();
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                token = token[2..];
            if (token.Length == 0 ||
                !uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var type))
                continue;
            if (seen.Add(type))
                result.Add(type);
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// Loads and parses an embedded type list from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="manifestResourceName">The manifest resource name.</param>
    /// <exception cref="InvalidOperationException">The resource does not exist.</exception>
    public static IReadOnlyList<uint> LoadEmbedded(Assembly assembly, string manifestResourceName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        using var stream = assembly.GetManifestResourceStream(manifestResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{manifestResourceName}' not found in {assembly.GetName().Name}.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Parse(reader.ReadToEnd());
    }
}
