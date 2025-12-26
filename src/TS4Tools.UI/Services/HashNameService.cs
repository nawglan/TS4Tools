using TS4Tools.Package;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.Services;

/// <summary>
/// Service for looking up human-readable names from FNV-64 hashes.
/// Aggregates mappings from all NameMap resources in loaded packages.
/// </summary>
public sealed class HashNameService
{
    /// <summary>
    /// NameMap resource type ID.
    /// </summary>
    private const uint NameMapTypeId = 0x0166038C;

    private readonly Dictionary<ulong, string> _hashToName = new();

    /// <summary>
    /// Number of loaded hash-to-name mappings.
    /// </summary>
    public int Count => _hashToName.Count;

    /// <summary>
    /// Loads all NameMap resources from a package and adds their mappings.
    /// </summary>
    /// <param name="package">The package to load from.</param>
    public async Task LoadFromPackageAsync(DbpfPackage package)
    {
        foreach (var entry in package.Resources)
        {
            if (entry.Key.ResourceType == NameMapTypeId)
            {
                try
                {
                    var data = await package.GetResourceDataAsync(entry);
                    var nameMap = new NameMapResource(entry.Key, data);

                    foreach (var (hash, name) in nameMap)
                    {
                        // Only add if not already present (first wins)
                        _hashToName.TryAdd(hash, name);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - some NameMaps may be malformed
                    System.Diagnostics.Debug.WriteLine($"[HashNameService] Failed to load NameMap: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Tries to get the name for a hash.
    /// </summary>
    /// <param name="hash">The FNV-64 hash.</param>
    /// <returns>The name if found, null otherwise.</returns>
    public string? TryGetName(ulong hash)
    {
        return _hashToName.TryGetValue(hash, out var name) ? name : null;
    }

    /// <summary>
    /// Gets a display-friendly name for a hash.
    /// Returns the name if found, or a formatted hex string as fallback.
    /// </summary>
    /// <param name="hash">The FNV-64 hash.</param>
    /// <returns>Human-readable name or hex string.</returns>
    public string GetDisplayName(ulong hash)
    {
        if (_hashToName.TryGetValue(hash, out var name))
        {
            return name;
        }
        return $"0x{hash:X16}";
    }

    /// <summary>
    /// Gets a display string showing both name and hash (if name available).
    /// </summary>
    /// <param name="hash">The FNV-64 hash.</param>
    /// <returns>Format: "name (0xHASH)" or just "0xHASH" if no name.</returns>
    public string GetDisplayNameWithHash(ulong hash)
    {
        if (_hashToName.TryGetValue(hash, out var name))
        {
            return $"{name} (0x{hash:X16})";
        }
        return $"0x{hash:X16}";
    }

    /// <summary>
    /// Clears all loaded mappings.
    /// </summary>
    public void Clear()
    {
        _hashToName.Clear();
    }

    /// <summary>
    /// Checks if a name contains the search text (case-insensitive).
    /// </summary>
    /// <param name="hash">The hash to look up.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <returns>True if the name contains the search text.</returns>
    public bool NameContains(ulong hash, string searchText)
    {
        if (_hashToName.TryGetValue(hash, out var name))
        {
            return name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
