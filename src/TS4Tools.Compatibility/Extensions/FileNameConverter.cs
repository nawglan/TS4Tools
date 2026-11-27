// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/FileNameConverter.cs lines 23-98

namespace s4pi.Extensions;

/// <summary>
/// Utility class to convert file names or paths in the TS4 resource name convention to the s4pe convention.
/// </summary>
public static partial class FileNameConverter
{
    private static readonly Dictionary<string, string> resourceMap = new()
    {
        { "trayitem", "0x2a8a5e22" },
        { "blueprint", "0x3924de26" },
        { "bpi", "0xd33c281e" },
        { "householdbinary", "0xb3c438f0" },
        { "hhi", "0x3bd45407" },
        { "sgi", "0x56278554" },
        { "room", "0x370efd6e" },
        { "rmi", "0x00de5ac5" },
    };

    [GeneratedRegex("0x[0-9a-f]{8}!0x[0-9a-f]{16}\\.(trayitem|blueprint|bpi|householdbinary|hhi|sgi|room|rmi)", RegexOptions.IgnoreCase)]
    private static partial Regex ValidFileName();

    /// <summary>
    /// Returns a value indicating whether the given <paramref name="path"/> is a valid TS4 resource file name.
    /// </summary>
    /// <param name="path">The file name or path.</param>
    /// <returns>true if it contains a <c>Group!Instance.type</c> TS4 name.</returns>
    public static bool IsValidFileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return ValidFileName().IsMatch(path);
    }

    /// <summary>
    /// Converts the specified TS4 file name or path to its s4pe equivalent.
    /// </summary>
    /// <param name="ts4Path">A path whose file name is <c>Group!Instance.type</c>.</param>
    /// <returns>The path with the file name as <c>S4_type_Group_Instance.type</c>.</returns>
    /// <exception cref="ArgumentException">The file name is not in the expected format.</exception>
    /// <exception cref="NotSupportedException">The type is not supported.</exception>
    public static string ConvertToPackageManagerConvention(string ts4Path)
    {
        string path = Path.GetDirectoryName(ts4Path) ?? string.Empty;
        string ts4Name = Path.GetFileName(ts4Path) ?? string.Empty;
        var parts = ts4Name.Split('!', '.');

        GuardInputIsValid(parts);

        string group = parts[0];
        string id = parts[1];
        string type = resourceMap[parts[2]];
        string extension = parts[2];

        return Path.Combine(path, string.Format(CultureInfo.InvariantCulture, "S4_{0}_{1}_{2}.{3}", type, group, id, extension));
    }

    private static void GuardInputIsValid(string[] parts)
    {
        if (parts.Length != 3)
            throw new ArgumentException("fileName must be in format 'Group!ID.Type'");

        if (!resourceMap.ContainsKey(parts[2]))
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "type '{0}' is not supported", parts[2]));
    }
}
