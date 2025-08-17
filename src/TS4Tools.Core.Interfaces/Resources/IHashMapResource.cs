using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for generic hash map resources that provide efficient key-value storage and lookup.
/// Supports various key and value types with optimized hash algorithms for game performance.
/// </summary>
public interface IHashMapResource : IResource
{
    /// <summary>
    /// Gets the version of the hash map format.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the number of entries in the hash map.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the hash map capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Gets the load factor of the hash map.
    /// </summary>
    double LoadFactor { get; }

    /// <summary>
    /// Checks if the hash map contains the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>True if the hash map contains the key; otherwise, false.</returns>
    bool ContainsKey(uint key);

    /// <summary>
    /// Gets the value for the specified key.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value if found; otherwise, the default value for T.</returns>
    T? GetValue<T>(uint key);

    /// <summary>
    /// Tries to get the value for the specified key.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default value for T.</param>
    /// <returns>True if the hash map contains the key; otherwise, false.</returns>
    bool TryGetValue<T>(uint key, out T? value);

    /// <summary>
    /// Sets the value for the specified key.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to set.</param>
    void SetValue(uint key, object value);

    /// <summary>
    /// Removes the entry with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed; otherwise, false.</returns>
    bool Remove(uint key);

    /// <summary>
    /// Clears all entries from the hash map.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all keys in the hash map.
    /// </summary>
    /// <returns>An enumerable collection of keys.</returns>
    IEnumerable<uint> GetKeys();

    /// <summary>
    /// Gets all values in the hash map.
    /// </summary>
    /// <returns>An enumerable collection of values.</returns>
    IEnumerable<object> GetValues();
}
