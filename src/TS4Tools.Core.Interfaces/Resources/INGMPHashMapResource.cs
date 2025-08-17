using System.Diagnostics.CodeAnalysis;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for NGMP (Named Game Map) hash map resources.
/// Provides efficient key-value lookup functionality for game data mapping.
/// </summary>
[SuppressMessage("Naming", "S101:Types should be named in PascalCase", Justification = "NGMP is a well-known acronym in the domain")]
public interface INGMPHashMapResource : IResource
{
    /// <summary>
    /// Gets the version of the NGMP hash map format.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the number of entries in the hash map.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets all name hash keys in the hash map.
    /// </summary>
    IReadOnlyList<ulong> NameHashes { get; }

    /// <summary>
    /// Gets all instance values in the hash map.
    /// </summary>
    IReadOnlyList<ulong> Instances { get; }

    /// <summary>
    /// Checks if the hash map contains the specified name hash.
    /// </summary>
    /// <param name="nameHash">The name hash to search for.</param>
    /// <returns>True if the hash map contains the name hash; otherwise, false.</returns>
    bool ContainsNameHash(ulong nameHash);

    /// <summary>
    /// Gets the instance value for the specified name hash.
    /// </summary>
    /// <param name="nameHash">The name hash to look up.</param>
    /// <returns>The instance value if found; otherwise, null.</returns>
    ulong? GetInstance(ulong nameHash);

    /// <summary>
    /// Tries to get the instance value for the specified name hash.
    /// </summary>
    /// <param name="nameHash">The name hash to look up.</param>
    /// <param name="instance">When this method returns, contains the instance value if found; otherwise, 0.</param>
    /// <returns>True if the hash map contains the name hash; otherwise, false.</returns>
    bool TryGetInstance(ulong nameHash, out ulong instance);

    /// <summary>
    /// Adds or updates an entry in the hash map.
    /// </summary>
    /// <param name="nameHash">The name hash key.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddOrUpdateAsync(ulong nameHash, ulong instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entry from the hash map.
    /// </summary>
    /// <param name="nameHash">The name hash key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the entry was removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(ulong nameHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all entries from the hash map.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
